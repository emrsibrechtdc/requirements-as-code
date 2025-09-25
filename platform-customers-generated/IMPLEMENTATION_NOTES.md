# Implementation Notes

## Platform.Shared Dependency

This Customer Service implementation is designed to use the **Platform.Shared** library for infrastructure concerns. Platform.Shared is hosted in a private Azure DevOps Artifacts feed that requires authentication.

**Package Source**: `coldchain-software`  
**URL**: `https://pkgs.dev.azure.com/DigitalAndConnectedTechnologies/_packaging/coldchain-software/nuget/v3/index.json`

## Current Status

✅ **Complete Architecture Implementation**:
- Clean Architecture with proper layer separation
- Domain-driven design with rich business logic  
- CQRS pattern using MediatR interfaces
- Comprehensive validation and error handling
- Integration events for decoupled communication
- Multi-product support architecture
- Database schema and Entity Framework configuration
- SQL Database Project for enterprise-grade schema management

✅ **Runtime Status**: 
The solution now builds and runs successfully with the following Platform packages from the coldchain-software artifact feed:
- **Platform.Shared** version 1.1.20250915.1
- **Platform.Common.EventGrid** version 5.20250909.1501

## Platform.Shared Integration Implementation

The solution has been successfully implemented using the latest Platform.Shared version with the following key components:

### API Implementation Applied
1. **CQRS Interfaces**: Uses `Platform.Shared.Cqrs.Mediatr` namespace for all commands and queries
2. **Repository Pattern**: Uses `Platform.Shared.DataLayer.Repositories` namespace with domain-specific interfaces  
3. **Integration Events**: Uses `SaveIntegrationEvent()` method (synchronous) for event publishing
4. **Database Context**: Implements Platform.Shared `PlatformDbContext` with `ILogger<T>` injection
5. **Entity Access**: Uses `DbContext.Set<T>()` for entity operations in repositories
6. **API Documentation**: Uses `WithSummary()` and `WithDescription()` for OpenAPI documentation
7. **Service Registration**: Organized Platform.Shared services across appropriate project layers:
   - `AddPlatformCommonHttpApi()`, `WithAuditing()`, `WithMultiProduct()` → HttpApi project
   - `AddIntegrationEventsServices()` → Application project
   - `AddCustomersIntegrationEvents()` → Infrastructure project (EventGrid messaging setup)
8. **EventGrid Integration**: Complete EventGrid messaging infrastructure with:
   - `EventGridMessageEnvelopePublisher` for publishing integration events to Azure EventGrid
   - `MessagePublisher` and `IMessageEnvelopePublisher` abstractions
   - Azure EventGrid client configuration with `AddAzureClients()`
   - **EventGrid URL Configuration**: Configured in appsettings.json for service registration

### Database Project Implementation

**Database-First Approach**: Migrated from EF Core migrations to SQL Database Project approach:
- Created Platform.Customers.Database project using MSBuild.Sdk.SqlProj
- Removed EF Core migrations from Infrastructure project
- **NEWSEQUENTIALID() Optimization**: All sample data and table defaults use sequential GUIDs for optimal database performance
- DACPAC-based deployment for enterprise-grade schema management
- Proper batch separators ("GO") added between SQL statements
- Sample data includes multiple products for testing data segregation

### Customer Domain Model

**Rich Domain Implementation**:
- Customer aggregate root with Platform.Shared base classes (`FullyAuditedActivableAggregateRoot<Guid>`)
- Value objects for ContactInfo and Address with domain validation
- Domain exceptions with Platform.Shared `BusinessException` base class
- Static factory methods for entity creation with domain invariant validation
- Business logic methods for updating customer information

### Application Layer Features

**CQRS Implementation**:
- `CreateCustomerCommand` and `UpdateCustomerCommand` with comprehensive validation
- `GetCustomersQuery` with filtering by customer code, type, company name, and active status
- FluentValidation validators for input validation
- AutoMapper profiles for object mapping
- Integration events: `CustomerCreatedIntegrationEvent` and `CustomerUpdatedIntegrationEvent`

### API Endpoints

**RESTful API Design**:
- `POST /customers/create` - Create new customers
- `PUT /customers/{customerCode}/update` - Update customer information  
- `GET /customers` - Get customers with optional filtering
- `GET /customers/{customerCode}` - Get specific customer by code
- Comprehensive OpenAPI documentation with response types
- Authorization support (disabled for development)

### Platform.Shared Service Registration Requirements

**Critical Platform.Shared Services**: The following services require manual registration for proper Platform.Shared integration:

1. **Context Providers** (manually registered):
   ```csharp
   // Required for multi-product data segregation and auditing
   services.AddScoped<IMultiProductRequestContextProvider, CustomersMultiProductRequestContextProvider>();
   services.AddScoped<IRequestContextProvider, CustomersRequestContextProvider>();
   ```

2. **Repository Pattern**: Uses domain-specific repository interfaces:
   ```csharp
   // ✅ Correct - Use domain repository interfaces
   public class GetCustomersQueryHandler : IQueryHandler<GetCustomersQuery, List<CustomerDto>>
   {
       private readonly ICustomerRepository _repository; // Domain-specific interface
   }
   
   // ❌ Avoided - Platform.Shared generic repositories not used in queries
   // private readonly IReadRepository<Customer, Guid> _repository;
   ```

3. **EventGrid Configuration**: EventGrid URL configured in appsettings.json:
   ```json
   {
     "EventGrid": {
       "Url": "https://dev-pfm-common-eus2-evgd.eastus2-1.eventgrid.azure.net/api/events"
     }
   }
   ```

**Service Registration Order**: Platform.Shared services registered in correct order:
1. HttpApi project: `AddPlatformCommonHttpApi().WithAuditing().WithMultiProduct()`
2. Application project: `AddIntegrationEventsServices()`
3. Infrastructure project: Domain repositories and EventGrid setup

### Dependencies Added
- `FluentValidation.DependencyInjectionExtensions` v11.9.0
- `Platform.Common.EventGrid` v5.20250909.1501 (for EventGrid integration messaging)
- `Microsoft.Extensions.Azure` v1.13.0 (for Azure client configuration)
- `Microsoft.Bcl.AsyncInterfaces` v9.0.2 (to resolve runtime dependency conflicts)
- `AutoMapper` v12.0.1 and `AutoMapper.Extensions.Microsoft.DependencyInjection` v12.0.1
- `Azure.Messaging.EventGrid` v4.27.0

### Key Namespaces for Platform Integration
- **Platform.Shared.EntityFrameworkCore**: For `PlatformDbContext` implementation
- **Platform.Common.Messaging**: For messaging abstractions (`IMessagePublisher`, `IMessageEnvelopePublisher`, `IMessageSerializer`)
- **EventGrid**: For `EventGridMessageEnvelopePublisher` implementation
- **Platform.Shared.DataLayer**: For repository patterns and data layer abstractions
- **Platform.Shared.IntegrationEvents**: For integration event interfaces and base classes
- **Platform.Shared.Cqrs.Mediatr**: For CQRS command/query interfaces and handlers

## Build Requirements

### Option 1: Access to Platform.Shared Package
The solution is configured with the coldchain-software package source. To access Platform.Shared:

1. **Install Azure Artifacts Credential Provider:**
   ```powershell
   # Run as Administrator
   iwr https://aka.ms/install-artifacts-credprovider.ps1 | iex
   ```

2. **Or configure Personal Access Token:**
   ```bash
   # Create PAT in Azure DevOps with Packaging (read) permission
   dotnet nuget add source "https://pkgs.dev.azure.com/DigitalAndConnectedTechnologies/_packaging/coldchain-software/nuget/v3/index.json" --name "coldchain-software" --username "PAT" --password "YOUR_PAT_TOKEN" --store-password-in-clear-text
   ```

3. **Restore and build:**
   ```bash
   dotnet restore
   dotnet build
   ```

### Database Deployment

1. **Build database project:**
   ```bash
   dotnet build src/Database/Platform.Customers.Database/Platform.Customers.Database.sqlproj
   ```

2. **Deploy to local database:**
   ```bash
   # Install SqlPackage tool (if not already installed)
   dotnet tool install -g microsoft.sqlpackage
   
   # Deploy to local database
   SqlPackage.exe /Action:Publish /SourceFile:"src/Database/Platform.Customers.Database/bin/Debug/Platform.Customers.Database.dacpac" /TargetServerName:"(localdb)\mssqllocaldb" /TargetDatabaseName:"Platform.Customers"
   ```

## Architecture Benefits

This implementation demonstrates:
- **Clean Architecture**: Clear separation of concerns across layers
- **Domain-Driven Design**: Rich domain models with business logic
- **CQRS Pattern**: Separate command and query responsibilities  
- **Repository Pattern**: Abstracted data access layer with domain-specific interfaces
- **Validation Strategy**: Multi-layer validation approach (Input → Application → Domain)
- **Event-Driven Architecture**: Integration events for loose coupling
- **API Design**: RESTful endpoints following OpenAPI specification
- **Database Optimization**: Sequential GUIDs and optimal indexing strategy
- **Enterprise Deployment**: DACPAC-based database deployment

## Code Quality

The implementation follows enterprise-grade patterns and practices:
- Immutable DTOs using records
- Comprehensive input validation with FluentValidation
- Domain invariant enforcement in aggregate roots
- Clean error handling with proper HTTP status codes
- Structured logging configuration with Serilog
- Database indexing strategy optimized for multi-product queries
- API versioning support
- OpenAPI documentation with comprehensive response types
- Sequential GUID optimization for database performance

## Customer-Specific Implementation Details

### Customer Entity Features
- **Rich Value Objects**: ContactInfo and Address with validation
- **Customer Types**: ENTERPRISE, SMALL_BUSINESS, RETAIL, INDIVIDUAL
- **Email Uniqueness**: Enforced across product boundaries
- **Address Validation**: Comprehensive address format validation
- **Contact Information**: First name, last name, email, and optional phone number

### Business Rules Implemented
- Customer code uniqueness within product
- Email uniqueness within product  
- Customer type validation against predefined constants
- Address and contact information format validation
- Domain invariant validation in entity creation and updates

### API Features
- Customer filtering by code, type, company name, and active status
- Product context isolation in development mode via `local-product` header
- Comprehensive validation with detailed error messages
- Integration events for customer lifecycle operations

## Next Steps

1. **If using Platform.Shared**: The solution is ready to build and run
2. **Add comprehensive tests**: Unit tests for domain logic, integration tests for APIs
3. **Configure CI/CD**: Set up build and deployment pipelines with database deployment
4. **Production configuration**: Authentication, authorization, monitoring, and EventGrid setup

The architecture and patterns demonstrated here provide a solid foundation for an enterprise-grade customer management microservice with Platform.Shared integration.