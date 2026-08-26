using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using PortManagement.Application.Security;
using PortManagement.Domain.Operations;
using PortManagement.Domain.Organizations;
using PortManagement.Domain.Planning;
using PortManagement.Domain.PortCalls;
using PortManagement.Domain.Ports;
using PortManagement.Domain.Vessels;
using PortManagement.Infrastructure.Identity;

namespace PortManagement.Infrastructure.Persistence;

public sealed class DemoDataSeeder(
    PortManagementDbContext database,
    RoleManager<ApplicationRole> roleManager,
    UserManager<ApplicationUser> userManager,
    IConfiguration configuration)
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (!await database.Ports.AnyAsync(
                port => port.UnLocode == "BRSSZ",
                cancellationToken))
        {
            await SeedPortDataAsync(cancellationToken);
        }

        await SeedIdentityAsync(cancellationToken);
        await SeedPlanningDataAsync(cancellationToken);
        await SeedOperationalExecutionDataAsync(cancellationToken);
        await SeedControlTowerDataAsync(cancellationToken);
    }

    private async Task SeedControlTowerDataAsync(CancellationToken cancellationToken)
    {
        var actor = "system:demo-seed";
        var source = "Simulação operacional";
        var now = DateTimeOffset.UtcNow;
        var operationalCallId = Guid.Parse("60000000-0000-0000-0000-000000000002");
        var anchorageCallId = Guid.Parse("60000000-0000-0000-0000-000000000003");

        var operationalWindow = await database.BerthWindows
            .SingleOrDefaultAsync(
                window => window.PortCallId == operationalCallId
                    && window.Status == BerthWindowStatus.Confirmed,
                cancellationToken);
        var berthedAt = await database.PortCallEvents
            .Where(portCallEvent => portCallEvent.PortCallId == operationalCallId
                && portCallEvent.Phase == PortCallEventPhase.Berth
                && portCallEvent.Action == PortCallEventAction.Completion
                && portCallEvent.Classifier == TemporalClassifier.Actual)
            .Select(portCallEvent => (DateTimeOffset?)portCallEvent.OccursAtUtc)
            .MinAsync(cancellationToken);
        if (operationalWindow is not null
            && berthedAt.HasValue
            && operationalWindow.StartsAtUtc > berthedAt.Value)
        {
            operationalWindow.Reprogram(
                berthedAt.Value.AddMinutes(-30),
                now.AddHours(2),
                actor,
                "Alinhamento da janela com a execução demonstrativa.",
                now);
        }

        var anchorageCall = await database.PortCalls.SingleOrDefaultAsync(
            portCall => portCall.Id == anchorageCallId,
            cancellationToken);
        var anchorageEventExists = await database.PortCallEvents.AnyAsync(
            portCallEvent => portCallEvent.PortCallId == anchorageCallId
                && portCallEvent.Phase == PortCallEventPhase.Anchorage
                && portCallEvent.Action == PortCallEventAction.Arrival
                && portCallEvent.Classifier == TemporalClassifier.Actual,
            cancellationToken);
        if (anchorageCall?.Status == PortCallStatus.AtAnchorage && !anchorageEventExists)
        {
            await database.PortCallEvents.AddAsync(
                new PortCallEvent(
                    Guid.NewGuid(),
                    anchorageCall.Id,
                    PortCallEventPhase.Anchorage,
                    PortCallEventAction.Arrival,
                    TemporalClassifier.Actual,
                    now.AddHours(-1),
                    source,
                    actor,
                    now),
                cancellationToken);
        }

        var pendingCallId = Guid.Parse("60000000-0000-0000-0000-000000000004");
        if (!await database.PortCalls.AnyAsync(portCall => portCall.Id == pendingCallId, cancellationToken))
        {
            var vesselId = Guid.Parse("50000000-0000-0000-0000-000000000004");
            var portId = Guid.Parse("10000000-0000-0000-0000-000000000001");
            var terminalId = Guid.Parse("20000000-0000-0000-0000-000000000001");
            var berthId = Guid.Parse("30000000-0000-0000-0000-000000000001");
            var agencyId = Guid.Parse("40000000-0000-0000-0000-000000000001");
            var shippingLineId = Guid.Parse("40000000-0000-0000-0000-000000000002");
            var vessel = await database.Vessels.SingleOrDefaultAsync(
                item => item.Id == vesselId,
                cancellationToken);
            if (vessel is null)
            {
                vessel = new Vessel(
                    vesselId,
                    "Horizonte Azul Demo",
                    null,
                    "BR",
                    VesselType.ContainerShip,
                    285,
                    42,
                    12.9m,
                    now.AddDays(-1),
                    "P5DEMO");
                await database.Vessels.AddAsync(vessel, cancellationToken);
            }
            var portCall = CreatePortCall(
                pendingCallId,
                vessel.Id,
                portId,
                "seed:port-call:4",
                "DEMO-004",
                now.AddHours(-10),
                agencyId,
                shippingLineId);
            portCall.TransitionTo(PortCallStatus.Requested, actor, now.AddHours(-9));
            portCall.TransitionTo(PortCallStatus.UnderReview, actor, now.AddHours(-8.5));
            portCall.PlanAt(terminalId, berthId, now.AddHours(-8));
            var pendingWindow = new BerthWindow(
                Guid.Parse("70000000-0000-0000-0000-000000000003"),
                portCall.Id,
                berthId,
                now.AddHours(-3),
                now.AddHours(5),
                actor,
                now.AddHours(-8));

            await database.PortCalls.AddAsync(portCall, cancellationToken);
            await database.BerthWindows.AddAsync(pendingWindow, cancellationToken);
        }

        await database.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedOperationalExecutionDataAsync(CancellationToken cancellationToken)
    {
        var portCallId = Guid.Parse("60000000-0000-0000-0000-000000000002");
        if (await database.PortCallEvents.AnyAsync(
                portCallEvent => portCallEvent.PortCallId == portCallId,
                cancellationToken))
        {
            return;
        }

        var portCall = await database.PortCalls.SingleOrDefaultAsync(
            item => item.Id == portCallId,
            cancellationToken);
        if (portCall is null || portCall.Status != PortCallStatus.Planned)
        {
            return;
        }

        var actor = "system:demo-seed";
        var source = "Simulação operacional";
        var now = DateTimeOffset.UtcNow;
        var milestones = new[]
        {
            new DemoMilestone(
                PortCallStatus.AtAnchorage,
                PortCallEventPhase.Anchorage,
                PortCallEventAction.Arrival,
                now.AddHours(-8)),
            new DemoMilestone(
                PortCallStatus.ClearedForBerthing,
                PortCallEventPhase.Pilotage,
                PortCallEventAction.Start,
                now.AddHours(-6)),
            new DemoMilestone(
                PortCallStatus.Berthed,
                PortCallEventPhase.Berth,
                PortCallEventAction.Completion,
                now.AddHours(-5.5)),
            new DemoMilestone(
                PortCallStatus.InOperation,
                PortCallEventPhase.CargoOperation,
                PortCallEventAction.Start,
                now.AddHours(-5))
        };

        foreach (var milestone in milestones)
        {
            portCall.TransitionTo(
                milestone.Status,
                actor,
                milestone.OccursAtUtc,
                "Avanço operacional demonstrativo.");
            await database.PortCallEvents.AddAsync(
                new PortCallEvent(
                    Guid.NewGuid(),
                    portCall.Id,
                    milestone.Phase,
                    milestone.Action,
                    TemporalClassifier.Actual,
                    milestone.OccursAtUtc,
                    source,
                    actor,
                    now),
                cancellationToken);
        }

        var completedCargo = new CargoOperation(
            Guid.Parse("80000000-0000-0000-0000-000000000001"),
            portCall.Id,
            CargoOperationDirection.Loading,
            "Açúcar a granel",
            15000,
            CargoQuantityUnit.MetricTon,
            false,
            now.AddHours(-6));
        completedCargo.Schedule(now.AddHours(-5), now.AddHours(-2), now.AddHours(-6));
        completedCargo.Start(now.AddHours(-5), now.AddHours(-5));
        completedCargo.Complete(14520, now.AddHours(-2.2), now.AddHours(-2.2));

        var activeCargo = new CargoOperation(
            Guid.Parse("80000000-0000-0000-0000-000000000002"),
            portCall.Id,
            CargoOperationDirection.Discharge,
            "Fertilizante granulado",
            9800,
            CargoQuantityUnit.MetricTon,
            false,
            now.AddHours(-6));
        activeCargo.Schedule(now.AddHours(-4.5), now.AddHours(1), now.AddHours(-6));
        activeCargo.Start(now.AddHours(-4.5), now.AddHours(-4.5));

        await database.CargoOperations.AddRangeAsync(
            [completedCargo, activeCargo],
            cancellationToken);
        await database.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedPlanningDataAsync(CancellationToken cancellationToken)
    {
        if (await database.BerthWindows.AnyAsync(cancellationToken))
        {
            return;
        }

        var plannedCallId = Guid.Parse("60000000-0000-0000-0000-000000000002");
        var anchorageCallId = Guid.Parse("60000000-0000-0000-0000-000000000003");
        var berthOneId = Guid.Parse("30000000-0000-0000-0000-000000000001");
        var berthTwoId = Guid.Parse("30000000-0000-0000-0000-000000000002");
        var availableCalls = await database.PortCalls
            .Where(portCall => portCall.Id == plannedCallId || portCall.Id == anchorageCallId)
            .Select(portCall => portCall.Id)
            .ToArrayAsync(cancellationToken);
        var availableBerths = await database.Berths
            .Where(berth => berth.Id == berthOneId || berth.Id == berthTwoId)
            .Select(berth => berth.Id)
            .ToArrayAsync(cancellationToken);

        if (availableCalls.Length != 2 || availableBerths.Length != 2)
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        var plannedWindow = new BerthWindow(
            Guid.Parse("70000000-0000-0000-0000-000000000001"),
            plannedCallId,
            berthTwoId,
            now.AddHours(3),
            now.AddHours(11),
            "system:demo-seed",
            now);
        plannedWindow.Confirm("system:demo-seed", now);

        var anchorageWindow = new BerthWindow(
            Guid.Parse("70000000-0000-0000-0000-000000000002"),
            anchorageCallId,
            berthOneId,
            now.AddHours(-2),
            now.AddHours(6),
            "system:demo-seed",
            now);
        anchorageWindow.Confirm("system:demo-seed", now);

        await database.BerthWindows.AddRangeAsync(
            [plannedWindow, anchorageWindow],
            cancellationToken);
        await database.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedPortDataAsync(CancellationToken cancellationToken)
    {
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

    private async Task SeedIdentityAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        foreach (var roleName in SecurityRoles.All)
        {
            if (await roleManager.RoleExistsAsync(roleName))
            {
                continue;
            }

            var roleResult = await roleManager.CreateAsync(new ApplicationRole
            {
                Id = Guid.NewGuid(),
                Name = roleName
            });
            EnsureSucceeded(roleResult, $"criar o papel {roleName}");
        }

        var password = configuration["Demo:UserPassword"];
        if (string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidOperationException(
                "Demo:UserPassword é obrigatório para criar os usuários demonstrativos.");
        }

        var demoUsers = new[]
        {
            new DemoUser(
                "Administrador Demo",
                "admin.demo@portmanagement.local",
                SecurityRoles.Administrator,
                null,
                false),
            new DemoUser(
                "Planejador Demo",
                "planner.demo@portmanagement.local",
                SecurityRoles.Planner,
                Guid.Parse("40000000-0000-0000-0000-000000000001"),
                false),
            new DemoUser(
                "Operador Demo",
                "operator.demo@portmanagement.local",
                SecurityRoles.Operator,
                Guid.Parse("40000000-0000-0000-0000-000000000002"),
                false),
            new DemoUser(
                "Visitante Demo",
                "viewer.demo@portmanagement.local",
                SecurityRoles.Viewer,
                null,
                true)
        };

        foreach (var demoUser in demoUsers)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var user = await userManager.FindByEmailAsync(demoUser.Email);
            if (user is null)
            {
                user = new ApplicationUser
                {
                    Id = Guid.NewGuid(),
                    UserName = demoUser.Email,
                    Email = demoUser.Email,
                    EmailConfirmed = true,
                    DisplayName = demoUser.DisplayName,
                    OrganizationId = demoUser.OrganizationId,
                    IsActive = true,
                    CreatedAtUtc = DateTimeOffset.UtcNow
                };
                var creationResult = await userManager.CreateAsync(user, password);
                EnsureSucceeded(creationResult, $"criar o usuário {demoUser.Email}");
            }

            if (user.OrganizationId != demoUser.OrganizationId)
            {
                user.OrganizationId = demoUser.OrganizationId;
                var updateResult = await userManager.UpdateAsync(user);
                EnsureSucceeded(updateResult, $"atualizar o escopo de {demoUser.Email}");
            }

            if (!await userManager.IsInRoleAsync(user, demoUser.Role))
            {
                var assignmentResult = await userManager.AddToRoleAsync(user, demoUser.Role);
                EnsureSucceeded(assignmentResult, $"atribuir o papel {demoUser.Role}");
            }

            var claims = await userManager.GetClaimsAsync(user);
            var globalScopeClaim = claims.SingleOrDefault(claim =>
                claim.Type == DataScopeClaims.Scope
                && claim.Value == DataScopeClaims.Global);
            if (demoUser.HasGlobalDataAccess && globalScopeClaim is null)
            {
                var claimResult = await userManager.AddClaimAsync(
                    user,
                    new Claim(DataScopeClaims.Scope, DataScopeClaims.Global));
                EnsureSucceeded(claimResult, $"atribuir o escopo global a {demoUser.Email}");
            }
            else if (!demoUser.HasGlobalDataAccess && globalScopeClaim is not null)
            {
                var claimResult = await userManager.RemoveClaimAsync(user, globalScopeClaim);
                EnsureSucceeded(claimResult, $"remover o escopo global de {demoUser.Email}");
            }
        }
    }

    private static void EnsureSucceeded(IdentityResult result, string operation)
    {
        if (result.Succeeded)
        {
            return;
        }

        var codes = string.Join(", ", result.Errors.Select(error => error.Code));
        throw new InvalidOperationException($"Não foi possível {operation}: {codes}.");
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

    private sealed record DemoUser(
        string DisplayName,
        string Email,
        string Role,
        Guid? OrganizationId,
        bool HasGlobalDataAccess);

    private sealed record DemoMilestone(
        PortCallStatus Status,
        PortCallEventPhase Phase,
        PortCallEventAction Action,
        DateTimeOffset OccursAtUtc);
}
