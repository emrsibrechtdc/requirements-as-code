using Platform.Shared.Auditing;
using Platform.Shared.Ddd.Domain;
using Platform.Shared.Entities;
using Platform.Shared.Instrumentation;
using Platform.Shared.MultiProduct;

namespace Platform.Locations.Domain.Locations;

public class Location : FullyAuditedActivableAggregateRoot<Guid>, IMultiProductObject, IInstrumentationObject
{
    public string Product { get; set; } = default!;
    public string LocationCode { get; private set; } = default!;
    public string LocationTypeCode { get; private set; } = default!;
    public string? LocationTypeName { get; private set; }
    public string AddressLine1 { get; private set; } = default!;
    public string? AddressLine2 { get; private set; }
    public string City { get; private set; } = default!;
    public string State { get; private set; } = default!;
    public string ZipCode { get; private set; } = default!;
    public string Country { get; private set; } = default!;

    // Private constructor for EF Core
    private Location() { }

    private Location(string locationCode, string locationTypeCode, string addressLine1, 
        string? addressLine2, string city, string state, string zipCode, string country)
    {
        Id = Guid.NewGuid();
        LocationCode = locationCode;
        LocationTypeCode = locationTypeCode;
        AddressLine1 = addressLine1;
        AddressLine2 = addressLine2;
        City = city;
        State = state;
        ZipCode = zipCode;
        Country = country;
        // IsActive will be set through base class initialization
    }

    // Static factory method with domain business logic
    public static Location Create(string locationCode, string locationTypeCode, 
        string addressLine1, string? addressLine2, string city, string state, 
        string zipCode, string country)
    {
        // Domain invariant validation - rules that must ALWAYS be true
        if (string.IsNullOrWhiteSpace(locationCode))
            throw new ArgumentException("Location code cannot be empty", nameof(locationCode));
        
        if (string.IsNullOrWhiteSpace(locationTypeCode))
            throw new ArgumentException("Location type code cannot be empty", nameof(locationTypeCode));

        if (string.IsNullOrWhiteSpace(addressLine1))
            throw new ArgumentException("Address line 1 cannot be empty", nameof(addressLine1));

        if (string.IsNullOrWhiteSpace(city))
            throw new ArgumentException("City cannot be empty", nameof(city));

        if (string.IsNullOrWhiteSpace(state))
            throw new ArgumentException("State cannot be empty", nameof(state));

        if (string.IsNullOrWhiteSpace(zipCode))
            throw new ArgumentException("Zip code cannot be empty", nameof(zipCode));

        if (string.IsNullOrWhiteSpace(country))
            throw new ArgumentException("Country cannot be empty", nameof(country));

        // Additional domain rules
        if (locationCode.Length > 50)
            throw new ArgumentException("Location code cannot exceed 50 characters", nameof(locationCode));

        if (locationTypeCode.Length > 20)
            throw new ArgumentException("Location type code cannot exceed 20 characters", nameof(locationTypeCode));

        if (addressLine1.Length > 200)
            throw new ArgumentException("Address line 1 cannot exceed 200 characters", nameof(addressLine1));

        if (!string.IsNullOrEmpty(addressLine2) && addressLine2.Length > 200)
            throw new ArgumentException("Address line 2 cannot exceed 200 characters", nameof(addressLine2));

        if (city.Length > 100)
            throw new ArgumentException("City cannot exceed 100 characters", nameof(city));

        if (state.Length > 50)
            throw new ArgumentException("State cannot exceed 50 characters", nameof(state));

        if (zipCode.Length > 20)
            throw new ArgumentException("Zip code cannot exceed 20 characters", nameof(zipCode));

        if (country.Length > 50)
            throw new ArgumentException("Country cannot exceed 50 characters", nameof(country));

        return new Location(locationCode, locationTypeCode, addressLine1, addressLine2, 
            city, state, zipCode, country);
    }

    // Domain business logic methods
    public void UpdateAddress(string addressLine1, string? addressLine2, string city, 
        string state, string zipCode, string country)
    {
        if (string.IsNullOrWhiteSpace(addressLine1))
            throw new ArgumentException("Address line 1 cannot be empty", nameof(addressLine1));

        if (string.IsNullOrWhiteSpace(city))
            throw new ArgumentException("City cannot be empty", nameof(city));

        if (string.IsNullOrWhiteSpace(state))
            throw new ArgumentException("State cannot be empty", nameof(state));

        if (string.IsNullOrWhiteSpace(zipCode))
            throw new ArgumentException("Zip code cannot be empty", nameof(zipCode));

        if (string.IsNullOrWhiteSpace(country))
            throw new ArgumentException("Country cannot be empty", nameof(country));

        // Length validations
        if (addressLine1.Length > 200)
            throw new ArgumentException("Address line 1 cannot exceed 200 characters", nameof(addressLine1));

        if (!string.IsNullOrEmpty(addressLine2) && addressLine2.Length > 200)
            throw new ArgumentException("Address line 2 cannot exceed 200 characters", nameof(addressLine2));

        if (city.Length > 100)
            throw new ArgumentException("City cannot exceed 100 characters", nameof(city));

        if (state.Length > 50)
            throw new ArgumentException("State cannot exceed 50 characters", nameof(state));

        if (zipCode.Length > 20)
            throw new ArgumentException("Zip code cannot exceed 20 characters", nameof(zipCode));

        if (country.Length > 50)
            throw new ArgumentException("Country cannot exceed 50 characters", nameof(country));

        AddressLine1 = addressLine1;
        AddressLine2 = addressLine2;
        City = city;
        State = state;
        ZipCode = zipCode;
        Country = country;
    }

    public void SetLocationTypeName(string? locationTypeName)
    {
        if (!string.IsNullOrEmpty(locationTypeName) && locationTypeName.Length > 100)
            throw new ArgumentException("Location type name cannot exceed 100 characters", nameof(locationTypeName));

        LocationTypeName = locationTypeName;
    }

    public new void Activate()
    {
        if (IsActive)
        {
            throw new LocationAlreadyActiveException(LocationCode);
        }
        base.Activate();
    }

    public new void Deactivate()
    {
        if (!IsActive)
        {
            throw new LocationAlreadyInactiveException(LocationCode);
        }
        base.Deactivate();
    }

    public Dictionary<string, string> ToInstrumentationProperties()
    {
        return new Dictionary<string, string>
        {
            { "locationCode", LocationCode },
            { "locationTypeCode", LocationTypeCode },
            { "city", City },
            { "state", State },
            { "country", Country },
            { "isActive", IsActive.ToString() }
        };
    }
}