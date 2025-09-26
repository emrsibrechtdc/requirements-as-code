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
    [Latitude] DECIMAL(10, 8) NULL,
    [Longitude] DECIMAL(11, 8) NULL,
    [GeofenceRadius] FLOAT NULL,
    [ComputedCoordinates] AS (
        CASE 
            WHEN [Latitude] IS NOT NULL AND [Longitude] IS NOT NULL 
            THEN geography::Point([Latitude], [Longitude], 4326)
            ELSE NULL 
        END
    ) PERSISTED,
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

-- Spatial index for coordinate-based queries
CREATE SPATIAL INDEX [IX_Locations_ComputedCoordinates] 
ON [dbo].[Locations] ([ComputedCoordinates])
USING GEOGRAPHY_GRID
WITH (
    GRIDS = (LEVEL_1 = MEDIUM, LEVEL_2 = MEDIUM, LEVEL_3 = MEDIUM, LEVEL_4 = MEDIUM),
    CELLS_PER_OBJECT = 16,
    PAD_INDEX = OFF,
    STATISTICS_NORECOMPUTE = OFF,
    SORT_IN_TEMPDB = OFF,
    DROP_EXISTING = OFF,
    ONLINE = OFF,
    ALLOW_ROW_LOCKS = ON,
    ALLOW_PAGE_LOCKS = ON
);
GO

-- Composite index for product-filtered coordinate queries
CREATE INDEX [IX_Locations_Product_Coordinates] 
ON [dbo].[Locations] ([Product], [Latitude], [Longitude], [GeofenceRadius])
WHERE [DeletedAt] IS NULL AND [Latitude] IS NOT NULL AND [Longitude] IS NOT NULL;
GO

-- Index for finding locations with coordinates within product
CREATE INDEX [IX_Locations_Product_HasCoordinates] 
ON [dbo].[Locations] ([Product], [IsActive])
WHERE [DeletedAt] IS NULL AND [Latitude] IS NOT NULL;
GO
