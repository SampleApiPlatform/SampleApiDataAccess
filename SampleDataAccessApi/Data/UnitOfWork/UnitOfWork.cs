using Microsoft.EntityFrameworkCore.Storage;
using SampleDataAccessApi.Data;

public class UnitOfWork : IUnitOfWork
{
    private readonly MoviesDbContext _db; 
    
    public UnitOfWork(MoviesDbContext db) => _db = db;
    
    public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken ct = default)
        => _db.Database.BeginTransactionAsync(ct);
}