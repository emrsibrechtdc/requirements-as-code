using Platform.Common.Messaging;
using Platform.Shared.IntegrationEvents;

namespace Platform.Locations.Application.IntegrationEvents;

[MessageType("LocationActivatedIntegrationEvent")]
public record LocationActivatedIntegrationEvent
(
    string LocationCode,
    DateTime ActivatedAt
) : IIntegrationEvent;

[MessageType("LocationDeactivatedIntegrationEvent")]
public record LocationDeactivatedIntegrationEvent
(
    string LocationCode,
    DateTime DeactivatedAt
) : IIntegrationEvent;

[MessageType("LocationDeletedIntegrationEvent")]
public record LocationDeletedIntegrationEvent
(
    string LocationCode,
    DateTime DeletedAt
) : IIntegrationEvent;