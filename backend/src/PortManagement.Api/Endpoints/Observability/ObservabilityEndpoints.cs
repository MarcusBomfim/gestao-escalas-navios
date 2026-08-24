using Microsoft.Extensions.Diagnostics.HealthChecks;
using PortManagement.Api.Observability;
using PortManagement.Application.Security;

namespace PortManagement.Api.Endpoints.Observability;

internal static class ObservabilityEndpoints
{
    public static IEndpointRouteBuilder MapObservabilityEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet(
                "/api/v1/observability/summary",
                async (
                    HttpContext context,
                    ApiTelemetry telemetry,
                    HealthCheckService healthChecks,
                    CancellationToken cancellationToken) =>
                {
                    var health = await healthChecks.CheckHealthAsync(
                        registration => registration.Tags.Contains("ready"),
                        cancellationToken);
                    context.Response.Headers.CacheControl = "no-store";
                    return Results.Ok(new ObservabilitySummaryResponse(
                        DateTimeOffset.UtcNow,
                        telemetry.GetSnapshot(),
                        health.Status.ToString(),
                        health.Entries
                            .OrderBy(entry => entry.Key, StringComparer.Ordinal)
                            .Select(entry => new ObservabilityComponentResponse(
                                entry.Key,
                                entry.Value.Status.ToString(),
                                Math.Round(entry.Value.Duration.TotalMilliseconds, 2)))
                            .ToArray()));
                })
            .WithTags("Observability")
            .WithName("GetObservabilitySummary")
            .WithSummary("Apresenta métricas recentes e a prontidão dos componentes")
            .RequireAuthorization(AuthorizationPolicies.ViewObservability);

        return endpoints;
    }

    private sealed record ObservabilitySummaryResponse(
        DateTimeOffset GeneratedAtUtc,
        ApiTelemetrySnapshot Api,
        string ReadinessStatus,
        IReadOnlyCollection<ObservabilityComponentResponse> Components);

    private sealed record ObservabilityComponentResponse(
        string Name,
        string Status,
        double DurationMilliseconds);
}
