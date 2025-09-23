using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Platform.Locations.Domain.Locations;
using Platform.Locations.SqlServer.Data;
using Platform.Locations.SqlServer.Repositories;

namespace Platform.Locations.SqlServer.Extensions;

public static class SqlServerServiceCollectionExtensions
{
    public static IServiceCollection AddLocationSqlServer(this IServiceCollection services, string connectionString)
    {
        // Add Entity Framework DbContext with Platform.Shared configuration
        services.AddDbContext<LocationsDbContext>(options =>
        {
            options.UseSqlServer(connectionString);
        });

        // Register repository implementations
        services.AddScoped<ILocationRepository, LocationRepository>();

        return services;
    }
}