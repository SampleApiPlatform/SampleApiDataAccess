using Microsoft.EntityFrameworkCore.Storage;

public interface IUnitOfWork
{
    Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken ct = default);
}