using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using PortManagement.Infrastructure.Persistence;

namespace PortManagement.Api.Observability;

internal sealed class DatabaseReadinessHealthCheck(IServiceScopeFactory scopeFactory) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var database = scope.ServiceProvider.GetRequiredService<PortManagementDbContext>();
            return await database.Database.CanConnectAsync(cancellationToken)
                ? HealthCheckResult.Healthy()
                : HealthCheckResult.Unhealthy("Database connection unavailable.");
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy("Database readiness check failed.", exception);
        }
    }
}
