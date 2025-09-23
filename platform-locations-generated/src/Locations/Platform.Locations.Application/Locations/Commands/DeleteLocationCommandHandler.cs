using AutoMapper;
using FluentValidation;
using Platform.Locations.Application.Locations.Dtos;
using Platform.Locations.Application.IntegrationEvents;
using Platform.Locations.Domain.Locations;
using Platform.Shared.Cqrs;
using Platform.Shared.IntegrationEvents;

namespace Platform.Locations.Application.Locations.Commands;

public class DeleteLocationCommandHandler : ICommandHandler<DeleteLocationCommand, LocationResponse>
{
    private readonly ILocationRepository _locationRepository;
    private readonly IValidator<DeleteLocationCommand> _validator;
    private readonly IMapper _mapper;
    private readonly IIntegrationEventPublisher _eventPublisher;

    public DeleteLocationCommandHandler(
        ILocationRepository locationRepository,
        IValidator<DeleteLocationCommand> validator,
        IMapper mapper,
        IIntegrationEventPublisher eventPublisher)
    {
        _locationRepository = locationRepository;
        _validator = validator;
        _mapper = mapper;
        _eventPublisher = eventPublisher;
    }

    public async Task<LocationResponse> Handle(DeleteLocationCommand request, CancellationToken cancellationToken)
    {
        // Input validation
        _validator.ValidateAndThrow(request);
        
        // Get existing location
        var location = await _locationRepository.GetByLocationCodeAsync(request.LocationCode, cancellationToken);
        if (location == null)
        {
            throw new LocationNotFoundException(request.LocationCode);
        }
        
        // Platform.Shared automatically handles soft delete:
        // - Setting audit fields (DeletedAt, DeletedBy)
        // - Database transaction management
        await _locationRepository.DeleteAsync(location, cancellationToken);
        
        // Publish integration event
        _eventPublisher.AddIntegrationEvent(
            new LocationDeletedIntegrationEvent(
                location.LocationCode,
                DateTime.UtcNow));
        
        return _mapper.Map<LocationResponse>(location);
    }
}