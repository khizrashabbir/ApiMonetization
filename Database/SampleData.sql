-- Sample data for testing
USE ApiMonetizationGateway;
GO

-- Insert sample customers
INSERT INTO Customers (Name, Email, ApiKey, TierId, CurrentMonthUsage, LastUsageReset) 
VALUES 
    ('John Doe', 'john.doe@example.com', 'free_api_key_12345', 1, 15, DATEFROMPARTS(YEAR(GETUTCDATE()), MONTH(GETUTCDATE()), 1)),
    ('Jane Smith', 'jane.smith@company.com', 'pro_api_key_67890', 2, 2500, DATEFROMPARTS(YEAR(GETUTCDATE()), MONTH(GETUTCDATE()), 1)),
    ('Bob Johnson', 'bob.johnson@startup.io', 'free_api_key_11111', 1, 89, DATEFROMPARTS(YEAR(GETUTCDATE()), MONTH(GETUTCDATE()), 1));
GO

-- Insert sample API usage logs
DECLARE @CustomerId1 INT = (SELECT Id FROM Customers WHERE Email = 'john.doe@example.com');
DECLARE @CustomerId2 INT = (SELECT Id FROM Customers WHERE Email = 'jane.smith@company.com');
DECLARE @CustomerId3 INT = (SELECT Id FROM Customers WHERE Email = 'bob.johnson@startup.io');

INSERT INTO ApiUsageLogs (CustomerId, UserId, Endpoint, HttpMethod, Timestamp, ResponseStatusCode, ResponseTimeMs, IpAddress, Cost)
VALUES 
    (@CustomerId1, 'user123', '/api/data', 'GET', DATEADD(hour, -2, GETUTCDATE()), 200, 125, '192.168.1.100', 0.00),
    (@CustomerId1, 'user123', '/api/users', 'GET', DATEADD(hour, -1, GETUTCDATE()), 200, 89, '192.168.1.100', 0.00),
    (@CustomerId2, 'user456', '/api/analytics', 'POST', DATEADD(minute, -30, GETUTCDATE()), 201, 234, '10.0.0.50', 0.0005),
    (@CustomerId2, 'user456', '/api/reports', 'GET', DATEADD(minute, -15, GETUTCDATE()), 200, 156, '10.0.0.50', 0.0005),
    (@CustomerId3, 'user789', '/api/health', 'GET', DATEADD(minute, -5, GETUTCDATE()), 200, 45, '172.16.0.10', 0.00);
GO

-- Insert rate limit trackers
INSERT INTO RateLimitTrackers (CustomerId, WindowStart, RequestCount, LastRequest)
VALUES 
    (@CustomerId1, DATEADD(second, -30, GETUTCDATE()), 1, DATEADD(second, -30, GETUTCDATE())),
    (@CustomerId2, DATEADD(second, -15, GETUTCDATE()), 2, DATEADD(second, -15, GETUTCDATE())),
    (@CustomerId3, DATEADD(second, -5, GETUTCDATE()), 1, DATEADD(second, -5, GETUTCDATE()));
GO

-- Insert monthly usage summaries for current month
INSERT INTO MonthlyUsageSummaries (CustomerId, Year, Month, TotalRequests, TotalCost)
VALUES 
    (@CustomerId1, YEAR(GETUTCDATE()), MONTH(GETUTCDATE()), 15, 0.00),
    (@CustomerId2, YEAR(GETUTCDATE()), MONTH(GETUTCDATE()), 2500, 1.25),
    (@CustomerId3, YEAR(GETUTCDATE()), MONTH(GETUTCDATE()), 89, 0.00);
GO

PRINT 'Sample data inserted successfully!';

-- Query to verify data
SELECT 'Customers' as TableName, COUNT(*) as RecordCount FROM Customers
UNION ALL
SELECT 'Tiers', COUNT(*) FROM Tiers
UNION ALL
SELECT 'ApiUsageLogs', COUNT(*) FROM ApiUsageLogs
UNION ALL
SELECT 'RateLimitTrackers', COUNT(*) FROM RateLimitTrackers
UNION ALL
SELECT 'MonthlyUsageSummaries', COUNT(*) FROM MonthlyUsageSummaries;
GO