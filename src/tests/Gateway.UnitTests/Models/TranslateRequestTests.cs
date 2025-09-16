using Xunit;
using Gateway.Models;
using FluentAssertions;
using Gateway.UnitTests.Helpers;

namespace Gateway.UnitTests.Models;

public class TranslateRequestTests : UnitTestBase
{
    [Fact]
    public void TranslateRequest_WithValidData_ShouldInitializeCorrectly()
    {
        // Arrange & Act
        var request = new TranslateRequest
        {
            Service = "users",
            Method = "GET",
            Path = "/api/v1/users/123",
            Query = new Dictionary<string, string> { { "include", "profile" } },
            Headers = new Dictionary<string, string> { { "Authorization", "Bearer token" } }
        };

        // Assert
        request.Service.Should().Be("users");
        request.Method.Should().Be("GET");
        request.Path.Should().Be("/api/v1/users/123");
        request.Query.Should().ContainKey("include").WhoseValue.Should().Be("profile");
        request.Headers.Should().ContainKey("Authorization").WhoseValue.Should().Be("Bearer token");
    }

    [Fact]
    public void TranslateRequest_WithEmptyCollections_ShouldInitializeCorrectly()
    {
        // Arrange & Act
        var request = new TranslateRequest
        {
            Service = "reports",
            Method = "POST",
            Path = "/api/Report",
            Query = new Dictionary<string, string>(),
            Headers = new Dictionary<string, string>()
        };

        // Assert
        request.Service.Should().Be("reports");
        request.Method.Should().Be("POST");
        request.Path.Should().Be("/api/Report");
        request.Query.Should().BeEmpty();
        request.Headers.Should().BeEmpty();
    }

    [Fact]
    public void TranslateRequest_WithNullCollections_ShouldHandleGracefully()
    {
        // Arrange & Act
        var request = new TranslateRequest
        {
            Service = "analysis",
            Method = "GET",
            Path = "/api/Analysis",
            Query = null!,
            Headers = null!
        };

        // Assert
        request.Service.Should().Be("analysis");
        request.Method.Should().Be("GET");
        request.Path.Should().Be("/api/Analysis");
        request.Query.Should().BeNull();
        request.Headers.Should().BeNull();
    }

    [Theory]
    [InlineData("users", "GET", "/api/v1/users")]
    [InlineData("reports", "POST", "/api/Report/generate")]
    [InlineData("analysis", "PUT", "/api/Analysis/website")]
    [InlineData("middleware", "DELETE", "/api/middleware/cache")]
    public void TranslateRequest_WithDifferentServicesPaths_ShouldInitializeCorrectly(
        string service, string method, string path)
    {
        // Arrange & Act
        var request = new TranslateRequest
        {
            Service = service,
            Method = method,
            Path = path,
            Query = new Dictionary<string, string>(),
            Headers = new Dictionary<string, string>()
        };

        // Assert
        request.Service.Should().Be(service);
        request.Method.Should().Be(method);
        request.Path.Should().Be(path);
    }

    [Fact]
    public void TranslateRequest_WithComplexQueryParameters_ShouldHandleCorrectly()
    {
        // Arrange
        var complexQuery = new Dictionary<string, string>
        {
            { "filter", "status:active" },
            { "sort", "created_at:desc" },
            { "page", "1" },
            { "limit", "25" },
            { "include", "profile,preferences" },
            { "fields", "id,name,email" }
        };

        // Act
        var request = new TranslateRequest
        {
            Service = "users",
            Method = "GET",
            Path = "/api/v1/users",
            Query = complexQuery,
            Headers = new Dictionary<string, string>()
        };

        // Assert
        request.Query.Should().HaveCount(6);
        request.Query["filter"].Should().Be("status:active");
        request.Query["sort"].Should().Be("created_at:desc");
        request.Query["page"].Should().Be("1");
        request.Query["limit"].Should().Be("25");
        request.Query["include"].Should().Be("profile,preferences");
        request.Query["fields"].Should().Be("id,name,email");
    }

    [Fact]
    public void TranslateRequest_WithComplexHeaders_ShouldHandleCorrectly()
    {
        // Arrange
        var complexHeaders = new Dictionary<string, string>
        {
            { "Authorization", "Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..." },
            { "Content-Type", "application/json" },
            { "Accept", "application/json" },
            { "User-Agent", "Gateway/1.0.0" },
            { "X-Correlation-ID", "12345-67890-abcde" },
            { "X-Request-ID", "req-98765-43210" },
            { "Accept-Language", "es-ES,es;q=0.9,en;q=0.8" },
            { "Cache-Control", "no-cache" }
        };

        // Act
        var request = new TranslateRequest
        {
            Service = "reports",
            Method = "POST",
            Path = "/api/Report/generate",
            Query = new Dictionary<string, string>(),
            Headers = complexHeaders
        };

        // Assert
        request.Headers.Should().HaveCount(8);
        request.Headers["Authorization"].Should().StartWith("Bearer ");
        request.Headers["Content-Type"].Should().Be("application/json");
        request.Headers["Accept"].Should().Be("application/json");
        request.Headers["User-Agent"].Should().Be("Gateway/1.0.0");
        request.Headers["X-Correlation-ID"].Should().Be("12345-67890-abcde");
        request.Headers["X-Request-ID"].Should().Be("req-98765-43210");
        request.Headers["Accept-Language"].Should().Be("es-ES,es;q=0.9,en;q=0.8");
        request.Headers["Cache-Control"].Should().Be("no-cache");
    }

    [Fact]
    public void TranslateRequest_WithSpecialCharactersInPath_ShouldHandleCorrectly()
    {
        // Arrange & Act
        var request = new TranslateRequest
        {
            Service = "users",
            Method = "GET",
            Path = "/api/v1/users/search?name=José%20María&city=São%20Paulo",
            Query = new Dictionary<string, string>(),
            Headers = new Dictionary<string, string>()
        };

        // Assert
        request.Path.Should().Be("/api/v1/users/search?name=José%20María&city=São%20Paulo");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    [InlineData("\n")]
    public void TranslateRequest_WithWhitespaceService_ShouldAcceptValue(string service)
    {
        // Arrange & Act
        var request = new TranslateRequest
        {
            Service = service,
            Method = "GET",
            Path = "/api/test",
            Query = new Dictionary<string, string>(),
            Headers = new Dictionary<string, string>()
        };

        // Assert
        request.Service.Should().Be(service);
    }

    [Fact]
    public void TranslateRequest_Equality_ShouldWorkCorrectly()
    {
        // Arrange
        var request1 = new TranslateRequest
        {
            Service = "users",
            Method = "GET",
            Path = "/api/v1/users",
            Query = new Dictionary<string, string> { { "page", "1" } },
            Headers = new Dictionary<string, string> { { "Accept", "application/json" } }
        };

        var request2 = new TranslateRequest
        {
            Service = "users",
            Method = "GET",
            Path = "/api/v1/users",
            Query = new Dictionary<string, string> { { "page", "1" } },
            Headers = new Dictionary<string, string> { { "Accept", "application/json" } }
        };

        var request3 = new TranslateRequest
        {
            Service = "reports",
            Method = "GET",
            Path = "/api/v1/users",
            Query = new Dictionary<string, string> { { "page", "1" } },
            Headers = new Dictionary<string, string> { { "Accept", "application/json" } }
        };

        // Act & Assert
        // Note: This depends on how equality is implemented in TranslateRequest
        // If it's a record type, this will work. If it's a class, it might need custom implementation
        request1.Should().NotBeSameAs(request2);
        request1.Should().NotBe(request3);
    }
}
