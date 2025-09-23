using Microsoft.EntityFrameworkCore;
using Platform.Locations.Domain.Locations;
using Platform.Locations.SqlServer.Configurations;
using Platform.Shared.EntityFrameworkCore;

namespace Platform.Locations.SqlServer.Data;

public class LocationsDbContext : PlatformDbContext
{
    public DbSet<Location> Locations { get; set; } = null!;

    public LocationsDbContext(DbContextOptions<LocationsDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Configure Platform.Shared entities
        modelBuilder.ConfigurePlatformEntities();
        
        // Configure location-specific entities
        modelBuilder.ApplyConfiguration(new LocationConfiguration());
    }
}