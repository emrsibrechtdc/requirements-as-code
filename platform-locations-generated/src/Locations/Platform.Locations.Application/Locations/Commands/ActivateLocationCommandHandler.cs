using AutoMapper;
using FluentValidation;
using Platform.Locations.Application.Locations.Dtos;
using Platform.Locations.Application.IntegrationEvents;
using Platform.Locations.Domain.Locations;
using Platform.Shared.Cqrs.Mediatr;
using Platform.Shared.IntegrationEvents;

namespace Platform.Locations.Application.Locations.Commands;

public class ActivateLocationCommandHandler : ICommandHandler<ActivateLocationCommand, LocationResponse>
{
    private readonly ILocationRepository _locationRepository;
    private readonly IValidator<ActivateLocationCommand> _validator;
    private readonly IMapper _mapper;
    private readonly IIntegrationEventPublisher _eventPublisher;

    public ActivateLocationCommandHandler(
        ILocationRepository locationRepository,
        IValidator<ActivateLocationCommand> validator,
        IMapper mapper,
        IIntegrationEventPublisher eventPublisher)
    {
        _locationRepository = locationRepository;
        _validator = validator;
        _mapper = mapper;
        _eventPublisher = eventPublisher;
    }

    public async Task<LocationResponse> Handle(ActivateLocationCommand request, CancellationToken cancellationToken)
    {
        // Input validation
        _validator.ValidateAndThrow(request);
        
        // Get existing location
        var location = await _locationRepository.GetByLocationCodeAsync(request.LocationCode, cancellationToken);
        if (location == null)
        {
            throw new LocationNotFoundException(request.LocationCode);
        }
        
        // Activate using domain business logic
        location.Activate();
        
        // Platform.Shared automatically handles:
        // - Setting audit fields (UpdatedAt, UpdatedBy)
        // - Database transaction management
        await _locationRepository.UpdateAsync(location, cancellationToken);
        
        // Publish integration event
        _eventPublisher.SaveIntegrationEvent(
            new LocationActivatedIntegrationEvent(
                location.LocationCode,
                DateTime.UtcNow));
        
        return _mapper.Map<LocationResponse>(location);
    }
}