namespace PortManagement.Application.Common;

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

public sealed class OptimisticConcurrencyException : Exception
{
    public OptimisticConcurrencyException()
        : base("O registro foi alterado por outra operação.")
    {
    }
}

public sealed class UniqueConstraintException : Exception
{
    public UniqueConstraintException(string? constraintName)
        : base("Uma restrição de unicidade do banco de dados foi violada.")
    {
        ConstraintName = constraintName;
    }

    public string? ConstraintName { get; }
}

public sealed class ExclusionConstraintException : Exception
{
    public ExclusionConstraintException(string? constraintName)
        : base("Uma restrição de exclusão do banco de dados foi violada.")
    {
        ConstraintName = constraintName;
    }

    public string? ConstraintName { get; }
}
