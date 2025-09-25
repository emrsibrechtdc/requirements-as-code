using Microsoft.Extensions.DependencyInjection;
using FluentValidation;
using System.Reflection;
using Platform.Shared.IntegrationEvents.Extensions;
using Platform.Shared.MultiProduct;
using Platform.Shared.RequestContext;

namespace Platform.Customers.Application.Extensions;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddCustomersApplication(this IServiceCollection services)
    {
        // Add Platform.Shared integration events services
        services.AddIntegrationEventsServices();
        services.AddScoped<IMultiProductRequestContextProvider, MultiProductRequestContextProvider>();
        services.AddScoped<IRequestContextProvider>((a) => a.GetService<IMultiProductRequestContextProvider>()!);

        // Add AutoMapper with profiles from this assembly
        services.AddAutoMapper(Assembly.GetExecutingAssembly());

        // Add FluentValidation validators
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        // Add MediatR with CQRS handlers from this assembly
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));

        return services;
    }
}