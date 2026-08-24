using PortManagement.Application.Common;
using PortManagement.Application.ControlTower;

namespace PortManagement.Application.Notifications;

public sealed record NotificationReceiptResponse(string AlertId, DateTimeOffset ReadAtUtc);

public sealed record NotificationItemResponse(
    string Id,
    Guid PortCallId,
    string PortCallPublicCode,
    string VesselName,
    OperationalAlertSeverity Severity,
    OperationalAlertType Type,
    string Title,
    string Description,
    int? DeviationMinutes,
    DateTimeOffset DetectedAtUtc,
    string ActionPath,
    bool IsRead,
    DateTimeOffset? ReadAtUtc);

public sealed record NotificationCenterResponse(
    DateTimeOffset GeneratedAtUtc,
    int UnreadCount,
    IReadOnlyCollection<NotificationItemResponse> Items);

public interface INotificationReceiptRepository
{
    Task<IReadOnlyCollection<NotificationReceiptResponse>> ListAsync(
        Guid userId,
        IReadOnlyCollection<string> alertIds,
        CancellationToken cancellationToken);

    Task MarkReadAsync(
        Guid userId,
        IReadOnlyCollection<string> alertIds,
        DateTimeOffset readAtUtc,
        CancellationToken cancellationToken);
}
