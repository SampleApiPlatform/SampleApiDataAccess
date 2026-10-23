using Microsoft.EntityFrameworkCore;
using NuGet.SampleSharedModels.Enums.EnumsServiceBus;
using SampleDataAccessApi.Models;

namespace SampleDataAccessApi.Data;

public class MoviesDbContext : DbContext
{
    public MoviesDbContext(DbContextOptions<MoviesDbContext> options) : base(options) {}

    public DbSet<Movie> Movies => Set<Movie>();

    public DbSet<BusRequest> BusRequests => Set<BusRequest>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<BusRequest>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.MessageId).IsRequired();

            // Enums als int (Default) oder als String für lesbare DB-Werte:
            entity.Property(e => e.MessageAction)
                .HasConversion<string>()   // optional: varchar statt int
                .HasMaxLength(20)
                .IsRequired();

            entity.Property(e => e.MessageStatus)
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();

            entity.Property(e => e.MessageData)
                .HasColumnType("nvarchar(max)")
                .IsRequired(false);

            entity.Property(e => e.CreatedOn)
                .IsRequired()
                .HasDefaultValueSql("GETUTCDATE()");

            entity.Property(e => e.UpdatedOn)
                .IsRequired()
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
