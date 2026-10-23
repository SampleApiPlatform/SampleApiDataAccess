using SampleDataAccessApi.Models;
using SampleDataAccessApi.Data;
using Microsoft.EntityFrameworkCore;



public class BusRequestRepository : IBusRequestRepository
{
    //ASYNC RULE
    //If your method uses await, it must be async.
    //If your method returns a Task directly, it must NOT be async

    private readonly MoviesDbContext _db;

    public BusRequestRepository(MoviesDbContext db)
    {
        _db = db;
    }

    public async Task<IEnumerable<BusRequest>> GetAll()
    {
        return await _db.BusRequests.ToListAsync();
    }

    public async Task<BusRequest?> GetById(string id)
    {
        return await _db.BusRequests.FindAsync(id);
    }

  
    public async Task Add(BusRequest busRequest)
    {
        _db.BusRequests.Add(busRequest);        
        await _db.SaveChangesAsync();
        //EF Core generates the Id, assigns it to movie.Id, that we just passed in as a parameter and tracks it.
        //So after SaveChangesAsync(), your movie object already contains the generated Id.
    }
    

    public async Task Update(BusRequest busRequest)
    {
        _db.BusRequests.Update(busRequest);
        await _db.SaveChangesAsync();
    }

    public async Task Delete(BusRequest busRequest)
    {
        _db.BusRequests.Remove(busRequest);
        await _db.SaveChangesAsync();
    }

}
