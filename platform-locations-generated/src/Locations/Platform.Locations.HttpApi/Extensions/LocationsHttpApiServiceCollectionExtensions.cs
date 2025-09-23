using Microsoft.Extensions.DependencyInjection;

namespace Platform.Locations.HttpApi.Extensions;

public static class LocationsHttpApiServiceCollectionExtensions
{
    public static IServiceCollection AddLocationHttpApi(this IServiceCollection services)
    {
        // HTTP API specific services can be added here
        // Currently no additional services needed beyond what's provided by Platform.Shared
        
        return services;
    }
}