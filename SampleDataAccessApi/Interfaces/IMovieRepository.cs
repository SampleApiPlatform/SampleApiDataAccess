/// <summary>
/// Repositiory Pattern: 
/// Decouples your application from the database
/// Provides a clean separation of concerns: Controllers handle HTTP,Repositories handle data access,DbContext handles EF Core.
/// Each layer has one responsibility.
/// Makes unit testing possible.
/// Centralizes data access logic: Instead of having EF Core queries scattered across controllers, everything is in one place.
/// Allows swapping the data source
/// </summary>
using SampleDataAccessApi.Models;

public interface IMovieRepository
{
    Task<IEnumerable<Movie>> GetAll();
    Task<Movie?> GetById(string id);
    Task Add(Movie movie);   
    Task Update(Movie movie);
    Task Delete(Movie movie);
}
