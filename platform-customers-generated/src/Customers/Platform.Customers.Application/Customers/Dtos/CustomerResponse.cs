namespace Platform.Customers.Application.Customers.Dtos;

public record CustomerResponse
(
    string CustomerCode,
    string CompanyName,
    string ContactFullName,
    string Email,
    bool IsActive,
    DateTime CreatedAt,
    string CreatedBy
);