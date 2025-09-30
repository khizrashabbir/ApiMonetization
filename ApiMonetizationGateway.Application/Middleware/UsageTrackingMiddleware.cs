using ApiMonetizationGateway.Application.Services;
using ApiMonetizationGateway.Domain.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace ApiMonetizationGateway.Application.Middleware
{
    public class UsageTrackingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<UsageTrackingMiddleware> _logger;
        
        public UsageTrackingMiddleware(RequestDelegate next, ILogger<UsageTrackingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }
        
        public async Task InvokeAsync(HttpContext context, IApiUsageTrackingService usageTrackingService, ICustomerRepository customerRepository)
        {
            // Skip tracking for health checks and non-API endpoints
            if (ShouldSkipTracking(context.Request.Path))
            {
                await _next(context);
                return;
            }
            
            var stopwatch = Stopwatch.StartNew();
            var originalBodyStream = context.Response.Body;
            
            try
            {
                await _next(context);
            }
            finally
            {
                stopwatch.Stop();
                
                // Extract information for logging
                var apiKey = context.Items["ApiKey"]?.ToString();
                if (!string.IsNullOrEmpty(apiKey))
                {
                    await LogUsageAsync(context, usageTrackingService, customerRepository, apiKey, stopwatch.ElapsedMilliseconds);
                }
            }
        }
        
        private async Task LogUsageAsync(HttpContext context, IApiUsageTrackingService usageTrackingService, 
            ICustomerRepository customerRepository, string apiKey, long responseTimeMs)
        {
            try
            {
                var customer = await customerRepository.GetByApiKeyAsync(apiKey);
                if (customer == null) return;
                
                var userId = ExtractUserId(context);
                var endpoint = context.Request.Path.ToString();
                var httpMethod = context.Request.Method;
                var statusCode = context.Response.StatusCode;
                var ipAddress = GetClientIpAddress(context);
                var userAgent = context.Request.Headers["User-Agent"].FirstOrDefault();
                
                await usageTrackingService.LogApiUsageAsync(
                    customer.Id, 
                    userId, 
                    endpoint, 
                    httpMethod, 
                    statusCode, 
                    responseTimeMs, 
                    ipAddress, 
                    userAgent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error tracking API usage for API key {ApiKey}", apiKey);
            }
        }
        
        private static bool ShouldSkipTracking(PathString path)
        {
            var pathString = path.ToString().ToLower();
            return pathString.StartsWith("/health") || 
                   pathString.StartsWith("/swagger") || 
                   pathString.StartsWith("/api/admin") ||
                   pathString == "/";
        }
        
        private static string ExtractUserId(HttpContext context)
        {
            // Try to get user ID from various sources
            
            // From claims (if authenticated)
            var userIdClaim = context.User?.FindFirst("sub") ?? context.User?.FindFirst("user_id") ?? context.User?.FindFirst("id");
            if (userIdClaim != null)
            {
                return userIdClaim.Value;
            }
            
            // From headers
            if (context.Request.Headers.TryGetValue("X-User-Id", out var userIdHeader))
            {
                var userId = userIdHeader.FirstOrDefault();
                if (!string.IsNullOrEmpty(userId))
                    return userId;
            }
            
            // From query parameter
            if (context.Request.Query.TryGetValue("userId", out var queryUserId))
            {
                var userId = queryUserId.FirstOrDefault();
                if (!string.IsNullOrEmpty(userId))
                    return userId;
            }
            
            // Fallback to anonymous
            return "anonymous";
        }
        
        private static string? GetClientIpAddress(HttpContext context)
        {
            // Check for forwarded IP first (for load balancers/proxies)
            if (context.Request.Headers.TryGetValue("X-Forwarded-For", out var forwardedFor))
            {
                var ip = forwardedFor.FirstOrDefault()?.Split(',').FirstOrDefault()?.Trim();
                if (!string.IsNullOrEmpty(ip))
                    return ip;
            }
            
            // Check for real IP header
            if (context.Request.Headers.TryGetValue("X-Real-IP", out var realIp))
            {
                var ip = realIp.FirstOrDefault();
                if (!string.IsNullOrEmpty(ip))
                    return ip;
            }
            
            // Fallback to connection remote IP
            return context.Connection.RemoteIpAddress?.ToString();
        }
    }
}