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

public sealed class CreateUserHandler(IUserAdministrationService userAdministration)
{
    public Task<Result<AuthenticatedUserResponse>> HandleAsync(
        CreateUserCommand command,
        CancellationToken cancellationToken = default) =>
        userAdministration.CreateUserAsync(command, cancellationToken);
}

public sealed class ListUsersHandler(IUserAdministrationService userAdministration)
{
    public Task<Result<PagedResult<ManagedUserResponse>>> HandleAsync(
        ListUsersQuery query,
        CancellationToken cancellationToken = default)
    {
        if (query.Page is < 1 or > 1_000_000 || query.PageSize is < 1 or > 100)
        {
            return Task.FromResult(Result.Failure<PagedResult<ManagedUserResponse>>(
                ApplicationErrors.Validation(
                    "security.invalid_pagination",
                    "A página e o tamanho da página possuem valores inválidos.")));
        }

        if (query.Search?.Length > 160)
        {
            return Task.FromResult(Result.Failure<PagedResult<ManagedUserResponse>>(
                ApplicationErrors.Validation(
                    "security.invalid_user_search",
                    "A busca deve possuir no máximo 160 caracteres.")));
        }

        if (!string.IsNullOrWhiteSpace(query.Role) && !SecurityRoles.All.Contains(query.Role))
        {
            return Task.FromResult(Result.Failure<PagedResult<ManagedUserResponse>>(
                ApplicationErrors.Validation(
                    "security.invalid_role",
                    "O papel informado não é reconhecido.")));
        }

        return userAdministration.ListUsersAsync(query, cancellationToken);
    }
}

public sealed class GetUserManagementOptionsHandler(IUserAdministrationService userAdministration)
{
    public Task<Result<UserManagementOptionsResponse>> HandleAsync(
        CancellationToken cancellationToken = default) =>
        userAdministration.GetUserManagementOptionsAsync(cancellationToken);
}

public sealed class UpdateUserHandler(IUserAdministrationService userAdministration)
{
    public Task<Result<ManagedUserResponse>> HandleAsync(
        UpdateUserCommand command,
        CancellationToken cancellationToken = default) =>
        userAdministration.UpdateUserAsync(command, cancellationToken);
}
