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

    private sealed class VesselRepositoryFake : IVesselRepository
    {
        public bool ImoExists { get; init; }

        public bool ListWasCalled { get; private set; }

        public Vessel? AddedVessel { get; private set; }

        public Task<bool> ActiveImoExistsAsync(
            ImoNumber imoNumber,
            CancellationToken cancellationToken) =>
            Task.FromResult(ImoExists);

        public Task AddAsync(Vessel vessel, CancellationToken cancellationToken)
        {
            AddedVessel = vessel;
            return Task.CompletedTask;
        }

        public Task<Vessel?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult<Vessel?>(null);

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
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(1);
    }
}
