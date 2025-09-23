using AutoMapper;
using FluentValidation;
using Platform.Locations.Application.Locations.Dtos;
using Platform.Locations.Application.IntegrationEvents;
using Platform.Locations.Domain.Locations;
using Platform.Shared.Cqrs;
using Platform.Shared.IntegrationEvents;

namespace Platform.Locations.Application.Locations.Commands;

public class DeactivateLocationCommandHandler : ICommandHandler<DeactivateLocationCommand, LocationResponse>
{
    private readonly ILocationRepository _locationRepository;
    private readonly IValidator<DeactivateLocationCommand> _validator;
    private readonly IMapper _mapper;
    private readonly IIntegrationEventPublisher _eventPublisher;

    public DeactivateLocationCommandHandler(
        ILocationRepository locationRepository,
        IValidator<DeactivateLocationCommand> validator,
        IMapper mapper,
        IIntegrationEventPublisher eventPublisher)
    {
        _locationRepository = locationRepository;
        _validator = validator;
        _mapper = mapper;
        _eventPublisher = eventPublisher;
    }

    public async Task<LocationResponse> Handle(DeactivateLocationCommand request, CancellationToken cancellationToken)
    {
        // Input validation
        _validator.ValidateAndThrow(request);
        
        // Get existing location
        var location = await _locationRepository.GetByLocationCodeAsync(request.LocationCode, cancellationToken);
        if (location == null)
        {
            throw new LocationNotFoundException(request.LocationCode);
        }
        
        // Deactivate using domain business logic
        location.Deactivate();
        
        // Platform.Shared automatically handles:
        // - Setting audit fields (UpdatedAt, UpdatedBy)
        // - Database transaction management
        await _locationRepository.UpdateAsync(location, cancellationToken);
        
        // Publish integration event
        _eventPublisher.AddIntegrationEvent(
            new LocationDeactivatedIntegrationEvent(
                location.LocationCode,
                DateTime.UtcNow));
        
        return _mapper.Map<LocationResponse>(location);
    }
}