namespace PortManagement.Domain.Common;

public abstract class AuditableEntity : Entity
{
    protected AuditableEntity()
    {
    }

    protected AuditableEntity(Guid id, DateTimeOffset createdAtUtc)
        : base(id)
    {
        CreatedAtUtc = DomainRules.ToUtc(createdAtUtc);
        UpdatedAtUtc = CreatedAtUtc;
    }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    protected void MarkUpdated(DateTimeOffset changedAtUtc)
    {
        UpdatedAtUtc = DomainRules.ToUtc(changedAtUtc);
    }
}
