using PortManagement.Domain.Common;

namespace PortManagement.Domain.Organizations;

public sealed class Organization : AuditableEntity
{
    private Organization()
    {
        Name = string.Empty;
        RegistrationNumber = string.Empty;
    }

    public Organization(
        Guid id,
        string name,
        string registrationNumber,
        OrganizationType type,
        DateTimeOffset createdAtUtc)
        : base(id, createdAtUtc)
    {
        Name = DomainRules.RequiredText(name, "Nome da organização", 180);
        RegistrationNumber = DomainRules.RequiredText(registrationNumber, "Registro da organização", 40);
        Type = ValidateType(type);
        IsActive = true;
    }

    public string Name { get; private set; }

    public string RegistrationNumber { get; private set; }

    public OrganizationType Type { get; private set; }

    public bool IsActive { get; private set; }

    public void Update(
        string name,
        string registrationNumber,
        OrganizationType type,
        bool isActive,
        DateTimeOffset changedAtUtc)
    {
        Name = DomainRules.RequiredText(name, "Nome da organização", 180);
        RegistrationNumber = DomainRules.RequiredText(
            registrationNumber,
            "Registro da organização",
            40);
        Type = ValidateType(type);
        IsActive = isActive;
        MarkUpdated(changedAtUtc);
    }

    private static OrganizationType ValidateType(OrganizationType type)
    {
        if (!Enum.IsDefined(type))
        {
            throw new DomainException("O tipo da organização não é válido.");
        }

        return type;
    }
}
