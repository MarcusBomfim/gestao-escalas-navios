using PortManagement.Application.Operations;
using PortManagement.Domain.Planning;
using PortManagement.Domain.PortCalls;

namespace PortManagement.Application.ControlTower;

public static class ControlTowerEvaluator
{
    private static readonly HashSet<PortCallStatus> BerthOccupyingStatuses =
    [
        PortCallStatus.Berthed,
        PortCallStatus.InOperation,
        PortCallStatus.OperationCompleted
    ];

    private static readonly HashSet<PortCallStatus> OperationalStatuses =
    [
        PortCallStatus.AtAnchorage,
        PortCallStatus.ClearedForBerthing,
        PortCallStatus.Berthed,
        PortCallStatus.InOperation,
        PortCallStatus.OperationCompleted,
        PortCallStatus.Unberthed
    ];

    private static readonly HashSet<OperationalAlertType> ScheduleAlertTypes =
    [
        OperationalAlertType.PendingBerthConfirmation,
        OperationalAlertType.ArrivalDelay,
        OperationalAlertType.BerthOverstay,
        OperationalAlertType.CargoDelay,
        OperationalAlertType.ScheduleDeviation
    ];

    public static ControlTowerResponse Evaluate(ControlTowerSnapshot snapshot, DateTimeOffset nowUtc)
    {
        var now = nowUtc.ToUniversalTime();
        var alerts = snapshot.Calls
            .SelectMany(call => EvaluateCall(call, now))
            .OrderByDescending(alert => alert.Severity)
            .ThenByDescending(alert => alert.DeviationMinutes)
            .ThenBy(alert => alert.VesselName)
            .ToArray();

        var calls = snapshot.Calls
            .Select(call =>
            {
                var callAlerts = alerts.Where(alert => alert.PortCallId == call.PortCallId).ToArray();
                return new ControlTowerCallResponse(
                    call.PortCallId,
                    call.PublicCode,
                    call.VesselName,
                    call.Status,
                    call.PortName,
                    call.TerminalName,
                    call.BerthName,
                    call.WindowStartsAtUtc,
                    call.WindowEndsAtUtc,
                    call.LastActivityAtUtc,
                    OperationalMilestoneRules.NextFor(call.Status),
                    callAlerts.Length,
                    callAlerts.Length == 0 ? null : callAlerts.Max(alert => alert.Severity));
            })
            .OrderByDescending(call => call.HighestAlertSeverity)
            .ThenBy(call => call.WindowStartsAtUtc ?? DateTimeOffset.MaxValue)
            .ThenBy(call => call.VesselName)
            .ToArray();

        var occupiedBerths = snapshot.Calls.Count(call => BerthOccupyingStatuses.Contains(call.Status));
        var plannedCalls = snapshot.Calls.Where(call => call.WindowStartsAtUtc.HasValue).ToArray();
        var nonCompliantCallIds = alerts
            .Where(alert => ScheduleAlertTypes.Contains(alert.Type))
            .Select(alert => alert.PortCallId)
            .ToHashSet();
        var compliantCalls = plannedCalls.Count(call => !nonCompliantCallIds.Contains(call.PortCallId));

        return new ControlTowerResponse(
            now,
            new ControlTowerSummaryResponse(
                snapshot.Calls.Count,
                snapshot.Calls.Count(call => call.Status == PortCallStatus.InOperation),
                alerts.Select(alert => alert.PortCallId).Distinct().Count(),
                alerts.Count(alert => alert.Severity == OperationalAlertSeverity.Critical),
                occupiedBerths,
                snapshot.TotalBerths,
                Percentage(occupiedBerths, snapshot.TotalBerths),
                Percentage(compliantCalls, plannedCalls.Length)),
            alerts,
            calls,
            VesselTrafficSimulator.Evaluate(snapshot, now));
    }

    public static IReadOnlyCollection<OperationalAlertResponse> EvaluateCall(
        ControlTowerCallSnapshot call,
        DateTimeOffset nowUtc)
    {
        var alerts = new List<OperationalAlertResponse>();

        if (call.Status is PortCallStatus.UnderReview or PortCallStatus.Planned
            && !call.WindowStartsAtUtc.HasValue)
        {
            alerts.Add(CreateAlert(
                call,
                OperationalAlertSeverity.Warning,
                OperationalAlertType.MissingBerthPlan,
                "Escala sem janela de berço",
                "A escala está pronta para planejamento, mas ainda não possui uma janela ativa.",
                null,
                nowUtc));
        }

        if (call.WindowStatus == BerthWindowStatus.Requested
            && call.WindowStartsAtUtc.HasValue
            && nowUtc >= call.WindowStartsAtUtc.Value.AddHours(-2))
        {
            var minutes = MinutesBetween(call.WindowStartsAtUtc.Value, nowUtc);
            alerts.Add(CreateAlert(
                call,
                minutes > 0 ? OperationalAlertSeverity.Critical : OperationalAlertSeverity.Warning,
                OperationalAlertType.PendingBerthConfirmation,
                "Janela aguardando confirmação",
                minutes > 0
                    ? "O início solicitado já passou e a janela ainda não foi confirmada."
                    : "A janela começa em menos de duas horas e ainda aguarda confirmação.",
                Math.Max(minutes, 0),
                nowUtc));
        }

        if (call.Status == PortCallStatus.Planned
            && call.WindowStatus == BerthWindowStatus.Confirmed
            && call.WindowStartsAtUtc.HasValue
            && !call.ArrivedAtAnchorageUtc.HasValue
            && nowUtc > call.WindowStartsAtUtc.Value.AddHours(1))
        {
            var minutes = MinutesBetween(call.WindowStartsAtUtc.Value, nowUtc);
            alerts.Add(CreateAlert(
                call,
                minutes >= 180 ? OperationalAlertSeverity.Critical : OperationalAlertSeverity.Warning,
                OperationalAlertType.ArrivalDelay,
                "Chegada ainda não registrada",
                "A janela confirmada começou, mas o navio ainda não chegou ao fundeadouro.",
                minutes,
                nowUtc));
        }

        if (BerthOccupyingStatuses.Contains(call.Status)
            && call.WindowEndsAtUtc.HasValue
            && !call.UnberthedAtUtc.HasValue
            && nowUtc > call.WindowEndsAtUtc.Value)
        {
            var minutes = MinutesBetween(call.WindowEndsAtUtc.Value, nowUtc);
            alerts.Add(CreateAlert(
                call,
                minutes >= 120 ? OperationalAlertSeverity.Critical : OperationalAlertSeverity.Warning,
                OperationalAlertType.BerthOverstay,
                "Ocupação de berço excedida",
                "O navio permanece atracado após o término da janela confirmada.",
                minutes,
                nowUtc));
        }

        if (call.OverdueCargoOperations > 0 && call.OldestOverdueCargoEndUtc.HasValue)
        {
            var minutes = MinutesBetween(call.OldestOverdueCargoEndUtc.Value, nowUtc);
            alerts.Add(CreateAlert(
                call,
                minutes >= 120 ? OperationalAlertSeverity.Critical : OperationalAlertSeverity.Warning,
                OperationalAlertType.CargoDelay,
                "Movimentação de carga atrasada",
                $"{call.OverdueCargoOperations} movimentação(ões) ultrapassaram o término planejado.",
                minutes,
                nowUtc));
        }

        if (call.WindowStartsAtUtc.HasValue && call.BerthedAtUtc.HasValue)
        {
            var deviation = MinutesBetween(call.WindowStartsAtUtc.Value, call.BerthedAtUtc.Value);
            if (Math.Abs(deviation) >= 60)
            {
                alerts.Add(CreateAlert(
                    call,
                    Math.Abs(deviation) >= 180 ? OperationalAlertSeverity.Critical : OperationalAlertSeverity.Warning,
                    OperationalAlertType.ScheduleDeviation,
                    deviation > 0 ? "Atracação realizada com atraso" : "Atracação antecipada",
                    $"A atracação ocorreu {Math.Abs(deviation)} minutos {(deviation > 0 ? "após" : "antes")} do início planejado.",
                    deviation,
                    nowUtc));
            }
        }

        if (OperationalStatuses.Contains(call.Status)
            && call.LastActivityAtUtc.HasValue
            && nowUtc > call.LastActivityAtUtc.Value.AddHours(4))
        {
            var minutes = MinutesBetween(call.LastActivityAtUtc.Value, nowUtc);
            alerts.Add(CreateAlert(
                call,
                OperationalAlertSeverity.Warning,
                OperationalAlertType.StaleOperationalUpdate,
                "Operação sem atualização recente",
                "Nenhum evento ou avanço de carga foi registrado nas últimas quatro horas.",
                minutes,
                nowUtc));
        }

        return alerts;
    }

    private static OperationalAlertResponse CreateAlert(
        ControlTowerCallSnapshot call,
        OperationalAlertSeverity severity,
        OperationalAlertType type,
        string title,
        string description,
        int? deviationMinutes,
        DateTimeOffset detectedAtUtc) => new(
            $"{call.PortCallId:N}:{type}",
            call.PortCallId,
            call.PublicCode,
            call.VesselName,
            severity,
            type,
            title,
            description,
            deviationMinutes,
            detectedAtUtc,
            $"/escalas/{Uri.EscapeDataString(call.PublicCode)}");

    private static int MinutesBetween(DateTimeOffset start, DateTimeOffset end) =>
        (int)Math.Round((end - start).TotalMinutes, MidpointRounding.AwayFromZero);

    private static decimal Percentage(int value, int total) =>
        total == 0 ? 0 : decimal.Round(value * 100m / total, 1);
}
