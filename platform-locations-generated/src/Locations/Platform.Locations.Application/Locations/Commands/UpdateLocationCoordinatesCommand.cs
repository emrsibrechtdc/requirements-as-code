using Platform.Locations.Application.Locations.Dtos;
using Platform.Shared.Cqrs.Mediatr;

namespace Platform.Locations.Application.Locations.Commands;

public record UpdateLocationCoordinatesCommand(
    string LocationCode,
    decimal Latitude,
    decimal Longitude,
    double? GeofenceRadius = null
) : ICommand<LocationResponse>;