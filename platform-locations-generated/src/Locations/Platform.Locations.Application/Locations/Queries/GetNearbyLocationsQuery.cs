using Platform.Locations.Application.Locations.Dtos;
using Platform.Shared.Cqrs.Mediatr;

namespace Platform.Locations.Application.Locations.Queries;

public record GetNearbyLocationsQuery(
    decimal Latitude,
    decimal Longitude, 
    double RadiusMeters = 5000,
    int MaxResults = 10
) : IQuery<IEnumerable<LocationWithDistanceDto>>;