using PortManagement.Domain.Common;

namespace PortManagement.Domain.PortCalls;

public sealed class PortCallEvent : Entity
{
    private PortCallEvent()
    {
        Source = string.Empty;
        RecordedBy = string.Empty;
        PortCall = null!;
    }

    public PortCallEvent(
        Guid id,
        Guid portCallId,
        PortCallEventPhase phase,
        PortCallEventAction action,
        TemporalClassifier classifier,
        DateTimeOffset occursAtUtc,
        string source,
        string recordedBy,
        DateTimeOffset recordedAtUtc,
        Guid? replacesEventId = null,
        string? correctionReason = null)
        : base(id)
    {
        if (portCallId == Guid.Empty)
        {
            throw new DomainException("A escala do evento é obrigatória.");
        }

        if (replacesEventId.HasValue && string.IsNullOrWhiteSpace(correctionReason))
        {
            throw new DomainException("A correção de um evento exige justificativa.");
        }

        PortCallId = portCallId;
        Phase = phase;
        Action = action;
        Classifier = classifier;
        OccursAtUtc = DomainRules.ToUtc(occursAtUtc);
        Source = DomainRules.RequiredText(source, "Fonte do evento", 100);
        RecordedBy = DomainRules.RequiredText(recordedBy, "Responsável pelo registro", 120);
        RecordedAtUtc = DomainRules.ToUtc(recordedAtUtc);
        ReplacesEventId = replacesEventId;
        CorrectionReason = DomainRules.OptionalText(correctionReason, "Justificativa da correção", 500);
    }

    public Guid PortCallId { get; private set; }

    public PortCall PortCall { get; private set; } = null!;

    public PortCallEventPhase Phase { get; private set; }

    public PortCallEventAction Action { get; private set; }

    public TemporalClassifier Classifier { get; private set; }

    public DateTimeOffset OccursAtUtc { get; private set; }

    public string Source { get; private set; }

    public string RecordedBy { get; private set; }

    public DateTimeOffset RecordedAtUtc { get; private set; }

    public Guid? ReplacesEventId { get; private set; }

    public PortCallEvent? ReplacesEvent { get; private set; }

    public string? CorrectionReason { get; private set; }
}
