using PortManagement.Application.Common;

namespace PortManagement.Application.Security;

public sealed record LoginCommand(string Email, string Password);

public sealed record RefreshSessionCommand(string RefreshToken);

public sealed record RevokeSessionCommand(string RefreshToken);

public sealed record RequestPasswordResetCommand(string Email);

public sealed record ResetPasswordCommand(
    string UserId,
    string Token,
    string NewPassword);

public sealed record CreateUserCommand(
    string DisplayName,
    string Email,
    string Password,
    string Role,
    Guid? OrganizationId);

public sealed record ListUsersQuery(
    int Page = 1,
    int PageSize = 20,
    string? Search = null,
    string? Role = null,
    bool? IsActive = null);

public sealed record UpdateUserCommand(
    Guid UserId,
    Guid ActingUserId,
    string DisplayName,
    string Role,
    Guid? OrganizationId,
    bool IsActive,
    string ExpectedVersion,
    string ClientIp);

public sealed record ManagedUserResponse(
    Guid Id,
    string DisplayName,
    string Email,
    Guid? OrganizationId,
    string? OrganizationName,
    bool IsActive,
    DateTimeOffset CreatedAtUtc,
    string Version,
    IReadOnlyCollection<string> Roles);

public sealed record OrganizationOptionResponse(
    Guid Id,
    string Name,
    string Type);

public sealed record UserManagementOptionsResponse(
    IReadOnlyCollection<string> Roles,
    IReadOnlyCollection<OrganizationOptionResponse> Organizations);

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

    Task<Result<bool>> RequestPasswordResetAsync(
        RequestPasswordResetCommand command,
        CancellationToken cancellationToken);

    Task<Result<bool>> ResetPasswordAsync(
        ResetPasswordCommand command,
        string clientIp,
        CancellationToken cancellationToken);

    Task<Result<AuthenticatedUserResponse>> GetUserAsync(
        Guid userId,
        CancellationToken cancellationToken);

}

public interface IUserAdministrationService
{
    Task<Result<AuthenticatedUserResponse>> CreateUserAsync(
        CreateUserCommand command,
        CancellationToken cancellationToken);

    Task<Result<PagedResult<ManagedUserResponse>>> ListUsersAsync(
        ListUsersQuery query,
        CancellationToken cancellationToken);

    Task<Result<UserManagementOptionsResponse>> GetUserManagementOptionsAsync(
        CancellationToken cancellationToken);

    Task<Result<ManagedUserResponse>> UpdateUserAsync(
        UpdateUserCommand command,
        CancellationToken cancellationToken);
}

public interface IPasswordResetEmailSender
{
    Task<bool> SendAsync(
        string recipientEmail,
        string displayName,
        string userId,
        string encodedToken,
        CancellationToken cancellationToken);
}
