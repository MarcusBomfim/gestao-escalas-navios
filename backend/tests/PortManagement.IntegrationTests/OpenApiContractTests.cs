using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace PortManagement.IntegrationTests;

public sealed class OpenApiContractTests(OpenApiContractApplicationFactory factory)
    : IClassFixture<OpenApiContractApplicationFactory>
{
    [Fact]
    public async Task VersionedDocumentDescribesRoutesAndBearerSecurity()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/openapi/v1.json", cancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        await using var content = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(
            content,
            cancellationToken: cancellationToken);
        var root = document.RootElement;

        Assert.StartsWith("3.", root.GetProperty("openapi").GetString());
        Assert.Equal(
            "Port Management API",
            root.GetProperty("info").GetProperty("title").GetString());
        Assert.Equal("v1", root.GetProperty("info").GetProperty("version").GetString());

        var paths = root.GetProperty("paths");
        Assert.True(paths.TryGetProperty("/api/v1/control-tower", out var controlTower));
        Assert.True(paths.TryGetProperty("/api/v1/port-calls", out _));
        Assert.True(paths.TryGetProperty("/api/v1/vessels", out _));
        Assert.True(paths.TryGetProperty("/api/v1/users", out var users));
        Assert.True(paths.TryGetProperty("/api/v1/users/options", out var userOptions));
        Assert.True(paths.TryGetProperty("/api/v1/users/{id}", out var updateUser));
        Assert.True(paths.TryGetProperty(
            "/api/v1/admin/master-data/organizations",
            out var organizations));
        Assert.True(paths.TryGetProperty(
            "/api/v1/admin/master-data/organizations/{id}",
            out var updateOrganization));
        Assert.True(paths.TryGetProperty(
            "/api/v1/admin/master-data/ports",
            out var ports));
        Assert.True(paths.TryGetProperty(
            "/api/v1/admin/master-data/ports/{id}",
            out var updatePort));
        Assert.True(paths.TryGetProperty(
            "/api/v1/admin/master-data/ports/{portId}/terminals",
            out var createTerminal));
        Assert.True(paths.TryGetProperty(
            "/api/v1/admin/master-data/terminals/{id}",
            out var updateTerminal));
        Assert.True(paths.TryGetProperty(
            "/api/v1/admin/master-data/terminals/{terminalId}/berths",
            out var createBerth));
        Assert.True(paths.TryGetProperty(
            "/api/v1/admin/master-data/berths/{id}",
            out var updateBerth));
        Assert.True(paths.TryGetProperty("/api/v1/auth/login", out var login));
        Assert.True(paths.TryGetProperty("/api/v1/auth/demo", out var publicDemo));
        Assert.True(paths.TryGetProperty(
            "/api/v1/auth/forgot-password",
            out var forgotPassword));
        Assert.True(paths.TryGetProperty(
            "/api/v1/auth/reset-password",
            out var resetPassword));

        var schemes = root
            .GetProperty("components")
            .GetProperty("securitySchemes");
        Assert.Equal(
            "bearer",
            schemes.GetProperty("Bearer").GetProperty("scheme").GetString());

        var protectedOperation = controlTower.GetProperty("get");
        var security = protectedOperation.GetProperty("security");
        Assert.Contains(security.EnumerateArray(),
            requirement => requirement.TryGetProperty("Bearer", out _));
        Assert.True(protectedOperation.GetProperty("responses").TryGetProperty("401", out _));
        Assert.True(protectedOperation.GetProperty("responses").TryGetProperty("403", out _));
        Assert.False(login.GetProperty("post").TryGetProperty("security", out _));
        Assert.False(publicDemo.GetProperty("post").TryGetProperty("security", out _));
        Assert.False(forgotPassword.GetProperty("post").TryGetProperty("security", out _));
        Assert.False(resetPassword.GetProperty("post").TryGetProperty("security", out _));
        Assert.True(users.GetProperty("get").TryGetProperty("security", out _));
        Assert.True(userOptions.GetProperty("get").TryGetProperty("security", out _));
        Assert.True(updateUser.GetProperty("put").TryGetProperty("security", out _));
        Assert.True(organizations.GetProperty("get").TryGetProperty("security", out _));
        Assert.True(organizations.GetProperty("post").TryGetProperty("security", out _));
        Assert.True(updateOrganization.GetProperty("put").TryGetProperty("security", out _));
        Assert.True(ports.GetProperty("get").TryGetProperty("security", out _));
        Assert.True(ports.GetProperty("post").TryGetProperty("security", out _));
        Assert.True(updatePort.GetProperty("put").TryGetProperty("security", out _));
        Assert.True(createTerminal.GetProperty("post").TryGetProperty("security", out _));
        Assert.True(updateTerminal.GetProperty("put").TryGetProperty("security", out _));
        Assert.True(createBerth.GetProperty("post").TryGetProperty("security", out _));
        Assert.True(updateBerth.GetProperty("put").TryGetProperty("security", out _));
    }

    [Fact]
    public async Task InteractiveReferenceIsAvailableInDevelopment()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/docs", cancellationToken);
        var content = await response.Content.ReadAsStringAsync(cancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Port Management API", content, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PublicDemoEndpointIsHiddenWhenTheFeatureIsDisabled()
    {
        using var client = factory.CreateClient();
        using var response = await client.PostAsync(
            "/api/v1/auth/demo",
            content: null,
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ApiDocumentationIsNotExposedInProduction()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var productionFactory =
            new OpenApiContractApplicationFactory(Environments.Production);
        using var client = productionFactory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("https://localhost"),
                AllowAutoRedirect = false
            });
        using var openApiResponse = await client.GetAsync(
            "/openapi/v1.json",
            cancellationToken);
        using var referenceResponse = await client.GetAsync("/docs", cancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, openApiResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, referenceResponse.StatusCode);
    }
}

public sealed class OpenApiContractApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string environment;

    public OpenApiContractApplicationFactory()
        : this(Environments.Development)
    {
    }

    internal OpenApiContractApplicationFactory(string environment)
    {
        this.environment = environment;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(environment);
        builder.UseSetting(
            "ConnectionStrings:Database",
            "Host=127.0.0.1;Port=5432;Database=openapi_contract;Username=contract");
        builder.UseSetting("Jwt:Issuer", "PortManagement.ContractTests");
        builder.UseSetting("Jwt:Audience", "PortManagement.ContractTests");
        builder.UseSetting("Jwt:SigningKey", new string('x', 64));
        builder.ConfigureServices(services => services.RemoveAll<IHostedService>());
    }
}
