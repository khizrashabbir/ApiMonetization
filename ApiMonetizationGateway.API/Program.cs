using ApiMonetizationGateway.Application.Middleware;
using ApiMonetizationGateway.Application.Services;
using ApiMonetizationGateway.Domain.Interfaces;
using ApiMonetizationGateway.Infrastructure.Data;
using ApiMonetizationGateway.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "API Monetization Gateway", Version = "v1" });
    c.AddSecurityDefinition("ApiKey", new()
    {
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Name = "X-API-Key",
        Description = "API Key needed to access the endpoints"
    });
    c.AddSecurityRequirement(new()
    {
        {
            new()
            {
                Reference = new()
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "ApiKey"
                }
            },
            new string[] {}
        }
    });
});

// Database configuration
var connectionString = builder.Configuration.GetConnectionString("Default") 
    ?? "Data Source=.;Initial Catalog=ApiMonetizationGateway;User ID=sa;Password=YourPassword123!;TrustServerCertificate=True";
builder.Services.AddSingleton<IDbConnectionFactory>(new SqlConnectionFactory(connectionString));

// Repository registration
builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
builder.Services.AddScoped<ITierRepository, TierRepository>();
builder.Services.AddScoped<IApiUsageLogRepository, ApiUsageLogRepository>();
builder.Services.AddScoped<IRateLimitRepository, RateLimitRepository>();
builder.Services.AddScoped<IMonthlyUsageSummaryRepository, MonthlyUsageSummaryRepository>();

// Application services
builder.Services.AddScoped<IRateLimitService, RateLimitService>();
builder.Services.AddScoped<IApiUsageTrackingService, ApiUsageTrackingService>();

// Background services - Disabled for now until database is setup
// builder.Services.AddHostedService<MonthlyUsageSummaryService>();

// CORS configuration
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");

// Custom middleware
app.UseMiddleware<RateLimitMiddleware>();
app.UseMiddleware<UsageTrackingMiddleware>();

app.UseRouting();
app.MapControllers();

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

app.Run();

// Make Program class accessible for testing
public partial class Program { }
