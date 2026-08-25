using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using PortManagement.Infrastructure.Resilience;

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
                npgsql.ConfigurePortManagementDatabase(new DatabaseResilienceOptions()))
            .UseSnakeCaseNamingConvention()
            .Options;

        return new PortManagementDbContext(options);
    }
}
