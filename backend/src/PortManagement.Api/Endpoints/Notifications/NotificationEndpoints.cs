using System.Security.Claims;
using PortManagement.Api.Common;
using PortManagement.Application.Notifications;

namespace PortManagement.Api.Endpoints.Notifications;

internal static class NotificationEndpoints
{
    public static IEndpointRouteBuilder MapNotificationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/v1/notifications")
            .WithTags("Notifications")
            .RequireAuthorization();

        group.MapGet(
                "/",
                async (
                    ClaimsPrincipal principal,
                    GetNotificationCenterHandler handler,
                    CancellationToken cancellationToken) =>
                {
                    if (!TryGetUserId(principal, out var userId))
                    {
                        return Results.Unauthorized();
                    }

                    var result = await handler.HandleAsync(userId, cancellationToken);
                    return result.ToHttpResult(Results.Ok);
                })
            .WithName("GetNotificationCenter")
            .WithSummary("Lista alertas ativos com o estado de leitura do usuário");

        group.MapPost(
                "/{alertId}/read",
                async (
                    string alertId,
                    ClaimsPrincipal principal,
                    MarkNotificationReadHandler handler,
                    CancellationToken cancellationToken) =>
                {
                    if (!TryGetUserId(principal, out var userId))
                    {
                        return Results.Unauthorized();
                    }

                    var result = await handler.HandleAsync(userId, alertId, cancellationToken);
                    return result.ToHttpResult(Results.Ok);
                })
            .WithName("MarkNotificationRead")
            .WithSummary("Marca um alerta ativo como lido para o usuário");

        group.MapPost(
                "/read-all",
                async (
                    ClaimsPrincipal principal,
                    MarkAllNotificationsReadHandler handler,
                    CancellationToken cancellationToken) =>
                {
                    if (!TryGetUserId(principal, out var userId))
                    {
                        return Results.Unauthorized();
                    }

                    var result = await handler.HandleAsync(userId, cancellationToken);
                    return result.ToHttpResult(Results.Ok);
                })
            .WithName("MarkAllNotificationsRead")
            .WithSummary("Marca todos os alertas ativos como lidos para o usuário");

        return endpoints;
    }

    private static bool TryGetUserId(ClaimsPrincipal principal, out Guid userId) =>
        Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out userId);
}
