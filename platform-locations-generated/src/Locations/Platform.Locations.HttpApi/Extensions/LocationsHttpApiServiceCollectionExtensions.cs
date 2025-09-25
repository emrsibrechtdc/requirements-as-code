using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Hosting;
using Platform.Shared.HttpApi.Extensions;

namespace Platform.Locations.HttpApi.Extensions;

public static class LocationsHttpApiServiceCollectionExtensions
{
    public static IServiceCollection AddLocationHttpApi(this IServiceCollection services, IConfiguration configuration, IConfigurationBuilder configurationBuilder, IWebHostEnvironment environment)
    {
        // Add Platform.Shared HTTP API services
        services.AddPlatformCommonHttpApi(configuration, configurationBuilder, environment, "Locations")
               .WithAuditing()
               .WithMultiProduct();

        services.AddHealthChecks()
                          .AddSqlServer(configuration.GetConnectionString("LocationDb")!);

        // HTTP API specific services can be added here
        // Currently no additional services needed beyond what's provided by Platform.Shared

        return services;
    }
}