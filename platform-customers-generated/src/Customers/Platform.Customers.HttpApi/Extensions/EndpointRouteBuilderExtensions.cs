using Asp.Versioning;
using Asp.Versioning.Builder;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Platform.Customers.Application.Customers.Commands;
using Platform.Customers.Application.Customers.Queries;

namespace Platform.Customers.HttpApi.Extensions;

public static class EndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapCustomersApiRoutes(this IEndpointRouteBuilder endpoints, ApiVersionSet apiVersionSet, bool authorizationRequired)
    {
        var createCustomer = endpoints.MapPost("/customers/create", async (HttpContext context, CreateCustomerCommand data, ISender sender, CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(data, cancellationToken);
            return TypedResults.Ok(result);

        }).WithApiVersionSet(apiVersionSet)
              .MapToApiVersion(1.0)
              .WithSummary("Create Customer endpoint")
              .WithDescription("Creates a new customer with the provided information including company details, contact information, and address.")
              .Produces<Platform.Customers.Application.Customers.Dtos.CustomerResponse>(StatusCodes.Status200OK)
              .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError)
              .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
              .Produces<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)
              .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
              .Produces<ProblemDetails>(StatusCodes.Status403Forbidden);

        var updateCustomer = endpoints.MapPut("/customers/{customerCode}/update", async (HttpContext context, string customerCode, UpdateCustomerCommand data, ISender sender, CancellationToken cancellationToken) =>
        {
            // Ensure the customer code in the URL matches the command
            var command = data with { CustomerCode = customerCode };
            var result = await sender.Send(command, cancellationToken);
            return TypedResults.Ok(result);

        }).WithApiVersionSet(apiVersionSet)
              .MapToApiVersion(1.0)
              .WithSummary("Update Customer endpoint")
              .WithDescription("Updates an existing customer's information including company details, contact information, and address.")
              .Produces<Platform.Customers.Application.Customers.Dtos.CustomerResponse>(StatusCodes.Status200OK)
              .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError)
              .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
              .Produces<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)
              .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
              .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
              .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        var getCustomers = endpoints.MapGet("/customers", async (HttpContext context, ISender sender, CancellationToken cancellationToken) =>
        {
            // Extract query parameters manually
            string? customerCode = context.Request.Query["customerCode"];
            string? customerType = context.Request.Query["customerType"];
            string? companyName = context.Request.Query["companyName"];
            bool? isActive = null;
            if (context.Request.Query.TryGetValue("isActive", out var isActiveValue))
            {
                if (bool.TryParse(isActiveValue, out var parsed))
                {
                    isActive = parsed;
                }
            }
            
            var query = new GetCustomersQuery(customerCode, customerType, companyName, isActive);
            var result = await sender.Send(query, cancellationToken);
            return TypedResults.Ok(result);

        }).WithApiVersionSet(apiVersionSet)
              .MapToApiVersion(1.0)
              .WithSummary("Get Customers endpoint")
              .WithDescription("Retrieves customers with optional filtering by customer code, type, company name, and active status. " +
                             "All parameters are optional. Results are automatically filtered by product context.")
              .Produces<List<Platform.Customers.Application.Customers.Dtos.CustomerDto>>(StatusCodes.Status200OK)
              .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError)
              .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
              .Produces<ProblemDetails>(StatusCodes.Status403Forbidden);

        var getCustomerByCode = endpoints.MapGet("/customers/{customerCode}", GetCustomerByCodeHandler).WithApiVersionSet(apiVersionSet)
              .MapToApiVersion(1.0)
              .WithSummary("Get Customer by Code endpoint")
              .WithDescription("Retrieves a specific customer by their customer code.")
              .Produces<Platform.Customers.Application.Customers.Dtos.CustomerDto>(StatusCodes.Status200OK)
              .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError)
              .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
              .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
              .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        if (authorizationRequired)
        {
            createCustomer.RequireAuthorization(builder =>
            {
                builder.RequireRole("Customers.Create");
            });

            updateCustomer.RequireAuthorization(builder =>
            {
                builder.RequireRole("Customers.Update");
            });

            getCustomers.RequireAuthorization(builder =>
            {
                builder.RequireRole("Customers.Read");
            });

            getCustomerByCode.RequireAuthorization(builder =>
            {
                builder.RequireRole("Customers.Read");
            });
        }

        return endpoints;
    }
    
    private static async Task<IResult> GetCustomerByCodeHandler(HttpContext context, string customerCode, ISender sender, CancellationToken cancellationToken)
    {
        var query = new GetCustomersQuery(customerCode, null, null, null);
        var result = await sender.Send(query, cancellationToken);
        var customer = result.FirstOrDefault();
        
        if (customer == null)
        {
            return TypedResults.NotFound();
        }
        
        return TypedResults.Ok(customer);
    }
}
