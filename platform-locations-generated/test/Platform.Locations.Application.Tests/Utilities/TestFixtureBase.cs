using AutoMapper;
using Platform.Locations.Application.Locations.Commands;
using Platform.Locations.Application.Locations.Dtos;
using Platform.Locations.Application.Profiles;
using Platform.Locations.Domain.Locations;
using Platform.Shared.IntegrationEvents;
using FluentValidation;
using Platform.Shared.DataLayer;
using Platform.Shared.Ddd.Domain.Entities;

namespace Platform.Locations.Application.Tests.Utilities;

public abstract class TestFixtureBase
{
    protected readonly Mock<ILocationRepository> MockLocationRepository;
    protected readonly Mock<IIntegrationEventPublisher> MockEventPublisher;
    protected readonly Mock<IDataFilter<IActivable>> MockActivableDataFilter = new();
    protected readonly IMapper Mapper;
    
    protected TestFixtureBase()
    {
        MockLocationRepository = new Mock<ILocationRepository>();
        MockEventPublisher = new Mock<IIntegrationEventPublisher>();
        
        // Set up AutoMapper with the actual application profiles
        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<AutoMapProfile>();
        });
        Mapper = config.CreateMapper();
    }

    protected static Location CreateTestLocation(
        string locationCode = "LOC-001",
        string locationTypeCode = "WAREHOUSE",
        string addressLine1 = "123 Test Street",
        string? addressLine2 = null,
        string city = "Test City",
        string state = "TX",
        string zipCode = "12345",
        string country = "USA",
        bool isActive = true)
    {
        var location = Location.Create(
            locationCode,
            locationTypeCode,
            addressLine1,
            addressLine2,
            city,
            state,
            zipCode,
            country);
            
        // Set a unique ID for each test location using reflection
        // This is only for testing purposes to simulate different entities
        var idProperty = typeof(Location).GetProperty("Id", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        if (idProperty != null && idProperty.CanWrite)
        {
            idProperty.SetValue(location, Guid.NewGuid());
        }
        
        // Set the activation status
        if (!isActive)
        {
            location.Deactivate();
        }
        
        return location;
    }

    protected static RegisterLocationCommand CreateValidRegisterCommand(
        string locationCode = "LOC-001",
        string locationTypeCode = "WAREHOUSE",
        string addressLine1 = "123 Test Street",
        string? addressLine2 = null,
        string city = "Test City",
        string state = "TX",
        string zipCode = "12345",
        string country = "USA")
    {
        return new RegisterLocationCommand(
            locationCode,
            locationTypeCode,
            addressLine1,
            addressLine2,
            city,
            state,
            zipCode,
            country);
    }

    protected static UpdateLocationAddressCommand CreateValidUpdateAddressCommand(
        string locationCode = "LOC-001",
        string addressLine1 = "456 Updated Street",
        string? addressLine2 = "Suite 200",
        string city = "Updated City",
        string state = "CA",
        string zipCode = "54321",
        string country = "USA")
    {
        return new UpdateLocationAddressCommand(
            locationCode,
            addressLine1,
            addressLine2,
            city,
            state,
            zipCode,
            country);
    }

    protected static ActivateLocationCommand CreateValidActivateCommand(
        string locationCode = "LOC-001")
    {
        return new ActivateLocationCommand(locationCode);
    }

    protected static DeactivateLocationCommand CreateValidDeactivateCommand(
        string locationCode = "LOC-001")
    {
        return new DeactivateLocationCommand(locationCode);
    }

    protected static DeleteLocationCommand CreateValidDeleteCommand(
        string locationCode = "LOC-001")
    {
        return new DeleteLocationCommand(locationCode);
    }

    protected void VerifyRepositoryAddCalledOnce()
    {
        MockLocationRepository.Verify(
            x => x.AddAsync(It.IsAny<Location>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    protected void VerifyRepositoryUpdateCalledOnce()
    {
        MockLocationRepository.Verify(
            x => x.UpdateAsync(It.IsAny<Location>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    protected void VerifyRepositoryDeleteCalledOnce()
    {
        MockLocationRepository.Verify(
            x => x.DeleteAsync(It.IsAny<Location>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    protected void VerifyEventPublisherCalledOnce()
    {
        MockEventPublisher.Verify(
            x => x.SaveIntegrationEvent(It.IsAny<object>()),
            Times.Once);
    }
}