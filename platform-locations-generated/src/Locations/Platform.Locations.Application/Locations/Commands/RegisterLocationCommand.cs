using Platform.Locations.Application.Locations.Dtos;
using Platform.Shared.Cqrs.Mediatr;

namespace Platform.Locations.Application.Locations.Commands;

public record RegisterLocationCommand
(
    string? LocationCode,
    string? LocationTypeCode,
    string? AddressLine1,
    string? AddressLine2,
    string? City,
    string? State,
    string? ZipCode,
    string? Country,
    decimal? Latitude,
    decimal? Longitude,
    double? GeofenceRadius
) : ICommand<LocationResponse>;
