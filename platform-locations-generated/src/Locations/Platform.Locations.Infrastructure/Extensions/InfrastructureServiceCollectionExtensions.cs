using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Platform.Locations.Domain.Locations;
using Platform.Locations.Infrastructure.Data;
using Platform.Locations.Infrastructure.Repositories;

namespace Platform.Locations.Infrastructure.Extensions;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddLocationInfrastructure(this IServiceCollection services)
    {
        // Infrastructure services that don't depend on specific data store
        
        return services;
    }

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
