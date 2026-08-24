using Microsoft.EntityFrameworkCore;
using PortManagement.Domain.Auditing;
using PortManagement.Domain.Planning;
using PortManagement.Domain.PortCalls;
using PortManagement.Domain.Vessels;
using PortManagement.Infrastructure.Identity;
using PortManagement.Infrastructure.Persistence;

namespace PortManagement.IntegrationTests;

public sealed class PersistenceModelTests
{
    [Fact]
    public void DomainTablesUseTheDedicatedPostgresSchema()
    {
        using var database = new PortManagementDbContextFactory().CreateDbContext([]);

        var vessel = database.Model.FindEntityType(typeof(Vessel));
        var berthWindow = database.Model.FindEntityType(typeof(BerthWindow));

        Assert.NotNull(vessel);
        Assert.NotNull(berthWindow);
        Assert.Equal(PortManagementDbContext.Schema, vessel.GetSchema());
        Assert.Equal("vessels", vessel.GetTableName());
        Assert.Equal("berth_windows", berthWindow.GetTableName());
    }

    [Fact]
    public void PortCallVersionIsAnOptimisticConcurrencyToken()
    {
        using var database = new PortManagementDbContextFactory().CreateDbContext([]);

        var portCall = database.Model.FindEntityType(typeof(PortCall));
        var version = portCall?.FindProperty(nameof(PortCall.Version));

        Assert.NotNull(version);
        Assert.True(version.IsConcurrencyToken);
    }

    [Fact]
    public void BerthWindowUsesConcurrencyAndAllowsOnlyOneActiveWindowPerPortCall()
    {
        using var database = new PortManagementDbContextFactory().CreateDbContext([]);

        var berthWindow = database.Model.FindEntityType(typeof(BerthWindow));
        var version = berthWindow?.FindProperty(nameof(BerthWindow.Version));
        var portCallId = berthWindow?.FindProperty(nameof(BerthWindow.PortCallId));

        Assert.NotNull(berthWindow);
        Assert.NotNull(version);
        Assert.True(version.IsConcurrencyToken);
        Assert.NotNull(portCallId);
        Assert.Contains(
            berthWindow.GetIndexes(),
            index => index.IsUnique
                && index.Properties.SequenceEqual([portCallId])
                && index.GetFilter() == "status IN ('Requested', 'Confirmed')");
    }

    [Fact]
    public void IdentityAndDomainDataUseSeparateSchemas()
    {
        using var database = new PortManagementDbContextFactory().CreateDbContext([]);

        var user = database.Model.FindEntityType(typeof(ApplicationUser));
        var refreshToken = database.Model.FindEntityType(typeof(RefreshToken));

        Assert.NotNull(user);
        Assert.NotNull(refreshToken);
        Assert.Equal(PortManagementDbContext.IdentitySchema, user.GetSchema());
        Assert.Equal("users", user.GetTableName());
        Assert.Equal(PortManagementDbContext.IdentitySchema, refreshToken.GetSchema());
        Assert.Equal("refresh_tokens", refreshToken.GetTableName());
    }

    [Fact]
    public void RefreshTokensPersistOnlyAUniqueHashWithConcurrencyProtection()
    {
        using var database = new PortManagementDbContextFactory().CreateDbContext([]);

        var refreshToken = database.Model.FindEntityType(typeof(RefreshToken));
        var hash = refreshToken?.FindProperty(nameof(RefreshToken.TokenHash));
        var revokedAt = refreshToken?.FindProperty(nameof(RefreshToken.RevokedAtUtc));

        Assert.NotNull(refreshToken);
        Assert.NotNull(hash);
        Assert.Equal(64, hash.GetMaxLength());
        Assert.DoesNotContain(
            refreshToken.GetProperties(),
            property => property.Name.Equals("Token", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(
            refreshToken.GetIndexes(),
            index => index.IsUnique && index.Properties.SequenceEqual([hash]));
        Assert.NotNull(revokedAt);
        Assert.True(revokedAt.IsConcurrencyToken);
    }

    [Fact]
    public void AuditRecordsAreIndexedAndDoNotPersistChangedValues()
    {
        using var database = new PortManagementDbContextFactory().CreateDbContext([]);

        var auditRecord = database.Model.FindEntityType(typeof(AuditRecord));
        var occurredAt = auditRecord?.FindProperty(nameof(AuditRecord.OccurredAtUtc));

        Assert.NotNull(auditRecord);
        Assert.Equal(PortManagementDbContext.Schema, auditRecord.GetSchema());
        Assert.Equal("audit_records", auditRecord.GetTableName());
        Assert.NotNull(occurredAt);
        Assert.Contains(
            auditRecord.GetIndexes(),
            index => index.Properties.Contains(occurredAt));
        Assert.DoesNotContain(
            auditRecord.GetProperties(),
            property => property.Name.Contains("Value", StringComparison.OrdinalIgnoreCase));
    }
}
