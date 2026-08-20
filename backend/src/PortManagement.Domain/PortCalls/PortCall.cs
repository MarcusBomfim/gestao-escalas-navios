using PortManagement.Domain.Common;
using PortManagement.Domain.Organizations;
using PortManagement.Domain.Ports;
using PortManagement.Domain.Vessels;

namespace PortManagement.Domain.PortCalls;

public sealed class PortCall : AuditableEntity
{
    private static readonly Dictionary<PortCallStatus, PortCallStatus[]> AllowedTransitions =
        new Dictionary<PortCallStatus, PortCallStatus[]>
        {
            [PortCallStatus.Draft] = [PortCallStatus.Requested, PortCallStatus.Cancelled],
            [PortCallStatus.Requested] = [PortCallStatus.UnderReview, PortCallStatus.Cancelled],
            [PortCallStatus.UnderReview] = [PortCallStatus.Planned, PortCallStatus.Cancelled],
            [PortCallStatus.Planned] = [PortCallStatus.AtAnchorage, PortCallStatus.ClearedForBerthing, PortCallStatus.Cancelled],
            [PortCallStatus.AtAnchorage] = [PortCallStatus.ClearedForBerthing, PortCallStatus.Cancelled],
            [PortCallStatus.ClearedForBerthing] = [PortCallStatus.Berthed, PortCallStatus.Cancelled],
            [PortCallStatus.Berthed] = [PortCallStatus.InOperation, PortCallStatus.Cancelled],
            [PortCallStatus.InOperation] = [PortCallStatus.OperationCompleted, PortCallStatus.Cancelled],
            [PortCallStatus.OperationCompleted] = [PortCallStatus.Unberthed, PortCallStatus.Cancelled],
            [PortCallStatus.Unberthed] = [PortCallStatus.Closed],
            [PortCallStatus.Closed] = [],
            [PortCallStatus.Cancelled] = []
        };

    private readonly List<PortCallStatusHistory> _statusHistory = [];

    private PortCall()
    {
        PublicCode = string.Empty;
        IdempotencyKey = string.Empty;
        Vessel = null!;
        Port = null!;
    }

    public PortCall(
        Guid id,
        Guid vesselId,
        Guid portId,
        PortCallPurpose purpose,
        string idempotencyKey,
        DateTimeOffset createdAtUtc,
        string? voyageNumber = null,
        string? previousPortUnLocode = null,
        string? nextPortUnLocode = null)
        : base(id, createdAtUtc)
    {
        if (vesselId == Guid.Empty || portId == Guid.Empty)
        {
            throw new DomainException("Navio e porto são obrigatórios para a escala.");
        }

        VesselId = vesselId;
        PortId = portId;
        Purpose = purpose;
        IdempotencyKey = DomainRules.RequiredText(idempotencyKey, "Chave de idempotência", 100);
        PublicCode = CreatePublicCode(createdAtUtc);
        VoyageNumber = DomainRules.OptionalText(voyageNumber, "Número da viagem", 50);
        PreviousPortUnLocode = NormalizeOptionalUnLocode(previousPortUnLocode);
        NextPortUnLocode = NormalizeOptionalUnLocode(nextPortUnLocode);
        Status = PortCallStatus.Draft;
    }

    public string PublicCode { get; private set; }

    public string IdempotencyKey { get; private set; }

    public Guid VesselId { get; private set; }

    public Vessel Vessel { get; private set; } = null!;

    public Guid PortId { get; private set; }

    public Port Port { get; private set; } = null!;

    public Guid? AgentOrganizationId { get; private set; }

    public Organization? AgentOrganization { get; private set; }

    public Guid? ShippingLineOrganizationId { get; private set; }

    public Organization? ShippingLineOrganization { get; private set; }

    public Guid? PlannedTerminalId { get; private set; }

    public Terminal? PlannedTerminal { get; private set; }

    public Guid? PlannedBerthId { get; private set; }

    public Berth? PlannedBerth { get; private set; }

    public PortCallPurpose Purpose { get; private set; }

    public string? VoyageNumber { get; private set; }

    public string? PreviousPortUnLocode { get; private set; }

    public string? NextPortUnLocode { get; private set; }

    public PortCallStatus Status { get; private set; }

    public long Version { get; private set; }

    public DateTimeOffset? ClosedAtUtc { get; private set; }

    public IReadOnlyCollection<PortCallStatusHistory> StatusHistory => _statusHistory.AsReadOnly();

    public void AssignOrganizations(Guid? agentOrganizationId, Guid? shippingLineOrganizationId, DateTimeOffset changedAtUtc)
    {
        AgentOrganizationId = agentOrganizationId;
        ShippingLineOrganizationId = shippingLineOrganizationId;
        MarkUpdated(changedAtUtc);
    }

    public void PlanAt(Guid terminalId, Guid berthId, DateTimeOffset changedAtUtc)
    {
        if (terminalId == Guid.Empty || berthId == Guid.Empty)
        {
            throw new DomainException("Terminal e berço planejados são obrigatórios.");
        }

        PlannedTerminalId = terminalId;
        PlannedBerthId = berthId;
        MarkUpdated(changedAtUtc);
    }

    public void TransitionTo(
        PortCallStatus newStatus,
        string changedBy,
        DateTimeOffset changedAtUtc,
        string? reason = null)
    {
        if (!AllowedTransitions[Status].Contains(newStatus))
        {
            throw new DomainException($"A transição de {Status} para {newStatus} não é permitida.");
        }

        var normalizedReason = DomainRules.OptionalText(reason, "Justificativa", 500);
        if (newStatus == PortCallStatus.Cancelled && normalizedReason is null)
        {
            throw new DomainException("O cancelamento da escala exige uma justificativa.");
        }

        var previousStatus = Status;
        var occurredAtUtc = DomainRules.ToUtc(changedAtUtc);
        Status = newStatus;
        ClosedAtUtc = newStatus == PortCallStatus.Closed ? occurredAtUtc : null;
        _statusHistory.Add(new PortCallStatusHistory(
            Guid.NewGuid(),
            Id,
            previousStatus,
            newStatus,
            changedBy,
            occurredAtUtc,
            normalizedReason));
        MarkUpdated(occurredAtUtc);
    }

    private static string CreatePublicCode(DateTimeOffset createdAtUtc)
    {
        var suffix = Guid.NewGuid().ToString("N")[..10].ToUpperInvariant();
        return $"ESC-{createdAtUtc.ToUniversalTime():yyyy}-{suffix}";
    }

    private static string? NormalizeOptionalUnLocode(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim().ToUpperInvariant().Replace(" ", string.Empty, StringComparison.Ordinal);
        if (normalized.Length != 5 || !normalized.All(char.IsAsciiLetterOrDigit))
        {
            throw new DomainException("O UN/LOCODE deve possuir cinco caracteres alfanuméricos.");
        }

        return normalized;
    }
}
