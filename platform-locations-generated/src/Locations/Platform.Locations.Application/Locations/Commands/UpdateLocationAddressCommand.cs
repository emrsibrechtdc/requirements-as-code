using Platform.Locations.Application.Locations.Dtos;
using Platform.Shared.Cqrs.Mediatr;

namespace Platform.Locations.Application.Locations.Commands;

public record UpdateLocationAddressCommand
(
    string LocationCode,
    string? AddressLine1,
    string? AddressLine2,
    string? City,
    string? State,
    string? ZipCode,
    string? Country
) : ICommand<LocationResponse>;