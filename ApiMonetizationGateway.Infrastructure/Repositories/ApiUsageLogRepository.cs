using ApiMonetizationGateway.Domain.Entities;
using ApiMonetizationGateway.Domain.Interfaces;
using ApiMonetizationGateway.Infrastructure.Data;
using Dapper;

namespace ApiMonetizationGateway.Infrastructure.Repositories
{
    public class ApiUsageLogRepository : IApiUsageLogRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;
        
        public ApiUsageLogRepository(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }
        
        public async Task<ApiUsageLog> CreateAsync(ApiUsageLog log)
        {
            const string sql = @"
                INSERT INTO ApiUsageLogs (CustomerId, UserId, Endpoint, HttpMethod, Timestamp,
                                        ResponseStatusCode, ResponseTimeMs, IpAddress, UserAgent, Cost)
                OUTPUT INSERTED.Id
                VALUES (@CustomerId, @UserId, @Endpoint, @HttpMethod, @Timestamp,
                        @ResponseStatusCode, @ResponseTimeMs, @IpAddress, @UserAgent, @Cost)";
                        
            using var connection = _connectionFactory.CreateConnection();
            
            var id = await connection.QuerySingleAsync<long>(sql, log);
            log.Id = id;
            
            return log;
        }
        
        public async Task<IEnumerable<ApiUsageLog>> GetByCustomerIdAsync(int customerId, DateTime? fromDate = null, DateTime? toDate = null)
        {
            var sql = @"
                SELECT Id, CustomerId, UserId, Endpoint, HttpMethod, Timestamp,
                       ResponseStatusCode, ResponseTimeMs, IpAddress, UserAgent, Cost
                FROM ApiUsageLogs
                WHERE CustomerId = @CustomerId";
            
            var parameterDict = new Dictionary<string, object>
            {
                ["CustomerId"] = customerId
            };
            
            if (fromDate.HasValue)
            {
                sql += " AND Timestamp >= @FromDate";
                parameterDict["FromDate"] = fromDate.Value;
            }
            
            if (toDate.HasValue)
            {
                sql += " AND Timestamp <= @ToDate";
                parameterDict["ToDate"] = toDate.Value;
            }
            
            sql += " ORDER BY Timestamp DESC";
            
            using var connection = _connectionFactory.CreateConnection();
            return await connection.QueryAsync<ApiUsageLog>(sql, parameterDict);
        }
        
        public async Task<IEnumerable<ApiUsageLog>> GetByCustomerIdAndMonthAsync(int customerId, int year, int month)
        {
            const string sql = @"
                SELECT Id, CustomerId, UserId, Endpoint, HttpMethod, Timestamp,
                       ResponseStatusCode, ResponseTimeMs, IpAddress, UserAgent, Cost
                FROM ApiUsageLogs
                WHERE CustomerId = @CustomerId 
                  AND YEAR(Timestamp) = @Year 
                  AND MONTH(Timestamp) = @Month
                ORDER BY Timestamp DESC";
                
            using var connection = _connectionFactory.CreateConnection();
            return await connection.QueryAsync<ApiUsageLog>(sql, new { CustomerId = customerId, Year = year, Month = month });
        }
        
        public async Task<long> GetTotalRequestCountAsync(int customerId)
        {
            const string sql = "SELECT COUNT(*) FROM ApiUsageLogs WHERE CustomerId = @CustomerId";
            
            using var connection = _connectionFactory.CreateConnection();
            return await connection.QuerySingleAsync<long>(sql, new { CustomerId = customerId });
        }
        
        public async Task<int> GetMonthlyRequestCountAsync(int customerId, int year, int month)
        {
            const string sql = @"
                SELECT COUNT(*) 
                FROM ApiUsageLogs 
                WHERE CustomerId = @CustomerId 
                  AND YEAR(Timestamp) = @Year 
                  AND MONTH(Timestamp) = @Month";
                  
            using var connection = _connectionFactory.CreateConnection();
            return await connection.QuerySingleAsync<int>(sql, new { CustomerId = customerId, Year = year, Month = month });
        }
        
        public async Task<decimal> GetMonthlyCostAsync(int customerId, int year, int month)
        {
            const string sql = @"
                SELECT ISNULL(SUM(Cost), 0) 
                FROM ApiUsageLogs 
                WHERE CustomerId = @CustomerId 
                  AND YEAR(Timestamp) = @Year 
                  AND MONTH(Timestamp) = @Month";
                  
            using var connection = _connectionFactory.CreateConnection();
            return await connection.QuerySingleAsync<decimal>(sql, new { CustomerId = customerId, Year = year, Month = month });
        }
    }
}