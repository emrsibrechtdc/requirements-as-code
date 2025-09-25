using Platform.Shared.Cqrs.Mediatr;
using Platform.Customers.Application.Customers.Dtos;

namespace Platform.Customers.Application.Customers.Queries;

public record GetCustomersQuery(
    string? CustomerCode,
    string? CustomerType,
    string? CompanyName,
    bool? IsActive) : IQuery<List<CustomerDto>>;