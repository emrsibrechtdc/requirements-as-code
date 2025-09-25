# Platform Customers Service

This is a .NET 8 ASP.NET Core Web API service implementing the Customer Service following Clean Architecture principles and using the Platform.Shared library for infrastructure concerns.

## Architecture Overview

The service follows Clean Architecture with clear separation of concerns:

- **Domain Layer**: Business logic, entities, and domain rules
- **Application Layer**: Use cases, commands, queries, and application services
- **Infrastructure Layer**: Data access and external service integrations  
- **HttpApi Layer**: API endpoints and middleware
- **Host Layer**: Entry point and service configuration
- **Database Layer**: SQL Database Project for schema management

## Key Features

### Platform.Shared Integration
- **Multi-Product Support**: Automatic data segregation by product context
- **CQRS Pattern**: Command Query Responsibility Segregation using MediatR
- **Auditing**: Automatic tracking of entity creation, updates, and deletions
- **Integration Events**: CloudEvents-based event publishing
- **Transaction Management**: Automatic database transaction handling

### API Endpoints

The service implements customer management endpoints:

- `POST /customers/create` - Create new customers
- `PUT /customers/{customerCode}/update` - Update customer information
- `GET /customers` - Get customers (supports filtering by customer code, type, company name, active status)
- `GET /customers/{customerCode}` - Get specific customer by code

### Business Logic
- Domain-driven design with rich entity models
- Input validation using FluentValidation
- Business rule enforcement in domain entities
- Custom domain exceptions with proper error handling
- Value objects for ContactInfo and Address

## Project Structure

```
Platform.Customers/
├── src/
│   ├── Platform.Customers.Host/                    # Web API Host
│   ├── Core/
│   │   └── Platform.Customers.Domain/              # Domain Layer
│   ├── Database/
│   │   └── Platform.Customers.Database/            # SQL Database Project
│   └── Customers/
│       ├── Platform.Customers.HttpApi/             # HTTP API Layer
│       ├── Platform.Customers.Application/         # Application Layer
│       └── Platform.Customers.Infrastructure/      # Infrastructure Layer
└── test/ (to be created)
    ├── Core/
    │   └── Platform.Customers.Domain.UnitTests/
    ├── Customers/
    │   ├── Platform.Customers.Application.UnitTests/
    │   └── Platform.Customers.Infrastructure.IntegrationTests/
    └── Platform.Customers.ApiClient.IntegrationTests/
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
   cd platform-customers-generated
   ```

2. **Build the solution**
   ```bash
   dotnet build
   ```

3. **Deploy database schema**
   ```bash
   # Build database project
   dotnet build src/Database/Platform.Customers.Database/Platform.Customers.Database.sqlproj
   
   # Install SqlPackage tool (if not already installed)
   dotnet tool install -g microsoft.sqlpackage
   
   # Deploy to local database
   SqlPackage.exe /Action:Publish /SourceFile:"src/Database/Platform.Customers.Database/bin/Debug/Platform.Customers.Database.dacpac" /TargetServerName:"(localdb)\mssqllocaldb" /TargetDatabaseName:"Platform.Customers"
   ```

4. **Run the service**
   ```bash
   dotnet run --project src/Platform.Customers.Host
   ```

5. **Access the API**
   - Swagger UI: `https://localhost:7067/swagger`
   - Health Check: `https://localhost:7067/health`

### Development Testing

In development mode, the service accepts a `local-product` header to simulate multi-product context:

```bash
curl -X POST https://localhost:7067/customers/create \
  -H "Content-Type: application/json" \
  -H "local-product: TestProduct" \
  -d '{
    "customerCode": "CUST001",
    "customerType": "ENTERPRISE",
    "companyName": "Test Company",
    "contactFirstName": "John",
    "contactLastName": "Doe",
    "email": "john.doe@testcompany.com",
    "phoneNumber": "555-0123",
    "addressLine1": "123 Test St",
    "city": "Test City",
    "state": "TS",
    "postalCode": "12345",
    "country": "USA"
  }'
```

## Configuration

### Database Connection
Update `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=Platform.Customers;Trusted_Connection=true;MultipleActiveResultSets=true"
  }
}
```

### EventGrid Configuration
Configure EventGrid URL for integration events:
```json
{
  "EventGrid": {
    "Url": "https://dev-pfm-common-eus2-evgd.eastus2-1.eventgrid.azure.net/api/events"
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

### Create a Customer
```bash
POST /customers/create
{
  "customerCode": "CUST001",
  "customerType": "ENTERPRISE",
  "companyName": "Acme Corporation",
  "contactFirstName": "John",
  "contactLastName": "Smith",
  "email": "john.smith@acme.com",
  "phoneNumber": "555-0101",
  "addressLine1": "123 Business Ave",
  "addressLine2": "Suite 100",
  "city": "Atlanta",
  "state": "GA",
  "postalCode": "30309",
  "country": "USA"
}
```

### Get Customers
```bash
# Get all customers
GET /customers

# Search by customer code
GET /customers?customerCode=CUST

# Filter by customer type
GET /customers?customerType=ENTERPRISE

# Filter by company name
GET /customers?companyName=Acme

# Filter by active status
GET /customers?isActive=true
```

### Update Customer
```bash
PUT /customers/CUST001/update
{
  "companyName": "Acme Corporation Updated",
  "contactFirstName": "John",
  "contactLastName": "Smith",
  "email": "john.smith@acme.com",
  "phoneNumber": "555-0101",
  "addressLine1": "456 Updated Ave",
  "city": "Atlanta",
  "state": "GA",
  "postalCode": "30309",
  "country": "USA"
}
```

### Get Customer by Code
```bash
GET /customers/CUST001
```

## Integration Events

The service publishes the following CloudEvents when operations complete:
- `CustomerCreatedIntegrationEvent`
- `CustomerUpdatedIntegrationEvent`

Events are published with product context in CloudEvents headers for proper routing.

## Database Schema

### Customers Table
- **Primary Key**: Id (uniqueidentifier with NEWSEQUENTIALID() for optimal performance)
- **Multi-Product Support**: Product (nvarchar(50))
- **Business Key**: CustomerCode (nvarchar(50))
- **Customer Type**: CustomerType (nvarchar(20)) - ENTERPRISE, SMALL_BUSINESS, RETAIL, INDIVIDUAL
- **Company Information**: CompanyName, ContactFirstName, ContactLastName
- **Contact Information**: Email, PhoneNumber
- **Address Fields**: AddressLine1, AddressLine2, City, State, PostalCode, Country
- **Platform.Shared Auditing**: CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, DeletedAt, DeletedBy
- **Activation State**: IsActive flag for activate/deactivate operations

**Performance Optimization**: The database uses `NEWSEQUENTIALID()` for generating primary key values, which provides better index performance compared to random GUIDs by reducing page splits and improving clustering efficiency.

Key indexes:
- Primary clustered index on Id (sequential GUID for optimal performance)
- Unique composite index on (Product, CustomerCode)
- Index on Product for multi-tenant queries
- Index on CustomerType for filtering
- Index on IsActive for activation state queries
- Index on Email for lookup operations
- Index on CompanyName for search operations

## Error Handling

The service implements comprehensive error handling:
- **Domain Exceptions**: Business rule violations (422 Unprocessable Entity)
- **Validation Exceptions**: Input validation failures (400 Bad Request)
- **Not Found Exceptions**: Resource not found (404 Not Found)
- **General Exceptions**: System errors (500 Internal Server Error)

All errors return RFC 7807 Problem Details format.

## Platform.Shared Features Used

- **FullyAuditedActivableAggregateRoot**: Base class for Customer entity
- **IMultiProductObject**: Multi-tenant data segregation
- **IInstrumentationObject**: Telemetry and monitoring
- **CQRS Interfaces**: ICommand, IQuery, ICommandHandler, IQueryHandler
- **TransactionBehavior**: Automatic transaction management
- **IIntegrationEventPublisher**: CloudEvents publishing
- **PlatformDbContext**: Entity Framework base context
- **EfCoreRepository**: Repository base implementation

## Development Guidelines Followed

This implementation follows the Copeland Platform API Development Guidelines v2.1:
- Clean Architecture with proper layer separation
- Domain-driven design with rich business logic
- CQRS pattern for command/query separation
- Platform.Shared integration for infrastructure concerns
- Minimal API pattern for endpoint implementation
- Comprehensive validation and error handling
- Integration events for decoupled communication
- Multi-product support with automatic data segregation
- Database-first approach with SQL Database Projects
- Sequential GUID optimization for performance

## Next Steps

To complete the service implementation:
1. Add unit tests for domain logic
2. Add integration tests for API endpoints  
3. Add application service unit tests
4. Configure CI/CD pipelines
5. Set up production monitoring and alerting
6. Add API client library generation
7. Configure authentication and authorization for production

## Support

For questions or issues related to Platform.Shared integration, refer to the Platform.Shared documentation or contact the platform team.