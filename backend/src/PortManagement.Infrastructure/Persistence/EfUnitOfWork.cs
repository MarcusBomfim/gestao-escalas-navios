using Microsoft.EntityFrameworkCore;
using Npgsql;
using PortManagement.Application.Common;

namespace PortManagement.Infrastructure.Persistence;

internal sealed class EfUnitOfWork(PortManagementDbContext database) : IUnitOfWork
{
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await database.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new OptimisticConcurrencyException();
        }
        catch (DbUpdateException exception)
            when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } postgres)
        {
            throw new UniqueConstraintException(postgres.ConstraintName);
        }
    }
}
