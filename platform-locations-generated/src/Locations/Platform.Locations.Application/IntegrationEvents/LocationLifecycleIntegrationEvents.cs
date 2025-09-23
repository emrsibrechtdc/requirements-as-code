using Platform.Shared.IntegrationEvents;

namespace Platform.Locations.Application.IntegrationEvents;

public record LocationActivatedIntegrationEvent
(
    string LocationCode,
    DateTime ActivatedAt
) : IIntegrationEvent;

public record LocationDeactivatedIntegrationEvent
(
    string LocationCode,
    DateTime DeactivatedAt
) : IIntegrationEvent;

public record LocationDeletedIntegrationEvent
(
    string LocationCode,
    DateTime DeletedAt
) : IIntegrationEvent;