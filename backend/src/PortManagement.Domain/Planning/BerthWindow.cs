using PortManagement.Domain.Common;
using PortManagement.Domain.PortCalls;
using PortManagement.Domain.Ports;

namespace PortManagement.Domain.Planning;

public sealed class BerthWindow : AuditableEntity
{
    private readonly List<BerthWindowRevision> _revisions = [];

    private BerthWindow()
    {
        RequestedBy = string.Empty;
        PortCall = null!;
        Berth = null!;
    }

    public BerthWindow(
        Guid id,
        Guid portCallId,
        Guid berthId,
        DateTimeOffset startsAtUtc,
        DateTimeOffset endsAtUtc,
        string requestedBy,
        DateTimeOffset createdAtUtc)
        : base(id, createdAtUtc)
    {
        if (portCallId == Guid.Empty || berthId == Guid.Empty)
        {
            throw new DomainException("Escala e berço são obrigatórios para a janela.");
        }

        var period = NormalizePeriod(startsAtUtc, endsAtUtc);
        PortCallId = portCallId;
        BerthId = berthId;
        StartsAtUtc = period.Start;
        EndsAtUtc = period.End;
        RequestedBy = DomainRules.RequiredText(requestedBy, "Solicitante da janela", 120);
        Status = BerthWindowStatus.Requested;
    }

    public Guid PortCallId { get; private set; }

    public PortCall PortCall { get; private set; } = null!;

    public Guid BerthId { get; private set; }

    public Berth Berth { get; private set; } = null!;

    public DateTimeOffset StartsAtUtc { get; private set; }

    public DateTimeOffset EndsAtUtc { get; private set; }

    public BerthWindowStatus Status { get; private set; }

    public string RequestedBy { get; private set; }

    public string? LastChangedBy { get; private set; }

    public string? LastChangeReason { get; private set; }

    public long Version { get; private set; }

    public IReadOnlyCollection<BerthWindowRevision> Revisions => _revisions.AsReadOnly();

    public void Confirm(string changedBy, DateTimeOffset changedAtUtc)
    {
        if (Status != BerthWindowStatus.Requested)
        {
            throw new DomainException("Somente uma janela solicitada pode ser confirmada.");
        }

        Status = BerthWindowStatus.Confirmed;
        RegisterChange(changedBy, null, changedAtUtc);
    }

    public void Reprogram(
        DateTimeOffset newStartsAtUtc,
        DateTimeOffset newEndsAtUtc,
        string changedBy,
        string reason,
        DateTimeOffset changedAtUtc)
    {
        ReprogramAt(
            BerthId,
            newStartsAtUtc,
            newEndsAtUtc,
            changedBy,
            reason,
            changedAtUtc);
    }

    public void ReprogramAt(
        Guid newBerthId,
        DateTimeOffset newStartsAtUtc,
        DateTimeOffset newEndsAtUtc,
        string changedBy,
        string reason,
        DateTimeOffset changedAtUtc)
    {
        if (Status is BerthWindowStatus.Completed or BerthWindowStatus.Cancelled)
        {
            throw new DomainException("Uma janela concluída ou cancelada não pode ser reprogramada.");
        }

        if (newBerthId == Guid.Empty)
        {
            throw new DomainException("O novo berço da janela é obrigatório.");
        }

        var normalizedReason = DomainRules.RequiredText(reason, "Justificativa da reprogramação", 500);
        var newPeriod = NormalizePeriod(newStartsAtUtc, newEndsAtUtc);
        _revisions.Add(new BerthWindowRevision(
            Guid.NewGuid(),
            Id,
            BerthId,
            newBerthId,
            StartsAtUtc,
            EndsAtUtc,
            newPeriod.Start,
            newPeriod.End,
            changedBy,
            normalizedReason,
            changedAtUtc));

        BerthId = newBerthId;
        StartsAtUtc = newPeriod.Start;
        EndsAtUtc = newPeriod.End;
        RegisterChange(changedBy, normalizedReason, changedAtUtc);
    }

    public void Cancel(string changedBy, string reason, DateTimeOffset changedAtUtc)
    {
        if (Status is BerthWindowStatus.Completed or BerthWindowStatus.Cancelled)
        {
            throw new DomainException("A janela já foi concluída ou cancelada.");
        }

        Status = BerthWindowStatus.Cancelled;
        RegisterChange(
            changedBy,
            DomainRules.RequiredText(reason, "Justificativa do cancelamento", 500),
            changedAtUtc);
    }

    private void RegisterChange(string changedBy, string? reason, DateTimeOffset changedAtUtc)
    {
        LastChangedBy = DomainRules.RequiredText(changedBy, "Responsável pela alteração", 120);
        LastChangeReason = reason;
        MarkUpdated(changedAtUtc);
    }

    private static (DateTimeOffset Start, DateTimeOffset End) NormalizePeriod(
        DateTimeOffset startsAtUtc,
        DateTimeOffset endsAtUtc)
    {
        var start = DomainRules.ToUtc(startsAtUtc);
        var end = DomainRules.ToUtc(endsAtUtc);
        if (end <= start)
        {
            throw new DomainException("O fim da janela deve ser posterior ao início.");
        }

        return (start, end);
    }
}
