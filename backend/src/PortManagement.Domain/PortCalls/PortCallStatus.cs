namespace PortManagement.Domain.PortCalls;

public enum PortCallStatus
{
    Draft,
    Requested,
    UnderReview,
    Planned,
    AtAnchorage,
    ClearedForBerthing,
    Berthed,
    InOperation,
    OperationCompleted,
    Unberthed,
    Closed,
    Cancelled
}
