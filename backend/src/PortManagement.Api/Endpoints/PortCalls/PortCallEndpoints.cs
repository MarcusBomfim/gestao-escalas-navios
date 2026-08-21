using System.Security.Claims;
using PortManagement.Api.Common;
using PortManagement.Application.PortCalls;
using PortManagement.Application.Security;
using PortManagement.Domain.PortCalls;

namespace PortManagement.Api.Endpoints.PortCalls;

internal static class PortCallEndpoints
{
    public static IEndpointRouteBuilder MapPortCallEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/v1/port-calls")
            .WithTags("Port Calls");

        group.MapPost(
                    "/",
                    async (
                        CreatePortCallRequest body,
                        HttpRequest request,
                        CreatePortCallHandler handler,
                        CancellationToken cancellationToken) =>
                    {
                        var idempotencyKey = request.Headers["Idempotency-Key"].ToString();
                        var result = await handler.HandleAsync(
                            new CreatePortCallCommand(
                                body.VesselId,
                                body.PortId,
                                body.Purpose,
                                idempotencyKey,
                                body.VoyageNumber,
                                body.PreviousPortUnLocode,
                                body.NextPortUnLocode),
                            cancellationToken);

                        return result.ToHttpResult(response =>
                            response.Created
                                ? Results.CreatedAtRoute(
                                    "GetPortCallByCode",
                                    new { publicCode = response.PortCall.PublicCode },
                                    response.PortCall)
                                : Results.Ok(response.PortCall));
                    })
                .WithName("CreatePortCall")
                .WithSummary("Cria uma escala de forma idempotente")
                .RequireAuthorization(AuthorizationPolicies.CreatePortCalls);

        group.MapGet(
                "/",
                async (
                    int? page,
                    int? pageSize,
                    PortCallStatus? status,
                    Guid? portId,
                    string? search,
                    ListPortCallsHandler handler,
                    CancellationToken cancellationToken) =>
                {
                    var result = await handler.HandleAsync(
                        new ListPortCallsQuery(
                            page ?? 1,
                            pageSize ?? 20,
                            status,
                            portId,
                            search),
                        cancellationToken);

                    return result.ToHttpResult(Results.Ok);
                })
            .WithName("ListPortCalls")
            .WithSummary("Lista escalas com filtros e paginação");

        group.MapGet(
                "/{publicCode}",
                async (
                    string publicCode,
                    GetPortCallHandler handler,
                    CancellationToken cancellationToken) =>
                {
                    var result = await handler.HandleAsync(publicCode, cancellationToken);
                    return result.ToHttpResult(Results.Ok);
                })
            .WithName("GetPortCallByCode")
            .WithSummary("Consulta os detalhes e o histórico de uma escala");

        group.MapPost(
                    "/{publicCode}/transitions",
                    async (
                        string publicCode,
                        TransitionPortCallRequest request,
                        ClaimsPrincipal principal,
                        TransitionPortCallHandler handler,
                        CancellationToken cancellationToken) =>
                    {
                        var result = await handler.HandleAsync(
                            new TransitionPortCallCommand(
                                publicCode,
                                request.NewStatus,
                                request.ExpectedVersion,
                                principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? "unknown",
                                request.Reason),
                            cancellationToken);

                        return result.ToHttpResult(Results.Ok);
                    })
                .WithName("TransitionPortCall")
                .WithSummary("Executa uma transição válida usando concorrência otimista")
                .RequireAuthorization(AuthorizationPolicies.TransitionPortCalls);

        return endpoints;
    }
}

internal sealed record CreatePortCallRequest(
    Guid VesselId,
    Guid PortId,
    PortCallPurpose Purpose,
    string? VoyageNumber,
    string? PreviousPortUnLocode,
    string? NextPortUnLocode);

internal sealed record TransitionPortCallRequest(
    PortCallStatus NewStatus,
    long ExpectedVersion,
    string? Reason);
