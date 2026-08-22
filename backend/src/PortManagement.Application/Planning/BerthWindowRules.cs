using PortManagement.Application.Common;
using PortManagement.Domain.Planning;
using PortManagement.Domain.PortCalls;
using PortManagement.Domain.Ports;
using PortManagement.Domain.Vessels;

namespace PortManagement.Application.Planning;

internal static class BerthWindowRules
{
    public static ApplicationError? ValidatePlanningContext(
        PortCall portCall,
        BerthPlanningReference berthReference,
        Vessel vessel)
    {
        var berth = berthReference.Berth;
        if (portCall.Status is not (PortCallStatus.UnderReview or PortCallStatus.Planned))
        {
            return ApplicationErrors.Validation(
                "planning.invalid_port_call_status",
                "A escala precisa estar em análise ou planejada para receber uma janela de berço.");
        }

        if (berthReference.PortId != portCall.PortId)
        {
            return ApplicationErrors.Validation(
                "planning.berth_from_another_port",
                "O berço selecionado não pertence ao porto da escala.");
        }

        if (!berth.CanReceive(vessel))
        {
            return ApplicationErrors.Validation(
                "planning.incompatible_berth",
                "O berço não é compatível com o tipo ou com as dimensões do navio.");
        }

        return null;
    }

    public static ApplicationError VersionConflict() => ApplicationErrors.Conflict(
        "planning.version_conflict",
        "O planejamento foi alterado por outra operação. Atualize os dados antes de tentar novamente.");

    public static ApplicationError OverlapConflict() => ApplicationErrors.Conflict(
        "planning.berth_window_overlap",
        "O berço já possui uma janela confirmada que se sobrepõe ao período informado.");

    public static ApplicationError ActiveWindowConflict() => ApplicationErrors.Conflict(
        "planning.active_window_already_exists",
        "A escala já possui uma janela de berço ativa.");
}
