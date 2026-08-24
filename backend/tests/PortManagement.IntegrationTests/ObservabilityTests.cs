using PortManagement.Api.Observability;

namespace PortManagement.IntegrationTests;

public sealed class ObservabilityTests
{
    [Theory]
    [InlineData("request-123")]
    [InlineData("ABC_def.456")]
    public void CorrelationIdentifierPreservesSafeClientValues(string value)
    {
        Assert.Equal(value, CorrelationAndMetricsMiddleware.ResolveCorrelationId(value));
    }

    [Theory]
    [InlineData("")]
    [InlineData("contains spaces")]
    [InlineData("bad\r\nheader")]
    [InlineData("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
    public void CorrelationIdentifierReplacesUnsafeClientValues(string value)
    {
        var resolved = CorrelationAndMetricsMiddleware.ResolveCorrelationId(value);

        Assert.NotEqual(value, resolved);
        Assert.Equal(32, resolved.Length);
        Assert.All(resolved, character => Assert.True(char.IsAsciiHexDigit(character)));
    }

    [Fact]
    public void TelemetrySnapshotSeparatesClientAndServerErrors()
    {
        var time = new ManualTimeProvider(new DateTimeOffset(2026, 8, 24, 10, 0, 0, TimeSpan.Zero));
        using var telemetry = new ApiTelemetry(time);

        Complete(telemetry, 200, 10);
        Complete(telemetry, 404, 20);
        Complete(telemetry, 503, 30);
        time.Advance(TimeSpan.FromSeconds(15));

        var snapshot = telemetry.GetSnapshot();

        Assert.Equal(3, snapshot.TotalRequests);
        Assert.Equal(1, snapshot.ClientErrors);
        Assert.Equal(1, snapshot.ServerErrors);
        Assert.Equal(0, snapshot.ActiveRequests);
        Assert.Equal(20, snapshot.AverageDurationMilliseconds);
        Assert.Equal(30, snapshot.P95DurationMilliseconds);
        Assert.Equal(15, snapshot.UptimeSeconds);
    }

    private static void Complete(ApiTelemetry telemetry, int statusCode, double milliseconds)
    {
        telemetry.RequestStarted();
        telemetry.RequestCompleted("GET", "/api/v1/test", statusCode, milliseconds);
    }

    private sealed class ManualTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        private DateTimeOffset current = utcNow;

        public override DateTimeOffset GetUtcNow() => current;

        public void Advance(TimeSpan duration) => current = current.Add(duration);
    }
}
