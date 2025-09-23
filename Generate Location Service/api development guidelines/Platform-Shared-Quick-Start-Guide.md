# Platform.Shared Quick Start Guide

## Overview

This guide will help you quickly integrate the Platform.Shared library into your .NET 8.0 application. Follow these steps to set up the core features including auditing, CQRS, integration events, and multi-product support.

## Prerequisites

- .NET 8.0 SDK
- Visual Studio 2022 or Visual Studio Code
- SQL Server (or another EF Core supported database)
- Redis (optional, for distributed caching)

## Step 1: Install NuGet Package

Add the Platform.Shared library to your project:

```xml
<PackageReference Include="Platform.Shared" Version="1.1.20250915.1" />
```

## Step 2: Basic Setup

### 2.1 Configure Services in Program.cs

```csharp
using Platform.Shared.Auditing.Extensions;
using Platform.Shared.HttpApi.Extensions;
using Platform.Shared.IntegrationEvents.Extensions;
using Platform.Shared.MultiProduct.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add Platform.Shared services with v1.1.20250915.1 patterns
builder.Services.AddPlatformCommonHttpApi(
    builder.Configuration, 
    (IConfigurationBuilder)builder.Configuration, 
    builder.Environment, 
    "YourService") // Module name
    .WithAuditing()
    .WithMultiProduct();

// Integration events typically added in Application project extension
builder.Services.AddIntegrationEventsServices();

// Add MediatR with Platform.Shared behaviors
builder.Services.AddMediatR(cfg => {
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);
});
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(TransactionBehavior<,>));

// Add Entity Framework
builder.Services.AddDbContext<YourDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IInstrumentationHelper, InstrumentationHelper>();

var app = builder.Build();

// Configure middleware
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
```

### 2.2 Create Your DbContext

```csharp
using Platform.Shared.EntityFrameworkCore;
using Platform.Shared.EntityFrameworkCore.Extensions;
using Platform.Shared.Auditing;
using Platform.Shared.DataLayer;

public class YourDbContext : PlatformDbContext
{
    public YourDbContext(
        DbContextOptions<YourDbContext> options,
        ILogger<YourDbContext> logger,
        IAuditPropertySetter auditPropertySetter,
        IDataFilter dataFilter) 
        : base(options, logger, auditPropertySetter, dataFilter)
    {
    }

    public DbSet<Device> Devices { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Configure platform entities (auditing, multi-product, soft delete)
        modelBuilder.ConfigurePlatformEntities();
        
        // Your entity configurations
        modelBuilder.Entity<Device>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.SerialNumber).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Model).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Location).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Product).IsRequired().HasMaxLength(100); // Multi-product context
            entity.Property(e => e.Status).HasConversion<string>();
        });
    }
}
```

## Step 3: Create Your Domain Entities

### 3.1 Basic Audited Multi-Product Entity

```csharp
using Platform.Shared.Auditing;
using Platform.Shared.MultiProduct;
using Platform.Shared.Exceptions;

public class Device : FullyAuditedAggregateRoot<Guid>, IMultiProductObject
{
    public string SerialNumber { get; private set; }
    public string Model { get; private set; }
    public string Location { get; private set; }
    public DeviceStatus Status { get; private set; }
    public string Product { get; set; } // Multi-product property - set by framework based on caller

    // Parameterless constructor for EF Core
    private Device() { }

    public Device(string serialNumber, string model, string location)
    {
        Id = Guid.NewGuid();
        SerialNumber = serialNumber;
        Model = model;
        Location = location;
        Status = DeviceStatus.Active;
        // Product will be set by the framework based on the caller's identity
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

## Step 4: Implement CQRS Commands and Queries

### 4.1 Create Device Command

```csharp
using MediatR;
using Platform.Shared.Cqrs.Mediatr;
using Platform.Shared.DataLayer.Repositories;
using Platform.Shared.MultiProduct;
using Platform.Shared.IntegrationEvents;

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

### 4.2 Get Device Query

```csharp
using MediatR;
using Platform.Shared.Cqrs.Mediatr;
using Platform.Shared.Exceptions;

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

## Step 5: Create Integration Events

### 5.1 Define Integration Event

```csharp
using Platform.Shared.IntegrationEvents;

public class DeviceCreatedIntegrationEvent : IIntegrationEvent
{
    public Guid DeviceId { get; }
    public string SerialNumber { get; }
    public string Model { get; }
    public string Location { get; }
    public DateTime CreatedAt { get; }

    public DeviceCreatedIntegrationEvent(Guid deviceId, string serialNumber, string model, string location)
    {
        DeviceId = deviceId;
        SerialNumber = serialNumber;
        Model = model;
        Location = location;
        CreatedAt = DateTime.UtcNow;
    }
}
```

### 5.2 CloudEvents and Product Context

The `CreateDeviceCommandHandler` shown above already includes integration event publishing. **Key architectural points about integration events:**

```csharp
// In the command handler - clean business event without product context:
var integrationEvent = new DeviceCreatedIntegrationEvent(
    device.Id,
    device.SerialNumber,
    device.Model,
    device.Location); // Only business data, no product context

_eventPublisher.AddIntegrationEvent(integrationEvent);
```

**Important:** Integration events are published as **CloudEvents** with:
1. **Clean Business Payload**: Only business-relevant data in the event body
2. **Product in Headers**: Product context is automatically added to CloudEvents headers by the framework
3. **Infrastructure-Level Routing**: Event routing and filtering happens at the infrastructure level using headers
4. **Event Schema Simplicity**: Events remain focused on business semantics, not routing concerns

## Step 6: Create API Controller

```csharp
using Microsoft.AspNetCore.Mvc;
using MediatR;
using Platform.Shared.Exceptions;

[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
public class DevicesController : ControllerBase
{
    private readonly IMediator _mediator;

    public DevicesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<ActionResult<Guid>> CreateDevice([FromBody] CreateDeviceCommand command)
    {
        try
        {
            // Product context is automatically determined from the caller's identity
            // No need to pass product in the request - it's extracted from authentication context
            var deviceId = await _mediator.Send(command);
            return CreatedAtAction(nameof(GetDevice), new { id = deviceId }, deviceId);
        }
        catch (BusinessException ex)
        {
            return BadRequest(new { error = ex.Code, message = ex.Message });
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Device>> GetDevice(Guid id)
    {
        var device = await _mediator.Send(new GetDeviceQuery { DeviceId = id });
        
        if (device == null)
            return NotFound(); // Could be not found OR not accessible due to product segregation
            
        return Ok(device);
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Device>>> GetDevices()
    {
        // This will automatically only return devices for the caller's product
        var devices = await _mediator.Send(new GetDevicesQuery());
        return Ok(devices);
    }
}
```

## Step 7: Repository Implementation

```csharp
using Platform.Shared.DataLayer.Repositories;
using Platform.Shared.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

public class EfDeviceRepository : EfCoreRepository<Device, Guid>, IRepository<Device, Guid>
{
    public EfDeviceRepository(YourDbContext context) : base(context)
    {
    }

    public async Task<Device?> GetBySerialNumberAsync(string serialNumber, CancellationToken cancellationToken = default)
    {
        // Multi-product filtering is automatically applied by the base repository
        return await _context.Devices
            .FirstOrDefaultAsync(d => d.SerialNumber == serialNumber, cancellationToken);
    }

    public async Task<IEnumerable<Device>> GetByLocationAsync(string location, CancellationToken cancellationToken = default)
    {
        // Multi-product filtering is automatically applied by the base repository
        return await _context.Devices
            .Where(d => d.Location == location)
            .ToListAsync(cancellationToken);
    }
}
```

Register the repository in Program.cs:

```csharp
builder.Services.AddScoped<IRepository<Device, Guid>, EfDeviceRepository>();
builder.Services.AddScoped<IReadRepository<Device, Guid>, EfDeviceRepository>();
```

## Step 8: Add Database Migrations

```bash
# Add initial migration
dotnet ef migrations add InitialCreate

# Update database
dotnet ef database update
```

## Step 9: Configuration Settings

Add to `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=YourServiceDb;Trusted_Connection=true;MultipleActiveResultSets=true"
  },
  "CorsOrigins": "http://localhost:3000,http://localhost:5173",
  "Version": "1.0.0",
  "AppConfigurationUrl": "https://your-app-config.azconfig.io" // For production
}
```

For production, add:

```json
{
  "ApplicationInsights": {
    "InstrumentationKey": "your-instrumentation-key"
  }
}
```

## Step 10: Testing Your Setup

### 10.1 Create a Simple Test

```csharp
using Microsoft.AspNetCore.Mvc.Testing;
using System.Text.Json;

public class ProductIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public ProductIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task CreateDevice_ReturnsDeviceId()
    {
        // Arrange
        var command = new CreateDeviceCommand
        {
            SerialNumber = "DEV001",
            Model = "TestModel",
            Location = "TestLocation"
        };

        var json = JsonSerializer.Serialize(command);
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/v1/devices", content);

        // Assert
        response.EnsureSuccessStatusCode();
        var deviceId = await response.Content.ReadAsStringAsync();
        Assert.True(Guid.TryParse(deviceId.Trim('"'), out _));
    }
}
```

## Common Scenarios

### Scenario 1: Adding Caching with Multi-Product Context

```csharp
public class GetDeviceQueryHandler : IQueryHandler<GetDeviceQuery, Device?>
{
    private readonly IReadRepository<Device, Guid> _deviceRepository;
    private readonly InMemoryCacheRequestHandler _cacheHandler;
    private readonly IMultiProductRequestContextProvider _contextProvider;

    public GetDeviceQueryHandler(
        IReadRepository<Device, Guid> deviceRepository,
        InMemoryCacheRequestHandler cacheHandler,
        IMultiProductRequestContextProvider contextProvider)
    {
        _deviceRepository = deviceRepository;
        _cacheHandler = cacheHandler;
        _contextProvider = contextProvider;
    }

    public async Task<Device?> Handle(GetDeviceQuery request, CancellationToken cancellationToken)
    {
        var currentProduct = await _contextProvider.GetProductAsync();
        var cacheKey = $"device_{currentProduct}_{request.DeviceId}"; // Include product in cache key
        
        return await _cacheHandler.GetOrSetAsync(cacheKey, async () =>
        {
            // Repository automatically filters by product context
            return await _deviceRepository.GetByIdAsync(request.DeviceId, cancellationToken);
        }, TimeSpan.FromMinutes(5));
    }
}
```

### Scenario 2: Custom Business Exception

```csharp
public class DeviceNotFoundException : BusinessException
{
    public DeviceNotFoundException(Guid deviceId) 
        : base("DEVICE_NOT_FOUND", $"Device with ID {deviceId} was not found or not accessible")
    {
    }
}

public class InvalidDeviceStateException : BusinessException
{
    public InvalidDeviceStateException(string serialNumber, DeviceStatus currentStatus, DeviceStatus requestedStatus)
        : base("INVALID_DEVICE_STATE", 
               $"Cannot change device {serialNumber} from {currentStatus} to {requestedStatus}")
    {
    }
}
```

### Scenario 3: Integration Event Handler with Product Context

```csharp
public class DeviceCreatedEventHandler : IIntegrationEventHandler<DeviceCreatedIntegrationEvent>
{
    private readonly ILogger<DeviceCreatedEventHandler> _logger;
    private readonly IDeviceProvisioningService _provisioningService;
    private readonly IRepository<DeviceInventory, Guid> _inventoryRepository;
    private readonly INotificationService _notificationService;

    public DeviceCreatedEventHandler(
        ILogger<DeviceCreatedEventHandler> logger,
        IDeviceProvisioningService provisioningService,
        IRepository<DeviceInventory, Guid> inventoryRepository,
        INotificationService notificationService)
    {
        _logger = logger;
        _provisioningService = provisioningService;
        _inventoryRepository = inventoryRepository;
        _notificationService = notificationService;
    }

    public async Task Handle(DeviceCreatedIntegrationEvent @event)
    {
        // Focus on business logic only - no manual product context handling
        _logger.LogInformation(
            "Processing device creation: {SerialNumber} ({Model})",
            @event.SerialNumber,
            @event.Model);

        // Business logic: Provision the device
        await _provisioningService.ProvisionDeviceAsync(
            @event.DeviceId,
            @event.SerialNumber,
            @event.Model);

        // Update inventory - Platform.Shared handles product segregation automatically
        var inventoryRecord = DeviceInventory.Create(
            @event.DeviceId,
            @event.SerialNumber,
            @event.Model,
            InventoryStatus.Active);
            
        await _inventoryRepository.AddAsync(inventoryRecord);
        
        // Send notifications
        await _notificationService.NotifyDeviceProvisionedAsync(@event.DeviceId);
        
        // Platform.Shared infrastructure automatically handles:
        // - Repository operations are filtered/associated by product
        // - Services receive appropriate product context through their own infrastructure
        // - No manual product-specific branching or filtering needed in handlers
    }
}
```

Register the handler:

```csharp
builder.Services.AddScoped<IIntegrationEventHandler<DeviceCreatedIntegrationEvent>, DeviceCreatedEventHandler>();
```

## Understanding Multi-Product Context Flow

### How Product Context is Determined

The Platform.Shared library implements multi-tenant data segregation through the multi-product context system. Here's how it works:

1. **Caller Identification**: When a request comes in, **ASP.NET Core middleware in each service** (not part of Platform.Shared) identifies the caller through:
   - JWT token claims
   - API key lookup
   - Client certificate information
   - Custom authentication headers

2. **Product Mapping**: The **service-specific middleware** maps the caller identity to one of the 3 products through business logic:
   ```csharp
   // Example in service-specific ASP.NET Core middleware (NOT in Platform.Shared)
   var product = callerInfo switch
   {
       var caller when caller.StartsWith("ProductA_") => "ProductA",
       var caller when caller.StartsWith("ProductB_") => "ProductB", 
       var caller when caller.StartsWith("ProductC_") => "ProductC",
       _ => throw new UnauthorizedAccessException("Invalid caller")
   };
   ```

3. **Context Setting**: The **service middleware** sets the product in the `IMultiProductRequestContextProvider` early in the request pipeline

4. **Automatic Platform.Shared Operations**: Once the context is set, all Platform.Shared operations automatically use it:
   - Repository operations filter data by current product context
   - Entity creation automatically associates entities with the caller's product
   - Integration events include product context

### Key Architecture Points

- **Middleware is Service-Specific**: Each service implements its own ASP.NET Core middleware to set product context
- **Platform.Shared Consumes Context**: Platform.Shared components automatically use the context once it's set
- **No Manual Product Handling**: Command/query handlers never manually set or pass product information
- **Automatic Segregation**: Data filtering and entity association happen automatically
- **Context Propagation**: Product context flows through the entire request pipeline automatically

### Example Request Flow

```
1. HTTP Request with Authentication → 
2. Service-Specific Authentication Middleware extracts caller identity → 
3. Service-Specific Product Context Middleware maps caller to product → 
4. Service-Specific Middleware sets product in IMultiProductRequestContextProvider → 
5. Controller → Command/Query Handler → Platform.Shared Repository (auto-filtered by product) → 
6. Platform.Shared automatically associates entities with product context → 
7. Platform.Shared Integration Events automatically include product context
```

**Important**: The middleware that determines and sets product context is implemented in each individual service's ASP.NET Core layer, not in Platform.Shared. Platform.Shared only provides the infrastructure to consume and use the product context once it's been set.

## Troubleshooting

### Common Issues

1. **TransactionBehavior not working**: Ensure `IInstrumentationHelper` is registered as scoped
2. **Audit properties not set**: Verify `IAuditPropertySetter` is registered and `PlatformDbContext` is used
3. **Integration events not published**: Check that `IIntegrationEventPublisher` is registered and events are added before transaction commits
4. **Multi-product isolation not working**: Ensure data filters are properly configured in `OnModelCreating`

### Debug Tips

1. Enable SQL logging in development:
```csharp
builder.Services.AddDbContext<YourDbContext>(options =>
    options.UseSqlServer(connectionString)
           .EnableSensitiveDataLogging()
           .LogTo(Console.WriteLine));
```

2. Add request logging:
```csharp
app.Use(async (context, next) =>
{
    Console.WriteLine($"Request: {context.Request.Method} {context.Request.Path}");
    await next();
});
```

---

You're now ready to use Platform.Shared in your application! This setup provides you with auditing, CQRS patterns, integration events, multi-product support, and a solid foundation for building enterprise applications.