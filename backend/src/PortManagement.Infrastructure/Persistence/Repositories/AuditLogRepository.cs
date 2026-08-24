using Microsoft.EntityFrameworkCore;
using PortManagement.Application.Auditing;
using PortManagement.Application.Common;
using PortManagement.Domain.Auditing;

namespace PortManagement.Infrastructure.Persistence.Repositories;

internal sealed class AuditLogRepository(PortManagementDbContext database) : IAuditLogRepository
{
    public async Task<PagedResult<AuditLogResponse>> ListAsync(
        AuditLogQuery query,
        CancellationToken cancellationToken)
    {
        var records = ApplyFilters(database.AuditRecords.AsNoTracking(), query);
        var totalItems = await records.CountAsync(cancellationToken);
        var page = await Order(records)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToArrayAsync(cancellationToken);

        return new PagedResult<AuditLogResponse>(
            page.Select(ToResponse).ToArray(),
            query.Page,
            query.PageSize,
            totalItems);
    }

    public async Task<IReadOnlyCollection<AuditLogResponse>> ExportAsync(
        AuditLogQuery query,
        int maximumRows,
        CancellationToken cancellationToken) =>
        (await Order(ApplyFilters(database.AuditRecords.AsNoTracking(), query))
                .Take(maximumRows)
                .ToArrayAsync(cancellationToken))
            .Select(ToResponse)
            .ToArray();

    private static IQueryable<AuditRecord> ApplyFilters(
        IQueryable<AuditRecord> records,
        AuditLogQuery query)
    {
        if (query.Action.HasValue)
        {
            records = records.Where(record => record.Action == query.Action);
        }

        if (!string.IsNullOrWhiteSpace(query.EntityType))
        {
            var entityType = query.EntityType.Trim();
            records = records.Where(record => record.EntityType == entityType);
        }

        if (query.FromUtc.HasValue)
        {
            records = records.Where(record => record.OccurredAtUtc >= query.FromUtc.Value);
        }

        if (query.ToUtc.HasValue)
        {
            records = records.Where(record => record.OccurredAtUtc <= query.ToUtc.Value);
        }

        return records;
    }

    private static IOrderedQueryable<AuditRecord> Order(IQueryable<AuditRecord> records) =>
        records
            .OrderByDescending(record => record.OccurredAtUtc)
            .ThenByDescending(record => record.Id);

    private static AuditLogResponse ToResponse(AuditRecord record) => new(
        record.Id,
        record.UserId,
        record.UserDisplayName,
        record.Action,
        record.EntityType,
        record.EntityId,
        record.ChangedFields?.Split(',', StringSplitOptions.RemoveEmptyEntries) ?? [],
        record.HttpMethod,
        record.RequestPath,
        record.CorrelationId,
        record.OccurredAtUtc);
}
