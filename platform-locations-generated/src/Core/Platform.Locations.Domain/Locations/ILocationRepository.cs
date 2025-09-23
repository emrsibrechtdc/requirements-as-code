using Platform.Shared.DataLayer;

namespace Platform.Locations.Domain.Locations;

public interface ILocationRepository : IRepository<Location, Guid>
{
    Task<Location?> GetByLocationCodeAsync(string locationCode, CancellationToken cancellationToken = default);
    Task<IEnumerable<Location>> GetByLocationCodeStartsWithAsync(string locationCodePrefix, CancellationToken cancellationToken = default);
    Task<bool> ExistsByLocationCodeAsync(string locationCode, CancellationToken cancellationToken = default);
}