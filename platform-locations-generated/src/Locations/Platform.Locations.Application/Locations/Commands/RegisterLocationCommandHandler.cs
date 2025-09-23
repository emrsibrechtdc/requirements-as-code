using AutoMapper;
using FluentValidation;
using Platform.Locations.Application.Locations.Dtos;
using Platform.Locations.Application.IntegrationEvents;
using Platform.Locations.Domain.Locations;
using Platform.Shared.Cqrs.Mediatr;
using Platform.Shared.IntegrationEvents;

namespace Platform.Locations.Application.Locations.Commands;

public class RegisterLocationCommandHandler : ICommandHandler<RegisterLocationCommand, LocationResponse>
{
    private readonly ILocationRepository _locationRepository;
    private readonly IValidator<RegisterLocationCommand> _validator;
    private readonly IMapper _mapper;
    private readonly IIntegrationEventPublisher _eventPublisher;

    public RegisterLocationCommandHandler(
        ILocationRepository locationRepository,
        IValidator<RegisterLocationCommand> validator,
        IMapper mapper,
        IIntegrationEventPublisher eventPublisher)
    {
        _locationRepository = locationRepository;
        _validator = validator;
        _mapper = mapper;
        _eventPublisher = eventPublisher;
    }

    public async Task<LocationResponse> Handle(RegisterLocationCommand request, CancellationToken cancellationToken)
    {
        // Input validation
        _validator.ValidateAndThrow(request);
        
        // Application-level business rules validation
        await ValidateBusinessRules(request, cancellationToken);
        
        // Create domain entity with business logic in aggregate root
        var location = Location.Create(
            request.LocationCode!, 
            request.LocationTypeCode!, 
            request.AddressLine1!,
            request.AddressLine2,
            request.City!,
            request.State!,
            request.ZipCode!,
            request.Country!);
        
        // Platform.Shared automatically handles:
        // - Setting audit fields (CreatedAt, CreatedBy)
        // - Setting product context from middleware
        // - Database transaction management (via TransactionBehavior)
        await _locationRepository.AddAsync(location, cancellationToken);
        
        // Publish clean integration events (no product context in payload)
        // Product context automatically added to CloudEvents headers
        _eventPublisher.SaveIntegrationEvent(
            new LocationRegisteredIntegrationEvent(
                location.LocationCode, 
                location.LocationTypeCode,
                location.AddressLine1,
                location.AddressLine2,
                location.City,
                location.State,
                location.ZipCode,
                location.Country,
                location.CreatedAt.DateTime));
        
        return _mapper.Map<LocationResponse>(location);
    }

    private async Task ValidateBusinessRules(RegisterLocationCommand request, CancellationToken cancellationToken)
    {
        // Repository automatically filters by product context - no manual filtering needed
        var existingLocation = await _locationRepository.GetByLocationCodeAsync(request.LocationCode!, cancellationToken);
        if (existingLocation != null)
        {
            throw new LocationAlreadyExistsException(request.LocationCode!);
        }
    }
}