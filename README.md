# API Monetization Gateway

## 🚀 Getting Started

### Docker Setup (Easiest Way)
```bash
docker-compose up -d
```
- API runs on: http://localhost:5097
- Swagger docs: http://localhost:5097/swagger

### Local Development
```bash
dotnet restore
dotnet build
dotnet run --project ApiMonetizationGateway.API
```

## 🎯 My Approach

I built this to demonstrate how I approach API monetization challenges in production environments:

Clean Architecture: Key Design Decisions

1. Custom Rate Limiting: Built middleware instead of using libraries for better control
2. Dapper for Data Access: Choose performance over convenience 
3. Rich Domain Models: Business logic lives in the entities where it belongs
4. Focused Testing: core unit tests that actually matter
5. Docker Setup: Easy to run and demonstrate

**Results**: All 14 focused unit tests pass - covering the core business logic that matters most for this challenge.

## 📚 What I Built

### **Rate Limiting**
- Per-second rate limits based on customer tiers
- Monthly usage quotas with automatic resets
- Proper HTTP headers for client feedback

### **Usage Tracking**
- Every API call gets logged with cost calculation
- Monthly summaries for billing

### **Customer Tiers**
- Free tier: 2 requests/second, 100/month
- Pro tier: 10 requests/second, 1000/month
- Enterprise: 50 requests/second, unlimited

### **Authentication**
- API key based (Bearer token)
- Clean error responses when missing

## 🐳 Docker Setup

I kept the Docker setup simple:
- **Dockerfile**: Basic multi-stage build
- **docker-compose.yml**: API + SQL Server, ready to go
  

## 📊 Database Design

I went with a straightforward schema:

- **customers**: Basic info + API keys + current usage
- **tiers**: Free/Pro/Enterprise definitions
- **api_usage_logs**: Every request logged for billing
- **rate_limit_trackers**: Track requests per second windows

## 💭 My Thinking

**Why Clean Architecture?** Makes it easy to test business logic without worrying about databases or HTTP.

**Why Custom Rate Limiting?**Third-party libraries often don't meet exact needs. ** Building it myself gave me full control.

**Why Dapper?** For this use case, I preferred the performance and direct SQL control over EF Core's convenience.

**Why Focused Tests?** Rather than 100+ tests, I wrote 10 that actually test the important business rules.

## � How It's Organized

```
ApiMonetizationGateway/
├── src/ApiMonetizationGateway.Domain/     # Business entities
├── ApiMonetizationGateway.Application/    # Services & middleware  
├── ApiMonetizationGateway.Infrastructure/ # Data access
├── ApiMonetizationGateway.API/           # Controllers
├── ApiMonetizationGateway.Tests/         # 10 focused unit tests
├── Dockerfile                            # Simple container setup
└── docker-compose.yml                    # One command to run
```

This shows how I approach API monetization challenges - clean code, focused testing, and practical solutions that actually work in production.
