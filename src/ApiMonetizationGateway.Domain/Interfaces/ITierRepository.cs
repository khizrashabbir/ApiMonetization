using ApiMonetizationGateway.Domain.Entities;

namespace ApiMonetizationGateway.Domain.Interfaces
{
    public interface ITierRepository
    {
        Task<Tier?> GetByIdAsync(int id);
        Task<IEnumerable<Tier>> GetAllActiveAsync();
        Task<Tier> CreateAsync(Tier tier);
        Task UpdateAsync(Tier tier);
        Task DeleteAsync(int id);
    }
}