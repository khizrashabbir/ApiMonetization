using System.ComponentModel.DataAnnotations;

namespace ApiMonetizationGateway.Domain.Entities
{
    public class Tier
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(50)]
        public string Name { get; set; } = string.Empty;
        
        [Required]
        [StringLength(200)]
        public string Description { get; set; } = string.Empty;
        
        public int MonthlyQuota { get; set; }
        
        public int RateLimitPerSecond { get; set; }
        
        public decimal MonthlyPrice { get; set; }
        
        public DateTime CreatedAt { get; set; }
        
        public DateTime UpdatedAt { get; set; }
        
        public bool IsActive { get; set; }
        
        public Tier()
        {
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
            IsActive = true;
        }
        
        public bool IsWithinRateLimit(int currentRequestsThisSecond)
        {
            return currentRequestsThisSecond < RateLimitPerSecond;
        }
        
        public bool IsWithinMonthlyQuota(int currentMonthlyUsage)
        {
            return currentMonthlyUsage < MonthlyQuota;
        }
        
        public static Tier CreateFreeTier()
        {
            return new Tier
            {
                Name = "Free",
                Description = "Free tier with basic access",
                MonthlyQuota = 100,
                RateLimitPerSecond = 2,
                MonthlyPrice = 0m
            };
        }
        
        public static Tier CreateProTier()
        {
            return new Tier
            {
                Name = "Pro",
                Description = "Professional tier with enhanced access",
                MonthlyQuota = 100000,
                RateLimitPerSecond = 10,
                MonthlyPrice = 50m
            };
        }
    }
}