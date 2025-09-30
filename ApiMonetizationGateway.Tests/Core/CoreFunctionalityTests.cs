using ApiMonetizationGateway.Domain.Entities;
using FluentAssertions;
using Xunit;

namespace ApiMonetizationGateway.Tests.Core;

public class CoreFunctionalityTests
{
    [Fact]
    public void Customer_Should_Initialize_With_Correct_Defaults()
    {
        var customer = new Customer();

        customer.Id.Should().Be(0);
        customer.IsActive.Should().BeTrue();
        customer.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        customer.CurrentMonthUsage.Should().Be(0);
    }

    [Fact]
    public void Customer_Should_Increment_Usage_Correctly()
    {
        var customer = new Customer { CurrentMonthUsage = 100 };

        customer.IncrementUsage();

        customer.CurrentMonthUsage.Should().Be(101);
    }

    [Fact]
    public void Customer_Should_Reset_Monthly_Usage()
    {
        var customer = new Customer { CurrentMonthUsage = 500 };
        var expectedResetDate = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);

        customer.ResetMonthlyUsage();

        customer.CurrentMonthUsage.Should().Be(0);
        customer.LastUsageReset.Should().Be(expectedResetDate);
    }

    [Theory]
    [InlineData(50, 100, false)]
    [InlineData(100, 100, true)]
    [InlineData(150, 100, true)]
    public void Customer_Should_Check_Monthly_Quota_Correctly(int usage, int quota, bool expectedExceeded)
    {
        var tier = new Tier { MonthlyQuota = quota };
        var customer = new Customer 
        { 
            CurrentMonthUsage = usage,
            Tier = tier 
        };

        var hasExceeded = customer.HasExceededMonthlyQuota();

        hasExceeded.Should().Be(expectedExceeded);
    }

    [Fact]
    public void Tier_Should_Create_Free_Tier_With_Correct_Limits()
    {
        var freeTier = Tier.CreateFreeTier();

        freeTier.Name.Should().Be("Free");
        freeTier.RateLimitPerSecond.Should().Be(2);
        freeTier.MonthlyQuota.Should().Be(100);
        freeTier.MonthlyPrice.Should().Be(0.00m);
    }

    [Theory]
    [InlineData(1, 2, true)]
    [InlineData(2, 2, false)]
    [InlineData(3, 2, false)]
    public void Tier_Should_Check_Rate_Limit_Correctly(int currentRequests, int rateLimit, bool expectedWithinLimit)
    {
        var tier = new Tier { RateLimitPerSecond = rateLimit };

        var withinLimit = tier.IsWithinRateLimit(currentRequests);

        withinLimit.Should().Be(expectedWithinLimit);
    }

    [Fact]
    public void RateLimitTracker_Should_Initialize_For_Customer()
    {
        const int customerId = 123;

        var tracker = RateLimitTracker.CreateForCustomer(customerId);

        tracker.CustomerId.Should().Be(customerId);
        tracker.RequestCount.Should().Be(0);
        tracker.WindowStart.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void RateLimitTracker_Should_Increment_Request_Count()
    {
        var tracker = RateLimitTracker.CreateForCustomer(1);

        tracker.IncrementRequest();
        tracker.IncrementRequest();

        tracker.RequestCount.Should().Be(2);
    }

    [Fact]
    public void RateLimitTracker_Should_Check_If_Within_Rate_Limit()
    {
        var tracker = RateLimitTracker.CreateForCustomer(1);
        tracker.IncrementRequest();
        tracker.IncrementRequest();

        tracker.IsWithinRateLimit(5).Should().BeTrue();
        tracker.IsWithinRateLimit(2).Should().BeFalse();
        tracker.IsWithinRateLimit(1).Should().BeFalse();
    }

    [Fact]
    public void ApiUsageLog_Should_Calculate_Cost_Correctly()
    {
        var pricePerRequest = 0.05m;
        var requestCount = 10;
        var usageLog = new ApiUsageLog
        {
            CustomerId = 1,
            Endpoint = "/api/test",
            Timestamp = DateTime.UtcNow
        };

        var cost = usageLog.CalculateCost(pricePerRequest, requestCount);

        // Assert
        cost.Should().Be(0.50m); // 10 requests * $0.05 = $0.50
    }
}