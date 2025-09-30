using System.ComponentModel.DataAnnotations;

namespace ApiMonetizationGateway.Domain.Entities
{
    public class ApiUsageLog
    {
        public long Id { get; set; }
        
        public int CustomerId { get; set; }
        
        [Required]
        [StringLength(50)]
        public string UserId { get; set; } = string.Empty;
        
        [Required]
        [StringLength(255)]
        public string Endpoint { get; set; } = string.Empty;
        
        [Required]
        [StringLength(10)]
        public string HttpMethod { get; set; } = string.Empty;
        
        public DateTime Timestamp { get; set; }
        
        public int ResponseStatusCode { get; set; }
        
        public long ResponseTimeMs { get; set; }
        
        [StringLength(50)]
        public string? IpAddress { get; set; }
        
        [StringLength(500)]
        public string? UserAgent { get; set; }
        
        public decimal? Cost { get; set; }
        
        public Customer? Customer { get; set; }
        
        public ApiUsageLog()
        {
            Timestamp = DateTime.UtcNow;
        }
        
        public static ApiUsageLog CreateLog(int customerId, string userId, string endpoint, 
            string httpMethod, int statusCode, long responseTime, string? ipAddress = null, 
            string? userAgent = null)
        {
            return new ApiUsageLog
            {
                CustomerId = customerId,
                UserId = userId,
                Endpoint = endpoint,
                HttpMethod = httpMethod,
                ResponseStatusCode = statusCode,
                ResponseTimeMs = responseTime,
                IpAddress = ipAddress,
                UserAgent = userAgent
            };
        }
        
        public void CalculateCost(Tier tier)
        {
            if (tier.MonthlyPrice > 0 && tier.MonthlyQuota > 0)
            {
                Cost = tier.MonthlyPrice / tier.MonthlyQuota;
            }
            else
            {
                Cost = 0m;
            }
        }

        public decimal CalculateCost(decimal pricePerRequest, int requestCount = 1)
        {
            return requestCount * pricePerRequest;
        }
    }
}