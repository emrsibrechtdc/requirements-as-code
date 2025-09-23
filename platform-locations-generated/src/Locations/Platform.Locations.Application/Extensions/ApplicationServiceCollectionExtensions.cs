using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Platform.Locations.Application.Locations.Validators;
using System.Reflection;

namespace Platform.Locations.Application.Extensions;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddLocationApplication(this IServiceCollection services)
    {
        // Add MediatR handlers from this assembly
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));
        
        // Add AutoMapper profiles from this assembly
        services.AddAutoMapper(Assembly.GetExecutingAssembly());
        
        // Add FluentValidation validators
        services.AddValidatorsFromAssemblyContaining<RegisterLocationCommandValidator>();
        
        return services;
    }
}