using Platform.Locations.Domain.Locations;
using Xunit;

namespace Platform.Locations.Application.Tests.Locations.Domain;

public class LocationCoordinateTests
{
    [Fact]
    public void SetCoordinates_ValidInput_SetsCoordinatesCorrectly()
    {
        // Arrange
        var location = Location.Create("LOC001", "WAREHOUSE", "123 Main St", null, "Chicago", "IL", "60601", "USA");
        var latitude = 41.8781m;
        var longitude = -87.6298m;
        var geofenceRadius = 100.0;

        // Act
        location.SetCoordinates(latitude, longitude, geofenceRadius);

        // Assert
        Assert.Equal(latitude, location.Latitude);
        Assert.Equal(longitude, location.Longitude);
        Assert.Equal(geofenceRadius, location.GeofenceRadius);
        Assert.True(location.HasCoordinates);
        Assert.True(location.HasGeofence);
    }

    [Fact]
    public void SetCoordinates_InvalidLatitude_ThrowsArgumentException()
    {
        // Arrange
        var location = Location.Create("LOC001", "WAREHOUSE", "123 Main St", null, "Chicago", "IL", "60601", "USA");
        var invalidLatitude = 91.0m; // Invalid latitude > 90
        var longitude = -87.6298m;

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => location.SetCoordinates(invalidLatitude, longitude));
        Assert.Contains("Latitude must be between -90 and +90 degrees", exception.Message);
    }

    [Fact]
    public void SetCoordinates_InvalidLongitude_ThrowsArgumentException()
    {
        // Arrange
        var location = Location.Create("LOC001", "WAREHOUSE", "123 Main St", null, "Chicago", "IL", "60601", "USA");
        var latitude = 41.8781m;
        var invalidLongitude = 181.0m; // Invalid longitude > 180

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => location.SetCoordinates(latitude, invalidLongitude));
        Assert.Contains("Longitude must be between -180 and +180 degrees", exception.Message);
    }

    [Fact]
    public void SetCoordinates_InvalidGeofenceRadius_ThrowsArgumentException()
    {
        // Arrange
        var location = Location.Create("LOC001", "WAREHOUSE", "123 Main St", null, "Chicago", "IL", "60601", "USA");
        var latitude = 41.8781m;
        var longitude = -87.6298m;
        var invalidRadius = -50.0; // Invalid negative radius

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => location.SetCoordinates(latitude, longitude, invalidRadius));
        Assert.Contains("Geofence radius must be positive", exception.Message);
    }

    [Fact]
    public void ClearCoordinates_RemovesAllCoordinateData()
    {
        // Arrange
        var location = Location.Create("LOC001", "WAREHOUSE", "123 Main St", null, "Chicago", "IL", "60601", "USA");
        location.SetCoordinates(41.8781m, -87.6298m, 100.0);

        // Act
        location.ClearCoordinates();

        // Assert
        Assert.Null(location.Latitude);
        Assert.Null(location.Longitude);
        Assert.Null(location.GeofenceRadius);
        Assert.False(location.HasCoordinates);
        Assert.False(location.HasGeofence);
    }

    [Fact]
    public void ApproximateDistanceTo_WithValidCoordinates_CalculatesDistance()
    {
        // Arrange
        var location = Location.Create("LOC001", "WAREHOUSE", "123 Main St", null, "Chicago", "IL", "60601", "USA");
        location.SetCoordinates(41.8781m, -87.6298m); // Chicago coordinates
        
        var targetLatitude = 41.8819m; // Slightly north
        var targetLongitude = -87.6278m; // Slightly east

        // Act
        var distance = location.ApproximateDistanceTo(targetLatitude, targetLongitude);

        // Assert
        Assert.True(distance > 0);
        Assert.True(distance < 1000); // Should be less than 1km for such close coordinates
    }

    [Fact]
    public void ApproximateDistanceTo_WithoutCoordinates_ThrowsInvalidOperationException()
    {
        // Arrange
        var location = Location.Create("LOC001", "WAREHOUSE", "123 Main St", null, "Chicago", "IL", "60601", "USA");
        // No coordinates set

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => 
            location.ApproximateDistanceTo(41.8781m, -87.6298m));
        Assert.Contains("Location does not have coordinates", exception.Message);
    }

    [Fact]
    public void HasCoordinates_WithBothLatitudeAndLongitude_ReturnsTrue()
    {
        // Arrange
        var location = Location.Create("LOC001", "WAREHOUSE", "123 Main St", null, "Chicago", "IL", "60601", "USA");
        location.SetCoordinates(41.8781m, -87.6298m);

        // Act & Assert
        Assert.True(location.HasCoordinates);
    }

    [Fact]
    public void HasGeofence_WithCoordinatesAndRadius_ReturnsTrue()
    {
        // Arrange
        var location = Location.Create("LOC001", "WAREHOUSE", "123 Main St", null, "Chicago", "IL", "60601", "USA");
        location.SetCoordinates(41.8781m, -87.6298m, 100.0);

        // Act & Assert
        Assert.True(location.HasGeofence);
    }

    [Fact]
    public void HasGeofence_WithCoordinatesButNoRadius_ReturnsFalse()
    {
        // Arrange
        var location = Location.Create("LOC001", "WAREHOUSE", "123 Main St", null, "Chicago", "IL", "60601", "USA");
        location.SetCoordinates(41.8781m, -87.6298m); // No radius specified

        // Act & Assert
        Assert.False(location.HasGeofence);
    }

    [Fact]
    public void ToInstrumentationProperties_WithCoordinates_IncludesCoordinateFields()
    {
        // Arrange
        var location = Location.Create("LOC001", "WAREHOUSE", "123 Main St", null, "Chicago", "IL", "60601", "USA");
        location.SetCoordinates(41.8781m, -87.6298m, 100.0);

        // Act
        var properties = location.ToInstrumentationProperties();

        // Assert
        Assert.Contains("latitude", properties.Keys);
        Assert.Contains("longitude", properties.Keys);
        Assert.Contains("geofenceRadius", properties.Keys);
        Assert.Equal("41.8781", properties["latitude"]);
        Assert.Equal("-87.6298", properties["longitude"]);
        Assert.Equal("100", properties["geofenceRadius"]);
    }
}