# Platform.Customers.Database

This is the database project for the Platform.Customers service, using MSBuild.Sdk.SqlProj for database schema management.

## Overview

This project uses a **database-first** approach with SQL Server Database Projects instead of Entity Framework migrations. This provides:

- **Better version control** of database schema
- **Declarative database state** management
- **Professional database deployment** pipeline
- **Schema drift detection**
- **Rollback capabilities**

## Project Structure

```
Platform.Customers.Database/
├── Tables/
│   └── Customers.sql              # Main customers table definition
├── Data/
│   ├── SampleCustomers.sql        # Sample data for development
│   └── PostDeployment.sql         # Post-deployment script
└── README.md                      # This file
```

## Building the Database Project

```bash
# Build the database project (creates DACPAC)
dotnet build src/Database/Platform.Customers.Database/Platform.Customers.Database.sqlproj

# The output DACPAC will be in:
# src/Database/Platform.Customers.Database/bin/Debug/Platform.Customers.Database.dacpac
```

## Local Development Deployment

### Using dotnet CLI (Recommended)
```bash
# Install SqlPackage tool (if not already installed)
dotnet tool install -g microsoft.sqlpackage

# Deploy to local database
SqlPackage.exe /Action:Publish /SourceFile:"src/Database/Platform.Customers.Database/bin/Debug/Platform.Customers.Database.dacpac" /TargetServerName:"(localdb)\mssqllocaldb" /TargetDatabaseName:"Platform.Customers"
```

### Using Visual Studio
1. Right-click the database project in Solution Explorer
2. Select "Publish..."
3. Configure target database connection
4. Click "Publish"

## Database Schema

### Customers Table
- **Primary Key**: `Id` (uniqueidentifier with `NEWSEQUENTIALID()` default)
- **Multi-Product Support**: `Product` (nvarchar(50))
- **Business Key**: `CustomerCode` (nvarchar(50))
- **Customer Type**: `CustomerType` (nvarchar(20))
- **Company Information**: CompanyName, ContactFirstName, ContactLastName
- **Contact Information**: Email, PhoneNumber
- **Address Fields**: AddressLine1, AddressLine2, City, State, PostalCode, Country
- **Soft Delete**: `IsActive` (bit), `DeletedAt`, `DeletedBy`
- **Audit Fields**: CreatedAt, CreatedBy, UpdatedAt, UpdatedBy

**Performance Note**: The primary key uses `NEWSEQUENTIALID()` instead of `NEWID()` to generate sequential GUIDs, which provides:
- Better clustered index performance (reduces page splits)
- Improved storage efficiency
- Better query performance on indexed lookups
- Reduced index fragmentation over time

### Indexes
- **Primary Access Pattern**: `IX_Customers_Product_CustomerCode`
- **Active Customer Filtering**: `IX_Customers_Product_IsActive`
- **Customer Type Filtering**: `IX_Customers_Product_CustomerType`
- **Email Lookup**: `IX_Customers_Product_Email`
- **Company Search**: `IX_Customers_Product_CompanyName`
- **Unique Constraint**: `UX_Customers_Product_CustomerCode` (within product, excluding deleted)

## Sample Data

The sample data script includes test customers for development environments using `NEWSEQUENTIALID()` for optimal database performance.

Run the sample data script after deployment:

```sql
-- Execute the sample data script
USE Platform.Customers;
GO
-- Run: src/Database/Platform.Customers.Database/Data/SampleCustomers.sql
```

**Note**: Sample data uses `NEWSEQUENTIALID()` for Id generation to demonstrate production-ready performance patterns.

## Connection String

For local development:
```
Server=(localdb)\mssqllocaldb;Database=Platform.Customers;Trusted_Connection=true;MultipleActiveResultSets=true
```

## Integration with Entity Framework

The Entity Framework DbContext in the Infrastructure project is configured to work with this database schema without migrations:

```csharp
// In CustomersDbContext - no migrations needed
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