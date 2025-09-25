using Platform.Locations.Application.Locations.Commands;
using Platform.Locations.Application.Locations.Dtos;
using Platform.Locations.Application.Tests.Utilities;
using Platform.Locations.Domain.Locations;
using FluentValidation;

namespace Platform.Locations.Application.Tests.Locations.Commands;

public class DeleteLocationCommandHandlerTests : TestFixtureBase
{
    private readonly DeleteLocationCommandHandler _handler;

    public DeleteLocationCommandHandlerTests()
    {
        // Use real validator for proper validation  
        var validator = new Platform.Locations.Application.Locations.Validators.DeleteLocationCommandValidator();
        _handler = new DeleteLocationCommandHandler(
            MockLocationRepository.Object,
            validator,
            Mapper,
            MockEventPublisher.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_DeletesLocationSuccessfully()
    {
        // Arrange
        var existingLocation = CreateTestLocation("LOC-001", isActive: false);
        var command = CreateValidDeleteCommand("LOC-001");
        var cancellationToken = CancellationToken.None;

        // No validator setup needed - real validator will validate
        MockLocationRepository.Setup(x => x.GetByLocationCodeAsync(command.LocationCode, cancellationToken))
            .ReturnsAsync(existingLocation);

        // Act
        await _handler.Handle(command, cancellationToken);

        // Assert
        VerifyRepositoryDeleteCalledOnce();
        VerifyEventPublisherCalledOnce();
    }

    [Fact]
    public async Task Handle_ValidationFails_ThrowsValidationException()
    {
        // Arrange
        var command = CreateValidDeleteCommand() with { LocationCode = "" }; // Empty location code will fail validation
        var cancellationToken = CancellationToken.None;

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => _handler.Handle(command, cancellationToken));

        exception.Should().BeOfType<ValidationException>();

        MockLocationRepository.Verify(
            x => x.DeleteAsync(It.IsAny<Location>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_LocationNotFound_ThrowsLocationNotFoundException()
    {
        // Arrange
        var command = CreateValidDeleteCommand("NON-EXISTENT-001");
        var cancellationToken = CancellationToken.None;

        // No validator setup needed - real validator will validate
        MockLocationRepository.Setup(x => x.GetByLocationCodeAsync(command.LocationCode, cancellationToken))
            .ReturnsAsync((Location?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<LocationNotFoundException>(
            () => _handler.Handle(command, cancellationToken));

        exception.LocationCode.Should().Be(command.LocationCode);

        MockLocationRepository.Verify(
            x => x.DeleteAsync(It.IsAny<Location>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_ActiveLocation_DeletesSuccessfully()
    {
        // Arrange
        var activeLocation = CreateTestLocation("LOC-001", isActive: true);
        var command = CreateValidDeleteCommand("LOC-001");
        var cancellationToken = CancellationToken.None;

        // No validator setup needed - real validator will validate
        MockLocationRepository.Setup(x => x.GetByLocationCodeAsync(command.LocationCode, cancellationToken))
            .ReturnsAsync(activeLocation);

        // Act
        await _handler.Handle(command, cancellationToken);

        // Assert
        VerifyRepositoryDeleteCalledOnce();
        VerifyEventPublisherCalledOnce();
    }

    [Fact]
    public async Task Handle_ValidCommand_CallsRepositoryDeleteWithCorrectLocation()
    {
        // Arrange
        var existingLocation = CreateTestLocation("LOC-001", isActive: false);
        var command = CreateValidDeleteCommand("LOC-001");
        var cancellationToken = CancellationToken.None;
        Location? deletedLocation = null;

        // No validator setup needed - real validator will validate
        MockLocationRepository.Setup(x => x.GetByLocationCodeAsync(command.LocationCode, cancellationToken))
            .ReturnsAsync(existingLocation);
        MockLocationRepository.Setup(x => x.DeleteAsync(It.IsAny<Location>(), cancellationToken))
            .Callback<Location, CancellationToken>((location, _) => deletedLocation = location);

        // Act
        await _handler.Handle(command, cancellationToken);

        // Assert
        deletedLocation.Should().NotBeNull();
        deletedLocation.Should().Be(existingLocation);
        deletedLocation!.LocationCode.Should().Be(command.LocationCode);
    }

    [Fact]
    public async Task Handle_ValidCommand_PublishesIntegrationEvent()
    {
        // Arrange
        var existingLocation = CreateTestLocation("LOC-001", isActive: false);
        var command = CreateValidDeleteCommand("LOC-001");
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
        publishedEvent.Should().BeOfType<Platform.Locations.Application.IntegrationEvents.LocationDeletedIntegrationEvent>();

        var locationDeletedEvent = publishedEvent as Platform.Locations.Application.IntegrationEvents.LocationDeletedIntegrationEvent;
        locationDeletedEvent!.LocationCode.Should().Be(command.LocationCode);
    }

    [Fact]
    public async Task Handle_RepositoryGetThrowsException_PropagatesException()
    {
        // Arrange
        var command = CreateValidDeleteCommand("LOC-001");
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
            x => x.DeleteAsync(It.IsAny<Location>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_RepositoryDeleteThrowsException_PropagatesException()
    {
        // Arrange
        var existingLocation = CreateTestLocation("LOC-001", isActive: false);
        var command = CreateValidDeleteCommand("LOC-001");
        var cancellationToken = CancellationToken.None;
        var repositoryException = new InvalidOperationException("Database error");

        // No validator setup needed - real validator will validate
        MockLocationRepository.Setup(x => x.GetByLocationCodeAsync(command.LocationCode, cancellationToken))
            .ReturnsAsync(existingLocation);
        MockLocationRepository.Setup(x => x.DeleteAsync(It.IsAny<Location>(), cancellationToken))
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
        var command = CreateValidDeleteCommand() with { LocationCode = locationCode! };
        var cancellationToken = CancellationToken.None;

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(
            () => _handler.Handle(command, cancellationToken));

        MockLocationRepository.Verify(
            x => x.GetByLocationCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_InactiveLocation_DeletesSuccessfully()
    {
        // Arrange
        var inactiveLocation = CreateTestLocation("LOC-001", isActive: false);
        var command = CreateValidDeleteCommand("LOC-001");
        var cancellationToken = CancellationToken.None;

        // No validator setup needed - real validator will validate
        MockLocationRepository.Setup(x => x.GetByLocationCodeAsync(command.LocationCode, cancellationToken))
            .ReturnsAsync(inactiveLocation);

        // Act
        await _handler.Handle(command, cancellationToken);

        // Assert
        MockLocationRepository.Verify(
            x => x.DeleteAsync(inactiveLocation, cancellationToken),
            Times.Once);
        VerifyEventPublisherCalledOnce();
    }
}