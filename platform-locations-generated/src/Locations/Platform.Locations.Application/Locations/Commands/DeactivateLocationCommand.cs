using Platform.Locations.Application.Locations.Dtos;
using Platform.Shared.Cqrs;

namespace Platform.Locations.Application.Locations.Commands;

public record DeactivateLocationCommand
(
    string LocationCode
) : ICommand<LocationResponse>;