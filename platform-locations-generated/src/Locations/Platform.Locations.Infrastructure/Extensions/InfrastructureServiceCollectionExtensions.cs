using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Platform.Locations.Domain.Locations;
using Platform.Locations.Infrastructure.Data;
using Platform.Locations.Infrastructure.Repositories;
using Platform.Shared.DataLayer;
using Platform.Shared.IntegrationEvents;
using Azure.Messaging.EventGrid;
using Microsoft.Extensions.Azure;
using Platform.Common;
using Platform.Common.Messaging;
using Platform.Shared.EntityFrameworkCore;
using EventGrid;

namespace Platform.Locations.Infrastructure.Extensions;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddLocationInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Infrastructure services that don't depend on specific data store
        services.AddLocationSqlServer(configuration);
        services.AddLocationIntegrationEvents(configuration);
        return services;
    }

    public static IServiceCollection AddLocationSqlServer(this IServiceCollection services, IConfiguration configuration)
    {
        // Add Entity Framework DbContext with Platform.Shared configuration
        var connectionString = configuration.GetConnectionString("LocationsDb");
        services.AddDbContext<LocationsDbContext>(options =>
        {
            options.UseSqlServer(connectionString);
        });

        // Register Unit of Work
        services.AddScoped<IUnitOfWork, UnitOfWork<LocationsDbContext>>();

        // Register repository implementations
        services.AddScoped<ILocationRepository, LocationRepository>();

        return services;
    }

    public static IServiceCollection AddLocationIntegrationEvents(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure integration events services
        var eventGridUrl = configuration.GetValue<string>("EventGrid:Url");
        if (!string.IsNullOrEmpty(eventGridUrl))
        {
            services.AddAzureClients(builder =>
            {
                builder.AddEventGridPublisherClient(new Uri(eventGridUrl));
            });
            
            services.AddSingleton<IMessageEnvelopePublisher>(serviceProvider => 
                new EventGridMessageEnvelopePublisher(
                    serviceProvider.GetRequiredService<EventGridPublisherClient>(),
                    serviceProvider.GetRequiredService<IMessageSerializer>(),
                    logger: serviceProvider.GetRequiredService<ILogger<IMessageEnvelopePublisher>>()));

            services.AddSingleton<IMessagePublisher, MessagePublisher>();
        }

        return services;
    }
}
