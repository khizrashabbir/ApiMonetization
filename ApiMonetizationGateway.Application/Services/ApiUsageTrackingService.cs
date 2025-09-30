using ApiMonetizationGateway.Domain.Entities;
using ApiMonetizationGateway.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace ApiMonetizationGateway.Application.Services
{
    public interface IApiUsageTrackingService
    {
        Task LogApiUsageAsync(int customerId, string userId, string endpoint, string httpMethod, 
            int statusCode, long responseTimeMs, string? ipAddress = null, string? userAgent = null);
        Task<UsageStatistics> GetUsageStatisticsAsync(int customerId, int? year = null, int? month = null);
    }
    
    public class UsageStatistics
    {
        public int TotalRequests { get; set; }
        public decimal TotalCost { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public Dictionary<string, int> EndpointBreakdown { get; set; } = new();
        public Dictionary<int, int> StatusCodeBreakdown { get; set; } = new();
    }
    
    public class ApiUsageTrackingService : IApiUsageTrackingService
    {
        private readonly IApiUsageLogRepository _usageLogRepository;
        private readonly ICustomerRepository _customerRepository;
        private readonly ILogger<ApiUsageTrackingService> _logger;
        
        public ApiUsageTrackingService(
            IApiUsageLogRepository usageLogRepository,
            ICustomerRepository customerRepository,
            ILogger<ApiUsageTrackingService> logger)
        {
            _usageLogRepository = usageLogRepository;
            _customerRepository = customerRepository;
            _logger = logger;
        }
        
        public async Task LogApiUsageAsync(int customerId, string userId, string endpoint, string httpMethod, 
            int statusCode, long responseTimeMs, string? ipAddress = null, string? userAgent = null)
        {
            try
            {
                var customer = await _customerRepository.GetByIdAsync(customerId);
                if (customer?.Tier == null)
                {
                    _logger.LogWarning("Attempted to log usage for non-existent customer {CustomerId}", customerId);
                    return;
                }
                
                var log = ApiUsageLog.CreateLog(customerId, userId, endpoint, httpMethod, statusCode, responseTimeMs, ipAddress, userAgent);
                
                // Calculate cost based on tier
                log.CalculateCost(customer.Tier);
                
                await _usageLogRepository.CreateAsync(log);
                
                _logger.LogDebug("Logged API usage for customer {CustomerId}: {Method} {Endpoint} -> {StatusCode} ({ResponseTime}ms)", 
                    customerId, httpMethod, endpoint, statusCode, responseTimeMs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging API usage for customer {CustomerId}", customerId);
            }
        }
        
        public async Task<UsageStatistics> GetUsageStatisticsAsync(int customerId, int? year = null, int? month = null)
        {
            try
            {
                IEnumerable<ApiUsageLog> logs;
                DateTime fromDate, toDate;
                
                if (year.HasValue && month.HasValue)
                {
                    logs = await _usageLogRepository.GetByCustomerIdAndMonthAsync(customerId, year.Value, month.Value);
                    fromDate = new DateTime(year.Value, month.Value, 1);
                    toDate = fromDate.AddMonths(1).AddDays(-1);
                }
                else if (year.HasValue)
                {
                    fromDate = new DateTime(year.Value, 1, 1);
                    toDate = new DateTime(year.Value, 12, 31);
                    logs = await _usageLogRepository.GetByCustomerIdAsync(customerId, fromDate, toDate);
                }
                else
                {
                    var now = DateTime.UtcNow;
                    fromDate = new DateTime(now.Year, now.Month, 1);
                    toDate = fromDate.AddMonths(1).AddDays(-1);
                    logs = await _usageLogRepository.GetByCustomerIdAndMonthAsync(customerId, now.Year, now.Month);
                }
                
                var logsList = logs.ToList();
                
                var statistics = new UsageStatistics
                {
                    TotalRequests = logsList.Count,
                    TotalCost = logsList.Sum(l => l.Cost ?? 0),
                    FromDate = fromDate,
                    ToDate = toDate,
                    EndpointBreakdown = logsList.GroupBy(l => l.Endpoint)
                        .ToDictionary(g => g.Key, g => g.Count()),
                    StatusCodeBreakdown = logsList.GroupBy(l => l.ResponseStatusCode)
                        .ToDictionary(g => g.Key, g => g.Count())
                };
                
                return statistics;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting usage statistics for customer {CustomerId}", customerId);
                return new UsageStatistics();
            }
        }
    }
}