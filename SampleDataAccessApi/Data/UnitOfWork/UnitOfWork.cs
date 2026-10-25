using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using SampleDataAccessApi.Data;
using SampleDataAccessApi.Models;

public class UnitOfWork : IUnitOfWork
{
    private readonly MoviesDbContext _db; 
    
    
    public UnitOfWork(MoviesDbContext db) => _db = db;
    
    //public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken ct = default)
    //    => _db.Database.BeginTransactionAsync(ct);

    public void Add(Object entity)
    {
        switch(entity.GetType().Name)
        {
            case "Movie": 
                _db.Movies.Add((Movie)entity);
                break;
            case "BusRequest":
                _db.BusRequests.Add((BusRequest)entity);
                break;
            default:
                throw new InvalidOperationException ($"Entity:{entity.GetType().Name} not allowed");
        }  
    }

    public void Update(Object entity)
    {
        switch(entity.GetType().Name)
        {
            case "Movie": 
                _db.Movies.Update((Movie)entity);
                break;
            case "BusRequest":
                _db.BusRequests.Update((BusRequest)entity);
                break;
            default:
                throw new InvalidOperationException ($"Entity:{entity.GetType().Name} not allowed");
        }  
    }

    public void Delete(Object entity)
    {
        switch(entity.GetType().Name)
        {
            case "Movie": 
                _db.Movies.Remove((Movie)entity);
                break;
            case "BusRequest":
                _db.BusRequests.Remove((BusRequest)entity);
                break;
            default:
                throw new InvalidOperationException ($"Entity:{entity.GetType().Name} not allowed");
        }  
    }


    public async Task SaveAllAsync()
    {
        await _db.SaveChangesAsync();
    }

    public void ClearAll()
    {
         _db.ChangeTracker.Clear();
    }


}