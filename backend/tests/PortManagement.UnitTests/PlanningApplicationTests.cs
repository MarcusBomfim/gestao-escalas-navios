using PortManagement.Application.Common;
using PortManagement.Application.Planning;
using PortManagement.Domain.Planning;
using PortManagement.Domain.PortCalls;
using PortManagement.Domain.Ports;
using PortManagement.Domain.Vessels;

namespace PortManagement.UnitTests;

public sealed class PlanningApplicationTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 22, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task RequestAssignsACompatibleBerthToThePortCall()
    {
        var context = CreateContext();
        var repository = new BerthWindowRepositoryFake(context.PortCall, context.Vessel, context.Berth, context.PortId);
        var unitOfWork = new UnitOfWorkFake();
        var handler = new RequestBerthWindowHandler(repository, unitOfWork);

        var result = await handler.HandleAsync(
            CreateRequest(context.PortCall, context.Berth.Id),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.NotNull(repository.AddedWindow);
        Assert.Equal(context.Berth.Id, context.PortCall.PlannedBerthId);
        Assert.Equal(1, unitOfWork.SaveCalls);
    }

    [Fact]
    public async Task RequestRejectsAnIncompatibleBerth()
    {
        var context = CreateContext(maximumDraft: 10);
        var repository = new BerthWindowRepositoryFake(context.PortCall, context.Vessel, context.Berth, context.PortId);
        var handler = new RequestBerthWindowHandler(repository, new UnitOfWorkFake());

        var result = await handler.HandleAsync(
            CreateRequest(context.PortCall, context.Berth.Id),
            TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        Assert.Equal("planning.incompatible_berth", result.Error?.Code);
        Assert.Null(repository.AddedWindow);
    }

    [Fact]
    public async Task RequestRejectsAConfirmedOverlap()
    {
        var context = CreateContext();
        var repository = new BerthWindowRepositoryFake(context.PortCall, context.Vessel, context.Berth, context.PortId)
        {
            HasConfirmedOverlap = true
        };
        var handler = new RequestBerthWindowHandler(repository, new UnitOfWorkFake());

        var result = await handler.HandleAsync(
            CreateRequest(context.PortCall, context.Berth.Id),
            TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        Assert.Equal("planning.berth_window_overlap", result.Error?.Code);
    }

    [Fact]
    public async Task ConfirmRejectsAStaleWindowVersion()
    {
        var context = CreateContext();
        var window = new BerthWindow(
            Guid.NewGuid(),
            context.PortCall.Id,
            context.Berth.Id,
            Now.AddHours(2),
            Now.AddHours(8),
            "planner:test",
            Now);
        var repository = new BerthWindowRepositoryFake(context.PortCall, context.Vessel, context.Berth, context.PortId)
        {
            ActiveWindow = window
        };
        var unitOfWork = new UnitOfWorkFake();
        var handler = new ConfirmBerthWindowHandler(repository, unitOfWork);

        var result = await handler.HandleAsync(
            new ChangeBerthWindowStatusCommand(
                context.PortCall.PublicCode,
                ExpectedWindowVersion: 99,
                "planner:test",
                null),
            TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        Assert.Equal("planning.version_conflict", result.Error?.Code);
        Assert.Equal(0, unitOfWork.SaveCalls);
    }

    private static RequestBerthWindowCommand CreateRequest(PortCall portCall, Guid berthId) => new(
        portCall.PublicCode,
        berthId,
        Now.AddHours(2),
        Now.AddHours(8),
        portCall.Version,
        "planner:test");

    private static PlanningContext CreateContext(decimal maximumDraft = 14)
    {
        var portId = Guid.NewGuid();
        var terminalId = Guid.NewGuid();
        var vessel = new Vessel(
            Guid.NewGuid(),
            "Navio Planejado",
            ImoNumber.Parse("IMO9074729"),
            "BR",
            VesselType.ContainerShip,
            280,
            40,
            12,
            Now);
        var portCall = new PortCall(
            Guid.NewGuid(),
            vessel.Id,
            portId,
            PortCallPurpose.CargoOperation,
            Guid.NewGuid().ToString("N"),
            Now);
        portCall.TransitionTo(PortCallStatus.Requested, "planner:test", Now.AddMinutes(1));
        portCall.TransitionTo(PortCallStatus.UnderReview, "planner:test", Now.AddMinutes(2));
        var berth = new Berth(
            Guid.NewGuid(),
            terminalId,
            "B01",
            "Berço de teste",
            320,
            48,
            maximumDraft,
            [VesselType.ContainerShip],
            Now);

        return new PlanningContext(portId, portCall, vessel, berth);
    }

    private sealed record PlanningContext(Guid PortId, PortCall PortCall, Vessel Vessel, Berth Berth);

    private sealed class BerthWindowRepositoryFake(
        PortCall portCall,
        Vessel vessel,
        Berth berth,
        Guid portId) : IBerthWindowRepository
    {
        public bool HasConfirmedOverlap { get; init; }

        public BerthWindow? ActiveWindow { get; init; }

        public BerthWindow? AddedWindow { get; private set; }

        public Task<PortCallPlanningReference?> FindPortCallForPlanningAsync(
            string publicCode,
            CancellationToken cancellationToken) =>
            Task.FromResult<PortCallPlanningReference?>(new(portCall, vessel));

        public Task<BerthPlanningReference?> FindBerthForPlanningAsync(
            Guid berthId,
            CancellationToken cancellationToken) =>
            Task.FromResult<BerthPlanningReference?>(new(berth, portId));

        public Task<BerthWindow?> FindActiveTrackedByPortCallAsync(
            Guid portCallId,
            CancellationToken cancellationToken) =>
            Task.FromResult(ActiveWindow);

        public Task<bool> ConfirmedOverlapExistsAsync(
            Guid berthId,
            DateTimeOffset startsAtUtc,
            DateTimeOffset endsAtUtc,
            Guid? excludingWindowId,
            CancellationToken cancellationToken) =>
            Task.FromResult(HasConfirmedOverlap);

        public Task AddAsync(BerthWindow window, CancellationToken cancellationToken)
        {
            AddedWindow = window;
            return Task.CompletedTask;
        }

        public Task<BerthWindowResponse?> GetDetailsByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            var window = AddedWindow ?? ActiveWindow;
            return Task.FromResult(window is null ? null : CreateResponse(window));
        }

        public Task<BerthWindowResponse?> GetActiveDetailsByPublicCodeAsync(
            string publicCode,
            CancellationToken cancellationToken) =>
            Task.FromResult(ActiveWindow is null ? null : CreateResponse(ActiveWindow));

        public Task<PagedResult<BerthWindowResponse>> ListAsync(
            ListBerthWindowsQuery query,
            CancellationToken cancellationToken) =>
            Task.FromResult(new PagedResult<BerthWindowResponse>([], 1, 20, 0));

        private BerthWindowResponse CreateResponse(BerthWindow window) => new(
            window.Id,
            portCall.Id,
            portCall.PublicCode,
            vessel.Id,
            vessel.Name,
            portId,
            "Porto de teste",
            berth.TerminalId,
            "Terminal de teste",
            berth.Id,
            berth.Code,
            berth.Name,
            window.StartsAtUtc,
            window.EndsAtUtc,
            window.Status,
            window.RequestedBy,
            window.LastChangedBy,
            window.LastChangeReason,
            window.Version,
            window.CreatedAtUtc,
            window.UpdatedAtUtc,
            []);
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
}
