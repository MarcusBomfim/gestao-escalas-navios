using Microsoft.EntityFrameworkCore;
using PortManagement.Application.Security;
using PortManagement.Infrastructure.Persistence;
using PortManagement.Infrastructure.Persistence.Repositories;

namespace PortManagement.IntegrationTests;

public sealed class OrganizationScopeQueryTests
{
    [Fact]
    public void PortCallQueryFiltersBothParticipatingOrganizationColumns()
    {
        using var database = new PortManagementDbContextFactory().CreateDbContext([]);
        var organizationId = Guid.Parse("20000000-0000-0000-0000-000000000002");

        var sql = database.PortCalls
            .ApplyOrganizationScope(new DataScopeFake(organizationId, false))
            .ToQueryString();

        Assert.Contains("WHERE", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("agent_organization_id", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("shipping_line_organization_id", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(organizationId.ToString(), sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void MissingOrganizationProducesDenyByDefaultQuery()
    {
        using var database = new PortManagementDbContextFactory().CreateDbContext([]);

        var sql = database.PortCalls
            .ApplyOrganizationScope(new DataScopeFake(null, false))
            .ToQueryString();

        Assert.Contains("WHERE FALSE", sql, StringComparison.OrdinalIgnoreCase);
    }

    private sealed record DataScopeFake(
        Guid? OrganizationId,
        bool HasGlobalAccess) : IUserDataScope;
}
