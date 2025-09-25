using Platform.Shared.Cqrs.Mediatr;
using Platform.Customers.Application.Customers.Dtos;

namespace Platform.Customers.Application.Customers.Commands;

public record UpdateCustomerCommand(
    string CustomerCode,
    string CompanyName,
    string ContactFirstName,
    string ContactLastName,
    string Email,
    string? PhoneNumber,
    string AddressLine1,
    string? AddressLine2,
    string City,
    string State,
    string PostalCode,
    string Country) : ICommand<CustomerResponse>;