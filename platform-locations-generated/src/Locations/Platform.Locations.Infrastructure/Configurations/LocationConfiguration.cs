using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Platform.Locations.Application.Locations;
using Platform.Locations.Domain.Locations;

namespace Platform.Locations.Infrastructure.Configurations;

public class LocationConfiguration : IEntityTypeConfiguration<Location>
{
    public void Configure(EntityTypeBuilder<Location> builder)
    {
        builder.ToTable("Locations");
        
        // Primary key
        builder.HasKey(x => x.Id);
        
        // Properties
        builder.Property(x => x.LocationCode)
            .IsRequired()
            .HasMaxLength(LocationsConstants.LocationCodeMaxLength);
        
        builder.Property(x => x.LocationTypeCode)
            .IsRequired()
            .HasMaxLength(LocationsConstants.LocationTypeCodeMaxLength);
        
        builder.Property(x => x.LocationTypeName)
            .HasMaxLength(LocationsConstants.LocationTypeNameMaxLength);
        
        builder.Property(x => x.AddressLine1)
            .IsRequired()
            .HasMaxLength(LocationsConstants.AddressLineMaxLength);
        
        builder.Property(x => x.AddressLine2)
            .HasMaxLength(LocationsConstants.AddressLineMaxLength);
        
        builder.Property(x => x.City)
            .IsRequired()
            .HasMaxLength(LocationsConstants.CityMaxLength);
        
        builder.Property(x => x.State)
            .IsRequired()
            .HasMaxLength(LocationsConstants.StateMaxLength);
        
        builder.Property(x => x.ZipCode)
            .IsRequired()
            .HasMaxLength(LocationsConstants.ZipCodeMaxLength);
        
        builder.Property(x => x.Country)
            .IsRequired()
            .HasMaxLength(LocationsConstants.CountryMaxLength);
        
        builder.Property(x => x.Product)
            .IsRequired()
            .HasMaxLength(100); // Platform standard product identifier length
        
        // Indexes
        builder.HasIndex(x => x.LocationCode)
            .IsUnique()
            .HasDatabaseName("IX_Locations_LocationCode");
        
        builder.HasIndex(x => x.Product)
            .HasDatabaseName("IX_Locations_Product");
        
        builder.HasIndex(x => new { x.Product, x.LocationCode })
            .IsUnique()
            .HasDatabaseName("IX_Locations_Product_LocationCode");
        
        builder.HasIndex(x => x.LocationTypeCode)
            .HasDatabaseName("IX_Locations_LocationTypeCode");
        
        builder.HasIndex(x => x.IsActive)
            .HasDatabaseName("IX_Locations_IsActive");
        
        // Platform.Shared audit properties are configured by base class
    }
}