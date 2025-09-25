namespace Platform.Customers.Application.Customers.Dtos;

public record CustomerDto
(
    string CustomerCode,
    string CustomerType,
    string CompanyName,
    string ContactFirstName,
    string ContactLastName,
    string ContactFullName,
    string Email,
    string? PhoneNumber,
    string AddressLine1,
    string? AddressLine2,
    string City,
    string State,
    string PostalCode,
    string Country,
    string FullAddress,
    bool IsActive,
    DateTime CreatedAt
);