using Platform.Common.Messaging;
using Platform.Shared.IntegrationEvents;

namespace Platform.Locations.Application.IntegrationEvents;

[MessageType("LocationRegisteredIntegrationEvent")]
public record LocationRegisteredIntegrationEvent
(
    string LocationCode,
    string LocationTypeCode,
    string AddressLine1,
    string? AddressLine2,
    string City,
    string State,
    string ZipCode,
    string Country,
    decimal? Latitude,
    decimal? Longitude,
    double? GeofenceRadius,
    DateTime RegisteredAt
) : IIntegrationEvent;
