# Platform.Shared Library Documentation

## Table of Contents
1. [Overview](#overview)
2. [Architecture](#architecture)
3. [Core Components](#core-components)
4. [Installation and Configuration](#installation-and-configuration)
5. [API Reference](#api-reference)
6. [Usage Examples](#usage-examples)
7. [Best Practices](#best-practices)
8. [Dependencies](#dependencies)

## Overview

The Platform.Shared library is a .NET 8.0 shared library that provides common infrastructure components for building enterprise applications. It implements Domain-Driven Design (DDD) patterns, Command Query Responsibility Segregation (CQRS), auditing, caching, integration events, and multi-product support.

### Key Features
- **DDD & CQRS Support**: Base classes for entities, aggregates, and CQRS patterns
- **Auditing System**: Comprehensive audit trail functionality
- **Integration Events**: Event-driven architecture support
- **Multi-Product Support**: Multi-tenancy capabilities
- **Caching**: Redis and in-memory caching implementations
- **Entity Framework Integration**: Repository patterns and unit of work
- **HTTP API Extensions**: Common HTTP API setup and configuration
- **Instrumentation**: Application insights and telemetry support

## Architecture

The library follows a layered architecture with clear separation of concerns:

```
Platform.Shared/
├── Auditing/          # Audit trail functionality
├── Caching/           # Caching abstractions and implementations
├── Cqrs/              # CQRS patterns and behaviors
├── DataLayer/         # Data access abstractions
├── Ddd.Domain/        # Domain-driven design base classes
├── Entities/          # Entity base classes and interfaces
├── EntityFrameworkCore/ # EF Core implementations
├── Exceptions/        # Custom exception types
├── HttpApi/           # HTTP API extensions and middleware
├── Instrumentation/   # Telemetry and instrumentation
├── IntegrationEvents/ # Event-driven architecture
├── MultiProduct/      # Multi-tenancy support
└── RequestContext/    # Request context and user context
```

## Core Components

### 1. Auditing System

The auditing system provides automatic tracking of entity changes with timestamps and user information.

#### Key Interfaces:
- `IAuditPropertySetter`: Sets audit properties on entities
- `ICreateAuditedEntity`: Entities that track creation
- `IUpdateAuditedEntity`: Entities that track updates
- `IDeleteAuditedEntity`: Entities that track deletions
- `IFullyAuditedEntity`: Entities with complete audit trail

#### Base Classes:
- `CreateAuditedEntity`: Base class for create-audited entities
- `FullyAuditedEntity`: Base class for fully audited entities
- `FullyAuditedAggregateRoot`: Audited aggregate root implementation

### 2. Data Layer

Provides abstraction layer for data access with repository pattern and unit of work.

#### Key Interfaces:
- `IUnitOfWork`: Manages database transactions
- `IRepository<TEntity, TId>`: Generic repository interface
- `IReadRepository<TEntity, TId>`: Read-only repository
- `IWriteRepository<TEntity, TId>`: Write-only repository
- `IDataFilter`: Data filtering functionality

### 3. CQRS Support

Implements Command Query Responsibility Segregation using MediatR.

#### Key Components:
- `TransactionBehavior<TRequest, TResponse>`: Handles transactions for commands
- `IRequest`: Base interface for CQRS requests

### 4. Integration Events

Supports event-driven architecture for decoupled communication between services using CloudEvents standard.

#### Key Interfaces:
- `IIntegrationEvent`: Base interface for integration events
- `IIntegrationEventPublisher`: Publishes integration events
- `IIntegrationEventProcessor`: Processes integration events
- `IIntegrationEventSubscriptionsManager`: Manages event subscriptions

#### CloudEvents Architecture:
- **Clean Business Payloads**: Events contain only business-relevant data
- **Header-Based Routing**: Product context is automatically included in CloudEvents headers
- **Infrastructure-Level Filtering**: Event routing and filtering based on headers
- **Schema Simplicity**: Events remain focused on business semantics

### 5. Multi-Product Support

Enables multi-tenancy and multi-product scenarios.

#### Key Interfaces:
- `IMultiProductObject`: Objects that belong to a specific product
- `IMultiProductRequestContextProvider`: Provides product context
- `IProductSetter`: Sets product information on objects

### 6. Caching

Provides caching abstractions with Redis and in-memory implementations.

#### Components:
- `InMemoryCacheRequestHandler`: In-memory caching implementation
- `RedisCacheRepository`: Redis caching repository
- `RedisCacheRequestHandler`: Redis caching request handler

## Installation and Configuration

### Prerequisites
- .NET 8.0 or later
- Entity Framework Core 8.0.7
- MediatR 12.4.0

### NuGet Dependencies
The library includes the following key dependencies:
- MediatR for CQRS implementation
- Microsoft.EntityFrameworkCore for data access
- Microsoft.Extensions.Caching.StackExchangeRedis for Redis caching
- Microsoft.ApplicationInsights.AspNetCore for telemetry
- FluentValidation for validation
- Serilog for logging

### Configuration

#### 1. Basic Setup
```csharp
public void ConfigureServices(IServiceCollection services)
{
    // Add Platform Common HTTP API with fluent configuration (v1.1.20250915.1+)
    services.AddPlatformCommonHttpApi(configuration, configBuilder, env, "YourModule")
           .WithAuditing()
           .WithMultiProduct();
    
    // Add integration events (typically in Application project)
    services.AddIntegrationEventsServices();
}
```

#### 2. Entity Framework Configuration
```csharp
public class YourDbContext : PlatformDbContext
{
    public YourDbContext(DbContextOptions<YourDbContext> options, ILogger<YourDbContext> logger) : base(options, logger)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Configure your entities
        // Note: ConfigurePlatformEntities() may have changed in v1.1.20250915.1
        // modelBuilder.ConfigurePlatformEntities();
    }
}
```

#### 3. MediatR Configuration
```csharp
services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));
services.AddTransient(typeof(IPipelineBehavior<,>), typeof(TransactionBehavior<,>));
```

## API Reference

### Auditing

#### IAuditPropertySetter
```csharp
public interface IAuditPropertySetter
{
    void SetCreateAuditedProperties(ICreateAuditedEntity entity);
    void SetUpdateAuditedProperties(IUpdateAuditedEntity entity);
    void SetDeleteAuditedProperties(IDeleteAuditedEntity entity);
}
```

#### ICreateAuditedEntity
```csharp
public interface ICreateAuditedEntity
{
    DateTime CreatedAt { get; set; }
    string? CreatedBy { get; set; }
}
```

### Data Layer

#### IUnitOfWork
```csharp
public interface IUnitOfWork : IDisposable
{
    bool HasActiveTransaction { get; }
    Task<int> SaveEntitiesAsync(CancellationToken cancellationToken = default);
    Task RollbackTransaction(CancellationToken cancellationToken = default);
    Task ExecuteOperationInTransactionAsync(Func<Task> operation);
    Guid? GetCurrentTransactionId();
    Task<Guid> BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task<int> CommitTransactionAsync(CancellationToken cancellationToken = default);
}
```

#### IRepository<TEntity, TId>
```csharp
public interface IRepository<TEntity, TId> : IReadRepository<TEntity, TId>, IWriteRepository<TEntity, TId>
    where TEntity : IEntity<TId>
{
}
```

### Integration Events

#### IIntegrationEventPublisher
```csharp
public interface IIntegrationEventPublisher
{
    Task PublishAllIntegrationEvents(CancellationToken cancellationToken = default);
    void AddIntegrationEvent(IIntegrationEvent integrationEvent);
}
```

#### IIntegrationEventProcessor
```csharp
public interface IIntegrationEventProcessor
{
    Task ProcessEvent(string eventName, string integrationEvent);
}
```

### Instrumentation

#### IInstrumentationHelper
```csharp
public interface IInstrumentationHelper
{
    Dictionary<string, string> GetInstrumentationProperties(IInstrumentationObject instrumentationObject);
    void SaveInstrumentationEvent(string eventName, Dictionary<string, string> properties);
    void SaveInstrumentationObjectEvent(string eventName, IInstrumentationObject instrumentationObject);
    void PublishInstrumentationEvents();
}
```

## Usage Examples

### 1. Creating an Audited Multi-Product Entity

```csharp
public class Device : FullyAuditedAggregateRoot<Guid>, IMultiProductObject
{
    public string SerialNumber { get; private set; }
    public string Model { get; private set; }
    public string Location { get; private set; }
    public DeviceStatus Status { get; private set; }
    public string Product { get; set; } // Multi-product property - set by framework

    // Parameterless constructor for EF Core
    private Device() { }

    public Device(string serialNumber, string model, string location)
    {
        Id = Guid.NewGuid();
        SerialNumber = serialNumber;
        Model = model;
        Location = location;
        Status = DeviceStatus.Active;
        // Product will be set by the framework based on caller context
    }

    public void UpdateLocation(string newLocation)
    {
        if (string.IsNullOrWhiteSpace(newLocation))
            throw new BusinessException("INVALID_LOCATION", "Device location cannot be empty");
            
        Location = newLocation;
    }

    public void SetStatus(DeviceStatus status)
    {
        Status = status;
    }
}

public enum DeviceStatus
{
    Active,
    Inactive,
    Maintenance,
    Decommissioned
}
```

### 2. Implementing a Command Handler with Multi-Product Context

```csharp
public class CreateDeviceCommand : ICommand<Guid>
{
    public string SerialNumber { get; set; }
    public string Model { get; set; }
    public string Location { get; set; }
}

public class CreateDeviceCommandHandler : ICommandHandler<CreateDeviceCommand, Guid>
{
    private readonly IRepository<Device, Guid> _deviceRepository;
    private readonly IMultiProductRequestContextProvider _contextProvider;
    private readonly IIntegrationEventPublisher _eventPublisher;

    public CreateDeviceCommandHandler(
        IRepository<Device, Guid> deviceRepository,
        IMultiProductRequestContextProvider contextProvider,
        IIntegrationEventPublisher eventPublisher)
    {
        _deviceRepository = deviceRepository;
        _contextProvider = contextProvider;
        _eventPublisher = eventPublisher;
    }

    public async Task<Guid> Handle(CreateDeviceCommand request, CancellationToken cancellationToken)
    {
        // Product context is automatically set by ASP.NET Core middleware
        // and is available through the context provider
        var currentProduct = await _contextProvider.GetProductAsync();
        
        var device = new Device(request.SerialNumber, request.Model, request.Location);
        
        // Product is automatically set by the repository/framework - no manual setting needed
        await _deviceRepository.AddAsync(device, cancellationToken);
        
        // Publish clean business integration event
        // Product context is automatically included in CloudEvents headers by the framework
        var integrationEvent = new DeviceCreatedIntegrationEvent(
            device.Id, 
            device.SerialNumber, 
            device.Model,
            device.Location);
        _eventPublisher.AddIntegrationEvent(integrationEvent);
        
        return device.Id;
    }
}
```

### 3. Creating Clean Integration Events

```csharp
public class DeviceCreatedIntegrationEvent : IIntegrationEvent
{
    public Guid DeviceId { get; }
    public string SerialNumber { get; }
    public string Model { get; }
    public string Location { get; }
    public DateTime OccurredAt { get; }

    public DeviceCreatedIntegrationEvent(Guid deviceId, string serialNumber, string model, string location)
    {
        DeviceId = deviceId;
        SerialNumber = serialNumber;
        Model = model;
        Location = location;
        OccurredAt = DateTime.UtcNow;
    }
}

public class DeviceCreatedIntegrationEventHandler : IIntegrationEventHandler<DeviceCreatedIntegrationEvent>
{
    private readonly ILogger<DeviceCreatedIntegrationEventHandler> _logger;
    private readonly IDeviceProvisioningService _provisioningService;
    private readonly IRepository<DeviceSetupRecord, Guid> _setupRecordRepository;
    
    public DeviceCreatedIntegrationEventHandler(
        ILogger<DeviceCreatedIntegrationEventHandler> logger,
        IDeviceProvisioningService provisioningService,
        IRepository<DeviceSetupRecord, Guid> setupRecordRepository)
    {
        _logger = logger;
        _provisioningService = provisioningService;
        _setupRecordRepository = setupRecordRepository;
    }

    public async Task Handle(DeviceCreatedIntegrationEvent @event)
    {
        // Focus on business logic - no manual product context handling
        _logger.LogInformation(
            "Device created: {SerialNumber} ({Model})",
            @event.SerialNumber,
            @event.Model);

        // Business logic: Provision the device
        await _provisioningService.ProvisionDeviceAsync(
            @event.DeviceId,
            @event.SerialNumber,
            @event.Model);

        // Create setup record - Platform.Shared handles product segregation automatically
        var setupRecord = DeviceSetupRecord.Create(
            @event.DeviceId,
            @event.SerialNumber,
            "Device provisioned successfully");
            
        await _setupRecordRepository.AddAsync(setupRecord);
        
        // Infrastructure automatically handles:
        // - Product-based data segregation in repository operations
        // - Service-level product context routing
        // - Audit trail with correct product association
    }
}
```

**Important**: Integration events are published as **CloudEvents** with the product context included in the **headers**, not in the payload. This enables:
- **Clean Business Events**: Event payloads contain only business-relevant data
- **Infrastructure-Level Routing**: Product information is used for routing without polluting the business event
- **Header-Based Filtering**: Event consumers can be filtered by product at the infrastructure level
- **Event Schema Simplicity**: Events remain focused on business semantics, not technical routing concerns

### 4. Using Caching with Product Context

```csharp
public class GetDeviceQuery : IQuery<Device?>
{
    public Guid DeviceId { get; set; }
}

public class GetDeviceQueryHandler : IQueryHandler<GetDeviceQuery, Device?>
{
    private readonly IReadRepository<Device, Guid> _deviceRepository;

    public GetDeviceQueryHandler(IReadRepository<Device, Guid> deviceRepository)
    {
        _deviceRepository = deviceRepository;
    }

    public async Task<Device?> Handle(GetDeviceQuery request, CancellationToken cancellationToken)
    {
        // Product context is automatically applied via data filters in the repository
        // This ensures the device can only be retrieved if it belongs to the caller's product
        var device = await _deviceRepository.GetByIdAsync(request.DeviceId, cancellationToken);
        
        // Device will be null if:
        // 1. It doesn't exist
        // 2. It exists but belongs to a different product (filtered out by framework)
        return device;
    }
}
```

### 5. Multi-Product Data Segregation in Action

```csharp
// Example of how product context flows through the application
public class DeviceService
{
    private readonly IMultiProductRequestContextProvider _contextProvider;
    private readonly IRepository<Device, Guid> _deviceRepository;

    public DeviceService(
        IMultiProductRequestContextProvider contextProvider,
        IRepository<Device, Guid> deviceRepository)
    {
        _contextProvider = contextProvider;
        _deviceRepository = deviceRepository;
    }

    public async Task<IEnumerable<Device>> GetDevicesForCurrentProductAsync()
    {
        // Repository automatically filters by current product context
        // No need to manually filter - data segregation is handled by the framework
        return await _deviceRepository.GetAllAsync();
    }

    public async Task<Device> CreateDeviceAsync(string serialNumber, string model, string location)
    {
        var device = new Device(serialNumber, model, location);
        
        // Product context is automatically applied by the framework during save
        // No manual product setting needed - middleware has already set the context
        return await _deviceRepository.AddAsync(device);
    }
}

// Note: Product context middleware is implemented in each service's ASP.NET Core layer
// It's not part of Platform.Shared but sets up the context that Platform.Shared uses
```

**Important Architecture Note**: The product context is set by ASP.NET Core middleware in each service (not part of Platform.Shared). This middleware:

1. **Extracts caller identity** from the HTTP request (JWT claims, API keys, client certificates, etc.)
2. **Maps the caller to a product** using business logic specific to each service
3. **Sets the product context** in `IMultiProductRequestContextProvider`
4. **Ensures all subsequent operations** use the correct product context automatically

Once the middleware has set the product context, all Platform.Shared components automatically use it for data segregation without any manual intervention.

## Best Practices

### 1. Entity Design
- Inherit from appropriate base classes (`FullyAuditedAggregateRoot`, `FullyAuditedEntity`, etc.)
- Implement `IMultiProductObject` for multi-tenant scenarios
- Use value objects for complex properties that don't have identity

### 2. Command/Query Handlers
- Keep handlers focused on a single responsibility
- Use the `TransactionBehavior` for commands that modify data
- Implement proper error handling and validation

### 3. Integration Events
- Design events to be immutable
- Keep event payloads clean - include only business data, no infrastructure concerns
- Product context is handled automatically via CloudEvents headers
- Use versioning for event schema evolution
- Event handlers can access product context via `IMultiProductRequestContextProvider`

### 4. Repository Pattern
- Use `IReadRepository` for read-only operations
- Use `IRepository` for full CRUD operations
- Implement caching at the repository level when appropriate

### 5. Auditing
- Enable auditing for all domain entities
- Implement `IAuditPropertySetter` to customize audit behavior
- Use audit information for compliance and debugging

### 6. Multi-Product Support
- Product context is automatically handled by the framework - no manual setting needed
- Data filters automatically ensure product isolation
- Test cross-product scenarios thoroughly
- Never include product context in integration event payloads - it's handled via headers

### 7. Error Handling
- Use `BusinessException` for domain-specific errors
- Implement proper exception handling in command handlers
- Log errors appropriately for debugging and monitoring

### 8. Performance Considerations
- Use caching for frequently accessed read-only data
- Implement proper indexing in database design
- Monitor and optimize database queries
- Use async/await patterns consistently

## Dependencies

### Core Dependencies
- **.NET 8.0**: Target framework
- **MediatR 12.4.0**: CQRS implementation
- **Microsoft.EntityFrameworkCore 8.0.7**: Data access
- **FluentValidation 11.9.0**: Input validation
- **Newtonsoft.Json 13.0.3**: JSON serialization

### HTTP API Dependencies
- **Microsoft.ApplicationInsights.AspNetCore 2.22.0**: Telemetry
- **Microsoft.Identity.Web 3.0.1**: Authentication
- **Swashbuckle.AspNetCore 6.6.2**: API documentation
- **Hellang.Middleware.ProblemDetails 6.5.1**: Error handling
- **Asp.Versioning.Mvc.ApiExplorer 8.1.0**: API versioning

### Logging Dependencies
- **Serilog.AspNetCore 8.0.2**: Structured logging
- **Serilog.Sinks.ApplicationInsights 4.0.0**: Application Insights integration
- **Serilog.Sinks.Console 6.0.0**: Console logging
- **Serilog.Sinks.Seq 8.0.0**: Seq logging

### Caching Dependencies
- **Microsoft.Extensions.Caching.StackExchangeRedis 8.0.7**: Redis caching

### Configuration Dependencies
- **Microsoft.Azure.AppConfiguration.AspNetCore 7.1.0**: Azure App Configuration

### Platform Dependencies
- **Platform.Common.ApiClient 1.0.2024628.1**: Common API client
- **Platform.Common.Instrumentation.ApplicationInsights 3.20250227.953**: Instrumentation
- **Platform.Common.ServiceBus 4.20250320.1524**: Service bus integration
- **Platform.Products.Product.ApiClient 1.0.20250103.1**: Product API client

---

*This documentation covers the Platform.Shared library as of version 1.0. For the latest updates and additional information, please refer to the source code and inline documentation.*