using AutoMapper;
using Moq;
using Platform.Locations.Application.Locations.Dtos;
using Platform.Locations.Application.Locations.Queries;
using Platform.Locations.Domain.Locations;
using Xunit;

namespace Platform.Locations.Application.Tests.Locations.Queries;

public class GetLocationByCoordinatesQueryHandlerTests
{
    private readonly Mock<ILocationRepository> _mockRepository;
    private readonly Mock<IMapper> _mockMapper;
    private readonly GetLocationByCoordinatesQueryHandler _handler;

    public GetLocationByCoordinatesQueryHandlerTests()
    {
        _mockRepository = new Mock<ILocationRepository>();
        _mockMapper = new Mock<IMapper>();
        _handler = new GetLocationByCoordinatesQueryHandler(_mockRepository.Object, _mockMapper.Object);
    }

    [Fact]
    public async Task Handle_LocationFound_ReturnsLocationDto()
    {
        // Arrange
        var query = new GetLocationByCoordinatesQuery(41.8781m, -87.6298m);
        var location = Location.Create("LOC001", "WAREHOUSE", "123 Main St", null, "Chicago", "IL", "60601", "USA");
        location.SetCoordinates(41.8781m, -87.6298m, 100.0);
        
        var expectedDto = new LocationDto(
            location.Id,
            "LOC001",
            "WAREHOUSE", 
            null,
            "123 Main St",
            null,
            "Chicago",
            "IL",
            "60601",
            "USA",
            true,
            DateTimeOffset.UtcNow,
            null,
            null,
            null,
            41.8781m,
            -87.6298m,
            100.0
        );

        _mockRepository
            .Setup(x => x.GetLocationByCoordinatesAsync(query.Latitude, query.Longitude, It.IsAny<CancellationToken>()))
            .ReturnsAsync(location);

        _mockMapper
            .Setup(x => x.Map<LocationDto>(location))
            .Returns(expectedDto);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedDto.LocationCode, result.LocationCode);
        Assert.Equal(expectedDto.Latitude, result.Latitude);
        Assert.Equal(expectedDto.Longitude, result.Longitude);
    }

    [Fact]
    public async Task Handle_LocationNotFound_ReturnsNull()
    {
        // Arrange
        var query = new GetLocationByCoordinatesQuery(41.8781m, -87.6298m);

        _mockRepository
            .Setup(x => x.GetLocationByCoordinatesAsync(query.Latitude, query.Longitude, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Location?)null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task Handle_RepositoryThrowsException_PropagatesException()
    {
        // Arrange
        var query = new GetLocationByCoordinatesQuery(41.8781m, -87.6298m);

        _mockRepository
            .Setup(x => x.GetLocationByCoordinatesAsync(query.Latitude, query.Longitude, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _handler.Handle(query, CancellationToken.None));
    }
}