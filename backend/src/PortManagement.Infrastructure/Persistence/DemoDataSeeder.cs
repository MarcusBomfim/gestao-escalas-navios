using Microsoft.EntityFrameworkCore;
using PortManagement.Domain.Organizations;
using PortManagement.Domain.PortCalls;
using PortManagement.Domain.Ports;
using PortManagement.Domain.Vessels;

namespace PortManagement.Infrastructure.Persistence;

public sealed class DemoDataSeeder(PortManagementDbContext database)
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (await database.Ports.AnyAsync(
                port => port.UnLocode == "BRSSZ",
                cancellationToken))
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        var portId = Guid.Parse("10000000-0000-0000-0000-000000000001");
        var terminalId = Guid.Parse("20000000-0000-0000-0000-000000000001");
        var berthOneId = Guid.Parse("30000000-0000-0000-0000-000000000001");
        var berthTwoId = Guid.Parse("30000000-0000-0000-0000-000000000002");
        var agencyId = Guid.Parse("40000000-0000-0000-0000-000000000001");
        var shippingLineId = Guid.Parse("40000000-0000-0000-0000-000000000002");

        var port = new Port(
            portId,
            "Porto de Santos — ambiente demonstrativo",
            "BRSSZ",
            "BR",
            "America/Sao_Paulo",
            now);
        var terminal = new Terminal(
            terminalId,
            portId,
            "TAD",
            "Terminal Atlântico Demo",
            "America/Sao_Paulo",
            now);
        var berthOne = new Berth(
            berthOneId,
            terminalId,
            "A01",
            "Berço A01 Demo",
            360,
            52,
            15.5m,
            [VesselType.ContainerShip, VesselType.GeneralCargo, VesselType.RoRo],
            now);
        var berthTwo = new Berth(
            berthTwoId,
            terminalId,
            "A02",
            "Berço A02 Demo",
            300,
            46,
            13.8m,
            [VesselType.BulkCarrier, VesselType.GeneralCargo],
            now);
        var agency = new Organization(
            agencyId,
            "Agência Marítima Demo",
            "DEMO-AGENCY-001",
            OrganizationType.ShippingAgency,
            now);
        var shippingLine = new Organization(
            shippingLineId,
            "Navegação Atlântica Demo",
            "DEMO-LINE-001",
            OrganizationType.ShippingLine,
            now);

        var vessels = new[]
        {
            new Vessel(
                Guid.Parse("50000000-0000-0000-0000-000000000001"),
                "Atlântico Demo",
                null,
                "BR",
                VesselType.ContainerShip,
                294,
                43,
                13.2m,
                now,
                "P3DEMO"),
            new Vessel(
                Guid.Parse("50000000-0000-0000-0000-000000000002"),
                "Costa Sul Demo",
                null,
                "BR",
                VesselType.BulkCarrier,
                230,
                36,
                12.5m,
                now,
                "P4DEMO"),
            new Vessel(
                Guid.Parse("50000000-0000-0000-0000-000000000003"),
                "Navegante Demo",
                null,
                "PT",
                VesselType.GeneralCargo,
                180,
                30,
                10.8m,
                now,
                "C5DEMO")
        };

        var draftCall = CreatePortCall(
            Guid.Parse("60000000-0000-0000-0000-000000000001"),
            vessels[0].Id,
            portId,
            "seed:port-call:1",
            "DEMO-001",
            now.AddHours(-4),
            agencyId,
            shippingLineId);

        var plannedCall = CreatePortCall(
            Guid.Parse("60000000-0000-0000-0000-000000000002"),
            vessels[1].Id,
            portId,
            "seed:port-call:2",
            "DEMO-002",
            now.AddHours(-3),
            agencyId,
            shippingLineId);
        AdvanceToPlanned(plannedCall, now.AddHours(-2));
        plannedCall.PlanAt(terminalId, berthTwoId, now.AddHours(-1));

        var anchorageCall = CreatePortCall(
            Guid.Parse("60000000-0000-0000-0000-000000000003"),
            vessels[2].Id,
            portId,
            "seed:port-call:3",
            "DEMO-003",
            now.AddHours(-8),
            agencyId,
            shippingLineId);
        AdvanceToPlanned(anchorageCall, now.AddHours(-7));
        anchorageCall.PlanAt(terminalId, berthOneId, now.AddHours(-6));
        anchorageCall.TransitionTo(
            PortCallStatus.AtAnchorage,
            "system:demo-seed",
            now.AddHours(-1),
            "Chegada demonstrativa ao fundeadouro.");

        await database.Ports.AddAsync(port, cancellationToken);
        await database.Terminals.AddAsync(terminal, cancellationToken);
        await database.Berths.AddRangeAsync([berthOne, berthTwo], cancellationToken);
        await database.Organizations.AddRangeAsync([agency, shippingLine], cancellationToken);
        await database.Vessels.AddRangeAsync(vessels, cancellationToken);
        await database.PortCalls.AddRangeAsync(
            [draftCall, plannedCall, anchorageCall],
            cancellationToken);
        await database.SaveChangesAsync(cancellationToken);
    }

    private static PortCall CreatePortCall(
        Guid id,
        Guid vesselId,
        Guid portId,
        string idempotencyKey,
        string voyageNumber,
        DateTimeOffset createdAtUtc,
        Guid agencyId,
        Guid shippingLineId)
    {
        var portCall = new PortCall(
            id,
            vesselId,
            portId,
            PortCallPurpose.CargoOperation,
            idempotencyKey,
            createdAtUtc,
            voyageNumber,
            "BRRIO",
            "BRPNG");
        portCall.AssignOrganizations(agencyId, shippingLineId, createdAtUtc);
        return portCall;
    }

    private static void AdvanceToPlanned(PortCall portCall, DateTimeOffset changedAtUtc)
    {
        portCall.TransitionTo(PortCallStatus.Requested, "system:demo-seed", changedAtUtc);
        portCall.TransitionTo(PortCallStatus.UnderReview, "system:demo-seed", changedAtUtc.AddMinutes(10));
        portCall.TransitionTo(PortCallStatus.Planned, "system:demo-seed", changedAtUtc.AddMinutes(20));
    }
}
