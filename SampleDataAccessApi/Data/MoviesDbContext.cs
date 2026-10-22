using Microsoft.EntityFrameworkCore;
using SampleDataAccessApi.Models;

namespace SampleDataAccessApi.Data;

public class MoviesDbContext : DbContext
{
    public MoviesDbContext(DbContextOptions<MoviesDbContext> options) : base(options) {}

    public DbSet<Movie> Movies => Set<Movie>();

    public DbSet<Request> Requests => Set<Request>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Request>(entity =>
        {
            // PK
            entity.HasKey(e => e.Id);

            // Column-Definitions
            entity.Property(e => e.Id)
                .ValueGeneratedOnAdd(); // Important: EF should generate an ID

            entity.Property(e => e.MessageId)
                .IsRequired();

            entity.Property(e => e.IsSucceeded)
                .IsRequired(); // In DB as NOT NULL, but allow  NULL-Values (bit)

            entity.Property(e => e.CreatedOn)
                .IsRequired();

            entity.Property(e => e.UpdatedOn)
                .IsRequired();

            // Automatic creation value for CreatedOn and UpdatedOn
            // Tip: EF Core srts CreatedOn only with the first Insert
            entity.Property(e => e.CreatedOn)
                .HasDefaultValueSql("GETUTCDATE()");

            // UpdatedOn must often manually in the Code oder via a Trigger updated
            // EF Core 7+ supports .HasDefaultValueSql also for Updates, but often is 
            // a manual call in SaveChanges more secure.
            // Alternative:
            entity.Property(e => e.UpdatedOn)
                .HasDefaultValueSql("GETUTCDATE()");
        });
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<IHasUpdateTimestamp>())
        {
            entry.Entity.UpdatedOn = DateTime.UtcNow;
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
