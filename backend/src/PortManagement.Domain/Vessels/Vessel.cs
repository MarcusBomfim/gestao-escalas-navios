using PortManagement.Domain.Common;

namespace PortManagement.Domain.Vessels;

public sealed class Vessel : AuditableEntity
{
    private Vessel()
    {
        Name = string.Empty;
        FlagCode = string.Empty;
    }

    public Vessel(
        Guid id,
        string name,
        ImoNumber? imoNumber,
        string flagCode,
        VesselType type,
        decimal lengthOverallMeters,
        decimal beamMeters,
        decimal maximumDraftMeters,
        DateTimeOffset createdAtUtc,
        string? callSign = null,
        string? mmsi = null)
        : base(id, createdAtUtc)
    {
        Name = DomainRules.RequiredText(name, "Nome do navio", 160);
        ImoNumber = imoNumber;
        FlagCode = DomainRules.RequiredText(flagCode, "Código da bandeira", 2).ToUpperInvariant();
        Type = type;
        LengthOverallMeters = DomainRules.Positive(lengthOverallMeters, "Comprimento total");
        BeamMeters = DomainRules.Positive(beamMeters, "Boca");
        MaximumDraftMeters = DomainRules.Positive(maximumDraftMeters, "Calado máximo");
        CallSign = DomainRules.OptionalText(callSign, "Indicativo de chamada", 20)?.ToUpperInvariant();
        Mmsi = DomainRules.OptionalText(mmsi, "MMSI", 9);
        IsActive = true;
    }

    public string Name { get; private set; }

    public ImoNumber? ImoNumber { get; private set; }

    public string FlagCode { get; private set; }

    public VesselType Type { get; private set; }

    public decimal LengthOverallMeters { get; private set; }

    public decimal BeamMeters { get; private set; }

    public decimal MaximumDraftMeters { get; private set; }

    public string? CallSign { get; private set; }

    public string? Mmsi { get; private set; }

    public bool IsActive { get; private set; }

    public void Rename(string name, DateTimeOffset changedAtUtc)
    {
        Name = DomainRules.RequiredText(name, "Nome do navio", 160);
        MarkUpdated(changedAtUtc);
    }

    public void UpdateDetails(
        string name,
        ImoNumber? imoNumber,
        string flagCode,
        VesselType type,
        decimal lengthOverallMeters,
        decimal beamMeters,
        decimal maximumDraftMeters,
        string? callSign,
        string? mmsi,
        DateTimeOffset changedAtUtc)
    {
        Name = DomainRules.RequiredText(name, "Nome do navio", 160);
        ImoNumber = imoNumber;
        FlagCode = DomainRules.RequiredText(flagCode, "Código da bandeira", 2).ToUpperInvariant();
        Type = type;
        LengthOverallMeters = DomainRules.Positive(lengthOverallMeters, "Comprimento total");
        BeamMeters = DomainRules.Positive(beamMeters, "Boca");
        MaximumDraftMeters = DomainRules.Positive(maximumDraftMeters, "Calado máximo");
        CallSign = DomainRules.OptionalText(callSign, "Indicativo de chamada", 20)?.ToUpperInvariant();
        Mmsi = DomainRules.OptionalText(mmsi, "MMSI", 9);
        MarkUpdated(changedAtUtc);
    }

    public void Deactivate(DateTimeOffset changedAtUtc)
    {
        IsActive = false;
        MarkUpdated(changedAtUtc);
    }
}
