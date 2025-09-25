using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Platform.Locations.Domain.Locations;
using Platform.Locations.Infrastructure.Configurations;
using Platform.Shared.Auditing;
using Platform.Shared.DataLayer;
using Platform.Shared.Ddd.Domain.Entities;
using Platform.Shared.EntityFrameworkCore;
using Platform.Shared.MultiProduct;

namespace Platform.Locations.Infrastructure.Data;

public class LocationsDbContext : PlatformDbContext
{
    public DbSet<Location> Locations { get; set; } = null!;


    public IMultiProductRequestContextProvider _requestContextProvider { get; private set; }
    private readonly IDataFilter<IActivable> _activableDataFilter;
    private readonly IDataFilter<IMultiProductObject> _multiProductObjectDataFilter;

    public LocationsDbContext(DbContextOptions<LocationsDbContext> options,
         IAuditPropertySetter auditPropertySetter,
            IProductSetter productSetter,
            ILogger<PlatformDbContext> logger,
            IMediator mediator,
            IMultiProductRequestContextProvider requestContextProvider,
            IDataFilter<IActivable> activableDataFilter,
            IDataFilter<IMultiProductObject> multiProductObjectDataFilter)
            : base(options, logger, auditPropertySetter, productSetter)
    {
        _requestContextProvider = requestContextProvider;
        _activableDataFilter = activableDataFilter;
        _multiProductObjectDataFilter = multiProductObjectDataFilter;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Configure Platform.Shared entities
        // modelBuilder.ConfigurePlatformEntities(); // Method may have changed in Platform.Shared
        
        // Configure location-specific entities
        modelBuilder.ApplyConfiguration(new LocationConfiguration(_requestContextProvider, _activableDataFilter, _multiProductObjectDataFilter));
    }
}