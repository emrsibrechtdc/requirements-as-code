namespace Platform.Customers.Domain.Customers.ValueObjects;

public class Address
{
    public string AddressLine1 { get; }
    public string? AddressLine2 { get; }
    public string City { get; }
    public string State { get; }
    public string PostalCode { get; }
    public string Country { get; }

    public Address(string addressLine1, string? addressLine2, string city, string state, string postalCode, string country)
    {
        if (string.IsNullOrWhiteSpace(addressLine1))
            throw new ArgumentException("Address line 1 is required", nameof(addressLine1));
            
        if (string.IsNullOrWhiteSpace(city))
            throw new ArgumentException("City is required", nameof(city));
            
        if (string.IsNullOrWhiteSpace(state))
            throw new ArgumentException("State is required", nameof(state));
            
        if (string.IsNullOrWhiteSpace(postalCode))
            throw new ArgumentException("Postal code is required", nameof(postalCode));
            
        if (string.IsNullOrWhiteSpace(country))
            throw new ArgumentException("Country is required", nameof(country));

        // Domain validation - length constraints
        if (addressLine1.Length > 255)
            throw new ArgumentException("Address line 1 cannot exceed 255 characters", nameof(addressLine1));
            
        if (!string.IsNullOrEmpty(addressLine2) && addressLine2.Length > 255)
            throw new ArgumentException("Address line 2 cannot exceed 255 characters", nameof(addressLine2));
            
        if (city.Length > 100)
            throw new ArgumentException("City cannot exceed 100 characters", nameof(city));
            
        if (state.Length > 50)
            throw new ArgumentException("State cannot exceed 50 characters", nameof(state));
            
        if (postalCode.Length > 20)
            throw new ArgumentException("Postal code cannot exceed 20 characters", nameof(postalCode));
            
        if (country.Length > 100)
            throw new ArgumentException("Country cannot exceed 100 characters", nameof(country));

        AddressLine1 = addressLine1.Trim();
        AddressLine2 = addressLine2?.Trim();
        City = city.Trim();
        State = state.Trim();
        PostalCode = postalCode.Trim();
        Country = country.Trim();
    }

    public string FullAddress
    {
        get
        {
            var parts = new List<string> { AddressLine1 };
            
            if (!string.IsNullOrEmpty(AddressLine2))
                parts.Add(AddressLine2);
                
            parts.Add($"{City}, {State} {PostalCode}");
            parts.Add(Country);
            
            return string.Join(", ", parts);
        }
    }

    public override bool Equals(object? obj)
    {
        return obj is Address other &&
               AddressLine1 == other.AddressLine1 &&
               AddressLine2 == other.AddressLine2 &&
               City == other.City &&
               State == other.State &&
               PostalCode == other.PostalCode &&
               Country == other.Country;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(AddressLine1, AddressLine2, City, State, PostalCode, Country);
    }
}