using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Platform.Locations.Application.Locations.Validators;
using Platform.Shared.IntegrationEvents.Extensions;
using System.Reflection;

namespace Platform.Locations.Application.Extensions;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddLocationApplication(this IServiceCollection services)
    {
        // Add Platform.Shared integration events services
        services.AddIntegrationEventsServices();
        
        // Add MediatR handlers from this assembly
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));
        
        // Add AutoMapper profiles from this assembly
        services.AddAutoMapper(Assembly.GetExecutingAssembly());
        
        // Add FluentValidation validators
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
        
        return services;
    }
}