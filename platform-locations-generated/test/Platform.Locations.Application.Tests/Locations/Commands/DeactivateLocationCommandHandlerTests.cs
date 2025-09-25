using Platform.Locations.Application.Locations.Commands;
using Platform.Locations.Application.Locations.Dtos;
using Platform.Locations.Application.Tests.Utilities;
using Platform.Locations.Domain.Locations;
using FluentValidation;

namespace Platform.Locations.Application.Tests.Locations.Commands;

public class DeactivateLocationCommandHandlerTests : TestFixtureBase
{
    private readonly DeactivateLocationCommandHandler _handler;

    public DeactivateLocationCommandHandlerTests()
    {
        // Use real validator for proper validation  
        var validator = new Platform.Locations.Application.Locations.Validators.DeactivateLocationCommandValidator();
        _handler = new DeactivateLocationCommandHandler(
            MockLocationRepository.Object,
            validator,
            Mapper,
            MockEventPublisher.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_DeactivatesLocationSuccessfully()
    {
        // Arrange
        var existingLocation = CreateTestLocation("LOC-001", isActive: true);
        var command = CreateValidDeactivateCommand("LOC-001");
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
        var command = CreateValidDeactivateCommand() with { LocationCode = "" }; // Empty location code will fail validation
        var cancellationToken = CancellationToken.None;

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
        var command = CreateValidDeactivateCommand("NON-EXISTENT-001");
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
    public async Task Handle_LocationAlreadyInactive_ThrowsLocationAlreadyInactiveException()
    {
        // Arrange
        var inactiveLocation = CreateTestLocation("LOC-001", isActive: false);
        var command = CreateValidDeactivateCommand("LOC-001");
        var cancellationToken = CancellationToken.None;

        // No validator setup needed - real validator will validate
        MockLocationRepository.Setup(x => x.GetByLocationCodeAsync(command.LocationCode, cancellationToken))
            .ReturnsAsync(inactiveLocation);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<LocationAlreadyInactiveException>(
            () => _handler.Handle(command, cancellationToken));

        exception.LocationCode.Should().Be(command.LocationCode);

        MockLocationRepository.Verify(
            x => x.UpdateAsync(It.IsAny<Location>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_ValidCommand_SetsLocationAsInactive()
    {
        // Arrange
        var activeLocation = CreateTestLocation("LOC-001", isActive: true);
        var command = CreateValidDeactivateCommand("LOC-001");
        var cancellationToken = CancellationToken.None;
        Location? updatedLocation = null;

        // No validator setup needed - real validator will validate
        MockLocationRepository.Setup(x => x.GetByLocationCodeAsync(command.LocationCode, cancellationToken))
            .ReturnsAsync(activeLocation);
        MockLocationRepository.Setup(x => x.UpdateAsync(It.IsAny<Location>(), cancellationToken))
            .Callback<Location, CancellationToken>((location, _) => updatedLocation = location);

        // Act
        await _handler.Handle(command, cancellationToken);

        // Assert
        updatedLocation.Should().NotBeNull();
        updatedLocation!.IsActive.Should().BeFalse();
        updatedLocation.LocationCode.Should().Be(command.LocationCode);
    }

    [Fact]
    public async Task Handle_ValidCommand_PublishesIntegrationEvent()
    {
        // Arrange
        var activeLocation = CreateTestLocation("LOC-001", isActive: true);
        var command = CreateValidDeactivateCommand("LOC-001");
        var cancellationToken = CancellationToken.None;
        object? publishedEvent = null;

        // No validator setup needed - real validator will validate
        MockLocationRepository.Setup(x => x.GetByLocationCodeAsync(command.LocationCode, cancellationToken))
            .ReturnsAsync(activeLocation);
        MockEventPublisher.Setup(x => x.SaveIntegrationEvent(It.IsAny<object>()))
            .Callback<object>(evt => publishedEvent = evt);

        // Act
        await _handler.Handle(command, cancellationToken);

        // Assert
        publishedEvent.Should().NotBeNull();
        publishedEvent.Should().BeOfType<Platform.Locations.Application.IntegrationEvents.LocationDeactivatedIntegrationEvent>();

        var locationDeactivatedEvent = publishedEvent as Platform.Locations.Application.IntegrationEvents.LocationDeactivatedIntegrationEvent;
        locationDeactivatedEvent!.LocationCode.Should().Be(command.LocationCode);
    }

    [Fact]
    public async Task Handle_RepositoryGetThrowsException_PropagatesException()
    {
        // Arrange
        var command = CreateValidDeactivateCommand("LOC-001");
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

    [Fact]
    public async Task Handle_RepositoryUpdateThrowsException_PropagatesException()
    {
        // Arrange
        var activeLocation = CreateTestLocation("LOC-001", isActive: true);
        var command = CreateValidDeactivateCommand("LOC-001");
        var cancellationToken = CancellationToken.None;
        var repositoryException = new InvalidOperationException("Database error");

        // No validator setup needed - real validator will validate
        MockLocationRepository.Setup(x => x.GetByLocationCodeAsync(command.LocationCode, cancellationToken))
            .ReturnsAsync(activeLocation);
        MockLocationRepository.Setup(x => x.UpdateAsync(It.IsAny<Location>(), cancellationToken))
            .ThrowsAsync(repositoryException);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(command, cancellationToken));

        exception.Should().Be(repositoryException);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task Handle_InvalidLocationCode_ThrowsValidationException(string? locationCode)
    {
        // Arrange
        var command = CreateValidDeactivateCommand() with { LocationCode = locationCode! };
        var cancellationToken = CancellationToken.None;

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(
            () => _handler.Handle(command, cancellationToken));

        MockLocationRepository.Verify(
            x => x.GetByLocationCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}