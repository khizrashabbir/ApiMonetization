using ApiMonetizationGateway.Domain.Entities;

namespace ApiMonetizationGateway.Domain.Interfaces
{
    public interface IRateLimitRepository
    {
        Task<RateLimitTracker?> GetByCustomerIdAsync(int customerId);
        Task UpsertAsync(RateLimitTracker tracker);
        Task CleanupExpiredRecordsAsync();
    }
}