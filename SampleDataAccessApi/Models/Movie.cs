namespace SampleDataAccessApi.Models;

public class Movie
{
    // required is not needed. It will be provided by the EF. 
    // This class is only meant to be used by the EF to represent the Movie Row on the DB.
    //public required string Id { get; set; } 
    // However we need to provide a value because Id, although is a reference type is declared as not nullable.
    // In our case we have an INMemory DB and the Ids are not generated. That is why instead of an empty string we assign a Guid
    //public string Id { get; set; } = String.Empty;
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public required string Title { get; set; }
    public int Year { get; set; }
}
