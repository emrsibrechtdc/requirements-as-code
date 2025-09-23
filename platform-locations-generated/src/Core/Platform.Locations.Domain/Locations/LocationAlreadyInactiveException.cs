using Platform.Shared.Exceptions;

namespace Platform.Locations.Domain.Locations;

public class LocationAlreadyInactiveException : BusinessException
{
    public string LocationCode { get; init; }

    public LocationAlreadyInactiveException(string locationCode)
        : base("LOCATION_ALREADY_INACTIVE",
               "Location Already Inactive",
               $"The location with code '{locationCode}' is already inactive")
    {
        LocationCode = locationCode;
    }
}