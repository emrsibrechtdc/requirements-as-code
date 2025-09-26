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
using MediatR;
using Platform.Shared.Exceptions;

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

// Configure minimal API endpoints
app.MapDeviceEndpoints();

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

## Step 6: Create Minimal API Endpoints

**Note:** This guide uses .NET 8's minimal API approach instead of traditional controllers for several benefits:
- **Reduced boilerplate code** - Less ceremony and more focused on the functionality
- **Better performance** - Direct route-to-delegate mapping without controller instantiation overhead
- **Cleaner organization** - Endpoints can be grouped by feature using extension methods
- **OpenAPI integration** - Built-in support for API documentation with `WithSummary()` and `WithDescription()`
- **Type safety** - Compile-time checking of parameters and return types with `TypedResults`

### 6.1 Device Endpoints Extension Method

```csharp
using MediatR;
using Platform.Shared.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;

public static class DeviceEndpoints
{
    public static void MapDeviceEndpoints(this WebApplication app)
    {
        var apiGroup = app.MapGroup("/api/v{version:apiVersion}/devices")
            .WithTags("Devices")
            .HasApiVersion(new ApiVersion(1, 0))
            .RequireAuthorization();

        // POST /api/v1/devices - Create a new device
        apiGroup.MapPost("/", CreateDevice)
            .WithSummary("Create a new device")
            .WithDescription("Creates a new device with the specified properties. Product context is automatically determined from the caller's identity.")
            .Produces<Guid>(201)
            .ProducesValidationProblem()
            .ProducesProblem(400);

        // GET /api/v1/devices/{id} - Get device by ID
        apiGroup.MapGet("/{id:guid}", GetDevice)
            .WithSummary("Get device by ID")
            .WithDescription("Retrieves a device by ID. Only returns devices accessible to the caller's product context.")
            .Produces<Device>(200)
            .ProducesProblem(404);

        // GET /api/v1/devices - Get all devices
        apiGroup.MapGet("/", GetDevices)
            .WithSummary("Get all devices")
            .WithDescription("Retrieves all devices accessible to the caller's product context.")
            .Produces<IEnumerable<Device>>(200);

        // PUT /api/v1/devices/{id}/location - Update device location
        apiGroup.MapPut("/{id:guid}/location", UpdateDeviceLocation)
            .WithSummary("Update device location")
            .WithDescription("Updates the location of a specific device.")
            .Produces<Device>(200)
            .ProducesValidationProblem()
            .ProducesProblem(404)
            .ProducesProblem(400);
    }

    private static async Task<IResult> CreateDevice(
        CreateDeviceCommand command,
        ISender sender,
        CancellationToken cancellationToken)
    {
        try
        {
            // Product context is automatically determined from the caller's identity
            // No need to pass product in the request - it's extracted from authentication context
            var deviceId = await sender.Send(command, cancellationToken);
            return TypedResults.Created($"/api/v1/devices/{deviceId}", deviceId);
        }
        catch (BusinessException ex)
        {
            return TypedResults.BadRequest(new { error = ex.Code, message = ex.Message });
        }
    }

    private static async Task<IResult> GetDevice(
        Guid id,
        ISender sender,
        CancellationToken cancellationToken)
    {
        try
        {
            var device = await sender.Send(new GetDeviceQuery { DeviceId = id }, cancellationToken);
            
            if (device == null)
                return TypedResults.NotFound(); // Could be not found OR not accessible due to product segregation
                
            return TypedResults.Ok(device);
        }
        catch (BusinessException ex)
        {
            return TypedResults.BadRequest(new { error = ex.Code, message = ex.Message });
        }
    }

    private static async Task<IResult> GetDevices(
        ISender sender,
        CancellationToken cancellationToken)
    {
        try
        {
            // This will automatically only return devices for the caller's product
            var devices = await sender.Send(new GetDevicesQuery(), cancellationToken);
            return TypedResults.Ok(devices);
        }
        catch (BusinessException ex)
        {
            return TypedResults.BadRequest(new { error = ex.Code, message = ex.Message });
        }
    }

    private static async Task<IResult> UpdateDeviceLocation(
        Guid id,
        UpdateDeviceLocationRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        try
        {
            var command = new UpdateDeviceLocationCommand
            {
                DeviceId = id,
                NewLocation = request.Location
            };
            
            var updatedDevice = await sender.Send(command, cancellationToken);
            return TypedResults.Ok(updatedDevice);
        }
        catch (BusinessException ex)
        {
            return TypedResults.BadRequest(new { error = ex.Code, message = ex.Message });
        }
    }
}

// Request DTOs for endpoints
public record UpdateDeviceLocationRequest(string Location);
```

### 6.2 Additional Command for Location Updates

```csharp
using Platform.Shared.Cqrs.Mediatr;
using Platform.Shared.DataLayer.Repositories;
using Platform.Shared.Exceptions;

public class UpdateDeviceLocationCommand : ICommand<Device>
{
    public Guid DeviceId { get; set; }
    public string NewLocation { get; set; } = string.Empty;
}

public class UpdateDeviceLocationCommandHandler : ICommandHandler<UpdateDeviceLocationCommand, Device>
{
    private readonly IRepository<Device, Guid> _deviceRepository;

    public UpdateDeviceLocationCommandHandler(IRepository<Device, Guid> deviceRepository)
    {
        _deviceRepository = deviceRepository;
    }

    public async Task<Device> Handle(UpdateDeviceLocationCommand request, CancellationToken cancellationToken)
    {
        var device = await _deviceRepository.GetByIdAsync(request.DeviceId, cancellationToken);
        
        if (device == null)
            throw new BusinessException("DEVICE_NOT_FOUND", "Device not found or not accessible");
        
        device.UpdateLocation(request.NewLocation);
        
        return await _deviceRepository.UpdateAsync(device, cancellationToken);
    }
}

// Missing GetDevicesQuery for completeness
public class GetDevicesQuery : IQuery<IEnumerable<Device>>
{
}

public class GetDevicesQueryHandler : IQueryHandler<GetDevicesQuery, IEnumerable<Device>>
{
    private readonly IReadRepository<Device, Guid> _deviceRepository;

    public GetDevicesQueryHandler(IReadRepository<Device, Guid> deviceRepository)
    {
        _deviceRepository = deviceRepository;
    }

    public async Task<IEnumerable<Device>> Handle(GetDevicesQuery request, CancellationToken cancellationToken)
    {
        // Product context is automatically applied via data filters in the repository
        return await _deviceRepository.GetAllAsync(cancellationToken);
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

### 10.1 Create Integration Tests for Minimal APIs

```csharp
using Microsoft.AspNetCore.Mvc.Testing;
using System.Text.Json;
using System.Net.Http.Json;
using Xunit;

public class DeviceApiIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _jsonOptions;

    public DeviceApiIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
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

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/devices", command, _jsonOptions);

        // Assert
        response.EnsureSuccessStatusCode();
        Assert.Equal(System.Net.HttpStatusCode.Created, response.StatusCode);
        
        var deviceId = await response.Content.ReadFromJsonAsync<Guid>(_jsonOptions);
        Assert.NotEqual(Guid.Empty, deviceId);
        
        // Verify Location header
        Assert.NotNull(response.Headers.Location);
        Assert.Contains($"devices/{deviceId}", response.Headers.Location.ToString());
    }

    [Fact]
    public async Task GetDevice_ExistingDevice_ReturnsDevice()
    {
        // Arrange - First create a device
        var createCommand = new CreateDeviceCommand
        {
            SerialNumber = "DEV002",
            Model = "TestModel2",
            Location = "TestLocation2"
        };
        
        var createResponse = await _client.PostAsJsonAsync("/api/v1/devices", createCommand, _jsonOptions);
        createResponse.EnsureSuccessStatusCode();
        var deviceId = await createResponse.Content.ReadFromJsonAsync<Guid>(_jsonOptions);

        // Act
        var response = await _client.GetAsync($"/api/v1/devices/{deviceId}");

        // Assert
        response.EnsureSuccessStatusCode();
        var device = await response.Content.ReadFromJsonAsync<Device>(_jsonOptions);
        Assert.NotNull(device);
        Assert.Equal(deviceId, device.Id);
        Assert.Equal("DEV002", device.SerialNumber);
        Assert.Equal("TestModel2", device.Model);
        Assert.Equal("TestLocation2", device.Location);
    }

    [Fact]
    public async Task GetDevice_NonExistentDevice_ReturnsNotFound()
    {
        // Act
        var response = await _client.GetAsync($"/api/v1/devices/{Guid.NewGuid()}");

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetAllDevices_ReturnsDevicesList()
    {
        // Arrange - Create a couple of devices first
        await _client.PostAsJsonAsync("/api/v1/devices", new CreateDeviceCommand
        {
            SerialNumber = "DEV003",
            Model = "TestModel3",
            Location = "TestLocation3"
        }, _jsonOptions);
        
        await _client.PostAsJsonAsync("/api/v1/devices", new CreateDeviceCommand
        {
            SerialNumber = "DEV004",
            Model = "TestModel4",
            Location = "TestLocation4"
        }, _jsonOptions);

        // Act
        var response = await _client.GetAsync("/api/v1/devices");

        // Assert
        response.EnsureSuccessStatusCode();
        var devices = await response.Content.ReadFromJsonAsync<IEnumerable<Device>>(_jsonOptions);
        Assert.NotNull(devices);
        Assert.True(devices.Count() >= 2); // At least the 2 we created
    }

    [Fact]
    public async Task UpdateDeviceLocation_ExistingDevice_ReturnsUpdatedDevice()
    {
        // Arrange - First create a device
        var createCommand = new CreateDeviceCommand
        {
            SerialNumber = "DEV005",
            Model = "TestModel5",
            Location = "OriginalLocation"
        };
        
        var createResponse = await _client.PostAsJsonAsync("/api/v1/devices", createCommand, _jsonOptions);
        createResponse.EnsureSuccessStatusCode();
        var deviceId = await createResponse.Content.ReadFromJsonAsync<Guid>(_jsonOptions);

        // Act
        var updateRequest = new UpdateDeviceLocationRequest("UpdatedLocation");
        var response = await _client.PutAsJsonAsync($"/api/v1/devices/{deviceId}/location", updateRequest, _jsonOptions);

        // Assert
        response.EnsureSuccessStatusCode();
        var updatedDevice = await response.Content.ReadFromJsonAsync<Device>(_jsonOptions);
        Assert.NotNull(updatedDevice);
        Assert.Equal("UpdatedLocation", updatedDevice.Location);
    }

    [Fact]
    public async Task UpdateDeviceLocation_NonExistentDevice_ReturnsBadRequest()
    {
        // Act
        var updateRequest = new UpdateDeviceLocationRequest("SomeLocation");
        var response = await _client.PutAsJsonAsync($"/api/v1/devices/{Guid.NewGuid()}/location", updateRequest, _jsonOptions);

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
        
        var errorResponse = await response.Content.ReadFromJsonAsync<object>(_jsonOptions);
        Assert.NotNull(errorResponse);
    }
}
```

## Common Scenarios

### Scenario 1: Adding Caching with Multi-Product Context (Minimal API)

**Enhanced Query Handler with Caching:**
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

**Minimal API Endpoint with Cache Headers:**
```csharp
public static class CachedDeviceEndpoints
{
    public static void MapCachedDeviceEndpoints(this WebApplication app)
    {
        var apiGroup = app.MapGroup("/api/v{version:apiVersion}/cached-devices")
            .WithTags("Cached Devices")
            .HasApiVersion(new ApiVersion(1, 0))
            .RequireAuthorization();

        // GET /api/v1/cached-devices/{id} - Get device with caching
        apiGroup.MapGet("/{id:guid}", GetCachedDevice)
            .WithSummary("Get device with caching")
            .WithDescription("Retrieves a device by ID with 5-minute cache. Includes cache headers for client-side caching.")
            .Produces<Device>(200)
            .ProducesProblem(404)
            .ProducesProblem(304); // Not Modified
    }

    private static async Task<IResult> GetCachedDevice(
        Guid id,
        ISender sender,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        try
        {
            // Check if-none-match header for client-side caching
            var ifNoneMatch = context.Request.Headers.IfNoneMatch.FirstOrDefault();
            
            var device = await sender.Send(new GetDeviceQuery { DeviceId = id }, cancellationToken);
            
            if (device == null)
                return TypedResults.NotFound();
                
            // Generate ETag based on device data and last updated time
            var etag = $"\"{device.Id}_{device.UpdatedAt:yyyyMMddHHmmss}\"";
            
            // Return 304 if client has current version
            if (ifNoneMatch == etag)
                return TypedResults.StatusCode(304); // Not Modified
            
            // Add caching headers to response
            return TypedResults.Ok(device)
                .WithHeaders(new Dictionary<string, string>
                {
                    { "ETag", etag },
                    { "Cache-Control", "private, max-age=300" }, // 5 minutes
                    { "Last-Modified", device.UpdatedAt.ToString("r") }
                });
        }
        catch (BusinessException ex)
        {
            return TypedResults.BadRequest(new { error = ex.Code, message = ex.Message });
        }
    }
}

// Extension method to add headers to TypedResults (helper)
public static class TypedResultsExtensions
{
    public static IResult WithHeaders(this IResult result, Dictionary<string, string> headers)
    {
        return new HeadersResult(result, headers);
    }
}

public class HeadersResult : IResult
{
    private readonly IResult _innerResult;
    private readonly Dictionary<string, string> _headers;

    public HeadersResult(IResult innerResult, Dictionary<string, string> headers)
    {
        _innerResult = innerResult;
        _headers = headers;
    }

    public async Task ExecuteAsync(HttpContext httpContext)
    {
        foreach (var header in _headers)
        {
            httpContext.Response.Headers.Append(header.Key, header.Value);
        }
        
        await _innerResult.ExecuteAsync(httpContext);
    }
}
```

**Register the cached endpoints in Program.cs:**
```csharp
// Add after app.MapDeviceEndpoints();
app.MapCachedDeviceEndpoints();
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

## Summary

You're now ready to use Platform.Shared with minimal APIs in your application! This setup provides you with:

### ✅ **Platform.Shared Features Integrated:**
- **Auditing system** - Automatic tracking of entity changes with timestamps and user information
- **CQRS patterns** - Clean separation of commands and queries with MediatR
- **Integration events** - Event-driven architecture with CloudEvents and clean business payloads  
- **Multi-product support** - Automatic data segregation and product context handling
- **Transaction management** - Automatic database transactions with the TransactionBehavior pipeline
- **Repository patterns** - Abstract data access with automatic product filtering

### ✅ **Modern API Architecture:**
- **Minimal APIs** - High-performance, low-ceremony endpoint definitions
- **Type safety** - Compile-time checking with TypedResults and parameter binding
- **OpenAPI integration** - Built-in Swagger documentation with WithSummary() and WithDescription()
- **Grouped endpoints** - Clean organization using extension methods and MapGroup()
- **API versioning** - Built-in support for versioned endpoints
- **Comprehensive testing** - Full integration test coverage with WebApplicationFactory

### ✅ **Enterprise-Ready Foundation:**
- **Clean architecture** compliance with Platform.Shared patterns
- **Production-ready** error handling and validation
- **Performance optimized** with caching and minimal overhead
- **Multi-tenant** data isolation handled automatically
- **Event-driven** integration with other services

This foundation enables you to build scalable, maintainable enterprise applications that integrate seamlessly with the broader Copeland Platform ecosystem.
