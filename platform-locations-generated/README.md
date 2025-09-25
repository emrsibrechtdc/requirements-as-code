# Platform Locations Service

This is a .NET 8 ASP.NET Core Web API service implementing the LocationService following Clean Architecture principles and using the Platform.Shared library for infrastructure concerns.

## Architecture Overview

The service follows Clean Architecture with clear separation of concerns:

- **Domain Layer**: Business logic, entities, and domain rules
- **Application Layer**: Use cases, commands, queries, and application services
- **Infrastructure Layer**: Data access and external service integrations  
- **HttpApi Layer**: API endpoints and middleware
- **Host Layer**: Entry point and service configuration

## Key Features

### Platform.Shared Integration
- **Multi-Product Support**: Automatic data segregation by product context
- **CQRS Pattern**: Command Query Responsibility Segregation using MediatR
- **Auditing**: Automatic tracking of entity creation, updates, and deletions
- **Integration Events**: CloudEvents-based event publishing
- **Transaction Management**: Automatic database transaction handling

### API Endpoints

The service implements all endpoints from the OpenAPI specification:

- `POST /locations/register` - Register new locations
- `PUT /locations/{locationCode}/updateaddress` - Update location address
- `GET /locations` - Get locations (supports filtering by location code prefix)
- `DELETE /locations` - Delete locations (soft delete)
- `PUT /locations/{locationCode}/activate` - Activate locations
- `PUT /locations/{locationCode}/deactivate` - Deactivate locations

### Business Logic
- Domain-driven design with rich entity models
- Input validation using FluentValidation
- Business rule enforcement in domain entities
- Custom domain exceptions with proper error handling

## Project Structure

```
Platform.Locations/
├── src/
│   ├── Platform.Locations.Host/                    # Web API Host
│   ├── Core/
│   │   └── Platform.Locations.Domain/              # Domain Layer
│   ├── Database/
│   │   └── Platform.Locations.Database/            # SQL Database Project
│   └── Locations/
│       ├── Platform.Locations.HttpApi/             # HTTP API Layer
│       ├── Platform.Locations.Application/         # Application Layer
│       └── Platform.Locations.Infrastructure/      # Infrastructure Layer
└── test/ ✅ IMPLEMENTED
    ├── Platform.Locations.Application.Tests/      # Unit Tests
    │   ├── CommandHandlers/                       # Command handler tests
    │   ├── QueryHandlers/                         # Query handler tests  
    │   ├── Validators/                             # Input validation tests
    │   └── Utilities/                              # Test utilities
    └── Platform.Locations.IntegrationTests/       # Integration Tests
        ├── Api/                                    # HTTP API endpoint tests
        ├── CommandHandlers/                        # Command handler integration tests
        ├── Infrastructure/                         # Test base classes
        └── Repositories/                           # Repository integration tests
```

## Getting Started

### Prerequisites
- .NET 8.0 SDK
- SQL Server (LocalDB or full instance)
- Access to Azure DevOps coldchain-software artifact feed
- Azure Artifacts Credential Provider (or Personal Access Token)
- Git (for source control)

### NuGet Package Setup

This solution requires access to the **Platform.Shared** library hosted in Azure DevOps Artifacts.

**Package Source**: `coldchain-software`  
**URL**: `https://pkgs.dev.azure.com/DigitalAndConnectedTechnologies/_packaging/coldchain-software/nuget/v3/index.json`

#### Authentication Setup

1. **Install Azure Artifacts Credential Provider (Recommended):**
   ```powershell
   # Run as Administrator
   iwr https://aka.ms/install-artifacts-credprovider.ps1 | iex
   ```

2. **Or configure with Personal Access Token:**
   - Create PAT in Azure DevOps with **Packaging (read)** permission
   - Add authenticated source:
   ```bash
   dotnet nuget add source "https://pkgs.dev.azure.com/DigitalAndConnectedTechnologies/_packaging/coldchain-software/nuget/v3/index.json" --name "coldchain-software" --username "PAT" --password "YOUR_PAT_TOKEN" --store-password-in-clear-text
   ```

3. **Verify configuration:**
   ```bash
   dotnet nuget list source
   ```

The solution includes a `NuGet.Config` file that automatically configures the package sources and mappings.

### Source Control Setup
The solution includes a comprehensive `.gitignore` file that excludes:
- Build artifacts and binaries
- IDE-specific files (.vs/, .vscode/, .idea/)
- Sensitive configuration files (appsettings.*.json)
- Platform.Shared local packages
- Test coverage reports
- Database files

**Important**: Environment-specific configuration files are excluded by design. Use the base `appsettings.json` as a template and create environment-specific files as needed.

### Running the Service

1. **Clone/Extract the solution**
   ```bash
   cd platform-locations-generated
   ```

2. **Build the solution**
   ```bash
   dotnet build
   ```

3. **Deploy database schema**
   ```bash
   # Build database project
   dotnet build src/Database/Platform.Locations.Database/Platform.Locations.Database.sqlproj
   
   # Install SqlPackage tool (if not already installed)
   dotnet tool install -g microsoft.sqlpackage
   
   # Deploy to local database
   SqlPackage.exe /Action:Publish /SourceFile:"src/Database/Platform.Locations.Database/bin/Debug/Platform.Locations.Database.dacpac" /TargetServerName:"(localdb)\\mssqllocaldb" /TargetDatabaseName:"Platform.Locations"
   ```

4. **Run the service**
   ```bash
   dotnet run --project src/Platform.Locations.Host
   ```

5. **Access the API**
   - Swagger UI: `https://localhost:7067/swagger`
   - Health Check: `https://localhost:7067/health`

### Development Testing

In development mode, the service accepts a `local-product` header to simulate multi-product context:

```bash
curl -X POST https://localhost:7067/locations/register \
  -H "Content-Type: application/json" \
  -H "local-product: TestProduct" \
  -d '{
    "locationCode": "LOC001",
    "locationTypeCode": "WAREHOUSE",
    "addressLine1": "123 Main St",
    "city": "Anytown",
    "state": "ST", 
    "zipCode": "12345",
    "country": "USA"
  }'
```

## Configuration

### Database Connection
Update `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=Platform.Locations;Trusted_Connection=true;MultipleActiveResultSets=true"
  }
}
```

### Logging
The service uses Serilog with console and Seq sinks. Configure in `appsettings.json`:
```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "System": "Warning"
      }
    }
  }
}
```

## API Usage Examples

### Register a Location
```bash
POST /locations/register
{
  "locationCode": "WH001",
  "locationTypeCode": "WAREHOUSE", 
  "addressLine1": "100 Industrial Blvd",
  "city": "Manufacturing City",
  "state": "TX",
  "zipCode": "75001",
  "country": "USA"
}
```

### Get Locations
```bash
# Get all locations
GET /locations

# Search by location code prefix (minimum 3 characters)
GET /locations?locationCode=WH0
```

### Update Address
```bash
PUT /locations/WH001/updateaddress
{
  "addressLine1": "200 Industrial Blvd",
  "city": "Manufacturing City", 
  "state": "TX",
  "zipCode": "75002",
  "country": "USA"
}
```

### Activate/Deactivate Location
```bash
PUT /locations/WH001/activate
PUT /locations/WH001/deactivate
```

### Delete Location
```bash
DELETE /locations?locationCode=WH001
```

## Integration Events

The service publishes the following CloudEvents when operations complete:
- `LocationRegisteredIntegrationEvent`
- `LocationAddressUpdatedIntegrationEvent`
- `LocationActivatedIntegrationEvent`
- `LocationDeactivatedIntegrationEvent`
- `LocationDeletedIntegrationEvent`

Events are published with product context in CloudEvents headers for proper routing.

## Database Schema

The Location entity includes:
- **Primary Key**: Id (uniqueidentifier with NEWSEQUENTIALID() for optimal performance)
- **Business Properties**: LocationCode, LocationTypeCode, Address fields
- **Platform.Shared Auditing**: CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, DeletedAt, DeletedBy
- **Multi-Product Support**: Product field for data segregation
- **Activation State**: IsActive flag for activate/deactivate operations

Key indexes:
- Primary clustered index on Id (sequential GUID for optimal performance)
- Unique composite index on (Product, LocationCode)
- Index on Product for multi-tenant queries
- Index on LocationTypeCode for filtering
- Index on IsActive for activation state queries

**Performance Optimization**: The database uses `NEWSEQUENTIALID()` for generating primary key values, which provides better index performance compared to random GUIDs by reducing page splits and improving clustering efficiency.

## Error Handling

The service implements comprehensive error handling:
- **Domain Exceptions**: Business rule violations (422 Unprocessable Entity)
- **Validation Exceptions**: Input validation failures (400 Bad Request)
- **Not Found Exceptions**: Resource not found (404 Not Found)
- **General Exceptions**: System errors (500 Internal Server Error)

All errors return RFC 7807 Problem Details format.

## Platform.Shared Features Used

- **FullyAuditedActivableAggregateRoot**: Base class for Location entity
- **IMultiProductObject**: Multi-tenant data segregation
- **IInstrumentationObject**: Telemetry and monitoring
- **CQRS Interfaces**: ICommand, IQuery, ICommandHandler, IQueryHandler
- **TransactionBehavior**: Automatic transaction management
- **IIntegrationEventPublisher**: CloudEvents publishing
- **PlatformDbContext**: Entity Framework base context
- **EfCoreRepository**: Repository base implementation

## Development Guidelines Followed

This implementation follows the Copeland Platform API Development Guidelines v2.0:
- Clean Architecture with proper layer separation
- Domain-driven design with rich business logic
- CQRS pattern for command/query separation
- Platform.Shared integration for infrastructure concerns
- Minimal API pattern for endpoint implementation
- Comprehensive validation and error handling
- Integration events for decoupled communication
- Multi-product support with automatic data segregation

## Testing Strategy

The service includes comprehensive unit and integration tests following enterprise-grade best practices. See [API Development and Testing Guidelines](API_DEVELOPMENT_AND_TESTING_GUIDELINES.md) for detailed testing standards.

### Test Projects

- **Platform.Locations.Application.Tests** - Unit tests for application logic
- **Platform.Locations.IntegrationTests** - API and database integration tests

### Key Testing Improvements Implemented

#### 1. HTTP API Data Seeding
Integration tests use the actual HTTP registration endpoint instead of direct database seeding:
```bash
# ✅ Uses registration endpoint - ensures multi-product setup
POST /locations/register
{
  "locationCode": "TEST-A1B2C3D4",
  "locationTypeCode": "WAREHOUSE",
  // ... other properties
}
```

**Benefits:**
- Ensures multi-product data filtering works correctly
- Validates complete application layer logic
- Tests actual API contracts
- Prevents `Product` field initialization issues

#### 2. Unique Test Data Strategy
All tests use unique identifiers to prevent conflicts:
```csharp
var locationCode = $"TEST-{Guid.NewGuid().ToString("N")[..8].ToUpper()}";
// Results in: "TEST-A1B2C3D4"
```

**Benefits:**
- Eliminates test interference and race conditions
- Enables parallel test execution
- No database cleanup required between tests

#### 3. HTTP Status Code Standards
Tests validate proper HTTP status codes for different scenarios:
- **400 Bad Request**: Input validation failures (invalid JSON, missing fields)
- **422 Unprocessable Entity**: Business rule violations (duplicate codes, invalid state transitions)
- **200 OK**: Successful operations
- **201 Created**: Resource creation

### Running Tests

```bash
# Run all tests
dotnet test

# Run only unit tests
dotnet test --filter "Category=Unit"

# Run only integration tests
dotnet test test/Platform.Locations.IntegrationTests

# Run with detailed output
dotnet test --verbosity normal
```

## Next Steps

To complete the service implementation:
1. ✅ **Testing Strategy** - Comprehensive unit and integration tests implemented
2. Configure CI/CD pipelines with automated testing
3. Set up production monitoring and alerting
4. Add API client library generation
5. Configure authentication and authorization for production
6. Add performance testing and load testing
7. Implement health checks and metrics collection

## Support

For questions or issues related to Platform.Shared integration, refer to the Platform.Shared documentation or contact the platform team.