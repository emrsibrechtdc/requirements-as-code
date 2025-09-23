using Platform.Shared.Exceptions;

namespace Platform.Locations.Domain.Locations;

public class LocationNotFoundException : BusinessException
{
    public string LocationCode { get; init; }

    public LocationNotFoundException(string locationCode)
        : base("LOCATION_NOT_FOUND",
               "Location Not Found",
               $"The location with code '{locationCode}' was not found")
    {
        LocationCode = locationCode;
    }
}