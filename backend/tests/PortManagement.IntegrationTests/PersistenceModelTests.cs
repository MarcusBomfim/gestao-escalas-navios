using Microsoft.EntityFrameworkCore;
using PortManagement.Domain.Planning;
using PortManagement.Domain.PortCalls;
using PortManagement.Domain.Vessels;
using PortManagement.Infrastructure.Persistence;

namespace PortManagement.IntegrationTests;

public sealed class PersistenceModelTests
{
    [Fact]
    public void DomainTablesUseTheDedicatedPostgresSchema()
    {
        using var database = new PortManagementDbContextFactory().CreateDbContext([]);

        var vessel = database.Model.FindEntityType(typeof(Vessel));
        var berthWindow = database.Model.FindEntityType(typeof(BerthWindow));

        Assert.NotNull(vessel);
        Assert.NotNull(berthWindow);
        Assert.Equal(PortManagementDbContext.Schema, vessel.GetSchema());
        Assert.Equal("vessels", vessel.GetTableName());
        Assert.Equal("berth_windows", berthWindow.GetTableName());
    }

    [Fact]
    public void PortCallVersionIsAnOptimisticConcurrencyToken()
    {
        using var database = new PortManagementDbContextFactory().CreateDbContext([]);

        var portCall = database.Model.FindEntityType(typeof(PortCall));
        var version = portCall?.FindProperty(nameof(PortCall.Version));

        Assert.NotNull(version);
        Assert.True(version.IsConcurrencyToken);
    }
}
