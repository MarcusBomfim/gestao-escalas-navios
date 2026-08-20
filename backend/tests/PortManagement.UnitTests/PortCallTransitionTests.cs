using PortManagement.Domain.Common;
using PortManagement.Domain.PortCalls;

namespace PortManagement.UnitTests;

public sealed class PortCallTransitionTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 20, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void ValidTransitionChangesStatusAndCreatesHistory()
    {
        var portCall = CreatePortCall();

        portCall.TransitionTo(PortCallStatus.Requested, "planner@example.test", Now.AddMinutes(5));

        var history = Assert.Single(portCall.StatusHistory);
        Assert.Equal(PortCallStatus.Requested, portCall.Status);
        Assert.Equal(PortCallStatus.Draft, history.PreviousStatus);
        Assert.Equal(PortCallStatus.Requested, history.NewStatus);
    }

    [Fact]
    public void InvalidTransitionPreservesCurrentStatus()
    {
        var portCall = CreatePortCall();

        Assert.Throws<DomainException>(() =>
            portCall.TransitionTo(PortCallStatus.InOperation, "planner@example.test", Now.AddMinutes(5)));

        Assert.Equal(PortCallStatus.Draft, portCall.Status);
        Assert.Empty(portCall.StatusHistory);
    }

    [Fact]
    public void CancellationRequiresAReason()
    {
        var portCall = CreatePortCall();

        Assert.Throws<DomainException>(() =>
            portCall.TransitionTo(PortCallStatus.Cancelled, "planner@example.test", Now.AddMinutes(5)));
    }

    private static PortCall CreatePortCall() => new(
        Guid.NewGuid(),
        Guid.NewGuid(),
        Guid.NewGuid(),
        PortCallPurpose.CargoOperation,
        Guid.NewGuid().ToString("N"),
        Now);
}
