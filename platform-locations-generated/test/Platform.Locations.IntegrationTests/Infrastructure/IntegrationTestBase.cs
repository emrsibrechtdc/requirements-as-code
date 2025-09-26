using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Platform.Locations.Infrastructure.Data;
using Respawn;
using System.Data.Common;
using Microsoft.Data.SqlClient;
using Platform.Locations.Domain.Locations;
using Bogus;
using Platform.Locations.Application.Locations.Commands;
using Platform.Locations.Application.Locations.Dtos;
using Xunit;
using Microsoft.Extensions.Configuration;
using Platform.Locations.Infrastructure.Repositories;
using Platform.Shared.DataLayer;
using Platform.Shared.Ddd.Domain.Entities;

namespace Platform.Locations.IntegrationTests.Infrastructure;

public class IntegrationTestBase : IAsyncLifetime
{
    private readonly string _connectionString;
    protected readonly WebApplicationFactory<Program> Factory;
    protected readonly HttpClient HttpClient;
    protected readonly IServiceScope ServiceScope;
    protected readonly LocationsDbContext DbContext;
    private ILocationRepository _locationRepository;
    private IDataFilter<IActivable> _activableDataFilter;
    
    private DbConnection _dbConnection = null!;
    private Respawner _respawner = null!;

    public IntegrationTestBase()
    {
        // Get connection string from configuration
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .Build();
        
        _connectionString = configuration.GetConnectionString("LocationsDb") 
            ?? "Server=.;Database=Platform.Locations.IntegrationTests;Integrated Security=true;Trust Server Certificate=true;";

        // Create WebApplicationFactory with test configuration
        Factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Development");
                
                builder.ConfigureAppConfiguration((context, configBuilder) =>
                {
                    configBuilder.AddJsonFile("appsettings.json", optional: true);
                });
                
                builder.ConfigureServices(services =>
                {
                    // Remove the existing DbContext registration
                    services.RemoveAll(typeof(DbContextOptions<LocationsDbContext>));
                    services.RemoveAll(typeof(LocationsDbContext));

                    // Add test database configuration
                    services.AddDbContext<LocationsDbContext>(options =>
                    {
                        options.UseSqlServer(_connectionString);
                        options.EnableSensitiveDataLogging();
                        options.EnableDetailedErrors();
                    });
                });
            });

        HttpClient = Factory.CreateClient();
        HttpClient.DefaultRequestHeaders.Add("local-product", "TEST_PRODUCT");
        ServiceScope = Factory.Services.CreateScope();
        _activableDataFilter = ServiceScope.ServiceProvider.GetRequiredService<IDataFilter<IActivable>>();
        DbContext = ServiceScope.ServiceProvider.GetRequiredService<LocationsDbContext>();
        _locationRepository = new LocationRepository(DbContext);
    }

    public async Task InitializeAsync()
    {
        // Initialize database connection
        _dbConnection = new SqlConnection(_connectionString);
        await _dbConnection.OpenAsync();
        
        // Initialize Respawner for database cleanup between tests
        _respawner = await Respawner.CreateAsync(_dbConnection, new RespawnerOptions
        {
            SchemasToInclude = ["dbo"],
            DbAdapter = DbAdapter.SqlServer
        });
    }

    public async Task DisposeAsync()
    {
        await _dbConnection.CloseAsync();
        await _dbConnection.DisposeAsync();
        ServiceScope.Dispose();
        HttpClient.Dispose();
        Factory.Dispose();
    }

    protected async Task ResetDatabaseAsync()
    {
        await _respawner.ResetAsync(_dbConnection);
    }

    #region Test Data Factories

    protected static readonly Faker<RegisterLocationCommand> RegisterLocationCommandFaker = new Faker<RegisterLocationCommand>()
        .CustomInstantiator(f => new RegisterLocationCommand(
            f.Random.AlphaNumeric(10).ToUpper(),
            f.PickRandom("WAREHOUSE", "STORE", "OFFICE", "FACTORY"),
            f.Address.StreetAddress(),
            f.Address.SecondaryAddress(),
            f.Address.City(),
            f.Address.StateAbbr(),
            f.Address.ZipCode(),
            f.Address.CountryCode(),
            null, // Latitude (optional)
            null, // Longitude (optional)
            null  // GeofenceRadius (optional)
        ));

    protected static readonly Faker<UpdateLocationAddressCommand> UpdateAddressCommandFaker = new Faker<UpdateLocationAddressCommand>()
        .CustomInstantiator(f => new UpdateLocationAddressCommand(
            "LOC-001", // Will be overridden in tests
            f.Address.StreetAddress(),
            f.Address.SecondaryAddress(),
            f.Address.City(),
            f.Address.StateAbbr(),
            f.Address.ZipCode(),
            f.Address.CountryCode()
        ));

    protected static RegisterLocationCommand CreateValidRegisterCommand(string? locationCode = null)
    {
        var command = RegisterLocationCommandFaker.Generate();
        if (locationCode != null)
        {
            return command with { LocationCode = locationCode };
        }
        return command;
    }

    protected static UpdateLocationAddressCommand CreateValidUpdateAddressCommand(string locationCode)
    {
        var command = UpdateAddressCommandFaker.Generate();
        return command with { LocationCode = locationCode };
    }

    protected static ActivateLocationCommand CreateActivateCommand(string locationCode)
    {
        return new ActivateLocationCommand(locationCode);
    }

    protected static DeactivateLocationCommand CreateDeactivateCommand(string locationCode)
    {
        return new DeactivateLocationCommand(locationCode);
    }

    protected static DeleteLocationCommand CreateDeleteCommand(string locationCode)
    {
        return new DeleteLocationCommand(locationCode);
    }

    protected async Task<Location> SeedLocationInDatabaseAsync(
        string locationCode = "TEST-LOC-001",
        string locationTypeCode = "WAREHOUSE",
        bool isActive = true)
    {
        // Use the registration endpoint instead of direct database seeding
        // This ensures the multi-product functionality is properly applied
        var registerCommand = new RegisterLocationCommand(
            locationCode,
            locationTypeCode,
            "123 Test Street",
            "Suite 100",
            "Test City",
            "TX",
            "12345",
            "USA",
            null, // Latitude
            null, // Longitude
            null  // GeofenceRadius
        );

        var registerResponse = await PostJsonAsync("/locations/register", registerCommand);
        registerResponse.EnsureSuccessStatusCode();

        // Get the created location from the database
        var location = await FindLocationByCodeAsync(locationCode);
        if (location == null)
        {
            throw new InvalidOperationException($"Location {locationCode} was not found after registration");
        }

        // If we need the location to be inactive, deactivate it
        if (!isActive)
        {
            var deactivateResponse = await HttpClient.PutAsync($"/locations/{locationCode}/deactivate", null);
            deactivateResponse.EnsureSuccessStatusCode();
        }

        return location!;
    }

    protected async Task<List<Location>> SeedMultipleLocationsAsync(int count)
    {
        var locations = new List<Location>();
        
        for (int i = 1; i <= count; i++)
        {
            var locationCode = $"TEST-LOC-{i:000}";
            var location = await SeedLocationInDatabaseAsync(locationCode, "WAREHOUSE", true);
            locations.Add(location);
        }

        return locations;
    }

    protected async Task<List<Location>> SeedMultipleUniqueLocationsAsync(int count)
    {
        var locations = new List<Location>();
        var baseTicks = DateTime.Now.Ticks;
        
        for (int i = 1; i <= count; i++)
        {
            var locationCode = $"UNIQUE-{baseTicks}-{i:000}";
            var location = await SeedLocationInDatabaseAsync(locationCode, "WAREHOUSE", true);
            locations.Add(location);
        }

        return locations;
    }

    protected async Task<List<Location>> SeedMultipleLocationsWithPrefixAsync(int count, string prefix)
    {
        var locations = new List<Location>();
        
        for (int i = 1; i <= count; i++)
        {
            var locationCode = $"{prefix}-{i:000}";
            var location = await SeedLocationInDatabaseAsync(locationCode, "WAREHOUSE", true);
            locations.Add(location);
        }

        return locations;
    }

    protected static AddressDto CreateValidAddressDto()
    {
        var faker = new Faker();
        return new AddressDto(
            faker.Address.StreetAddress(),
            faker.Address.SecondaryAddress(),
            faker.Address.City(),
            faker.Address.StateAbbr(),
            faker.Address.ZipCode(),
            faker.Address.CountryCode()
        );
    }

    #endregion

    #region HTTP Helper Methods

    protected async Task<T?> GetJsonAsync<T>(string requestUri)
    {
        var response = await HttpClient.GetAsync(requestUri);
        var content = await response.Content.ReadAsStringAsync();
        
        if (string.IsNullOrEmpty(content))
            return default;
            
        return System.Text.Json.JsonSerializer.Deserialize<T>(content, new System.Text.Json.JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
    }

    protected async Task<HttpResponseMessage> PostJsonAsync<T>(string requestUri, T data)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(data);
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
        return await HttpClient.PostAsync(requestUri, content);
    }

    protected async Task<HttpResponseMessage> PutJsonAsync<T>(string requestUri, T data)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(data);
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
        return await HttpClient.PutAsync(requestUri, content);
    }

    #endregion

    #region Database Helper Methods

    protected async Task<Location?> FindLocationByCodeAsync(string locationCode)
    {
        return await DbContext.Locations
            .FirstOrDefaultAsync(l => l.LocationCode == locationCode);

    }

    protected async Task<int> GetLocationCountAsync()
    {
        return await DbContext.Locations.CountAsync();
    }

    protected async Task<List<Location>> GetAllLocationsAsync()
    {
        return await DbContext.Locations.ToListAsync();
    }

    #endregion
}