using Platform.Customers.Application.Customers.Commands;
using Platform.Customers.Application.Customers.Dtos;
using Platform.Customers.Application.Tests.Utilities;
using Platform.Customers.Domain.Customers;
using FluentValidation;

namespace Platform.Customers.Application.Tests.Customers.Commands;

public class UpdateCustomerCommandHandlerTests : TestFixtureBase
{
    private readonly UpdateCustomerCommandHandler _handler;

    public UpdateCustomerCommandHandlerTests()
    {
        // Use real validator for proper validation
        var validator = new Platform.Customers.Application.Customers.Validators.UpdateCustomerCommandValidator();
        _handler = new UpdateCustomerCommandHandler(
            MockCustomerRepository.Object,
            validator,
            Mapper,
            MockEventPublisher.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_UpdatesCustomerSuccessfully()
    {
        // Arrange
        var existingCustomer = CreateTestCustomer("TEST-001", email: "old@test.com");
        var command = CreateValidUpdateCommand("TEST-001", email: "new@test.com");
        var cancellationToken = CancellationToken.None;

        // No validator setup needed - real validator will validate
        MockCustomerRepository.Setup(x => x.GetByCustomerCodeAsync(command.CustomerCode, cancellationToken))
            .ReturnsAsync(existingCustomer);
        MockCustomerRepository.Setup(x => x.GetByEmailAsync(command.Email, cancellationToken))
            .ReturnsAsync((Customer?)null);

        // Act
        var result = await _handler.Handle(command, cancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.CustomerCode.Should().Be(command.CustomerCode);
        result.CompanyName.Should().Be(command.CompanyName);
        result.Email.Should().Be(command.Email);

        VerifyRepositoryUpdateCalledOnce();
        VerifyEventPublisherCalledOnce();
    }

    [Fact]
    public async Task Handle_ValidationFails_ThrowsValidationException()
    {
        // Arrange
        var command = CreateValidUpdateCommand();
        var cancellationToken = CancellationToken.None;
        // Create command with invalid data to trigger real validation error
        command = CreateValidUpdateCommand() with { CustomerCode = "" }; // Empty customer code will fail validation

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => _handler.Handle(command, cancellationToken));

        exception.Should().BeOfType<ValidationException>();

        MockCustomerRepository.Verify(
            x => x.UpdateAsync(It.IsAny<Customer>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_CustomerNotFound_ThrowsCustomerNotFoundException()
    {
        // Arrange
        var command = CreateValidUpdateCommand("NON-EXISTENT-001");
        var cancellationToken = CancellationToken.None;

        // No validator setup needed - real validator will validate
        MockCustomerRepository.Setup(x => x.GetByCustomerCodeAsync(command.CustomerCode, cancellationToken))
            .ReturnsAsync((Customer?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<CustomerNotFoundException>(
            () => _handler.Handle(command, cancellationToken));

        exception.CustomerCode.Should().Be(command.CustomerCode);

        MockCustomerRepository.Verify(
            x => x.UpdateAsync(It.IsAny<Customer>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_EmailAlreadyExistsForDifferentCustomer_ThrowsEmailAlreadyExistsException()
    {
        // Arrange
        var existingCustomer = CreateTestCustomer("TEST-001");
        var otherCustomer = CreateTestCustomer("TEST-002", email: "other@test.com");
        var command = CreateValidUpdateCommand("TEST-001", email: "other@test.com");
        var cancellationToken = CancellationToken.None;

        // No validator setup needed - real validator will validate
        MockCustomerRepository.Setup(x => x.GetByCustomerCodeAsync(command.CustomerCode, cancellationToken))
            .ReturnsAsync(existingCustomer);
        MockCustomerRepository.Setup(x => x.GetByEmailAsync(command.Email, cancellationToken))
            .ReturnsAsync(otherCustomer);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<EmailAlreadyExistsException>(
            () => _handler.Handle(command, cancellationToken));

        exception.Email.Should().Be(command.Email);

        MockCustomerRepository.Verify(
            x => x.UpdateAsync(It.IsAny<Customer>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_SameEmailForSameCustomer_UpdatesSuccessfully()
    {
        // Arrange
        var existingCustomer = CreateTestCustomer("TEST-001", email: "same@test.com");
        var command = CreateValidUpdateCommand("TEST-001", email: "same@test.com");
        var cancellationToken = CancellationToken.None;

        // No validator setup needed - real validator will validate
        MockCustomerRepository.Setup(x => x.GetByCustomerCodeAsync(command.CustomerCode, cancellationToken))
            .ReturnsAsync(existingCustomer);
        MockCustomerRepository.Setup(x => x.GetByEmailAsync(command.Email, cancellationToken))
            .ReturnsAsync(existingCustomer);

        // Act
        var result = await _handler.Handle(command, cancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.CustomerCode.Should().Be(command.CustomerCode);
        result.Email.Should().Be(command.Email);

        VerifyRepositoryUpdateCalledOnce();
        VerifyEventPublisherCalledOnce();
    }

    [Fact]
    public async Task Handle_ValidCommand_UpdatesCustomerProperties()
    {
        // Arrange
        var existingCustomer = CreateTestCustomer(
            "TEST-001",
            companyName: "Old Company",
            firstName: "Old",
            lastName: "Name",
            email: "old@test.com",
            phoneNumber: "555-0000",
            addressLine1: "Old Address",
            city: "Old City",
            state: "OS",
            postalCode: "00000");

        var command = CreateValidUpdateCommand(
            "TEST-001",
            companyName: "New Company",
            firstName: "New",
            lastName: "Name",
            email: "new@test.com",
            phoneNumber: "555-1111",
            addressLine1: "New Address",
            addressLine2: "Suite 100",
            city: "New City",
            state: "NS",
            postalCode: "11111");

        var cancellationToken = CancellationToken.None;
        Customer? updatedCustomer = null;

        // No validator setup needed - real validator will validate
        MockCustomerRepository.Setup(x => x.GetByCustomerCodeAsync(command.CustomerCode, cancellationToken))
            .ReturnsAsync(existingCustomer);
        MockCustomerRepository.Setup(x => x.GetByEmailAsync(command.Email, cancellationToken))
            .ReturnsAsync((Customer?)null);
        MockCustomerRepository.Setup(x => x.UpdateAsync(It.IsAny<Customer>(), cancellationToken))
            .Callback<Customer, CancellationToken>((customer, _) => updatedCustomer = customer);

        // Act
        await _handler.Handle(command, cancellationToken);

        // Assert
        updatedCustomer.Should().NotBeNull();
        updatedCustomer!.CompanyName.Should().Be(command.CompanyName);
        updatedCustomer.ContactInfo.FirstName.Should().Be(command.ContactFirstName);
        updatedCustomer.ContactInfo.LastName.Should().Be(command.ContactLastName);
        updatedCustomer.ContactInfo.Email.Should().Be(command.Email);
        updatedCustomer.ContactInfo.PhoneNumber.Should().Be(command.PhoneNumber);
        updatedCustomer.Address.AddressLine1.Should().Be(command.AddressLine1);
        updatedCustomer.Address.AddressLine2.Should().Be(command.AddressLine2);
        updatedCustomer.Address.City.Should().Be(command.City);
        updatedCustomer.Address.State.Should().Be(command.State);
        updatedCustomer.Address.PostalCode.Should().Be(command.PostalCode);
        updatedCustomer.Address.Country.Should().Be(command.Country);
    }

    [Fact]
    public async Task Handle_ValidCommand_PublishesIntegrationEvent()
    {
        // Arrange
        var existingCustomer = CreateTestCustomer("TEST-001");
        var command = CreateValidUpdateCommand("TEST-001");
        var cancellationToken = CancellationToken.None;
        object? publishedEvent = null;

        // No validator setup needed - real validator will validate
        MockCustomerRepository.Setup(x => x.GetByCustomerCodeAsync(command.CustomerCode, cancellationToken))
            .ReturnsAsync(existingCustomer);
        MockCustomerRepository.Setup(x => x.GetByEmailAsync(command.Email, cancellationToken))
            .ReturnsAsync((Customer?)null);
        MockEventPublisher.Setup(x => x.SaveIntegrationEvent(It.IsAny<object>()))
            .Callback<object>(evt => publishedEvent = evt);

        // Act
        await _handler.Handle(command, cancellationToken);

        // Assert
        publishedEvent.Should().NotBeNull();
        publishedEvent.Should().BeOfType<Platform.Customers.Application.IntegrationEvents.CustomerUpdatedIntegrationEvent>();

        var customerUpdatedEvent = publishedEvent as Platform.Customers.Application.IntegrationEvents.CustomerUpdatedIntegrationEvent;
        customerUpdatedEvent!.CustomerCode.Should().Be(command.CustomerCode);
        customerUpdatedEvent.CompanyName.Should().Be(command.CompanyName);
        customerUpdatedEvent.Email.Should().Be(command.Email);
    }

    [Theory]
    [InlineData("", "Updated Company", "John", "Doe", "john@test.com")]
    [InlineData("TEST-001", "", "John", "Doe", "john@test.com")]
    [InlineData("TEST-001", "Updated Company", "", "Doe", "john@test.com")]
    [InlineData("TEST-001", "Updated Company", "John", "", "john@test.com")]
    [InlineData("TEST-001", "Updated Company", "John", "Doe", "")]
    public async Task Handle_InvalidCommand_ValidationShouldCatch(
        string customerCode,
        string companyName,
        string firstName,
        string lastName,
        string email)
    {
        // Arrange
        var command = CreateValidUpdateCommand(
            customerCode: customerCode,
            companyName: companyName,
            firstName: firstName,
            lastName: lastName,
            email: email);
        var cancellationToken = CancellationToken.None;

        // Using actually invalid data - the real validator will catch these

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(
            () => _handler.Handle(command, cancellationToken));

        MockCustomerRepository.Verify(
            x => x.UpdateAsync(It.IsAny<Customer>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_RepositoryThrowsException_PropagatesException()
    {
        // Arrange
        var existingCustomer = CreateTestCustomer("TEST-001");
        var command = CreateValidUpdateCommand("TEST-001");
        var cancellationToken = CancellationToken.None;
        var repositoryException = new InvalidOperationException("Database error");

        // No validator setup needed - real validator will validate
        MockCustomerRepository.Setup(x => x.GetByCustomerCodeAsync(command.CustomerCode, cancellationToken))
            .ReturnsAsync(existingCustomer);
        MockCustomerRepository.Setup(x => x.GetByEmailAsync(command.Email, cancellationToken))
            .ReturnsAsync((Customer?)null);
        MockCustomerRepository.Setup(x => x.UpdateAsync(It.IsAny<Customer>(), cancellationToken))
            .ThrowsAsync(repositoryException);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(command, cancellationToken));

        exception.Should().Be(repositoryException);
    }

    [Fact]
    public async Task Handle_GetByCustomerCodeThrowsException_PropagatesException()
    {
        // Arrange
        var command = CreateValidUpdateCommand("TEST-001");
        var cancellationToken = CancellationToken.None;
        var repositoryException = new InvalidOperationException("Database connection error");

        // No validator setup needed - real validator will validate
        MockCustomerRepository.Setup(x => x.GetByCustomerCodeAsync(command.CustomerCode, cancellationToken))
            .ThrowsAsync(repositoryException);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(command, cancellationToken));

        exception.Should().Be(repositoryException);

        MockCustomerRepository.Verify(
            x => x.UpdateAsync(It.IsAny<Customer>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_GetByEmailThrowsException_PropagatesException()
    {
        // Arrange
        var existingCustomer = CreateTestCustomer("TEST-001");
        var command = CreateValidUpdateCommand("TEST-001");
        var cancellationToken = CancellationToken.None;
        var repositoryException = new InvalidOperationException("Email lookup failed");

        // No validator setup needed - real validator will validate
        MockCustomerRepository.Setup(x => x.GetByCustomerCodeAsync(command.CustomerCode, cancellationToken))
            .ReturnsAsync(existingCustomer);
        MockCustomerRepository.Setup(x => x.GetByEmailAsync(command.Email, cancellationToken))
            .ThrowsAsync(repositoryException);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(command, cancellationToken));

        exception.Should().Be(repositoryException);

        MockCustomerRepository.Verify(
            x => x.UpdateAsync(It.IsAny<Customer>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}