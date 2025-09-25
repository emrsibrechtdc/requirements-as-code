using AutoMapper;
using Platform.Locations.Application.Locations.Dtos;
using Platform.Locations.Domain.Locations;
using Platform.Shared.Cqrs.Mediatr;

namespace Platform.Locations.Application.Locations.Commands;

public class UpdateLocationCoordinatesCommandHandler : ICommandHandler<UpdateLocationCoordinatesCommand, LocationResponse>
{
    private readonly ILocationRepository _locationRepository;
    private readonly IMapper _mapper;

    public UpdateLocationCoordinatesCommandHandler(ILocationRepository locationRepository, IMapper mapper)
    {
        _locationRepository = locationRepository;
        _mapper = mapper;
    }

    public async Task<LocationResponse> Handle(UpdateLocationCoordinatesCommand request, CancellationToken cancellationToken)
    {
        // Platform.Shared automatically applies product filtering through repository data filters
        var location = await _locationRepository.GetByLocationCodeAsync(request.LocationCode, cancellationToken);

        if (location == null)
        {
            throw new LocationNotFoundException(request.LocationCode);
        }

        // Use domain method to update coordinates with validation
        location.SetCoordinates(request.Latitude, request.Longitude, request.GeofenceRadius);

        // Save changes
        await _locationRepository.UpdateAsync(location, cancellationToken);

        // Map to response DTO
        var locationDto = _mapper.Map<LocationDto>(location);
        
        return new LocationResponse(
            locationDto.LocationCode
        );
    }
}