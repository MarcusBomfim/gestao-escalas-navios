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
        Assert.True(paths.TryGetProperty("/api/v1/auth/login", out var login));

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
