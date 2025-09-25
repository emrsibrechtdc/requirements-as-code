using Platform.Customers.Application.Customers.Commands;
using Platform.Customers.Application.Customers.Dtos;
using Platform.Customers.Application.Tests.Utilities;
using Platform.Customers.Domain.Customers;
using FluentValidation;

namespace Platform.Customers.Application.Tests.Customers.Commands;

public class CreateCustomerCommandHandlerTests : TestFixtureBase
{
    private readonly CreateCustomerCommandHandler _handler;

    public CreateCustomerCommandHandlerTests()
    {
        // Use real validator for proper validation
        var validator = new Platform.Customers.Application.Customers.Validators.CreateCustomerCommandValidator();
        _handler = new CreateCustomerCommandHandler(
            MockCustomerRepository.Object,
            validator,
            Mapper,
            MockEventPublisher.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_CreatesCustomerSuccessfully()
    {
        // Arrange
        var command = CreateValidCreateCommand();
        var cancellationToken = CancellationToken.None;

        // No validator setup needed - real validator will validate
        MockCustomerRepository.Setup(x => x.GetByCustomerCodeAsync(command.CustomerCode, cancellationToken))
            .ReturnsAsync((Customer?)null);
        MockCustomerRepository.Setup(x => x.GetByEmailAsync(command.Email, cancellationToken))
            .ReturnsAsync((Customer?)null);

        // Act
        var result = await _handler.Handle(command, cancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.CustomerCode.Should().Be(command.CustomerCode);
        result.CompanyName.Should().Be(command.CompanyName);
        result.Email.Should().Be(command.Email);
        result.IsActive.Should().BeTrue();

        VerifyRepositoryAddCalledOnce();
        VerifyEventPublisherCalledOnce();
    }

    [Fact]
    public async Task Handle_ValidationFails_ThrowsValidationException()
    {
        // Arrange
        var command = CreateValidCreateCommand();
        var cancellationToken = CancellationToken.None;
        // Create command with invalid data to trigger real validation error
        command = CreateValidCreateCommand() with { CustomerCode = "" }; // Empty customer code will fail validation

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => _handler.Handle(command, cancellationToken));

        exception.Should().BeOfType<ValidationException>();

        MockCustomerRepository.Verify(
            x => x.AddAsync(It.IsAny<Customer>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_CustomerCodeAlreadyExists_ThrowsCustomerAlreadyExistsException()
    {
        // Arrange
        var command = CreateValidCreateCommand();
        var existingCustomer = CreateTestCustomer(customerCode: command.CustomerCode);
        var cancellationToken = CancellationToken.None;

        // No validator setup needed - real validator will validate
        MockCustomerRepository.Setup(x => x.GetByCustomerCodeAsync(command.CustomerCode, cancellationToken))
            .ReturnsAsync(existingCustomer);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<CustomerAlreadyExistsException>(
            () => _handler.Handle(command, cancellationToken));

        exception.CustomerCode.Should().Be(command.CustomerCode);

        MockCustomerRepository.Verify(
            x => x.AddAsync(It.IsAny<Customer>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_EmailAlreadyExists_ThrowsEmailAlreadyExistsException()
    {
        // Arrange
        var command = CreateValidCreateCommand();
        var existingCustomer = CreateTestCustomer(email: command.Email, customerCode: "DIFFERENT-001");
        var cancellationToken = CancellationToken.None;

        // No validator setup needed - real validator will validate
        MockCustomerRepository.Setup(x => x.GetByCustomerCodeAsync(command.CustomerCode, cancellationToken))
            .ReturnsAsync((Customer?)null);
        MockCustomerRepository.Setup(x => x.GetByEmailAsync(command.Email, cancellationToken))
            .ReturnsAsync(existingCustomer);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<EmailAlreadyExistsException>(
            () => _handler.Handle(command, cancellationToken));

        exception.Email.Should().Be(command.Email);

        MockCustomerRepository.Verify(
            x => x.AddAsync(It.IsAny<Customer>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_ValidCommand_CallsRepositoryWithCorrectCustomer()
    {
        // Arrange
        var command = CreateValidCreateCommand(
            customerCode: "TEST-123",
            customerType: "ENTERPRISE",
            companyName: "Test Corp",
            firstName: "John",
            lastName: "Doe",
            email: "john@test.com");
        var cancellationToken = CancellationToken.None;
        Customer? capturedCustomer = null;

        // No validator setup needed - real validator will validate
        MockCustomerRepository.Setup(x => x.GetByCustomerCodeAsync(command.CustomerCode, cancellationToken))
            .ReturnsAsync((Customer?)null);
        MockCustomerRepository.Setup(x => x.GetByEmailAsync(command.Email, cancellationToken))
            .ReturnsAsync((Customer?)null);
        MockCustomerRepository.Setup(x => x.AddAsync(It.IsAny<Customer>(), cancellationToken))
            .Callback<Customer, CancellationToken>((customer, _) => capturedCustomer = customer);

        // Act
        await _handler.Handle(command, cancellationToken);

        // Assert
        capturedCustomer.Should().NotBeNull();
        capturedCustomer!.CustomerCode.Should().Be(command.CustomerCode);
        capturedCustomer.CustomerType.Should().Be(command.CustomerType);
        capturedCustomer.CompanyName.Should().Be(command.CompanyName);
        capturedCustomer.ContactInfo.FirstName.Should().Be(command.ContactFirstName);
        capturedCustomer.ContactInfo.LastName.Should().Be(command.ContactLastName);
        capturedCustomer.ContactInfo.Email.Should().Be(command.Email);
        capturedCustomer.Address.AddressLine1.Should().Be(command.AddressLine1);
        capturedCustomer.Address.City.Should().Be(command.City);
        capturedCustomer.Address.State.Should().Be(command.State);
        capturedCustomer.Address.PostalCode.Should().Be(command.PostalCode);
        capturedCustomer.Address.Country.Should().Be(command.Country);
    }

    [Fact]
    public async Task Handle_ValidCommand_PublishesIntegrationEvent()
    {
        // Arrange
        var command = CreateValidCreateCommand();
        var cancellationToken = CancellationToken.None;
        object? publishedEvent = null;

        // No validator setup needed - real validator will validate
        MockCustomerRepository.Setup(x => x.GetByCustomerCodeAsync(command.CustomerCode, cancellationToken))
            .ReturnsAsync((Customer?)null);
        MockCustomerRepository.Setup(x => x.GetByEmailAsync(command.Email, cancellationToken))
            .ReturnsAsync((Customer?)null);
        MockEventPublisher.Setup(x => x.SaveIntegrationEvent(It.IsAny<object>()))
            .Callback<object>(evt => publishedEvent = evt);

        // Act
        await _handler.Handle(command, cancellationToken);

        // Assert
        publishedEvent.Should().NotBeNull();
        publishedEvent.Should().BeOfType<Platform.Customers.Application.IntegrationEvents.CustomerCreatedIntegrationEvent>();
        
        var customerCreatedEvent = publishedEvent as Platform.Customers.Application.IntegrationEvents.CustomerCreatedIntegrationEvent;
        customerCreatedEvent!.CustomerCode.Should().Be(command.CustomerCode);
        customerCreatedEvent.CompanyName.Should().Be(command.CompanyName);
        customerCreatedEvent.Email.Should().Be(command.Email);
    }

    [Theory]
    [InlineData("", "ENTERPRISE", "Test Company", "John", "Doe", "john@test.com")]
    [InlineData("TEST-001", "", "Test Company", "John", "Doe", "john@test.com")]
    [InlineData("TEST-001", "ENTERPRISE", "", "John", "Doe", "john@test.com")]
    [InlineData("TEST-001", "ENTERPRISE", "Test Company", "", "Doe", "john@test.com")]
    [InlineData("TEST-001", "ENTERPRISE", "Test Company", "John", "", "john@test.com")]
    [InlineData("TEST-001", "ENTERPRISE", "Test Company", "John", "Doe", "")]
    public async Task Handle_InvalidCommand_ValidationShouldCatch(
        string customerCode,
        string customerType,
        string companyName,
        string firstName,
        string lastName,
        string email)
    {
        // Arrange
        var command = CreateValidCreateCommand(
            customerCode: customerCode,
            customerType: customerType,
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
            x => x.AddAsync(It.IsAny<Customer>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_RepositoryThrowsException_PropagatesException()
    {
        // Arrange
        var command = CreateValidCreateCommand();
        var cancellationToken = CancellationToken.None;
        var repositoryException = new InvalidOperationException("Database error");

        // No validator setup needed - real validator will validate
        MockCustomerRepository.Setup(x => x.GetByCustomerCodeAsync(command.CustomerCode, cancellationToken))
            .ReturnsAsync((Customer?)null);
        MockCustomerRepository.Setup(x => x.GetByEmailAsync(command.Email, cancellationToken))
            .ReturnsAsync((Customer?)null);
        MockCustomerRepository.Setup(x => x.AddAsync(It.IsAny<Customer>(), cancellationToken))
            .ThrowsAsync(repositoryException);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(command, cancellationToken));

        exception.Should().Be(repositoryException);
    }
}