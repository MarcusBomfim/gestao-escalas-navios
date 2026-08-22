using PortManagement.Application.Common;
using PortManagement.Application.Operations;
using PortManagement.Domain.Operations;
using PortManagement.Domain.PortCalls;

namespace PortManagement.UnitTests;

public sealed class OperationalExecutionApplicationTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 22, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task RecordMilestoneAdvancesStatusAndAddsAnActualEvent()
    {
        var portCall = CreatePlannedPortCall();
        var repository = new OperationalRepositoryFake(portCall);
        var handler = new RecordOperationalMilestoneHandler(repository, new UnitOfWorkFake(), new FixedTimeProvider(Now));

        var result = await handler.HandleAsync(
            new RecordOperationalMilestoneCommand(
                portCall.PublicCode,
                OperationalMilestone.ArrivedAtAnchorage,
                Now.AddMinutes(-10),
                portCall.Version,
                "Teste automatizado",
                "operator:test"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(PortCallStatus.AtAnchorage, portCall.Status);
        var portCallEvent = Assert.Single(repository.Events);
        Assert.Equal(TemporalClassifier.Actual, portCallEvent.Classifier);
        Assert.Equal(PortCallEventPhase.Anchorage, portCallEvent.Phase);
    }

    [Fact]
    public async Task RecordMilestoneRejectsAnInvalidSequence()
    {
        var portCall = CreatePlannedPortCall();
        var repository = new OperationalRepositoryFake(portCall);
        var handler = new RecordOperationalMilestoneHandler(repository, new UnitOfWorkFake(), new FixedTimeProvider(Now));

        var result = await handler.HandleAsync(
            new RecordOperationalMilestoneCommand(
                portCall.PublicCode,
                OperationalMilestone.BerthingCompleted,
                Now,
                portCall.Version,
                "Teste automatizado",
                "operator:test"),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("operations.invalid_sequence", result.Error?.Code);
        Assert.Empty(repository.Events);
    }

    [Fact]
    public async Task RecordMilestoneRejectsAFutureActualEvent()
    {
        var portCall = CreatePlannedPortCall();
        var repository = new OperationalRepositoryFake(portCall);
        var handler = new RecordOperationalMilestoneHandler(repository, new UnitOfWorkFake(), new FixedTimeProvider(Now));

        var result = await handler.HandleAsync(
            new RecordOperationalMilestoneCommand(
                portCall.PublicCode,
                OperationalMilestone.ArrivedAtAnchorage,
                Now.AddMinutes(6),
                portCall.Version,
                "Teste automatizado",
                "operator:test"),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("operations.future_event", result.Error?.Code);
    }

    private static PortCall CreatePlannedPortCall()
    {
        var portCall = new PortCall(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            PortCallPurpose.CargoOperation,
            Guid.NewGuid().ToString("N"),
            Now.AddDays(-1));
        portCall.TransitionTo(PortCallStatus.Requested, "planner:test", Now.AddHours(-12));
        portCall.TransitionTo(PortCallStatus.UnderReview, "planner:test", Now.AddHours(-11));
        portCall.TransitionTo(PortCallStatus.Planned, "planner:test", Now.AddHours(-10));
        return portCall;
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }

    private sealed class UnitOfWorkFake : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(1);
    }

    private sealed class OperationalRepositoryFake(PortCall portCall) : IOperationalExecutionRepository
    {
        public List<PortCallEvent> Events { get; } = [];

        public Task<PortCall?> FindPortCallTrackedAsync(string publicCode, CancellationToken cancellationToken) =>
            Task.FromResult<PortCall?>(portCall.PublicCode == publicCode ? portCall : null);

        public Task<CargoOperation?> FindCargoOperationTrackedAsync(string publicCode, Guid cargoOperationId, CancellationToken cancellationToken) =>
            Task.FromResult<CargoOperation?>(null);

        public Task<DateTimeOffset?> GetLatestActualEventAtAsync(Guid portCallId, CancellationToken cancellationToken) =>
            Task.FromResult(Events.Count == 0 ? (DateTimeOffset?)null : Events.Max(item => item.OccursAtUtc));

        public Task<bool> HasCargoOperationsAsync(Guid portCallId, CancellationToken cancellationToken) => Task.FromResult(false);

        public Task<bool> AreAllCargoOperationsCompletedAsync(Guid portCallId, CancellationToken cancellationToken) => Task.FromResult(false);

        public Task<OperationalExecutionResponse?> GetAsync(string publicCode, CancellationToken cancellationToken) =>
            Task.FromResult<OperationalExecutionResponse?>(new OperationalExecutionResponse(
                portCall.Id,
                portCall.PublicCode,
                portCall.Status,
                portCall.Version,
                OperationalMilestoneRules.NextFor(portCall.Status),
                [],
                [],
                new OperationalKpiResponse(null, null, null, [])));

        public Task AddEventAsync(PortCallEvent portCallEvent, CancellationToken cancellationToken)
        {
            Events.Add(portCallEvent);
            return Task.CompletedTask;
        }

        public Task AddCargoOperationAsync(CargoOperation cargoOperation, CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
