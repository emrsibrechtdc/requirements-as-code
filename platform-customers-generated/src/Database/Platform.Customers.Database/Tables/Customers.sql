-- Customers table definition with Platform.Shared integration
-- Uses NEWSEQUENTIALID() for optimal clustering performance
CREATE TABLE [dbo].[Customers]
(
    [Id] UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID(),
    [Product] NVARCHAR(50) NOT NULL,
    
    -- Business Properties
    [CustomerCode] NVARCHAR(50) NOT NULL,
    [CustomerType] NVARCHAR(20) NOT NULL,
    [CompanyName] NVARCHAR(200) NOT NULL,
    [ContactFirstName] NVARCHAR(100) NOT NULL,
    [ContactLastName] NVARCHAR(100) NOT NULL,
    [Email] NVARCHAR(255) NOT NULL,
    [PhoneNumber] NVARCHAR(20) NULL,
    
    -- Address Information
    [AddressLine1] NVARCHAR(255) NOT NULL,
    [AddressLine2] NVARCHAR(255) NULL,
    [City] NVARCHAR(100) NOT NULL,
    [State] NVARCHAR(50) NOT NULL,
    [PostalCode] NVARCHAR(20) NOT NULL,
    [Country] NVARCHAR(100) NOT NULL,
    
    -- Platform.Shared Audit Fields
    [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [CreatedBy] NVARCHAR(100) NOT NULL DEFAULT 'system',
    [UpdatedAt] DATETIME2 NULL,
    [UpdatedBy] NVARCHAR(100) NULL,
    [DeletedAt] DATETIME2 NULL,
    [DeletedBy] NVARCHAR(100) NULL,
    
    -- Activation State
    [IsActive] BIT NOT NULL DEFAULT 1,
    
    CONSTRAINT [PK_Customers] PRIMARY KEY CLUSTERED ([Id] ASC)
);
GO

-- Indexes for Platform.Shared Multi-Product Support
CREATE UNIQUE NONCLUSTERED INDEX [IX_Customers_Product_CustomerCode] ON [dbo].[Customers] 
(
    [Product] ASC, [CustomerCode] ASC
) WHERE [DeletedAt] IS NULL;
GO

CREATE NONCLUSTERED INDEX [IX_Customers_Product_IsActive] ON [dbo].[Customers] 
(
    [Product] ASC, [IsActive] ASC
) INCLUDE ([CustomerCode], [CompanyName], [Email]) WHERE [DeletedAt] IS NULL;
GO

CREATE NONCLUSTERED INDEX [IX_Customers_Product_CustomerType] ON [dbo].[Customers] 
(
    [Product] ASC, [CustomerType] ASC
) WHERE [DeletedAt] IS NULL;
GO

CREATE NONCLUSTERED INDEX [IX_Customers_Product_Email] ON [dbo].[Customers] 
(
    [Product] ASC, [Email] ASC
) WHERE [DeletedAt] IS NULL;
GO

CREATE NONCLUSTERED INDEX [IX_Customers_Product_CompanyName] ON [dbo].[Customers] 
(
    [Product] ASC, [CompanyName] ASC
) WHERE [DeletedAt] IS NULL;
GO