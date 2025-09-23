using Platform.Shared.Exceptions;

namespace Platform.Locations.Domain.Locations;

public class LocationAlreadyActiveException : BusinessException
{
    public string LocationCode { get; init; }

    public LocationAlreadyActiveException(string locationCode)
        : base("LOCATION_ALREADY_ACTIVE",
               "Location Already Active",
               $"The location with code '{locationCode}' is already active")
    {
        LocationCode = locationCode;
    }
}