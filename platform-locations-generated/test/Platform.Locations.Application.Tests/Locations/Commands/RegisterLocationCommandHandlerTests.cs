using Platform.Locations.Application.Locations.Commands;
using Platform.Locations.Application.Locations.Dtos;
using Platform.Locations.Application.Tests.Utilities;
using Platform.Locations.Domain.Locations;
using FluentValidation;

namespace Platform.Locations.Application.Tests.Locations.Commands;

public class RegisterLocationCommandHandlerTests : TestFixtureBase
{
    private readonly RegisterLocationCommandHandler _handler;

    public RegisterLocationCommandHandlerTests()
    {
        // Use real validator for proper validation
        var validator = new Platform.Locations.Application.Locations.Validators.RegisterLocationCommandValidator();
        _handler = new RegisterLocationCommandHandler(
            MockLocationRepository.Object,
            validator,
            Mapper,
            MockEventPublisher.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_RegistersLocationSuccessfully()
    {
        // Arrange
        var command = CreateValidRegisterCommand();
        var cancellationToken = CancellationToken.None;

        // No validator setup needed - real validator will validate
        MockLocationRepository.Setup(x => x.GetByLocationCodeAsync(command.LocationCode!, cancellationToken))
            .ReturnsAsync((Location?)null);

        // Act
        var result = await _handler.Handle(command, cancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.LocationCode.Should().Be(command.LocationCode);

        VerifyRepositoryAddCalledOnce();
        VerifyEventPublisherCalledOnce();
    }

    [Fact]
    public async Task Handle_ValidationFails_ThrowsValidationException()
    {
        // Arrange
        var command = CreateValidRegisterCommand();
        var cancellationToken = CancellationToken.None;
        // Create command with invalid data to trigger real validation error
        command = CreateValidRegisterCommand() with { LocationCode = "" }; // Empty location code will fail validation

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => _handler.Handle(command, cancellationToken));

        exception.Should().BeOfType<ValidationException>();

        MockLocationRepository.Verify(
            x => x.AddAsync(It.IsAny<Location>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_LocationCodeAlreadyExists_ThrowsLocationAlreadyExistsException()
    {
        // Arrange
        var command = CreateValidRegisterCommand();
        var existingLocation = CreateTestLocation(locationCode: command.LocationCode!);
        var cancellationToken = CancellationToken.None;

        // No validator setup needed - real validator will validate
        MockLocationRepository.Setup(x => x.GetByLocationCodeAsync(command.LocationCode!, cancellationToken))
            .ReturnsAsync(existingLocation);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<LocationAlreadyExistsException>(
            () => _handler.Handle(command, cancellationToken));

        exception.LocationCode.Should().Be(command.LocationCode);

        MockLocationRepository.Verify(
            x => x.AddAsync(It.IsAny<Location>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_ValidCommand_CallsRepositoryWithCorrectLocation()
    {
        // Arrange
        var command = CreateValidRegisterCommand(
            locationCode: "WAREHOUSE-001",
            locationTypeCode: "WAREHOUSE",
            addressLine1: "123 Storage Way",
            addressLine2: "Building A",
            city: "Austin",
            state: "TX",
            zipCode: "78701",
            country: "USA");
        var cancellationToken = CancellationToken.None;
        Location? capturedLocation = null;

        // No validator setup needed - real validator will validate
        MockLocationRepository.Setup(x => x.GetByLocationCodeAsync(command.LocationCode!, cancellationToken))
            .ReturnsAsync((Location?)null);
        MockLocationRepository.Setup(x => x.AddAsync(It.IsAny<Location>(), cancellationToken))
            .Callback<Location, CancellationToken>((location, _) => capturedLocation = location);

        // Act
        await _handler.Handle(command, cancellationToken);

        // Assert
        capturedLocation.Should().NotBeNull();
        capturedLocation!.LocationCode.Should().Be(command.LocationCode);
        capturedLocation.LocationTypeCode.Should().Be(command.LocationTypeCode);
        capturedLocation.AddressLine1.Should().Be(command.AddressLine1);
        capturedLocation.AddressLine2.Should().Be(command.AddressLine2);
        capturedLocation.City.Should().Be(command.City);
        capturedLocation.State.Should().Be(command.State);
        capturedLocation.ZipCode.Should().Be(command.ZipCode);
        capturedLocation.Country.Should().Be(command.Country);
    }

    [Fact]
    public async Task Handle_ValidCommand_PublishesIntegrationEvent()
    {
        // Arrange
        var command = CreateValidRegisterCommand();
        var cancellationToken = CancellationToken.None;
        object? publishedEvent = null;

        // No validator setup needed - real validator will validate
        MockLocationRepository.Setup(x => x.GetByLocationCodeAsync(command.LocationCode!, cancellationToken))
            .ReturnsAsync((Location?)null);
        MockEventPublisher.Setup(x => x.SaveIntegrationEvent(It.IsAny<object>()))
            .Callback<object>(evt => publishedEvent = evt);

        // Act
        await _handler.Handle(command, cancellationToken);

        // Assert
        publishedEvent.Should().NotBeNull();
        publishedEvent.Should().BeOfType<Platform.Locations.Application.IntegrationEvents.LocationRegisteredIntegrationEvent>();
        
        var locationRegisteredEvent = publishedEvent as Platform.Locations.Application.IntegrationEvents.LocationRegisteredIntegrationEvent;
        locationRegisteredEvent!.LocationCode.Should().Be(command.LocationCode);
        locationRegisteredEvent.AddressLine1.Should().Be(command.AddressLine1);
        locationRegisteredEvent.City.Should().Be(command.City);
        locationRegisteredEvent.Country.Should().Be(command.Country);
    }

    [Theory]
    [InlineData("", "WAREHOUSE", "123 Test St", "Test City", "TX", "12345", "USA")]
    [InlineData("LOC-001", "", "123 Test St", "Test City", "TX", "12345", "USA")]
    [InlineData("LOC-001", "WAREHOUSE", "", "Test City", "TX", "12345", "USA")]
    [InlineData("LOC-001", "WAREHOUSE", "123 Test St", "", "TX", "12345", "USA")]
    [InlineData("LOC-001", "WAREHOUSE", "123 Test St", "Test City", "", "12345", "USA")]
    [InlineData("LOC-001", "WAREHOUSE", "123 Test St", "Test City", "TX", "", "USA")]
    [InlineData("LOC-001", "WAREHOUSE", "123 Test St", "Test City", "TX", "12345", "")]
    public async Task Handle_InvalidCommand_ValidationShouldCatch(
        string locationCode,
        string locationTypeCode,
        string addressLine1,
        string city,
        string state,
        string zipCode,
        string country)
    {
        // Arrange
        var command = CreateValidRegisterCommand(
            locationCode: locationCode,
            locationTypeCode: locationTypeCode,
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
            x => x.AddAsync(It.IsAny<Location>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_RepositoryThrowsException_PropagatesException()
    {
        // Arrange
        var command = CreateValidRegisterCommand();
        var cancellationToken = CancellationToken.None;
        var repositoryException = new InvalidOperationException("Database error");

        // No validator setup needed - real validator will validate
        MockLocationRepository.Setup(x => x.GetByLocationCodeAsync(command.LocationCode!, cancellationToken))
            .ReturnsAsync((Location?)null);
        MockLocationRepository.Setup(x => x.AddAsync(It.IsAny<Location>(), cancellationToken))
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
        var command = CreateValidRegisterCommand();
        var cancellationToken = CancellationToken.None;
        var repositoryException = new InvalidOperationException("Database connection error");

        // No validator setup needed - real validator will validate
        MockLocationRepository.Setup(x => x.GetByLocationCodeAsync(command.LocationCode!, cancellationToken))
            .ThrowsAsync(repositoryException);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(command, cancellationToken));

        exception.Should().Be(repositoryException);

        MockLocationRepository.Verify(
            x => x.AddAsync(It.IsAny<Location>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}