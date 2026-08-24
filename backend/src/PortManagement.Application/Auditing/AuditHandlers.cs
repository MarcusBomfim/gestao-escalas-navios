using PortManagement.Application.Common;

namespace PortManagement.Application.Auditing;

public sealed class GetAuditLogHandler(IAuditLogRepository repository)
{
    public async Task<Result<PagedResult<AuditLogResponse>>> HandleAsync(
        AuditLogQuery query,
        CancellationToken cancellationToken)
    {
        var validation = Validate(query);
        if (validation is not null)
        {
            return Result.Failure<PagedResult<AuditLogResponse>>(validation);
        }

        return Result.Success(await repository.ListAsync(query, cancellationToken));
    }

    internal static ApplicationError? Validate(AuditLogQuery query)
    {
        if (query.Page is < 1 or > 1_000_000 || query.PageSize is < 1 or > 100)
        {
            return ApplicationErrors.Validation(
                "audit.pagination_invalid",
                "A página deve ser maior que zero e o tamanho deve estar entre 1 e 100.");
        }

        if (query.EntityType?.Length > 120)
        {
            return ApplicationErrors.Validation(
                "audit.entity_type_too_long",
                "O tipo de entidade deve possuir no máximo 120 caracteres.");
        }

        if (query.FromUtc.HasValue && query.ToUtc.HasValue && query.FromUtc > query.ToUtc)
        {
            return ApplicationErrors.Validation(
                "audit.invalid_period",
                "A data inicial não pode ser posterior à data final.");
        }

        return null;
    }
}

public sealed class ExportAuditLogHandler(IAuditLogRepository repository)
{
    private const int MaximumExportRows = 10_000;

    public async Task<Result<string>> HandleAsync(
        AuditLogQuery query,
        CancellationToken cancellationToken)
    {
        var validation = GetAuditLogHandler.Validate(query);
        if (validation is not null)
        {
            return Result.Failure<string>(validation);
        }

        var rows = await repository.ExportAsync(query, MaximumExportRows, cancellationToken);
        return Result.Success(CsvReportBuilder.BuildAuditLog(rows));
    }
}
