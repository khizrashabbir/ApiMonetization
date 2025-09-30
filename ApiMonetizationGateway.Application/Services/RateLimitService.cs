using ApiMonetizationGateway.Domain.Entities;
using ApiMonetizationGateway.Domain.Interfaces;
using ApiMonetizationGateway.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace ApiMonetizationGateway.Application.Services
{
    public interface IRateLimitService
    {
        Task<RateLimitResult> CheckRateLimitAsync(string apiKey);
        Task RecordRequestAsync(string apiKey);
    }
    
    public class RateLimitService : IRateLimitService
    {
        private readonly ICustomerRepository _customerRepository;
        private readonly IRateLimitRepository _rateLimitRepository;
        private readonly ILogger<RateLimitService> _logger;
        
        public RateLimitService(
            ICustomerRepository customerRepository,
            IRateLimitRepository rateLimitRepository,
            ILogger<RateLimitService> logger)
        {
            _customerRepository = customerRepository;
            _rateLimitRepository = rateLimitRepository;
            _logger = logger;
        }
        
        public async Task<RateLimitResult> CheckRateLimitAsync(string apiKey)
        {
            try
            {
                var customer = await _customerRepository.GetByApiKeyAsync(apiKey);
                if (customer?.Tier == null)
                {
                    return RateLimitResult.Deny("Invalid API key", TimeSpan.FromMinutes(1));
                }
                
                // Check monthly quota first
                if (customer.HasExceededMonthlyQuota())
                {
                    _logger.LogWarning("Customer {CustomerId} has exceeded monthly quota of {Quota}. Current usage: {Usage}", 
                        customer.Id, customer.Tier.MonthlyQuota, customer.CurrentMonthUsage);
                    
                    var nextMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1).AddMonths(1);
                    var timeUntilReset = nextMonth - DateTime.UtcNow;
                    
                    return RateLimitResult.Deny("Monthly quota exceeded", timeUntilReset);
                }
                
                // Check rate limiting (per second)
                var rateLimitTracker = await _rateLimitRepository.GetByCustomerIdAsync(customer.Id);
                
                if (rateLimitTracker != null)
                {
                    if (!rateLimitTracker.IsWithinRateLimit(customer.Tier.RateLimitPerSecond))
                    {
                        _logger.LogWarning("Customer {CustomerId} has exceeded rate limit of {RateLimit} requests per second", 
                            customer.Id, customer.Tier.RateLimitPerSecond);
                            
                        return RateLimitResult.Deny("Rate limit exceeded", TimeSpan.FromSeconds(1), 
                            customer.Tier.RateLimitPerSecond - rateLimitTracker.GetCurrentRequestCount());
                    }
                }
                
                var remainingQuota = customer.Tier.MonthlyQuota - customer.CurrentMonthUsage;
                return RateLimitResult.Allow(remainingQuota);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking rate limit for API key {ApiKey}", apiKey);
                return RateLimitResult.Deny("Internal server error", TimeSpan.FromMinutes(1));
            }
        }
        
        public async Task RecordRequestAsync(string apiKey)
        {
            try
            {
                var customer = await _customerRepository.GetByApiKeyAsync(apiKey);
                if (customer == null) return;
                
                // Update rate limit tracker
                var rateLimitTracker = await _rateLimitRepository.GetByCustomerIdAsync(customer.Id);
                
                if (rateLimitTracker == null)
                {
                    rateLimitTracker = RateLimitTracker.CreateForCustomer(customer.Id);
                }
                
                rateLimitTracker.IncrementRequest();
                await _rateLimitRepository.UpsertAsync(rateLimitTracker);
                
                // Update customer monthly usage
                customer.IncrementUsage();
                await _customerRepository.UpdateAsync(customer);
                
                _logger.LogDebug("Recorded request for customer {CustomerId}. Monthly usage: {Usage}/{Quota}", 
                    customer.Id, customer.CurrentMonthUsage, customer.Tier?.MonthlyQuota);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recording request for API key {ApiKey}", apiKey);
            }
        }
    }
}