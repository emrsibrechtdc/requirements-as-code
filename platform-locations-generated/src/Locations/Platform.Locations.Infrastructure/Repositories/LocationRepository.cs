using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Platform.Locations.Domain.Locations;
using Platform.Locations.Infrastructure.Data;
using Platform.Shared.EntityFrameworkCore;

namespace Platform.Locations.Infrastructure.Repositories;

public class LocationRepository : EfCoreRepository<Location, Guid, LocationsDbContext>, ILocationRepository
{
    private readonly ILogger<LocationRepository> _logger;
    
    public LocationRepository(LocationsDbContext context, ILogger<LocationRepository> logger) : base(context)
    {
        _logger = logger;
    }

    public async Task<Location?> GetByLocationCodeAsync(string locationCode, CancellationToken cancellationToken = default)
    {
        // Platform.Shared automatically applies product filtering
        return await DbContext.Set<Location>()
            .FirstOrDefaultAsync(x => x.LocationCode == locationCode, cancellationToken);
    }

    public async Task<IEnumerable<Location>> GetByLocationCodeStartsWithAsync(string locationCodePrefix, CancellationToken cancellationToken = default)
    {
        // Platform.Shared automatically applies product filtering
        return await DbContext.Set<Location>()
            .Where(x => x.LocationCode.StartsWith(locationCodePrefix))
            .OrderBy(x => x.LocationCode)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsByLocationCodeAsync(string locationCode, CancellationToken cancellationToken = default)
    {
        // Platform.Shared automatically applies product filtering
        return await DbContext.Set<Location>()
            .AnyAsync(x => x.LocationCode == locationCode, cancellationToken);
    }
    
    public async Task<Location?> GetLocationByCoordinatesAsync(decimal latitude, decimal longitude, CancellationToken cancellationToken = default)
    {
        
        // Use hybrid approach: raw SQL to find candidate IDs, then LINQ for Platform.Shared filtering
        // This works around EF Core parameter precision issues with spatial queries
        
        // Build SQL string with literal values to avoid parameter precision truncation
        var sqlQuery = $@"
                SELECT TOP(1) l.Id
                FROM [Locations] l
                WHERE l.[Latitude] IS NOT NULL 
                  AND l.[Longitude] IS NOT NULL 
                  AND l.[GeofenceRadius] IS NOT NULL
                  AND l.[DeletedAt] IS NULL
                  AND l.[IsActive] = 1
                  AND l.[ComputedCoordinates].STDistance(geography::Point({latitude:F8}, {longitude:F8}, 4326)) <= l.[GeofenceRadius]
                ORDER BY l.[ComputedCoordinates].STDistance(geography::Point({latitude:F8}, {longitude:F8}, 4326))";
        
        var candidateIds = await DbContext.Database
            .SqlQueryRaw<Guid>(sqlQuery)
            .ToListAsync(cancellationToken);
            
        if (!candidateIds.Any())
        {
            return null;
        }
        
        // Query using regular LINQ to apply Platform.Shared filters properly
        var location = await DbContext.Set<Location>()
            .Where(l => candidateIds.Contains(l.Id))
            .FirstOrDefaultAsync(cancellationToken);
            
        return location;
    }
    public async Task<IEnumerable<Location>> GetNearbyLocationsAsync(decimal latitude, decimal longitude, double radiusMeters, int maxResults, CancellationToken cancellationToken = default)
    {
        // Use hybrid approach: raw SQL to find candidate IDs, then LINQ for Platform.Shared filtering
        // This works around EF Core parameter precision issues with spatial queries
        
        // Build SQL string with literal values to avoid parameter precision truncation
        var sqlQuery = $@"
                SELECT TOP({maxResults}) l.Id
                FROM [Locations] l
                WHERE l.[Latitude] IS NOT NULL 
                  AND l.[Longitude] IS NOT NULL 
                  AND l.[DeletedAt] IS NULL
                  AND l.[IsActive] = 1
                  AND l.[ComputedCoordinates].STDistance(geography::Point({latitude:F8}, {longitude:F8}, 4326)) <= {radiusMeters}
                ORDER BY l.[ComputedCoordinates].STDistance(geography::Point({latitude:F8}, {longitude:F8}, 4326))";
        
        var candidateIds = await DbContext.Database
            .SqlQueryRaw<Guid>(sqlQuery)
            .ToListAsync(cancellationToken);
            
        if (!candidateIds.Any())
        {
            return new List<Location>();
        }
        
        // Query using regular LINQ to apply Platform.Shared filters properly
        // Maintain the distance-based ordering by getting locations in ID order
        var locations = new List<Location>();
        foreach (var id in candidateIds)
        {
            var location = await DbContext.Set<Location>()
                .FirstOrDefaultAsync(l => l.Id == id, cancellationToken);
            if (location != null)
            {
                locations.Add(location);
            }
        }
        
        return locations;
    }
    
    public async Task<IEnumerable<Location>> GetLocationsWithoutCoordinatesAsync(int batchSize = 100, CancellationToken cancellationToken = default)
    {
        // Platform.Shared automatically applies product filtering through data filters
        return await DbContext.Set<Location>()
            .Where(l => l.Latitude == null || l.Longitude == null)
            .Take(batchSize)
            .ToListAsync(cancellationToken);
    }
    
    public async Task<int> CountLocationsWithCoordinatesAsync(CancellationToken cancellationToken = default)
    {
        // Platform.Shared automatically applies product filtering through data filters
        return await DbContext.Set<Location>()
            .CountAsync(l => l.Latitude != null && l.Longitude != null, cancellationToken);
    }
}