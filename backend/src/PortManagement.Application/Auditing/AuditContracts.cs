using PortManagement.Application.Common;
using PortManagement.Domain.Auditing;

namespace PortManagement.Application.Auditing;

public interface IAuditRequestContext
{
    Guid? UserId { get; }

    string UserDisplayName { get; }

    string HttpMethod { get; }

    string RequestPath { get; }

    string CorrelationId { get; }
}

public sealed record AuditLogQuery(
    int Page,
    int PageSize,
    AuditAction? Action,
    string? EntityType,
    DateTimeOffset? FromUtc,
    DateTimeOffset? ToUtc);

public sealed record AuditLogResponse(
    Guid Id,
    Guid UserId,
    string UserDisplayName,
    AuditAction Action,
    string EntityType,
    string EntityId,
    IReadOnlyCollection<string> ChangedFields,
    string HttpMethod,
    string RequestPath,
    string CorrelationId,
    DateTimeOffset OccurredAtUtc);

public interface IAuditLogRepository
{
    Task<PagedResult<AuditLogResponse>> ListAsync(
        AuditLogQuery query,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<AuditLogResponse>> ExportAsync(
        AuditLogQuery query,
        int maximumRows,
        CancellationToken cancellationToken);
}
