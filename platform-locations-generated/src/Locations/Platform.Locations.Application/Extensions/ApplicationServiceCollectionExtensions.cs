using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Platform.Common.Messaging;
using Platform.Locations.Application.IntegrationEvents;
using Platform.Locations.Application.Locations.Commands;
using Platform.Locations.Application.Locations.Validators;
using Platform.Shared.Cqrs.Mediatr;
using Platform.Shared.IntegrationEvents.Extensions;
using Platform.Shared.MultiProduct;
using Platform.Shared.RequestContext;
using System.Reflection;

namespace Platform.Locations.Application.Extensions;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddLocationApplication(this IServiceCollection services)
    {
        // Add Platform.Shared integration events services
        services.AddIntegrationEventsServices();
        // create the MessageSerializer
        services.AddSingleton<IMessageTypeRegistry>(_ => new MessageTypeRegistry(_.GetRequiredService<ILogger<MessageTypeRegistry>>(), typeof(LocationRegisteredIntegrationEvent).Assembly));
        services.AddSingleton<IMessageSerializer>(_ => new MessageSerializer(
                        _.GetRequiredService<IMessageTypeRegistry>(),
                        _.GetRequiredService<ILogger<MessageSerializer>>(),
                        // necessary to support both camel and pascal case for now
                        // when all services are deployed, the custom JsonSerializerOptions can be removed and the default JsonSerializerOptions of Platform.Common can be used
                        // the default JsonSerializerOptions of Platform.Common only supports camel case
                        new System.Text.Json.JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true,
                            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
                        }));
        services.AddScoped<IMultiProductRequestContextProvider, MultiProductRequestContextProvider>();
        services.AddScoped<IRequestContextProvider>((a) => a.GetService<IMultiProductRequestContextProvider>()!);
        // Add MediatR handlers from this assembly
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(RegisterLocationCommand).Assembly);
            }).AddScoped(typeof(IPipelineBehavior<,>), typeof(TransactionBehavior<,>));
        
        // Add AutoMapper profiles from this assembly
        services.AddAutoMapper(Assembly.GetExecutingAssembly());
        
        // Add FluentValidation validators
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
        
        return services;
    }
}