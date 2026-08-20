using PortManagement.Application.Common;
using PortManagement.Application.PortCalls;
using PortManagement.Domain.PortCalls;

namespace PortManagement.UnitTests;

public sealed class PortCallApplicationTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 20, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task TransitionRejectsAStaleVersionWithoutSaving()
    {
        var repository = new PortCallRepositoryFake
        {
            TrackedPortCall = CreatePortCall()
        };
        var unitOfWork = new UnitOfWorkFake();
        var handler = new TransitionPortCallHandler(repository, unitOfWork);

        var result = await handler.HandleAsync(
            new TransitionPortCallCommand(
                repository.TrackedPortCall.PublicCode,
                PortCallStatus.Requested,
                ExpectedVersion: 10,
                "system:test",
                null),
            TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        Assert.Equal("port_calls.version_conflict", result.Error?.Code);
        Assert.Equal(0, unitOfWork.SaveCalls);
    }

    [Fact]
    public async Task CreateRequiresAnIdempotencyKey()
    {
        var repository = new PortCallRepositoryFake();
        var handler = new CreatePortCallHandler(repository, new UnitOfWorkFake());

        var result = await handler.HandleAsync(
            new CreatePortCallCommand(
                Guid.NewGuid(),
                Guid.NewGuid(),
                PortCallPurpose.CargoOperation,
                string.Empty,
                null,
                null,
                null),
            TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        Assert.Equal("port_calls.idempotency_key_required", result.Error?.Code);
    }

    [Fact]
    public async Task RepeatedIdempotencyKeyReturnsTheExistingPortCall()
    {
        var existing = CreatePortCall();
        var response = CreateResponse(existing);
        var repository = new PortCallRepositoryFake
        {
            PortCallByIdempotencyKey = existing,
            Details = response
        };
        var handler = new CreatePortCallHandler(repository, new UnitOfWorkFake());

        var result = await handler.HandleAsync(
            new CreatePortCallCommand(
                existing.VesselId,
                existing.PortId,
                existing.Purpose,
                existing.IdempotencyKey,
                null,
                null,
                null),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value?.Created);
        Assert.Equal(existing.PublicCode, result.Value?.PortCall.PublicCode);
    }

    private static PortCall CreatePortCall() => new(
        Guid.NewGuid(),
        Guid.NewGuid(),
        Guid.NewGuid(),
        PortCallPurpose.CargoOperation,
        Guid.NewGuid().ToString("N"),
        Now);

    private static PortCallResponse CreateResponse(PortCall portCall) => new(
        portCall.Id,
        portCall.PublicCode,
        portCall.VesselId,
        "Navio Demo",
        portCall.PortId,
        "Porto Demo",
        portCall.Purpose,
        portCall.Status,
        null,
        null,
        null,
        null,
        null,
        null,
        null,
        portCall.Version,
        portCall.CreatedAtUtc,
        portCall.UpdatedAtUtc,
        null,
        []);

    private sealed class PortCallRepositoryFake : IPortCallRepository
    {
        public PortCall? PortCallByIdempotencyKey { get; init; }

        public PortCall? TrackedPortCall { get; init; }

        public PortCallResponse? Details { get; init; }

        public Task<bool> ActiveVesselExistsAsync(Guid vesselId, CancellationToken cancellationToken) =>
            Task.FromResult(true);

        public Task<bool> ActivePortExistsAsync(Guid portId, CancellationToken cancellationToken) =>
            Task.FromResult(true);

        public Task<PortCall?> FindByIdempotencyKeyAsync(
            string idempotencyKey,
            CancellationToken cancellationToken) =>
            Task.FromResult(PortCallByIdempotencyKey);

        public Task<PortCall?> FindTrackedByPublicCodeAsync(
            string publicCode,
            CancellationToken cancellationToken) =>
            Task.FromResult(TrackedPortCall);

        public Task<PortCallResponse?> GetDetailsByPublicCodeAsync(
            string publicCode,
            CancellationToken cancellationToken) =>
            Task.FromResult(Details);

        public Task<PagedResult<PortCallResponse>> ListAsync(
            ListPortCallsQuery query,
            CancellationToken cancellationToken) =>
            Task.FromResult(new PagedResult<PortCallResponse>([], 1, 20, 0));

        public Task AddAsync(PortCall portCall, CancellationToken cancellationToken) =>
            Task.CompletedTask;
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
