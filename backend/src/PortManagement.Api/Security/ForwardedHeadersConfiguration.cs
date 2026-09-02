using System.Net;
using Microsoft.AspNetCore.HttpOverrides;
using IPNetwork = System.Net.IPNetwork;

namespace PortManagement.Api.Security;

/// <summary>
/// A aplicação roda atrás de um proxy que encerra o TLS, então o endereço da
/// conexão é sempre o do proxy. Sem processar <c>X-Forwarded-For</c>, o limite
/// de tentativas de login vira global — uma única origem esgota a cota de todos
/// — e a trilha de auditoria registra o mesmo IP para todo mundo.
/// </summary>
internal static class ForwardedHeadersConfiguration
{
    public static IServiceCollection AddProxyForwarding(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var trustedProxies = configuration
            .GetSection("Security:TrustedProxies")
            .GetChildren()
            .Select(entry => entry.Value)
            .OfType<string>()
            .Select(value => value.Trim())
            .Where(value => value.Length > 0)
            .ToArray();

        return services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders =
                ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

            // Apenas o salto mais próximo é considerado. Sem esse limite, quem
            // enviasse o próprio X-Forwarded-For escolheria o IP que aparece.
            options.ForwardLimit = 1;

            // O padrão do ASP.NET Core confia em loopback e redes privadas.
            // Aqui a confiança é declarada por configuração, e nada além disso.
            options.KnownIPNetworks.Clear();
            options.KnownProxies.Clear();

            foreach (var proxy in trustedProxies)
            {
                if (IPAddress.TryParse(proxy, out var address))
                {
                    options.KnownProxies.Add(address);
                    continue;
                }

                if (IPNetwork.TryParse(proxy, out var network))
                {
                    options.KnownIPNetworks.Add(network);
                }
            }
        });
    }
}
