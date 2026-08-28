using PortManagement.Domain.Common;

namespace PortManagement.Domain.Ports;

public sealed class Terminal : AuditableEntity
{
    private Terminal()
    {
        Code = string.Empty;
        Name = string.Empty;
        TimeZoneId = string.Empty;
        Port = null!;
    }

    public Terminal(
        Guid id,
        Guid portId,
        string code,
        string name,
        string timeZoneId,
        DateTimeOffset createdAtUtc)
        : base(id, createdAtUtc)
    {
        if (portId == Guid.Empty)
        {
            throw new DomainException("O porto do terminal é obrigatório.");
        }

        PortId = portId;
        Code = DomainRules.RequiredText(code, "Código do terminal", 30).ToUpperInvariant();
        Name = DomainRules.RequiredText(name, "Nome do terminal", 160);
        TimeZoneId = DomainRules.RequiredText(timeZoneId, "Fuso horário", 80);
        IsActive = true;
    }

    public Guid PortId { get; private set; }

    public Port Port { get; private set; } = null!;

    public string Code { get; private set; }

    public string Name { get; private set; }

    public string TimeZoneId { get; private set; }

    public bool IsActive { get; private set; }

    public void Update(
        string code,
        string name,
        string timeZoneId,
        bool isActive,
        DateTimeOffset changedAtUtc)
    {
        Code = DomainRules.RequiredText(code, "Código do terminal", 30).ToUpperInvariant();
        Name = DomainRules.RequiredText(name, "Nome do terminal", 160);
        TimeZoneId = DomainRules.RequiredText(timeZoneId, "Fuso horário", 80);
        IsActive = isActive;
        MarkUpdated(changedAtUtc);
    }
}
