using Microsoft.Extensions.DependencyInjection;
using Platform.Locations.Domain.Locations;

namespace Platform.Locations.Infrastructure.Extensions;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddLocationInfrastructure(this IServiceCollection services)
    {
        // Repository implementations will be registered by SqlServer project
        // This is a placeholder for any infrastructure services that don't depend on specific data store
        
        return services;
    }
}