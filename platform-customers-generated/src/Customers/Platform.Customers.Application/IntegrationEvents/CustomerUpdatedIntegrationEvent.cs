using Platform.Shared.IntegrationEvents;

namespace Platform.Customers.Application.IntegrationEvents;

public record CustomerUpdatedIntegrationEvent(
    string CustomerCode,
    string CompanyName,
    string Email,
    DateTime UpdatedAt) : IIntegrationEvent;