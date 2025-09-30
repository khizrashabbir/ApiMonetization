using ApiMonetizationGateway.Domain.Entities;
using ApiMonetizationGateway.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ApiMonetizationGateway.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TiersController : ControllerBase
    {
        private readonly ITierRepository _tierRepository;
        private readonly ILogger<TiersController> _logger;
        
        public TiersController(ITierRepository tierRepository, ILogger<TiersController> logger)
        {
            _tierRepository = tierRepository;
            _logger = logger;
        }
        
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Tier>>> GetTiers()
        {
            try
            {
                var tiers = await _tierRepository.GetAllActiveAsync();
                return Ok(tiers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving tiers");
                return StatusCode(500, "Internal server error");
            }
        }
        
        [HttpGet("{id}")]
        public async Task<ActionResult<Tier>> GetTier(int id)
        {
            try
            {
                var tier = await _tierRepository.GetByIdAsync(id);
                if (tier == null)
                {
                    return NotFound($"Tier with ID {id} not found");
                }
                
                return Ok(tier);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving tier {TierId}", id);
                return StatusCode(500, "Internal server error");
            }
        }
        
        [HttpPost]
        public async Task<ActionResult<Tier>> CreateTier(CreateTierRequest request)
        {
            try
            {
                var tier = new Tier
                {
                    Name = request.Name,
                    Description = request.Description,
                    MonthlyQuota = request.MonthlyQuota,
                    RateLimitPerSecond = request.RateLimitPerSecond,
                    MonthlyPrice = request.MonthlyPrice
                };
                
                var createdTier = await _tierRepository.CreateAsync(tier);
                
                return CreatedAtAction(nameof(GetTier), new { id = createdTier.Id }, createdTier);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating tier");
                return StatusCode(500, "Internal server error");
            }
        }
        
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTier(int id, UpdateTierRequest request)
        {
            try
            {
                var tier = await _tierRepository.GetByIdAsync(id);
                if (tier == null)
                {
                    return NotFound($"Tier with ID {id} not found");
                }
                
                // Update fields if provided
                tier.Name = request.Name ?? tier.Name;
                tier.Description = request.Description ?? tier.Description;
                tier.MonthlyQuota = request.MonthlyQuota ?? tier.MonthlyQuota;
                tier.RateLimitPerSecond = request.RateLimitPerSecond ?? tier.RateLimitPerSecond;
                tier.MonthlyPrice = request.MonthlyPrice ?? tier.MonthlyPrice;
                if (request.IsActive.HasValue)
                    tier.IsActive = request.IsActive.Value;
                
                tier.UpdatedAt = DateTime.UtcNow;
                
                await _tierRepository.UpdateAsync(tier);
                
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating tier {TierId}", id);
                return StatusCode(500, "Internal server error");
            }
        }
        
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTier(int id)
        {
            try
            {
                var tier = await _tierRepository.GetByIdAsync(id);
                if (tier == null)
                {
                    return NotFound($"Tier with ID {id} not found");
                }
                
                await _tierRepository.DeleteAsync(id);
                
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting tier {TierId}", id);
                return StatusCode(500, "Internal server error");
            }
        }
    }
    
    public class CreateTierRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int MonthlyQuota { get; set; }
        public int RateLimitPerSecond { get; set; }
        public decimal MonthlyPrice { get; set; }
    }
    
    public class UpdateTierRequest
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public int? MonthlyQuota { get; set; }
        public int? RateLimitPerSecond { get; set; }
        public decimal? MonthlyPrice { get; set; }
        public bool? IsActive { get; set; }
    }
}