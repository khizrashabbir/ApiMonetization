using ApiMonetizationGateway.Domain.Entities;
using ApiMonetizationGateway.Domain.Interfaces;
using ApiMonetizationGateway.Infrastructure.Data;
using Dapper;

namespace ApiMonetizationGateway.Infrastructure.Repositories
{
    public class MonthlyUsageSummaryRepository : IMonthlyUsageSummaryRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;
        
        public MonthlyUsageSummaryRepository(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }
        
        public async Task<MonthlyUsageSummary?> GetByCustomerAndMonthAsync(int customerId, int year, int month)
        {
            const string sql = @"
                SELECT Id, CustomerId, Year, Month, TotalRequests, TotalCost, CreatedAt, UpdatedAt
                FROM MonthlyUsageSummaries
                WHERE CustomerId = @CustomerId AND Year = @Year AND Month = @Month";
                
            using var connection = _connectionFactory.CreateConnection();
            return await connection.QuerySingleOrDefaultAsync<MonthlyUsageSummary>(sql, 
                new { CustomerId = customerId, Year = year, Month = month });
        }
        
        public async Task<IEnumerable<MonthlyUsageSummary>> GetByCustomerIdAsync(int customerId)
        {
            const string sql = @"
                SELECT Id, CustomerId, Year, Month, TotalRequests, TotalCost, CreatedAt, UpdatedAt
                FROM MonthlyUsageSummaries
                WHERE CustomerId = @CustomerId
                ORDER BY Year DESC, Month DESC";
                
            using var connection = _connectionFactory.CreateConnection();
            return await connection.QueryAsync<MonthlyUsageSummary>(sql, new { CustomerId = customerId });
        }
        
        public async Task UpsertAsync(MonthlyUsageSummary summary)
        {
            const string sql = @"
                MERGE MonthlyUsageSummaries AS target
                USING (SELECT @CustomerId as CustomerId, @Year as Year, @Month as Month) AS source
                ON target.CustomerId = source.CustomerId AND target.Year = source.Year AND target.Month = source.Month
                WHEN MATCHED THEN
                    UPDATE SET TotalRequests = @TotalRequests, TotalCost = @TotalCost, UpdatedAt = @UpdatedAt
                WHEN NOT MATCHED THEN
                    INSERT (CustomerId, Year, Month, TotalRequests, TotalCost, CreatedAt, UpdatedAt)
                    VALUES (@CustomerId, @Year, @Month, @TotalRequests, @TotalCost, @CreatedAt, @UpdatedAt);";
                    
            using var connection = _connectionFactory.CreateConnection();
            await connection.ExecuteAsync(sql, summary);
        }
        
        public async Task<IEnumerable<MonthlyUsageSummary>> GetAllForMonthAsync(int year, int month)
        {
            const string sql = @"
                SELECT m.Id, m.CustomerId, m.Year, m.Month, m.TotalRequests, m.TotalCost, m.CreatedAt, m.UpdatedAt,
                       c.Id, c.Name, c.Email, c.ApiKey, c.TierId, c.CreatedAt, c.UpdatedAt, 
                       c.IsActive, c.CurrentMonthUsage, c.LastUsageReset
                FROM MonthlyUsageSummaries m
                INNER JOIN Customers c ON m.CustomerId = c.Id
                WHERE m.Year = @Year AND m.Month = @Month
                ORDER BY m.TotalCost DESC";
                
            using var connection = _connectionFactory.CreateConnection();
            
            var summaryDict = new Dictionary<long, MonthlyUsageSummary>();
            
            var result = await connection.QueryAsync<MonthlyUsageSummary, Customer, MonthlyUsageSummary>(
                sql,
                (summary, customer) =>
                {
                    if (!summaryDict.TryGetValue(summary.Id, out var summaryEntry))
                    {
                        summaryEntry = summary;
                        summaryDict.Add(summary.Id, summaryEntry);
                    }
                    summaryEntry.Customer = customer;
                    return summaryEntry;
                },
                new { Year = year, Month = month },
                splitOn: "Id"
            );
            
            return summaryDict.Values;
        }
    }
}