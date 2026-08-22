using PortManagement.Domain.Operations;
using PortManagement.Domain.PortCalls;

namespace PortManagement.Application.Operations;

public sealed record OperationalMilestoneRule(
    PortCallStatus CurrentStatus,
    PortCallStatus TargetStatus,
    PortCallEventPhase Phase,
    PortCallEventAction Action);

public static class OperationalMilestoneRules
{
    private static readonly Dictionary<OperationalMilestone, OperationalMilestoneRule> Rules =
        new Dictionary<OperationalMilestone, OperationalMilestoneRule>
        {
            [OperationalMilestone.ArrivedAtAnchorage] = new(
                PortCallStatus.Planned, PortCallStatus.AtAnchorage,
                PortCallEventPhase.Anchorage, PortCallEventAction.Arrival),
            [OperationalMilestone.PilotageStarted] = new(
                PortCallStatus.AtAnchorage, PortCallStatus.ClearedForBerthing,
                PortCallEventPhase.Pilotage, PortCallEventAction.Start),
            [OperationalMilestone.BerthingCompleted] = new(
                PortCallStatus.ClearedForBerthing, PortCallStatus.Berthed,
                PortCallEventPhase.Berth, PortCallEventAction.Completion),
            [OperationalMilestone.CargoOperationStarted] = new(
                PortCallStatus.Berthed, PortCallStatus.InOperation,
                PortCallEventPhase.CargoOperation, PortCallEventAction.Start),
            [OperationalMilestone.CargoOperationCompleted] = new(
                PortCallStatus.InOperation, PortCallStatus.OperationCompleted,
                PortCallEventPhase.CargoOperation, PortCallEventAction.Completion),
            [OperationalMilestone.UnberthingCompleted] = new(
                PortCallStatus.OperationCompleted, PortCallStatus.Unberthed,
                PortCallEventPhase.Departure, PortCallEventAction.Start),
            [OperationalMilestone.Departed] = new(
                PortCallStatus.Unberthed, PortCallStatus.Closed,
                PortCallEventPhase.Departure, PortCallEventAction.Departure)
        };

    public static OperationalMilestoneRule Get(OperationalMilestone milestone) => Rules[milestone];

    public static OperationalMilestone? NextFor(PortCallStatus status)
    {
        foreach (var pair in Rules)
        {
            if (pair.Value.CurrentStatus == status)
            {
                return pair.Key;
            }
        }

        return null;
    }
}
