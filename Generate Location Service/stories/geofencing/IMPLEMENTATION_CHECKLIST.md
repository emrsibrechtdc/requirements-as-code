# Geofencing Implementation Checklist

## Pre-Implementation Setup

### Environment Requirements
- [ ] SQL Server 2016+ (for GEOGRAPHY support)
- [ ] .NET 8.0 SDK
- [ ] Azure Maps API subscription and key
- [ ] Entity Framework Core 8.0+
- [ ] Platform.Shared library access

### Development Tools
- [ ] Visual Studio 2022 or VS Code
- [ ] SQL Server Management Studio
- [ ] Postman or similar API testing tool
- [ ] Git for source control

## Phase 1: Database Schema Enhancement (1-2 days)

### Database Changes
- [ ] **Create migration script** for new columns
  - [ ] `Latitude DECIMAL(10,8) NULL`
  - [ ] `Longitude DECIMAL(11,8) NULL`
  - [ ] `GeofenceRadius FLOAT NULL`
  - [ ] `ComputedCoordinates` computed column
- [ ] **Create spatial indexes**
  - [ ] Primary spatial index on `ComputedCoordinates`
  - [ ] Composite index on `Product + coordinates`
- [ ] **Test migration script** on development database
- [ ] **Create rollback script** for safety
- [ ] **Update Entity Framework configuration**
  - [ ] Add coordinate properties to `LocationConfiguration.cs`
  - [ ] Configure computed column mapping

### Files to Modify
```
src/Database/Platform.Locations.Database/Tables/Locations_Migration.sql (new)
src/Locations/Platform.Locations.Infrastructure/Configurations/LocationConfiguration.cs
```

## Phase 2: Domain Model Updates (2-3 days)

### Location Entity Extensions
- [ ] **Add coordinate properties**
  - [ ] `public decimal? Latitude { get; private set; }`
  - [ ] `public decimal? Longitude { get; private set; }`
  - [ ] `public double? GeofenceRadius { get; private set; }`
- [ ] **Add coordinate methods**
  - [ ] `SetCoordinates(lat, lng, radius)`
  - [ ] `ClearCoordinates()`
  - [ ] `ValidateCoordinates()` (private)
  - [ ] `HasCoordinates` property
  - [ ] `HasGeofence` property
- [ ] **Update instrumentation**
  - [ ] Add coordinates to `ToInstrumentationProperties()`
- [ ] **Create unit tests** for new domain logic

### Files to Modify
```
src/Core/Platform.Locations.Domain/Locations/Location.cs
test/Platform.Locations.Application.Tests/Locations/Domain/LocationTests.cs (new)
```

### New Domain Exceptions (Optional)
- [ ] `InvalidCoordinatesException.cs`
- [ ] `GeofenceValidationException.cs`

## Phase 3: Application Layer Extensions (3-4 days)

### New Query Models
- [ ] **Create coordinate-based queries**
  - [ ] `GetLocationByCoordinatesQuery.cs`
  - [ ] `GetLocationByCoordinatesQueryHandler.cs`
  - [ ] `GetNearbyLocationsQuery.cs`
  - [ ] `GetNearbyLocationsQueryHandler.cs`
- [ ] **Create coordinate commands**
  - [ ] `UpdateLocationCoordinatesCommand.cs`
  - [ ] `UpdateLocationCoordinatesCommandHandler.cs`
- [ ] **Extend existing commands**
  - [ ] Update `RegisterLocationCommand` with coordinate fields
  - [ ] Update `RegisterLocationCommandHandler` 
  - [ ] Update validators

### Enhanced DTOs
- [ ] **Update existing DTOs**
  - [ ] Add coordinate fields to `LocationDto.cs`
  - [ ] Add coordinate fields to `LocationResponse.cs`
- [ ] **Create new DTOs**
  - [ ] `LocationWithDistanceDto.cs`
  - [ ] `CoordinatesDto.cs`
- [ ] **Update AutoMapper profile**
  - [ ] Map coordinate properties in `AutoMapProfile.cs`

### Files to Create/Modify
```
src/Locations/Platform.Locations.Application/Locations/Queries/
  ├── GetLocationByCoordinatesQuery.cs (new)
  ├── GetLocationByCoordinatesQueryHandler.cs (new)
  ├── GetNearbyLocationsQuery.cs (new)
  └── GetNearbyLocationsQueryHandler.cs (new)

src/Locations/Platform.Locations.Application/Locations/Commands/
  ├── UpdateLocationCoordinatesCommand.cs (new)
  ├── UpdateLocationCoordinatesCommandHandler.cs (new)
  └── RegisterLocationCommand.cs (update)

src/Locations/Platform.Locations.Application/Locations/Dtos/
  ├── LocationDto.cs (update)
  ├── LocationWithDistanceDto.cs (new)
  └── CoordinatesDto.cs (new)
```

## Phase 4: Repository Extensions (2-3 days)

### Repository Interface Updates
- [ ] **Extend `ILocationRepository`**
  - [ ] `GetLocationByCoordinatesAsync()`
  - [ ] `GetNearbyLocationsAsync()`
  - [ ] `GetLocationsWithoutCoordinatesAsync()`
  - [ ] `CountLocationsWithCoordinatesAsync()`

### Repository Implementation
- [ ] **Implement spatial queries in `LocationRepository`**
  - [ ] Use SQL Server spatial functions
  - [ ] Optimize with proper indexing
  - [ ] Handle multi-product filtering
- [ ] **Test spatial query performance**
- [ ] **Add error handling for spatial operations**

### Files to Modify
```
src/Core/Platform.Locations.Domain/Locations/ILocationRepository.cs
src/Locations/Platform.Locations.Infrastructure/Repositories/LocationRepository.cs
```

## Phase 5: API Endpoints Implementation (2-3 days)

### New REST Endpoints
- [ ] **Create coordinate-based endpoints**
  - [ ] `GET /locations/by-coordinates`
  - [ ] `GET /locations/nearby`
  - [ ] `PUT /locations/{locationCode}/coordinates`
- [ ] **Update existing endpoints**
  - [ ] Add coordinate fields to registration endpoint
  - [ ] Add coordinate fields to update address endpoint
  - [ ] Include coordinates in GET locations response

### Endpoint Implementation
- [ ] **Add routes to `EndpointRouteBuilderExtensions.cs`**
- [ ] **Implement proper validation**
- [ ] **Add authorization requirements**
- [ ] **Update OpenAPI documentation**

### Files to Modify
```
src/Locations/Platform.Locations.HttpApi/Extensions/EndpointRouteBuilderExtensions.cs
```

## Phase 6: Integration Services (4-5 days)

### Azure Maps Integration
- [ ] **Create geocoding service interface**
  - [ ] `IGeocodingService.cs`
- [ ] **Implement Azure Maps service**
  - [ ] `AzureMapsGeocodingService.cs`
  - [ ] Configure HttpClient
  - [ ] Add retry policies
  - [ ] Implement caching
- [ ] **Create background service**
  - [ ] `LocationCoordinatePopulationService.cs`
  - [ ] Batch processing logic
  - [ ] Error handling and logging

### Configuration
- [ ] **Add Azure Maps configuration**
  - [ ] Update `appsettings.json`
  - [ ] Add configuration classes
  - [ ] Register services in DI container

### Files to Create
```
src/Locations/Platform.Locations.Application/Services/
  ├── IGeocodingService.cs (new)
  └── AzureMapsGeocodingService.cs (new)

src/Locations/Platform.Locations.Infrastructure/Services/
  └── LocationCoordinatePopulationService.cs (new)

src/Locations/Platform.Locations.Infrastructure/Extensions/
  └── InfrastructureServiceCollectionExtensions.cs (update)
```

## Phase 7: Testing Implementation (3-4 days)

### Unit Tests
- [ ] **Domain model tests**
  - [ ] Coordinate validation tests
  - [ ] Geofence calculation tests
  - [ ] Business logic tests
- [ ] **Application handler tests**
  - [ ] Query handler tests
  - [ ] Command handler tests
  - [ ] Validation tests

### Integration Tests
- [ ] **Repository tests**
  - [ ] Spatial query tests
  - [ ] Performance tests
- [ ] **API endpoint tests**
  - [ ] Coordinate lookup tests
  - [ ] Nearby search tests
  - [ ] Error handling tests

### Test Files to Create
```
test/Platform.Locations.Application.Tests/Locations/
  ├── Domain/LocationCoordinateTests.cs (new)
  ├── Queries/GetLocationByCoordinatesQueryHandlerTests.cs (new)
  ├── Queries/GetNearbyLocationsQueryHandlerTests.cs (new)
  └── Commands/UpdateLocationCoordinatesCommandHandlerTests.cs (new)

test/Platform.Locations.IntegrationTests/
  ├── Repositories/LocationRepositorySpatialTests.cs (new)
  └── Api/LocationsCoordinateApiTests.cs (new)
```

## Phase 8: Documentation (2-3 days)

### API Documentation
- [ ] **Update OpenAPI specs**
- [ ] **Create usage examples**
- [ ] **Document error responses**
- [ ] **Add integration guides**

### Database Documentation
- [ ] **Migration procedures**
- [ ] **Index maintenance guides**
- [ ] **Performance tuning documentation**

### Operational Documentation
- [ ] **Monitoring setup**
- [ ] **Alerting configuration**
- [ ] **Troubleshooting guides**

## Deployment Checklist

### Pre-Deployment
- [ ] **Database migration tested**
- [ ] **All tests passing**
- [ ] **Performance benchmarks met**
- [ ] **Azure Maps service configured**
- [ ] **Monitoring and alerting ready**

### Deployment Steps
- [ ] **Deploy database changes**
- [ ] **Deploy application code**
- [ ] **Configure Azure Maps settings**
- [ ] **Start coordinate population service**
- [ ] **Validate new endpoints**
- [ ] **Monitor performance metrics**

### Post-Deployment
- [ ] **Run coordinate population for existing locations**
- [ ] **Monitor spatial query performance**
- [ ] **Validate geocoding service metrics**
- [ ] **Update API documentation**

## Success Validation

### Functional Tests
- [ ] **Coordinate-based lookup works correctly**
- [ ] **Nearby search returns accurate results**
- [ ] **Geofencing identifies correct location boundaries**
- [ ] **Multi-product filtering respected**
- [ ] **Existing API functionality unaffected**

### Performance Tests
- [ ] **Coordinate queries < 100ms (95th percentile)**
- [ ] **Spatial indexes being used effectively**
- [ ] **1000+ concurrent requests handled**
- [ ] **Memory usage within acceptable limits**

### Integration Tests
- [ ] **Azure Maps geocoding working**
- [ ] **Background coordinate population running**
- [ ] **Error handling and retries functioning**
- [ ] **Monitoring and alerting operational**

## Quick Reference Commands

### Database Operations
```sql
-- Check coordinate data coverage
SELECT 
    Product,
    COUNT(*) as TotalLocations,
    COUNT(Latitude) as LocationsWithCoordinates,
    (COUNT(Latitude) * 100.0 / COUNT(*)) as CoveragePercentage
FROM Locations 
WHERE DeletedAt IS NULL
GROUP BY Product;

-- Test spatial query performance
SET STATISTICS IO ON;
DECLARE @point geography = geography::Point(41.8781, -87.6298, 4326);
SELECT LocationCode, ComputedCoordinates.STDistance(@point) as Distance
FROM Locations 
WHERE ComputedCoordinates.STDistance(@point) <= 5000
ORDER BY Distance;
```

### API Testing
```bash
# Test coordinate lookup
curl -X GET "https://localhost:7067/locations/by-coordinates?lat=41.8781&lng=-87.6298" \
     -H "local-product: TestProduct"

# Test nearby search
curl -X GET "https://localhost:7067/locations/nearby?lat=41.8781&lng=-87.6298&radius=5000&maxResults=5" \
     -H "local-product: TestProduct"

# Register location with coordinates
curl -X POST "https://localhost:7067/locations/register" \
     -H "Content-Type: application/json" \
     -H "local-product: TestProduct" \
     -d '{
       "locationCode": "TEST001",
       "locationTypeCode": "WAREHOUSE",
       "addressLine1": "123 Test St",
       "city": "Chicago",
       "state": "IL",
       "zipCode": "60601",
       "country": "USA",
       "latitude": 41.8781,
       "longitude": -87.6298,
       "geofenceRadius": 100
     }'
```

## Troubleshooting Common Issues

### Spatial Index Problems
- Check index fragmentation: `sys.dm_db_index_physical_stats`
- Rebuild spatial indexes during maintenance windows
- Verify GEOGRAPHY data type usage (not GEOMETRY)

### Performance Issues
- Ensure product filtering in all spatial queries
- Monitor query execution plans for index usage
- Consider coordinate data caching for frequently accessed locations

### Geocoding Service Issues
- Check Azure Maps API key and quota
- Monitor rate limiting and implement backoff
- Verify network connectivity and firewall rules

This checklist provides a comprehensive roadmap for implementing geofencing functionality in your location service. Each phase builds upon the previous one, ensuring a systematic and reliable implementation process.