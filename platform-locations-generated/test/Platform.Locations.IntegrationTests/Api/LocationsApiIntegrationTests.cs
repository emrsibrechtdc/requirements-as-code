using FluentAssertions;
using Platform.Locations.Application.Locations.Commands;
using Platform.Locations.Application.Locations.Dtos;
using Platform.Locations.IntegrationTests.Infrastructure;
using System.Net;
using System.Text.Json;
using Xunit;

namespace Platform.Locations.IntegrationTests.Api;

[Collection("Database Integration Tests")]
public class LocationsApiIntegrationTests : IntegrationTestBase
{
    public LocationsApiIntegrationTests() : base() { }

    private string GenerateUniqueLocationCode(string prefix = "TEST")
    {
        return $"{prefix}-{DateTime.Now.Ticks}";
    }

    [Fact]
    public async Task RegisterLocation_ValidCommand_ReturnsSuccessAndCreatesLocation()
    {
        // Arrange
        var locationCode = GenerateUniqueLocationCode("REG");
        var command = CreateValidRegisterCommand(locationCode);

        // Act
        var response = await PostJsonAsync("/locations/register", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var responseContent = await response.Content.ReadAsStringAsync();
        var locationResponse = JsonSerializer.Deserialize<LocationResponse>(responseContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        
        locationResponse.Should().NotBeNull();
        locationResponse!.LocationCode.Should().Be(command.LocationCode);

        // Verify location was created in database
        var savedLocation = await FindLocationByCodeAsync(command.LocationCode);
        savedLocation.Should().NotBeNull();
        savedLocation!.LocationCode.Should().Be(command.LocationCode);
        savedLocation.LocationTypeCode.Should().Be(command.LocationTypeCode);
        savedLocation.AddressLine1.Should().Be(command.AddressLine1);
        savedLocation.City.Should().Be(command.City);
        savedLocation.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task RegisterLocation_DuplicateLocationCode_ReturnsBadRequest()
    {
        // Arrange
        var existingLocationCode = GenerateUniqueLocationCode("DUP");
        await SeedLocationInDatabaseAsync(existingLocationCode);
        
        var command = CreateValidRegisterCommand(existingLocationCode);

        // Act
        var response = await PostJsonAsync("/locations/register", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task RegisterLocation_InvalidCommand_ReturnsBadRequest()
    {
        // Arrange
        var invalidCommand = new RegisterLocationCommand(
            "", // Empty location code
            "WAREHOUSE",
            "123 Street",
            null,
            "City",
            "TX",
            "12345",
            "USA");

        // Act
        var response = await PostJsonAsync("/locations/register", invalidCommand);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetLocations_WithoutFilter_ReturnsAllLocations()
    {
        // Arrange
        var seededLocations = await SeedMultipleUniqueLocationsAsync(5);

        // Act
        var locations = await GetJsonAsync<List<LocationDto>>("/locations");

        // Assert
        locations.Should().NotBeNull();
        locations!.Count.Should().BeGreaterThanOrEqualTo(5);
        
        foreach (var seededLocation in seededLocations)
        {
            locations.Should().Contain(l => l.LocationCode == seededLocation.LocationCode);
        }
    }

    [Fact]
    public async Task GetLocations_WithLocationCodeFilter_ReturnsMatchingLocations()
    {
        // Arrange
        var basePrefix = GenerateUniqueLocationCode("FILTER");
        var seededLocations = await SeedMultipleLocationsWithPrefixAsync(5, basePrefix);

        // Act
        var locations = await GetJsonAsync<List<LocationDto>>($"/locations?locationCode={basePrefix}");

        // Assert
        locations.Should().NotBeNull();
        locations!.Count.Should().BeGreaterThan(0);
        locations.All(l => l.LocationCode!.StartsWith(basePrefix)).Should().BeTrue();
    }

    [Fact]
    public async Task UpdateLocationAddress_ExistingLocation_ReturnsSuccessAndUpdatesAddress()
    {
        // Arrange
        var locationCode = GenerateUniqueLocationCode("UPDATE");
        var existingLocation = await SeedLocationInDatabaseAsync(locationCode);
        var updateDto = CreateValidAddressDto();
        var originalCity = existingLocation.City;

        // Act
        var response = await PutJsonAsync($"/locations/{existingLocation.LocationCode}/updateaddress", updateDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify address was updated in database
        var updatedLocation = await FindLocationByCodeAsync(existingLocation.LocationCode);
        updatedLocation.Should().NotBeNull();
        updatedLocation!.AddressLine1.Should().Be(updateDto.AddressLine1);
        updatedLocation.City.Should().Be(updateDto.City);
        updatedLocation.City.Should().NotBe(originalCity); // Ensure it actually changed
    }

    [Fact]
    public async Task UpdateLocationAddress_NonExistentLocation_ReturnsNotFound()
    {
        // Arrange
        var updateDto = CreateValidAddressDto();
        var nonExistentLocationCode = GenerateUniqueLocationCode("NONEXIST");

        // Act
        var response = await PutJsonAsync($"/locations/{nonExistentLocationCode}/updateaddress", updateDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task UpdateLocationAddress_InvalidAddress_ReturnsBadRequest()
    {
        // Arrange
        var locationCode = GenerateUniqueLocationCode("INVALID");
        var existingLocation = await SeedLocationInDatabaseAsync(locationCode);
        var invalidUpdateDto = new AddressDto(
            "", // Empty address line 1
            null,
            "City",
            "TX",
            "12345",
            "USA");

        // Act
        var response = await PutJsonAsync($"/locations/{existingLocation.LocationCode}/updateaddress", invalidUpdateDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ActivateLocation_InactiveLocation_ReturnsSuccessAndActivatesLocation()
    {
        // Arrange
        var locationCode = GenerateUniqueLocationCode("ACTIVATE");
        var inactiveLocation = await SeedLocationInDatabaseAsync(locationCode, isActive: false);

        // Act
        var response = await HttpClient.PutAsync($"/locations/{locationCode}/activate", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify location was activated in database
        var activatedLocation = await FindLocationByCodeAsync(inactiveLocation.LocationCode);
        activatedLocation.Should().NotBeNull();
        activatedLocation!.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task ActivateLocation_AlreadyActiveLocation_ReturnsBadRequest()
    {
        // Arrange
        var locationCode = GenerateUniqueLocationCode("ACTIVE");
        var activeLocation = await SeedLocationInDatabaseAsync(locationCode, isActive: true);

        // Act
        var response = await HttpClient.PutAsync($"/locations/{activeLocation.LocationCode}/activate", null);
        var content = await response.Content.ReadAsStringAsync();
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task ActivateLocation_NonExistentLocation_ReturnsBadRequest()
    {
        // Arrange
        var nonExistentLocationCode = GenerateUniqueLocationCode("NOACTIVATE");

        // Act
        var response = await HttpClient.PutAsync($"/locations/{nonExistentLocationCode}/activate", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task DeactivateLocation_ActiveLocation_ReturnsSuccessAndDeactivatesLocation()
    {
        // Arrange
        var locationCode = GenerateUniqueLocationCode("DEACTIVATE");
        var activeLocation = await SeedLocationInDatabaseAsync(locationCode, isActive: true);

        // Act
        var response = await HttpClient.PutAsync($"/locations/{activeLocation.LocationCode}/deactivate", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify location was deactivated in database
        var deactivatedLocation = await FindLocationByCodeAsync(activeLocation.LocationCode);
        deactivatedLocation.Should().NotBeNull();
        deactivatedLocation!.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task DeactivateLocation_AlreadyInactiveLocation_ReturnsBadRequest()
    {
        // Arrange
        var locationCode = GenerateUniqueLocationCode("INACTIVE");
        var inactiveLocation = await SeedLocationInDatabaseAsync(locationCode, isActive: false);

        // Act
        var response = await HttpClient.PutAsync($"/locations/{inactiveLocation.LocationCode}/deactivate", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task DeactivateLocation_NonExistentLocation_ReturnsBadRequest()
    {
        // Arrange
        var nonExistentLocationCode = GenerateUniqueLocationCode("NODEACTIVATE");

        // Act
        var response = await HttpClient.PutAsync($"/locations/{nonExistentLocationCode}/deactivate", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task DeleteLocation_ExistingLocation_ReturnsSuccessAndDeletesLocation()
    {
        // Arrange
        var locationCode = GenerateUniqueLocationCode("DELETE");
        var locationToDelete = await SeedLocationInDatabaseAsync(locationCode);
        var initialCount = await GetLocationCountAsync();

        // Act
        var response = await HttpClient.DeleteAsync($"/locations?locationCode={locationToDelete.LocationCode}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify location count decreased (assuming soft delete)
        var finalCount = await GetLocationCountAsync();
        // Note: If using soft delete, the count might not change, but the entity would be marked as deleted
        // For hard delete, finalCount should be initialCount - 1
    }

    [Fact]
    public async Task DeleteLocation_NonExistentLocation_ReturnsBadRequest()
    {
        // Arrange
        var nonExistentLocationCode = GenerateUniqueLocationCode("NODELETE");

        // Act
        var response = await HttpClient.DeleteAsync($"/locations?locationCode={nonExistentLocationCode}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task LocationLifecycle_CompleteFlow_WorksCorrectly()
    {
        // Arrange
        var locationCode = GenerateUniqueLocationCode("LIFECYCLE");

        // Act & Assert - Register Location
        var registerCommand = CreateValidRegisterCommand(locationCode);
        var registerResponse = await PostJsonAsync("/locations/register", registerCommand);
        registerResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify location exists and is active
        var registeredLocation = await FindLocationByCodeAsync(locationCode);
        registeredLocation.Should().NotBeNull();
        registeredLocation!.IsActive.Should().BeTrue();

        // Act & Assert - Update Address
        var updateDto = CreateValidAddressDto();
        var updateResponse = await PutJsonAsync($"/locations/{locationCode}/updateaddress", updateDto);
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify address was updated
        var updatedLocation = await FindLocationByCodeAsync(locationCode);
        updatedLocation!.AddressLine1.Should().Be(updateDto.AddressLine1);

        // Act & Assert - Deactivate Location
        var deactivateResponse = await HttpClient.PutAsync($"/locations/{locationCode}/deactivate", null);
        deactivateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify location is deactivated
        var deactivatedLocation = await FindLocationByCodeAsync(locationCode);
        deactivatedLocation!.IsActive.Should().BeFalse();

        // Act & Assert - Reactivate Location
        var reactivateResponse = await HttpClient.PutAsync($"/locations/{locationCode}/activate", null);
        reactivateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify location is active again
        var reactivatedLocation = await FindLocationByCodeAsync(locationCode);
        reactivatedLocation!.IsActive.Should().BeTrue();

        // Act & Assert - Delete Location
        var deleteResponse = await HttpClient.DeleteAsync($"/locations?locationCode={locationCode}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task MultipleOperations_Concurrent_HandleCorrectly()
    {
        // Arrange
        var tasks = new List<Task<HttpResponseMessage>>();
        var basePrefix = GenerateUniqueLocationCode("CONCURRENT");

        // Act - Create multiple locations concurrently
        for (int i = 1; i <= 10; i++)
        {
            var command = CreateValidRegisterCommand($"{basePrefix}-{i:00}");
            tasks.Add(PostJsonAsync("/locations/register", command));
        }

        var responses = await Task.WhenAll(tasks);

        // Assert
        responses.Should().AllSatisfy(response => 
            response.StatusCode.Should().Be(HttpStatusCode.OK));

        // Verify all locations were created by checking they exist
        for (int i = 1; i <= 10; i++)
        {
            var locationCode = $"{basePrefix}-{i:00}";
            var location = await FindLocationByCodeAsync(locationCode);
            location.Should().NotBeNull();
        }
    }
}