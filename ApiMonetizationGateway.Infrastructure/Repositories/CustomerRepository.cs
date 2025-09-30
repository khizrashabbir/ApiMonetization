using ApiMonetizationGateway.Domain.Entities;
using ApiMonetizationGateway.Domain.Interfaces;
using ApiMonetizationGateway.Infrastructure.Data;
using Dapper;

namespace ApiMonetizationGateway.Infrastructure.Repositories
{
    public class CustomerRepository : ICustomerRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;
        
        public CustomerRepository(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }
        
        public async Task<Customer?> GetByIdAsync(int id)
        {
            const string sql = @"
                SELECT c.Id, c.Name, c.Email, c.ApiKey, c.TierId, c.CreatedAt, c.UpdatedAt, 
                       c.IsActive, c.CurrentMonthUsage, c.LastUsageReset,
                       t.Id, t.Name, t.Description, t.MonthlyQuota, t.RateLimitPerSecond, 
                       t.MonthlyPrice, t.CreatedAt, t.UpdatedAt, t.IsActive
                FROM Customers c
                INNER JOIN Tiers t ON c.TierId = t.Id
                WHERE c.Id = @Id";
                
            using var connection = _connectionFactory.CreateConnection();
            
            var customerDict = new Dictionary<int, Customer>();
            
            var result = await connection.QueryAsync<Customer, Tier, Customer>(
                sql,
                (customer, tier) =>
                {
                    if (!customerDict.TryGetValue(customer.Id, out var customerEntry))
                    {
                        customerEntry = customer;
                        customerDict.Add(customer.Id, customerEntry);
                    }
                    customerEntry.Tier = tier;
                    return customerEntry;
                },
                new { Id = id },
                splitOn: "Id"
            );
            
            return result.FirstOrDefault();
        }
        
        public async Task<Customer?> GetByApiKeyAsync(string apiKey)
        {
            const string sql = @"
                SELECT c.Id, c.Name, c.Email, c.ApiKey, c.TierId, c.CreatedAt, c.UpdatedAt, 
                       c.IsActive, c.CurrentMonthUsage, c.LastUsageReset,
                       t.Id, t.Name, t.Description, t.MonthlyQuota, t.RateLimitPerSecond, 
                       t.MonthlyPrice, t.CreatedAt, t.UpdatedAt, t.IsActive
                FROM Customers c
                INNER JOIN Tiers t ON c.TierId = t.Id
                WHERE c.ApiKey = @ApiKey AND c.IsActive = 1";
                
            using var connection = _connectionFactory.CreateConnection();
            
            var customerDict = new Dictionary<int, Customer>();
            
            var result = await connection.QueryAsync<Customer, Tier, Customer>(
                sql,
                (customer, tier) =>
                {
                    if (!customerDict.TryGetValue(customer.Id, out var customerEntry))
                    {
                        customerEntry = customer;
                        customerDict.Add(customer.Id, customerEntry);
                    }
                    customerEntry.Tier = tier;
                    return customerEntry;
                },
                new { ApiKey = apiKey },
                splitOn: "Id"
            );
            
            return result.FirstOrDefault();
        }
        
        public async Task<IEnumerable<Customer>> GetAllAsync()
        {
            const string sql = @"
                SELECT c.Id, c.Name, c.Email, c.ApiKey, c.TierId, c.CreatedAt, c.UpdatedAt, 
                       c.IsActive, c.CurrentMonthUsage, c.LastUsageReset,
                       t.Id, t.Name, t.Description, t.MonthlyQuota, t.RateLimitPerSecond, 
                       t.MonthlyPrice, t.CreatedAt, t.UpdatedAt, t.IsActive
                FROM Customers c
                INNER JOIN Tiers t ON c.TierId = t.Id
                ORDER BY c.CreatedAt DESC";
                
            using var connection = _connectionFactory.CreateConnection();
            
            var customerDict = new Dictionary<int, Customer>();
            
            var result = await connection.QueryAsync<Customer, Tier, Customer>(
                sql,
                (customer, tier) =>
                {
                    if (!customerDict.TryGetValue(customer.Id, out var customerEntry))
                    {
                        customerEntry = customer;
                        customerDict.Add(customer.Id, customerEntry);
                    }
                    customerEntry.Tier = tier;
                    return customerEntry;
                },
                splitOn: "Id"
            );
            
            return customerDict.Values;
        }
        
        public async Task<Customer> CreateAsync(Customer customer)
        {
            const string sql = @"
                INSERT INTO Customers (Name, Email, ApiKey, TierId, CreatedAt, UpdatedAt, 
                                     IsActive, CurrentMonthUsage, LastUsageReset)
                OUTPUT INSERTED.Id
                VALUES (@Name, @Email, @ApiKey, @TierId, @CreatedAt, @UpdatedAt, 
                        @IsActive, @CurrentMonthUsage, @LastUsageReset)";
                        
            using var connection = _connectionFactory.CreateConnection();
            
            var id = await connection.QuerySingleAsync<int>(sql, customer);
            customer.Id = id;
            
            return customer;
        }
        
        public async Task UpdateAsync(Customer customer)
        {
            const string sql = @"
                UPDATE Customers 
                SET Name = @Name, Email = @Email, ApiKey = @ApiKey, TierId = @TierId,
                    UpdatedAt = @UpdatedAt, IsActive = @IsActive, 
                    CurrentMonthUsage = @CurrentMonthUsage, LastUsageReset = @LastUsageReset
                WHERE Id = @Id";
                
            using var connection = _connectionFactory.CreateConnection();
            await connection.ExecuteAsync(sql, customer);
        }
        
        public async Task DeleteAsync(int id)
        {
            const string sql = "UPDATE Customers SET IsActive = 0, UpdatedAt = GETUTCDATE() WHERE Id = @Id";
            
            using var connection = _connectionFactory.CreateConnection();
            await connection.ExecuteAsync(sql, new { Id = id });
        }
        
        public async Task<bool> ApiKeyExistsAsync(string apiKey)
        {
            const string sql = "SELECT COUNT(1) FROM Customers WHERE ApiKey = @ApiKey";
            
            using var connection = _connectionFactory.CreateConnection();
            var count = await connection.QuerySingleAsync<int>(sql, new { ApiKey = apiKey });
            
            return count > 0;
        }
    }
}