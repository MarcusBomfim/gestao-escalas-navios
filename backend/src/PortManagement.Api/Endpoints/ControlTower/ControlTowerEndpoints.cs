using PortManagement.Api.Common;
using PortManagement.Application.ControlTower;

namespace PortManagement.Api.Endpoints.ControlTower;

internal static class ControlTowerEndpoints
{
    public static IEndpointRouteBuilder MapControlTowerEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet(
                "/api/v1/control-tower",
                async (
                    GetControlTowerHandler handler,
                    CancellationToken cancellationToken) =>
                {
                    var result = await handler.HandleAsync(cancellationToken);
                    return result.ToHttpResult(Results.Ok);
                })
            .WithName("GetControlTower")
            .WithTags("Control Tower")
            .WithSummary("Consolida escalas ativas, indicadores e alertas operacionais")
            .RequireAuthorization();

        return endpoints;
    }
}
