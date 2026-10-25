using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

public interface IUnitOfWork
{
    //Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken ct = default);
    //DbContext GetDbContext();

    void Add(Object entity);
    void Update(Object entity);
    void Delete(Object entity);
    Task SaveAllAsync();
    void ClearAll();
}