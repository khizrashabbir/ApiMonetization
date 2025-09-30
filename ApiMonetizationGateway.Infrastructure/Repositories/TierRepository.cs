using ApiMonetizationGateway.Domain.Entities;
using ApiMonetizationGateway.Domain.Interfaces;
using ApiMonetizationGateway.Infrastructure.Data;
using Dapper;

namespace ApiMonetizationGateway.Infrastructure.Repositories
{
    public class TierRepository : ITierRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;
        
        public TierRepository(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }
        
        public async Task<Tier?> GetByIdAsync(int id)
        {
            const string sql = @"
                SELECT Id, Name, Description, MonthlyQuota, RateLimitPerSecond, 
                       MonthlyPrice, CreatedAt, UpdatedAt, IsActive
                FROM Tiers
                WHERE Id = @Id";
                
            using var connection = _connectionFactory.CreateConnection();
            return await connection.QuerySingleOrDefaultAsync<Tier>(sql, new { Id = id });
        }
        
        public async Task<IEnumerable<Tier>> GetAllActiveAsync()
        {
            const string sql = @"
                SELECT Id, Name, Description, MonthlyQuota, RateLimitPerSecond, 
                       MonthlyPrice, CreatedAt, UpdatedAt, IsActive
                FROM Tiers
                WHERE IsActive = 1
                ORDER BY MonthlyPrice";
                
            using var connection = _connectionFactory.CreateConnection();
            return await connection.QueryAsync<Tier>(sql);
        }
        
        public async Task<Tier> CreateAsync(Tier tier)
        {
            const string sql = @"
                INSERT INTO Tiers (Name, Description, MonthlyQuota, RateLimitPerSecond, 
                                 MonthlyPrice, CreatedAt, UpdatedAt, IsActive)
                OUTPUT INSERTED.Id
                VALUES (@Name, @Description, @MonthlyQuota, @RateLimitPerSecond, 
                        @MonthlyPrice, @CreatedAt, @UpdatedAt, @IsActive)";
                        
            using var connection = _connectionFactory.CreateConnection();
            
            var id = await connection.QuerySingleAsync<int>(sql, tier);
            tier.Id = id;
            
            return tier;
        }
        
        public async Task UpdateAsync(Tier tier)
        {
            const string sql = @"
                UPDATE Tiers 
                SET Name = @Name, Description = @Description, MonthlyQuota = @MonthlyQuota,
                    RateLimitPerSecond = @RateLimitPerSecond, MonthlyPrice = @MonthlyPrice,
                    UpdatedAt = @UpdatedAt, IsActive = @IsActive
                WHERE Id = @Id";
                
            using var connection = _connectionFactory.CreateConnection();
            await connection.ExecuteAsync(sql, tier);
        }
        
        public async Task DeleteAsync(int id)
        {
            const string sql = "UPDATE Tiers SET IsActive = 0, UpdatedAt = GETUTCDATE() WHERE Id = @Id";
            
            using var connection = _connectionFactory.CreateConnection();
            await connection.ExecuteAsync(sql, new { Id = id });
        }
    }
}