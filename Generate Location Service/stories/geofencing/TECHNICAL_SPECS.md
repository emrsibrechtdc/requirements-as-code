# Geofencing Technical Specifications

## Database Schema Changes

### New Columns for Locations Table

```sql
-- Add coordinate and geofencing columns to existing Locations table
ALTER TABLE [dbo].[Locations] ADD 
    [Latitude] DECIMAL(10, 8) NULL,           -- -90.00000000 to +90.00000000
    [Longitude] DECIMAL(11, 8) NULL,          -- -180.00000000 to +180.00000000  
    [GeofenceRadius] FLOAT NULL;              -- radius in meters, NULL = no geofence

-- Add computed column for SQL Server spatial operations
ALTER TABLE [dbo].[Locations] ADD 
    [ComputedCoordinates] AS (
        CASE 
            WHEN [Latitude] IS NOT NULL AND [Longitude] IS NOT NULL 
            THEN geography::Point([Latitude], [Longitude], 4326)
            ELSE NULL 
        END
    ) PERSISTED;
```

### Spatial Indexes

```sql
-- Primary spatial index for coordinate-based queries
CREATE SPATIAL INDEX [IX_Locations_ComputedCoordinates] 
ON [dbo].[Locations] ([ComputedCoordinates])
USING GEOGRAPHY_GRID
WITH (
    GRIDS = (LEVEL_1 = MEDIUM, LEVEL_2 = MEDIUM, LEVEL_3 = MEDIUM, LEVEL_4 = MEDIUM),
    CELLS_PER_OBJECT = 16,
    PAD_INDEX = OFF,
    STATISTICS_NORECOMPUTE = OFF,
    SORT_IN_TEMPDB = OFF,
    DROP_EXISTING = OFF,
    ONLINE = OFF,
    ALLOW_ROW_LOCKS = ON,
    ALLOW_PAGE_LOCKS = ON
);

-- Composite index for product-filtered coordinate queries
CREATE INDEX [IX_Locations_Product_Coordinates] 
ON [dbo].[Locations] ([Product], [Latitude], [Longitude], [GeofenceRadius])
WHERE [DeletedAt] IS NULL AND [Latitude] IS NOT NULL AND [Longitude] IS NOT NULL;

-- Index for finding locations with coordinates within product
CREATE INDEX [IX_Locations_Product_HasCoordinates] 
ON [dbo].[Locations] ([Product], [IsActive])
WHERE [DeletedAt] IS NULL AND [Latitude] IS NOT NULL;
```

## Entity Framework Configuration

### LocationConfiguration.cs Updates

```csharp
public class LocationConfiguration : IEntityTypeConfiguration<Location>
{
    public void Configure(EntityTypeBuilder<Location> builder)
    {
        // Existing configuration...
        
        // Coordinate properties
        builder.Property(e => e.Latitude)
            .HasColumnType("DECIMAL(10,8)")
            .IsRequired(false);
            
        builder.Property(e => e.Longitude)
            .HasColumnType("DECIMAL(11,8)")
            .IsRequired(false);
            
        builder.Property(e => e.GeofenceRadius)
            .HasColumnType("FLOAT")
            .IsRequired(false);
            
        // Computed spatial column (read-only)
        builder.Property(e => e.ComputedCoordinates)
            .HasColumnType("GEOGRAPHY")
            .HasComputedColumnSql("CASE WHEN [Latitude] IS NOT NULL AND [Longitude] IS NOT NULL THEN geography::Point([Latitude], [Longitude], 4326) ELSE NULL END", stored: true)
            .ValueGeneratedOnAddOrUpdate();
    }
}
```

## Domain Model Extensions

### Location.cs Coordinate Properties

```csharp
public class Location : FullyAuditedActivableAggregateRoot<Guid>
{
    // Existing properties...
    
    // Coordinate properties
    public decimal? Latitude { get; private set; }
    public decimal? Longitude { get; private set; }
    public double? GeofenceRadius { get; private set; }
    
    // Read-only computed property (managed by database)
    public object? ComputedCoordinates { get; private set; }
    
    // Coordinate business logic methods
    public void SetCoordinates(decimal latitude, decimal longitude, double? geofenceRadius = null)
    {
        ValidateCoordinates(latitude, longitude, geofenceRadius);
        
        Latitude = latitude;
        Longitude = longitude;
        GeofenceRadius = geofenceRadius;
    }
    
    public void ClearCoordinates()
    {
        Latitude = null;
        Longitude = null;
        GeofenceRadius = null;
    }
    
    public bool HasCoordinates => Latitude.HasValue && Longitude.HasValue;
    
    public bool HasGeofence => HasCoordinates && GeofenceRadius.HasValue && GeofenceRadius > 0;
    
    // Validation method
    private static void ValidateCoordinates(decimal latitude, decimal longitude, double? geofenceRadius)
    {
        if (latitude < -90 || latitude > 90)
            throw new ArgumentException("Latitude must be between -90 and +90 degrees", nameof(latitude));
            
        if (longitude < -180 || longitude > 180)
            throw new ArgumentException("Longitude must be between -180 and +180 degrees", nameof(longitude));
            
        if (geofenceRadius.HasValue && geofenceRadius <= 0)
            throw new ArgumentException("Geofence radius must be positive", nameof(geofenceRadius));
    }
    
    // Distance calculation (approximate, for business logic - use database spatial functions for queries)
    public double ApproximateDistanceTo(decimal latitude, decimal longitude)
    {
        if (!HasCoordinates)
            throw new InvalidOperationException("Location does not have coordinates");
            
        // Haversine formula approximation (in meters)
        const double earthRadius = 6371000; // meters
        var dLat = DegreesToRadians((double)(latitude - Latitude.Value));
        var dLng = DegreesToRadians((double)(longitude - Longitude.Value));
        
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(DegreesToRadians((double)Latitude.Value)) * 
                Math.Cos(DegreesToRadians((double)latitude)) *
                Math.Sin(dLng / 2) * Math.Sin(dLng / 2);
                
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return earthRadius * c;
    }
    
    private static double DegreesToRadians(double degrees) => degrees * Math.PI / 180;
    
    // Update instrumentation to include coordinates
    public Dictionary<string, string> ToInstrumentationProperties()
    {
        var properties = new Dictionary<string, string>
        {
            { "locationCode", LocationCode },
            { "locationTypeCode", LocationTypeCode },
            { "city", City },
            { "state", State },
            { "country", Country },
            { "isActive", IsActive.ToString() }
        };
        
        if (HasCoordinates)
        {
            properties.Add("latitude", Latitude.ToString());
            properties.Add("longitude", Longitude.ToString());
        }
        
        if (HasGeofence)
        {
            properties.Add("geofenceRadius", GeofenceRadius.ToString());
        }
        
        return properties;
    }
}
```

## Application Layer Components

### New Query Models

```csharp
// Coordinate-based location lookup
public record GetLocationByCoordinatesQuery(
    decimal Latitude, 
    decimal Longitude, 
    string? Product = null
) : IQuery<LocationDto?>;

// Nearby locations search
public record GetNearbyLocationsQuery(
    decimal Latitude,
    decimal Longitude, 
    double RadiusMeters = 5000,
    int MaxResults = 10,
    string? Product = null
) : IQuery<List<LocationWithDistanceDto>>;

// Update coordinates command
public record UpdateLocationCoordinatesCommand(
    string LocationCode,
    decimal Latitude,
    decimal Longitude,
    double? GeofenceRadius = null
) : ICommand<LocationResponse>;
```

### Extended DTOs

```csharp
// Enhanced LocationDto with coordinates
public record LocationDto(
    Guid Id,
    string LocationCode,
    string LocationTypeCode,
    string? LocationTypeName,
    string AddressLine1,
    string? AddressLine2,
    string City,
    string State,
    string ZipCode,
    string Country,
    bool IsActive,
    DateTimeOffset CreatedAt,
    string? CreatedBy,
    DateTimeOffset? UpdatedAt,
    string? UpdatedBy,
    // New coordinate fields
    decimal? Latitude,
    decimal? Longitude,
    double? GeofenceRadius
);

// New DTO for nearby search results
public record LocationWithDistanceDto(
    Guid Id,
    string LocationCode,
    string LocationTypeCode,
    string? LocationTypeName,
    string AddressLine1,
    string? AddressLine2,
    string City,
    string State,
    string ZipCode,
    string Country,
    bool IsActive,
    decimal? Latitude,
    decimal? Longitude,
    double? GeofenceRadius,
    double DistanceMeters  // Distance from search point
);

// Coordinates update DTO
public record CoordinatesDto(
    decimal Latitude,
    decimal Longitude,
    double? GeofenceRadius = null
);
```

## Repository Extensions

### Spatial Query Methods

```csharp
public interface ILocationRepository : IRepository<Location, Guid>
{
    // Existing methods...
    
    // New coordinate-based methods
    Task<Location?> GetLocationByCoordinatesAsync(decimal latitude, decimal longitude, string? product = null, CancellationToken cancellationToken = default);
    
    Task<List<Location>> GetNearbyLocationsAsync(decimal latitude, decimal longitude, double radiusMeters, int maxResults, string? product = null, CancellationToken cancellationToken = default);
    
    Task<List<Location>> GetLocationsWithoutCoordinatesAsync(string? product = null, int batchSize = 100, CancellationToken cancellationToken = default);
    
    Task<int> CountLocationsWithCoordinatesAsync(string? product = null, CancellationToken cancellationToken = default);
}
```

### Repository Implementation

```csharp
public class LocationRepository : EfCoreRepository<Location, Guid, LocationsDbContext>, ILocationRepository
{
    public LocationRepository(LocationsDbContext context) : base(context) { }

    public async Task<Location?> GetLocationByCoordinatesAsync(
        decimal latitude, 
        decimal longitude, 
        string? product = null, 
        CancellationToken cancellationToken = default)
    {
        var point = $"POINT({longitude} {latitude})"; // Note: longitude first in WKT
        
        var query = Context.Set<Location>()
            .Where(l => !l.IsDeleted && l.IsActive)
            .Where(l => l.Latitude != null && l.Longitude != null && l.GeofenceRadius != null)
            .Where(l => l.ComputedCoordinates.STDistance(SqlServerDbFunctionsExtensions.Geography.Parse(point)) <= l.GeofenceRadius);
            
        if (!string.IsNullOrEmpty(product))
        {
            query = query.Where(l => l.Product == product);
        }
        
        // If multiple locations contain the point, return the closest to center
        return await query
            .OrderBy(l => l.ComputedCoordinates.STDistance(SqlServerDbFunctionsExtensions.Geography.Parse(point)))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<List<Location>> GetNearbyLocationsAsync(
        decimal latitude, 
        decimal longitude, 
        double radiusMeters, 
        int maxResults, 
        string? product = null, 
        CancellationToken cancellationToken = default)
    {
        var point = $"POINT({longitude} {latitude})";
        
        var query = Context.Set<Location>()
            .Where(l => !l.IsDeleted && l.IsActive)
            .Where(l => l.Latitude != null && l.Longitude != null)
            .Where(l => l.ComputedCoordinates.STDistance(SqlServerDbFunctionsExtensions.Geography.Parse(point)) <= radiusMeters);
            
        if (!string.IsNullOrEmpty(product))
        {
            query = query.Where(l => l.Product == product);
        }
        
        return await query
            .OrderBy(l => l.ComputedCoordinates.STDistance(SqlServerDbFunctionsExtensions.Geography.Parse(point)))
            .Take(maxResults)
            .ToListAsync(cancellationToken);
    }
    
    public async Task<List<Location>> GetLocationsWithoutCoordinatesAsync(
        string? product = null, 
        int batchSize = 100, 
        CancellationToken cancellationToken = default)
    {
        var query = Context.Set<Location>()
            .Where(l => !l.IsDeleted)
            .Where(l => l.Latitude == null || l.Longitude == null);
            
        if (!string.IsNullOrEmpty(product))
        {
            query = query.Where(l => l.Product == product);
        }
        
        return await query
            .Take(batchSize)
            .ToListAsync(cancellationToken);
    }
}
```

## API Endpoint Specifications

### New REST Endpoints

```csharp
// GET /locations/by-coordinates?lat={lat}&lng={lng}&product={product}
public static IEndpointRouteBuilder MapCoordinateLocationRoutes(this IEndpointRouteBuilder endpoints, ApiVersionSet apiVersionSet, bool authorizationRequired)
{
    var getLocationByCoordinates = endpoints.MapGet("/locations/by-coordinates", 
        async (HttpContext context, 
               [FromQuery] decimal latitude, 
               [FromQuery] decimal longitude, 
               [FromQuery] string? product, 
               ISender sender, 
               CancellationToken cancellationToken) =>
    {
        var query = new GetLocationByCoordinatesQuery(latitude, longitude, product);
        var result = await sender.Send(query, cancellationToken);
        return result != null ? TypedResults.Ok(result) : TypedResults.NotFound();
    })
    .WithApiVersionSet(apiVersionSet)
    .MapToApiVersion(1.0)
    .WithSummary("Get location containing specific coordinates")
    .WithDescription("Returns the location that contains the given coordinates within its geofence boundary")
    .Produces<LocationDto>(StatusCodes.Status200OK)
    .Produces(StatusCodes.Status404NotFound)
    .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
    .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

    var getNearbyLocations = endpoints.MapGet("/locations/nearby",
        async (HttpContext context,
               [FromQuery] decimal latitude,
               [FromQuery] decimal longitude,
               [FromQuery] double radiusMeters,
               [FromQuery] int maxResults,
               [FromQuery] string? product,
               ISender sender,
               CancellationToken cancellationToken) =>
    {
        var query = new GetNearbyLocationsQuery(latitude, longitude, radiusMeters, maxResults, product);
        var result = await sender.Send(query, cancellationToken);
        return TypedResults.Ok(result);
    })
    .WithApiVersionSet(apiVersionSet)
    .MapToApiVersion(1.0)
    .WithSummary("Get nearby locations within radius")
    .WithDescription("Returns locations within specified radius, ordered by distance")
    .Produces<List<LocationWithDistanceDto>>(StatusCodes.Status200OK)
    .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);

    // Apply authorization if required
    if (authorizationRequired)
    {
        getLocationByCoordinates.RequireAuthorization(builder =>
        {
            builder.RequireRole("Locations.Read");
        });
        
        getNearbyLocations.RequireAuthorization(builder =>
        {
            builder.RequireRole("Locations.Read");
        });
    }

    return endpoints;
}
```

## Configuration Requirements

### Azure Maps Configuration

```json
{
  "AzureMaps": {
    "ApiKey": "{{AZURE_MAPS_API_KEY}}",
    "BaseUrl": "https://atlas.microsoft.com",
    "SearchEndpoint": "/search/address/json",
    "ReverseGeocodeEndpoint": "/search/address/reverse/json", 
    "MaxRetries": 3,
    "TimeoutSeconds": 30,
    "CacheExpirationHours": 24,
    "RateLimitPerSecond": 10
  },
  "Geofencing": {
    "DefaultGeofenceRadius": 100.0,
    "MaxGeofenceRadius": 50000.0,
    "MaxNearbySearchRadius": 100000.0,
    "MaxNearbyResults": 100,
    "CoordinatePopulation": {
      "BatchSize": 50,
      "DelayBetweenBatchesMs": 1000,
      "MaxConcurrentRequests": 5
    }
  }
}
```

## Performance Considerations

### Query Optimization Tips

1. **Spatial Index Maintenance**
   - Monitor spatial index fragmentation
   - Consider rebuild during low-traffic periods
   - Adjust grid density based on data distribution

2. **Query Patterns**
   - Always include Product filter for multi-tenant isolation
   - Use appropriate distance units (meters recommended)
   - Limit result sets with TOP/LIMIT clauses

3. **Caching Strategy**
   - Cache frequently accessed location coordinates
   - Cache geocoding results to minimize API calls
   - Use Redis for distributed coordinate caching

### Monitoring Metrics

- Spatial query execution time (p95 < 100ms target)
- Spatial index seek vs scan ratio (>90% seeks)
- Geocoding API success rate and latency
- Coordinate data coverage percentage
- Geofence false positive/negative rates