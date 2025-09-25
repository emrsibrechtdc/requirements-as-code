using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Platform.Locations.Application.Locations;
using Platform.Locations.Domain.Locations;
using Platform.Shared.Auditing;
using Platform.Shared.DataLayer;
using Platform.Shared.Ddd.Domain.Entities;
using Platform.Shared.Entities;
using Platform.Shared.EntityFrameworkCore.Extensions;
using Platform.Shared.MultiProduct;
using Platform.Shared.RequestContext;

namespace Platform.Locations.Infrastructure.Configurations;

public class LocationConfiguration : IEntityTypeConfiguration<Location>
{
    private readonly IDataFilter<IActivable> _activableDataFilter;
    private readonly IDataFilter<IMultiProductObject> _multiProductObjectDataFilter;
    private readonly IMultiProductRequestContextProvider _requestContextProvider;
    public LocationConfiguration(IMultiProductRequestContextProvider requestContextProvider,
            IDataFilter<IActivable> activableDataFilter,
            IDataFilter<IMultiProductObject> multiProductObjectDataFilter)
    {
        _requestContextProvider = requestContextProvider;
        _activableDataFilter = activableDataFilter;
        _multiProductObjectDataFilter = multiProductObjectDataFilter;
    }
    private void AddQueryFilters<TEntity>(EntityTypeBuilder<TEntity> entityType) where TEntity : class
    {
        if (typeof(IDeleteAuditedEntity).IsAssignableFrom(typeof(TEntity)))
            entityType.AddQueryFilter<IDeleteAuditedEntity>(x => !x.IsDeleted);
        if (typeof(IActivable).IsAssignableFrom(typeof(TEntity)))
            entityType.AddQueryFilter<IActivable>(x => x.IsActive || !_activableDataFilter.IsEnabled);
        if (typeof(IMultiProductObject).IsAssignableFrom(typeof(TEntity)))
            entityType.AddQueryFilter<IMultiProductObject>(x => x.Product == _requestContextProvider.Product || !_multiProductObjectDataFilter.IsEnabled);
    }

    public void Configure(EntityTypeBuilder<Location> builder)
    {
        builder.ConfigureByConvention();
        AddQueryFilters(builder);
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
        
        // LocationTypeName might not exist in existing database - ignore it for now
        builder.Ignore(x => x.LocationTypeName);
        // If your database has this column, comment out the line above and uncomment below:
        // builder.Property(x => x.LocationTypeName)
        //     .HasMaxLength(LocationsConstants.LocationTypeNameMaxLength);
        
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
        
        // Coordinate properties
        builder.Property(x => x.Latitude)
            .HasColumnType("DECIMAL(10,8)")
            .IsRequired(false);
            
        builder.Property(x => x.Longitude)
            .HasColumnType("DECIMAL(11,8)")
            .IsRequired(false);
            
        builder.Property(x => x.GeofenceRadius)
            .HasColumnType("FLOAT")
            .IsRequired(false);
            
        // Computed spatial column (read-only)
        builder.Property(x => x.ComputedCoordinates)
            .HasColumnType("GEOGRAPHY")
            .HasComputedColumnSql("CASE WHEN [Latitude] IS NOT NULL AND [Longitude] IS NOT NULL THEN geography::Point([Latitude], [Longitude], 4326) ELSE NULL END", stored: true)
            .ValueGeneratedOnAddOrUpdate();
        
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
        
        // Keep the properties that likely exist in your database
        // (these are typically part of basic audit patterns)
        // builder.Property(x => x.CreatedAt); // configured by base class
        // builder.Property(x => x.CreatedBy); // configured by base class
        // builder.Property(x => x.UpdatedAt); // configured by base class
        // builder.Property(x => x.UpdatedBy); // configured by base class
        // builder.Property(x => x.IsActive); // configured by base class
    }
}