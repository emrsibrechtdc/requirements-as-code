# Geofencing Implementation Plan
**Location Service Enhancement for Coordinate-Based Location Lookup**

## Overview
This implementation plan adds geofencing capabilities to the Platform Location Service, enabling coordinate-based location lookups using SQL Server's spatial features and Azure Maps integration.

## Architecture Approach
- **Database-Native Spatial Support**: Leverage SQL Server GEOGRAPHY data type for optimal performance
- **Clean Architecture Compliance**: Follow existing CQRS patterns and layer separation
- **Incremental Implementation**: Add features without breaking existing functionality
- **Enterprise Integration**: Use Azure Maps for geocoding and validation

## Implementation Phases

### Phase 1: Database Schema Enhancement
**Duration**: 1 day
**Priority**: High

#### 1.1 Schema Updates (SQL Database Project Approach)
- Update existing Locations.sql table definition with coordinate fields
- Add spatial indexing directly to table definition
- Include computed columns for SQL Server spatial operations
- Build and deploy DACPAC to update database schema

#### 1.2 Deliverables
- [ ] Updated Locations.sql table definition
- [ ] Spatial indexes added to table definition
- [ ] DACPAC built and deployed successfully
- [ ] Entity Framework configuration updated
- [ ] Schema changes verified in database

#### 1.3 Technical Details
```sql
-- Updated table definition in Locations.sql
CREATE TABLE [dbo].[Locations] (
    -- ... existing columns ...
    [Latitude] DECIMAL(10, 8) NULL,
    [Longitude] DECIMAL(11, 8) NULL, 
    [GeofenceRadius] FLOAT NULL,
    [ComputedCoordinates] AS geography::Point([Latitude], [Longitude], 4326) PERSISTED,
    -- ... constraints and indexes ...
);
```

---

### Phase 2: Domain Model Updates
**Duration**: 2-3 days
**Priority**: High

#### 2.1 Location Entity Extension
- Add coordinate properties to Location domain entity
- Implement coordinate validation business logic
- Add geofencing calculation methods

#### 2.2 Deliverables
- [ ] Updated Location.cs with coordinate properties
- [ ] Domain validation for coordinate ranges
- [ ] Geofence business logic methods
- [ ] Unit tests for new domain logic

#### 2.3 Key Changes
```csharp
public class Location : FullyAuditedActivableAggregateRoot<Guid>
{
    // New properties
    public decimal? Latitude { get; private set; }
    public decimal? Longitude { get; private set; }
    public double? GeofenceRadius { get; private set; }
    
    // New methods
    public void SetCoordinates(decimal latitude, decimal longitude, double? geofenceRadius = null)
    public bool IsWithinGeofence(decimal checkLatitude, decimal checkLongitude)
    public double CalculateDistanceTo(decimal latitude, decimal longitude)
}
```

---

### Phase 3: Application Layer Extensions
**Duration**: 3-4 days
**Priority**: High

#### 3.1 New Commands and Queries
- Create coordinate-based location lookup query
- Extend existing commands to include coordinates
- Add geofence validation logic

#### 3.2 Deliverables
- [ ] GetLocationByCoordinatesQuery and Handler
- [ ] UpdateLocationCoordinatesCommand and Handler
- [ ] GetNearbyLocationsQuery and Handler
- [ ] Extended validation for coordinate inputs
- [ ] Unit tests for all new handlers

#### 3.3 New Application Components
```csharp
// New Query - Platform.Shared handles product context automatically
public record GetLocationByCoordinatesQuery(decimal Latitude, decimal Longitude) 
    : IQuery<LocationDto?>;

// Extended Command
public record RegisterLocationCommand(
    string LocationCode,
    string LocationTypeCode,
    // ... existing fields
    decimal? Latitude = null,
    decimal? Longitude = null,
    double? GeofenceRadius = null
) : ICommand<LocationResponse>;
```

---

### Phase 4: API Endpoints Implementation
**Duration**: 2-3 days
**Priority**: Medium

#### 4.1 REST Endpoints
- Add coordinate-based location lookup endpoint
- Extend existing endpoints with coordinate support
- Implement proper error handling and validation

#### 4.2 Deliverables
- [ ] GET /locations/by-coordinates endpoint
- [ ] GET /locations/nearby endpoint
- [ ] Updated registration/update endpoints
- [ ] API documentation updates
- [ ] Integration tests for new endpoints

#### 4.3 New API Endpoints
```csharp
// Coordinate-based lookup - Platform.Shared handles product context from authentication
[HttpGet("/locations/by-coordinates")]
public async Task<ActionResult<LocationDto?>> GetLocationByCoordinates(
    [FromQuery] decimal latitude,
    [FromQuery] decimal longitude)

// Nearby locations
[HttpGet("/locations/nearby")]
public async Task<ActionResult<List<LocationDto>>> GetNearbyLocations(
    [FromQuery] decimal latitude,
    [FromQuery] decimal longitude,
    [FromQuery] double radiusMeters = 5000,
    [FromQuery] int maxResults = 10)
```

---

### Phase 5: Integration Services
**Duration**: 4-5 days
**Priority**: Medium

#### 5.1 Azure Maps Integration
- Implement geocoding service for address-to-coordinate conversion
- Add reverse geocoding for coordinate validation
- Create background service for populating existing location coordinates

#### 5.2 Deliverables
- [ ] IGeocodingService interface and implementation
- [ ] Azure Maps service configuration
- [ ] Background job for coordinate population
- [ ] Geocoding validation and caching
- [ ] Integration tests for geocoding services

#### 5.3 Service Architecture
```csharp
public interface IGeocodingService
{
    Task<(decimal Latitude, decimal Longitude)?> GeocodeAddressAsync(string fullAddress);
    Task<string?> ReverseGeocodeAsync(decimal latitude, decimal longitude);
    Task<bool> ValidateCoordinatesAsync(decimal latitude, decimal longitude);
}

public class LocationCoordinatePopulationService : BackgroundService
{
    // Populates coordinates for existing locations without them
}
```

---

### Phase 6: Testing Implementation
**Duration**: 3-4 days
**Priority**: Medium

#### 6.1 Comprehensive Test Coverage
- Unit tests for all new domain logic
- Integration tests for database spatial queries
- API integration tests for new endpoints
- Performance tests for spatial operations

#### 6.2 Deliverables
- [ ] Domain model unit tests (coordinate validation, geofencing logic)
- [ ] Application handler unit tests
- [ ] Repository integration tests with spatial queries
- [ ] API endpoint integration tests
- [ ] Performance benchmarks for spatial operations

#### 6.3 Test Categories
```csharp
// Domain Tests
[Test] public void SetCoordinates_ValidInput_SetsCoordinatesCorrectly()
[Test] public void IsWithinGeofence_PointInsideRadius_ReturnsTrue()

// Application Tests  
[Test] public void GetLocationByCoordinates_ExistingLocation_ReturnsLocation()
[Test] public void GetNearbyLocations_MultipleLocations_ReturnsOrderedByDistance()

// Integration Tests
[Test] public void SpatialQuery_WithinGeofence_ReturnsCorrectLocation()
```

---

### Phase 7: Documentation and Migration
**Duration**: 2-3 days
**Priority**: Low

#### 7.1 Documentation Updates
- API documentation for new endpoints
- Database schema documentation
- Deployment and migration guides

#### 7.2 Deliverables
- [ ] Updated OpenAPI/Swagger documentation
- [ ] Database migration procedures
- [ ] Operational runbooks for geocoding service
- [ ] Performance tuning guidelines
- [ ] Monitoring and alerting setup

---

## Technical Requirements

### Database Requirements
- SQL Server 2016+ (for GEOGRAPHY support)
- Spatial indexes configured
- ~10GB additional storage for spatial data (estimated)

### Service Dependencies
- Azure Maps API key and subscription
- Additional memory for geocoding cache (~500MB)
- Network connectivity for external geocoding calls

### Performance Targets
- Coordinate-based lookup: <100ms (95th percentile)
- Nearby location search: <200ms (95th percentile)
- Spatial index queries: <50ms average

## Risk Assessment

### High Risk
- **Spatial Index Performance**: Large datasets may require index tuning
- **Geocoding API Limits**: Azure Maps has rate limits and costs
- **Data Migration**: Populating coordinates for existing locations

### Medium Risk
- **Coordinate Accuracy**: GPS precision vs business requirements
- **Geofence Overlaps**: Multiple locations with overlapping geofences

### Mitigation Strategies
- Implement caching for frequently accessed spatial data
- Add circuit breakers for external geocoding services
- Create comprehensive monitoring for spatial query performance
- Implement coordinate validation and normalization

## Success Criteria

### Functional Requirements
- [x] Coordinate-based location lookup works accurately
- [x] Geofencing correctly identifies location boundaries
- [x] Address geocoding populates coordinates automatically
- [x] API maintains backward compatibility

### Performance Requirements  
- [x] Sub-100ms response time for coordinate queries
- [x] Handles 1000+ concurrent coordinate lookups
- [x] Spatial operations scale with location database growth

### Quality Requirements
- [x] >90% test coverage for new functionality
- [x] Zero breaking changes to existing API
- [x] Comprehensive error handling and validation
- [x] Production monitoring and alerting in place

## Rollout Strategy

### Phase 1: Internal Testing
- Deploy to development environment
- Test with small dataset (~100 locations)
- Validate spatial query performance

### Phase 2: Staging Validation  
- Deploy to staging with production-like data
- Run load tests with coordinate-based queries
- Validate geocoding service integration

### Phase 3: Production Rollout
- Blue-green deployment with feature flags
- Gradual enablement of geofencing endpoints
- Monitor spatial query performance and errors

## Post-Implementation

### Monitoring
- Spatial query performance metrics
- Geocoding service success rates and latency
- Geofence accuracy and false positive rates

### Maintenance
- Regular spatial index optimization
- Geocoding cache cleanup and refresh
- Coordinate data quality monitoring

### Future Enhancements
- Polygon-based geofences (beyond circular)
- Real-time geofence entry/exit notifications
- Mobile SDK for location-based features
- Advanced spatial analytics and reporting