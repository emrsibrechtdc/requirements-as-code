using AutoMapper;
using Platform.Customers.Application.Customers.Commands;
using Platform.Customers.Application.Customers.Dtos;
using Platform.Customers.Application.Profiles;
using Platform.Customers.Domain.Customers;
using Platform.Shared.IntegrationEvents;
using FluentValidation;

namespace Platform.Customers.Application.Tests.Utilities;

public abstract class TestFixtureBase
{
    protected readonly Mock<ICustomerRepository> MockCustomerRepository;
    protected readonly Mock<IIntegrationEventPublisher> MockEventPublisher;
    protected readonly IMapper Mapper;
    
    protected TestFixtureBase()
    {
        MockCustomerRepository = new Mock<ICustomerRepository>();
        MockEventPublisher = new Mock<IIntegrationEventPublisher>();
        
        // Set up AutoMapper with the actual application profiles
        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<AutoMapProfile>();
        });
        Mapper = config.CreateMapper();
    }

    protected static Customer CreateTestCustomer(
        string customerCode = "TEST-001",
        string customerType = "ENTERPRISE",
        string companyName = "Test Company",
        string firstName = "John",
        string lastName = "Doe",
        string email = "john.doe@test.com",
        string? phoneNumber = "555-0123",
        string addressLine1 = "123 Test St",
        string? addressLine2 = null,
        string city = "Test City",
        string state = "TS",
        string postalCode = "12345",
        string country = "USA")
    {
        var customer = Customer.Create(
            customerCode,
            customerType,
            companyName,
            firstName,
            lastName,
            email,
            phoneNumber,
            addressLine1,
            addressLine2,
            city,
            state,
            postalCode,
            country);
            
        // Set a unique ID for each test customer using reflection
        // This is only for testing purposes to simulate different entities
        var idProperty = typeof(Customer).GetProperty("Id", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        if (idProperty != null && idProperty.CanWrite)
        {
            idProperty.SetValue(customer, Guid.NewGuid());
        }
        
        return customer;
    }

    protected static CreateCustomerCommand CreateValidCreateCommand(
        string customerCode = "TEST-001",
        string customerType = "ENTERPRISE",
        string companyName = "Test Company",
        string firstName = "John",
        string lastName = "Doe",
        string email = "john.doe@test.com",
        string? phoneNumber = "555-0123",
        string addressLine1 = "123 Test St",
        string? addressLine2 = null,
        string city = "Test City",
        string state = "TS",
        string postalCode = "12345",
        string country = "USA")
    {
        return new CreateCustomerCommand(
            customerCode,
            customerType,
            companyName,
            firstName,
            lastName,
            email,
            phoneNumber,
            addressLine1,
            addressLine2,
            city,
            state,
            postalCode,
            country);
    }

    protected static UpdateCustomerCommand CreateValidUpdateCommand(
        string customerCode = "TEST-001",
        string companyName = "Updated Company",
        string firstName = "Jane",
        string lastName = "Smith",
        string email = "jane.smith@test.com",
        string? phoneNumber = "555-0124",
        string addressLine1 = "456 Updated St",
        string? addressLine2 = "Suite 100",
        string city = "Updated City",
        string state = "UP",
        string postalCode = "54321",
        string country = "USA")
    {
        return new UpdateCustomerCommand(
            customerCode,
            companyName,
            firstName,
            lastName,
            email,
            phoneNumber,
            addressLine1,
            addressLine2,
            city,
            state,
            postalCode,
            country);
    }

    protected void VerifyRepositoryAddCalledOnce()
    {
        MockCustomerRepository.Verify(
            x => x.AddAsync(It.IsAny<Customer>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    protected void VerifyRepositoryUpdateCalledOnce()
    {
        MockCustomerRepository.Verify(
            x => x.UpdateAsync(It.IsAny<Customer>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    protected void VerifyEventPublisherCalledOnce()
    {
        MockEventPublisher.Verify(
            x => x.SaveIntegrationEvent(It.IsAny<object>()),
            Times.Once);
    }
}