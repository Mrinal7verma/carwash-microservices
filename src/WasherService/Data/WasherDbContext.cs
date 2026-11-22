namespace WasherService.Data;

using Microsoft.EntityFrameworkCore;
using WasherService.Data.Entities;

/// <summary>
/// EF Core DbContext for the WasherService database.
/// </summary>
public class WasherDbContext : DbContext
{
    public WasherDbContext(DbContextOptions<WasherDbContext> options)
        : base(options)
    {
    }

    public DbSet<Washer> Washers => Set<Washer>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Washer>(entity =>
        {
            entity.ToTable("Washers");

            entity.HasKey(e => e.WasherId);
            entity.Property(e => e.WasherId)
                .ValueGeneratedOnAdd();

            entity.Property(e => e.FirstName)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(e => e.LastName)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(e => e.Email)
                .HasMaxLength(150)
                .IsRequired();

            entity.HasIndex(e => e.Email)
                .IsUnique();

            entity.Property(e => e.Phone)
                .HasMaxLength(15)
                .IsRequired();

            entity.HasIndex(e => e.Phone)
                .IsUnique();

            entity.Property(e => e.PasswordHash)
                .HasMaxLength(255)
                .IsRequired();

            entity.Property(e => e.IsAvailable)
                .HasDefaultValue(true)
                .IsRequired();
        });
    }
}
