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

    private sealed class IdentityServiceFake : IIdentityService
    {
        public LoginCommand? LoginCommand { get; private set; }

        public CreateUserCommand? CreateUserCommand { get; private set; }

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
    }
}
