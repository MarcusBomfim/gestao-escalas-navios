using System.Globalization;
using System.Text;
using PortManagement.Application.ControlTower;

namespace PortManagement.Application.Auditing;

public static class CsvReportBuilder
{
    public static string BuildAuditLog(IReadOnlyCollection<AuditLogResponse> rows)
    {
        var csv = new StringBuilder();
        AppendRow(csv, "Data UTC", "Usuário", "Ação", "Entidade", "Identificador", "Campos alterados", "Requisição", "Correlação");

        foreach (var row in rows)
        {
            AppendRow(
                csv,
                row.OccurredAtUtc.ToString("O", CultureInfo.InvariantCulture),
                row.UserDisplayName,
                row.Action.ToString(),
                row.EntityType,
                row.EntityId,
                string.Join(", ", row.ChangedFields),
                $"{row.HttpMethod} {row.RequestPath}",
                row.CorrelationId);
        }

        return csv.ToString();
    }

    public static string BuildOperationalReport(ControlTowerResponse tower)
    {
        var csv = new StringBuilder();
        AppendRow(csv, "Relatório operacional", tower.GeneratedAtUtc.ToString("O", CultureInfo.InvariantCulture));
        AppendRow(csv, "Escalas ativas", tower.Summary.ActivePortCalls.ToString(CultureInfo.InvariantCulture));
        AppendRow(csv, "Em operação", tower.Summary.InOperation.ToString(CultureInfo.InvariantCulture));
        AppendRow(csv, "Requerem atenção", tower.Summary.CallsRequiringAttention.ToString(CultureInfo.InvariantCulture));
        AppendRow(csv, "Alertas críticos", tower.Summary.CriticalAlerts.ToString(CultureInfo.InvariantCulture));
        AppendRow(csv, "Ocupação de berços (%)", tower.Summary.BerthOccupancyPercent.ToString(CultureInfo.InvariantCulture));
        AppendRow(csv, "Aderência à programação (%)", tower.Summary.ScheduleCompliancePercent.ToString(CultureInfo.InvariantCulture));
        csv.AppendLine();
        AppendRow(csv, "Escala", "Navio", "Status", "Porto", "Terminal", "Berço", "Alertas", "Última atividade UTC");

        foreach (var call in tower.Calls)
        {
            AppendRow(
                csv,
                call.PublicCode,
                call.VesselName,
                call.Status.ToString(),
                call.PortName,
                call.TerminalName ?? string.Empty,
                call.BerthName ?? string.Empty,
                call.AlertCount.ToString(CultureInfo.InvariantCulture),
                call.LastActivityAtUtc?.ToString("O", CultureInfo.InvariantCulture) ?? string.Empty);
        }

        return csv.ToString();
    }

    private static void AppendRow(StringBuilder csv, params string[] cells) =>
        csv.AppendLine(string.Join(';', cells.Select(SanitizeCell)));

    private static string SanitizeCell(string value)
    {
        var safeValue = value.Length > 0 && value[0] is '=' or '+' or '-' or '@' or '\t' or '\r'
            ? $"'{value}"
            : value;

        return $"\"{safeValue.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
    }
}
