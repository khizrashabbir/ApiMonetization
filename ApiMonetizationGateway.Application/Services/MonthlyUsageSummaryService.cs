using ApiMonetizationGateway.Domain.Entities;
using ApiMonetizationGateway.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ApiMonetizationGateway.Application.Services
{
    public class MonthlyUsageSummaryService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<MonthlyUsageSummaryService> _logger;
        private readonly TimeSpan _checkInterval = TimeSpan.FromHours(1); // Check every hour
        
        public MonthlyUsageSummaryService(IServiceProvider serviceProvider, ILogger<MonthlyUsageSummaryService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }
        
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Monthly Usage Summary Service started");
            
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessMonthlySummaries();
                    await Task.Delay(_checkInterval, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    // Expected when cancellation is requested
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in Monthly Usage Summary Service");
                    await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken); // Wait 5 minutes before retrying
                }
            }
            
            _logger.LogInformation("Monthly Usage Summary Service stopped");
        }
        
        private async Task ProcessMonthlySummaries()
        {
            using var scope = _serviceProvider.CreateScope();
            var customerRepository = scope.ServiceProvider.GetRequiredService<ICustomerRepository>();
            var usageLogRepository = scope.ServiceProvider.GetRequiredService<IApiUsageLogRepository>();
            var summaryRepository = scope.ServiceProvider.GetRequiredService<IMonthlyUsageSummaryRepository>();
            
            var now = DateTime.UtcNow;
            var currentYear = now.Year;
            var currentMonth = now.Month;
            
            // Process current month for all customers
            var customers = await customerRepository.GetAllAsync();
            
            foreach (var customer in customers.Where(c => c.IsActive))
            {
                await ProcessCustomerMonthlySummary(customer, currentYear, currentMonth, 
                    usageLogRepository, summaryRepository);
                
                // Also process previous month if it's early in the current month
                if (now.Day <= 3)
                {
                    var prevMonth = now.AddMonths(-1);
                    await ProcessCustomerMonthlySummary(customer, prevMonth.Year, prevMonth.Month, 
                        usageLogRepository, summaryRepository);
                }
            }
            
            // Reset monthly usage for customers if needed
            await ResetMonthlyUsageIfNeeded(customerRepository);
            
            _logger.LogDebug("Processed monthly summaries for {CustomerCount} customers", customers.Count());
        }
        
        private async Task ProcessCustomerMonthlySummary(Customer customer, int year, int month,
            IApiUsageLogRepository usageLogRepository, IMonthlyUsageSummaryRepository summaryRepository)
        {
            try
            {
                var requestCount = await usageLogRepository.GetMonthlyRequestCountAsync(customer.Id, year, month);
                var totalCost = await usageLogRepository.GetMonthlyCostAsync(customer.Id, year, month);
                
                if (requestCount == 0) return; // No usage this month
                
                var existingSummary = await summaryRepository.GetByCustomerAndMonthAsync(customer.Id, year, month);
                
                if (existingSummary == null)
                {
                    existingSummary = MonthlyUsageSummary.CreateForCustomer(customer.Id, year, month);
                }
                
                existingSummary.UpdateUsage(requestCount - existingSummary.TotalRequests, 
                    totalCost - existingSummary.TotalCost);
                
                await summaryRepository.UpsertAsync(existingSummary);
                
                _logger.LogDebug("Updated monthly summary for customer {CustomerId} for {Year}-{Month}: {Requests} requests, ${Cost}", 
                    customer.Id, year, month, requestCount, totalCost);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing monthly summary for customer {CustomerId} for {Year}-{Month}", 
                    customer.Id, year, month);
            }
        }
        
        private async Task ResetMonthlyUsageIfNeeded(ICustomerRepository customerRepository)
        {
            try
            {
                var customers = await customerRepository.GetAllAsync();
                var now = DateTime.UtcNow;
                var currentMonthStart = new DateTime(now.Year, now.Month, 1);
                
                foreach (var customer in customers.Where(c => c.IsActive))
                {
                    // Check if customer's usage needs to be reset
                    if (customer.LastUsageReset < currentMonthStart)
                    {
                        customer.ResetMonthlyUsage();
                        await customerRepository.UpdateAsync(customer);
                        
                        _logger.LogInformation("Reset monthly usage for customer {CustomerId}", customer.Id);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting monthly usage");
            }
        }
    }
}