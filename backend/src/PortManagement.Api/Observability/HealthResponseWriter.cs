using System.Text.Json;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace PortManagement.Api.Observability;

internal static class HealthResponseWriter
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public static Task WriteAsync(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json; charset=utf-8";
        context.Response.Headers.CacheControl = "no-store";
        var response = new HealthResponse(
            report.Status.ToString(),
            Math.Round(report.TotalDuration.TotalMilliseconds, 2),
            report.Entries
                .OrderBy(entry => entry.Key, StringComparer.Ordinal)
                .Select(entry => new HealthComponentResponse(
                    entry.Key,
                    entry.Value.Status.ToString(),
                    Math.Round(entry.Value.Duration.TotalMilliseconds, 2)))
                .ToArray());

        return context.Response.WriteAsync(JsonSerializer.Serialize(response, SerializerOptions));
    }

    private sealed record HealthResponse(
        string Status,
        double DurationMilliseconds,
        IReadOnlyCollection<HealthComponentResponse> Components);

    private sealed record HealthComponentResponse(
        string Name,
        string Status,
        double DurationMilliseconds);
}
