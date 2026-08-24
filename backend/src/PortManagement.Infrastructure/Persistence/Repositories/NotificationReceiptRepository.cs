using Microsoft.EntityFrameworkCore;
using PortManagement.Application.Notifications;
using PortManagement.Domain.Notifications;

namespace PortManagement.Infrastructure.Persistence.Repositories;

internal sealed class NotificationReceiptRepository(PortManagementDbContext database)
    : INotificationReceiptRepository
{
    public async Task<IReadOnlyCollection<NotificationReceiptResponse>> ListAsync(
        Guid userId,
        IReadOnlyCollection<string> alertIds,
        CancellationToken cancellationToken)
    {
        if (alertIds.Count == 0)
        {
            return [];
        }

        return await database.NotificationReceipts
            .AsNoTracking()
            .Where(receipt => receipt.UserId == userId && alertIds.Contains(receipt.AlertId))
            .Select(receipt => new NotificationReceiptResponse(receipt.AlertId, receipt.ReadAtUtc))
            .ToArrayAsync(cancellationToken);
    }

    public async Task MarkReadAsync(
        Guid userId,
        IReadOnlyCollection<string> alertIds,
        DateTimeOffset readAtUtc,
        CancellationToken cancellationToken)
    {
        foreach (var alertId in alertIds.Distinct(StringComparer.Ordinal))
        {
            var id = Guid.NewGuid();
            await database.Database.ExecuteSqlInterpolatedAsync(
                $"""
                INSERT INTO port_management.notification_receipts (id, user_id, alert_id, read_at_utc)
                VALUES ({id}, {userId}, {alertId}, {readAtUtc})
                ON CONFLICT (user_id, alert_id)
                DO UPDATE SET read_at_utc = EXCLUDED.read_at_utc
                """,
                cancellationToken);
        }
    }
}
