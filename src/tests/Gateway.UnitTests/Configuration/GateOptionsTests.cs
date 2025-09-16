using Xunit;
using FluentAssertions;
using Gateway.UnitTests.Helpers;

namespace Gateway.UnitTests.Configuration;

public class GateOptionsTests : UnitTestBase
{
    [Fact]
    public void GateOptions_DefaultValues_ShouldBeSetCorrectly()
    {
        // Arrange & Act
        var options = new GateOptions();

        // Assert
        options.Services.Should().NotBeNull().And.BeEmpty();
        options.AllowedRoutes.Should().NotBeNull().And.BeEmpty();
        options.DefaultTimeoutSeconds.Should().Be(30);
        options.MaxPayloadSizeBytes.Should().Be(10_485_760); // 10MB
        options.EnableCaching.Should().BeTrue();
        options.CacheExpirationMinutes.Should().Be(5);
        options.EnableMetrics.Should().BeTrue();
        options.EnableTracing.Should().BeTrue();
    }

    [Fact]
    public void GateOptions_WithCustomValues_ShouldInitializeCorrectly()
    {
        // Arrange & Act
        var options = new GateOptions
        {
            Services = new Dictionary<string, string>
            {
                { "users", "http://users-service:8081" },
                { "reports", "http://reports-service:8083" }
            },
            AllowedRoutes = new List<AllowedRoute>
            {
                new AllowedRoute
                {
                    Service = "users",
                    Methods = new[] { "GET", "POST" },
                    PathPrefix = "/api/v1/users"
                }
            },
            DefaultTimeoutSeconds = 60,
            MaxPayloadSizeBytes = 20_971_520, // 20MB
            EnableCaching = false,
            CacheExpirationMinutes = 10,
            EnableMetrics = false,
            EnableTracing = false
        };

        // Assert
        options.Services.Should().HaveCount(2);
        options.Services["users"].Should().Be("http://users-service:8081");
        options.Services["reports"].Should().Be("http://reports-service:8083");

        options.AllowedRoutes.Should().HaveCount(1);
        options.AllowedRoutes[0].Service.Should().Be("users");
        options.AllowedRoutes[0].Methods.Should().Equal("GET", "POST");
        options.AllowedRoutes[0].PathPrefix.Should().Be("/api/v1/users");

        options.DefaultTimeoutSeconds.Should().Be(60);
        options.MaxPayloadSizeBytes.Should().Be(20_971_520);
        options.EnableCaching.Should().BeFalse();
        options.CacheExpirationMinutes.Should().Be(10);
        options.EnableMetrics.Should().BeFalse();
        options.EnableTracing.Should().BeFalse();
    }

    [Fact]
    public void AllowedRoute_DefaultValues_ShouldBeSetCorrectly()
    {
        // Arrange & Act
        var route = new AllowedRoute
        {
            Service = "test-service",
            Methods = new[] { "GET" },
            PathPrefix = "/api/test"
        };

        // Assert
        route.Service.Should().Be("test-service");
        route.Methods.Should().Equal("GET");
        route.PathPrefix.Should().Be("/api/test");
        route.RequiresAuth.Should().BeFalse();
        route.RequiredRoles.Should().BeNull();
    }

    [Fact]
    public void AllowedRoute_WithAuthAndRoles_ShouldInitializeCorrectly()
    {
        // Arrange & Act
        var route = new AllowedRoute
        {
            Service = "admin-service",
            Methods = new[] { "GET", "POST", "PUT", "DELETE" },
            PathPrefix = "/api/admin",
            RequiresAuth = true,
            RequiredRoles = new[] { "Admin", "SuperUser" }
        };

        // Assert
        route.Service.Should().Be("admin-service");
        route.Methods.Should().Equal("GET", "POST", "PUT", "DELETE");
        route.PathPrefix.Should().Be("/api/admin");
        route.RequiresAuth.Should().BeTrue();
        route.RequiredRoles.Should().Equal("Admin", "SuperUser");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(30)]
    [InlineData(300)]
    [InlineData(3600)]
    public void GateOptions_WithDifferentTimeouts_ShouldAcceptValidValues(int timeoutSeconds)
    {
        // Arrange & Act
        var options = new GateOptions
        {
            DefaultTimeoutSeconds = timeoutSeconds
        };

        // Assert
        options.DefaultTimeoutSeconds.Should().Be(timeoutSeconds);
    }

    [Theory]
    [InlineData(1024)] // 1KB
    [InlineData(1_048_576)] // 1MB
    [InlineData(10_485_760)] // 10MB
    [InlineData(104_857_600)] // 100MB
    public void GateOptions_WithDifferentPayloadSizes_ShouldAcceptValidValues(int payloadSize)
    {
        // Arrange & Act
        var options = new GateOptions
        {
            MaxPayloadSizeBytes = payloadSize
        };

        // Assert
        options.MaxPayloadSizeBytes.Should().Be(payloadSize);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(15)]
    [InlineData(60)]
    public void GateOptions_WithDifferentCacheExpiration_ShouldAcceptValidValues(int expirationMinutes)
    {
        // Arrange & Act
        var options = new GateOptions
        {
            CacheExpirationMinutes = expirationMinutes
        };

        // Assert
        options.CacheExpirationMinutes.Should().Be(expirationMinutes);
    }

    [Fact]
    public void GateOptions_WithMultipleServices_ShouldHandleCorrectly()
    {
        // Arrange & Act
        var options = new GateOptions
        {
            Services = new Dictionary<string, string>
            {
                { "users", "http://users-service:8081" },
                { "reports", "http://reports-service:8083" },
                { "analysis", "http://analysis-service:8082" },
                { "middleware", "http://middleware-service:3001" },
                { "notifications", "http://notifications-service:9001" }
            }
        };

        // Assert
        options.Services.Should().HaveCount(5);
        options.Services.Keys.Should().Contain("users", "reports", "analysis", "middleware", "notifications");
    }

    [Fact]
    public void GateOptions_WithMultipleAllowedRoutes_ShouldHandleCorrectly()
    {
        // Arrange & Act
        var options = new GateOptions
        {
            AllowedRoutes = new List<AllowedRoute>
            {
                new AllowedRoute
                {
                    Service = "users",
                    Methods = new[] { "GET", "POST", "PUT", "DELETE" },
                    PathPrefix = "/api/v1/users"
                },
                new AllowedRoute
                {
                    Service = "reports",
                    Methods = new[] { "GET", "POST" },
                    PathPrefix = "/api/Report",
                    RequiresAuth = true
                },
                new AllowedRoute
                {
                    Service = "analysis",
                    Methods = new[] { "POST" },
                    PathPrefix = "/api/Analysis",
                    RequiresAuth = true,
                    RequiredRoles = new[] { "User", "Analyst" }
                }
            }
        };

        // Assert
        options.AllowedRoutes.Should().HaveCount(3);

        var usersRoute = options.AllowedRoutes.First(r => r.Service == "users");
        usersRoute.Methods.Should().Equal("GET", "POST", "PUT", "DELETE");
        usersRoute.RequiresAuth.Should().BeFalse();

        var reportsRoute = options.AllowedRoutes.First(r => r.Service == "reports");
        reportsRoute.Methods.Should().Equal("GET", "POST");
        reportsRoute.RequiresAuth.Should().BeTrue();
        reportsRoute.RequiredRoles.Should().BeNull();

        var analysisRoute = options.AllowedRoutes.First(r => r.Service == "analysis");
        analysisRoute.Methods.Should().Equal("POST");
        analysisRoute.RequiresAuth.Should().BeTrue();
        analysisRoute.RequiredRoles.Should().Equal("User", "Analyst");
    }
}
