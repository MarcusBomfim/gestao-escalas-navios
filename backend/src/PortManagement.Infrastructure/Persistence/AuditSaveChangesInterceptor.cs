using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using PortManagement.Application.Auditing;
using PortManagement.Domain.Auditing;
using PortManagement.Domain.Common;
using PortManagement.Domain.Notifications;

namespace PortManagement.Infrastructure.Persistence;

internal sealed class AuditSaveChangesInterceptor(
    IAuditRequestContext requestContext,
    TimeProvider timeProvider)
    : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        CaptureChanges(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        CaptureChanges(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void CaptureChanges(DbContext? database)
    {
        if (database is null || requestContext.UserId is not Guid userId)
        {
            return;
        }

        database.ChangeTracker.DetectChanges();
        var records = database.ChangeTracker
            .Entries<Entity>()
            .Where(IsAuditable)
            .Select(entry => CreateRecord(entry, userId))
            .ToArray();

        if (records.Length > 0)
        {
            database.Set<AuditRecord>().AddRange(records);
        }
    }

    private static bool IsAuditable(EntityEntry<Entity> entry) =>
        entry.Entity is not AuditRecord and not NotificationReceipt
        && entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted;

    private AuditRecord CreateRecord(EntityEntry<Entity> entry, Guid userId)
    {
        var action = entry.State switch
        {
            EntityState.Added => AuditAction.Created,
            EntityState.Modified => AuditAction.Updated,
            EntityState.Deleted => AuditAction.Deleted,
            _ => throw new InvalidOperationException("Estado não auditável.")
        };
        var changedFields = entry.State == EntityState.Modified
            ? entry.Properties
                .Where(property => property.IsModified && !property.Metadata.IsShadowProperty())
                .Select(property => property.Metadata.Name)
                .ToArray()
            : [];

        return AuditRecord.Capture(
            userId,
            requestContext.UserDisplayName,
            action,
            entry.Metadata.ClrType.Name,
            entry.Entity.Id.ToString(),
            changedFields,
            requestContext.HttpMethod,
            requestContext.RequestPath,
            requestContext.CorrelationId,
            timeProvider.GetUtcNow());
    }
}
