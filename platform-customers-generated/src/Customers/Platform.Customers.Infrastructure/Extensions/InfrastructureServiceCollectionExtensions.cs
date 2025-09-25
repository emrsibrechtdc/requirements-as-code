using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Platform.Customers.Domain.Customers;
using Platform.Customers.Infrastructure.Data;
using Platform.Customers.Infrastructure.Repositories;

namespace Platform.Customers.Infrastructure.Extensions;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddCustomersInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
        ?? "Server=(localdb)\\mssqllocaldb;Database=Platform.Customers;Trusted_Connection=true;MultipleActiveResultSets=true";

        // Add Entity Framework DbContext
        services.AddDbContext<CustomersDbContext>(options =>
            options.UseSqlServer(connectionString));

        // Register domain-specific repositories (not Platform.Shared generics)
        services.AddScoped<ICustomerRepository, CustomerRepository>();

        services.AddCustomersIntegrationEvents(configuration);

        return services;
    }
}