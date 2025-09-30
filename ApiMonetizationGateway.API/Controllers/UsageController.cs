using ApiMonetizationGateway.Application.Services;
using ApiMonetizationGateway.Domain.Entities;
using ApiMonetizationGateway.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ApiMonetizationGateway.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsageController : ControllerBase
    {
        private readonly IApiUsageLogRepository _usageLogRepository;
        private readonly IMonthlyUsageSummaryRepository _summaryRepository;
        private readonly ICustomerRepository _customerRepository;
        private readonly IApiUsageTrackingService _usageTrackingService;
        private readonly ILogger<UsageController> _logger;
        
        public UsageController(
            IApiUsageLogRepository usageLogRepository,
            IMonthlyUsageSummaryRepository summaryRepository,
            ICustomerRepository customerRepository,
            IApiUsageTrackingService usageTrackingService,
            ILogger<UsageController> logger)
        {
            _usageLogRepository = usageLogRepository;
            _summaryRepository = summaryRepository;
            _customerRepository = customerRepository;
            _usageTrackingService = usageTrackingService;
            _logger = logger;
        }
        
        [HttpGet("logs/{customerId}")]
        public async Task<ActionResult<IEnumerable<ApiUsageLog>>> GetUsageLogs(
            int customerId, 
            [FromQuery] DateTime? fromDate, 
            [FromQuery] DateTime? toDate,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            try
            {
                var customer = await _customerRepository.GetByIdAsync(customerId);
                if (customer == null)
                {
                    return NotFound($"Customer with ID {customerId} not found");
                }
                
                var logs = await _usageLogRepository.GetByCustomerIdAsync(customerId, fromDate, toDate);
                
                // Simple pagination
                var pagedLogs = logs.Skip((page - 1) * pageSize).Take(pageSize);
                
                return Ok(new
                {
                    Data = pagedLogs,
                    Page = page,
                    PageSize = pageSize,
                    TotalRecords = logs.Count()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving usage logs for customer {CustomerId}", customerId);
                return StatusCode(500, "Internal server error");
            }
        }
        
        [HttpGet("summary/{customerId}")]
        public async Task<ActionResult<IEnumerable<MonthlyUsageSummary>>> GetUsageSummary(int customerId)
        {
            try
            {
                var customer = await _customerRepository.GetByIdAsync(customerId);
                if (customer == null)
                {
                    return NotFound($"Customer with ID {customerId} not found");
                }
                
                var summaries = await _summaryRepository.GetByCustomerIdAsync(customerId);
                return Ok(summaries);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving usage summary for customer {CustomerId}", customerId);
                return StatusCode(500, "Internal server error");
            }
        }
        
        [HttpGet("summary/month/{year}/{month}")]
        public async Task<ActionResult<IEnumerable<MonthlyUsageSummary>>> GetMonthlySummary(int year, int month)
        {
            try
            {
                var summaries = await _summaryRepository.GetAllForMonthAsync(year, month);
                return Ok(summaries);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving monthly summary for {Year}-{Month}", year, month);
                return StatusCode(500, "Internal server error");
            }
        }
        
        [HttpGet("statistics/{customerId}")]
        public async Task<ActionResult<UsageStatistics>> GetUsageStatistics(
            int customerId, 
            [FromQuery] int? year, 
            [FromQuery] int? month)
        {
            try
            {
                var customer = await _customerRepository.GetByIdAsync(customerId);
                if (customer == null)
                {
                    return NotFound($"Customer with ID {customerId} not found");
                }
                
                var statistics = await _usageTrackingService.GetUsageStatisticsAsync(customerId, year, month);
                return Ok(statistics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving usage statistics for customer {CustomerId}", customerId);
                return StatusCode(500, "Internal server error");
            }
        }
        
        [HttpGet("dashboard")]
        public async Task<ActionResult<DashboardData>> GetDashboard()
        {
            try
            {
                var customers = await _customerRepository.GetAllAsync();
                var now = DateTime.UtcNow;
                var currentMonthSummaries = await _summaryRepository.GetAllForMonthAsync(now.Year, now.Month);
                
                var totalCustomers = customers.Count();
                var activeCustomers = customers.Count(c => c.IsActive);
                var totalRevenue = currentMonthSummaries.Sum(s => s.TotalCost);
                var totalRequests = currentMonthSummaries.Sum(s => s.TotalRequests);
                
                var topCustomers = currentMonthSummaries
                    .OrderByDescending(s => s.TotalCost)
                    .Take(10)
                    .Select(s => new TopCustomer
                    {
                        CustomerId = s.CustomerId,
                        CustomerName = s.Customer?.Name ?? "Unknown",
                        TotalRequests = s.TotalRequests,
                        TotalCost = s.TotalCost
                    })
                    .ToList();
                
                var dashboardData = new DashboardData
                {
                    TotalCustomers = totalCustomers,
                    ActiveCustomers = activeCustomers,
                    MonthlyRevenue = totalRevenue,
                    MonthlyRequests = totalRequests,
                    TopCustomers = topCustomers,
                    Period = $"{now:yyyy-MM}"
                };
                
                return Ok(dashboardData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving dashboard data");
                return StatusCode(500, "Internal server error");
            }
        }
    }
    
    public class DashboardData
    {
        public int TotalCustomers { get; set; }
        public int ActiveCustomers { get; set; }
        public decimal MonthlyRevenue { get; set; }
        public int MonthlyRequests { get; set; }
        public List<TopCustomer> TopCustomers { get; set; } = new();
        public string Period { get; set; } = string.Empty;
    }
    
    public class TopCustomer
    {
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public int TotalRequests { get; set; }
        public decimal TotalCost { get; set; }
    }
}