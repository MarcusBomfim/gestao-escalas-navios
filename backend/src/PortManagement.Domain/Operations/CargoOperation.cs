using PortManagement.Domain.Common;
using PortManagement.Domain.PortCalls;

namespace PortManagement.Domain.Operations;

public sealed class CargoOperation : AuditableEntity
{
    private CargoOperation()
    {
        CargoType = string.Empty;
        PortCall = null!;
    }

    public CargoOperation(
        Guid id,
        Guid portCallId,
        CargoOperationDirection direction,
        string cargoType,
        decimal plannedQuantity,
        CargoQuantityUnit quantityUnit,
        bool isDangerousCargo,
        DateTimeOffset createdAtUtc,
        string? dangerousCargoClassification = null)
        : base(id, createdAtUtc)
    {
        if (portCallId == Guid.Empty)
        {
            throw new DomainException("A escala da operação de carga é obrigatória.");
        }

        if (plannedQuantity < 0)
        {
            throw new DomainException("A quantidade planejada não pode ser negativa.");
        }

        if (isDangerousCargo && string.IsNullOrWhiteSpace(dangerousCargoClassification))
        {
            throw new DomainException("Carga perigosa exige sua classificação.");
        }

        PortCallId = portCallId;
        Direction = direction;
        CargoType = DomainRules.RequiredText(cargoType, "Tipo de carga", 120);
        PlannedQuantity = plannedQuantity;
        QuantityUnit = quantityUnit;
        IsDangerousCargo = isDangerousCargo;
        DangerousCargoClassification = DomainRules.OptionalText(
            dangerousCargoClassification,
            "Classificação da carga perigosa",
            80);
    }

    public Guid PortCallId { get; private set; }

    public PortCall PortCall { get; private set; } = null!;

    public CargoOperationDirection Direction { get; private set; }

    public string CargoType { get; private set; }

    public decimal PlannedQuantity { get; private set; }

    public decimal? ActualQuantity { get; private set; }

    public CargoQuantityUnit QuantityUnit { get; private set; }

    public bool IsDangerousCargo { get; private set; }

    public string? DangerousCargoClassification { get; private set; }

    public DateTimeOffset? PlannedStartAtUtc { get; private set; }

    public DateTimeOffset? PlannedEndAtUtc { get; private set; }

    public DateTimeOffset? ActualStartAtUtc { get; private set; }

    public DateTimeOffset? ActualEndAtUtc { get; private set; }

    public long Version { get; private set; }

    public void Schedule(
        DateTimeOffset plannedStartAtUtc,
        DateTimeOffset plannedEndAtUtc,
        DateTimeOffset changedAtUtc)
    {
        var start = DomainRules.ToUtc(plannedStartAtUtc);
        var end = DomainRules.ToUtc(plannedEndAtUtc);
        if (end <= start)
        {
            throw new DomainException("O término planejado deve ser posterior ao início.");
        }

        PlannedStartAtUtc = start;
        PlannedEndAtUtc = end;
        MarkUpdated(changedAtUtc);
    }

    public void Start(DateTimeOffset startedAtUtc, DateTimeOffset changedAtUtc)
    {
        if (ActualStartAtUtc.HasValue)
        {
            throw new DomainException("A operação de carga já foi iniciada.");
        }

        ActualStartAtUtc = DomainRules.ToUtc(startedAtUtc);
        MarkUpdated(changedAtUtc);
    }

    public void Complete(
        decimal actualQuantity,
        DateTimeOffset completedAtUtc,
        DateTimeOffset changedAtUtc)
    {
        if (!ActualStartAtUtc.HasValue)
        {
            throw new DomainException("Inicie a operação de carga antes de concluí-la.");
        }

        if (ActualEndAtUtc.HasValue)
        {
            throw new DomainException("A operação de carga já foi concluída.");
        }

        var completion = DomainRules.ToUtc(completedAtUtc);
        if (completion < ActualStartAtUtc.Value)
        {
            throw new DomainException("O término realizado não pode ser anterior ao início.");
        }

        RecordActualQuantity(actualQuantity, changedAtUtc);
        ActualEndAtUtc = completion;
        MarkUpdated(changedAtUtc);
    }

    public void RecordActualQuantity(decimal quantity, DateTimeOffset changedAtUtc)
    {
        if (quantity < 0)
        {
            throw new DomainException("A quantidade realizada não pode ser negativa.");
        }

        ActualQuantity = quantity;
        MarkUpdated(changedAtUtc);
    }
}
