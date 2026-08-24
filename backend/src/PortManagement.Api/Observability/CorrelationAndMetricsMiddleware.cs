using System.Diagnostics;
using Microsoft.AspNetCore.Routing;

namespace PortManagement.Api.Observability;

internal sealed class CorrelationAndMetricsMiddleware(
    RequestDelegate next,
    ApiTelemetry telemetry,
    ILogger<CorrelationAndMetricsMiddleware> logger)
{
    public const string CorrelationHeader = "X-Correlation-ID";
    private static readonly Action<ILogger, string, string, int, double, string, Exception?> LogRequestCompleted =
        LoggerMessage.Define<string, string, int, double, string>(
            LogLevel.Information,
            new EventId(1201, nameof(CorrelationAndMetricsMiddleware)),
            "HTTP {Method} {Route} respondeu {StatusCode} em {ElapsedMilliseconds} ms. CorrelationId={CorrelationId}");
    private static readonly Action<ILogger, string, int, double, Exception?> LogHealthCheckCompleted =
        LoggerMessage.Define<string, int, double>(
            LogLevel.Debug,
            new EventId(1202, nameof(CorrelationAndMetricsMiddleware)),
            "Health check {Route} respondeu {StatusCode} em {ElapsedMilliseconds} ms.");

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = ResolveCorrelationId(context.Request.Headers[CorrelationHeader]);
        context.TraceIdentifier = correlationId;
        context.Response.Headers[CorrelationHeader] = correlationId;
        Activity.Current?.SetTag("port_management.correlation_id", correlationId);
        var started = Stopwatch.GetTimestamp();
        var isHealthCheck = context.Request.Path.StartsWithSegments("/health");
        if (!isHealthCheck)
        {
            telemetry.RequestStarted();
        }

        try
        {
            await next(context);
        }
        finally
        {
            var elapsed = Stopwatch.GetElapsedTime(started).TotalMilliseconds;
            var route = context.GetEndpoint()
                ?.Metadata.GetMetadata<RouteEndpoint>()
                ?.RoutePattern.RawText
                ?? "unmatched";
            var statusCode = context.Response.StatusCode;
            var roundedElapsed = Math.Round(elapsed, 2);
            if (isHealthCheck)
            {
                LogHealthCheckCompleted(logger, route, statusCode, roundedElapsed, null);
            }
            else
            {
                telemetry.RequestCompleted(context.Request.Method, route, statusCode, elapsed);
                LogRequestCompleted(
                    logger,
                    context.Request.Method,
                    route,
                    statusCode,
                    roundedElapsed,
                    correlationId,
                    null);
            }
        }
    }

    internal static string ResolveCorrelationId(string? candidate)
    {
        if (!string.IsNullOrWhiteSpace(candidate)
            && candidate.Length <= 64
            && candidate.All(character => char.IsLetterOrDigit(character) || character is '-' or '_' or '.'))
        {
            return candidate;
        }

        return Guid.NewGuid().ToString("N");
    }
}
