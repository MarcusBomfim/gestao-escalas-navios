using PortManagement.Application.Common;
using PortManagement.Application.ControlTower;

namespace PortManagement.Application.Notifications;

public sealed class GetNotificationCenterHandler(
    GetControlTowerHandler controlTower,
    INotificationReceiptRepository receipts)
{
    public async Task<Result<NotificationCenterResponse>> HandleAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var towerResult = await controlTower.HandleAsync(cancellationToken);
        var tower = towerResult.Value!;
        var alertIds = tower.Alerts.Select(alert => alert.Id).ToArray();
        var readReceipts = await receipts.ListAsync(userId, alertIds, cancellationToken);
        var readByAlert = readReceipts.ToDictionary(receipt => receipt.AlertId, StringComparer.Ordinal);
        var items = tower.Alerts.Select(alert =>
        {
            readByAlert.TryGetValue(alert.Id, out var receipt);
            return new NotificationItemResponse(
                alert.Id,
                alert.PortCallId,
                alert.PortCallPublicCode,
                alert.VesselName,
                alert.Severity,
                alert.Type,
                alert.Title,
                alert.Description,
                alert.DeviationMinutes,
                alert.DetectedAtUtc,
                alert.ActionPath,
                receipt is not null,
                receipt?.ReadAtUtc);
        }).ToArray();

        return Result.Success(new NotificationCenterResponse(
            tower.GeneratedAtUtc,
            items.Count(item => !item.IsRead),
            items));
    }
}

public sealed class MarkNotificationReadHandler(
    GetControlTowerHandler controlTower,
    GetNotificationCenterHandler notificationCenter,
    INotificationReceiptRepository receipts,
    TimeProvider timeProvider)
{
    public async Task<Result<NotificationCenterResponse>> HandleAsync(
        Guid userId,
        string alertId,
        CancellationToken cancellationToken)
    {
        var normalizedId = alertId.Trim();
        var tower = (await controlTower.HandleAsync(cancellationToken)).Value!;
        if (!tower.Alerts.Any(alert => string.Equals(alert.Id, normalizedId, StringComparison.Ordinal)))
        {
            return Result.Failure<NotificationCenterResponse>(ApplicationErrors.NotFound(
                "notifications.alert_not_found",
                "O alerta não está mais ativo."));
        }

        await receipts.MarkReadAsync(userId, [normalizedId], timeProvider.GetUtcNow(), cancellationToken);
        return await notificationCenter.HandleAsync(userId, cancellationToken);
    }
}

public sealed class MarkAllNotificationsReadHandler(
    GetControlTowerHandler controlTower,
    GetNotificationCenterHandler notificationCenter,
    INotificationReceiptRepository receipts,
    TimeProvider timeProvider)
{
    public async Task<Result<NotificationCenterResponse>> HandleAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var tower = (await controlTower.HandleAsync(cancellationToken)).Value!;
        var alertIds = tower.Alerts.Select(alert => alert.Id).ToArray();
        if (alertIds.Length > 0)
        {
            await receipts.MarkReadAsync(userId, alertIds, timeProvider.GetUtcNow(), cancellationToken);
        }

        return await notificationCenter.HandleAsync(userId, cancellationToken);
    }
}
