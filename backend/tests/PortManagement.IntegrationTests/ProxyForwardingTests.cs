using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using PortManagement.Api.Security;

namespace PortManagement.IntegrationTests;

/// <summary>
/// A aplicação fica atrás de um proxy que encerra o TLS. Sem processar
/// <c>X-Forwarded-For</c>, o endereço de todas as requisições é o do proxy: o
/// limite de tentativas de login vira global e a auditoria registra sempre o
/// mesmo IP. Estes testes travam esse comportamento.
/// </summary>
public sealed class ProxyForwardingTests
{
    private const string ProxyNetwork = "172.18.0.0/16";
    private static readonly IPAddress ProxyAddress = IPAddress.Parse("172.18.0.5");
    private static readonly IPAddress ClientAddress = IPAddress.Parse("203.0.113.42");

    [Fact]
    public void ForwardedAddressIsAcceptedFromATrustedProxy()
    {
        var context = ApplyForwarding(
            connectionAddress: ProxyAddress,
            forwardedFor: ClientAddress.ToString());

        Assert.Equal(ClientAddress, context.Connection.RemoteIpAddress);
    }

    [Fact]
    public void ForwardedAddressIsIgnoredWhenTheConnectionIsNotATrustedProxy()
    {
        var attacker = IPAddress.Parse("198.51.100.7");

        var context = ApplyForwarding(
            connectionAddress: attacker,
            forwardedFor: "10.0.0.1");

        // Sem essa recusa, qualquer cliente escolheria o IP que aparece no
        // rate limit e escaparia do limite trocando o cabeçalho a cada tentativa.
        Assert.Equal(attacker, context.Connection.RemoteIpAddress);
    }

    [Fact]
    public void OnlyTheClosestHopIsTrusted()
    {
        var context = ApplyForwarding(
            connectionAddress: ProxyAddress,
            forwardedFor: $"10.9.9.9, {ClientAddress}");

        Assert.Equal(ClientAddress, context.Connection.RemoteIpAddress);
    }

    [Fact]
    public void ForwardedProtocolIsAcceptedFromATrustedProxy()
    {
        var context = ApplyForwarding(
            connectionAddress: ProxyAddress,
            forwardedFor: ClientAddress.ToString(),
            forwardedProto: "https");

        Assert.Equal("https", context.Request.Scheme);
        Assert.True(context.Request.IsHttps);
    }

    [Fact]
    public void LoopbackIsNotTrustedByDefault()
    {
        // O padrão do ASP.NET Core confia em loopback e redes privadas. Aqui a
        // confiança precisa ser declarada, e nada mais é aceito.
        var context = ApplyForwarding(
            connectionAddress: IPAddress.Loopback,
            forwardedFor: ClientAddress.ToString());

        Assert.Equal(IPAddress.Loopback, context.Connection.RemoteIpAddress);
    }

    [Fact]
    public void RateLimitPartitionUsesTheResolvedClientAddress()
    {
        var fromClient = SecurityConfiguration.BuildAuthenticationPartitionKey(
            CreateContext(ClientAddress, "/api/v1/auth/login"));
        var fromProxy = SecurityConfiguration.BuildAuthenticationPartitionKey(
            CreateContext(ProxyAddress, "/api/v1/auth/login"));

        Assert.StartsWith("203.0.113.42:", fromClient, StringComparison.Ordinal);
        Assert.NotEqual(fromProxy, fromClient);
    }

    [Fact]
    public void RateLimitPartitionSeparatesPaths()
    {
        var login = SecurityConfiguration.BuildAuthenticationPartitionKey(
            CreateContext(ClientAddress, "/api/v1/auth/login"));
        var forgot = SecurityConfiguration.BuildAuthenticationPartitionKey(
            CreateContext(ClientAddress, "/api/v1/auth/forgot-password"));

        Assert.NotEqual(login, forgot);
    }

    [Fact]
    public void RateLimitPartitionNormalisesIPv4MappedAddresses()
    {
        var mapped = SecurityConfiguration.BuildAuthenticationPartitionKey(
            CreateContext(ClientAddress.MapToIPv6(), "/api/v1/auth/login"));
        var plain = SecurityConfiguration.BuildAuthenticationPartitionKey(
            CreateContext(ClientAddress, "/api/v1/auth/login"));

        // Sem normalizar, o mesmo cliente ganharia duas cotas.
        Assert.Equal(plain, mapped);
    }

    private static DefaultHttpContext CreateContext(IPAddress address, string path)
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = address;
        context.Request.Path = path;
        return context;
    }

    private static DefaultHttpContext ApplyForwarding(
        IPAddress connectionAddress,
        string forwardedFor,
        string? forwardedProto = null)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Security:TrustedProxies:0"] = ProxyNetwork
            })
            .Build();

        var services = new ServiceCollection();
        services.AddProxyForwarding(configuration);
        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<ForwardedHeadersOptions>>().Value;

        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = connectionAddress;
        context.Request.Headers["X-Forwarded-For"] = forwardedFor;

        if (forwardedProto is not null)
        {
            context.Request.Headers["X-Forwarded-Proto"] = forwardedProto;
        }

        var middleware = new ForwardedHeadersMiddleware(
            _ => Task.CompletedTask,
            NullLoggerFactory.Instance,
            Options.Create(options));

        middleware.ApplyForwarders(context);
        return context;
    }
}
