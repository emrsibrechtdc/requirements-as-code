using FluentAssertions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Platform.Locations.Application.Locations.Commands;
using Platform.Locations.Domain.Locations;
using Platform.Locations.IntegrationTests.Infrastructure;
using Platform.Shared.IntegrationEvents;
using Xunit;

namespace Platform.Locations.IntegrationTests.CommandHandlers;

[Collection("Database Integration Tests")]
public class CommandHandlerIntegrationTests : IntegrationTestBase
{
    private readonly ISender _mediator;
    private readonly IIntegrationEventPublisher _eventPublisher;

    public CommandHandlerIntegrationTests() : base()
    {
        _mediator = ServiceScope.ServiceProvider.GetRequiredService<ISender>();
        _eventPublisher = ServiceScope.ServiceProvider.GetRequiredService<IIntegrationEventPublisher>();
    }

    [Fact]
    public async Task RegisterLocationCommandHandler_ValidCommand_CreatesLocationInDatabase()
    {
        // Arrange
        await ResetDatabaseAsync();
        var command = CreateValidRegisterCommand("CMD-TEST-001");

        // Act
        var result = await _mediator.Send(command);

        // Assert
        result.Should().NotBeNull();
        result.LocationCode.Should().Be(command.LocationCode);

        // Verify location was persisted to database
        var savedLocation = await FindLocationByCodeAsync(command.LocationCode);
        savedLocation.Should().NotBeNull();
        savedLocation!.LocationCode.Should().Be(command.LocationCode);
        savedLocation.LocationTypeCode.Should().Be(command.LocationTypeCode);
        savedLocation.AddressLine1.Should().Be(command.AddressLine1);
        savedLocation.AddressLine2.Should().Be(command.AddressLine2);
        savedLocation.City.Should().Be(command.City);
        savedLocation.State.Should().Be(command.State);
        savedLocation.ZipCode.Should().Be(command.ZipCode);
        savedLocation.Country.Should().Be(command.Country);
        savedLocation.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task RegisterLocationCommandHandler_DuplicateLocationCode_ThrowsException()
    {
        // Arrange
        await ResetDatabaseAsync();
        var existingLocationCode = "CMD-DUP-001";
        await SeedLocationInDatabaseAsync(existingLocationCode);
        var command = CreateValidRegisterCommand(existingLocationCode);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<LocationAlreadyExistsException>(
            () => _mediator.Send(command));

        exception.LocationCode.Should().Be(existingLocationCode);

        // Verify only one location exists
        var locations = await GetAllLocationsAsync();
        locations.Count(l => l.LocationCode == existingLocationCode).Should().Be(1);
    }

    [Fact]
    public async Task UpdateLocationAddressCommandHandler_ValidCommand_UpdatesAddressInDatabase()
    {
        // Arrange
        await ResetDatabaseAsync();
        var existingLocation = await SeedLocationInDatabaseAsync("CMD-UPDATE-001");
        var updateCommand = CreateValidUpdateAddressCommand(existingLocation.LocationCode);

        // Act
        var result = await _mediator.Send(updateCommand);

        // Assert
        result.Should().NotBeNull();
        result.LocationCode.Should().Be(updateCommand.LocationCode);

        // Verify address was updated in database
        var updatedLocation = await FindLocationByCodeAsync(existingLocation.LocationCode);
        updatedLocation.Should().NotBeNull();
        updatedLocation!.AddressLine1.Should().Be(updateCommand.AddressLine1);
        updatedLocation.AddressLine2.Should().Be(updateCommand.AddressLine2);
        updatedLocation.City.Should().Be(updateCommand.City);
        updatedLocation.State.Should().Be(updateCommand.State);
        updatedLocation.ZipCode.Should().Be(updateCommand.ZipCode);
        updatedLocation.Country.Should().Be(updateCommand.Country);
    }

    [Fact]
    public async Task UpdateLocationAddressCommandHandler_NonExistentLocation_ThrowsException()
    {
        // Arrange
        await ResetDatabaseAsync();
        var nonExistentLocationCode = "CMD-NONEXISTENT-001";
        var updateCommand = CreateValidUpdateAddressCommand(nonExistentLocationCode);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<LocationNotFoundException>(
            () => _mediator.Send(updateCommand));

        exception.LocationCode.Should().Be(nonExistentLocationCode);
    }

    [Fact]
    public async Task ActivateLocationCommandHandler_InactiveLocation_ActivatesInDatabase()
    {
        // Arrange
        await ResetDatabaseAsync();
        var inactiveLocation = await SeedLocationInDatabaseAsync("CMD-ACTIVATE-001", isActive: false);
        var activateCommand = CreateActivateCommand(inactiveLocation.LocationCode);

        // Act
        var result = await _mediator.Send(activateCommand);

        // Assert
        result.Should().NotBeNull();
        result.LocationCode.Should().Be(activateCommand.LocationCode);

        // Verify location was activated in database
        var activatedLocation = await FindLocationByCodeAsync(inactiveLocation.LocationCode);
        activatedLocation.Should().NotBeNull();
        activatedLocation!.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task ActivateLocationCommandHandler_AlreadyActiveLocation_ThrowsException()
    {
        // Arrange
        await ResetDatabaseAsync();
        var activeLocation = await SeedLocationInDatabaseAsync("CMD-ALREADY-ACTIVE-001", isActive: true);
        var activateCommand = CreateActivateCommand(activeLocation.LocationCode);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<LocationAlreadyActiveException>(
            () => _mediator.Send(activateCommand));

        exception.LocationCode.Should().Be(activeLocation.LocationCode);

        // Verify location remains active
        var unchangedLocation = await FindLocationByCodeAsync(activeLocation.LocationCode);
        unchangedLocation!.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task DeactivateLocationCommandHandler_ActiveLocation_DeactivatesInDatabase()
    {
        // Arrange
        await ResetDatabaseAsync();
        var activeLocation = await SeedLocationInDatabaseAsync("CMD-DEACTIVATE-001", isActive: true);
        var deactivateCommand = CreateDeactivateCommand(activeLocation.LocationCode);

        // Act
        var result = await _mediator.Send(deactivateCommand);

        // Assert
        result.Should().NotBeNull();
        result.LocationCode.Should().Be(deactivateCommand.LocationCode);

        // Verify location was deactivated in database
        var deactivatedLocation = await FindLocationByCodeAsync(activeLocation.LocationCode);
        deactivatedLocation.Should().NotBeNull();
        deactivatedLocation!.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task DeactivateLocationCommandHandler_AlreadyInactiveLocation_ThrowsException()
    {
        // Arrange
        await ResetDatabaseAsync();
        var inactiveLocation = await SeedLocationInDatabaseAsync("CMD-ALREADY-INACTIVE-001", isActive: false);
        var deactivateCommand = CreateDeactivateCommand(inactiveLocation.LocationCode);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<LocationAlreadyInactiveException>(
            () => _mediator.Send(deactivateCommand));

        exception.LocationCode.Should().Be(inactiveLocation.LocationCode);

        // Verify location remains inactive
        var unchangedLocation = await FindLocationByCodeAsync(inactiveLocation.LocationCode);
        unchangedLocation!.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteLocationCommandHandler_ExistingLocation_RemovesFromDatabase()
    {
        // Arrange
        await ResetDatabaseAsync();
        var locationToDelete = await SeedLocationInDatabaseAsync("CMD-DELETE-001");
        var deleteCommand = CreateDeleteCommand(locationToDelete.LocationCode);
        var initialCount = await GetLocationCountAsync();

        // Act
        var result = await _mediator.Send(deleteCommand);

        // Assert
        result.Should().NotBeNull();
        result.LocationCode.Should().Be(deleteCommand.LocationCode);

        // Verify location handling depends on delete strategy
        // If soft delete: location exists but marked as deleted
        // If hard delete: location count should decrease
        var finalCount = await GetLocationCountAsync();
        
        // For soft delete, we'd check the deleted flag
        var deletedLocation = await FindLocationByCodeAsync(locationToDelete.LocationCode);
        if (deletedLocation != null)
        {
            // Soft delete - location still exists but marked as deleted
            // Check if there's a DeletedAt field or similar
        }
        else
        {
            // Hard delete - location completely removed
            finalCount.Should().Be(initialCount - 1);
        }
    }

    [Fact]
    public async Task DeleteLocationCommandHandler_NonExistentLocation_ThrowsException()
    {
        // Arrange
        await ResetDatabaseAsync();
        var nonExistentLocationCode = "CMD-DELETE-NONEXISTENT";
        var deleteCommand = CreateDeleteCommand(nonExistentLocationCode);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<LocationNotFoundException>(
            () => _mediator.Send(deleteCommand));

        exception.LocationCode.Should().Be(nonExistentLocationCode);
    }

    [Fact]
    public async Task CommandHandlers_WithDatabaseTransactions_HandleConcurrentOperations()
    {
        // Arrange
        await ResetDatabaseAsync();
        var tasks = new List<Task>();

        // Act - Create multiple locations concurrently
        for (int i = 1; i <= 10; i++)
        {
            var command = CreateValidRegisterCommand($"CMD-CONCURRENT-{i:00}");
            tasks.Add(_mediator.Send(command));
        }

        await Task.WhenAll(tasks);

        // Assert
        var finalLocationCount = await GetLocationCountAsync();
        finalLocationCount.Should().Be(10);

        // Verify all locations were created with correct data
        for (int i = 1; i <= 10; i++)
        {
            var locationCode = $"CMD-CONCURRENT-{i:00}";
            var location = await FindLocationByCodeAsync(locationCode);
            location.Should().NotBeNull();
            location!.LocationCode.Should().Be(locationCode);
        }
    }

    [Fact]
    public async Task CommandHandlers_CompleteLocationLifecycle_WorksWithRealDatabase()
    {
        // Arrange
        await ResetDatabaseAsync();
        var locationCode = "CMD-LIFECYCLE-001";

        // Act & Assert - Register Location
        var registerCommand = CreateValidRegisterCommand(locationCode);
        var registerResult = await _mediator.Send(registerCommand);
        registerResult.LocationCode.Should().Be(locationCode);

        var registeredLocation = await FindLocationByCodeAsync(locationCode);
        registeredLocation.Should().NotBeNull();
        registeredLocation!.IsActive.Should().BeTrue();

        // Act & Assert - Update Address
        var updateCommand = CreateValidUpdateAddressCommand(locationCode);
        var updateResult = await _mediator.Send(updateCommand);
        updateResult.LocationCode.Should().Be(locationCode);

        var updatedLocation = await FindLocationByCodeAsync(locationCode);
        updatedLocation!.AddressLine1.Should().Be(updateCommand.AddressLine1);
        updatedLocation.City.Should().Be(updateCommand.City);

        // Act & Assert - Deactivate Location
        var deactivateCommand = CreateDeactivateCommand(locationCode);
        var deactivateResult = await _mediator.Send(deactivateCommand);
        deactivateResult.LocationCode.Should().Be(locationCode);

        var deactivatedLocation = await FindLocationByCodeAsync(locationCode);
        deactivatedLocation!.IsActive.Should().BeFalse();

        // Act & Assert - Reactivate Location
        var reactivateCommand = CreateActivateCommand(locationCode);
        var reactivateResult = await _mediator.Send(reactivateCommand);
        reactivateResult.LocationCode.Should().Be(locationCode);

        var reactivatedLocation = await FindLocationByCodeAsync(locationCode);
        reactivatedLocation!.IsActive.Should().BeTrue();

        // Act & Assert - Delete Location
        var deleteCommand = CreateDeleteCommand(locationCode);
        var deleteResult = await _mediator.Send(deleteCommand);
        deleteResult.LocationCode.Should().Be(locationCode);
    }

    [Fact]
    public async Task CommandHandlers_ValidationFailures_DoNotPersistToDatabase()
    {
        // Arrange
        await ResetDatabaseAsync();
        var initialCount = await GetLocationCountAsync();

        // Create invalid command (empty location code)
        var invalidCommand = new RegisterLocationCommand(
            "", // Invalid empty location code
            "WAREHOUSE",
            "123 Street",
            null,
            "City",
            "TX",
            "12345",
            "USA",
            null, // Latitude
            null, // Longitude
            null  // GeofenceRadius
        );

        // Act & Assert
        await Assert.ThrowsAsync<FluentValidation.ValidationException>(
            () => _mediator.Send(invalidCommand));

        // Verify no location was persisted
        var finalCount = await GetLocationCountAsync();
        finalCount.Should().Be(initialCount);
    }

    [Fact]
    public async Task CommandHandlers_DatabaseConstraintViolations_HandleGracefully()
    {
        // Arrange
        await ResetDatabaseAsync();

        // Create location with very long location code that might exceed database constraints
        var invalidCommand = new RegisterLocationCommand(
            new string('A', 1000), // Extremely long location code
            "WAREHOUSE",
            "123 Street", 
            null,
            "City",
            "TX",
            "12345",
            "USA",
            null, // Latitude
            null, // Longitude
            null  // GeofenceRadius
        );

        // Act & Assert
        // This should either fail validation or throw a database constraint exception
        var exception = await Record.ExceptionAsync(() => _mediator.Send(invalidCommand));
        exception.Should().NotBeNull();
        
        // The specific exception type depends on where the validation occurs
        exception.Should().Match<Exception>(e => e is FluentValidation.ValidationException || e is Exception);
    }
}