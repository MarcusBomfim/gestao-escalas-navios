using System.Text;
using PortManagement.Api.Common;
using PortManagement.Application.Auditing;
using PortManagement.Application.ControlTower;
using PortManagement.Application.Security;
using PortManagement.Domain.Auditing;

namespace PortManagement.Api.Endpoints.Auditing;

internal static class AuditEndpoints
{
    public static IEndpointRouteBuilder MapAuditEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/v1/audit")
            .WithTags("Audit")
            .RequireAuthorization(AuthorizationPolicies.ViewAuditReports);

        group.MapGet(
                "/",
                async (
                    int? page,
                    int? pageSize,
                    AuditAction? action,
                    string? entityType,
                    DateTimeOffset? fromUtc,
                    DateTimeOffset? toUtc,
                    GetAuditLogHandler handler,
                    CancellationToken cancellationToken) =>
                {
                    var result = await handler.HandleAsync(
                        CreateQuery(page, pageSize, action, entityType, fromUtc, toUtc),
                        cancellationToken);
                    return result.ToHttpResult(Results.Ok);
                })
            .WithName("GetAuditLog")
            .WithSummary("Consulta a trilha de auditoria com filtros e paginação");

        group.MapGet(
                "/export",
                async (
                    AuditAction? action,
                    string? entityType,
                    DateTimeOffset? fromUtc,
                    DateTimeOffset? toUtc,
                    ExportAuditLogHandler handler,
                    CancellationToken cancellationToken) =>
                {
                    var result = await handler.HandleAsync(
                        CreateQuery(1, 100, action, entityType, fromUtc, toUtc),
                        cancellationToken);
                    return result.ToHttpResult(csv => CsvFile(csv, $"auditoria-{DateTime.UtcNow:yyyyMMdd-HHmm}.csv"));
                })
            .WithName("ExportAuditLog")
            .WithSummary("Exporta até dez mil registros de auditoria em CSV seguro para planilhas");

        endpoints.MapGet(
                "/api/v1/reports/operations/export",
                async (GetControlTowerHandler handler, CancellationToken cancellationToken) =>
                {
                    var result = await handler.HandleAsync(cancellationToken);
                    return result.ToHttpResult(tower => CsvFile(
                        CsvReportBuilder.BuildOperationalReport(tower),
                        $"relatorio-operacional-{DateTime.UtcNow:yyyyMMdd-HHmm}.csv"));
                })
            .WithTags("Reports")
            .WithName("ExportOperationalReport")
            .WithSummary("Exporta o retrato atual da operação em CSV")
            .RequireAuthorization(AuthorizationPolicies.ViewAuditReports);

        return endpoints;
    }

    private static AuditLogQuery CreateQuery(
        int? page,
        int? pageSize,
        AuditAction? action,
        string? entityType,
        DateTimeOffset? fromUtc,
        DateTimeOffset? toUtc) =>
        new(page ?? 1, pageSize ?? 20, action, entityType, fromUtc, toUtc);

    private static IResult CsvFile(string csv, string fileName) =>
        Results.File(
            Encoding.UTF8.GetBytes($"\uFEFF{csv}"),
            "text/csv; charset=utf-8",
            fileName);
}
