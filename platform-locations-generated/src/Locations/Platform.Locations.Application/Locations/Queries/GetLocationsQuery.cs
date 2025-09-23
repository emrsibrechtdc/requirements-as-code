using Platform.Locations.Application.Locations.Dtos;
using Platform.Shared.Cqrs.Mediatr;

namespace Platform.Locations.Application.Locations.Queries;

public record GetLocationsQuery
(
    string? LocationCode
) : IQuery<List<LocationDto>>;