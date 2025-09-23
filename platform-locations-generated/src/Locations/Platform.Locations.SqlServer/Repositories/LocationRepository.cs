using Microsoft.EntityFrameworkCore;
using Platform.Locations.Domain.Locations;
using Platform.Locations.SqlServer.Data;
using Platform.Shared.EntityFrameworkCore;

namespace Platform.Locations.SqlServer.Repositories;

public class LocationRepository : EfCoreRepository<Location, Guid, LocationsDbContext>, ILocationRepository
{
    public LocationRepository(LocationsDbContext context) : base(context)
    {
    }

    public async Task<Location?> GetByLocationCodeAsync(string locationCode, CancellationToken cancellationToken = default)
    {
        // Platform.Shared automatically applies product filtering
        return await GetQueryable()
            .FirstOrDefaultAsync(x => x.LocationCode == locationCode, cancellationToken);
    }

    public async Task<IEnumerable<Location>> GetByLocationCodeStartsWithAsync(string locationCodePrefix, CancellationToken cancellationToken = default)
    {
        // Platform.Shared automatically applies product filtering
        return await GetQueryable()
            .Where(x => x.LocationCode.StartsWith(locationCodePrefix))
            .OrderBy(x => x.LocationCode)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsByLocationCodeAsync(string locationCode, CancellationToken cancellationToken = default)
    {
        // Platform.Shared automatically applies product filtering
        return await GetQueryable()
            .AnyAsync(x => x.LocationCode == locationCode, cancellationToken);
    }
}