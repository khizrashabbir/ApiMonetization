using System.ComponentModel.DataAnnotations;

namespace ApiMonetizationGateway.Domain.Entities
{
    public class Customer
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;
        
        [Required]
        [EmailAddress]
        [StringLength(255)]
        public string Email { get; set; } = string.Empty;
        
        [Required]
        [StringLength(50)]
        public string ApiKey { get; set; } = string.Empty;
        
        public int TierId { get; set; }
        
        public DateTime CreatedAt { get; set; }
        
        public DateTime UpdatedAt { get; set; }
        
        public bool IsActive { get; set; }
        
        public Tier? Tier { get; set; }
        
        public int CurrentMonthUsage { get; set; }
        
        public DateTime LastUsageReset { get; set; }
        
        public Customer()
        {
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
            IsActive = true;
            LastUsageReset = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
        }
        
        public void ResetMonthlyUsage()
        {
            CurrentMonthUsage = 0;
            LastUsageReset = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
            UpdatedAt = DateTime.UtcNow;
        }
        
        public void IncrementUsage()
        {
            CurrentMonthUsage++;
            UpdatedAt = DateTime.UtcNow;
        }
        
        public bool HasExceededMonthlyQuota()
        {
            return Tier != null && CurrentMonthUsage >= Tier.MonthlyQuota;
        }
    }
}