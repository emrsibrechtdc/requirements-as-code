using Platform.Locations.Application.Locations.Commands;
using Platform.Locations.Application.Locations.Dtos;
using Platform.Locations.Application.Tests.Utilities;
using Platform.Locations.Domain.Locations;
using FluentValidation;

namespace Platform.Locations.Application.Tests.Locations.Commands;

public class UpdateLocationAddressCommandHandlerTests : TestFixtureBase
{
    private readonly UpdateLocationAddressCommandHandler _handler;

    public UpdateLocationAddressCommandHandlerTests()
    {
        // Use real validator for proper validation  
        var validator = new Platform.Locations.Application.Locations.Validators.UpdateLocationAddressCommandValidator();
        _handler = new UpdateLocationAddressCommandHandler(
            MockLocationRepository.Object,
            validator,
            Mapper,
            MockEventPublisher.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_UpdatesLocationAddressSuccessfully()
    {
        // Arrange
        var existingLocation = CreateTestLocation("LOC-001", addressLine1: "Old Address", city: "Old City");
        var command = CreateValidUpdateAddressCommand("LOC-001", addressLine1: "New Address", city: "New City");
        var cancellationToken = CancellationToken.None;

        // No validator setup needed - real validator will validate
        MockLocationRepository.Setup(x => x.GetByLocationCodeAsync(command.LocationCode, cancellationToken))
            .ReturnsAsync(existingLocation);

        // Act
        var result = await _handler.Handle(command, cancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.LocationCode.Should().Be(command.LocationCode);

        VerifyRepositoryUpdateCalledOnce();
        VerifyEventPublisherCalledOnce();
    }

    [Fact]
    public async Task Handle_ValidationFails_ThrowsValidationException()
    {
        // Arrange
        var command = CreateValidUpdateAddressCommand();
        var cancellationToken = CancellationToken.None;
        // Create command with invalid data to trigger real validation error
        command = CreateValidUpdateAddressCommand() with { AddressLine1 = "" }; // Empty address line 1 will fail validation

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => _handler.Handle(command, cancellationToken));

        exception.Should().BeOfType<ValidationException>();

        MockLocationRepository.Verify(
            x => x.UpdateAsync(It.IsAny<Location>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_LocationNotFound_ThrowsLocationNotFoundException()
    {
        // Arrange
        var command = CreateValidUpdateAddressCommand("NON-EXISTENT-001");
        var cancellationToken = CancellationToken.None;

        // No validator setup needed - real validator will validate
        MockLocationRepository.Setup(x => x.GetByLocationCodeAsync(command.LocationCode, cancellationToken))
            .ReturnsAsync((Location?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<LocationNotFoundException>(
            () => _handler.Handle(command, cancellationToken));

        exception.LocationCode.Should().Be(command.LocationCode);

        MockLocationRepository.Verify(
            x => x.UpdateAsync(It.IsAny<Location>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_ValidCommand_UpdatesLocationProperties()
    {
        // Arrange
        var existingLocation = CreateTestLocation(
            "LOC-001",
            addressLine1: "Old Street 123",
            addressLine2: "Old Building",
            city: "Old City",
            state: "OS",
            zipCode: "00000",
            country: "Old Country");

        var command = CreateValidUpdateAddressCommand(
            "LOC-001",
            addressLine1: "New Street 456",
            addressLine2: "New Building",
            city: "New City",
            state: "NS",
            zipCode: "11111",
            country: "New Country");

        var cancellationToken = CancellationToken.None;
        Location? updatedLocation = null;

        // No validator setup needed - real validator will validate
        MockLocationRepository.Setup(x => x.GetByLocationCodeAsync(command.LocationCode, cancellationToken))
            .ReturnsAsync(existingLocation);
        MockLocationRepository.Setup(x => x.UpdateAsync(It.IsAny<Location>(), cancellationToken))
            .Callback<Location, CancellationToken>((location, _) => updatedLocation = location);

        // Act
        await _handler.Handle(command, cancellationToken);

        // Assert
        updatedLocation.Should().NotBeNull();
        updatedLocation!.AddressLine1.Should().Be(command.AddressLine1);
        updatedLocation.AddressLine2.Should().Be(command.AddressLine2);
        updatedLocation.City.Should().Be(command.City);
        updatedLocation.State.Should().Be(command.State);
        updatedLocation.ZipCode.Should().Be(command.ZipCode);
        updatedLocation.Country.Should().Be(command.Country);
    }

    [Fact]
    public async Task Handle_ValidCommand_PublishesIntegrationEvent()
    {
        // Arrange
        var existingLocation = CreateTestLocation("LOC-001");
        var command = CreateValidUpdateAddressCommand("LOC-001");
        var cancellationToken = CancellationToken.None;
        object? publishedEvent = null;

        // No validator setup needed - real validator will validate
        MockLocationRepository.Setup(x => x.GetByLocationCodeAsync(command.LocationCode, cancellationToken))
            .ReturnsAsync(existingLocation);
        MockEventPublisher.Setup(x => x.SaveIntegrationEvent(It.IsAny<object>()))
            .Callback<object>(evt => publishedEvent = evt);

        // Act
        await _handler.Handle(command, cancellationToken);

        // Assert
        publishedEvent.Should().NotBeNull();
        publishedEvent.Should().BeOfType<Platform.Locations.Application.IntegrationEvents.LocationAddressUpdatedIntegrationEvent>();

        var locationAddressUpdatedEvent = publishedEvent as Platform.Locations.Application.IntegrationEvents.LocationAddressUpdatedIntegrationEvent;
        locationAddressUpdatedEvent!.LocationCode.Should().Be(command.LocationCode);
        locationAddressUpdatedEvent.AddressLine1.Should().Be(command.AddressLine1);
        locationAddressUpdatedEvent.City.Should().Be(command.City);
        locationAddressUpdatedEvent.Country.Should().Be(command.Country);
    }

    [Theory]
    [InlineData("LOC-001", "", "New City", "CA", "54321", "USA")]
    [InlineData("LOC-001", "New Address", "", "CA", "54321", "USA")]
    [InlineData("LOC-001", "New Address", "New City", "", "54321", "USA")]
    [InlineData("LOC-001", "New Address", "New City", "CA", "", "USA")]
    [InlineData("LOC-001", "New Address", "New City", "CA", "54321", "")]
    public async Task Handle_InvalidCommand_ValidationShouldCatch(
        string locationCode,
        string addressLine1,
        string city,
        string state,
        string zipCode,
        string country)
    {
        // Arrange
        var command = CreateValidUpdateAddressCommand(
            locationCode: locationCode,
            addressLine1: addressLine1,
            city: city,
            state: state,
            zipCode: zipCode,
            country: country);
        var cancellationToken = CancellationToken.None;

        // Using actually invalid data - the real validator will catch these

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(
            () => _handler.Handle(command, cancellationToken));

        MockLocationRepository.Verify(
            x => x.UpdateAsync(It.IsAny<Location>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_RepositoryUpdateThrowsException_PropagatesException()
    {
        // Arrange
        var existingLocation = CreateTestLocation("LOC-001");
        var command = CreateValidUpdateAddressCommand("LOC-001");
        var cancellationToken = CancellationToken.None;
        var repositoryException = new InvalidOperationException("Database error");

        // No validator setup needed - real validator will validate
        MockLocationRepository.Setup(x => x.GetByLocationCodeAsync(command.LocationCode, cancellationToken))
            .ReturnsAsync(existingLocation);
        MockLocationRepository.Setup(x => x.UpdateAsync(It.IsAny<Location>(), cancellationToken))
            .ThrowsAsync(repositoryException);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(command, cancellationToken));

        exception.Should().Be(repositoryException);
    }

    [Fact]
    public async Task Handle_GetByLocationCodeThrowsException_PropagatesException()
    {
        // Arrange
        var command = CreateValidUpdateAddressCommand("LOC-001");
        var cancellationToken = CancellationToken.None;
        var repositoryException = new InvalidOperationException("Database connection error");

        // No validator setup needed - real validator will validate
        MockLocationRepository.Setup(x => x.GetByLocationCodeAsync(command.LocationCode, cancellationToken))
            .ThrowsAsync(repositoryException);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(command, cancellationToken));

        exception.Should().Be(repositoryException);

        MockLocationRepository.Verify(
            x => x.UpdateAsync(It.IsAny<Location>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}