namespace BookingService.Data;

using BookingService.Data.Entities;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// EF Core DbContext for the BookingService database.
/// </summary>
public class BookingDbContext : DbContext
{
    public BookingDbContext(DbContextOptions<BookingDbContext> options)
        : base(options)
    {
    }

    public DbSet<ServicePackage> ServicePackages => Set<ServicePackage>();

    public DbSet<Booking> Bookings => Set<Booking>();

    public DbSet<BookingRejection> BookingRejections => Set<BookingRejection>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ServicePackage>(entity =>
        {
            entity.ToTable("ServicePackages");

            entity.HasKey(e => e.PackageId);
            entity.Property(e => e.PackageId)
                .ValueGeneratedOnAdd();

            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(e => e.Description)
                .HasMaxLength(500)
                .IsRequired();

            entity.Property(e => e.Price)
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            entity.Property(e => e.EstimatedDurationMinutes)
                .IsRequired();

            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .IsRequired();

            // Seed initial packages
            entity.HasData(
                new ServicePackage
                {
                    PackageId = 1L,
                    Name = "Basic Wash",
                    Description = "Basic exterior wash and dry",
                    Price = 299.00m,
                    EstimatedDurationMinutes = 30,
                    IsActive = true
                },
                new ServicePackage
                {
                    PackageId = 2L,
                    Name = "Full Interior & Exterior",
                    Description = "Comprehensive interior vacuum and full exterior wash",
                    Price = 599.00m,
                    EstimatedDurationMinutes = 90,
                    IsActive = true
                });
        });

        modelBuilder.Entity<Booking>(entity =>
        {
            entity.ToTable("Bookings");

            entity.HasKey(e => e.BookingId);
            entity.Property(e => e.BookingId)
                .ValueGeneratedOnAdd();

            entity.Property(e => e.CustomerId)
                .IsRequired();

            entity.Property(e => e.CarId)
                .IsRequired();

            entity.Property(e => e.WasherId);

            entity.Property(e => e.BookingDate)
                .HasColumnType("datetime")
                .IsRequired();

            entity.Property(e => e.ScheduledTime)
                .HasColumnType("datetime")
                .IsRequired();

            entity.Property(e => e.Status)
                .HasMaxLength(30)
                .IsRequired();

            entity.Property(e => e.TotalAmount)
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            entity.HasIndex(e => new { e.Status, e.ScheduledTime });
            entity.HasIndex(e => e.CustomerId);
            entity.HasIndex(e => e.WasherId);

            entity.HasOne(e => e.Package)
                .WithMany(p => p.Bookings)
                .HasForeignKey(e => e.PackageId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(e => e.Rejections)
                .WithOne(r => r.Booking)
                .HasForeignKey(r => r.BookingId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<BookingRejection>(entity =>
        {
            entity.ToTable("BookingRejections");

            entity.HasKey(e => e.BookingRejectionId);
            entity.Property(e => e.BookingRejectionId)
                .ValueGeneratedOnAdd();

            entity.Property(e => e.WasherId)
                .IsRequired();

            entity.Property(e => e.RejectedAt)
                .HasColumnType("datetime")
                .IsRequired();

            entity.HasIndex(e => new { e.BookingId, e.WasherId })
                .IsUnique();
        });
    }
}
