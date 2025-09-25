using Microsoft.EntityFrameworkCore;
using Platform.Locations.Domain.Locations;
using Platform.Locations.Infrastructure.Data;
using Platform.Shared.EntityFrameworkCore;

namespace Platform.Locations.Infrastructure.Repositories;

public class LocationRepository : EfCoreRepository<Location, Guid, LocationsDbContext>, ILocationRepository
{
    public LocationRepository(LocationsDbContext context) : base(context)
    {
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
        // Platform.Shared automatically applies product filtering through data filters
        // Use raw SQL for spatial operations since EF Core spatial support with computed columns can be complex
        var point = $"POINT({longitude} {latitude})"; // Note: longitude first in WKT
        
        var location = await DbContext.Set<Location>()
            .FromSqlRaw(@"
                SELECT * FROM [Locations] l
                WHERE l.[DeletedAt] IS NULL 
                  AND l.[IsActive] = 1
                  AND l.[Latitude] IS NOT NULL 
                  AND l.[Longitude] IS NOT NULL 
                  AND l.[GeofenceRadius] IS NOT NULL
                  AND l.[ComputedCoordinates].STDistance(geography::Parse({0})) <= l.[GeofenceRadius]
                ORDER BY l.[ComputedCoordinates].STDistance(geography::Parse({0}))", point)
            .FirstOrDefaultAsync(cancellationToken);
            
        return location;
    }
    
    public async Task<IEnumerable<Location>> GetNearbyLocationsAsync(decimal latitude, decimal longitude, double radiusMeters, int maxResults, CancellationToken cancellationToken = default)
    {
        // Platform.Shared automatically applies product filtering through data filters
        var point = $"POINT({longitude} {latitude})";
        
        var locations = await DbContext.Set<Location>()
            .FromSqlRaw(@"
                SELECT TOP({1}) * FROM [Locations] l
                WHERE l.[DeletedAt] IS NULL 
                  AND l.[IsActive] = 1
                  AND l.[Latitude] IS NOT NULL 
                  AND l.[Longitude] IS NOT NULL
                  AND l.[ComputedCoordinates].STDistance(geography::Parse({0})) <= {2}
                ORDER BY l.[ComputedCoordinates].STDistance(geography::Parse({0}))", 
                point, maxResults, radiusMeters)
            .ToListAsync(cancellationToken);
            
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