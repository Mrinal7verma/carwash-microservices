namespace CustomerService.Data;

using CustomerService.Data.Entities;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// EF Core DbContext for the CustomerService database.
/// </summary>
public class CustomerDbContext : DbContext
{
    public CustomerDbContext(DbContextOptions<CustomerDbContext> options)
        : base(options)
    {
    }

    public DbSet<Customer> Customers => Set<Customer>();

    public DbSet<Car> Cars => Set<Car>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Customer>(entity =>
        {
            entity.ToTable("Customers");

            entity.HasKey(e => e.CustomerId);
            entity.Property(e => e.CustomerId)
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

            entity.Property(e => e.PasswordHash)
                .HasMaxLength(255)
                .IsRequired();

            entity.Property(e => e.Address)
                .HasMaxLength(255);

            entity.Property(e => e.City)
                .HasMaxLength(100);

            entity.HasMany(e => e.Cars)
                .WithOne(c => c.Customer)
                .HasForeignKey(c => c.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Car>(entity =>
        {
            entity.ToTable("Cars");

            entity.HasKey(e => e.CarId);
            entity.Property(e => e.CarId)
                .ValueGeneratedOnAdd();

            entity.Property(e => e.Make)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(e => e.Model)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(e => e.RegistrationNo)
                .HasMaxLength(20)
                .IsRequired();

            entity.HasIndex(e => e.RegistrationNo)
                .IsUnique();
        });
    }
}
