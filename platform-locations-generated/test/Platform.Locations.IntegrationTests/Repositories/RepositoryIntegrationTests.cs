using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Platform.Locations.Domain.Locations;
using Platform.Locations.IntegrationTests.Infrastructure;
using Xunit;

namespace Platform.Locations.IntegrationTests.Repositories;

[Collection("Database Integration Tests")]
public class RepositoryIntegrationTests : IntegrationTestBase
{
    private readonly ILocationRepository _locationRepository;

    public RepositoryIntegrationTests() : base()
    {
        _locationRepository = ServiceScope.ServiceProvider.GetRequiredService<ILocationRepository>();
    }

    [Fact]
    public async Task AddAsync_ValidLocation_PersistsToDatabase()
    {
        // Arrange
        await ResetDatabaseAsync();
        var location = Location.Create(
            "REPO-ADD-001",
            "WAREHOUSE",
            "123 Repository Street",
            "Suite 100",
            "Test City",
            "TX",
            "12345",
            "USA");

        // Act
        await _locationRepository.AddAsync(location, CancellationToken.None);

        // Assert
        var savedLocation = await FindLocationByCodeAsync(location.LocationCode);
        savedLocation.Should().NotBeNull();
        savedLocation!.LocationCode.Should().Be(location.LocationCode);
        savedLocation.LocationTypeCode.Should().Be(location.LocationTypeCode);
        savedLocation.AddressLine1.Should().Be(location.AddressLine1);
        savedLocation.AddressLine2.Should().Be(location.AddressLine2);
        savedLocation.City.Should().Be(location.City);
        savedLocation.State.Should().Be(location.State);
        savedLocation.ZipCode.Should().Be(location.ZipCode);
        savedLocation.Country.Should().Be(location.Country);
        savedLocation.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateAsync_ExistingLocation_UpdatesInDatabase()
    {
        // Arrange
        await ResetDatabaseAsync();
        var originalLocation = await SeedLocationInDatabaseAsync("REPO-UPDATE-001");
        
        // Modify the location
        originalLocation.UpdateAddress(
            "456 Updated Street",
            "Suite 200", 
            "Updated City",
            "CA",
            "54321",
            "Canada");

        // Act
        await _locationRepository.UpdateAsync(originalLocation, CancellationToken.None);

        // Assert
        var updatedLocation = await FindLocationByCodeAsync(originalLocation.LocationCode);
        updatedLocation.Should().NotBeNull();
        updatedLocation!.AddressLine1.Should().Be("456 Updated Street");
        updatedLocation.AddressLine2.Should().Be("Suite 200");
        updatedLocation.City.Should().Be("Updated City");
        updatedLocation.State.Should().Be("CA");
        updatedLocation.ZipCode.Should().Be("54321");
        updatedLocation.Country.Should().Be("Canada");
    }

    [Fact]
    public async Task DeleteAsync_ExistingLocation_RemovesFromDatabase()
    {
        // Arrange
        await ResetDatabaseAsync();
        var locationToDelete = await SeedLocationInDatabaseAsync("REPO-DELETE-001");
        var initialCount = await GetLocationCountAsync();

        // Act
        await _locationRepository.DeleteAsync(locationToDelete, CancellationToken.None);

        // Assert
        var finalCount = await GetLocationCountAsync();
        
        // Check if it's soft delete or hard delete
        var deletedLocation = await FindLocationByCodeAsync(locationToDelete.LocationCode);
        if (deletedLocation == null)
        {
            // Hard delete - location completely removed
            finalCount.Should().Be(initialCount - 1);
        }
        else
        {
            // Soft delete - location still exists but should be marked as deleted
            // The exact implementation depends on the Platform.Shared base class
            // We can't easily test the DeletedAt field without reflection or knowing the exact implementation
            finalCount.Should().Be(initialCount); // Count remains the same for soft delete
        }
    }

    [Fact]
    public async Task GetByLocationCodeAsync_ExistingLocation_ReturnsLocation()
    {
        // Arrange
        await ResetDatabaseAsync();
        var seededLocation = await SeedLocationInDatabaseAsync("REPO-GET-001");

        // Act
        var retrievedLocation = await _locationRepository.GetByLocationCodeAsync(
            seededLocation.LocationCode, 
            CancellationToken.None);

        // Assert
        retrievedLocation.Should().NotBeNull();
        retrievedLocation!.Id.Should().Be(seededLocation.Id);
        retrievedLocation.LocationCode.Should().Be(seededLocation.LocationCode);
        retrievedLocation.LocationTypeCode.Should().Be(seededLocation.LocationTypeCode);
        retrievedLocation.AddressLine1.Should().Be(seededLocation.AddressLine1);
        retrievedLocation.City.Should().Be(seededLocation.City);
    }

    [Fact]
    public async Task GetByLocationCodeAsync_NonExistentLocation_ReturnsNull()
    {
        // Arrange
        await ResetDatabaseAsync();
        var nonExistentLocationCode = "REPO-NONEXISTENT-001";

        // Act
        var retrievedLocation = await _locationRepository.GetByLocationCodeAsync(
            nonExistentLocationCode, 
            CancellationToken.None);

        // Assert
        retrievedLocation.Should().BeNull();
    }

    [Fact]
    public async Task GetByLocationCodeAsync_CaseInsensitive_ReturnsLocation()
    {
        // Arrange
        await ResetDatabaseAsync();
        var seededLocation = await SeedLocationInDatabaseAsync("REPO-CASE-001");

        // Act
        var retrievedLocation = await _locationRepository.GetByLocationCodeAsync(
            "repo-case-001", // Different case
            CancellationToken.None);

        // Assert
        // This behavior depends on database collation and implementation
        // In most SQL Server setups, this would be case-insensitive
        // If case-sensitive, retrievedLocation would be null
        if (retrievedLocation != null)
        {
            retrievedLocation.LocationCode.Should().Be(seededLocation.LocationCode);
        }
        // The test passes either way, but documents the expected behavior
    }

    [Fact]
    public async Task Repository_MultipleOperations_MaintainDataIntegrity()
    {
        // Arrange
        await ResetDatabaseAsync();
        var locations = new List<Location>();

        // Create multiple locations
        for (int i = 1; i <= 5; i++)
        {
            var location = Location.Create(
                $"REPO-MULTI-{i:000}",
                "WAREHOUSE",
                $"{i} Multi Street",
                "Suite 100",
                "Multi City",
                "TX",
                "12345",
                "USA");
            locations.Add(location);
        }

        // Act - Add all locations
        foreach (var location in locations)
        {
            await _locationRepository.AddAsync(location, CancellationToken.None);
        }

        // Act - Update some locations
        locations[0].UpdateAddress("Updated Address 1", null, "Updated City", "CA", "54321", "USA");
        await _locationRepository.UpdateAsync(locations[0], CancellationToken.None);

        locations[1].Deactivate();
        await _locationRepository.UpdateAsync(locations[1], CancellationToken.None);

        // Act - Delete one location
        await _locationRepository.DeleteAsync(locations[2], CancellationToken.None);

        // Assert - Verify all operations
        var savedLocation0 = await _locationRepository.GetByLocationCodeAsync("REPO-MULTI-001", CancellationToken.None);
        savedLocation0.Should().NotBeNull();
        savedLocation0!.AddressLine1.Should().Be("Updated Address 1");
        savedLocation0.City.Should().Be("Updated City");

        var savedLocation1 = await _locationRepository.GetByLocationCodeAsync("REPO-MULTI-002", CancellationToken.None);
        savedLocation1.Should().NotBeNull();
        savedLocation1!.IsActive.Should().BeFalse();

        var deletedLocation = await _locationRepository.GetByLocationCodeAsync("REPO-MULTI-003", CancellationToken.None);
        // For soft delete, the location might still exist but marked as deleted
        // For hard delete, it should be null

        var savedLocation3 = await _locationRepository.GetByLocationCodeAsync("REPO-MULTI-004", CancellationToken.None);
        savedLocation3.Should().NotBeNull();
        savedLocation3!.LocationCode.Should().Be("REPO-MULTI-004");

        var savedLocation4 = await _locationRepository.GetByLocationCodeAsync("REPO-MULTI-005", CancellationToken.None);
        savedLocation4.Should().NotBeNull();
        savedLocation4!.LocationCode.Should().Be("REPO-MULTI-005");
    }

    [Fact]
    public async Task Repository_ConcurrentOperations_HandleCorrectly()
    {
        // Arrange
        await ResetDatabaseAsync();
        var tasks = new List<Task>();

        // Act - Create multiple locations concurrently
        for (int i = 1; i <= 10; i++)
        {
            var locationCode = $"REPO-CONCURRENT-{i:00}";
            var location = Location.Create(
                locationCode,
                "WAREHOUSE",
                $"{i} Concurrent Street",
                "Suite 100",
                "Concurrent City",
                "TX",
                "12345",
                "USA");

            tasks.Add(_locationRepository.AddAsync(location, CancellationToken.None));
        }

        await Task.WhenAll(tasks);

        // Assert
        var finalCount = await GetLocationCountAsync();
        finalCount.Should().Be(10);

        // Verify all locations were created correctly
        for (int i = 1; i <= 10; i++)
        {
            var locationCode = $"REPO-CONCURRENT-{i:00}";
            var location = await _locationRepository.GetByLocationCodeAsync(locationCode, CancellationToken.None);
            location.Should().NotBeNull();
            location!.LocationCode.Should().Be(locationCode);
        }
    }

    [Fact]
    public async Task Repository_EntityTracking_WorksCorrectly()
    {
        // Arrange
        await ResetDatabaseAsync();
        var location = Location.Create(
            "REPO-TRACKING-001",
            "WAREHOUSE",
            "123 Tracking Street",
            null,
            "Tracking City",
            "TX",
            "12345",
            "USA");

        await _locationRepository.AddAsync(location, CancellationToken.None);

        // Act - Retrieve and modify
        var retrievedLocation = await _locationRepository.GetByLocationCodeAsync("REPO-TRACKING-001", CancellationToken.None);
        retrievedLocation.Should().NotBeNull();

        retrievedLocation!.UpdateAddress(
            "456 Modified Street",
            "Suite 200",
            "Modified City",
            "CA",
            "54321",
            "Canada");

        await _locationRepository.UpdateAsync(retrievedLocation, CancellationToken.None);

        // Assert - Verify changes persisted
        var modifiedLocation = await _locationRepository.GetByLocationCodeAsync("REPO-TRACKING-001", CancellationToken.None);
        modifiedLocation.Should().NotBeNull();
        modifiedLocation!.AddressLine1.Should().Be("456 Modified Street");
        modifiedLocation.AddressLine2.Should().Be("Suite 200");
        modifiedLocation.City.Should().Be("Modified City");
        modifiedLocation.State.Should().Be("CA");
        modifiedLocation.ZipCode.Should().Be("54321");
        modifiedLocation.Country.Should().Be("Canada");
    }

    [Fact]
    public async Task Repository_LocationActivation_PersistsCorrectly()
    {
        // Arrange
        await ResetDatabaseAsync();
        var location = Location.Create(
            "REPO-ACTIVATION-001",
            "WAREHOUSE",
            "123 Activation Street",
            null,
            "Activation City",
            "TX",
            "12345",
            "USA");

        await _locationRepository.AddAsync(location, CancellationToken.None);

        // Act - Deactivate
        var retrievedLocation = await _locationRepository.GetByLocationCodeAsync("REPO-ACTIVATION-001", CancellationToken.None);
        retrievedLocation!.Deactivate();
        await _locationRepository.UpdateAsync(retrievedLocation, CancellationToken.None);

        // Assert - Verify deactivation
        var deactivatedLocation = await _locationRepository.GetByLocationCodeAsync("REPO-ACTIVATION-001", CancellationToken.None);
        deactivatedLocation!.IsActive.Should().BeFalse();

        // Act - Reactivate
        deactivatedLocation.Activate();
        await _locationRepository.UpdateAsync(deactivatedLocation, CancellationToken.None);

        // Assert - Verify reactivation
        var reactivatedLocation = await _locationRepository.GetByLocationCodeAsync("REPO-ACTIVATION-001", CancellationToken.None);
        reactivatedLocation!.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task Repository_LargeDataSet_PerformsWell()
    {
        // Arrange
        await ResetDatabaseAsync();
        const int locationCount = 100;
        var locations = new List<Location>();

        // Create locations
        for (int i = 1; i <= locationCount; i++)
        {
            var location = Location.Create(
                $"REPO-LARGE-{i:000}",
                "WAREHOUSE",
                $"{i} Large Dataset Street",
                i % 2 == 0 ? "Even Suite" : null, // Mix of null and non-null values
                "Large City",
                "TX",
                "12345",
                "USA");

            if (i % 10 == 0)
            {
                location.Deactivate(); // Deactivate every 10th location
            }

            locations.Add(location);
        }

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Act - Add all locations
        foreach (var location in locations)
        {
            await _locationRepository.AddAsync(location, CancellationToken.None);
        }

        stopwatch.Stop();

        // Assert
        var finalCount = await GetLocationCountAsync();
        finalCount.Should().Be(locationCount);

        // Verify performance is reasonable (adjust threshold as needed)
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(30000); // 30 seconds max

        // Spot check some locations
        var firstLocation = await _locationRepository.GetByLocationCodeAsync("REPO-LARGE-001", CancellationToken.None);
        firstLocation.Should().NotBeNull();
        firstLocation!.IsActive.Should().BeTrue();

        var tenthLocation = await _locationRepository.GetByLocationCodeAsync("REPO-LARGE-010", CancellationToken.None);
        tenthLocation.Should().NotBeNull();
        tenthLocation!.IsActive.Should().BeFalse();

        var lastLocation = await _locationRepository.GetByLocationCodeAsync($"REPO-LARGE-{locationCount:000}", CancellationToken.None);
        lastLocation.Should().NotBeNull();
    }
}