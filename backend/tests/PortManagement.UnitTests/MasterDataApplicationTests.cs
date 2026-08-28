using PortManagement.Application.Administration;
using PortManagement.Application.Common;
using PortManagement.Domain.Common;
using PortManagement.Domain.Organizations;
using PortManagement.Domain.Ports;
using PortManagement.Domain.Vessels;

namespace PortManagement.UnitTests;

public sealed class MasterDataApplicationTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 27, 15, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task ListOrganizationsRejectsInvalidPaginationBeforeQueryingRepository()
    {
        var repository = new MasterDataRepositoryFake();
        var handler = new ListOrganizationsHandler(repository);

        var result = await handler.HandleAsync(
            new ListOrganizationsQuery(Page: 0),
            TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        Assert.Equal("master_data.invalid_pagination", result.Error?.Code);
        Assert.Equal(0, repository.OrganizationListCalls);
    }

    [Fact]
    public async Task CreateOrganizationRejectsDuplicateRegistration()
    {
        var repository = new MasterDataRepositoryFake
        {
            OrganizationRegistrationExists = true
        };
        var unitOfWork = new UnitOfWorkFake();
        var handler = new CreateOrganizationHandler(
            repository,
            unitOfWork,
            new FixedTimeProvider(Now));

        var result = await handler.HandleAsync(
            new CreateOrganizationCommand(
                "Agência Marítima",
                " 12.345.678/0001-90 ",
                OrganizationType.ShippingAgency),
            TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        Assert.Equal("master_data.organization_registration_exists", result.Error?.Code);
        Assert.Null(repository.AddedOrganization);
        Assert.Equal(0, unitOfWork.SaveCalls);
    }

    [Fact]
    public async Task UpdateOrganizationBlocksDeactivationWhileActiveUsersExist()
    {
        var organization = CreateOrganization();
        var repository = new MasterDataRepositoryFake
        {
            Organization = organization,
            OrganizationHasActiveUsers = true
        };
        var handler = new UpdateOrganizationHandler(
            repository,
            new UnitOfWorkFake(),
            new FixedTimeProvider(Now));

        var result = await handler.HandleAsync(
            new UpdateOrganizationCommand(
                organization.Id,
                organization.Name,
                organization.RegistrationNumber,
                organization.Type,
                false,
                organization.UpdatedAtUtc),
            TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        Assert.Equal("master_data.organization_has_active_users", result.Error?.Code);
        Assert.True(organization.IsActive);
    }

    [Fact]
    public async Task UpdateOrganizationPersistsNormalizedDataAndExpectedVersion()
    {
        var organization = CreateOrganization();
        var repository = new MasterDataRepositoryFake { Organization = organization };
        var unitOfWork = new UnitOfWorkFake();
        var handler = new UpdateOrganizationHandler(
            repository,
            unitOfWork,
            new FixedTimeProvider(Now));

        var result = await handler.HandleAsync(
            new UpdateOrganizationCommand(
                organization.Id,
                " Agência Atualizada ",
                " registro-02 ",
                OrganizationType.ShippingLine,
                true,
                organization.UpdatedAtUtc),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal("Agência Atualizada", result.Value?.Name);
        Assert.Equal("REGISTRO-02", result.Value?.RegistrationNumber);
        Assert.Equal(Now.AddDays(-1), repository.ExpectedUpdatedAtUtc);
        Assert.Equal(1, unitOfWork.SaveCalls);
    }

    [Fact]
    public async Task UpdatePortRejectsAStaleVersion()
    {
        var port = CreatePort();
        var repository = new MasterDataRepositoryFake { Port = port };
        var handler = new UpdatePortHandler(
            repository,
            new UnitOfWorkFake(),
            new FixedTimeProvider(Now));

        var result = await handler.HandleAsync(
            new UpdatePortCommand(
                port.Id,
                port.Name,
                port.UnLocode,
                port.CountryCode,
                port.TimeZoneId,
                true,
                port.UpdatedAtUtc.AddSeconds(-1)),
            TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        Assert.Equal("master_data.concurrent_update", result.Error?.Code);
    }

    [Fact]
    public async Task UpdatePortBlocksDeactivationWhileActiveTerminalsExist()
    {
        var port = CreatePort();
        var repository = new MasterDataRepositoryFake
        {
            Port = port,
            PortHasActiveTerminals = true
        };
        var handler = new UpdatePortHandler(
            repository,
            new UnitOfWorkFake(),
            new FixedTimeProvider(Now));

        var result = await handler.HandleAsync(
            new UpdatePortCommand(
                port.Id,
                port.Name,
                port.UnLocode,
                port.CountryCode,
                port.TimeZoneId,
                false,
                port.UpdatedAtUtc),
            TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        Assert.Equal("master_data.port_has_active_terminals", result.Error?.Code);
        Assert.True(port.IsActive);
    }

    [Fact]
    public async Task CreateTerminalRejectsAnInactivePort()
    {
        var port = CreatePort();
        port.Update(
            port.Name,
            port.UnLocode,
            port.CountryCode,
            port.TimeZoneId,
            false,
            Now.AddHours(-1));
        var repository = new MasterDataRepositoryFake { Port = port };
        var handler = new CreateTerminalHandler(
            repository,
            new UnitOfWorkFake(),
            new FixedTimeProvider(Now));

        var result = await handler.HandleAsync(
            new CreateTerminalCommand(port.Id, "T01", "Terminal 01", "UTC"),
            TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        Assert.Equal("master_data.inactive_parent", result.Error?.Code);
        Assert.Null(repository.AddedTerminal);
    }

    [Fact]
    public async Task UpdateTerminalBlocksDeactivationWhileAvailableBerthsExist()
    {
        var terminal = CreateTerminal();
        var repository = new MasterDataRepositoryFake
        {
            Terminal = terminal,
            TerminalHasAvailableBerths = true
        };
        var handler = new UpdateTerminalHandler(
            repository,
            new UnitOfWorkFake(),
            new FixedTimeProvider(Now));

        var result = await handler.HandleAsync(
            new UpdateTerminalCommand(
                terminal.Id,
                terminal.Code,
                terminal.Name,
                terminal.TimeZoneId,
                false,
                terminal.UpdatedAtUtc),
            TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        Assert.Equal("master_data.terminal_has_available_berths", result.Error?.Code);
        Assert.True(terminal.IsActive);
    }

    [Fact]
    public async Task UpdateTerminalBlocksActivationInsideAnInactivePort()
    {
        var port = CreatePort();
        port.Update(
            port.Name,
            port.UnLocode,
            port.CountryCode,
            port.TimeZoneId,
            false,
            Now.AddHours(-2));
        var terminal = CreateTerminal(port.Id);
        terminal.Update(
            terminal.Code,
            terminal.Name,
            terminal.TimeZoneId,
            false,
            Now.AddHours(-1));
        var repository = new MasterDataRepositoryFake { Port = port, Terminal = terminal };
        var handler = new UpdateTerminalHandler(
            repository,
            new UnitOfWorkFake(),
            new FixedTimeProvider(Now));

        var result = await handler.HandleAsync(
            new UpdateTerminalCommand(
                terminal.Id,
                terminal.Code,
                terminal.Name,
                terminal.TimeZoneId,
                true,
                terminal.UpdatedAtUtc),
            TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        Assert.Equal("master_data.inactive_parent", result.Error?.Code);
        Assert.False(terminal.IsActive);
    }

    [Fact]
    public async Task UpdateBerthBlocksCapacityChangesWhileOpenWindowsExist()
    {
        var terminal = CreateTerminal();
        var berth = CreateBerth(terminal.Id);
        var repository = new MasterDataRepositoryFake
        {
            Terminal = terminal,
            Berth = berth,
            BerthHasOpenWindows = true
        };
        var handler = new UpdateBerthHandler(
            repository,
            new UnitOfWorkFake(),
            new FixedTimeProvider(Now));

        var result = await handler.HandleAsync(
            new UpdateBerthCommand(
                berth.Id,
                berth.Code,
                berth.Name,
                berth.UsefulLengthMeters - 10,
                berth.MaximumBeamMeters,
                berth.MaximumDraftMeters,
                berth.SupportedVesselTypes,
                berth.Status,
                berth.UpdatedAtUtc),
            TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        Assert.Equal("master_data.berth_has_open_windows", result.Error?.Code);
        Assert.Equal(350, berth.UsefulLengthMeters);
    }

    [Fact]
    public async Task CreateBerthRejectsInvalidVesselTypes()
    {
        var terminal = CreateTerminal();
        var repository = new MasterDataRepositoryFake { Terminal = terminal };
        var handler = new CreateBerthHandler(
            repository,
            new UnitOfWorkFake(),
            new FixedTimeProvider(Now));

        var result = await handler.HandleAsync(
            new CreateBerthCommand(
                terminal.Id,
                "B02",
                "Berço 02",
                300,
                45,
                14,
                [(VesselType)999]),
            TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        Assert.Equal("master_data.invalid_data", result.Error?.Code);
        Assert.Null(repository.AddedBerth);
    }

    [Fact]
    public async Task UpdateBerthBlocksAvailabilityInsideAnInactiveTerminal()
    {
        var terminal = CreateTerminal();
        terminal.Update(
            terminal.Code,
            terminal.Name,
            terminal.TimeZoneId,
            false,
            Now.AddHours(-1));
        var berth = CreateBerth(terminal.Id);
        berth.Update(
            berth.Code,
            berth.Name,
            berth.UsefulLengthMeters,
            berth.MaximumBeamMeters,
            berth.MaximumDraftMeters,
            berth.SupportedVesselTypes,
            BerthStatus.Unavailable,
            Now.AddMinutes(-30));
        var repository = new MasterDataRepositoryFake { Terminal = terminal, Berth = berth };
        var handler = new UpdateBerthHandler(
            repository,
            new UnitOfWorkFake(),
            new FixedTimeProvider(Now));

        var result = await handler.HandleAsync(
            new UpdateBerthCommand(
                berth.Id,
                berth.Code,
                berth.Name,
                berth.UsefulLengthMeters,
                berth.MaximumBeamMeters,
                berth.MaximumDraftMeters,
                berth.SupportedVesselTypes,
                BerthStatus.Available,
                berth.UpdatedAtUtc),
            TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        Assert.Equal("master_data.inactive_parent", result.Error?.Code);
        Assert.Equal(BerthStatus.Unavailable, berth.Status);
    }

    private static Organization CreateOrganization() => new(
        Guid.NewGuid(),
        "Agência Demo",
        "REGISTRO-01",
        OrganizationType.ShippingAgency,
        Now.AddDays(-1));

    private static Port CreatePort() => new(
        Guid.NewGuid(),
        "Porto de Santos",
        "BRSSZ",
        "BR",
        "UTC",
        Now.AddDays(-1));

    private static Terminal CreateTerminal(Guid? portId = null) => new(
        Guid.NewGuid(),
        portId ?? Guid.NewGuid(),
        "T01",
        "Terminal 01",
        "UTC",
        Now.AddDays(-1));

    private static Berth CreateBerth(Guid? terminalId = null) => new(
        Guid.NewGuid(),
        terminalId ?? Guid.NewGuid(),
        "B01",
        "Berço 01",
        350,
        50,
        15,
        [VesselType.ContainerShip],
        Now.AddDays(-1));

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }

    private sealed class UnitOfWorkFake : IUnitOfWork
    {
        public int SaveCalls { get; private set; }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveCalls++;
            return Task.FromResult(1);
        }
    }

    private sealed class MasterDataRepositoryFake : IMasterDataRepository
    {
        public Organization? Organization { get; init; }

        public Port? Port { get; init; }

        public Terminal? Terminal { get; init; }

        public Berth? Berth { get; init; }

        public bool OrganizationRegistrationExists { get; init; }

        public bool OrganizationHasActiveUsers { get; init; }

        public bool PortHasActiveTerminals { get; init; }

        public bool TerminalHasAvailableBerths { get; init; }

        public bool BerthHasOpenWindows { get; init; }

        public int OrganizationListCalls { get; private set; }

        public Organization? AddedOrganization { get; private set; }

        public Terminal? AddedTerminal { get; private set; }

        public Berth? AddedBerth { get; private set; }

        public DateTimeOffset? ExpectedUpdatedAtUtc { get; private set; }

        public Task<PagedResult<OrganizationAdminResponse>> ListOrganizationsAsync(
            ListOrganizationsQuery query,
            CancellationToken cancellationToken)
        {
            OrganizationListCalls++;
            return Task.FromResult(new PagedResult<OrganizationAdminResponse>(
                [],
                query.Page,
                query.PageSize,
                0));
        }

        public Task<bool> OrganizationRegistrationExistsAsync(
            string registrationNumber,
            Guid? excludingId,
            CancellationToken cancellationToken) =>
            Task.FromResult(OrganizationRegistrationExists);

        public Task<bool> OrganizationHasActiveUsersAsync(
            Guid organizationId,
            CancellationToken cancellationToken) => Task.FromResult(OrganizationHasActiveUsers);

        public Task AddOrganizationAsync(
            Organization organization,
            CancellationToken cancellationToken)
        {
            AddedOrganization = organization;
            return Task.CompletedTask;
        }

        public Task<Organization?> FindOrganizationAsync(
            Guid id,
            CancellationToken cancellationToken) =>
            Task.FromResult(Organization?.Id == id ? Organization : null);

        public Task<IReadOnlyCollection<PortAdminResponse>> ListPortStructureAsync(
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyCollection<PortAdminResponse>>([]);

        public Task<bool> PortUnLocodeExistsAsync(
            string unLocode,
            Guid? excludingId,
            CancellationToken cancellationToken) => Task.FromResult(false);

        public Task<bool> PortHasActiveTerminalsAsync(
            Guid portId,
            CancellationToken cancellationToken) => Task.FromResult(PortHasActiveTerminals);

        public Task AddPortAsync(Port port, CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task<Port?> FindPortAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(Port?.Id == id ? Port : null);

        public Task<bool> TerminalCodeExistsAsync(
            Guid portId,
            string code,
            Guid? excludingId,
            CancellationToken cancellationToken) => Task.FromResult(false);

        public Task<bool> TerminalHasAvailableBerthsAsync(
            Guid terminalId,
            CancellationToken cancellationToken) => Task.FromResult(TerminalHasAvailableBerths);

        public Task AddTerminalAsync(Terminal terminal, CancellationToken cancellationToken)
        {
            AddedTerminal = terminal;
            return Task.CompletedTask;
        }

        public Task<Terminal?> FindTerminalAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(Terminal?.Id == id ? Terminal : null);

        public Task<bool> BerthCodeExistsAsync(
            Guid terminalId,
            string code,
            Guid? excludingId,
            CancellationToken cancellationToken) => Task.FromResult(false);

        public Task<bool> BerthHasOpenWindowsAsync(
            Guid berthId,
            DateTimeOffset fromUtc,
            CancellationToken cancellationToken) => Task.FromResult(BerthHasOpenWindows);

        public Task AddBerthAsync(Berth berth, CancellationToken cancellationToken)
        {
            AddedBerth = berth;
            return Task.CompletedTask;
        }

        public Task<Berth?> FindBerthAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(Berth?.Id == id ? Berth : null);

        public void UseExpectedUpdatedAt(
            AuditableEntity entity,
            DateTimeOffset expectedUpdatedAtUtc) =>
            ExpectedUpdatedAtUtc = expectedUpdatedAtUtc;
    }
}
