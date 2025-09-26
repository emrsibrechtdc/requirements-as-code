using AutoMapper;
using Platform.Locations.Application.Locations.Dtos;
using Platform.Locations.Domain.Locations;
using Platform.Shared.Cqrs.Mediatr;

namespace Platform.Locations.Application.Locations.Queries;

public class GetLocationByCoordinatesQueryHandler : IQueryHandler<GetLocationByCoordinatesQuery, LocationDto?>
{
    private readonly ILocationRepository _locationRepository;
    private readonly IMapper _mapper;

    public GetLocationByCoordinatesQueryHandler(ILocationRepository locationRepository, IMapper mapper)
    {
        _locationRepository = locationRepository;
        _mapper = mapper;
    }

    public async Task<LocationDto?> Handle(GetLocationByCoordinatesQuery request, CancellationToken cancellationToken)
    {
        // Platform.Shared automatically applies product filtering through repository data filters
        var location = await _locationRepository.GetLocationByCoordinatesAsync(
            request.Latitude, 
            request.Longitude, 
            cancellationToken);

        return location != null ? _mapper.Map<LocationDto>(location) : null;
    }
}