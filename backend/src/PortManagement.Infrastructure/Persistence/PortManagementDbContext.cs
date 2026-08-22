using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PortManagement.Domain.Operations;
using PortManagement.Domain.Organizations;
using PortManagement.Domain.Planning;
using PortManagement.Domain.PortCalls;
using PortManagement.Domain.Ports;
using PortManagement.Domain.Vessels;
using PortManagement.Infrastructure.Identity;

namespace PortManagement.Infrastructure.Persistence;

public sealed class PortManagementDbContext(DbContextOptions<PortManagementDbContext> options)
    : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>(options)
{
    public const string Schema = "port_management";
    public const string IdentitySchema = "identity";

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

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        IncrementPortCallVersions();
        IncrementBerthWindowVersions();
        IncrementCargoOperationVersions();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        IncrementPortCallVersions();
        IncrementBerthWindowVersions();
        IncrementCargoOperationVersions();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.HasDefaultSchema(Schema);
        ConfigureIdentityTables(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(PortManagementDbContext).Assembly);
    }

    private static void ConfigureIdentityTables(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ApplicationUser>(user =>
        {
            user.ToTable("users", IdentitySchema);
            user.Property(applicationUser => applicationUser.DisplayName)
                .HasMaxLength(160)
                .IsRequired();
            user.HasIndex(applicationUser => applicationUser.NormalizedEmail)
                .HasDatabaseName("EmailIndex")
                .IsUnique();
            user.HasOne<Organization>()
                .WithMany()
                .HasForeignKey(applicationUser => applicationUser.OrganizationId)
                .OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<ApplicationRole>().ToTable("roles", IdentitySchema);
        modelBuilder.Entity<IdentityUserRole<Guid>>().ToTable("user_roles", IdentitySchema);
        modelBuilder.Entity<IdentityUserClaim<Guid>>().ToTable("user_claims", IdentitySchema);
        modelBuilder.Entity<IdentityUserLogin<Guid>>().ToTable("user_logins", IdentitySchema);
        modelBuilder.Entity<IdentityRoleClaim<Guid>>().ToTable("role_claims", IdentitySchema);
        modelBuilder.Entity<IdentityUserToken<Guid>>().ToTable("user_tokens", IdentitySchema);
    }

    private void IncrementPortCallVersions()
    {
        foreach (var entry in ChangeTracker.Entries<PortCall>().Where(entry => entry.State == EntityState.Modified))
        {
            entry.Property(portCall => portCall.Version).CurrentValue++;
        }
    }

    private void IncrementBerthWindowVersions()
    {
        foreach (var entry in ChangeTracker.Entries<BerthWindow>().Where(entry => entry.State == EntityState.Modified))
        {
            entry.Property(window => window.Version).CurrentValue++;
        }
    }

    private void IncrementCargoOperationVersions()
    {
        foreach (var entry in ChangeTracker.Entries<CargoOperation>().Where(entry => entry.State == EntityState.Modified))
        {
            entry.Property(operation => operation.Version).CurrentValue++;
        }
    }
}
