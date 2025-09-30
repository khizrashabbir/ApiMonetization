using ApiMonetizationGateway.Application.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace ApiMonetizationGateway.Application.Middleware
{
    public class RateLimitMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RateLimitMiddleware> _logger;
        
        public RateLimitMiddleware(RequestDelegate next, ILogger<RateLimitMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }
        
        public async Task InvokeAsync(HttpContext context, IRateLimitService rateLimitService)
        {
            if (ShouldSkipRateLimit(context.Request.Path))
            {
                await _next(context);
                return;
            }
            
            var apiKey = ExtractApiKey(context.Request);
            
            if (string.IsNullOrEmpty(apiKey))
            {
                await WriteErrorResponse(context, 401, "API key is required", "MISSING_API_KEY");
                return;
            }
            
            var rateLimitResult = await rateLimitService.CheckRateLimitAsync(apiKey);
            
            if (!rateLimitResult.IsAllowed)
            {
                _logger.LogWarning("Rate limit exceeded for API key: {ApiKey}. Reason: {Reason}", 
                    apiKey, rateLimitResult.Reason);
                
                context.Response.Headers["X-RateLimit-Remaining"] = rateLimitResult.RemainingRequests.ToString();
                context.Response.Headers["X-RateLimit-Reset"] = DateTimeOffset.UtcNow.Add(rateLimitResult.RetryAfter).ToUnixTimeSeconds().ToString();
                context.Response.Headers["Retry-After"] = ((int)rateLimitResult.RetryAfter.TotalSeconds).ToString();
                
                await WriteErrorResponse(context, 429, rateLimitResult.Reason, "RATE_LIMIT_EXCEEDED");
                return;
            }
            
            await rateLimitService.RecordRequestAsync(apiKey);
            
            context.Response.Headers["X-RateLimit-Remaining"] = rateLimitResult.RemainingRequests.ToString();
            
            // Store for downstream services
            context.Items["ApiKey"] = apiKey;
            
            await _next(context);
        }
        
        private static bool ShouldSkipRateLimit(PathString path)
        {
            var pathString = path.ToString().ToLower();
            return pathString.StartsWith("/health") || 
                   pathString.StartsWith("/swagger") || 
                   pathString.StartsWith("/api/admin") ||
                   pathString == "/";
        }
        
        private static string? ExtractApiKey(HttpRequest request)
        {
            // Check Authorization header first
            if (request.Headers.TryGetValue("Authorization", out var authHeader))
            {
                var authValue = authHeader.FirstOrDefault();
                if (!string.IsNullOrEmpty(authValue) && authValue.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                {
                    return authValue[7..];
                }
            }
            
            if (request.Headers.TryGetValue("X-API-Key", out var apiKeyHeader))
            {
                return apiKeyHeader.FirstOrDefault();
            }
            
            if (request.Query.TryGetValue("apiKey", out var queryApiKey))
            {
                return queryApiKey.FirstOrDefault();
            }
            
            return null;
        }
        
        private static async Task WriteErrorResponse(HttpContext context, int statusCode, string message, string errorCode)
        {
            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/json";
            
            var errorResponse = new
            {
                Error = new
                {
                    Code = errorCode,
                    Message = message,
                    Timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
                }
            };
            
            var json = JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            
            await context.Response.WriteAsync(json);
        }
    }
}