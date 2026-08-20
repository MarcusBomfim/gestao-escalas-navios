using PortManagement.Application.ReferenceData;

namespace PortManagement.Api.Endpoints.ReferenceData;

internal static class ReferenceDataEndpoints
{
    public static IEndpointRouteBuilder MapReferenceDataEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet(
                "/api/v1/reference-data/ports",
                async (
                    GetPortStructureHandler handler,
                    CancellationToken cancellationToken) =>
                    Results.Ok(await handler.HandleAsync(cancellationToken)))
            .WithName("ListPortStructure")
            .WithTags("Reference Data")
            .WithSummary("Lista portos, terminais e berços ativos");

        return endpoints;
    }
}
