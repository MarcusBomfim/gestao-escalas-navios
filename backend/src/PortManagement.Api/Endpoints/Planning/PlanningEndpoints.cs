using System.Security.Claims;
using PortManagement.Api.Common;
using PortManagement.Application.Planning;
using PortManagement.Application.Security;
using PortManagement.Domain.Planning;

namespace PortManagement.Api.Endpoints.Planning;

internal static class PlanningEndpoints
{
    public static IEndpointRouteBuilder MapPlanningEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/v1/planning")
            .WithTags("Berth Planning")
            .RequireAuthorization();

        group.MapGet(
                "/berth-windows",
                async (
                    int? page,
                    int? pageSize,
                    Guid? portId,
                    Guid? berthId,
                    BerthWindowStatus? status,
                    DateTimeOffset? fromUtc,
                    DateTimeOffset? toUtc,
                    ListBerthWindowsHandler handler,
                    CancellationToken cancellationToken) =>
                {
                    var result = await handler.HandleAsync(
                        new ListBerthWindowsQuery(
                            page ?? 1,
                            pageSize ?? 20,
                            portId,
                            berthId,
                            status,
                            fromUtc,
                            toUtc),
                        cancellationToken);

                    return result.ToHttpResult(Results.Ok);
                })
            .WithName("ListBerthWindows")
            .WithSummary("Lista janelas de berço para a agenda operacional");

        group.MapGet(
                "/port-calls/{publicCode}/berth-window",
                async (
                    string publicCode,
                    GetPortCallBerthWindowHandler handler,
                    CancellationToken cancellationToken) =>
                {
                    var result = await handler.HandleAsync(publicCode, cancellationToken);
                    return result.ToHttpResult(Results.Ok);
                })
            .WithName("GetPortCallBerthWindow")
            .WithSummary("Consulta a janela de berço ativa de uma escala");

        group.MapPost(
                    "/port-calls/{publicCode}/berth-window",
                    async (
                        string publicCode,
                        RequestBerthWindowRequest request,
                        ClaimsPrincipal principal,
                        RequestBerthWindowHandler handler,
                        CancellationToken cancellationToken) =>
                    {
                        var result = await handler.HandleAsync(
                            new RequestBerthWindowCommand(
                                publicCode,
                                request.BerthId,
                                request.StartsAtUtc,
                                request.EndsAtUtc,
                                request.ExpectedPortCallVersion,
                                GetActor(principal)),
                            cancellationToken);

                        return result.ToHttpResult(window => Results.Created(
                            $"/api/v1/planning/port-calls/{publicCode}/berth-window",
                            window));
                    })
                .WithName("RequestBerthWindow")
                .WithSummary("Solicita uma janela de berço para a escala")
                .RequireAuthorization(AuthorizationPolicies.ManageBerthPlanning);

        group.MapPut(
                    "/port-calls/{publicCode}/berth-window",
                    async (
                        string publicCode,
                        ReprogramBerthWindowRequest request,
                        ClaimsPrincipal principal,
                        ReprogramBerthWindowHandler handler,
                        CancellationToken cancellationToken) =>
                    {
                        var result = await handler.HandleAsync(
                            new ReprogramBerthWindowCommand(
                                publicCode,
                                request.BerthId,
                                request.StartsAtUtc,
                                request.EndsAtUtc,
                                request.ExpectedWindowVersion,
                                GetActor(principal),
                                request.Reason),
                            cancellationToken);

                        return result.ToHttpResult(Results.Ok);
                    })
                .WithName("ReprogramBerthWindow")
                .WithSummary("Reprograma período ou berço preservando a revisão anterior")
                .RequireAuthorization(AuthorizationPolicies.ManageBerthPlanning);

        group.MapPost(
                    "/port-calls/{publicCode}/berth-window/confirm",
                    async (
                        string publicCode,
                        ChangeBerthWindowStatusRequest request,
                        ClaimsPrincipal principal,
                        ConfirmBerthWindowHandler handler,
                        CancellationToken cancellationToken) =>
                    {
                        var result = await handler.HandleAsync(
                            new ChangeBerthWindowStatusCommand(
                                publicCode,
                                request.ExpectedWindowVersion,
                                GetActor(principal),
                                null),
                            cancellationToken);

                        return result.ToHttpResult(Results.Ok);
                    })
                .WithName("ConfirmBerthWindow")
                .WithSummary("Confirma uma janela com proteção contra sobreposição")
                .RequireAuthorization(AuthorizationPolicies.ManageBerthPlanning);

        group.MapPost(
                    "/port-calls/{publicCode}/berth-window/cancel",
                    async (
                        string publicCode,
                        ChangeBerthWindowStatusRequest request,
                        ClaimsPrincipal principal,
                        CancelBerthWindowHandler handler,
                        CancellationToken cancellationToken) =>
                    {
                        var result = await handler.HandleAsync(
                            new ChangeBerthWindowStatusCommand(
                                publicCode,
                                request.ExpectedWindowVersion,
                                GetActor(principal),
                                request.Reason),
                            cancellationToken);

                        return result.ToHttpResult(Results.Ok);
                    })
                .WithName("CancelBerthWindow")
                .WithSummary("Cancela uma janela de berço com justificativa")
                .RequireAuthorization(AuthorizationPolicies.ManageBerthPlanning);

        return endpoints;
    }

    private static string GetActor(ClaimsPrincipal principal) =>
        principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? "unknown";
}

internal sealed record RequestBerthWindowRequest(
    Guid BerthId,
    DateTimeOffset StartsAtUtc,
    DateTimeOffset EndsAtUtc,
    long ExpectedPortCallVersion);

internal sealed record ReprogramBerthWindowRequest(
    Guid BerthId,
    DateTimeOffset StartsAtUtc,
    DateTimeOffset EndsAtUtc,
    long ExpectedWindowVersion,
    string Reason);

internal sealed record ChangeBerthWindowStatusRequest(
    long ExpectedWindowVersion,
    string? Reason);
