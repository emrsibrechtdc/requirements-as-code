using AutoMapper;
using Platform.Locations.Application.Locations.Dtos;
using Platform.Locations.Domain.Locations;
using Platform.Shared.Cqrs.Mediatr;
using Platform.Shared.DataLayer.Repositories;

namespace Platform.Locations.Application.Locations.Queries;

public class GetLocationsQueryHandler : IQueryHandler<GetLocationsQuery, List<LocationDto>>
{
    private readonly IReadRepository<Location, Guid> _repository;
    private readonly IMapper _mapper;

    public GetLocationsQueryHandler(IReadRepository<Location, Guid> repository, IMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }

    public async Task<List<LocationDto>> Handle(GetLocationsQuery request, CancellationToken cancellationToken)
    {
        IEnumerable<Location> locations;

        if (string.IsNullOrEmpty(request.LocationCode))
        {
            // Repository automatically filters by product context - no manual filtering needed
            locations = await _repository.GetAllAsync(cancellationToken);
        }
        else
        {
            // Validate minimum search length
            if (request.LocationCode.Length < LocationsConstants.LocationCodeMinSearchLength)
            {
                throw new ArgumentException($"Location code search requires minimum {LocationsConstants.LocationCodeMinSearchLength} characters", nameof(request.LocationCode));
            }

            // Use repository-specific method for prefix search
            if (_repository is ILocationRepository locationRepo)
            {
                locations = await locationRepo.GetByLocationCodeStartsWithAsync(request.LocationCode, cancellationToken);
            }
            else
            {
                // Fallback to get all and filter (not optimal but functional)
                var allLocations = await _repository.GetAllAsync(cancellationToken);
                locations = allLocations.Where(l => l.LocationCode.StartsWith(request.LocationCode, StringComparison.OrdinalIgnoreCase));
            }
        }

        return _mapper.Map<List<LocationDto>>(locations.ToList());
    }
}