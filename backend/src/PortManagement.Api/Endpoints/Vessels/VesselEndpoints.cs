using PortManagement.Api.Common;
using PortManagement.Application.Vessels;
using PortManagement.Domain.Vessels;

namespace PortManagement.Api.Endpoints.Vessels;

internal static class VesselEndpoints
{
    public static IEndpointRouteBuilder MapVesselEndpoints(
        this IEndpointRouteBuilder endpoints,
        bool enableUnauthenticatedWrites)
    {
        var group = endpoints
            .MapGroup("/api/v1/vessels")
            .WithTags("Vessels");

        if (enableUnauthenticatedWrites)
        {
            group.MapPost(
                    "/",
                    async (
                        RegisterVesselRequest request,
                        RegisterVesselHandler handler,
                        CancellationToken cancellationToken) =>
                    {
                        var result = await handler.HandleAsync(
                            new RegisterVesselCommand(
                                request.Name,
                                request.ImoNumber,
                                request.FlagCode,
                                request.Type,
                                request.LengthOverallMeters,
                                request.BeamMeters,
                                request.MaximumDraftMeters,
                                request.CallSign,
                                request.Mmsi),
                            cancellationToken);

                        return result.ToHttpResult(vessel =>
                            Results.CreatedAtRoute("GetVesselById", new { id = vessel.Id }, vessel));
                    })
                .WithName("RegisterVessel")
                .WithSummary("Cadastra um navio");
        }

        group.MapGet(
                "/",
                async (
                    int? page,
                    int? pageSize,
                    string? search,
                    bool? activeOnly,
                    ListVesselsHandler handler,
                    CancellationToken cancellationToken) =>
                {
                    var result = await handler.HandleAsync(
                        new ListVesselsQuery(
                            page ?? 1,
                            pageSize ?? 20,
                            search,
                            activeOnly ?? true),
                        cancellationToken);

                    return result.ToHttpResult(Results.Ok);
                })
            .WithName("ListVessels")
            .WithSummary("Lista navios com paginação e busca");

        group.MapGet(
                "/{id:guid}",
                async (
                    Guid id,
                    GetVesselHandler handler,
                    CancellationToken cancellationToken) =>
                {
                    var result = await handler.HandleAsync(id, cancellationToken);
                    return result.ToHttpResult(Results.Ok);
                })
            .WithName("GetVesselById")
            .WithSummary("Consulta um navio pelo identificador");

        return endpoints;
    }
}

internal sealed record RegisterVesselRequest(
    string Name,
    string? ImoNumber,
    string FlagCode,
    VesselType Type,
    decimal LengthOverallMeters,
    decimal BeamMeters,
    decimal MaximumDraftMeters,
    string? CallSign,
    string? Mmsi);
