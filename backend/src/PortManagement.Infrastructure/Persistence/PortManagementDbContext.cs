using Microsoft.EntityFrameworkCore;
using PortManagement.Domain.Operations;
using PortManagement.Domain.Organizations;
using PortManagement.Domain.Planning;
using PortManagement.Domain.PortCalls;
using PortManagement.Domain.Ports;
using PortManagement.Domain.Vessels;

namespace PortManagement.Infrastructure.Persistence;

public sealed class PortManagementDbContext(DbContextOptions<PortManagementDbContext> options)
    : DbContext(options)
{
    public const string Schema = "port_management";

    public DbSet<Vessel> Vessels => Set<Vessel>();

    public DbSet<Organization> Organizations => Set<Organization>();

    public DbSet<Port> Ports => Set<Port>();

    public DbSet<Terminal> Terminals => Set<Terminal>();

    public DbSet<Berth> Berths => Set<Berth>();

    public DbSet<PortCall> PortCalls => Set<PortCall>();

    public DbSet<PortCallStatusHistory> PortCallStatusHistory => Set<PortCallStatusHistory>();

    public DbSet<PortCallEvent> PortCallEvents => Set<PortCallEvent>();

    public DbSet<BerthWindow> BerthWindows => Set<BerthWindow>();

    public DbSet<BerthWindowRevision> BerthWindowRevisions => Set<BerthWindowRevision>();

    public DbSet<CargoOperation> CargoOperations => Set<CargoOperation>();

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        IncrementPortCallVersions();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        IncrementPortCallVersions();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PortManagementDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }

    private void IncrementPortCallVersions()
    {
        foreach (var entry in ChangeTracker.Entries<PortCall>().Where(entry => entry.State == EntityState.Modified))
        {
            entry.Property(portCall => portCall.Version).CurrentValue++;
        }
    }
}
