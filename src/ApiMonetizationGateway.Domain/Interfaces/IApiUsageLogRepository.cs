using ApiMonetizationGateway.Domain.Entities;

namespace ApiMonetizationGateway.Domain.Interfaces
{
    public interface IApiUsageLogRepository
    {
        Task<ApiUsageLog> CreateAsync(ApiUsageLog log);
        Task<IEnumerable<ApiUsageLog>> GetByCustomerIdAsync(int customerId, DateTime? fromDate = null, DateTime? toDate = null);
        Task<IEnumerable<ApiUsageLog>> GetByCustomerIdAndMonthAsync(int customerId, int year, int month);
        Task<long> GetTotalRequestCountAsync(int customerId);
        Task<int> GetMonthlyRequestCountAsync(int customerId, int year, int month);
        Task<decimal> GetMonthlyCostAsync(int customerId, int year, int month);
    }
}