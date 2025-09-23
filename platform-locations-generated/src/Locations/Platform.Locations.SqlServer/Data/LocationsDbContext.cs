using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Platform.Locations.Domain.Locations;
using Platform.Locations.SqlServer.Configurations;
using Platform.Shared.EntityFrameworkCore;

namespace Platform.Locations.SqlServer.Data;

public class LocationsDbContext : PlatformDbContext
{
    public DbSet<Location> Locations { get; set; } = null!;

    public LocationsDbContext(DbContextOptions<LocationsDbContext> options, ILogger<LocationsDbContext> logger) : base(options, logger)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Configure Platform.Shared entities
        // modelBuilder.ConfigurePlatformEntities(); // Method may have changed in Platform.Shared
        
        // Configure location-specific entities
        modelBuilder.ApplyConfiguration(new LocationConfiguration());
    }
}