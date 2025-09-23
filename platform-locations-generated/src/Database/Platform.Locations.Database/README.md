# Platform.Locations.Database

This is the database project for the Platform.Locations service, using MSBuild.Sdk.SqlProj for database schema management.

## Overview

This project uses a **database-first** approach with SQL Server Database Projects instead of Entity Framework migrations. This provides:

- **Better version control** of database schema
- **Declarative database state** management
- **Professional database deployment** pipeline
- **Schema drift detection**
- **Rollback capabilities**

## Project Structure

```
Platform.Locations.Database/
├── Tables/
│   └── Locations.sql          # Main locations table definition
├── Data/
│   └── SampleLocations.sql    # Sample data for development
└── README.md                  # This file
```

## Building the Database Project

```bash
# Build the database project (creates DACPAC)
dotnet build src/Database/Platform.Locations.Database/Platform.Locations.Database.sqlproj

# The output DACPAC will be in:
# src/Database/Platform.Locations.Database/bin/Debug/Platform.Locations.Database.dacpac
```

## Local Development Deployment

### Using dotnet CLI (Recommended)
```bash
# Install SqlPackage tool (if not already installed)
dotnet tool install -g microsoft.sqlpackage

# Deploy to local database
SqlPackage.exe /Action:Publish /SourceFile:"src/Database/Platform.Locations.Database/bin/Debug/Platform.Locations.Database.dacpac" /TargetServerName:"(localdb)\mssqllocaldb" /TargetDatabaseName:"Platform.Locations"
```

### Using Visual Studio
1. Right-click the database project in Solution Explorer
2. Select "Publish..."
3. Configure target database connection
4. Click "Publish"

## Database Schema

### Locations Table
- **Primary Key**: `Id` (uniqueidentifier with `NEWSEQUENTIALID()` default)
- **Multi-Product Support**: `Product` (nvarchar(50))
- **Business Key**: `LocationCode` (nvarchar(50))
- **Location Type**: `LocationTypeCode` (nvarchar(20))
- **Address Fields**: AddressLine1, AddressLine2, City, State, ZipCode, Country
- **Soft Delete**: `IsActive` (bit), `DeletedAt`, `DeletedBy`
- **Audit Fields**: CreatedAt, CreatedBy, UpdatedAt, UpdatedBy

**Performance Note**: The primary key uses `NEWSEQUENTIALID()` instead of `NEWID()` to generate sequential GUIDs, which provides:
- Better clustered index performance (reduces page splits)
- Improved storage efficiency
- Better query performance on indexed lookups
- Reduced index fragmentation over time

### Indexes
- **Primary Access Pattern**: `IX_Locations_Product_LocationCode`
- **Active Location Filtering**: `IX_Locations_Product_IsActive`
- **Location Type Filtering**: `IX_Locations_Product_LocationTypeCode`
- **Unique Constraint**: `UX_Locations_Product_LocationCode` (within product, excluding deleted)

## Sample Data

The sample data script includes test locations for development environments using `NEWSEQUENTIALID()` for optimal database performance.

Run the sample data script after deployment:

```sql
-- Execute the sample data script
USE Platform.Locations;
GO
-- Run: src/Database/Platform.Locations.Database/Data/SampleLocations.sql
```

**Note**: Sample data uses `NEWSEQUENTIALID()` for Id generation to demonstrate production-ready performance patterns.

## Connection String

For local development:
```
Server=(localdb)\mssqllocaldb;Database=Platform.Locations;Trusted_Connection=true;MultipleActiveResultSets=true
```

## Integration with Entity Framework

The Entity Framework DbContext in the Infrastructure project is configured to work with this database schema without migrations:

```csharp
// In LocationsDbContext - no migrations needed
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    // Configure entities to match database schema
    // Database schema is managed by this SQL project
}
```

## Deployment Pipeline

For CI/CD pipelines, the DACPAC can be deployed using:
- Azure DevOps SqlAzureDacpacDeployment task
- GitHub Actions with SqlPackage
- PowerShell/bash scripts with SqlPackage CLI

## Best Practices

1. **Always build locally** before committing changes
2. **Test schema changes** against sample data
3. **Use descriptive names** for database objects
4. **Include appropriate indexes** for query patterns
5. **Follow naming conventions** (PascalCase for objects, lowercase for keywords)