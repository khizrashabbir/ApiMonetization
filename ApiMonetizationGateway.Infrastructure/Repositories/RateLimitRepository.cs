using ApiMonetizationGateway.Domain.Entities;
using ApiMonetizationGateway.Domain.Interfaces;
using ApiMonetizationGateway.Infrastructure.Data;
using Dapper;

namespace ApiMonetizationGateway.Infrastructure.Repositories
{
    public class RateLimitRepository : IRateLimitRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;
        
        public RateLimitRepository(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }
        
        public async Task<RateLimitTracker?> GetByCustomerIdAsync(int customerId)
        {
            const string sql = @"
                SELECT Id, CustomerId, WindowStart, RequestCount, LastRequest
                FROM RateLimitTrackers
                WHERE CustomerId = @CustomerId";
                
            using var connection = _connectionFactory.CreateConnection();
            return await connection.QuerySingleOrDefaultAsync<RateLimitTracker>(sql, new { CustomerId = customerId });
        }
        
        public async Task UpsertAsync(RateLimitTracker tracker)
        {
            const string sql = @"
                MERGE RateLimitTrackers AS target
                USING (SELECT @CustomerId as CustomerId) AS source
                ON target.CustomerId = source.CustomerId
                WHEN MATCHED THEN
                    UPDATE SET WindowStart = @WindowStart, RequestCount = @RequestCount, LastRequest = @LastRequest
                WHEN NOT MATCHED THEN
                    INSERT (CustomerId, WindowStart, RequestCount, LastRequest)
                    VALUES (@CustomerId, @WindowStart, @RequestCount, @LastRequest);";
                    
            using var connection = _connectionFactory.CreateConnection();
            await connection.ExecuteAsync(sql, tracker);
        }
        
        public async Task CleanupExpiredRecordsAsync()
        {
            // Remove rate limit trackers older than 5 minutes (much longer than needed for per-second limits)
            const string sql = @"
                DELETE FROM RateLimitTrackers 
                WHERE LastRequest < DATEADD(minute, -5, GETUTCDATE())";
                
            using var connection = _connectionFactory.CreateConnection();
            await connection.ExecuteAsync(sql);
        }
    }
}