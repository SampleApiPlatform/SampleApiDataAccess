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

            //  MessageId must have a value
            entity.Property(e => e.MessageId).IsRequired();

            // Enums as int (Default) or as String for reading DB values:
            entity.Property(e => e.MessageAction)
                .HasConversion<string>()   // optional: varchar instaead of int
                .HasMaxLength(20)
                .IsRequired();

            entity.Property(e => e.MessageStatus)
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();

            entity.Property(e => e.MessageData)
                .HasColumnType("nvarchar(max)")
                .IsRequired(false);
            
            entity.Property(e => e.MovieId).IsRequired();
            
            entity.Property(e => e.ErrorMessage)
                .HasColumnType("nvarchar(max)")
                .IsRequired(false);
                //.HasMaxLength(2000); // Begrenzung, um DB-Performance nicht zu stark zu belasten;

            entity.Property(e => e.CreatedOn)
                .IsRequired()
                .HasDefaultValueSql("GETUTCDATE()");

            entity.Property(e => e.UpdatedOn)
                .IsRequired()
                .HasDefaultValueSql("GETUTCDATE()");
            
            // UNIQUE + INDEX: Avoids Duplicate MessageId + provides faster Lookup (GetByMessageId)
            entity.HasIndex(b => b.MessageId)
            .IsUnique()
            .HasDatabaseName("IX_BusRequest_MessageId");

            // The object needs to exists
            entity.HasIndex(b => b.MovieId)
            .HasDatabaseName("IX_BusRequest_MovieId");

            //entity.Property(b => b.ErrorMessage)
            //.HasMaxLength(2000); // Begrenzung, um DB-Performance nicht zu stark zu belasten
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
