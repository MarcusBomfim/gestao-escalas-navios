using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PortManagement.Application.Auditing;
using PortManagement.Application.Common;
using PortManagement.Application.ControlTower;
using PortManagement.Application.Notifications;
using PortManagement.Application.Operations;
using PortManagement.Application.Planning;
using PortManagement.Application.PortCalls;
using PortManagement.Application.ReferenceData;
using PortManagement.Application.Security;
using PortManagement.Application.Vessels;
using PortManagement.Infrastructure.Identity;
using PortManagement.Infrastructure.Persistence;
using PortManagement.Infrastructure.Persistence.Repositories;
using PortManagement.Infrastructure.Resilience;
using PortManagement.Infrastructure.Security;

namespace PortManagement.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string connectionString,
        JwtOptions jwtOptions,
        DatabaseResilienceOptions databaseResilienceOptions,
        PasswordRecoveryOptions passwordRecoveryOptions)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        ArgumentNullException.ThrowIfNull(jwtOptions);
        ArgumentNullException.ThrowIfNull(databaseResilienceOptions);
        ArgumentNullException.ThrowIfNull(passwordRecoveryOptions);
        databaseResilienceOptions.Validate();
        passwordRecoveryOptions.Validate();

        services.AddScoped<AuditSaveChangesInterceptor>();
        services.AddDbContext<PortManagementDbContext>((provider, options) =>
            options
                .UseNpgsql(connectionString, npgsql =>
                    npgsql.ConfigurePortManagementDatabase(databaseResilienceOptions))
                .UseSnakeCaseNamingConvention()
                .AddInterceptors(provider.GetRequiredService<AuditSaveChangesInterceptor>()));

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
            .AddEntityFrameworkStores<PortManagementDbContext>()
            .AddDefaultTokenProviders();

        services.Configure<DataProtectionTokenProviderOptions>(options =>
            options.TokenLifespan = TimeSpan.FromMinutes(
                passwordRecoveryOptions.TokenLifetimeMinutes));

        services.AddScoped<IUnitOfWork, EfUnitOfWork>();
        services.AddScoped<IVesselRepository, VesselRepository>();
        services.AddScoped<IPortCallRepository, PortCallRepository>();
        services.AddScoped<IPortStructureRepository, PortStructureRepository>();
        services.AddScoped<IBerthWindowRepository, BerthWindowRepository>();
        services.AddScoped<IOperationalExecutionRepository, OperationalExecutionRepository>();
        services.AddScoped<IControlTowerRepository, ControlTowerRepository>();
        services.AddScoped<INotificationReceiptRepository, NotificationReceiptRepository>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();
        services.AddScoped<IIdentityService, IdentityService>();
        services.AddScoped<IUserAdministrationService, UserAdministrationService>();
        services.AddScoped<IPasswordResetEmailSender, SmtpPasswordResetEmailSender>();
        services.AddScoped<DemoDataSeeder>();
        services.AddSingleton(jwtOptions);
        services.AddSingleton(passwordRecoveryOptions);
        services.AddSingleton(TimeProvider.System);

        return services;
    }
}
