-- Create Database
CREATE DATABASE ApiMonetizationGateway;
GO

USE ApiMonetizationGateway;
GO

-- Create Tiers table
CREATE TABLE Tiers (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(50) NOT NULL UNIQUE,
    Description NVARCHAR(200) NOT NULL,
    MonthlyQuota INT NOT NULL,
    RateLimitPerSecond INT NOT NULL,
    MonthlyPrice DECIMAL(10,2) NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    IsActive BIT NOT NULL DEFAULT 1,
    
    INDEX IX_Tiers_Name (Name),
    INDEX IX_Tiers_IsActive (IsActive)
);
GO

-- Create Customers table
CREATE TABLE Customers (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL,
    Email NVARCHAR(255) NOT NULL UNIQUE,
    ApiKey NVARCHAR(50) NOT NULL UNIQUE,
    TierId INT NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    IsActive BIT NOT NULL DEFAULT 1,
    CurrentMonthUsage INT NOT NULL DEFAULT 0,
    LastUsageReset DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    
    CONSTRAINT FK_Customers_TierId FOREIGN KEY (TierId) REFERENCES Tiers(Id),
    INDEX IX_Customers_ApiKey (ApiKey),
    INDEX IX_Customers_Email (Email),
    INDEX IX_Customers_TierId (TierId),
    INDEX IX_Customers_IsActive (IsActive)
);
GO

-- Create ApiUsageLogs table
CREATE TABLE ApiUsageLogs (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    CustomerId INT NOT NULL,
    UserId NVARCHAR(50) NOT NULL,
    Endpoint NVARCHAR(255) NOT NULL,
    HttpMethod NVARCHAR(10) NOT NULL,
    Timestamp DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ResponseStatusCode INT NOT NULL,
    ResponseTimeMs BIGINT NOT NULL,
    IpAddress NVARCHAR(50),
    UserAgent NVARCHAR(500),
    Cost DECIMAL(10,4),
    
    CONSTRAINT FK_ApiUsageLogs_CustomerId FOREIGN KEY (CustomerId) REFERENCES Customers(Id),
    INDEX IX_ApiUsageLogs_CustomerId_Timestamp (CustomerId, Timestamp),
    INDEX IX_ApiUsageLogs_Endpoint (Endpoint),
    INDEX IX_ApiUsageLogs_Timestamp (Timestamp)
);
GO

-- Create RateLimitTrackers table
CREATE TABLE RateLimitTrackers (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    CustomerId INT NOT NULL UNIQUE,
    WindowStart DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    RequestCount INT NOT NULL DEFAULT 0,
    LastRequest DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    
    CONSTRAINT FK_RateLimitTrackers_CustomerId FOREIGN KEY (CustomerId) REFERENCES Customers(Id),
    INDEX IX_RateLimitTrackers_CustomerId (CustomerId),
    INDEX IX_RateLimitTrackers_WindowStart (WindowStart)
);
GO

-- Create MonthlyUsageSummaries table
CREATE TABLE MonthlyUsageSummaries (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    CustomerId INT NOT NULL,
    Year INT NOT NULL,
    Month INT NOT NULL,
    TotalRequests INT NOT NULL DEFAULT 0,
    TotalCost DECIMAL(10,2) NOT NULL DEFAULT 0,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    
    CONSTRAINT FK_MonthlyUsageSummaries_CustomerId FOREIGN KEY (CustomerId) REFERENCES Customers(Id),
    CONSTRAINT UQ_MonthlyUsageSummaries_Customer_Period UNIQUE (CustomerId, Year, Month),
    INDEX IX_MonthlyUsageSummaries_Period (Year, Month),
    INDEX IX_MonthlyUsageSummaries_CustomerId (CustomerId)
);
GO

-- Insert default tiers
INSERT INTO Tiers (Name, Description, MonthlyQuota, RateLimitPerSecond, MonthlyPrice) 
VALUES 
    ('Free', 'Free tier with basic access', 100, 2, 0.00),
    ('Pro', 'Professional tier with enhanced access', 100000, 10, 50.00);
GO

-- Create stored procedures for common operations

-- Procedure to get customer with tier information
CREATE PROCEDURE sp_GetCustomerWithTier
    @ApiKey NVARCHAR(50)
AS
BEGIN
    SELECT 
        c.Id, c.Name, c.Email, c.ApiKey, c.TierId, c.CreatedAt, c.UpdatedAt, 
        c.IsActive, c.CurrentMonthUsage, c.LastUsageReset,
        t.Id as TierId, t.Name as TierName, t.Description as TierDescription, 
        t.MonthlyQuota, t.RateLimitPerSecond, t.MonthlyPrice
    FROM Customers c
    INNER JOIN Tiers t ON c.TierId = t.Id
    WHERE c.ApiKey = @ApiKey AND c.IsActive = 1;
END
GO

-- Procedure to update customer usage
CREATE PROCEDURE sp_UpdateCustomerUsage
    @CustomerId INT
AS
BEGIN
    UPDATE Customers 
    SET CurrentMonthUsage = CurrentMonthUsage + 1,
        UpdatedAt = GETUTCDATE()
    WHERE Id = @CustomerId;
END
GO

-- Procedure to reset monthly usage for all customers
CREATE PROCEDURE sp_ResetMonthlyUsage
AS
BEGIN
    UPDATE Customers 
    SET CurrentMonthUsage = 0,
        LastUsageReset = DATEFROMPARTS(YEAR(GETUTCDATE()), MONTH(GETUTCDATE()), 1),
        UpdatedAt = GETUTCDATE()
    WHERE MONTH(LastUsageReset) != MONTH(GETUTCDATE()) 
       OR YEAR(LastUsageReset) != YEAR(GETUTCDATE());
END
GO

-- Create trigger to auto-update timestamps
CREATE TRIGGER tr_Customers_UpdateTimestamp
ON Customers
AFTER UPDATE
AS
BEGIN
    UPDATE Customers 
    SET UpdatedAt = GETUTCDATE()
    FROM Customers c
    INNER JOIN inserted i ON c.Id = i.Id;
END
GO

CREATE TRIGGER tr_Tiers_UpdateTimestamp
ON Tiers
AFTER UPDATE
AS
BEGIN
    UPDATE Tiers 
    SET UpdatedAt = GETUTCDATE()
    FROM Tiers t
    INNER JOIN inserted i ON t.Id = i.Id;
END
GO

CREATE TRIGGER tr_MonthlyUsageSummaries_UpdateTimestamp
ON MonthlyUsageSummaries
AFTER UPDATE
AS
BEGIN
    UPDATE MonthlyUsageSummaries 
    SET UpdatedAt = GETUTCDATE()
    FROM MonthlyUsageSummaries m
    INNER JOIN inserted i ON m.Id = i.Id;
END
GO

PRINT 'Database schema created successfully!';