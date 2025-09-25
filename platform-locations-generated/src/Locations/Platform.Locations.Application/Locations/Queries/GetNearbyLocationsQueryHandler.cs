using AutoMapper;
using Platform.Locations.Application.Locations.Dtos;
using Platform.Locations.Domain.Locations;
using Platform.Shared.Cqrs.Mediatr;

namespace Platform.Locations.Application.Locations.Queries;

public class GetNearbyLocationsQueryHandler : IQueryHandler<GetNearbyLocationsQuery, IEnumerable<LocationWithDistanceDto>>
{
    private readonly ILocationRepository _locationRepository;
    private readonly IMapper _mapper;

    public GetNearbyLocationsQueryHandler(ILocationRepository locationRepository, IMapper mapper)
    {
        _locationRepository = locationRepository;
        _mapper = mapper;
    }

    public async Task<IEnumerable<LocationWithDistanceDto>> Handle(GetNearbyLocationsQuery request, CancellationToken cancellationToken)
    {
        // Platform.Shared automatically applies product filtering through repository data filters
        var locations = await _locationRepository.GetNearbyLocationsAsync(
            request.Latitude, 
            request.Longitude, 
            request.RadiusMeters, 
            request.MaxResults, 
            cancellationToken);

        var result = new List<LocationWithDistanceDto>();
        
        foreach (var location in locations)
        {
            // Calculate distance using domain method for consistency
            var distance = location.HasCoordinates 
                ? location.ApproximateDistanceTo(request.Latitude, request.Longitude)
                : 0.0;
                
            var locationDto = _mapper.Map<LocationDto>(location);
            
            // Create LocationWithDistanceDto with distance information
            var locationWithDistance = new LocationWithDistanceDto(
                locationDto.Id,
                locationDto.LocationCode,
                locationDto.LocationTypeCode,
                locationDto.LocationTypeName,
                locationDto.AddressLine1,
                locationDto.AddressLine2,
                locationDto.City,
                locationDto.State,
                locationDto.ZipCode,
                locationDto.Country,
                locationDto.IsActive,
                locationDto.CreatedAt,
                locationDto.CreatedBy,
                locationDto.UpdatedAt,
                locationDto.UpdatedBy,
                locationDto.Latitude,
                locationDto.Longitude,
                locationDto.GeofenceRadius,
                distance
            );
            
            result.Add(locationWithDistance);
        }

        return result;
    }
}