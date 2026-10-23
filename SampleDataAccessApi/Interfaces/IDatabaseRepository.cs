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

public interface IDatabaseRepository<T>
{
    Task<IEnumerable<T>> GetAll();
    Task<T?> GetById(string id);
    Task Add(T movie);   
    Task Update(T movie);
    Task Delete(T movie);
}
