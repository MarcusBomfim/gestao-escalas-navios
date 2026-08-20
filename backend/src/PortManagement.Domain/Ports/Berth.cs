using PortManagement.Domain.Common;
using PortManagement.Domain.Vessels;

namespace PortManagement.Domain.Ports;

public sealed class Berth : AuditableEntity
{
    private Berth()
    {
        Code = string.Empty;
        Name = string.Empty;
        SupportedVesselTypes = [];
        Terminal = null!;
    }

    public Berth(
        Guid id,
        Guid terminalId,
        string code,
        string name,
        decimal usefulLengthMeters,
        decimal maximumBeamMeters,
        decimal maximumDraftMeters,
        IEnumerable<VesselType> supportedVesselTypes,
        DateTimeOffset createdAtUtc)
        : base(id, createdAtUtc)
    {
        if (terminalId == Guid.Empty)
        {
            throw new DomainException("O terminal do berço é obrigatório.");
        }

        TerminalId = terminalId;
        Code = DomainRules.RequiredText(code, "Código do berço", 30).ToUpperInvariant();
        Name = DomainRules.RequiredText(name, "Nome do berço", 120);
        UsefulLengthMeters = DomainRules.Positive(usefulLengthMeters, "Comprimento útil");
        MaximumBeamMeters = DomainRules.Positive(maximumBeamMeters, "Boca máxima");
        MaximumDraftMeters = DomainRules.Positive(maximumDraftMeters, "Calado máximo");
        SupportedVesselTypes = supportedVesselTypes.Distinct().ToArray();
        Status = BerthStatus.Available;
    }

    public Guid TerminalId { get; private set; }

    public Terminal Terminal { get; private set; } = null!;

    public string Code { get; private set; }

    public string Name { get; private set; }

    public decimal UsefulLengthMeters { get; private set; }

    public decimal MaximumBeamMeters { get; private set; }

    public decimal MaximumDraftMeters { get; private set; }

    public VesselType[] SupportedVesselTypes { get; private set; }

    public BerthStatus Status { get; private set; }

    public bool CanReceive(Vessel vessel)
    {
        ArgumentNullException.ThrowIfNull(vessel);

        return Status == BerthStatus.Available
            && vessel.LengthOverallMeters <= UsefulLengthMeters
            && vessel.BeamMeters <= MaximumBeamMeters
            && vessel.MaximumDraftMeters <= MaximumDraftMeters
            && (SupportedVesselTypes.Length == 0 || SupportedVesselTypes.Contains(vessel.Type));
    }
}
