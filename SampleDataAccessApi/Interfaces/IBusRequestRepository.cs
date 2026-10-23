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

public interface IBusRequestRepository
{
    Task<IEnumerable<BusRequest>> GetAll();
    Task<BusRequest?> GetById(string id);
    Task Add(BusRequest BusRequest);   
    Task Update(BusRequest BusRequest);
    Task Delete(BusRequest BusRequest);
}

