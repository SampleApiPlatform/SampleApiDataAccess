using Microsoft.EntityFrameworkCore;
using SampleDataAccessApi.Models;

namespace SampleDataAccessApi.Data;

public class MoviesDbContext : DbContext
{
    public MoviesDbContext(DbContextOptions<MoviesDbContext> options) : base(options) {}

    public DbSet<Movie> Movies => Set<Movie>();


}
