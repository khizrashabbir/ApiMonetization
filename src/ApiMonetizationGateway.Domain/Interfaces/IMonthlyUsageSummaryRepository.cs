using ApiMonetizationGateway.Domain.Entities;

namespace ApiMonetizationGateway.Domain.Interfaces
{
    public interface IMonthlyUsageSummaryRepository
    {
        Task<MonthlyUsageSummary?> GetByCustomerAndMonthAsync(int customerId, int year, int month);
        Task<IEnumerable<MonthlyUsageSummary>> GetByCustomerIdAsync(int customerId);
        Task UpsertAsync(MonthlyUsageSummary summary);
        Task<IEnumerable<MonthlyUsageSummary>> GetAllForMonthAsync(int year, int month);
    }
}