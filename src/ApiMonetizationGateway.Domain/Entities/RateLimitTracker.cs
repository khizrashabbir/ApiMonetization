namespace ApiMonetizationGateway.Domain.Entities
{
    public class RateLimitTracker
    {
        public long Id { get; set; }
        
        public int CustomerId { get; set; }
        
        public DateTime WindowStart { get; set; }
        
        public int RequestCount { get; set; }
        
        public DateTime LastRequest { get; set; }
        
        public Customer? Customer { get; set; }
        
        public RateLimitTracker()
        {
            WindowStart = DateTime.UtcNow;
            LastRequest = DateTime.UtcNow;
            RequestCount = 0;
        }
        
        public static RateLimitTracker CreateForCustomer(int customerId)
        {
            return new RateLimitTracker
            {
                CustomerId = customerId
            };
        }
        
        public void IncrementRequest()
        {
            var now = DateTime.UtcNow;
            
            // Reset window after 1 second
            if ((now - WindowStart).TotalSeconds >= 1)
            {
                WindowStart = now;
                RequestCount = 1;
            }
            else
            {
                RequestCount++;
            }
            
            LastRequest = now;
        }
        
        public bool IsWithinRateLimit(int maxRequestsPerSecond)
        {
            var now = DateTime.UtcNow;
            
            if ((now - WindowStart).TotalSeconds >= 1)
            {
                return true;
            }
            
            return RequestCount < maxRequestsPerSecond;
        }
        
        public int GetCurrentRequestCount()
        {
            var now = DateTime.UtcNow;
            
            if ((now - WindowStart).TotalSeconds >= 1)
            {
                return 0;
            }
            
            return RequestCount;
        }
    }
}