using PortManagement.Application.ControlTower;
using PortManagement.Domain.PortCalls;

namespace PortManagement.UnitTests;

public sealed class VesselTrafficSimulatorTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 25, 14, 32, 8, TimeSpan.Zero);

    [Fact]
    public void SnapshotContainsOnlyBoundedAndExplicitlySimulatedPositions()
    {
        var snapshot = new ControlTowerSnapshot(
            4,
            [
                CreateCall(Guid.Parse("10000000-0000-0000-0000-000000000001"), PortCallStatus.Planned),
                CreateCall(Guid.Parse("20000000-0000-0000-0000-000000000002"), PortCallStatus.AtAnchorage),
                CreateCall(Guid.Parse("30000000-0000-0000-0000-000000000003"), PortCallStatus.InOperation)
            ]);

        var result = VesselTrafficSimulator.Evaluate(snapshot, Now);

        Assert.True(result.IsSimulated);
        Assert.Equal("Canal portuário demonstrativo", result.CoverageLabel);
        Assert.Equal(new DateTimeOffset(2026, 8, 25, 14, 32, 5, TimeSpan.Zero), result.GeneratedAtUtc);
        Assert.Equal(3, result.Positions.Count);
        Assert.All(result.Positions, position =>
        {
            Assert.True(position.IsSimulated);
            Assert.InRange(position.XPercent, 5m, 95m);
            Assert.InRange(position.YPercent, 5m, 95m);
            Assert.Equal(result.GeneratedAtUtc, position.ObservedAtUtc);
        });
    }

    [Theory]
    [InlineData(PortCallStatus.Requested, VesselNavigationState.AwaitingSchedule)]
    [InlineData(PortCallStatus.Planned, VesselNavigationState.Approaching)]
    [InlineData(PortCallStatus.AtAnchorage, VesselNavigationState.Anchored)]
    [InlineData(PortCallStatus.ClearedForBerthing, VesselNavigationState.Manoeuvring)]
    [InlineData(PortCallStatus.Berthed, VesselNavigationState.Berthed)]
    [InlineData(PortCallStatus.InOperation, VesselNavigationState.Operating)]
    [InlineData(PortCallStatus.OperationCompleted, VesselNavigationState.ReadyToSail)]
    [InlineData(PortCallStatus.Unberthed, VesselNavigationState.Departing)]
    public void PortCallStatusDefinesNavigationState(
        PortCallStatus status,
        VesselNavigationState expectedState)
    {
        var snapshot = new ControlTowerSnapshot(
            1,
            [CreateCall(Guid.Parse("40000000-0000-0000-0000-000000000004"), status)]);

        var position = Assert.Single(VesselTrafficSimulator.Evaluate(snapshot, Now).Positions);

        Assert.Equal(expectedState, position.NavigationState);
    }

    [Fact]
    public void MovingVesselChangesPositionWhileBerthedVesselRemainsStable()
    {
        var approaching = CreateCall(
            Guid.Parse("50000000-0000-0000-0000-000000000005"),
            PortCallStatus.Planned);
        var berthed = CreateCall(
            Guid.Parse("60000000-0000-0000-0000-000000000006"),
            PortCallStatus.InOperation);
        var snapshot = new ControlTowerSnapshot(2, [approaching, berthed]);

        var first = VesselTrafficSimulator.Evaluate(snapshot, Now);
        var second = VesselTrafficSimulator.Evaluate(snapshot, Now.AddSeconds(5));
        var firstApproaching = first.Positions.Single(item => item.PortCallId == approaching.PortCallId);
        var secondApproaching = second.Positions.Single(item => item.PortCallId == approaching.PortCallId);
        var firstBerthed = first.Positions.Single(item => item.PortCallId == berthed.PortCallId);
        var secondBerthed = second.Positions.Single(item => item.PortCallId == berthed.PortCallId);

        Assert.NotEqual(firstApproaching.XPercent, secondApproaching.XPercent);
        Assert.Equal(firstBerthed.XPercent, secondBerthed.XPercent);
        Assert.Equal(firstBerthed.YPercent, secondBerthed.YPercent);
    }

    private static ControlTowerCallSnapshot CreateCall(Guid id, PortCallStatus status) => new(
        id,
        $"ESC-{id.ToString("N", null)[..6]}",
        $"Navio {id.ToString("N", null)[..4]}",
        status,
        "Porto demonstrativo",
        "Terminal demonstrativo",
        "Berço 01",
        null,
        null,
        null,
        null,
        null,
        null,
        null,
        null,
        Now,
        0,
        0,
        null);
}
