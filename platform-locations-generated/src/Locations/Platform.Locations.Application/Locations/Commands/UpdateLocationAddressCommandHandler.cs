using AutoMapper;
using FluentValidation;
using Platform.Locations.Application.Locations.Dtos;
using Platform.Locations.Application.IntegrationEvents;
using Platform.Locations.Domain.Locations;
using Platform.Shared.Cqrs.Mediatr;
using Platform.Shared.IntegrationEvents;

namespace Platform.Locations.Application.Locations.Commands;

public class UpdateLocationAddressCommandHandler : ICommandHandler<UpdateLocationAddressCommand, LocationResponse>
{
    private readonly ILocationRepository _locationRepository;
    private readonly IValidator<UpdateLocationAddressCommand> _validator;
    private readonly IMapper _mapper;
    private readonly IIntegrationEventPublisher _eventPublisher;

    public UpdateLocationAddressCommandHandler(
        ILocationRepository locationRepository,
        IValidator<UpdateLocationAddressCommand> validator,
        IMapper mapper,
        IIntegrationEventPublisher eventPublisher)
    {
        _locationRepository = locationRepository;
        _validator = validator;
        _mapper = mapper;
        _eventPublisher = eventPublisher;
    }

    public async Task<LocationResponse> Handle(UpdateLocationAddressCommand request, CancellationToken cancellationToken)
    {
        // Input validation
        _validator.ValidateAndThrow(request);
        
        // Get existing location
        var location = await _locationRepository.GetByLocationCodeAsync(request.LocationCode, cancellationToken);
        if (location == null)
        {
            throw new LocationNotFoundException(request.LocationCode);
        }
        
        // Update address using domain business logic
        location.UpdateAddress(
            request.AddressLine1!,
            request.AddressLine2,
            request.City!,
            request.State!,
            request.ZipCode!,
            request.Country!);
        
        // Platform.Shared automatically handles:
        // - Setting audit fields (UpdatedAt, UpdatedBy)
        // - Database transaction management
        await _locationRepository.UpdateAsync(location, cancellationToken);
        
        // Publish integration event
        _eventPublisher.SaveIntegrationEvent(
            new LocationAddressUpdatedIntegrationEvent(
                location.LocationCode,
                location.AddressLine1,
                location.AddressLine2,
                location.City,
                location.State,
                location.ZipCode,
                location.Country,
                location.UpdatedAt?.DateTime ?? DateTime.UtcNow));
        
        return _mapper.Map<LocationResponse>(location);
    }
}