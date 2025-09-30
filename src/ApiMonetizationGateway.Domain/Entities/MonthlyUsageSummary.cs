namespace ApiMonetizationGateway.Domain.Entities
{
    public class MonthlyUsageSummary
    {
        public long Id { get; set; }
        
        public int CustomerId { get; set; }
        
        public int Year { get; set; }
        
        public int Month { get; set; }
        
        public int TotalRequests { get; set; }
        
        public decimal TotalCost { get; set; }
        
        public DateTime CreatedAt { get; set; }
        
        public DateTime UpdatedAt { get; set; }
        
        public Customer? Customer { get; set; }
        
        public MonthlyUsageSummary()
        {
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }
        
        public static MonthlyUsageSummary CreateForCustomer(int customerId, int year, int month)
        {
            return new MonthlyUsageSummary
            {
                CustomerId = customerId,
                Year = year,
                Month = month,
                TotalRequests = 0,
                TotalCost = 0m
            };
        }
        
        public void UpdateUsage(int additionalRequests, decimal additionalCost)
        {
            TotalRequests += additionalRequests;
            TotalCost += additionalCost;
            UpdatedAt = DateTime.UtcNow;
        }
        
        public string GetPeriodDescription()
        {
            return $"{Year:D4}-{Month:D2}";
        }
        
        public bool IsCurrentMonth()
        {
            var now = DateTime.UtcNow;
            return Year == now.Year && Month == now.Month;
        }
    }
}