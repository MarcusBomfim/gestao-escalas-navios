using PortManagement.Api.Resilience;
using PortManagement.Infrastructure.Resilience;

namespace PortManagement.IntegrationTests;

public sealed class ResilienceOptionsTests
{
    [Fact]
    public void DatabaseOptionsAcceptSafeDefaults()
    {
        var options = new DatabaseResilienceOptions();

        options.Validate();
    }

    [Fact]
    public void DatabaseOptionsRejectInvalidCommandTimeout()
    {
        var options = new DatabaseResilienceOptions
        {
            CommandTimeoutSeconds = 0
        };

        Assert.Throws<InvalidOperationException>(options.Validate);
    }

    [Fact]
    public void DatabaseOptionsRejectExcessiveRetryCount()
    {
        var options = new DatabaseResilienceOptions
        {
            MaxRetryCount = 11
        };

        Assert.Throws<InvalidOperationException>(options.Validate);
    }

    [Fact]
    public void DatabaseOptionsRejectInvalidRetryDelay()
    {
        var options = new DatabaseResilienceOptions
        {
            MaxRetryDelaySeconds = 0
        };

        Assert.Throws<InvalidOperationException>(options.Validate);
    }

    [Fact]
    public void ApiOptionsAcceptSafeDefaults()
    {
        var options = new ApiResilienceOptions();

        options.Validate();
    }

    [Fact]
    public void ApiOptionsRejectInvalidRequestTimeout()
    {
        var options = new ApiResilienceOptions
        {
            RequestTimeoutSeconds = 301
        };

        Assert.Throws<InvalidOperationException>(options.Validate);
    }

    [Fact]
    public void ApiOptionsRejectInvalidShutdownTimeout()
    {
        var options = new ApiResilienceOptions
        {
            ShutdownTimeoutSeconds = 0
        };

        Assert.Throws<InvalidOperationException>(options.Validate);
    }
}
