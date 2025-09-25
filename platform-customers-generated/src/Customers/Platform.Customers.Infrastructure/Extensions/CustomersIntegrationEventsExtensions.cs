using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Azure.Messaging.EventGrid;
using Microsoft.Extensions.Azure;
using Platform.Common.Messaging;
using Platform.Shared.EntityFrameworkCore;
using EventGrid;

namespace Platform.Customers.Infrastructure.Extensions;

public static class CustomersIntegrationEventsExtensions
{
    public static IServiceCollection AddCustomersIntegrationEvents(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure EventGrid client for integration events
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