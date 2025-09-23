using Platform.Locations.Application.Locations.Dtos;
using Platform.Shared.Cqrs.Mediatr;

namespace Platform.Locations.Application.Locations.Commands;

public record DeleteLocationCommand
(
    string LocationCode
) : ICommand<LocationResponse>;