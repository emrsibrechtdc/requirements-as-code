using Platform.Shared.Exceptions;

namespace Platform.Locations.Domain.Locations;

public class LocationAlreadyExistsException : BusinessException
{
    public string LocationCode { get; init; }

    public LocationAlreadyExistsException(string locationCode)
        : base("LOCATION_ALREADY_EXISTS",
               "Location Already Exists",
               $"A location with code '{locationCode}' already exists")
    {
        LocationCode = locationCode;
    }
}