CREATE TABLE [dbo].[Locations] (
    [Id] UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
    [Product] NVARCHAR(50) NOT NULL,
    [LocationCode] NVARCHAR(50) NOT NULL,
    [LocationTypeCode] NVARCHAR(20) NOT NULL,
    [AddressLine1] NVARCHAR(100) NOT NULL,
    [AddressLine2] NVARCHAR(100) NULL,
    [City] NVARCHAR(50) NOT NULL,
    [State] NVARCHAR(50) NOT NULL,
    [ZipCode] NVARCHAR(20) NOT NULL,
    [Country] NVARCHAR(50) NOT NULL,
    [IsActive] BIT NOT NULL DEFAULT 1,
    [CreatedAt] DATETIMEOFFSET(2) NOT NULL DEFAULT GETUTCDATE(),
    [CreatedBy] NVARCHAR(256) NULL,
    [UpdatedAt] DATETIMEOFFSET(2) NULL,
    [UpdatedBy] NVARCHAR(256) NULL,
    [DeletedAt] DATETIMEOFFSET(2) NULL,
    [DeletedBy] NVARCHAR(256) NULL,
    [IsDeleted] bit NOT NULL DEFAULT 0,
    [LastActivationDate]	DATETIMEOFFSET(2)		NULL,
	[LastDeactivationDate]	DATETIMEOFFSET(2)		NULL,
    CONSTRAINT [PK_Locations] PRIMARY KEY ([Id])
);
GO

-- Index for multi-product filtering (most common query pattern)
CREATE INDEX [IX_Locations_Product_LocationCode] 
ON [dbo].[Locations] ([Product], [LocationCode]);
GO

-- Index for location code lookups within a product
CREATE INDEX [IX_Locations_Product_IsActive] 
ON [dbo].[Locations] ([Product], [IsActive])
WHERE [DeletedAt] IS NULL;
GO

-- Index for location type filtering
CREATE INDEX [IX_Locations_Product_LocationTypeCode] 
ON [dbo].[Locations] ([Product], [LocationTypeCode])
WHERE [DeletedAt] IS NULL;
GO

-- Unique constraint for location code within product
CREATE UNIQUE INDEX [UX_Locations_Product_LocationCode] 
ON [dbo].[Locations] ([Product], [LocationCode])
WHERE [DeletedAt] IS NULL;
GO
