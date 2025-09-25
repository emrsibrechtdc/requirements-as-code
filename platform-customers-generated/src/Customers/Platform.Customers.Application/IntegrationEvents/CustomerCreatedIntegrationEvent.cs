using Platform.Shared.IntegrationEvents;

namespace Platform.Customers.Application.IntegrationEvents;

public record CustomerCreatedIntegrationEvent(
    string CustomerCode,
    string CompanyName,
    string CustomerType,
    string Email,
    DateTime CreatedAt) : IIntegrationEvent;