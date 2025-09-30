using ApiMonetizationGateway.Application.Services;
using ApiMonetizationGateway.Domain.Entities;
using ApiMonetizationGateway.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ApiMonetizationGateway.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CustomersController : ControllerBase
    {
        private readonly ICustomerRepository _customerRepository;
        private readonly ITierRepository _tierRepository;
        private readonly IApiUsageTrackingService _usageTrackingService;
        private readonly ILogger<CustomersController> _logger;
        
        public CustomersController(
            ICustomerRepository customerRepository,
            ITierRepository tierRepository,
            IApiUsageTrackingService usageTrackingService,
            ILogger<CustomersController> logger)
        {
            _customerRepository = customerRepository;
            _tierRepository = tierRepository;
            _usageTrackingService = usageTrackingService;
            _logger = logger;
        }
        
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Customer>>> GetCustomers()
        {
            try
            {
                var customers = await _customerRepository.GetAllAsync();
                return Ok(customers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving customers");
                return StatusCode(500, "Internal server error");
            }
        }
        
        [HttpGet("{id}")]
        public async Task<ActionResult<Customer>> GetCustomer(int id)
        {
            try
            {
                var customer = await _customerRepository.GetByIdAsync(id);
                if (customer == null)
                {
                    return NotFound($"Customer with ID {id} not found");
                }
                
                return Ok(customer);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving customer {CustomerId}", id);
                return StatusCode(500, "Internal server error");
            }
        }
        
        [HttpPost]
        public async Task<ActionResult<Customer>> CreateCustomer(CreateCustomerRequest request)
        {
            try
            {
                // Validate tier exists
                var tier = await _tierRepository.GetByIdAsync(request.TierId);
                if (tier == null)
                {
                    return BadRequest($"Tier with ID {request.TierId} not found");
                }
                
                // Check if API key already exists
                if (await _customerRepository.ApiKeyExistsAsync(request.ApiKey))
                {
                    return BadRequest("API key already exists");
                }
                
                var customer = new Customer
                {
                    Name = request.Name,
                    Email = request.Email,
                    ApiKey = request.ApiKey,
                    TierId = request.TierId
                };
                
                var createdCustomer = await _customerRepository.CreateAsync(customer);
                
                return CreatedAtAction(nameof(GetCustomer), new { id = createdCustomer.Id }, createdCustomer);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating customer");
                return StatusCode(500, "Internal server error");
            }
        }
        
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCustomer(int id, UpdateCustomerRequest request)
        {
            try
            {
                var customer = await _customerRepository.GetByIdAsync(id);
                if (customer == null)
                {
                    return NotFound($"Customer with ID {id} not found");
                }
                
                // Validate tier exists if changed
                if (request.TierId.HasValue && request.TierId != customer.TierId)
                {
                    var tier = await _tierRepository.GetByIdAsync(request.TierId.Value);
                    if (tier == null)
                    {
                        return BadRequest($"Tier with ID {request.TierId} not found");
                    }
                    customer.TierId = request.TierId.Value;
                }
                
                // Update fields if provided
                customer.Name = request.Name ?? customer.Name;
                customer.Email = request.Email ?? customer.Email;
                if (request.IsActive.HasValue)
                    customer.IsActive = request.IsActive.Value;
                
                customer.UpdatedAt = DateTime.UtcNow;
                
                await _customerRepository.UpdateAsync(customer);
                
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating customer {CustomerId}", id);
                return StatusCode(500, "Internal server error");
            }
        }
        
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCustomer(int id)
        {
            try
            {
                var customer = await _customerRepository.GetByIdAsync(id);
                if (customer == null)
                {
                    return NotFound($"Customer with ID {id} not found");
                }
                
                await _customerRepository.DeleteAsync(id);
                
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting customer {CustomerId}", id);
                return StatusCode(500, "Internal server error");
            }
        }
        
        [HttpGet("{id}/usage")]
        public async Task<ActionResult<UsageStatistics>> GetCustomerUsage(int id, [FromQuery] int? year, [FromQuery] int? month)
        {
            try
            {
                var customer = await _customerRepository.GetByIdAsync(id);
                if (customer == null)
                {
                    return NotFound($"Customer with ID {id} not found");
                }
                
                var statistics = await _usageTrackingService.GetUsageStatisticsAsync(id, year, month);
                return Ok(statistics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving usage statistics for customer {CustomerId}", id);
                return StatusCode(500, "Internal server error");
            }
        }
        
        [HttpPost("{id}/reset-usage")]
        public async Task<IActionResult> ResetCustomerUsage(int id)
        {
            try
            {
                var customer = await _customerRepository.GetByIdAsync(id);
                if (customer == null)
                {
                    return NotFound($"Customer with ID {id} not found");
                }
                
                customer.ResetMonthlyUsage();
                await _customerRepository.UpdateAsync(customer);
                
                return Ok(new { message = "Monthly usage reset successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting usage for customer {CustomerId}", id);
                return StatusCode(500, "Internal server error");
            }
        }
    }
    
    public class CreateCustomerRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string ApiKey { get; set; } = string.Empty;
        public int TierId { get; set; }
    }
    
    public class UpdateCustomerRequest
    {
        public string? Name { get; set; }
        public string? Email { get; set; }
        public int? TierId { get; set; }
        public bool? IsActive { get; set; }
    }
}