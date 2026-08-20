namespace PortManagement.Domain.PortCalls;

public enum PortCallEventPhase
{
    Anchorage,
    Pilotage,
    Berth,
    CargoOperation,
    Departure
}

public enum PortCallEventAction
{
    Arrival,
    Start,
    Completion,
    Departure
}

public enum TemporalClassifier
{
    Estimated,
    Requested,
    Planned,
    Actual
}
