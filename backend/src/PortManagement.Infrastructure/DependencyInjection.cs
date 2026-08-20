using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PortManagement.Application.Common;
using PortManagement.Application.PortCalls;
using PortManagement.Application.ReferenceData;
using PortManagement.Application.Vessels;
using PortManagement.Infrastructure.Persistence;
using PortManagement.Infrastructure.Persistence.Repositories;

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

        services.AddScoped<IUnitOfWork, EfUnitOfWork>();
        services.AddScoped<IVesselRepository, VesselRepository>();
        services.AddScoped<IPortCallRepository, PortCallRepository>();
        services.AddScoped<IPortStructureRepository, PortStructureRepository>();
        services.AddScoped<DemoDataSeeder>();

        return services;
    }
}
