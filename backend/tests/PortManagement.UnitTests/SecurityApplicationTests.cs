using PortManagement.Application.Common;
using PortManagement.Application.Security;

namespace PortManagement.UnitTests;

public sealed class SecurityApplicationTests
{
    [Fact]
    public void SecurityRolesAreExplicitAndUnique()
    {
        Assert.Equal(4, SecurityRoles.All.Count);
        Assert.Contains(SecurityRoles.Administrator, SecurityRoles.All);
        Assert.Contains(SecurityRoles.Planner, SecurityRoles.All);
        Assert.Contains(SecurityRoles.Operator, SecurityRoles.All);
        Assert.Contains(SecurityRoles.Viewer, SecurityRoles.All);
    }

    [Fact]
    public async Task LoginHandlerDelegatesCredentialsAndClientIp()
    {
        var identity = new IdentityServiceFake();
        var handler = new LoginHandler(identity);
        var command = new LoginCommand("user@example.com", "Secret!12345");

        var result = await handler.HandleAsync(
            command,
            "127.0.0.1",
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(command, identity.LoginCommand);
        Assert.Equal("127.0.0.1", identity.ClientIp);
    }

    [Fact]
    public async Task CreateUserHandlerPreservesTheRequestedRole()
    {
        var identity = new IdentityServiceFake();
        var handler = new CreateUserHandler(identity);
        var command = new CreateUserCommand(
            "Planejador Demo",
            "planner@example.com",
            "Secret!12345",
            SecurityRoles.Planner,
            null);

        var result = await handler.HandleAsync(
            command,
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(SecurityRoles.Planner, identity.CreateUserCommand?.Role);
    }

    [Fact]
    public async Task PasswordResetHandlersDelegateTheRequestAndClientIp()
    {
        var identity = new IdentityServiceFake();
        var requestHandler = new RequestPasswordResetHandler(identity);
        var resetHandler = new ResetPasswordHandler(identity);
        var request = new RequestPasswordResetCommand("user@example.com");
        var reset = new ResetPasswordCommand(
            "10000000-0000-0000-0000-000000000001",
            "encoded-token",
            "NewSecret!12345");

        var requestResult = await requestHandler.HandleAsync(
            request,
            TestContext.Current.CancellationToken);
        var resetResult = await resetHandler.HandleAsync(
            reset,
            "127.0.0.1",
            TestContext.Current.CancellationToken);

        Assert.True(requestResult.IsSuccess);
        Assert.True(resetResult.IsSuccess);
        Assert.Equal(request, identity.PasswordResetRequest);
        Assert.Equal(reset, identity.PasswordResetCommand);
        Assert.Equal("127.0.0.1", identity.ClientIp);
    }

    [Fact]
    public async Task ListUsersHandlerRejectsInvalidPaginationBeforeQueryingIdentity()
    {
        var identity = new IdentityServiceFake();
        var handler = new ListUsersHandler(identity);

        var result = await handler.HandleAsync(
            new ListUsersQuery(Page: 0),
            TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        Assert.Equal("security.invalid_pagination", result.Error?.Code);
        Assert.Null(identity.ListUsersQuery);
    }

    [Fact]
    public async Task UserManagementHandlersDelegateFiltersAndUpdateContext()
    {
        var identity = new IdentityServiceFake();
        var listHandler = new ListUsersHandler(identity);
        var optionsHandler = new GetUserManagementOptionsHandler(identity);
        var updateHandler = new UpdateUserHandler(identity);
        var query = new ListUsersQuery(2, 10, "operador", SecurityRoles.Operator, true);
        var command = new UpdateUserCommand(
            Guid.Parse("10000000-0000-0000-0000-000000000001"),
            Guid.Parse("10000000-0000-0000-0000-000000000002"),
            "Operador Atualizado",
            SecurityRoles.Operator,
            Guid.Parse("40000000-0000-0000-0000-000000000002"),
            true,
            "version-1",
            "127.0.0.1");

        var listResult = await listHandler.HandleAsync(
            query,
            TestContext.Current.CancellationToken);
        var optionsResult = await optionsHandler.HandleAsync(
            TestContext.Current.CancellationToken);
        var updateResult = await updateHandler.HandleAsync(
            command,
            TestContext.Current.CancellationToken);

        Assert.True(listResult.IsSuccess);
        Assert.True(optionsResult.IsSuccess);
        Assert.True(updateResult.IsSuccess);
        Assert.Equal(query, identity.ListUsersQuery);
        Assert.Equal(command, identity.UpdateUserCommand);
    }

    private sealed class IdentityServiceFake : IIdentityService, IUserAdministrationService
    {
        public LoginCommand? LoginCommand { get; private set; }

        public CreateUserCommand? CreateUserCommand { get; private set; }

        public RequestPasswordResetCommand? PasswordResetRequest { get; private set; }

        public ResetPasswordCommand? PasswordResetCommand { get; private set; }

        public ListUsersQuery? ListUsersQuery { get; private set; }

        public UpdateUserCommand? UpdateUserCommand { get; private set; }

        public string? ClientIp { get; private set; }

        public Task<Result<AuthTokenResponse>> LoginAsync(
            LoginCommand command,
            string clientIp,
            CancellationToken cancellationToken)
        {
            LoginCommand = command;
            ClientIp = clientIp;
            return Task.FromResult(Result.Success(CreateTokenResponse()));
        }

        public Task<Result<AuthTokenResponse>> RefreshSessionAsync(
            RefreshSessionCommand command,
            string clientIp,
            CancellationToken cancellationToken) =>
            Task.FromResult(Result.Success(CreateTokenResponse()));

        public Task<Result<bool>> RevokeSessionAsync(
            RevokeSessionCommand command,
            string clientIp,
            CancellationToken cancellationToken) =>
            Task.FromResult(Result.Success(true));

        public Task<Result<bool>> RequestPasswordResetAsync(
            RequestPasswordResetCommand command,
            CancellationToken cancellationToken)
        {
            PasswordResetRequest = command;
            return Task.FromResult(Result.Success(true));
        }

        public Task<Result<bool>> ResetPasswordAsync(
            ResetPasswordCommand command,
            string clientIp,
            CancellationToken cancellationToken)
        {
            PasswordResetCommand = command;
            ClientIp = clientIp;
            return Task.FromResult(Result.Success(true));
        }

        public Task<Result<AuthenticatedUserResponse>> GetUserAsync(
            Guid userId,
            CancellationToken cancellationToken) =>
            Task.FromResult(Result.Success(CreateUserResponse()));

        public Task<Result<AuthenticatedUserResponse>> CreateUserAsync(
            CreateUserCommand command,
            CancellationToken cancellationToken)
        {
            CreateUserCommand = command;
            return Task.FromResult(Result.Success(CreateUserResponse(command.Role)));
        }

        public Task<Result<PagedResult<ManagedUserResponse>>> ListUsersAsync(
            ListUsersQuery query,
            CancellationToken cancellationToken)
        {
            ListUsersQuery = query;
            return Task.FromResult(Result.Success(new PagedResult<ManagedUserResponse>(
                [CreateManagedUserResponse()],
                query.Page,
                query.PageSize,
                1)));
        }

        public Task<Result<UserManagementOptionsResponse>> GetUserManagementOptionsAsync(
            CancellationToken cancellationToken) =>
            Task.FromResult(Result.Success(new UserManagementOptionsResponse(
                SecurityRoles.All,
                [])));

        public Task<Result<ManagedUserResponse>> UpdateUserAsync(
            UpdateUserCommand command,
            CancellationToken cancellationToken)
        {
            UpdateUserCommand = command;
            return Task.FromResult(Result.Success(CreateManagedUserResponse(command.Role)));
        }

        private static AuthTokenResponse CreateTokenResponse() => new(
            "access-token",
            "refresh-token",
            DateTimeOffset.UtcNow.AddMinutes(15),
            CreateUserResponse());

        private static AuthenticatedUserResponse CreateUserResponse(
            string role = SecurityRoles.Viewer) => new(
            Guid.Parse("10000000-0000-0000-0000-000000000001"),
            "Usuário Demo",
            "user@example.com",
            null,
            [role]);

        private static ManagedUserResponse CreateManagedUserResponse(
            string role = SecurityRoles.Viewer) => new(
            Guid.Parse("10000000-0000-0000-0000-000000000001"),
            "Usuário Demo",
            "user@example.com",
            null,
            null,
            true,
            DateTimeOffset.UtcNow,
            "version-1",
            [role]);
    }
}
