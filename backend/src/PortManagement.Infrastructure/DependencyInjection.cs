using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PortManagement.Infrastructure.Persistence;

namespace PortManagement.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        services.AddDbContext<PortManagementDbContext>(options =>
            options
                .UseNpgsql(connectionString, npgsql =>
                    npgsql.MigrationsHistoryTable("__ef_migrations_history", PortManagementDbContext.Schema))
                .UseSnakeCaseNamingConvention());

        return services;
    }
}
