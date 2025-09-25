-- Sample customer data for development and testing
-- This script should be run after database deployment for development environments
-- Uses NEWSEQUENTIALID() for optimal performance

IF NOT EXISTS (SELECT 1 FROM [dbo].[Customers] WHERE [CustomerCode] = 'CUST-001')
BEGIN
    INSERT INTO [dbo].[Customers] 
        ([Id], [Product], [CustomerCode], [CustomerType], [CompanyName], [ContactFirstName], [ContactLastName], [Email], [PhoneNumber], [AddressLine1], [AddressLine2], [City], [State], [PostalCode], [Country], [IsActive], [CreatedAt], [CreatedBy])
    VALUES
        -- Product A customers
        (NEWSEQUENTIALID(), 'ProductA', 'CUST-001', 'ENTERPRISE', 'Acme Corporation', 'John', 'Smith', 'john.smith@acme.com', '555-0101', '123 Business Ave', 'Suite 100', 'Atlanta', 'GA', '30309', 'USA', 1, GETUTCDATE(), 'system'),
        (NEWSEQUENTIALID(), 'ProductA', 'CUST-002', 'SMALL_BUSINESS', 'Tech Solutions LLC', 'Sarah', 'Johnson', 'sarah@techsolutions.com', '555-0102', '456 Innovation Dr', NULL, 'Austin', 'TX', '78701', 'USA', 1, GETUTCDATE(), 'system'),
        (NEWSEQUENTIALID(), 'ProductA', 'CUST-003', 'ENTERPRISE', 'Global Industries', 'Michael', 'Brown', 'mbrown@globalind.com', '555-0103', '789 Corporate Blvd', 'Floor 15', 'New York', 'NY', '10001', 'USA', 1, GETUTCDATE(), 'system'),
        
        -- Product B customers
        (NEWSEQUENTIALID(), 'ProductB', 'CUST-001', 'RETAIL', 'Retail Mart Inc', 'Lisa', 'Davis', 'lisa.davis@retailmart.com', '555-0201', '321 Commerce St', NULL, 'Chicago', 'IL', '60601', 'USA', 1, GETUTCDATE(), 'system'),
        (NEWSEQUENTIALID(), 'ProductB', 'CUST-002', 'ENTERPRISE', 'Manufacturing Plus', 'Robert', 'Wilson', 'rwilson@mfgplus.com', '555-0202', '654 Industrial Way', 'Building A', 'Detroit', 'MI', '48201', 'USA', 1, GETUTCDATE(), 'system'),
        (NEWSEQUENTIALID(), 'ProductB', 'CUST-003', 'SMALL_BUSINESS', 'Local Services Co', 'Jennifer', 'Taylor', 'jen@localservices.com', '555-0203', '987 Main Street', NULL, 'Seattle', 'WA', '98101', 'USA', 1, GETUTCDATE(), 'system'),
        
        -- Product C customers
        (NEWSEQUENTIALID(), 'ProductC', 'CUST-001', 'ENTERPRISE', 'Healthcare Systems', 'David', 'Anderson', 'danderson@healthsys.com', '555-0301', '159 Medical Center Dr', 'Suite 200', 'Phoenix', 'AZ', '85001', 'USA', 1, GETUTCDATE(), 'system'),
        (NEWSEQUENTIALID(), 'ProductC', 'CUST-002', 'RETAIL', 'Pharmacy Chain', 'Emily', 'Martinez', 'emartinez@pharmchain.com', '555-0302', '753 Health Blvd', NULL, 'Miami', 'FL', '33101', 'USA', 1, GETUTCDATE(), 'system'),
        
        -- Inactive customer example
        (NEWSEQUENTIALID(), 'ProductA', 'CUST-INACTIVE-001', 'ENTERPRISE', 'Former Client Corp', 'Thomas', 'Clark', 'tclark@formerclient.com', '555-9999', '111 Closed Ave', NULL, 'Denver', 'CO', '80201', 'USA', 0, GETUTCDATE(), 'system');
END
GO

-- Verify sample data
SELECT 
    [Product],
    [CustomerCode],
    [CustomerType],
    [CompanyName],
    [ContactFirstName] + ' ' + [ContactLastName] AS [ContactName],
    [Email],
    [City],
    [State],
    [IsActive]
FROM [dbo].[Customers]
ORDER BY [Product], [CustomerCode];
GO