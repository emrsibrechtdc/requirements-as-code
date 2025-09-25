using Platform.Locations.Application.Locations.Dtos;
using Platform.Shared.Cqrs.Mediatr;

namespace Platform.Locations.Application.Locations.Queries;

public record GetLocationByCoordinatesQuery(
    decimal Latitude, 
    decimal Longitude
) : IQuery<LocationDto?>;