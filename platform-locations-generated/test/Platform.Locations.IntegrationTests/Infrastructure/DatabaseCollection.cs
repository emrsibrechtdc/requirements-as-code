using Xunit;

namespace Platform.Locations.IntegrationTests.Infrastructure;

/// <summary>
/// Collection definition to ensure integration tests that use the database run sequentially.
/// This prevents race conditions when tests reset the database.
/// </summary>
[CollectionDefinition("Database Integration Tests", DisableParallelization = true)]
public class DatabaseCollection : ICollectionFixture<IntegrationTestBase>
{
    // This class is never instantiated. It just serves as the marker for the collection.
    // The ICollectionFixture<IntegrationTestBase> tells xUnit to use IntegrationTestBase
    // as the fixture for all tests in this collection.
}