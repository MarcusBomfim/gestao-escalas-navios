namespace PortManagement.Domain.Common;

public abstract class Entity
{
    protected Entity()
    {
    }

    protected Entity(Guid id)
    {
        if (id == Guid.Empty)
        {
            throw new DomainException("O identificador não pode ser vazio.");
        }

        Id = id;
    }

    public Guid Id { get; private set; }
}
