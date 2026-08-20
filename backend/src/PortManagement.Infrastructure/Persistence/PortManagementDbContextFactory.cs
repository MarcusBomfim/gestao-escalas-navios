using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace PortManagement.Infrastructure.Persistence;

public sealed class PortManagementDbContextFactory : IDesignTimeDbContextFactory<PortManagementDbContext>
{
    private const string DevelopmentConnection =
        "Host=localhost;Port=5432;Database=port_management;Username=port_management";

    public PortManagementDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("PORT_MANAGEMENT_DB")
            ?? DevelopmentConnection;

        var options = new DbContextOptionsBuilder<PortManagementDbContext>()
            .UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsHistoryTable("__ef_migrations_history", PortManagementDbContext.Schema))
            .UseSnakeCaseNamingConvention()
            .Options;

        return new PortManagementDbContext(options);
    }
}
