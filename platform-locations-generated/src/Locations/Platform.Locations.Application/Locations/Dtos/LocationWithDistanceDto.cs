namespace Platform.Locations.Application.Locations.Dtos;

public record LocationWithDistanceDto(
    Guid Id,
    string LocationCode,
    string LocationTypeCode,
    string? LocationTypeName,
    string AddressLine1,
    string? AddressLine2,
    string City,
    string State,
    string ZipCode,
    string Country,
    bool IsActive,
    DateTimeOffset CreatedAt,
    string? CreatedBy,
    DateTimeOffset? UpdatedAt,
    string? UpdatedBy,
    // Coordinate fields
    decimal? Latitude,
    decimal? Longitude,
    double? GeofenceRadius,
    // Distance from search point
    double DistanceMeters
);