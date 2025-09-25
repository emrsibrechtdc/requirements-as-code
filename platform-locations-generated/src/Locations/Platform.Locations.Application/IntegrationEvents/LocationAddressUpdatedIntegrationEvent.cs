using Platform.Common.Messaging;
using Platform.Shared.IntegrationEvents;

namespace Platform.Locations.Application.IntegrationEvents;

[MessageType("LocationAddressUpdatedIntegrationEvent")]
public record LocationAddressUpdatedIntegrationEvent
(
    string LocationCode,
    string AddressLine1,
    string? AddressLine2,
    string City,
    string State,
    string ZipCode,
    string Country,
    DateTime UpdatedAt
) : IIntegrationEvent;