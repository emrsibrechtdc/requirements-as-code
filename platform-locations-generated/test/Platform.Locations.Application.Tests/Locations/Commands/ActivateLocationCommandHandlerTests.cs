using Platform.Locations.Application.Locations.Commands;
using Platform.Locations.Application.Locations.Dtos;
using Platform.Locations.Application.Tests.Utilities;
using Platform.Locations.Domain.Locations;
using FluentValidation;

namespace Platform.Locations.Application.Tests.Locations.Commands;

public class ActivateLocationCommandHandlerTests : TestFixtureBase
{
    private readonly ActivateLocationCommandHandler _handler;

    public ActivateLocationCommandHandlerTests()
    {
        // Use real validator for proper validation  
        var validator = new Platform.Locations.Application.Locations.Validators.ActivateLocationCommandValidator();
        _handler = new ActivateLocationCommandHandler(
            MockLocationRepository.Object,
            validator,
            Mapper,
            MockEventPublisher.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_ActivatesLocationSuccessfully()
    {
        // Arrange
        var existingLocation = CreateTestLocation("LOC-001", isActive: false);
        var command = CreateValidActivateCommand("LOC-001");
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
        var command = CreateValidActivateCommand() with { LocationCode = "" }; // Empty location code will fail validation
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
        var command = CreateValidActivateCommand("NON-EXISTENT-001");
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
    public async Task Handle_LocationAlreadyActive_ThrowsLocationAlreadyActiveException()
    {
        // Arrange
        var activeLocation = CreateTestLocation("LOC-001", isActive: true);
        var command = CreateValidActivateCommand("LOC-001");
        var cancellationToken = CancellationToken.None;

        // No validator setup needed - real validator will validate
        MockLocationRepository.Setup(x => x.GetByLocationCodeAsync(command.LocationCode, cancellationToken))
            .ReturnsAsync(activeLocation);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<LocationAlreadyActiveException>(
            () => _handler.Handle(command, cancellationToken));

        exception.LocationCode.Should().Be(command.LocationCode);

        MockLocationRepository.Verify(
            x => x.UpdateAsync(It.IsAny<Location>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_ValidCommand_SetsLocationAsActive()
    {
        // Arrange
        var inactiveLocation = CreateTestLocation("LOC-001", isActive: false);
        var command = CreateValidActivateCommand("LOC-001");
        var cancellationToken = CancellationToken.None;
        Location? updatedLocation = null;

        // No validator setup needed - real validator will validate
        MockLocationRepository.Setup(x => x.GetByLocationCodeAsync(command.LocationCode, cancellationToken))
            .ReturnsAsync(inactiveLocation);
        MockLocationRepository.Setup(x => x.UpdateAsync(It.IsAny<Location>(), cancellationToken))
            .Callback<Location, CancellationToken>((location, _) => updatedLocation = location);

        // Act
        await _handler.Handle(command, cancellationToken);

        // Assert
        updatedLocation.Should().NotBeNull();
        updatedLocation!.IsActive.Should().BeTrue();
        updatedLocation.LocationCode.Should().Be(command.LocationCode);
    }

    [Fact]
    public async Task Handle_ValidCommand_PublishesIntegrationEvent()
    {
        // Arrange
        var inactiveLocation = CreateTestLocation("LOC-001", isActive: false);
        var command = CreateValidActivateCommand("LOC-001");
        var cancellationToken = CancellationToken.None;
        object? publishedEvent = null;

        // No validator setup needed - real validator will validate
        MockLocationRepository.Setup(x => x.GetByLocationCodeAsync(command.LocationCode, cancellationToken))
            .ReturnsAsync(inactiveLocation);
        MockEventPublisher.Setup(x => x.SaveIntegrationEvent(It.IsAny<object>()))
            .Callback<object>(evt => publishedEvent = evt);

        // Act
        await _handler.Handle(command, cancellationToken);

        // Assert
        publishedEvent.Should().NotBeNull();
        publishedEvent.Should().BeOfType<Platform.Locations.Application.IntegrationEvents.LocationActivatedIntegrationEvent>();

        var locationActivatedEvent = publishedEvent as Platform.Locations.Application.IntegrationEvents.LocationActivatedIntegrationEvent;
        locationActivatedEvent!.LocationCode.Should().Be(command.LocationCode);
    }

    [Fact]
    public async Task Handle_RepositoryGetThrowsException_PropagatesException()
    {
        // Arrange
        var command = CreateValidActivateCommand("LOC-001");
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
        var inactiveLocation = CreateTestLocation("LOC-001", isActive: false);
        var command = CreateValidActivateCommand("LOC-001");
        var cancellationToken = CancellationToken.None;
        var repositoryException = new InvalidOperationException("Database error");

        // No validator setup needed - real validator will validate
        MockLocationRepository.Setup(x => x.GetByLocationCodeAsync(command.LocationCode, cancellationToken))
            .ReturnsAsync(inactiveLocation);
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
        var command = CreateValidActivateCommand() with { LocationCode = locationCode! };
        var cancellationToken = CancellationToken.None;

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(
            () => _handler.Handle(command, cancellationToken));

        MockLocationRepository.Verify(
            x => x.GetByLocationCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}