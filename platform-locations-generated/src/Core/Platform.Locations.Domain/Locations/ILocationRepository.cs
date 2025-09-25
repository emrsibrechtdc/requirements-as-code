using Platform.Shared.DataLayer.Repositories;

namespace Platform.Locations.Domain.Locations;

public interface ILocationRepository : IRepository<Location, Guid>
{
    Task<Location?> GetByLocationCodeAsync(string locationCode, CancellationToken cancellationToken = default);
    Task<IEnumerable<Location>> GetByLocationCodeStartsWithAsync(string locationCodePrefix, CancellationToken cancellationToken = default);
    Task<bool> ExistsByLocationCodeAsync(string locationCode, CancellationToken cancellationToken = default);
    
    // Spatial query methods for geofencing
    Task<Location?> GetLocationByCoordinatesAsync(decimal latitude, decimal longitude, CancellationToken cancellationToken = default);
    Task<IEnumerable<Location>> GetNearbyLocationsAsync(decimal latitude, decimal longitude, double radiusMeters, int maxResults, CancellationToken cancellationToken = default);
    Task<IEnumerable<Location>> GetLocationsWithoutCoordinatesAsync(int batchSize = 100, CancellationToken cancellationToken = default);
    Task<int> CountLocationsWithCoordinatesAsync(CancellationToken cancellationToken = default);
}