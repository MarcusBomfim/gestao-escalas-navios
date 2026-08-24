using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace PortManagement.Api.Observability;

internal sealed class ApiTelemetry : IDisposable
{
    private const int RecentSampleLimit = 512;
    private readonly TimeProvider timeProvider;
    private readonly Meter meter = new("PortManagement.Api", "1.0.0");
    private readonly ConcurrentQueue<RequestSample> recentSamples = new();
    private readonly DateTimeOffset startedAtUtc;
    private readonly Counter<long> requestCounter;
    private readonly Counter<long> clientErrorCounter;
    private readonly Counter<long> serverErrorCounter;
    private readonly UpDownCounter<long> activeRequestCounter;
    private readonly Histogram<double> durationHistogram;
    private long totalRequests;
    private long clientErrors;
    private long serverErrors;
    private long activeRequests;
    private long totalDurationMicroseconds;

    public ApiTelemetry(TimeProvider timeProvider)
    {
        this.timeProvider = timeProvider;
        startedAtUtc = timeProvider.GetUtcNow();
        requestCounter = meter.CreateCounter<long>("port_management.http.server.requests", "{request}");
        clientErrorCounter = meter.CreateCounter<long>("port_management.http.server.client_errors", "{request}");
        serverErrorCounter = meter.CreateCounter<long>("port_management.http.server.errors", "{request}");
        activeRequestCounter = meter.CreateUpDownCounter<long>("port_management.http.server.active_requests", "{request}");
        durationHistogram = meter.CreateHistogram<double>("port_management.http.server.duration", "ms");
    }

    public void RequestStarted()
    {
        Interlocked.Increment(ref activeRequests);
        activeRequestCounter.Add(1);
    }

    public void RequestCompleted(
        string method,
        string route,
        int statusCode,
        double durationMilliseconds)
    {
        var tags = new TagList
        {
            { "http.request.method", method },
            { "http.route", route },
            { "http.response.status_code", statusCode }
        };
        requestCounter.Add(1, tags);
        durationHistogram.Record(durationMilliseconds, tags);
        activeRequestCounter.Add(-1);
        Interlocked.Decrement(ref activeRequests);
        Interlocked.Increment(ref totalRequests);
        Interlocked.Add(
            ref totalDurationMicroseconds,
            checked((long)Math.Round(durationMilliseconds * 1_000, MidpointRounding.AwayFromZero)));

        if (statusCode is >= 400 and < 500)
        {
            Interlocked.Increment(ref clientErrors);
            clientErrorCounter.Add(1, tags);
        }
        else if (statusCode >= 500)
        {
            Interlocked.Increment(ref serverErrors);
            serverErrorCounter.Add(1, tags);
        }

        recentSamples.Enqueue(new RequestSample(durationMilliseconds, timeProvider.GetUtcNow()));
        while (recentSamples.Count > RecentSampleLimit && recentSamples.TryDequeue(out _))
        {
        }
    }

    public ApiTelemetrySnapshot GetSnapshot()
    {
        var now = timeProvider.GetUtcNow();
        var total = Interlocked.Read(ref totalRequests);
        var recent = recentSamples.ToArray();
        var durations = recent
            .Select(sample => sample.DurationMilliseconds)
            .Order()
            .ToArray();
        var p95Index = durations.Length == 0
            ? 0
            : Math.Min(durations.Length - 1, (int)Math.Ceiling(durations.Length * 0.95) - 1);

        return new ApiTelemetrySnapshot(
            startedAtUtc,
            Math.Max(0, (long)(now - startedAtUtc).TotalSeconds),
            total,
            Interlocked.Read(ref clientErrors),
            Interlocked.Read(ref serverErrors),
            Interlocked.Read(ref activeRequests),
            total == 0
                ? 0
                : Math.Round(Interlocked.Read(ref totalDurationMicroseconds) / 1_000d / total, 2),
            durations.Length == 0 ? 0 : Math.Round(durations[p95Index], 2),
            recent.Count(sample => sample.CompletedAtUtc >= now.AddMinutes(-1)));
    }

    public void Dispose() => meter.Dispose();

    private sealed record RequestSample(double DurationMilliseconds, DateTimeOffset CompletedAtUtc);
}

internal sealed record ApiTelemetrySnapshot(
    DateTimeOffset StartedAtUtc,
    long UptimeSeconds,
    long TotalRequests,
    long ClientErrors,
    long ServerErrors,
    long ActiveRequests,
    double AverageDurationMilliseconds,
    double P95DurationMilliseconds,
    int RequestsLastMinute);
