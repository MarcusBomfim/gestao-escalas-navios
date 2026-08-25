using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure;
using PortManagement.Infrastructure.Persistence;

namespace PortManagement.Infrastructure.Resilience;

internal static class NpgsqlResilienceExtensions
{
    public static NpgsqlDbContextOptionsBuilder ConfigurePortManagementDatabase(
        this NpgsqlDbContextOptionsBuilder options,
        DatabaseResilienceOptions resilienceOptions)
    {
        ArgumentNullException.ThrowIfNull(resilienceOptions);
        resilienceOptions.Validate();

        options.MigrationsHistoryTable(
            "__ef_migrations_history",
            PortManagementDbContext.Schema);
        options.CommandTimeout(resilienceOptions.CommandTimeoutSeconds);

        if (resilienceOptions.MaxRetryCount > 0)
        {
            options.EnableRetryOnFailure(
                resilienceOptions.MaxRetryCount,
                TimeSpan.FromSeconds(resilienceOptions.MaxRetryDelaySeconds),
                errorCodesToAdd: null);
        }

        return options;
    }
}
