# Copeland Platform API Development Guidelines

> **Version:** 2.1  
> **Last Updated:** 2025-09-23  
> **Based on:** Clean Architecture principles, Copeland Platform standards, and Platform.Shared library v1.1.20250915.1

This document outlines the comprehensive development standards and best practices for building RESTful APIs within the Copeland Platform ecosystem using the **Platform.Shared library**. These guidelines ensure consistency, maintainability, and scalability across all platform services while leveraging Platform.Shared's infrastructure capabilities for auditing, multi-product support, CQRS, and integration events.

**🆕 Platform.Shared Integration**: This document has been updated to reflect how the Platform.Shared library provides infrastructure components for auditing, multi-product data segregation, CQRS patterns, CloudEvents integration, and automatic transaction management.

## Table of Contents

1. [Solution Architecture](#solution-architecture)
2. [Project Structure & Organization](#project-structure--organization)
3. [API Design Patterns](#api-design-patterns)
4. [Minimal API Implementation](#minimal-api-implementation)
5. [Command Query Responsibility Segregation (CQRS)](#command-query-responsibility-segregation-cqrs)
6. [Data Transfer Objects (DTOs)](#data-transfer-objects-dtos)
7. [Domain-Driven Design (DDD)](#domain-driven-design-ddd)
8. [Validation Standards](#validation-standards)
9. [Error Handling & Exception Management](#error-handling--exception-management)
10. [Authentication & Authorization](#authentication--authorization)
11. [Middleware & Request Pipeline](#middleware--request-pipeline)
12. [Integration Events](#integration-events)
13. [Testing Standards](#testing-standards)
14. [API Versioning](#api-versioning)
15. [OpenAPI Documentation](#openapi-documentation)
16. [Logging & Monitoring](#logging--monitoring)
17. [Code Quality & Style Guidelines](#code-quality--style-guidelines)
18. [NuGet Package Configuration](#nuget-package-configuration)
19. [Source Control Management](#source-control-management)

---

## 1. Solution Architecture

### Clean Architecture Layers

The platform follows a clean architecture approach with the following layers:

```plaintext
├── src/
│   ├── Platform.{ServiceName}.Host/                    # Web API Host
│   ├── Core/
│   │   └── Platform.{ServiceName}.Domain/              # Domain Layer
│   └── {ServiceName}/
│       ├── Platform.{ServiceName}.HttpApi/             # HTTP API Layer
│       ├── Platform.{ServiceName}.Application/         # Application Layer
│       ├── Platform.{ServiceName}.Infrastructure/      # Infrastructure Layer
│       ├── Platform.{ServiceName}.{DataStore}/         # Database Context
│       └── Platform.{ServiceName}.ApiClient/           # API Client
└── test/
    ├── Core/
    │   └── Platform.{ServiceName}.Domain.UnitTests/
    ├── {ServiceName}/
    │   ├── Platform.{ServiceName}.Application.UnitTests/
    │   └── Platform.{ServiceName}.Infrastructure.IntegrationTests/
    └── Platform.{ServiceName}.ApiClient.IntegrationTests/
```

### Dependency Flow

- **Domain Layer**: Contains business logic, entities, and domain services. No dependencies on other layers.
- **Application Layer**: Contains use cases, commands, queries, and application services. Depends only on Domain.
- **Infrastructure Layer**: Contains data access, external service integrations. Depends on Application and Domain.
- **HttpApi Layer**: Contains API endpoints and middleware. Depends on Application.
- **Host Layer**: Entry point and configuration. Orchestrates all layers.

---

## 2. Project Structure & Organization

### Domain Project Structure

```plaintext
Platform.{ServiceName}.Domain/
├── {EntityName}s/
│   ├── I{EntityName}Repository.cs
│   ├── {EntityName}.cs
│   ├── {EntityName}NotFoundException.cs
│   └── {EntityName}AlreadyExistsException.cs
└── {ServiceName}Constants.cs
```

### Application Project Structure

```plaintext
Platform.{ServiceName}.Application/
├── {EntityName}s/
│   ├── Commands/
│   │   ├── {Action}{EntityName}Command.cs
│   │   └── {Action}{EntityName}CommandHandler.cs
│   ├── Queries/
│   │   ├── Get{EntityName}sQuery.cs
│   │   └── Get{EntityName}sQueryHandler.cs
│   ├── Dtos/
│   │   ├── {EntityName}Dto.cs
│   │   └── {EntityName}Response.cs
│   └── Validators/
│       └── {Action}{EntityName}CommandValidator.cs
├── IntegrationEvents/
├── Profiles/
│   └── AutoMapProfile.cs
└── Extensions/
    └── ApplicationServiceCollectionExtensions.cs
```

### HttpApi Project Structure

```plaintext
Platform.{ServiceName}.HttpApi/
├── Extensions/
│   ├── EndpointRouteBuilderExtensions.cs
│   └── {ServiceName}HttpApiServiceCollectionExtensions.cs
└── RequestMiddleware.cs
```

---

## 3. API Design Patterns

### RESTful Design Principles

1. **Resource-Based URLs**: Use nouns, not verbs
   - ✅ `GET /orders`
   - ✅ `POST /orders/submit`
   - ❌ `GET /getOrders`

2. **HTTP Method Usage**:
   - `GET`: Retrieve resources
   - `POST`: Create new resources or execute actions
   - `PUT`: Update existing resources or execute state changes
   - `DELETE`: Remove resources

3. **Semantic URL Structure**:
   ```csharp
   POST /orders/create                         // Create
   GET  /orders                               // Read (all)
   GET  /orders?status=pending                // Read (filtered)
   PUT  /orders/{orderId}/update              // Update
   PUT  /orders/{orderId}/approve             // Action
   PUT  /orders/{orderId}/cancel              // Action
   DELETE /orders/{orderId}                   // Delete
   ```

### Action-Based Endpoints

For business operations that don't fit standard CRUD patterns:

```csharp
PUT /{entity}/{id}/activate
PUT /{entity}/{id}/deactivate
POST /{entity}/create
PUT /{entity}/{id}/approve
POST /{entity}/{id}/submit
```

---

## 4. Minimal API Implementation

### Endpoint Registration Pattern

```csharp
public static IEndpointRouteBuilder Map{ServiceName}ApiRoutes(this IEndpointRouteBuilder endpoints, ApiVersionSet apiVersionSet, bool authorizationRequired)
{
    var createEntity = endpoints.MapPost("/{entity}/create", async (HttpContext context, CreateEntityCommand data, ISender sender, CancellationToken cancellationToken) =>
    {
        var result = await sender.Send(data, cancellationToken);
        return TypedResults.Ok(result);

    }).WithApiVersionSet(apiVersionSet)
          .MapToApiVersion(1.0)
          .WithSummary("Create {Entity} endpoint")
          .Produces<EntityResponse>(StatusCodes.Status200OK)
          .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError)
          .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
          .Produces<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)
          .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
          .Produces<ProblemDetails>(StatusCodes.Status403Forbidden);

    if (authorizationRequired)
    {
        createEntity.RequireAuthorization(builder =>
        {
            builder.RequireRole("{ServiceName}.Create");
        });
    }

    return endpoints;
}
```

### Standard Response Types

All endpoints must specify their possible response types:

```csharp
.Produces<TSuccessResponse>(StatusCodes.Status200OK)
.Produces<ProblemDetails>(StatusCodes.Status400BadRequest)     // Validation errors
.Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)   // Authentication required
.Produces<ProblemDetails>(StatusCodes.Status403Forbidden)      // Access denied
.Produces<ProblemDetails>(StatusCodes.Status422UnprocessableEntity) // Business logic errors
.Produces<ProblemDetails>(StatusCodes.Status500InternalServerError) // System errors
```

### Endpoint Requirements Checklist

- [ ] Uses `TypedResults` for responses
- [ ] Includes `CancellationToken` parameter
- [ ] Uses MediatR `ISender` for command/query dispatch
- [ ] Specifies API version with `MapToApiVersion()`
- [ ] Includes endpoint summary with `WithSummary()` and optional `WithDescription()`
- [ ] Declares all possible response types with `Produces<>()`
- [ ] Applies authorization rules when `authorizationRequired` is true

---

## 5. Command Query Responsibility Segregation (CQRS)

### Command Pattern with Platform.Shared

Commands represent write operations and should use **Platform.Shared CQRS interfaces** from the `Platform.Shared.Cqrs.Mediatr` namespace:

```csharp
// Use Platform.Shared CQRS interfaces from Platform.Shared.Cqrs.Mediatr
using Platform.Shared.Cqrs.Mediatr;

public record CreateEntityCommand(
    string EntityCode,
    string EntityType,
    string Name,
    string? Description,
    Dictionary<string, object>? Properties) : ICommand<EntityResponse>
{ }
```

**Platform.Shared CQRS Features:**
- **ICommand<TResponse>**: Command interface from Platform.Shared
- **ICommandHandler<TCommand, TResponse>**: Handler interface
- **TransactionBehavior**: Automatic database transaction management
- **Integration Events**: Automatic event publishing after transaction commits
- **Instrumentation**: Automatic telemetry and performance tracking

**Command Guidelines:**
- Use `record` types for immutability
- Inherit from Platform.Shared `ICommand<TResponse>`
- Include all necessary data for the operation
- Use descriptive names: `{Action}{Entity}Command`
- **Never include product context** - handled automatically by middleware

### Command Handler Pattern with Platform.Shared

```csharp
// Use Platform.Shared interfaces and infrastructure
using Platform.Shared.Cqrs.Mediatr;
using Platform.Shared.DataLayer.Repositories;

public class CreateEntityCommandHandler : ICommandHandler<CreateEntityCommand, EntityResponse>
{
    private readonly IRepository<Entity, Guid> _entityRepository; // Platform.Shared repository
    private readonly IValidator<CreateEntityCommand> _validator;
    private readonly IMapper _mapper;
    private readonly IIntegrationEventPublisher _eventPublisher; // Platform.Shared event publisher

    public async Task<EntityResponse> Handle(CreateEntityCommand request, CancellationToken cancellationToken)
    {
        // Input validation
        _validator.ValidateAndThrow(request);
        
        // Application-level business rules validation
        await ValidateBusinessRules(request, cancellationToken);
        
        // Create domain entity with business logic in aggregate root
        var entity = Entity.Create(request.EntityCode, request.EntityType, request.Name, request.Description);
        
        // Platform.Shared automatically handles:
        // - Setting audit fields (CreatedAt, CreatedBy)
        // - Setting product context from middleware
        // - Database transaction management (via TransactionBehavior)
        await _entityRepository.AddAsync(entity, cancellationToken);
        
        // Publish clean integration events (no product context in payload)
        // Product context automatically added to CloudEvents headers
        _eventPublisher.SaveIntegrationEvent(
            new EntityCreatedIntegrationEvent(entity.EntityCode, entity.Name));
        
        return _mapper.Map<EntityResponse>(entity);
    }

    private async Task ValidateBusinessRules(CreateEntityCommand request, CancellationToken cancellationToken)
    {
        // Repository automatically filters by product context - no manual filtering needed
        var existingEntity = await _entityRepository.GetByCodeAsync(request.EntityCode, cancellationToken);
        if (existingEntity != null)
        {
            throw new EntityAlreadyExistsException(request.EntityCode);
        }
        // ... other application-level validations
    }
}
```

**Platform.Shared Command Handler Benefits:**
- **Automatic Transaction Management**: `TransactionBehavior` handles database transactions
- **Multi-Product Data Segregation**: Repository automatically filters by product context
- **Automatic Auditing**: Entity creation/update timestamps and user tracking
- **Integration Events**: CloudEvents published with product context in headers
- **Instrumentation**: Automatic telemetry and performance tracking

**Command Handler Guidelines:**
- Always validate input with `_validator.ValidateAndThrow()`
- Perform application-level business rule validation (existence checks, uniqueness, etc.)
- Use Platform.Shared `IRepository<TEntity, TId>` for data access with automatic filtering
- Call static factory methods on aggregate roots for domain business logic
- Use Platform.Shared `IIntegrationEventPublisher` for event publishing
- **Never manually set product context** - handled automatically by middleware
- Use AutoMapper for object mapping

### Query Pattern with Platform.Shared

```csharp
// Use Platform.Shared query interfaces
using Platform.Shared.Cqrs.Mediatr;
using Platform.Shared.DataLayer.Repositories;

public record GetEntitiesQuery(string? EntityCode, string? EntityType) : IQuery<List<EntityDto>>
{ }

public class GetEntitiesQueryHandler : IQueryHandler<GetEntitiesQuery, List<EntityDto>>
{
    private readonly IReadRepository<Entity, Guid> _repository; // Platform.Shared read repository
    private readonly IMapper _mapper;

    public async Task<List<EntityDto>> Handle(GetEntitiesQuery request, CancellationToken cancellationToken)
    {
        // Repository automatically filters by product context - no manual filtering needed
        var entities = await _repository.GetAllAsync(cancellationToken);
        
        // Apply additional filters if specified
        if (!string.IsNullOrEmpty(request.EntityCode))
        {
            entities = entities.Where(e => e.EntityCode.Contains(request.EntityCode));
        }
        
        return _mapper.Map<List<EntityDto>>(entities);
    }
}
```

**Platform.Shared Query Benefits:**
- **IQuery<TResponse>**: Query interface from Platform.Shared
- **IQueryHandler<TQuery, TResponse>**: Handler interface
- **IReadRepository**: Read-only repository with automatic product filtering
- **No Transaction Overhead**: Queries bypass transaction behavior

**Query Guidelines:**
- Use `record` types
- Inherit from Platform.Shared `IQuery<TResponse>`
- Use Platform.Shared `IQueryHandler<TQuery, TResponse>`
- Use `IReadRepository<TEntity, TId>` for read operations
- Include filtering/pagination parameters
- Return DTOs, never domain entities
- **Product filtering is automatic** - no manual implementation needed

---

## 6. Data Transfer Objects (DTOs)

### DTO Design Principles

```csharp
public record EntityDto
(
    string EntityCode,
    string EntityType,
    string Name,
    string? Description,
    bool IsActive,
    DateTime CreatedAt
)
{ }
```

**DTO Standards:**
- Use `record` types for immutability
- Include only data needed by the consumer
- Use nullable types (`string?`) for optional fields
- Never expose internal domain concepts
- Use clear, descriptive property names

### Response DTOs

```csharp
public record EntityResponse
(
    string EntityCode,
    bool IsActive,
    DateTime CreatedAt,
    string CreatedBy
)
{ }
```

**Response DTO Guidelines:**
- Include operation result data
- Add metadata like timestamps and audit fields
- Use for both success responses and operation confirmations

---

## 7. Domain-Driven Design (DDD)

### Aggregate Root Design

```csharp
public class Entity : FullyAuditedActivableAggregateRoot<Guid>, IMultiProductObject, IInstrumentationObject
{
    public string Product { get; set; } = default!;
    public string EntityCode { get; set; } = default!;
    public string EntityType { get; init; } = default!;
    public string Name { get; private set; } = default!;
    public string? Description { get; private set; }

    private Entity() { }

    private Entity(string entityCode, string entityType, string name, string? description)
    {
        EntityCode = entityCode;
        EntityType = entityType;
        Name = name;
        Description = description;
    }

    // Static factory method with domain business logic
    public static Entity Create(string entityCode, string entityType, string name, string? description)
    {
        // Domain invariant validation - rules that must ALWAYS be true
        if (string.IsNullOrWhiteSpace(entityCode))
            throw new ArgumentException("Entity code cannot be empty", nameof(entityCode));
        
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty", nameof(name));

        // Additional domain rules that don't require external dependencies
        if (name.Length > 200)
            throw new ArgumentException("Name cannot exceed 200 characters", nameof(name));

        return new Entity(entityCode, entityType, name, description);
    }

    // Domain business logic methods
    public void UpdateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty", nameof(name));
        
        if (name.Length > 200)
            throw new ArgumentException("Name cannot exceed 200 characters", nameof(name));
        
        Name = name;
    }

    public new void Activate()
    {
        if (IsActive)
        {
            throw new EntityAlreadyActiveException(EntityCode);
        }
        base.Activate();
    }

    public new void Deactivate()
    {
        if (!IsActive)
        {
            throw new EntityAlreadyInactiveException(EntityCode);
        }
        base.Deactivate();
    }

    public Dictionary<string, string> ToInstrumentationProperties()
    {
        return new Dictionary<string, string>
        {
            { "entityCode", EntityCode },
            { "entityType", EntityType }
        };
    }
}
```

**Domain Entity Guidelines:**
- Inherit from appropriate base classes (`AggregateRoot`, `Entity`)
- Implement platform interfaces (`IMultiProductObject`, `IInstrumentationObject`)
- Use private setters for properties that should only be changed through methods
- Include **domain invariant validation** in static factory methods and instance methods
- Domain rules should NOT depend on external systems (repositories, APIs, etc.)
- Focus on business logic that ensures the entity remains in a valid state
- Use static factory methods like `Create()` for entity construction with validation
- Throw domain-specific exceptions for rule violations
- Keep all domain business logic within the aggregate root
- Use private constructors to enforce use of factory methods

### Value Objects

```csharp
public class ContactInfo : ValueObject
{
    public string Email { get; }
    public string? PhoneNumber { get; }
    public string? Address { get; }

    public ContactInfo(string email, string? phoneNumber = null, string? address = null)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email is required", nameof(email));
        
        // Domain validation - format checking, etc.
        if (!IsValidEmailFormat(email))
            throw new ArgumentException("Invalid email format", nameof(email));
        
        Email = email;
        PhoneNumber = phoneNumber;
        Address = address;
    }

    private static bool IsValidEmailFormat(string email)
    {
        // Basic email format validation
        return email.Contains("@") && email.Contains(".");
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Email;
        yield return PhoneNumber ?? string.Empty;
        yield return Address ?? string.Empty;
    }
}
```

### Platform.Shared Integration

The domain layer leverages **Platform.Shared** infrastructure components:

```csharp
// Inherit from Platform.Shared base classes for automatic infrastructure features
public class Entity : FullyAuditedAggregateRoot<Guid>, IMultiProductObject, IInstrumentationObject
{
    // Platform.Shared automatically handles:
    // - CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, DeletedAt, DeletedBy (auditing)
    // - Product property for multi-tenant data segregation
    // - Instrumentation properties for telemetry
    
    public string Product { get; set; } = default!; // Set by Platform.Shared middleware
    
    // Your domain logic here...
    public static Entity Create(string entityCode, string name)
    {
        // Domain invariant validation - handled by your domain logic
        // Platform.Shared will automatically set audit fields and product context
        return new Entity(entityCode, name);
    }
}
```

**Platform.Shared Features Automatically Provided:**
- **Auditing**: All entity changes tracked with timestamps and user information
- **Multi-Product Context**: Automatic data segregation based on caller identity  
- **Soft Deletion**: Entities marked as deleted instead of hard deleted
- **Instrumentation**: Telemetry and monitoring integration
- **Integration Events**: CloudEvents infrastructure for event publishing

### Domain vs Application Layer Responsibilities

#### Domain Layer Responsibilities:
- **Domain invariant validation**: Rules that must always be true for the entity to be valid
- **Business logic**: Core business rules that don't depend on external systems
- **Entity state management**: Ensuring entities remain in valid states
- **Value object validation**: Format validation, range validation, etc.
- **Platform.Shared Integration**: Implement required interfaces (`IMultiProductObject`, `IInstrumentationObject`)

#### Application Layer Responsibilities:
- **Entity existence validation**: Checking if entities already exist in the database
- **Uniqueness validation**: Ensuring unique constraints across the system
- **Cross-aggregate validation**: Rules involving multiple aggregates
- **External system validation**: Rules that require calls to external APIs or services
- **Use case orchestration**: Coordinating multiple domain operations
- **Platform.Shared Integration**: Use provided repositories, unit of work, and integration events

### Example of Proper Separation

```csharp
// Domain Layer - Entity with domain invariants only
public class Order : AggregateRoot<Guid>
{
    public static Order Create(string orderNumber, CustomerId customerId, List<OrderItem> items)
    {
        // Domain invariant - order must have items
        if (items == null || !items.Any())
            throw new ArgumentException("Order must have at least one item");
        
        // Domain invariant - order number format
        if (string.IsNullOrWhiteSpace(orderNumber) || orderNumber.Length < 5)
            throw new ArgumentException("Order number must be at least 5 characters");
        
        return new Order(orderNumber, customerId, items);
    }
}

// Application Layer - Command handler with application validations
public class CreateOrderCommandHandler : ICommandHandler<CreateOrderCommand, OrderResponse>
{
    public async Task<OrderResponse> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        _validator.ValidateAndThrow(request);
        
        // APPLICATION VALIDATION - Check if order number is unique
        var existingOrder = await _orderRepository.FindByOrderNumberAsync(request.OrderNumber, cancellationToken);
        if (existingOrder != null)
        {
            throw new OrderAlreadyExistsException(request.OrderNumber);
        }
        
        // APPLICATION VALIDATION - Check if customer exists
        var customer = await _customerRepository.FindByIdAsync(request.CustomerId, cancellationToken);
        if (customer == null)
        {
            throw new CustomerNotFoundException(request.CustomerId);
        }
        
        // DOMAIN LOGIC - Create order with domain invariant validation
        var order = Order.Create(request.OrderNumber, request.CustomerId, request.Items);
        
        await _orderRepository.AddAsync(order, cancellationToken);
        
        return _mapper.Map<OrderResponse>(order);
    }
}
```

---

## 8. Validation Standards

### Validation Layer Separation

#### Input Validation (FluentValidation)
Handle data format, required fields, and basic constraints:

```csharp
public class CreateEntityCommandValidator : AbstractValidator<CreateEntityCommand>
{
    public CreateEntityCommandValidator()
    {
        RuleFor(x => x.EntityCode).NotEmpty().NotNull().MaximumLength(50);
        RuleFor(x => x.EntityType).NotEmpty().NotNull().MaximumLength(20);
        RuleFor(x => x.Name).NotEmpty().NotNull().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(500);
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .When(x => !string.IsNullOrEmpty(x.Email));
    }
}
```

#### Application Validation (Command Handlers)
Handle existence checks, uniqueness, and cross-aggregate rules:

```csharp
public class CreateEntityCommandHandler : ICommandHandler<CreateEntityCommand, EntityResponse>
{
    public async Task<EntityResponse> Handle(CreateEntityCommand request, CancellationToken cancellationToken)
    {
        // 1. Input validation
        _validator.ValidateAndThrow(request);
        
        // 2. Application business rule validation
        await ValidateApplicationRules(request, cancellationToken);
        
        // 3. Domain logic execution
        var entity = Entity.Create(request.EntityCode, request.EntityType, request.Name, request.Description);
        
        await _entityRepository.AddAsync(entity, cancellationToken);
        
        return _mapper.Map<EntityResponse>(entity);
    }

    private async Task ValidateApplicationRules(CreateEntityCommand request, CancellationToken cancellationToken)
    {
        // Uniqueness validation - APPLICATION CONCERN
        var existingEntity = await _entityRepository.FindByCodeAsync(request.EntityCode, cancellationToken);
        if (existingEntity != null)
        {
            throw new EntityAlreadyExistsException(request.EntityCode);
        }

        // Entity type validation - APPLICATION CONCERN
        if (!string.IsNullOrEmpty(request.EntityType))
        {
            var entityType = await _entityTypeRepository.FindByCodeAsync(request.EntityType, cancellationToken);
            if (entityType == null)
            {
                throw new EntityTypeNotFoundException(request.EntityType);
            }
        }

        // Cross-aggregate validation - APPLICATION CONCERN
        if (request.ParentEntityId.HasValue)
        {
            var parentEntity = await _entityRepository.FindByIdAsync(request.ParentEntityId.Value, cancellationToken);
            if (parentEntity == null)
            {
                throw new ParentEntityNotFoundException(request.ParentEntityId.Value);
            }
        }
    }
}
```

#### Domain Validation (Aggregate Roots)
Handle invariants that ensure entity validity:

```csharp
public static Entity Create(string entityCode, string entityType, string name, string? description)
{
    // Domain invariant validation - DOMAIN CONCERN
    if (string.IsNullOrWhiteSpace(entityCode))
        throw new ArgumentException("Entity code cannot be empty", nameof(entityCode));
    
    if (string.IsNullOrWhiteSpace(name))
        throw new ArgumentException("Name cannot be empty", nameof(name));

    // Domain business rules - DOMAIN CONCERN
    if (name.Length > 200)
        throw new ArgumentException("Name cannot exceed 200 characters", nameof(name));

    return new Entity(entityCode, entityType, name, description);
}
```

### Validation Standards Summary

**FluentValidation (Input Layer):**
- Data type validation
- Required field validation
- Format validation (email, phone, etc.)
- Length constraints
- Range validation

**Application Layer:**
- Entity existence validation
- Uniqueness constraints
- Cross-aggregate business rules
- External system validation
- Use case specific rules

**Domain Layer:**
- Entity invariant validation
- Core business rule validation
- State consistency validation
- Value object validation

---

## 9. Error Handling & Exception Management

### Domain Exception Pattern

```csharp
public class EntityNotFoundException : BusinessException
{
    public string EntityCode { get; init; }

    public EntityNotFoundException(string entityCode)
        : base(Exceptions.EntityNotFound.TITLE,
               Exceptions.EntityNotFound.ERRORCODE,
               $"The entity with code {entityCode} was not found")
    {
        EntityCode = entityCode;
    }
}
```

**Exception Guidelines:**
- Inherit from `BusinessException` for business rule violations
- Include relevant context properties
- Use constants for error codes and titles
- Provide descriptive error messages

### Error Response Format

All errors should return RFC 7807 Problem Details format:

```json
{
  "type": "ENTITY_NOT_FOUND",
  "title": "Entity Not Found",
  "detail": "The entity with code ABC123 was not found",
  "status": 422
}
```

### HTTP Status Code Mapping

| Exception Type | HTTP Status | Usage |
|---------------|-------------|-------|
| `ValidationException` | 400 Bad Request | Input validation failures |
| `BusinessException` | 422 Unprocessable Entity | Business rule violations |
| `NotFoundException` | 404 Not Found | Resource not found |
| `UnauthorizedException` | 401 Unauthorized | Authentication required |
| `ForbiddenException` | 403 Forbidden | Access denied |
| `Exception` | 500 Internal Server Error | System errors |

---

## 10. Authentication & Authorization

### Authorization Configuration

```csharp
if (authorizationRequired)
{
    createEntity.RequireAuthorization(builder =>
    {
        builder.RequireRole("{ServiceName}.Create");
    });

    updateEntity.RequireAuthorization(builder =>
    {
        builder.RequireRole("{ServiceName}.Update");
    });
}
```

### Role-Based Authorization

**Standard Role Naming Convention:**
- `{ServiceName}.{Action}` (e.g., `Orders.Create`, `Products.Read`)
- Use granular permissions for fine-grained control
- Apply authorization conditionally based on environment

### Security Headers

Ensure all APIs include appropriate security headers:
- `X-Request-Id`: Correlation tracking
- `X-Product-Type`: Product context identification

---

## 11. Middleware & Request Pipeline

### Request Middleware Pattern with Platform.Shared

```csharp
public async Task Invoke(HttpContext context,
    IMultiProductRequestContextProvider requestContextProvider, // Platform.Shared interface
    ILogger<RequestMiddleware> logger,
    IWebHostEnvironment environment,
    ProductApiClient productApiClient)
{
    // Set correlationId
    string correlationId = GenerateOrExtractCorrelationId(context);
    
    // Set product context - this feeds into Platform.Shared's multi-product system
    await SetProductContext(context, requestContextProvider, productApiClient);
    
    // Process request with logging context
    using (LogContext.PushProperty("CorrelationId", correlationId))
    {
        await _next(context); // Platform.Shared components automatically use the product context
    }
}

private async Task SetProductContext(
    HttpContext context, 
    IMultiProductRequestContextProvider contextProvider, 
    ProductApiClient productApiClient)
{
    string? product = null;
    
    if (!_environment.IsDevelopment())
    {
        // Production: Extract product from user claims or API key
        if (context.User.Identity!.IsAuthenticated)
        {
            var productClaim = context.User.FindFirst("product")?.Value;
            if (!string.IsNullOrEmpty(productClaim))
            {
                product = productClaim;
            }
        }
    }
    else
    {
        // Development: Use local-product header for testing
        product = context.Request.Headers["local-product"].FirstOrDefault();
    }
    
    if (!string.IsNullOrEmpty(product))
    {
        // This sets the context that Platform.Shared uses for data segregation
        contextProvider.SetProduct(product);
    }
}
```

**Middleware Integration with Platform.Shared:**
1. **Correlation ID Management**: Generate or extract request correlation IDs
2. **Product Context Setting**: Set product context that Platform.Shared consumes
3. **User Context**: Extract user information for Platform.Shared auditing
4. **Request/Response Logging**: Structured logging with correlation
5. **Authentication Challenges**: Handle authentication for non-development environments

**Critical**: Once middleware sets the product context via `IMultiProductRequestContextProvider`, all Platform.Shared components automatically use it for:
- Data filtering in repositories
- Entity association during creation
- Integration event header population
- Audit trail product tracking

### Development vs Production Behavior

```csharp
if (!environment.IsDevelopment())
{
    // Production: Use authentication and product API
    if (context.User.Identity!.IsAuthenticated)
    {
        SetProductContextWithUserClaims(requestContextProvider, context.User, product);
    }
}
else
{
    // Development: Use local-product header for testing
    var localProductHeader = context.Request.Headers["local-product"];
    if (!string.IsNullOrEmpty(localProductHeader))
    {
        requestContextProvider.SetProduct(localProductHeader.ToString());
    }
}
```

---

## 12. Integration Events with Platform.Shared

### CloudEvents Architecture

Platform.Shared implements integration events using the **CloudEvents standard** with product context in headers:

```csharp
// Use Platform.Shared event publisher
_eventPublisher.SaveIntegrationEvent(
    new EntityCreatedIntegrationEvent(entity.EntityCode, entity.Name));

// Platform.Shared automatically:
// 1. Wraps event in CloudEvents format
// 2. Adds product context to CloudEvents headers
// 3. Publishes after successful transaction commit
```

### Clean Event Design

**✅ Correct - Clean Business Event:**
```csharp
public record EntityCreatedIntegrationEvent(
    string EntityCode,
    string Name,
    DateTime CreatedAt) : IIntegrationEvent;
```

**❌ Incorrect - Product Context in Payload:**
```csharp
public record EntityCreatedIntegrationEvent(
    string EntityCode,
    string Name,
    string Product, // ❌ Don't include product context
    DateTime CreatedAt) : IIntegrationEvent;
```

### Event Naming Convention

- `{Entity}{Action}IntegrationEvent`
- Examples: `EntityCreatedIntegrationEvent`, `OrderSubmittedIntegrationEvent`
- Inherit from Platform.Shared `IIntegrationEvent`

### Platform.Shared Event Features

**Automatic Capabilities:**
- **CloudEvents Format**: Events wrapped in standard CloudEvents envelope
- **Header-Based Routing**: Product context in CloudEvents headers for routing
- **Transaction Integration**: Events published only after successful database transaction
- **Reliability**: Built-in retry and error handling
- **Monitoring**: Automatic instrumentation and telemetry

### Event Handler Pattern - Focus on Business Logic

```csharp
public class EntityCreatedIntegrationEventHandler : IIntegrationEventHandler<EntityCreatedIntegrationEvent>
{
    private readonly INotificationService _notificationService;
    private readonly IRepository<EntityAuditLog, Guid> _auditLogRepository;
    private readonly IEntityIndexingService _indexingService;
    
    public EntityCreatedIntegrationEventHandler(
        INotificationService notificationService,
        IRepository<EntityAuditLog, Guid> auditLogRepository,
        IEntityIndexingService indexingService)
    {
        _notificationService = notificationService;
        _auditLogRepository = auditLogRepository;
        _indexingService = indexingService;
    }
    
    public async Task Handle(EntityCreatedIntegrationEvent @event)
    {
        // Focus ONLY on business logic - no manual product context handling needed
        
        // Send business notifications
        await _notificationService.SendEntityCreatedNotificationAsync(
            @event.EntityCode, 
            @event.Name);
        
        // Create audit trail entry
        // Platform.Shared automatically handles product segregation
        var auditLog = EntityAuditLog.Create(
            @event.EntityCode,
            "Entity Created",
            $"Entity {@event.Name} was created at {@event.CreatedAt}");
            
        await _auditLogRepository.AddAsync(auditLog);
        
        // Update search index
        await _indexingService.IndexEntityAsync(@event.EntityCode, @event.Name);
        
        // All repositories and services automatically handle product context:
        // - Repository saves audit log with correct product association
        // - Services receive product context through their infrastructure
        // - No manual product filtering or routing needed
    }
}
```

### Event Data Guidelines

**Include in Event Payload:**
- Essential business identifiers (EntityCode, OrderId, etc.)
- Business-relevant data (Name, Status, etc.)
- Timestamps (CreatedAt, UpdatedAt, etc.)

**Never Include in Event Payload:**
- Product context (handled via CloudEvents headers)
- Infrastructure concerns (correlation IDs, etc.)
- Authentication/authorization data

### Integration Event Handler Best Practices

**✅ DO - Focus on Business Logic Only:**
```csharp
public async Task Handle(DeviceCreatedIntegrationEvent @event)
{
    // Business logic without manual product handling
    await _provisioningService.ProvisionDeviceAsync(@event.DeviceId);
    
    // Repository operations automatically handle product segregation
    var record = SetupRecord.Create(@event.DeviceId, "Setup Complete");
    await _repository.AddAsync(record);
}
```

**❌ DON'T - Manual Product Context Handling:**
```csharp
public async Task Handle(DeviceCreatedIntegrationEvent @event)
{
    // ❌ Don't manually extract product context
    var product = await _contextProvider.GetProductAsync();
    
    // ❌ Don't implement product-specific branching
    if (product == "ProductA")
    {
        await HandleProductALogic(@event);
    }
    else if (product == "ProductB")
    {
        await HandleProductBLogic(@event);
    }
}
```

**Key Principles:**
1. **No Manual Product Filtering**: Platform.Shared infrastructure handles product segregation automatically
2. **Business Logic Focus**: Handlers should focus on business operations, not infrastructure concerns
3. **Automatic Data Segregation**: Repository operations automatically associate data with the correct product
4. **Service-Level Context**: Services receive product context through their own infrastructure, not through handlers
5. **Clean Separation**: Keep business event handling separate from multi-product routing logic

---

## 13. Testing Standards

### Integration Test Structure

```csharp
[Fact]
public async Task should_create_entity()
{
    var command = _fixture.GenerateCreateCommand();
    var result = await _apiClient.Entities.Create.PostAsync(command, cancellationToken: default(CancellationToken));
    result.ShouldNotBeNull();
    result.EntityCode.ShouldBe(command.EntityCode);
}
```

### Test Categories

1. **Unit Tests**
   - Domain entity behavior
   - Application service logic  
   - Validation rules

2. **Integration Tests**
   - API endpoint functionality
   - Database interactions
   - External service integration

3. **End-to-End Tests**
   - Complete user workflows
   - Cross-service communication

### Test Naming Convention

- `should_{expected_behavior}`
- `should_throw_{exception_type}_when_{condition}`

### Assertion Library

Use **Shouldly** for fluent assertions:

```csharp
result.ShouldNotBeNull();
result.EntityCode.ShouldBe(command.EntityCode);
exception.ResponseStatusCode.ShouldBe(400);
```

---

## 14. API Versioning

### Version Configuration

```csharp
var versionString = config.GetValue<string>("Version");
if (string.IsNullOrEmpty(versionString))
{
    versionString = "1.0.0.0";
}
var version = Version.Parse(versionString);
ApiVersion apiVersion = new ApiVersion(version.Major, version.Minor);
var versionSet = app.NewApiVersionSet().HasApiVersion(apiVersion).ReportApiVersions().Build();
```

### Endpoint Versioning

```csharp
.WithApiVersionSet(apiVersionSet)
.MapToApiVersion(1.0)
```

**Versioning Standards:**
- Use semantic versioning (Major.Minor.Patch)
- Apply versions to endpoint groups, not individual endpoints
- Configure version from `appsettings.json`
- Support multiple versions during transition periods

---

## 15. OpenAPI Documentation

### Endpoint Documentation

```csharp
.WithSummary("Create Entity endpoint")
```

### Enhanced Documentation

```csharp
.WithSummary("Get Entities endpoint")
.WithDescription("Filter entities by code. Minimum 3 characters required for search. " +
                "Optional parameter - if not provided, returns all entities for the product.")
```

**Documentation Standards:**
- Provide clear, concise endpoint summaries
- Document parameter purposes and constraints
- Include example use cases in descriptions
- Specify required vs optional parameters

---

## 16. Logging & Monitoring

### Structured Logging

```csharp
using (LogContext.PushProperty("CorrelationId", correlationId))
{
    logger.LogDebug("{CorrelationId} beginning...", correlationId);
    await _next(context);
}
```

### Instrumentation

```csharp
_instrumentation.TrackEvent("entity_created", 
    _instrumentHelper.GetInstrumentationProperties(entity));
```

**Logging Standards:**
- Use structured logging with Serilog
- Include correlation IDs in all log entries
- Log at appropriate levels (Debug, Information, Warning, Error)
- Avoid logging sensitive data
- Use instrumentation for business metrics

---

## 17. Code Quality & Style Guidelines

### Naming Conventions

| Type | Convention | Example |
|------|------------|---------|
| Namespace | `Platform.{ServiceName}.{Layer}` | `Platform.Orders.Application` |
| Class | PascalCase | `OrderRepository` |
| Interface | IPascalCase | `IOrderRepository` |
| Method | PascalCase | `CreateOrder` |
| Property | PascalCase | `OrderCode` |
| Field (private) | _camelCase | `_orderRepository` |
| Parameter | camelCase | `orderCode` |
| Local Variable | camelCase | `result` |

### File Organization

- One class per file
- File name matches class name
- Organize using folders by feature/aggregate
- Keep related files together

### Dependency Injection

```csharp
public CreateEntityCommandHandler(
    IEntityRepository entityRepository,
    IValidator<CreateEntityCommand> validator,
    IMapper mapper,
    IIntegrationEventPublisher integrationEventPublisher)
{
    _entityRepository = entityRepository;
    _validator = validator;
    _mapper = mapper;
    _integrationEventPublisher = integrationEventPublisher;
}
```

**DI Guidelines:**
- Use constructor injection
- Register services in appropriate service collection extensions
- Prefer interfaces over concrete implementations
- Use scoped lifetime for repositories and services

### Code Style

- Use `record` for immutable data structures
- Prefer `var` for local variables when type is obvious
- Use nullable reference types (`string?`)
- Include XML documentation for public APIs
- Follow async/await patterns consistently

---

## 18. NuGet Package Configuration

### Platform.Shared Package Source

All Platform API projects require access to the **Platform.Shared** library, which is hosted in a private Azure DevOps Artifacts feed. Proper NuGet configuration is essential for package restoration and build success.

### Required NuGet Package Source

**Artifact Feed**: `coldchain-software`  
**URL**: `https://pkgs.dev.azure.com/DigitalAndConnectedTechnologies/_packaging/coldchain-software/nuget/v3/index.json`

### NuGet.Config File

Every solution **must** include a `NuGet.Config` file at the solution root with the following configuration:

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" protocolVersion="3" />
    <add key="coldchain-software" value="https://pkgs.dev.azure.com/DigitalAndConnectedTechnologies/_packaging/coldchain-software/nuget/v3/index.json" protocolVersion="3" />
  </packageSources>
  <packageSourceMapping>
    <packageSource key="nuget.org">
      <package pattern="*" />
    </packageSource>
    <packageSource key="coldchain-software">
      <package pattern="Platform.Shared*" />
    </packageSource>
  </packageSourceMapping>
</configuration>
```

### Global NuGet Configuration

Developers should also configure the package source globally on their development machines:

```bash
# Add the coldchain-software package source globally
dotnet nuget add source "https://pkgs.dev.azure.com/DigitalAndConnectedTechnologies/_packaging/coldchain-software/nuget/v3/index.json" --name "coldchain-software"

# Verify package sources
dotnet nuget list source
```

### Authentication Requirements

#### Azure DevOps Artifacts Credential Provider

Install the Azure Artifacts Credential Provider for automatic authentication:

**Windows:**
```powershell
# Install via PowerShell (run as Administrator)
iwr https://aka.ms/install-artifacts-credprovider.ps1 | iex
```

**Alternative - Manual Installation:**
```bash
# Download and install the Azure Artifacts Credential Provider
# https://github.com/microsoft/artifacts-credprovider
```

#### Personal Access Token (PAT)

If the credential provider doesn't work, configure a Personal Access Token:

1. **Create PAT in Azure DevOps:**
   - Go to Azure DevOps → User Settings → Personal Access Tokens
   - Create new token with **Packaging (read)** scope
   - Copy the token value

2. **Configure PAT for NuGet:**
   ```bash
   # Add authenticated source with PAT
   dotnet nuget add source "https://pkgs.dev.azure.com/DigitalAndConnectedTechnologies/_packaging/coldchain-software/nuget/v3/index.json" --name "coldchain-software" --username "PAT" --password "YOUR_PAT_TOKEN" --store-password-in-clear-text
   ```

#### Environment Variables

For CI/CD pipelines, use environment variables:

```yaml
# Azure DevOps Pipeline
env:
  NUGET_CREDENTIALPROVIDER_SESSIONTOKENCACHE_ENABLED: true
  VSS_NUGET_EXTERNAL_FEED_ENDPOINTS: |
    {
      "endpointCredentials": [
        {
          "endpoint":"https://pkgs.dev.azure.com/DigitalAndConnectedTechnologies/_packaging/coldchain-software/nuget/v3/index.json", 
          "password":"$(System.AccessToken)"
        }
      ]
    }
```

### Package Source Verification

Verify package source configuration:

```bash
# List all configured sources
dotnet nuget list source

# Test package restoration
dotnet restore --verbosity detailed

# Search for Platform.Shared packages (if authenticated)
dotnet package search Platform.Shared --source coldchain-software
```

### Troubleshooting

#### Common Issues

1. **Authentication Failures:**
   - Ensure Azure Artifacts Credential Provider is installed
   - Verify PAT has correct permissions (Packaging - Read)
   - Clear NuGet cache: `dotnet nuget locals all --clear`

2. **Package Not Found:**
   - Verify package source URL is correct
   - Check if you have access to the Azure DevOps organization
   - Ensure package source mapping is configured correctly

3. **Build Failures:**
   - Run `dotnet restore` explicitly before building
   - Check for package version conflicts
   - Verify target framework compatibility

#### Diagnostic Commands

```bash
# Clear all NuGet caches
dotnet nuget locals all --clear

# Restore with detailed logging
dotnet restore --verbosity detailed

# List package sources with authentication status
dotnet nuget list source

# Test connectivity to Azure DevOps feed
curl -I "https://pkgs.dev.azure.com/DigitalAndConnectedTechnologies/_packaging/coldchain-software/nuget/v3/index.json"
```

### CI/CD Pipeline Configuration

#### Azure DevOps Pipeline

```yaml
steps:
- task: DotNetCoreCLI@2
  displayName: 'Restore NuGet Packages'
  inputs:
    command: 'restore'
    projects: '**/*.csproj'
    feedsToUse: 'config'
    nugetConfigPath: 'NuGet.Config'
    includeNuGetOrg: true
  env:
    NUGET_CREDENTIALPROVIDER_SESSIONTOKENCACHE_ENABLED: true
```

#### GitHub Actions

```yaml
steps:
- name: Setup .NET
  uses: actions/setup-dotnet@v3
  with:
    dotnet-version: '8.0.x'
    source-url: https://pkgs.dev.azure.com/DigitalAndConnectedTechnologies/_packaging/coldchain-software/nuget/v3/index.json
  env:
    NUGET_AUTH_TOKEN: ${{ secrets.AZURE_DEVOPS_PAT }}

- name: Restore dependencies
  run: dotnet restore
```

### Security Considerations

- **Never commit PAT tokens to source control**
- Use environment variables or secure secrets storage for tokens
- Regularly rotate Personal Access Tokens
- Use minimum required permissions (Packaging - Read only)
- Consider using service connections for production pipelines

### Development Environment Setup Checklist

- [ ] Azure Artifacts Credential Provider installed
- [ ] `NuGet.Config` file present in solution root
- [ ] Package source configured globally: `dotnet nuget list source`
- [ ] Authentication working: `dotnet restore` succeeds
- [ ] Package search working: `dotnet package search Platform.Shared`
- [ ] Build succeeds: `dotnet build`

---

## 19. Source Control Management

### Git Configuration Requirements

All Platform API projects must include proper source control configuration to ensure consistent development practices and protect sensitive information.

### .gitignore File

Every solution must include a comprehensive `.gitignore` file at the solution root to prevent committing:

#### Build Artifacts
```gitignore
# .NET build outputs
bin/
obj/
out/
*.dll
*.exe
*.pdb
*.cache

# Build results
[Dd]ebug/
[Rr]elease/
artifacts/
```

#### IDE and Tool Files
```gitignore
# Visual Studio
.vs/
*.user
*.suo
*.sln.docstates

# Visual Studio Code
.vscode/

# JetBrains Rider
.idea/
*.sln.iml
```

#### Sensitive Configuration
```gitignore
# Environment-specific settings (containing sensitive data)
**/appsettings.Development.json
**/appsettings.Production.json
**/appsettings.Staging.json

# Keep template files
!**/appsettings.example.json
!**/appsettings.template.json

# Environment files
.env
.env.local
.env.*.local
```

#### Platform.Shared Specific
```gitignore
# Platform.Shared local packages (if using local package source)
packages/Platform.Shared*

# Test coverage and analysis
TestResults/
coverage/
*.trx
*.coverage
.sonarqube/
```

#### Database Files
```gitignore
# SQL Server local database files
*.mdf
*.ldf
*.ndf

# SQLite databases
*.db
*.sqlite
*.sqlite3
```

### .gitignore Template

Use the comprehensive `.gitignore` template that includes:
- Standard .NET build artifacts
- IDE-specific files (Visual Studio, VS Code, Rider)
- Platform.Shared specific exclusions
- Sensitive configuration files
- Test and coverage reports
- Database files
- OS-specific files (Windows, macOS, Linux)
- CI/CD pipeline files (when appropriate)

**Location**: Place `.gitignore` at the solution root level.

### Repository Structure Standards

#### Branching Strategy
- Use GitFlow or GitHub Flow branching strategy
- `main`/`master`: Production-ready code
- `develop`: Integration branch for features
- `feature/*`: Feature development branches
- `release/*`: Release preparation branches
- `hotfix/*`: Critical fixes for production

#### Commit Message Format
```
<type>(<scope>): <description>

[optional body]

[optional footer(s)]
```

**Types:**
- `feat`: New feature
- `fix`: Bug fix
- `docs`: Documentation changes
- `style`: Formatting, missing semicolons, etc.
- `refactor`: Code refactoring
- `test`: Adding tests
- `chore`: Maintenance tasks

**Examples:**
```
feat(locations): add location registration endpoint
fix(validation): correct location code length validation
docs(readme): update API usage examples
```

### Pre-commit Standards

#### Required Files in Repository
- `.gitignore` - Comprehensive exclusion rules
- `README.md` - Project documentation and usage
- `CHANGELOG.md` - Version history and changes
- Solution file (`.sln`) - Visual Studio solution

#### Security Considerations
- Never commit connection strings with credentials
- Use appsettings templates with placeholder values
- Exclude all environment-specific configuration files
- Use Azure Key Vault or similar for production secrets

### GitHub/Azure DevOps Integration

#### Pull Request Requirements
- All changes must go through pull request review
- Require at least one approval from code owner
- Enforce branch protection rules
- Run automated tests before merge
- Include relevant issue/work item references

#### Continuous Integration
- Automated build on all branches
- Run unit and integration tests
- Code quality analysis (SonarQube)
- Security scanning for vulnerabilities

### Documentation Requirements

Each repository must include:

1. **README.md** with:
   - Project overview and purpose
   - Getting started instructions
   - API usage examples
   - Configuration requirements
   - Development setup guide

2. **IMPLEMENTATION_NOTES.md** (if applicable):
   - Platform.Shared integration notes
   - Known limitations or dependencies
   - Build and deployment considerations

3. **API Documentation**:
   - OpenAPI/Swagger specifications
   - Endpoint usage examples
   - Error handling documentation

---

## Platform.Shared v1.1.20250915.1 Migration Guide

The following changes are required when upgrading to Platform.Shared version 1.1.20250915.1:

### Namespace Updates

1. **CQRS Interfaces**:
   ```csharp
   // OLD
   using Platform.Shared.Cqrs;
   
   // NEW
   using Platform.Shared.Cqrs.Mediatr;
   ```

2. **Repository Interfaces**:
   ```csharp
   // OLD
   using Platform.Shared.DataLayer;
   
   // NEW
   using Platform.Shared.DataLayer.Repositories;
   ```

### API Method Changes

3. **Integration Events Publisher**:
   ```csharp
   // OLD
   _eventPublisher.AddIntegrationEvent(eventObject);
   
   // NEW (now synchronous)
   _eventPublisher.SaveIntegrationEvent(eventObject);
   ```

4. **OpenAPI Documentation**:
   ```csharp
   // OLD
   .WithOpenApi(operation => new(operation) { Summary = "Description" })
   
   // NEW
   .WithSummary("Description")
   .WithDescription("Detailed description") // optional
   ```

5. **API Versioning**:
   ```csharp
   // OLD
   .HasApiVersions(new ApiVersion[] { apiVersion })
   
   // NEW
   .HasApiVersion(apiVersion)
   ```

### Database Context Changes

6. **PlatformDbContext Constructor**:
   ```csharp
   // NEW - Requires ILogger injection
   public YourDbContext(DbContextOptions<YourDbContext> options, ILogger<YourDbContext> logger) 
       : base(options, logger)
   {
   }
   ```

7. **Repository Entity Access**:
   ```csharp
   // OLD (method no longer available)
   return await GetQueryable().FirstOrDefaultAsync(...);
   
   // NEW
   return await DbContext.Set<Entity>().FirstOrDefaultAsync(...);
   ```

### Service Registration Changes

8. **Platform.Shared Services Organization**:
   ```csharp
   // HttpApi Project Extension
   public static IServiceCollection AddYourHttpApi(this IServiceCollection services, 
       IConfiguration configuration, IConfigurationBuilder configurationBuilder, IWebHostEnvironment environment)
   {
       services.AddPlatformCommonHttpApi(configuration, configurationBuilder, environment, "ServiceName")
              .WithAuditing()
              .WithMultiProduct();
       return services;
   }
   
   // Application Project Extension
   public static IServiceCollection AddYourApplication(this IServiceCollection services)
   {
       services.AddIntegrationEventsServices();
       // ... other services
       return services;
   }
   ```

### Dependencies

9. **FluentValidation Extension**:
   ```xml
   <!-- Add to Application project -->
   <PackageReference Include="FluentValidation.DependencyInjectionExtensions" Version="11.9.0" />
   ```

### Domain Model Updates

10. **Entity IsActive Property**:
    ```csharp
    // OLD - Direct assignment causes compilation error
    entity.IsActive = true;
    
    // NEW - IsActive is now read-only, use methods
    entity.Activate();
    entity.Deactivate();
    ```

### Breaking Changes Summary

- **Integration events are now synchronous** - remove `await` from `SaveIntegrationEvent()` calls
- **Database context requires ILogger** - update constructors and DI registration
- **Repository entity access changed** - use `DbContext.Set<T>()` instead of `GetQueryable()`
- **Namespace reorganization** - update using statements for CQRS and repository interfaces
- **OpenAPI methods renamed** - replace `WithOpenApi()` with `WithSummary()`/`WithDescription()`

---

## Implementation Checklist

When implementing new APIs with Platform.Shared, ensure:

### Pre-Development
- [ ] Domain entities inherit from Platform.Shared base classes (`FullyAuditedAggregateRoot`, etc.)
- [ ] Entities implement Platform.Shared interfaces (`IMultiProductObject`, `IInstrumentationObject`)
- [ ] Repository interfaces use Platform.Shared abstractions (`IRepository<TEntity, TId>`)
- [ ] Exception classes inherit from Platform.Shared `BusinessException`
- [ ] Constants defined

### NuGet Package Configuration
- [ ] `NuGet.Config` file created at solution root
- [ ] coldchain-software package source configured in NuGet.Config
- [ ] Package source mapping configured for Platform.Shared packages
- [ ] Global NuGet source configured on development machine
- [ ] Azure Artifacts Credential Provider installed (or PAT configured)
- [ ] Package restoration succeeds: `dotnet restore`
- [ ] Build succeeds with Platform.Shared packages: `dotnet build`

### Platform.Shared Integration
- [ ] Added Platform.Shared NuGet package reference
- [ ] Configured Platform.Shared services in appropriate project extensions:
  - [ ] HttpApi project: `services.AddPlatformCommonHttpApi().WithAuditing().WithMultiProduct()`
  - [ ] Application project: `services.AddIntegrationEventsServices()`
- [ ] Configured `TransactionBehavior` in MediatR pipeline
- [ ] DbContext inherits from `PlatformDbContext`
- [ ] Middleware sets product context via `IMultiProductRequestContextProvider`

### Development
- [ ] Commands/Queries use Platform.Shared CQRS interfaces:
  - [ ] Commands inherit from `ICommand<TResponse>`
  - [ ] Command handlers inherit from `ICommandHandler<TCommand, TResponse>`
  - [ ] Queries inherit from `IQuery<TResponse>`
  - [ ] Query handlers inherit from `IQueryHandler<TQuery, TResponse>`
- [ ] Validators created using FluentValidation for input validation
- [ ] Command/Query handlers use Platform.Shared repositories with automatic filtering
- [ ] DTOs defined for requests and responses
- [ ] AutoMapper profiles configured
- [ ] Integration events inherit from Platform.Shared `IIntegrationEvent`
- [ ] Integration events contain **only business data** (no product context)

### Domain Logic
- [ ] Business invariants implemented in aggregate roots
- [ ] Static factory methods created for entity construction
- [ ] Domain rule validation in entity methods (no external dependencies)
- [ ] Domain exceptions thrown for invariant violations
- [ ] Value objects used for complex data types
- [ ] Entities implement `ToInstrumentationProperties()` for telemetry

### Application Logic
- [ ] Entity existence validation in command handlers
- [ ] Uniqueness validation in command handlers
- [ ] Cross-aggregate validation in command handlers
- [ ] External system validation in command handlers
- [ ] **No manual product context handling** - Platform.Shared handles automatically
- [ ] **No manual audit field setting** - Platform.Shared handles automatically
- [ ] Use Platform.Shared `IIntegrationEventPublisher.SaveIntegrationEvent()` (synchronous)

### API Layer
- [ ] Minimal API endpoints registered
- [ ] OpenAPI documentation added
- [ ] Response types declared
- [ ] Authorization applied where needed
- [ ] API versioning configured
- [ ] Middleware configured to set product context from caller identity

### Testing
- [ ] Unit tests for domain logic (no external dependencies)
- [ ] Unit tests for application services (with mocked Platform.Shared dependencies)
- [ ] Integration tests for API endpoints
- [ ] Validation tests for all validation layers
- [ ] Exception handling tests
- [ ] Multi-product isolation tests
- [ ] Integration event tests (verify clean payloads)

### Platform.Shared Verification
- [ ] Auditing fields automatically populated on entity changes
- [ ] Product context automatically set on new entities
- [ ] Repository queries automatically filtered by product context
- [ ] Integration events published as CloudEvents with product in headers
- [ ] Database transactions managed by `TransactionBehavior`
- [ ] Soft deletes working (entities marked as deleted, not removed)
- [ ] Instrumentation events published for business operations

### Documentation
- [ ] API endpoints documented
- [ ] Business rules documented
- [ ] Validation layer responsibilities documented
- [ ] Error scenarios documented
- [ ] Integration event contracts documented (business data only)
- [ ] Multi-product context flow documented
- [ ] Platform.Shared integration points documented

### Source Control Management
- [ ] Comprehensive `.gitignore` file added at solution root
- [ ] Sensitive configuration files excluded from source control
- [ ] Platform.Shared local packages excluded
- [ ] `README.md` with project overview and usage instructions
- [ ] `IMPLEMENTATION_NOTES.md` for Platform.Shared integration notes (if applicable)
- [ ] Repository branching strategy defined
- [ ] Commit message format guidelines followed
- [ ] Pull request templates configured (if using GitHub/Azure DevOps)
- [ ] Branch protection rules enabled for main branches
- [ ] No hardcoded credentials or connection strings committed
- [ ] Environment-specific appsettings files excluded but templates provided

---

This document should be treated as a living standard that evolves with the platform. Regular reviews and updates ensure consistency across all API implementations within the Copeland Platform ecosystem.
