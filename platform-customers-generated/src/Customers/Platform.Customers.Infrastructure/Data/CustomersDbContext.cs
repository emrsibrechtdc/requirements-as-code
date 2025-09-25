using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Platform.Shared.EntityFrameworkCore;
using Platform.Customers.Domain.Customers;

namespace Platform.Customers.Infrastructure.Data;

public class CustomersDbContext : PlatformDbContext
{
    public CustomersDbContext(DbContextOptions<CustomersDbContext> options, ILogger<CustomersDbContext> logger) 
        : base(options, logger)
    {
    }

    public DbSet<Customer> Customers { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder); // Platform.Shared configuration
        
        // Configure entities to match database schema
        // No migrations needed - schema managed by SQL project
        modelBuilder.Entity<Customer>(entity =>
        {
            entity.ToTable("Customers");
            entity.HasKey(e => e.Id);
            
            // Configure business properties to match database schema
            entity.Property(e => e.CustomerCode).HasMaxLength(50).IsRequired();
            entity.Property(e => e.CustomerType).HasMaxLength(20).IsRequired();
            entity.Property(e => e.CompanyName).HasMaxLength(200).IsRequired();
            
            // Configure ContactInfo value object properties
            entity.OwnsOne(e => e.ContactInfo, contactInfo =>
            {
                contactInfo.Property(c => c.FirstName)
                    .HasColumnName("ContactFirstName")
                    .HasMaxLength(100)
                    .IsRequired();
                    
                contactInfo.Property(c => c.LastName)
                    .HasColumnName("ContactLastName")
                    .HasMaxLength(100)
                    .IsRequired();
                    
                contactInfo.Property(c => c.Email)
                    .HasColumnName("Email")
                    .HasMaxLength(255)
                    .IsRequired();
                    
                contactInfo.Property(c => c.PhoneNumber)
                    .HasColumnName("PhoneNumber")
                    .HasMaxLength(20);
            });
            
            // Configure Address value object properties
            entity.OwnsOne(e => e.Address, address =>
            {
                address.Property(a => a.AddressLine1)
                    .HasColumnName("AddressLine1")
                    .HasMaxLength(255)
                    .IsRequired();
                    
                address.Property(a => a.AddressLine2)
                    .HasColumnName("AddressLine2")
                    .HasMaxLength(255);
                    
                address.Property(a => a.City)
                    .HasColumnName("City")
                    .HasMaxLength(100)
                    .IsRequired();
                    
                address.Property(a => a.State)
                    .HasColumnName("State")
                    .HasMaxLength(50)
                    .IsRequired();
                    
                address.Property(a => a.PostalCode)
                    .HasColumnName("PostalCode")
                    .HasMaxLength(20)
                    .IsRequired();
                    
                address.Property(a => a.Country)
                    .HasColumnName("Country")
                    .HasMaxLength(100)
                    .IsRequired();
            });
            
            // Indexes are defined in SQL project - no need to define here
            // Platform.Shared handles audit fields and soft delete automatically
        });
    }
}