using PortManagement.Domain.Common;

namespace PortManagement.Domain.PortCalls;

public sealed class PortCallStatusHistory : Entity
{
    private PortCallStatusHistory()
    {
        ChangedBy = string.Empty;
        PortCall = null!;
    }

    internal PortCallStatusHistory(
        Guid id,
        Guid portCallId,
        PortCallStatus previousStatus,
        PortCallStatus newStatus,
        string changedBy,
        DateTimeOffset changedAtUtc,
        string? reason)
        : base(id)
    {
        PortCallId = portCallId;
        PreviousStatus = previousStatus;
        NewStatus = newStatus;
        ChangedBy = DomainRules.RequiredText(changedBy, "Responsável pela alteração", 120);
        ChangedAtUtc = DomainRules.ToUtc(changedAtUtc);
        Reason = reason;
    }

    public Guid PortCallId { get; private set; }

    public PortCall PortCall { get; private set; } = null!;

    public PortCallStatus PreviousStatus { get; private set; }

    public PortCallStatus NewStatus { get; private set; }

    public string ChangedBy { get; private set; }

    public DateTimeOffset ChangedAtUtc { get; private set; }

    public string? Reason { get; private set; }
}
