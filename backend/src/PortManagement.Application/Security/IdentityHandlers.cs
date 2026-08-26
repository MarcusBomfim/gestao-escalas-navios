using PortManagement.Application.Common;

namespace PortManagement.Application.Security;

public sealed class LoginHandler(IIdentityService identityService)
{
    public Task<Result<AuthTokenResponse>> HandleAsync(
        LoginCommand command,
        string clientIp,
        CancellationToken cancellationToken = default) =>
        identityService.LoginAsync(command, clientIp, cancellationToken);
}

public sealed class RefreshSessionHandler(IIdentityService identityService)
{
    public Task<Result<AuthTokenResponse>> HandleAsync(
        RefreshSessionCommand command,
        string clientIp,
        CancellationToken cancellationToken = default) =>
        identityService.RefreshSessionAsync(command, clientIp, cancellationToken);
}

public sealed class RevokeSessionHandler(IIdentityService identityService)
{
    public Task<Result<bool>> HandleAsync(
        RevokeSessionCommand command,
        string clientIp,
        CancellationToken cancellationToken = default) =>
        identityService.RevokeSessionAsync(command, clientIp, cancellationToken);
}

public sealed class RequestPasswordResetHandler(IIdentityService identityService)
{
    public Task<Result<bool>> HandleAsync(
        RequestPasswordResetCommand command,
        CancellationToken cancellationToken = default) =>
        identityService.RequestPasswordResetAsync(command, cancellationToken);
}

public sealed class ResetPasswordHandler(IIdentityService identityService)
{
    public Task<Result<bool>> HandleAsync(
        ResetPasswordCommand command,
        string clientIp,
        CancellationToken cancellationToken = default) =>
        identityService.ResetPasswordAsync(command, clientIp, cancellationToken);
}

public sealed class GetCurrentUserHandler(IIdentityService identityService)
{
    public Task<Result<AuthenticatedUserResponse>> HandleAsync(
        Guid userId,
        CancellationToken cancellationToken = default) =>
        identityService.GetUserAsync(userId, cancellationToken);
}

public sealed class CreateUserHandler(IIdentityService identityService)
{
    public Task<Result<AuthenticatedUserResponse>> HandleAsync(
        CreateUserCommand command,
        CancellationToken cancellationToken = default) =>
        identityService.CreateUserAsync(command, cancellationToken);
}
