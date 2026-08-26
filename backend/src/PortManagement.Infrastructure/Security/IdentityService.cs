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

    public async Task<Result<AuthenticatedUserResponse>> CreateUserAsync(
        CreateUserCommand command,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.DisplayName) ||
            command.DisplayName.Trim().Length > 160 ||
            string.IsNullOrWhiteSpace(command.Email) ||
            command.Email.Length > 320 ||
            string.IsNullOrEmpty(command.Password) ||
            command.Password.Length > 256)
        {
            return Result.Failure<AuthenticatedUserResponse>(new ApplicationError(
                "security.invalid_user",
                "Nome, e-mail ou senha possuem formato inválido.",
                ApplicationErrorType.Validation));
        }

        if (!SecurityRoles.All.Contains(command.Role))
        {
            return Result.Failure<AuthenticatedUserResponse>(new ApplicationError(
                "security.invalid_role",
                "O papel informado não é reconhecido.",
                ApplicationErrorType.Validation));
        }

        if (command.OrganizationId is Guid organizationId &&
            !await database.Organizations.AnyAsync(
                organization => organization.Id == organizationId,
                cancellationToken))
        {
            return Result.Failure<AuthenticatedUserResponse>(new ApplicationError(
                "security.organization_not_found",
                "A organização informada não existe.",
                ApplicationErrorType.Validation));
        }

        var email = command.Email.Trim();
        if (await userManager.FindByEmailAsync(email) is not null)
        {
            return Result.Failure<AuthenticatedUserResponse>(new ApplicationError(
                "security.email_already_registered",
                "Já existe um usuário com esse e-mail.",
                ApplicationErrorType.Conflict));
        }

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            DisplayName = command.DisplayName.Trim(),
            OrganizationId = command.OrganizationId,
            CreatedAtUtc = timeProvider.GetUtcNow(),
            IsActive = true
        };

        await using var transaction = await database.Database.BeginTransactionAsync(cancellationToken);
        var creation = await userManager.CreateAsync(user, command.Password);
        if (!creation.Succeeded)
        {
            return Result.Failure<AuthenticatedUserResponse>(IdentityValidationFailure(creation));
        }

        var roleAssignment = await userManager.AddToRoleAsync(user, command.Role);
        if (!roleAssignment.Succeeded)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Result.Failure<AuthenticatedUserResponse>(IdentityValidationFailure(roleAssignment));
        }

        await transaction.CommitAsync(cancellationToken);
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
            new(ClaimTypes.Email, user.Email ?? string.Empty)
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
