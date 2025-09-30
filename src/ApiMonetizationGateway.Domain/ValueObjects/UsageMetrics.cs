namespace ApiMonetizationGateway.Domain.ValueObjects
{
    public record UsageMetrics
    {
        public int TotalRequests { get; init; }
        public int CurrentMonthRequests { get; init; }
        public decimal CurrentMonthCost { get; init; }
        public DateTime LastRequestTime { get; init; }
        public int RemainingQuota { get; init; }
        public double QuotaUsagePercentage { get; init; }
        
        public static UsageMetrics Create(int totalRequests, int currentMonthRequests, 
            decimal currentMonthCost, DateTime lastRequestTime, int monthlyQuota)
        {
            var remainingQuota = Math.Max(0, monthlyQuota - currentMonthRequests);
            var usagePercentage = monthlyQuota > 0 
                ? (double)currentMonthRequests / monthlyQuota * 100 
                : 0;
                
            return new UsageMetrics
            {
                TotalRequests = totalRequests,
                CurrentMonthRequests = currentMonthRequests,
                CurrentMonthCost = currentMonthCost,
                LastRequestTime = lastRequestTime,
                RemainingQuota = remainingQuota,
                QuotaUsagePercentage = Math.Round(usagePercentage, 2)
            };
        }
    }
}