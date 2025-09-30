namespace ApiMonetizationGateway.Domain.ValueObjects
{
    public record RateLimitResult
    {
        public bool IsAllowed { get; init; }
        public string Reason { get; init; } = string.Empty;
        public int RemainingRequests { get; init; }
        public TimeSpan RetryAfter { get; init; }
        
        public static RateLimitResult Allow(int remainingRequests)
        {
            return new RateLimitResult 
            { 
                IsAllowed = true, 
                RemainingRequests = remainingRequests 
            };
        }
        
        public static RateLimitResult Deny(string reason, TimeSpan retryAfter, int remainingRequests = 0)
        {
            return new RateLimitResult 
            { 
                IsAllowed = false, 
                Reason = reason, 
                RetryAfter = retryAfter,
                RemainingRequests = remainingRequests
            };
        }
    }
}