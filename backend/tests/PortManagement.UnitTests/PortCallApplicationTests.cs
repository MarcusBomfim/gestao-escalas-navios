using PortManagement.Application.Common;
using PortManagement.Application.PortCalls;
using PortManagement.Application.Security;
using PortManagement.Domain.Organizations;
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
        var handler = new CreatePortCallHandler(repository, new UnitOfWorkFake(), GlobalDataScope);

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
        var handler = new CreatePortCallHandler(repository, new UnitOfWorkFake(), GlobalDataScope);

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

    [Fact]
    public async Task ScopedShippingAgencyOwnsNewPortCallAndUsesScopedIdempotency()
    {
        var organizationId = Guid.Parse("70000000-0000-0000-0000-000000000007");
        var repository = new PortCallRepositoryFake
        {
            OrganizationType = OrganizationType.ShippingAgency
        };
        var unitOfWork = new UnitOfWorkFake();
        var handler = new CreatePortCallHandler(
            repository,
            unitOfWork,
            new DataScopeFake(organizationId, false));

        var result = await handler.HandleAsync(
            new CreatePortCallCommand(
                Guid.NewGuid(),
                Guid.NewGuid(),
                PortCallPurpose.CargoOperation,
                "request-001",
                null,
                null,
                null),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(organizationId, repository.AddedPortCall?.AgentOrganizationId);
        Assert.Null(repository.AddedPortCall?.ShippingLineOrganizationId);
        Assert.NotEqual("request-001", repository.AddedPortCall?.IdempotencyKey);
        Assert.Equal(64, repository.AddedPortCall?.IdempotencyKey.Length);
        Assert.Equal(1, unitOfWork.SaveCalls);
    }

    [Fact]
    public async Task OrganizationWithoutOwnershipRoleCannotCreatePortCall()
    {
        var organizationId = Guid.Parse("80000000-0000-0000-0000-000000000008");
        var repository = new PortCallRepositoryFake
        {
            OrganizationType = OrganizationType.TerminalOperator
        };
        var unitOfWork = new UnitOfWorkFake();
        var handler = new CreatePortCallHandler(
            repository,
            unitOfWork,
            new DataScopeFake(organizationId, false));

        var result = await handler.HandleAsync(
            new CreatePortCallCommand(
                Guid.NewGuid(),
                Guid.NewGuid(),
                PortCallPurpose.CargoOperation,
                "request-002",
                null,
                null,
                null),
            TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        Assert.Equal("port_calls.organization_not_allowed", result.Error?.Code);
        Assert.Equal(ApplicationErrorType.Forbidden, result.Error?.Type);
        Assert.Null(repository.AddedPortCall);
        Assert.Equal(0, unitOfWork.SaveCalls);
    }

    private static IUserDataScope GlobalDataScope { get; } = new DataScopeFake(null, true);

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

        public OrganizationType? OrganizationType { get; init; }

        public PortCall? AddedPortCall { get; private set; }

        public Task<bool> ActiveVesselExistsAsync(Guid vesselId, CancellationToken cancellationToken) =>
            Task.FromResult(true);

        public Task<bool> ActivePortExistsAsync(Guid portId, CancellationToken cancellationToken) =>
            Task.FromResult(true);

        public Task<OrganizationType?> GetActiveOrganizationTypeAsync(
            Guid organizationId,
            CancellationToken cancellationToken) =>
            Task.FromResult(OrganizationType);

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
            CancellationToken cancellationToken) => Task.FromResult(
                Details ?? (AddedPortCall is null ? null : CreateResponse(AddedPortCall)));

        public Task<PagedResult<PortCallResponse>> ListAsync(
            ListPortCallsQuery query,
            CancellationToken cancellationToken) =>
            Task.FromResult(new PagedResult<PortCallResponse>([], 1, 20, 0));

        public Task AddAsync(PortCall portCall, CancellationToken cancellationToken)
        {
            AddedPortCall = portCall;
            return Task.CompletedTask;
        }
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

    private sealed record DataScopeFake(
        Guid? OrganizationId,
        bool HasGlobalAccess) : IUserDataScope;
}
