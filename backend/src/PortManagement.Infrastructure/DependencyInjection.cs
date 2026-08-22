using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PortManagement.Application.Common;
using PortManagement.Application.Planning;
using PortManagement.Application.Operations;
using PortManagement.Application.PortCalls;
using PortManagement.Application.ReferenceData;
using PortManagement.Application.Security;
using PortManagement.Application.Vessels;
using PortManagement.Infrastructure.Identity;
using PortManagement.Infrastructure.Persistence;
using PortManagement.Infrastructure.Persistence.Repositories;
using PortManagement.Infrastructure.Security;

namespace PortManagement.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string connectionString,
        JwtOptions jwtOptions)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        ArgumentNullException.ThrowIfNull(jwtOptions);

        services.AddDbContext<PortManagementDbContext>(options =>
            options
                .UseNpgsql(connectionString, npgsql =>
                    npgsql.MigrationsHistoryTable("__ef_migrations_history", PortManagementDbContext.Schema))
                .UseSnakeCaseNamingConvention());

        services
            .AddIdentityCore<ApplicationUser>(options =>
            {
                options.Password.RequiredLength = 12;
                options.Password.RequiredUniqueChars = 4;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                options.User.RequireUniqueEmail = true;
            })
            .AddRoles<ApplicationRole>()
            .AddEntityFrameworkStores<PortManagementDbContext>();

        services.AddScoped<IUnitOfWork, EfUnitOfWork>();
        services.AddScoped<IVesselRepository, VesselRepository>();
        services.AddScoped<IPortCallRepository, PortCallRepository>();
        services.AddScoped<IPortStructureRepository, PortStructureRepository>();
        services.AddScoped<IBerthWindowRepository, BerthWindowRepository>();
        services.AddScoped<IOperationalExecutionRepository, OperationalExecutionRepository>();
        services.AddScoped<IIdentityService, IdentityService>();
        services.AddScoped<DemoDataSeeder>();
        services.AddSingleton(jwtOptions);
        services.AddSingleton(TimeProvider.System);

        return services;
    }
}
