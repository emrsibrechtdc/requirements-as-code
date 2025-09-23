-- Sample location data for development and testing
-- This script should be run after database deployment for development environments

-- Insert sample locations for different products
INSERT INTO [dbo].[Locations] 
    ([Id], [Product], [LocationCode], [LocationTypeCode], [AddressLine1], [AddressLine2], [City], [State], [ZipCode], [Country], [IsActive], [CreatedAt], [CreatedBy])
VALUES
    -- Product A locations
    (NEWSEQUENTIALID(), 'ProductA', 'PA-WAREHOUSE-01', 'WAREHOUSE', '123 Industrial Blvd', NULL, 'Atlanta', 'GA', '30309', 'USA', 1, GETUTCDATE(), 'system'),
    (NEWSEQUENTIALID(), 'ProductA', 'PA-RETAIL-01', 'RETAIL', '456 Main St', 'Suite 100', 'Atlanta', 'GA', '30308', 'USA', 1, GETUTCDATE(), 'system'),
    (NEWSEQUENTIALID(), 'ProductA', 'PA-DISTRIBUTION-01', 'DISTRIBUTION', '789 Commerce Dr', NULL, 'Marietta', 'GA', '30062', 'USA', 1, GETUTCDATE(), 'system'),
    
    -- Product B locations
    (NEWSEQUENTIALID(), 'ProductB', 'PB-WAREHOUSE-01', 'WAREHOUSE', '321 Factory Rd', NULL, 'Nashville', 'TN', '37201', 'USA', 1, GETUTCDATE(), 'system'),
    (NEWSEQUENTIALID(), 'ProductB', 'PB-RETAIL-01', 'RETAIL', '654 Shopping Blvd', 'Unit 200', 'Nashville', 'TN', '37203', 'USA', 1, GETUTCDATE(), 'system'),
    (NEWSEQUENTIALID(), 'ProductB', 'PB-MANUFACTURING-01', 'MANUFACTURING', '987 Plant Ave', NULL, 'Franklin', 'TN', '37064', 'USA', 1, GETUTCDATE(), 'system'),
    
    -- Inactive location example
    (NEWSEQUENTIALID(), 'ProductA', 'PA-OLD-LOCATION-01', 'RETAIL', '111 Closed St', NULL, 'Atlanta', 'GA', '30310', 'USA', 0, GETUTCDATE(), 'system');
GO

-- Verify sample data
SELECT 
    [Product],
    [LocationCode],
    [LocationTypeCode],
    [City],
    [State],
    [IsActive]
FROM [dbo].[Locations]
ORDER BY [Product], [LocationCode];