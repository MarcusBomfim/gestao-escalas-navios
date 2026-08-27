using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PortManagement.Application.Common;
using PortManagement.Application.Security;
using PortManagement.Infrastructure.Identity;
using PortManagement.Infrastructure.Persistence;

namespace PortManagement.Infrastructure.Security;

internal sealed class IdentityService(
    UserManager<ApplicationUser> userManager,
    PortManagementDbContext database,
    JwtOptions jwtOptions,
    IPasswordResetEmailSender passwordResetEmailSender,
    TimeProvider timeProvider) : IIdentityService
{
    private static readonly ApplicationError InvalidCredentials = new(
        "security.invalid_credentials",
        "E-mail ou senha inválidos.",
        ApplicationErrorType.Unauthorized);

    private static readonly ApplicationError InvalidRefreshToken = new(
        "security.invalid_refresh_token",
        "A sessão não é válida ou já expirou.",
        ApplicationErrorType.Unauthorized);

    private static readonly ApplicationError InvalidPasswordReset = new(
        "security.invalid_password_reset",
        "O link de redefinição é inválido ou expirou.",
        ApplicationErrorType.Validation);

    public async Task<Result<AuthTokenResponse>> LoginAsync(
        LoginCommand command,
        string clientIp,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(command.Email) ||
            string.IsNullOrEmpty(command.Password) ||
            command.Email.Length > 320 ||
            command.Password.Length > 256)
        {
            return Result.Failure<AuthTokenResponse>(InvalidCredentials);
        }

        var email = command.Email.Trim();
        var user = await userManager.FindByEmailAsync(email);
        if (user is null || !user.IsActive)
        {
            return Result.Failure<AuthTokenResponse>(InvalidCredentials);
        }

        if (await userManager.IsLockedOutAsync(user))
        {
            return Result.Failure<AuthTokenResponse>(new ApplicationError(
                "security.account_locked",
                "Conta temporariamente bloqueada após tentativas inválidas.",
                ApplicationErrorType.Unauthorized));
        }

        if (!await userManager.CheckPasswordAsync(user, command.Password))
        {
            await userManager.AccessFailedAsync(user);
            return Result.Failure<AuthTokenResponse>(InvalidCredentials);
        }

        await userManager.ResetAccessFailedCountAsync(user);
        return Result.Success(await IssueSessionAsync(user, clientIp, cancellationToken));
    }

    public async Task<Result<AuthTokenResponse>> RefreshSessionAsync(
        RefreshSessionCommand command,
        string clientIp,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.RefreshToken) || command.RefreshToken.Length > 512)
        {
            return Result.Failure<AuthTokenResponse>(InvalidRefreshToken);
        }

        var now = timeProvider.GetUtcNow();
        var tokenHash = HashToken(command.RefreshToken);
        var storedToken = await database.RefreshTokens
            .Include(token => token.User)
            .SingleOrDefaultAsync(token => token.TokenHash == tokenHash, cancellationToken);

        if (storedToken is null || !storedToken.User.IsActive)
        {
            return Result.Failure<AuthTokenResponse>(InvalidRefreshToken);
        }

        if (storedToken.RevokedAtUtc is not null)
        {
            await RevokeAllActiveSessionsAsync(storedToken.UserId, now, clientIp, cancellationToken);
            return Result.Failure<AuthTokenResponse>(InvalidRefreshToken);
        }

        if (storedToken.ExpiresAtUtc <= now)
        {
            storedToken.Revoke(now, clientIp);
            await database.SaveChangesAsync(cancellationToken);
            return Result.Failure<AuthTokenResponse>(InvalidRefreshToken);
        }

        var replacement = GenerateRefreshToken();
        storedToken.Revoke(now, clientIp, replacement.Hash);
        database.RefreshTokens.Add(new RefreshToken(
            Guid.NewGuid(),
            storedToken.UserId,
            replacement.Hash,
            now,
            now.AddDays(jwtOptions.RefreshTokenDays),
            clientIp));

        try
        {
            await database.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            database.ChangeTracker.Clear();
            return Result.Failure<AuthTokenResponse>(InvalidRefreshToken);
        }

        return Result.Success(await CreateTokenResponseAsync(
            storedToken.User,
            replacement.Raw,
            now));
    }

    public async Task<Result<bool>> RevokeSessionAsync(
        RevokeSessionCommand command,
        string clientIp,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.RefreshToken) || command.RefreshToken.Length > 512)
        {
            return Result.Success(true);
        }

        var tokenHash = HashToken(command.RefreshToken);
        var storedToken = await database.RefreshTokens.SingleOrDefaultAsync(
            token => token.TokenHash == tokenHash,
            cancellationToken);

        if (storedToken is null || storedToken.RevokedAtUtc is not null)
        {
            return Result.Success(true);
        }

        storedToken.Revoke(timeProvider.GetUtcNow(), clientIp);

        try
        {
            await database.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            database.ChangeTracker.Clear();
        }

        return Result.Success(true);
    }

    public async Task<Result<bool>> RequestPasswordResetAsync(
        RequestPasswordResetCommand command,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(command.Email) || command.Email.Length > 320)
        {
            return Result.Success(true);
        }

        var user = await userManager.FindByEmailAsync(command.Email.Trim());
        if (user is null || !user.IsActive || !await userManager.IsEmailConfirmedAsync(user))
        {
            return Result.Success(true);
        }

        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        var encodedToken = Base64UrlEncoder.Encode(Encoding.UTF8.GetBytes(token));
        await passwordResetEmailSender.SendAsync(
            user.Email ?? string.Empty,
            user.DisplayName,
            user.Id.ToString(),
            encodedToken,
            cancellationToken);

        return Result.Success(true);
    }

    public async Task<Result<bool>> ResetPasswordAsync(
        ResetPasswordCommand command,
        string clientIp,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!Guid.TryParse(command.UserId, out var userId) ||
            string.IsNullOrWhiteSpace(command.Token) ||
            command.Token.Length > 8_192 ||
            string.IsNullOrEmpty(command.NewPassword) ||
            command.NewPassword.Length > 256)
        {
            return Result.Failure<bool>(InvalidPasswordReset);
        }

        string token;
        try
        {
            token = Encoding.UTF8.GetString(Base64UrlEncoder.DecodeBytes(command.Token));
        }
        catch (FormatException)
        {
            return Result.Failure<bool>(InvalidPasswordReset);
        }
        catch (ArgumentException)
        {
            return Result.Failure<bool>(InvalidPasswordReset);
        }

        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null || !user.IsActive)
        {
            return Result.Failure<bool>(InvalidPasswordReset);
        }

        var reset = await userManager.ResetPasswordAsync(user, token, command.NewPassword);
        if (!reset.Succeeded)
        {
            return reset.Errors.Any(error =>
                    string.Equals(error.Code, "InvalidToken", StringComparison.Ordinal))
                ? Result.Failure<bool>(InvalidPasswordReset)
                : Result.Failure<bool>(IdentityValidationFailure(reset));
        }

        await RevokeAllActiveSessionsAsync(
            user.Id,
            timeProvider.GetUtcNow(),
            clientIp,
            cancellationToken);

        return Result.Success(true);
    }

    public async Task<Result<AuthenticatedUserResponse>> GetUserAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null || !user.IsActive)
        {
            return Result.Failure<AuthenticatedUserResponse>(new ApplicationError(
                "security.user_not_found",
                "Usuário não encontrado.",
                ApplicationErrorType.NotFound));
        }

        return Result.Success(await ToResponseAsync(user));
    }

    private async Task<AuthTokenResponse> IssueSessionAsync(
        ApplicationUser user,
        string clientIp,
        CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var refreshToken = GenerateRefreshToken();
        database.RefreshTokens.Add(new RefreshToken(
            Guid.NewGuid(),
            user.Id,
            refreshToken.Hash,
            now,
            now.AddDays(jwtOptions.RefreshTokenDays),
            clientIp));
        await database.SaveChangesAsync(cancellationToken);

        return await CreateTokenResponseAsync(user, refreshToken.Raw, now);
    }

    private async Task<AuthTokenResponse> CreateTokenResponseAsync(
        ApplicationUser user,
        string rawRefreshToken,
        DateTimeOffset now)
    {
        var roles = await userManager.GetRolesAsync(user);
        var expiresAt = now.AddMinutes(jwtOptions.AccessTokenMinutes);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.DisplayName),
            new(ClaimTypes.Email, user.Email ?? string.Empty),
            new(DataScopeClaims.SecurityStamp, user.SecurityStamp ?? string.Empty)
        };

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));
        var dataScopeClaims = await userManager.GetClaimsAsync(user);
        claims.AddRange(dataScopeClaims.Where(claim => claim.Type == DataScopeClaims.Scope));
        if (user.OrganizationId is Guid organizationId)
        {
            claims.Add(new Claim(DataScopeClaims.OrganizationId, organizationId.ToString()));
        }

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
            SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            jwtOptions.Issuer,
            jwtOptions.Audience,
            claims,
            now.UtcDateTime,
            expiresAt.UtcDateTime,
            credentials);

        return new AuthTokenResponse(
            new JwtSecurityTokenHandler().WriteToken(token),
            rawRefreshToken,
            expiresAt,
            new AuthenticatedUserResponse(
                user.Id,
                user.DisplayName,
                user.Email ?? string.Empty,
                user.OrganizationId,
                roles.ToArray()));
    }

    private async Task<AuthenticatedUserResponse> ToResponseAsync(ApplicationUser user)
    {
        var roles = await userManager.GetRolesAsync(user);
        return new AuthenticatedUserResponse(
            user.Id,
            user.DisplayName,
            user.Email ?? string.Empty,
            user.OrganizationId,
            roles.ToArray());
    }

    private Task<int> RevokeAllActiveSessionsAsync(
        Guid userId,
        DateTimeOffset now,
        string clientIp,
        CancellationToken cancellationToken) =>
        database.RefreshTokens
            .Where(token => token.UserId == userId && token.RevokedAtUtc == null)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(token => token.RevokedAtUtc, now)
                    .SetProperty(token => token.RevokedByIp, clientIp),
                cancellationToken);

    private static (string Raw, string Hash) GenerateRefreshToken()
    {
        var raw = Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(64));
        return (raw, HashToken(raw));
    }

    private static string HashToken(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    private static ApplicationError IdentityValidationFailure(IdentityResult result) => new(
        "security.identity_validation_failed",
        string.Join(" ", result.Errors.Select(error => error.Description)),
        ApplicationErrorType.Validation);
}
