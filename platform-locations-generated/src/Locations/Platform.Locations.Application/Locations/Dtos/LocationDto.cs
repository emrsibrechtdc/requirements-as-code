namespace Platform.Locations.Application.Locations.Dtos;

public record LocationDto
(
    string? LocationCode,
    string? LocationTypeCode,
    string? LocationTypeName,
    string? AddressLine1,
    string? AddressLine2,
    string? City,
    string? State,
    string? ZipCode,
    string? Country
);