using PortManagement.Domain.Common;

namespace PortManagement.Domain.Planning;

public sealed class BerthWindowRevision : Entity
{
    private BerthWindowRevision()
    {
        ChangedBy = string.Empty;
        Reason = string.Empty;
        BerthWindow = null!;
    }

    internal BerthWindowRevision(
        Guid id,
        Guid berthWindowId,
        Guid previousBerthId,
        Guid newBerthId,
        DateTimeOffset previousStartsAtUtc,
        DateTimeOffset previousEndsAtUtc,
        DateTimeOffset newStartsAtUtc,
        DateTimeOffset newEndsAtUtc,
        string changedBy,
        string reason,
        DateTimeOffset changedAtUtc)
        : base(id)
    {
        BerthWindowId = berthWindowId;
        PreviousBerthId = previousBerthId;
        NewBerthId = newBerthId;
        PreviousStartsAtUtc = DomainRules.ToUtc(previousStartsAtUtc);
        PreviousEndsAtUtc = DomainRules.ToUtc(previousEndsAtUtc);
        NewStartsAtUtc = DomainRules.ToUtc(newStartsAtUtc);
        NewEndsAtUtc = DomainRules.ToUtc(newEndsAtUtc);
        ChangedBy = DomainRules.RequiredText(changedBy, "Responsável pela alteração", 120);
        Reason = DomainRules.RequiredText(reason, "Justificativa", 500);
        ChangedAtUtc = DomainRules.ToUtc(changedAtUtc);
    }

    public Guid BerthWindowId { get; private set; }

    public BerthWindow BerthWindow { get; private set; } = null!;

    public Guid PreviousBerthId { get; private set; }

    public Guid NewBerthId { get; private set; }

    public DateTimeOffset PreviousStartsAtUtc { get; private set; }

    public DateTimeOffset PreviousEndsAtUtc { get; private set; }

    public DateTimeOffset NewStartsAtUtc { get; private set; }

    public DateTimeOffset NewEndsAtUtc { get; private set; }

    public string ChangedBy { get; private set; }

    public string Reason { get; private set; }

    public DateTimeOffset ChangedAtUtc { get; private set; }
}
