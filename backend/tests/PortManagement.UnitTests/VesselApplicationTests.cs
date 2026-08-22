using PortManagement.Application.Common;
using PortManagement.Application.Vessels;
using PortManagement.Domain.Vessels;

namespace PortManagement.UnitTests;

public sealed class VesselApplicationTests
{
    [Fact]
    public async Task RegisterReturnsConflictWhenActiveImoAlreadyExists()
    {
        var repository = new VesselRepositoryFake { ImoExists = true };
        var handler = new RegisterVesselHandler(repository, new UnitOfWorkFake());
        var command = new RegisterVesselCommand(
            "Navio Demo",
            "IMO9074729",
            "BR",
            VesselType.ContainerShip,
            250,
            38,
            12,
            null,
            null);

        var result = await handler.HandleAsync(command, TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        Assert.Equal("vessels.imo_already_exists", result.Error?.Code);
        Assert.Null(repository.AddedVessel);
    }

    [Fact]
    public async Task ListRejectsAnInvalidPageBeforeQueryingTheRepository()
    {
        var repository = new VesselRepositoryFake();
        var handler = new ListVesselsHandler(repository);

        var result = await handler.HandleAsync(
            new ListVesselsQuery(Page: 0),
            TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        Assert.Equal("pagination.invalid", result.Error?.Code);
        Assert.False(repository.ListWasCalled);
    }

    [Fact]
    public async Task UpdateChangesOperationalDetailsAndSaves()
    {
        var vessel = new Vessel(
            Guid.NewGuid(),
            "Navio Antigo",
            ImoNumber.Parse("IMO9074729"),
            "BR",
            VesselType.ContainerShip,
            250,
            38,
            12,
            DateTimeOffset.UtcNow);
        var repository = new VesselRepositoryFake { TrackedVessel = vessel };
        var unitOfWork = new UnitOfWorkFake();
        var handler = new UpdateVesselHandler(repository, unitOfWork);

        var result = await handler.HandleAsync(
            new UpdateVesselCommand(
                vessel.Id,
                "Navio Atualizado",
                "IMO9074729",
                "PA",
                VesselType.GeneralCargo,
                210,
                32,
                10.5m,
                "PTMB",
                "710000001"),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal("Navio Atualizado", vessel.Name);
        Assert.Equal("PA", vessel.FlagCode);
        Assert.Equal(vessel.Id, repository.ExcludedVesselId);
        Assert.Equal(1, unitOfWork.SaveCalls);
    }

    private sealed class VesselRepositoryFake : IVesselRepository
    {
        public bool ImoExists { get; init; }

        public bool ListWasCalled { get; private set; }

        public Vessel? AddedVessel { get; private set; }

        public Vessel? TrackedVessel { get; init; }

        public Guid? ExcludedVesselId { get; private set; }

        public Task<bool> ActiveImoExistsAsync(
            ImoNumber imoNumber,
            Guid? excludingVesselId,
            CancellationToken cancellationToken)
        {
            ExcludedVesselId = excludingVesselId;
            return Task.FromResult(ImoExists);
        }

        public Task AddAsync(Vessel vessel, CancellationToken cancellationToken)
        {
            AddedVessel = vessel;
            return Task.CompletedTask;
        }

        public Task<Vessel?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult<Vessel?>(null);

        public Task<Vessel?> FindTrackedByIdAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(TrackedVessel);

        public Task<PagedResult<VesselResponse>> ListAsync(
            ListVesselsQuery query,
            CancellationToken cancellationToken)
        {
            ListWasCalled = true;
            return Task.FromResult(new PagedResult<VesselResponse>([], 1, 20, 0));
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
}
