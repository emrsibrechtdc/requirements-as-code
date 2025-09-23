# Platform.Shared API Reference Guide

## Table of Contents
1. [Auditing API](#auditing-api)
2. [Data Layer API](#data-layer-api)
3. [CQRS API](#cqrs-api)
4. [Integration Events API](#integration-events-api)
5. [Multi-Product API](#multi-product-api)
6. [Caching API](#caching-api)
7. [HTTP API Extensions](#http-api-extensions)
8. [Instrumentation API](#instrumentation-api)
9. [Entity Framework Extensions](#entity-framework-extensions)
10. [Exception Types](#exception-types)

## Auditing API

### Core Interfaces

#### IAuditPropertySetter
Sets audit properties on entities automatically.

```csharp
namespace Platform.Shared.Auditing
{
    /// <summary>
    /// Sets audit properties
    /// </summary>
    public interface IAuditPropertySetter
    {
        /// <summary>
        /// Sets create audited properties
        /// </summary>
        /// <param name="entity">Entity to set properties on</param>
        void SetCreateAuditedProperties(ICreateAuditedEntity entity);
        
        /// <summary>
        /// Sets update audit properties
        /// </summary>
        /// <param name="entity">Entity to set properties on</param>
        void SetUpdateAuditedProperties(IUpdateAuditedEntity entity);
        
        /// <summary>
        /// Sets delete audit properties
        /// </summary>
        /// <param name="entity">Entity to set properties on</param>
        void SetDeleteAuditedProperties(IDeleteAuditedEntity entity);
    }
}
```

#### Audit Entity Interfaces

```csharp
namespace Platform.Shared.Auditing
{
    /// <summary>
    /// Interface for entities that track creation
    /// </summary>
    public interface ICreateAuditedEntity
    {
        DateTime CreatedAt { get; set; }
        string? CreatedBy { get; set; }
    }

    /// <summary>
    /// Mutable version of create audited entity
    /// </summary>
    public interface ICreateAuditedEntityMutable : ICreateAuditedEntity
    {
        new DateTime CreatedAt { get; set; }
        new string? CreatedBy { get; set; }
    }

    /// <summary>
    /// Interface for entities that track updates
    /// </summary>
    public interface IUpdateAuditedEntity
    {
        DateTime UpdatedAt { get; set; }
        string? UpdatedBy { get; set; }
    }

    /// <summary>
    /// Mutable version of update audited entity
    /// </summary>
    public interface IUpdateAuditedEntityMutable : IUpdateAuditedEntity
    {
        new DateTime UpdatedAt { get; set; }
        new string? UpdatedBy { get; set; }
    }

    /// <summary>
    /// Interface for entities that track deletion
    /// </summary>
    public interface IDeleteAuditedEntity
    {
        DateTime? DeletedAt { get; set; }
        string? DeletedBy { get; set; }
    }

    /// <summary>
    /// Interface for fully audited entities
    /// </summary>
    public interface IFullyAuditedEntity : ICreateAuditedEntity, IUpdateAuditedEntity, IDeleteAuditedEntity
    {
    }
}
```

### Base Classes

#### FullyAuditedAggregateRoot<TId>
```csharp
namespace Platform.Shared.Auditing
{
    /// <summary>
    /// Base class for fully audited aggregate roots
    /// </summary>
    /// <typeparam name="TId">Type of the identifier</typeparam>
    public abstract class FullyAuditedAggregateRoot<TId> : AggregateRoot<TId>, IFullyAuditedEntity
    {
        public DateTime CreatedAt { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string? UpdatedBy { get; set; }
        public DateTime? DeletedAt { get; set; }
        public string? DeletedBy { get; set; }
    }
}
```

### Extension Methods
```csharp
namespace Platform.Shared.Auditing.Extensions
{
    public static class AuditingServiceCollectionExtensions
    {
        /// <summary>
        /// Adds Platform Common Auditing services to DI container
        /// </summary>
        /// <param name="services">Service collection</param>
        /// <returns>Service collection for chaining</returns>
        public static IServiceCollection AddPlatformCommonAuditing(this IServiceCollection services)
        {
            services.AddScoped<IAuditPropertySetter, AuditPropertySetter>();
            return services;
        }
    }
}
```

## Data Layer API

### Core Interfaces

#### IUnitOfWork
```csharp
namespace Platform.Shared.DataLayer
{
    /// <summary>
    /// Unit of work pattern implementation
    /// </summary>
    public interface IUnitOfWork : IDisposable
    {
        /// <summary>
        /// Boolean indicating whether there is an active transaction
        /// </summary>
        bool HasActiveTransaction { get; }

        /// <summary>
        /// Save entity changes to the database
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Number of affected entities</returns>
        Task<int> SaveEntitiesAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Rollback the current transaction
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        Task RollbackTransaction(CancellationToken cancellationToken = default);

        /// <summary>
        /// Executes operation within the current transaction
        /// </summary>
        /// <param name="operation">Operation to execute</param>
        Task ExecuteOperationInTransactionAsync(Func<Task> operation);

        /// <summary>
        /// Gets the guid of the current transaction
        /// </summary>
        /// <returns>Transaction ID or null if no active transaction</returns>
        Guid? GetCurrentTransactionId();

        /// <summary>
        /// Begin a new transaction
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Transaction ID</returns>
        Task<Guid> BeginTransactionAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Commits the current transaction
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Number of affected entities</returns>
        Task<int> CommitTransactionAsync(CancellationToken cancellationToken = default);
    }
}
```

### Repository Interfaces

```csharp
namespace Platform.Shared.DataLayer.Repositories
{
    /// <summary>
    /// Read-only repository interface
    /// </summary>
    /// <typeparam name="TEntity">Entity type</typeparam>
    /// <typeparam name="TId">ID type</typeparam>
    public interface IReadRepository<TEntity, TId> where TEntity : IEntity<TId>
    {
        Task<TEntity?> GetByIdAsync(TId id, CancellationToken cancellationToken = default);
        Task<IEnumerable<TEntity>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<PageResult<TEntity>> GetPagedAsync(int pageSize, int pageNumber, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Write-only repository interface
    /// </summary>
    /// <typeparam name="TEntity">Entity type</typeparam>
    /// <typeparam name="TId">ID type</typeparam>
    public interface IWriteRepository<TEntity, TId> where TEntity : IEntity<TId>
    {
        Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default);
        Task<TEntity> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);
        Task DeleteAsync(TId id, CancellationToken cancellationToken = default);
        Task DeleteAsync(TEntity entity, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Generic repository with full CRUD operations
    /// </summary>
    /// <typeparam name="TEntity">Entity type</typeparam>
    /// <typeparam name="TId">ID type</typeparam>
    public interface IRepository<TEntity, TId> : IReadRepository<TEntity, TId>, IWriteRepository<TEntity, TId>
        where TEntity : IEntity<TId>
    {
    }
}
```

### Data Filter Interface
```csharp
namespace Platform.Shared.DataLayer
{
    /// <summary>
    /// Data filtering functionality
    /// </summary>
    public interface IDataFilter
    {
        /// <summary>
        /// Enable filter for the specified type
        /// </summary>
        /// <typeparam name="TFilter">Filter type</typeparam>
        void Enable<TFilter>();

        /// <summary>
        /// Disable filter for the specified type
        /// </summary>
        /// <typeparam name="TFilter">Filter type</typeparam>
        void Disable<TFilter>();

        /// <summary>
        /// Check if filter is enabled for the specified type
        /// </summary>
        /// <typeparam name="TFilter">Filter type</typeparam>
        /// <returns>True if enabled, false otherwise</returns>
        bool IsEnabled<TFilter>();
    }
}
```

## CQRS API

### Transaction Behavior
```csharp
namespace Platform.Shared.Cqrs.Mediatr
{
    /// <summary>
    /// MediatR pipeline behavior for handling database transactions
    /// </summary>
    /// <typeparam name="TRequest">Request type</typeparam>
    /// <typeparam name="TResponse">Response type</typeparam>
    public class TransactionBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> 
        where TRequest : notnull
    {
        public TransactionBehavior(
            IUnitOfWork unitOfWork,
            ILogger<TransactionBehavior<TRequest, TResponse>> logger,
            IIntegrationEventPublisher integrationEventPublisher,
            IInstrumentation instrumentation,
            IInstrumentationHelper instrumentationHelper)
        {
            // Constructor implementation
        }

        public Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            // Implementation handles transaction management, logging, and integration events
        }
    }
}
```

### CQRS Interfaces
```csharp
namespace Platform.Shared.Cqrs.Mediatr
{
    /// <summary>
    /// Empty command
    /// </summary>
    public interface ICommand : IRequest { }

    /// <summary>
    /// Command handler
    /// </summary>
    /// <typeparam name="TCommand">Command type</typeparam>
    public interface ICommandHandler<in TCommand> : IRequestHandler<TCommand> where TCommand : ICommand { }

    /// <summary>
    /// Command that produces a result
    /// </summary>
    /// <typeparam name="TCommandResult">Result type</typeparam>
    public interface ICommand<out TCommandResult> : IRequest<TCommandResult> { }

    /// <summary>
    /// Command handler that produces a result
    /// </summary>
    /// <typeparam name="TCommand">Command type</typeparam>
    /// <typeparam name="TCommandResult">Result type</typeparam>
    public interface ICommandHandler<in TCommand, TCommandResult> : IRequestHandler<TCommand, TCommandResult> where TCommand : ICommand<TCommandResult> { }

    /// <summary>
    /// Query command
    /// </summary>
    /// <typeparam name="TQueryResult">Query result type</typeparam>
    public interface IQuery<out TQueryResult> : IRequest<TQueryResult> { }

    /// <summary>
    /// Query command handler
    /// </summary>
    /// <typeparam name="TQuery">Query type</typeparam>
    /// <typeparam name="TQueryResult">Query result type</typeparam>
    public interface IQueryHandler<in TQuery, TQueryResult> : IRequestHandler<TQuery, TQueryResult> where TQuery : IQuery<TQueryResult> { }
}
```

## Integration Events API

### Core Interfaces

```csharp
namespace Platform.Shared.IntegrationEvents
{
    /// <summary>
    /// Base interface for integration events
    /// </summary>
    public interface IIntegrationEvent
    {
    }

    /// <summary>
    /// Publishes integration events
    /// </summary>
    public interface IIntegrationEventPublisher
    {
        /// <summary>
        /// Publishes all queued integration events
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        Task PublishAllIntegrationEvents(CancellationToken cancellationToken = default);

        /// <summary>
        /// Saves an integration event to be published after transaction commit
        /// </summary>
        /// <param name="integrationEvent">Event to save</param>
        void SaveIntegrationEvent(IIntegrationEvent integrationEvent);
    }

    /// <summary>
    /// Processes incoming integration events
    /// </summary>
    public interface IIntegrationEventProcessor
    {
        /// <summary>
        /// Processes an integration event
        /// </summary>
        /// <param name="eventName">Name of the event</param>
        /// <param name="integrationEvent">Serialized event data</param>
        Task ProcessEvent(string eventName, string integrationEvent);
    }

    /// <summary>
    /// Manages integration event subscriptions
    /// </summary>
    public interface IIntegrationEventSubscriptionsManager
    {
        /// <summary>
        /// Subscribe to an integration event
        /// </summary>
        /// <typeparam name="T">Event type</typeparam>
        /// <typeparam name="TH">Handler type</typeparam>
        void Subscribe<T, TH>()
            where T : IIntegrationEvent
            where TH : IIntegrationEventHandler<T>;

        /// <summary>
        /// Unsubscribe from an integration event
        /// </summary>
        /// <typeparam name="T">Event type</typeparam>
        /// <typeparam name="TH">Handler type</typeparam>
        void Unsubscribe<T, TH>()
            where T : IIntegrationEvent
            where TH : IIntegrationEventHandler<T>;

        /// <summary>
        /// Check if there are subscriptions for an event
        /// </summary>
        /// <param name="eventName">Event name</param>
        /// <returns>True if subscriptions exist</returns>
        bool HasSubscriptionsForEvent(string eventName);

        /// <summary>
        /// Get event type by name
        /// </summary>
        /// <param name="eventName">Event name</param>
        /// <returns>Event type</returns>
        Type? GetEventTypeByName(string eventName);

        /// <summary>
        /// Get handler types for event
        /// </summary>
        /// <param name="eventName">Event name</param>
        /// <returns>Collection of handler types</returns>
        IEnumerable<Type> GetHandlersForEvent(string eventName);

        /// <summary>
        /// Get event name for type
        /// </summary>
        /// <typeparam name="T">Event type</typeparam>
        /// <returns>Event name</returns>
        string GetEventName<T>();
    }
}
```

### Event Handler Interface
```csharp
namespace Platform.Shared.IntegrationEvents
{
    /// <summary>
    /// Base interface for integration event handlers
    /// </summary>
    /// <typeparam name="TIntegrationEvent">Event type</typeparam>
    public interface IIntegrationEventHandler<in TIntegrationEvent> : IIntegrationEventHandler
        where TIntegrationEvent : IIntegrationEvent
    {
        /// <summary>
        /// Handles the integration event
        /// </summary>
        /// <param name="event">Event to handle</param>
        Task Handle(TIntegrationEvent @event);
    }

    /// <summary>
    /// Non-generic base interface for integration event handlers
    /// </summary>
    public interface IIntegrationEventHandler
    {
    }
}
```

### Extension Methods
```csharp
namespace Platform.Shared.IntegrationEvents.Extensions
{
    public static class IntegrationEventsServiceCollectionExtensions
    {
        /// <summary>
        /// Adds integration events services to DI container
        /// </summary>
        /// <param name="services">Service collection</param>
        /// <returns>Service collection for chaining</returns>
        public static IServiceCollection AddIntegrationEventsServices(this IServiceCollection services)
        {
            services.AddScoped<IIntegrationEventPublisher, IntegrationEventPublisher>();
            services.AddSingleton<IIntegrationEventSubscriptionsManager, IntegrationEventSubscriptionsManager>();
            services.AddScoped<IIntegrationEventProcessor, IntegrationEventProcessor>();

            return services;
        }
    }
}
```

## Multi-Product API

### Core Interfaces

```csharp
namespace Platform.Shared.MultiProduct
{
    /// <summary>
    /// Interface for objects that belong to a specific product
    /// </summary>
    public interface IMultiProductObject
    {
        /// <summary>
        /// The product identifier (should be immutable and nullable)
        /// </summary>
        string Product { get; set; }
    }

    /// <summary>
    /// Provides the current product context for the request
    /// </summary>
    public interface IMultiProductRequestContextProvider
    {
        /// <summary>
        /// Gets the current product for the request
        /// </summary>
        /// <returns>Product identifier</returns>
        Task<string> GetProductAsync();
        
        /// <summary>
        /// Sets the current product for the request
        /// </summary>
        /// <param name="product">Product identifier</param>
        void SetProduct(string product);
    }

    /// <summary>
    /// Sets product information on objects
    /// </summary>
    public interface IProductSetter
    {
        /// <summary>
        /// Sets the product on a multi-product object
        /// </summary>
        /// <param name="obj">Object to set product on</param>
        /// <param name="product">Product identifier</param>
        void SetProduct(IMultiProductObject obj, string product);
    }
}
```

### Extension Methods
```csharp
namespace Platform.Shared.MultiProduct.Extensions
{
    public static class MultiProductServiceCollectionExtensions
    {
        /// <summary>
        /// Adds multi-product services to DI container
        /// </summary>
        /// <param name="services">Service collection</param>
        /// <returns>Service collection for chaining</returns>
        public static IServiceCollection AddMultiProductServices(this IServiceCollection services)
        {
            services.AddScoped<IMultiProductRequestContextProvider, MultiProductRequestContextProvider>();
            services.AddScoped<IProductSetter, ProductSetter>();

            return services;
        }
    }
}
```

## Caching API

### In-Memory Caching
```csharp
namespace Platform.Shared.Caching.InMemory
{
    /// <summary>
    /// In-memory cache request handler
    /// </summary>
    public class InMemoryCacheRequestHandler
    {
        public InMemoryCacheRequestHandler(IMemoryCache memoryCache)
        {
            // Constructor implementation
        }

        /// <summary>
        /// Gets or sets cached value
        /// </summary>
        /// <typeparam name="T">Value type</typeparam>
        /// <param name="key">Cache key</param>
        /// <param name="factory">Value factory function</param>
        /// <param name="expiration">Cache expiration time</param>
        /// <returns>Cached value</returns>
        public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null);
    }
}
```

### Redis Caching
```csharp
namespace Platform.Shared.Caching.Redis
{
    /// <summary>
    /// Redis cache repository
    /// </summary>
    public class RedisCacheRepository
    {
        public RedisCacheRepository(IDistributedCache distributedCache)
        {
            // Constructor implementation
        }

        /// <summary>
        /// Gets cached value
        /// </summary>
        /// <typeparam name="T">Value type</typeparam>
        /// <param name="key">Cache key</param>
        /// <returns>Cached value or default</returns>
        public async Task<T?> GetAsync<T>(string key);

        /// <summary>
        /// Sets cached value
        /// </summary>
        /// <typeparam name="T">Value type</typeparam>
        /// <param name="key">Cache key</param>
        /// <param name="value">Value to cache</param>
        /// <param name="expiration">Cache expiration time</param>
        public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null);

        /// <summary>
        /// Removes cached value
        /// </summary>
        /// <param name="key">Cache key</param>
        public async Task RemoveAsync(string key);
    }
}
```

## HTTP API Extensions

### Main Extension
```csharp
namespace Platform.Shared.HttpApi.Extensions
{
    public static class HttpApiServiceCollectionExtensions
    {
        /// <summary>
        /// Adds Platform Common HTTP API services
        /// </summary>
        /// <param name="services">Service collection</param>
        /// <param name="configuration">Configuration</param>
        /// <param name="configBuilder">Configuration builder</param>
        /// <param name="env">Web host environment</param>
        /// <param name="moduleName">Module name</param>
        /// <param name="majorVersion">Major API version</param>
        /// <param name="minorVersion">Minor API version</param>
        /// <returns>Platform HTTP API builder</returns>
        public static PlatformCommonHttpApiBuilder AddPlatformCommonHttpApi(
            this IServiceCollection services,
            IConfiguration configuration,
            IConfigurationBuilder configBuilder,
            IWebHostEnvironment env,
            string moduleName,
            int majorVersion = 1,
            int minorVersion = 0)
        {
            // Implementation includes:
            // - HTTP context accessor
            // - API versioning
            // - Problem details
            // - Authentication (non-development)
            // - Application Insights (non-development)
            // - CORS configuration
            // - JSON serialization options
        }
    }
}
```

### Builder Pattern
```csharp
namespace Platform.Shared.HttpApi.Extensions
{
    /// <summary>
    /// Builder for Platform Common HTTP API configuration
    /// </summary>
    public class PlatformCommonHttpApiBuilder
    {
        public IServiceCollection Services { get; }
        public IConfiguration Configuration { get; }
        public string ModuleName { get; }

        public PlatformCommonHttpApiBuilder(
            IServiceCollection services, 
            IConfiguration configuration, 
            string moduleName)
        {
            Services = services;
            Configuration = configuration;
            ModuleName = moduleName;
        }

        /// <summary>
        /// Adds auditing services
        /// </summary>
        /// <returns>Builder for chaining</returns>
        public PlatformCommonHttpApiBuilder WithAuditing()
        {
            Services.AddPlatformCommonAuditing();
            return this;
        }

        /// <summary>
        /// Adds multi-product services
        /// </summary>
        /// <returns>Builder for chaining</returns>
        public PlatformCommonHttpApiBuilder WithMultiProduct()
        {
            Services.AddMultiProductServices();
            return this;
        }
    }
}
```

## Instrumentation API

### Core Interface
```csharp
namespace Platform.Shared.Instrumentation
{
    /// <summary>
    /// Helper for instrumentation and telemetry
    /// </summary>
    public interface IInstrumentationHelper
    {
        /// <summary>
        /// Get instrumentation properties for an object
        /// </summary>
        /// <param name="instrumentationObject">Object to extract properties from</param>
        /// <returns>Dictionary of properties</returns>
        Dictionary<string, string> GetInstrumentationProperties(IInstrumentationObject instrumentationObject);

        /// <summary>
        /// Saves an event to be published later
        /// </summary>
        /// <param name="eventName">Name of the event</param>
        /// <param name="properties">Event properties</param>
        void SaveInstrumentationEvent(string eventName, Dictionary<string, string> properties);

        /// <summary>
        /// Saves an instrumentation object event to be published later
        /// </summary>
        /// <param name="eventName">Name of the event</param>
        /// <param name="instrumentationObject">Object to instrument</param>
        void SaveInstrumentationObjectEvent(string eventName, IInstrumentationObject instrumentationObject);

        /// <summary>
        /// Publishes saved instrumentation events
        /// </summary>
        void PublishInstrumentationEvents();
    }
}
```

### Instrumentation Object Event
```csharp
namespace Platform.Shared.Instrumentation
{
    /// <summary>
    /// Event containing instrumentation object data
    /// </summary>
    public class InstrumentationObjectEvent
    {
        public string EventName { get; set; }
        public Dictionary<string, string> Properties { get; set; }

        public InstrumentationObjectEvent(string eventName, Dictionary<string, string> properties)
        {
            EventName = eventName;
            Properties = properties;
        }
    }
}
```

## Entity Framework Extensions

### Platform DbContext
```csharp
namespace Platform.Shared.EntityFrameworkCore
{
    using Microsoft.Extensions.Logging;
    
    /// <summary>
    /// Base database context with platform features
    /// </summary>
    public class PlatformDbContext : DbContext
    {
        protected readonly IAuditPropertySetter? _auditPropertySetter;
        protected readonly IDataFilter? _dataFilter;
        protected readonly ILogger? _logger;

        public PlatformDbContext(DbContextOptions options, ILogger logger) : base(options)
        {
            _logger = logger;
        }

        public PlatformDbContext(
            DbContextOptions options,
            ILogger logger,
            IAuditPropertySetter auditPropertySetter,
            IDataFilter dataFilter) : base(options)
        {
            _logger = logger;
            _auditPropertySetter = auditPropertySetter;
            _dataFilter = dataFilter;
        }

        /// <summary>
        /// Applies audit properties and saves changes
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Number of affected entities</returns>
        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            ApplyAuditProperties();
            return await base.SaveChangesAsync(cancellationToken);
        }

        /// <summary>
        /// Applies audit properties to tracked entities
        /// </summary>
        protected virtual void ApplyAuditProperties()
        {
            if (_auditPropertySetter == null) return;

            var entries = ChangeTracker.Entries().ToList();

            foreach (var entry in entries)
            {
                switch (entry.State)
                {
                    case EntityState.Added:
                        if (entry.Entity is ICreateAuditedEntity createAudited)
                            _auditPropertySetter.SetCreateAuditedProperties(createAudited);
                        break;

                    case EntityState.Modified:
                        if (entry.Entity is IUpdateAuditedEntity updateAudited)
                            _auditPropertySetter.SetUpdateAuditedProperties(updateAudited);
                        break;

                    case EntityState.Deleted:
                        if (entry.Entity is IDeleteAuditedEntity deleteAudited)
                        {
                            _auditPropertySetter.SetDeleteAuditedProperties(deleteAudited);
                            entry.State = EntityState.Modified; // Soft delete
                        }
                        break;
                }
            }
        }
    }
}
```

### Model Builder Extensions
```csharp
namespace Platform.Shared.EntityFrameworkCore.Extensions
{
    /// <summary>
    /// Extensions for configuring platform entities
    /// </summary>
    public static class PlatformModelBuilderExtensions
    {
        /// <summary>
        /// Configures platform entity conventions
        /// </summary>
        /// <param name="modelBuilder">Model builder</param>
        /// <returns>Model builder for chaining</returns>
        public static ModelBuilder ConfigurePlatformEntities(this ModelBuilder modelBuilder)
        {
            // Configure audit properties
            ConfigureAuditProperties(modelBuilder);
            
            // Configure multi-product properties
            ConfigureMultiProductProperties(modelBuilder);
            
            // Configure soft delete filters
            ConfigureSoftDeleteFilters(modelBuilder);

            return modelBuilder;
        }

        private static void ConfigureAuditProperties(ModelBuilder modelBuilder)
        {
            // Implementation for configuring audit properties
        }

        private static void ConfigureMultiProductProperties(ModelBuilder modelBuilder)
        {
            // Implementation for configuring multi-product properties
        }

        private static void ConfigureSoftDeleteFilters(ModelBuilder modelBuilder)
        {
            // Implementation for configuring soft delete global query filters
        }
    }
}
```

### Page Result
```csharp
namespace Platform.Shared.EntityFrameworkCore
{
    /// <summary>
    /// Result of a paged query
    /// </summary>
    /// <typeparam name="T">Entity type</typeparam>
    public class PageResult<T>
    {
        /// <summary>
        /// Items in the current page
        /// </summary>
        public IEnumerable<T> Items { get; set; }

        /// <summary>
        /// Current page number (1-based)
        /// </summary>
        public int PageNumber { get; set; }

        /// <summary>
        /// Number of items per page
        /// </summary>
        public int PageSize { get; set; }

        /// <summary>
        /// Total number of items across all pages
        /// </summary>
        public long TotalItems { get; set; }

        /// <summary>
        /// Total number of pages
        /// </summary>
        public int TotalPages => (int)Math.Ceiling((double)TotalItems / PageSize);

        /// <summary>
        /// Whether there is a previous page
        /// </summary>
        public bool HasPrevious => PageNumber > 1;

        /// <summary>
        /// Whether there is a next page
        /// </summary>
        public bool HasNext => PageNumber < TotalPages;

        public PageResult(IEnumerable<T> items, int pageNumber, int pageSize, long totalItems)
        {
            Items = items;
            PageNumber = pageNumber;
            PageSize = pageSize;
            TotalItems = totalItems;
        }
    }
}
```

## Exception Types

### Business Exception
```csharp
namespace Platform.Shared.Exceptions
{
    /// <summary>
    /// Exception for business logic violations
    /// </summary>
    public class BusinessException : Exception
    {
        /// <summary>
        /// Business error code
        /// </summary>
        public string? Code { get; }

        /// <summary>
        /// Additional details about the error
        /// </summary>
        public object? Details { get; }

        public BusinessException(string message) : base(message)
        {
        }

        public BusinessException(string message, Exception innerException) : base(message, innerException)
        {
        }

        public BusinessException(string code, string message) : base(message)
        {
            Code = code;
        }

        public BusinessException(string code, string message, object details) : base(message)
        {
            Code = code;
            Details = details;
        }

        public BusinessException(string code, string message, Exception innerException) : base(message, innerException)
        {
            Code = code;
        }
    }
}
```

### Missing Configuration Exception
```csharp
namespace Platform.Shared.Exceptions
{
    /// <summary>
    /// Exception for missing configuration values
    /// </summary>
    public class MissingConfigException : Exception
    {
        /// <summary>
        /// Name of the missing configuration key
        /// </summary>
        public string ConfigKey { get; }

        public MissingConfigException(string configKey) 
            : base($"Configuration key '{configKey}' is missing or has no value.")
        {
            ConfigKey = configKey;
        }

        public MissingConfigException(string configKey, string message) : base(message)
        {
            ConfigKey = configKey;
        }

        public MissingConfigException(string configKey, string message, Exception innerException) 
            : base(message, innerException)
        {
            ConfigKey = configKey;
        }
    }
}
```

---

*This API reference guide provides detailed interface definitions and implementation guidelines for the Platform.Shared library. For usage examples and architectural guidance, refer to the main documentation.*