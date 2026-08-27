using SampleDataAccessApi.Models;
using SampleDataAccessApi.Data;
using Microsoft.EntityFrameworkCore;



public class MovieRepository : IMovieRepository
{
    //ASYNC RULE
    //If your method uses await, it must be async.
    //If your method returns a Task directly, it must NOT be async

    private readonly MoviesDbContext _db;

    public MovieRepository(MoviesDbContext db)
    {
        _db = db;
    }

    public async Task<IEnumerable<Movie>> GetAll()
    {
        return await _db.Movies.ToListAsync();
    }

    public async Task<Movie?> GetById(string id)
    {
        return await _db.Movies.FindAsync(id);
    }

  
    public async Task Add(Movie movie)
    {
        _db.Movies.Add(movie);        
        await _db.SaveChangesAsync();
        //EF Core generates the Id, assigns it to movie.Id, that we just passed in as a parameter and tracks it.
        //So after SaveChangesAsync(), your movie object already contains the generated Id.
    }
    

    public async Task Update(Movie movie)
    {
        _db.Movies.Update(movie);
        await _db.SaveChangesAsync();
    }

    public async Task Delete(Movie movie)
    {
        _db.Movies.Remove(movie);
        await _db.SaveChangesAsync();
    }

}
