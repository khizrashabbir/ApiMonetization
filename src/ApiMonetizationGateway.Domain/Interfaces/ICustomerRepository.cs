using ApiMonetizationGateway.Domain.Entities;

namespace ApiMonetizationGateway.Domain.Interfaces
{
    public interface ICustomerRepository
    {
        Task<Customer?> GetByIdAsync(int id);
        Task<Customer?> GetByApiKeyAsync(string apiKey);
        Task<IEnumerable<Customer>> GetAllAsync();
        Task<Customer> CreateAsync(Customer customer);
        Task UpdateAsync(Customer customer);
        Task DeleteAsync(int id);
        Task<bool> ApiKeyExistsAsync(string apiKey);
    }
}