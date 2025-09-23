using Asp.Versioning;
using Asp.Versioning.Builder;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Platform.Locations.Application.Locations.Commands;
using Platform.Locations.Application.Locations.Dtos;
using Platform.Locations.Application.Locations.Queries;

namespace Platform.Locations.HttpApi.Extensions;

public static class EndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapLocationApiRoutes(this IEndpointRouteBuilder endpoints, ApiVersionSet apiVersionSet, bool authorizationRequired)
    {
        var registerLocation = endpoints.MapPost("/locations/register", async (HttpContext context, RegisterLocationCommand data, ISender sender, CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(data, cancellationToken);
            return TypedResults.Ok(result);

        }).WithApiVersionSet(apiVersionSet)
              .MapToApiVersion(1.0)
              .WithSummary("Register Locations endpoint")
              .Produces<LocationResponse>(StatusCodes.Status200OK)
              .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError)
              .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
              .Produces<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)
              .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
              .Produces<ProblemDetails>(StatusCodes.Status403Forbidden);

        var updateLocationAddress = endpoints.MapPut("/locations/{locationCode}/updateaddress", async (HttpContext context, string locationCode, AddressDto data, ISender sender, CancellationToken cancellationToken) =>
        {
            var command = new UpdateLocationAddressCommand(locationCode, data.AddressLine1, data.AddressLine2, data.City, data.State, data.ZipCode, data.Country);
            var result = await sender.Send(command, cancellationToken);
            return TypedResults.Ok(result);

        }).WithApiVersionSet(apiVersionSet)
              .MapToApiVersion(1.0)
              .WithSummary("Update Location address endpoint")
              .Produces<LocationResponse>(StatusCodes.Status200OK)
              .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError)
              .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
              .Produces<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)
              .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
              .Produces<ProblemDetails>(StatusCodes.Status403Forbidden);

        var getLocations = endpoints.MapGet("/locations", async (HttpContext context, string? locationCode, ISender sender, CancellationToken cancellationToken) =>
        {
            var query = new GetLocationsQuery(locationCode);
            var result = await sender.Send(query, cancellationToken);
            return TypedResults.Ok(result);

        }).WithApiVersionSet(apiVersionSet)
              .MapToApiVersion(1.0)
              .WithSummary("Get Locations endpoint")
              .WithDescription("Get locations matched with starting characters of the Location Code. Minimum 3 characters are required to search for the locations. LocationCode is optional and if not provided then this endpoint will return all the locations associated with the consumer product.")
              .Produces<List<LocationDto>>(StatusCodes.Status200OK)
              .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError)
              .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
              .Produces<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)
              .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
              .Produces<ProblemDetails>(StatusCodes.Status403Forbidden);

        var deleteLocation = endpoints.MapDelete("/locations", async (HttpContext context, string locationCode, ISender sender, CancellationToken cancellationToken) =>
        {
            var command = new DeleteLocationCommand(locationCode);
            var result = await sender.Send(command, cancellationToken);
            return TypedResults.Ok(result);

        }).WithApiVersionSet(apiVersionSet)
              .MapToApiVersion(1.0)
              .WithSummary("Delete Location endpoint")
              .Produces<LocationResponse>(StatusCodes.Status200OK)
              .Produces<LocationResponse>(StatusCodes.Status202Accepted)
              .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError)
              .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
              .Produces<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)
              .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
              .Produces<ProblemDetails>(StatusCodes.Status403Forbidden);

        var activateLocation = endpoints.MapPut("/locations/{locationCode}/activate", async (HttpContext context, string locationCode, ISender sender, CancellationToken cancellationToken) =>
        {
            var command = new ActivateLocationCommand(locationCode);
            var result = await sender.Send(command, cancellationToken);
            return TypedResults.Ok(result);

        }).WithApiVersionSet(apiVersionSet)
              .MapToApiVersion(1.0)
              .WithSummary("Activate Location endpoint")
              .Produces<LocationResponse>(StatusCodes.Status200OK)
              .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError)
              .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
              .Produces<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)
              .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
              .Produces<ProblemDetails>(StatusCodes.Status403Forbidden);

        var deactivateLocation = endpoints.MapPut("/locations/{locationCode}/deactivate", async (HttpContext context, string locationCode, ISender sender, CancellationToken cancellationToken) =>
        {
            var command = new DeactivateLocationCommand(locationCode);
            var result = await sender.Send(command, cancellationToken);
            return TypedResults.Ok(result);

        }).WithApiVersionSet(apiVersionSet)
              .MapToApiVersion(1.0)
              .WithSummary("Deactivate Location endpoint")
              .Produces<LocationResponse>(StatusCodes.Status200OK)
              .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError)
              .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
              .Produces<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)
              .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
              .Produces<ProblemDetails>(StatusCodes.Status403Forbidden);

        if (authorizationRequired)
        {
            registerLocation.RequireAuthorization(builder =>
            {
                builder.RequireRole("Locations.Create");
            });

            updateLocationAddress.RequireAuthorization(builder =>
            {
                builder.RequireRole("Locations.Update");
            });

            getLocations.RequireAuthorization(builder =>
            {
                builder.RequireRole("Locations.Read");
            });

            deleteLocation.RequireAuthorization(builder =>
            {
                builder.RequireRole("Locations.Delete");
            });

            activateLocation.RequireAuthorization(builder =>
            {
                builder.RequireRole("Locations.Update");
            });

            deactivateLocation.RequireAuthorization(builder =>
            {
                builder.RequireRole("Locations.Update");
            });
        }

        return endpoints;
    }
}