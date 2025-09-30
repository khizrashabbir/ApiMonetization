using Microsoft.AspNetCore.Mvc;

namespace ApiMonetizationGateway.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TestController : ControllerBase
    {
        private readonly ILogger<TestController> _logger;
        
        public TestController(ILogger<TestController> logger)
        {
            _logger = logger;
        }
        
        [HttpGet("hello")]
        public IActionResult Hello()
        {
            return Ok(new { 
                message = "Hello from API Monetization Gateway!", 
                timestamp = DateTime.UtcNow,
                version = "1.0.0"
            });
        }
        
        [HttpGet("protected")]
        public IActionResult ProtectedEndpoint()
        {
            // Extract customer info from context (set by middleware)
            var apiKey = HttpContext.Items["ApiKey"]?.ToString();
            
            return Ok(new { 
                message = "This is a protected endpoint that counts towards your quota",
                apiKey = apiKey,
                timestamp = DateTime.UtcNow
            });
        }
        
        [HttpPost("data")]
        public IActionResult PostData([FromBody] TestDataRequest request)
        {
            _logger.LogInformation("Received test data: {Data}", request.Data);
            
            return Ok(new { 
                message = "Data received successfully",
                receivedData = request.Data,
                timestamp = DateTime.UtcNow
            });
        }
        
        [HttpGet("slow")]
        public async Task<IActionResult> SlowEndpoint()
        {
            // Simulate slow processing
            await Task.Delay(2000);
            
            return Ok(new { 
                message = "This was a slow endpoint (2 second delay)",
                timestamp = DateTime.UtcNow
            });
        }
        
        [HttpGet("error")]
        public IActionResult ErrorEndpoint()
        {
            // Simulate an error for testing
            throw new InvalidOperationException("This is a test error");
        }
    }
    
    public class TestDataRequest
    {
        public string Data { get; set; } = string.Empty;
    }
}