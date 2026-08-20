using PortManagement.Domain.Common;
using PortManagement.Domain.Planning;
using PortManagement.Domain.Ports;
using PortManagement.Domain.Vessels;

namespace PortManagement.UnitTests;

public sealed class BerthRulesTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 20, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void CanReceiveAcceptsACompatibleVessel()
    {
        var berth = CreateBerth();
        var vessel = new Vessel(
            Guid.NewGuid(),
            "Demo Container",
            ImoNumber.Parse("IMO9074729"),
            "BR",
            VesselType.ContainerShip,
            280,
            40,
            12,
            Now);

        Assert.True(berth.CanReceive(vessel));
    }

    [Fact]
    public void CanReceiveRejectsAVesselExceedingTheDraftLimit()
    {
        var berth = CreateBerth();
        var vessel = new Vessel(
            Guid.NewGuid(),
            "Demo Deep Draft",
            ImoNumber.Parse("IMO9074729"),
            "BR",
            VesselType.ContainerShip,
            280,
            40,
            16,
            Now);

        Assert.False(berth.CanReceive(vessel));
    }

    [Fact]
    public void ReprogrammingAWindowPreservesThePreviousPeriod()
    {
        var window = new BerthWindow(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Now,
            Now.AddHours(8),
            "planner@example.test",
            Now);

        window.Reprogram(
            Now.AddHours(2),
            Now.AddHours(10),
            "planner@example.test",
            "Atraso na chegada do navio",
            Now.AddMinutes(30));

        var revision = Assert.Single(window.Revisions);
        Assert.Equal(Now, revision.PreviousStartsAtUtc);
        Assert.Equal(Now.AddHours(8), revision.PreviousEndsAtUtc);
        Assert.Equal(Now.AddHours(2), window.StartsAtUtc);
    }

    [Fact]
    public void WindowRejectsAnInvalidPeriod()
    {
        Assert.Throws<DomainException>(() => new BerthWindow(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Now.AddHours(1),
            Now,
            "planner@example.test",
            Now));
    }

    private static Berth CreateBerth() => new(
        Guid.NewGuid(),
        Guid.NewGuid(),
        "B01",
        "Berço de demonstração",
        320,
        48,
        14,
        [VesselType.ContainerShip, VesselType.GeneralCargo],
        Now);
}
