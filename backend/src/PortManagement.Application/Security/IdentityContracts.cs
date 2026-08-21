using PortManagement.Application.Common;

namespace PortManagement.Application.Security;

public sealed record LoginCommand(string Email, string Password);

public sealed record RefreshSessionCommand(string RefreshToken);

public sealed record RevokeSessionCommand(string RefreshToken);

public sealed record CreateUserCommand(
    string DisplayName,
    string Email,
    string Password,
    string Role,
    Guid? OrganizationId);

public sealed record AuthenticatedUserResponse(
    Guid Id,
    string DisplayName,
    string Email,
    Guid? OrganizationId,
    IReadOnlyCollection<string> Roles);

public sealed record AuthTokenResponse(
    string AccessToken,
    string RefreshToken,
    DateTimeOffset AccessTokenExpiresAtUtc,
    AuthenticatedUserResponse User);

public interface IIdentityService
{
    Task<Result<AuthTokenResponse>> LoginAsync(
        LoginCommand command,
        string clientIp,
        CancellationToken cancellationToken);

    Task<Result<AuthTokenResponse>> RefreshSessionAsync(
        RefreshSessionCommand command,
        string clientIp,
        CancellationToken cancellationToken);

    Task<Result<bool>> RevokeSessionAsync(
        RevokeSessionCommand command,
        string clientIp,
        CancellationToken cancellationToken);

    Task<Result<AuthenticatedUserResponse>> GetUserAsync(
        Guid userId,
        CancellationToken cancellationToken);

    Task<Result<AuthenticatedUserResponse>> CreateUserAsync(
        CreateUserCommand command,
        CancellationToken cancellationToken);
}
