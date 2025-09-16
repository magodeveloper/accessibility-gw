using FluentAssertions;
using Gateway.Models;
using Gateway.UnitTests.Helpers;

namespace Gateway.UnitTests.Models;

/// <summary>
/// Tests unitarios para TranslateResponse
/// </summary>
public class TranslateResponseTests : UnitTestBase
{
    [Fact]
    public void TranslateResponse_WithValidData_ShouldInitializeCorrectly()
    {
        // Arrange & Act
        var response = new TranslateResponse
        {
            StatusCode = 200,
            Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } },
            Body = new { message = "success" },
            ProcessingTimeMs = 150,
            FromCache = false,
            ProcessedByService = "users"
        };

        // Assert
        response.StatusCode.Should().Be(200);
        response.Headers.Should().ContainKey("Content-Type").WhoseValue.Should().Be("application/json");
        response.Body.Should().BeEquivalentTo(new { message = "success" });
        response.ProcessingTimeMs.Should().Be(150);
        response.FromCache.Should().BeFalse();
        response.ProcessedByService.Should().Be("users");
    }

    [Fact]
    public void TranslateResponse_WithMinimalData_ShouldInitializeCorrectly()
    {
        // Arrange & Act
        var response = new TranslateResponse
        {
            StatusCode = 404
        };

        // Assert
        response.StatusCode.Should().Be(404);
        response.Headers.Should().BeNull();
        response.Body.Should().BeNull();
        response.ProcessingTimeMs.Should().Be(0);
        response.FromCache.Should().BeFalse(); // Default value
        response.ProcessedByService.Should().BeNull();
    }

    [Fact]
    public void TranslateResponse_WithCachedResponse_ShouldInitializeCorrectly()
    {
        // Arrange & Act
        var response = new TranslateResponse
        {
            StatusCode = 200,
            Headers = new Dictionary<string, string> { { "Cache-Control", "max-age=3600" } },
            Body = new { data = "cached-result" },
            ProcessingTimeMs = 5,
            FromCache = true,
            ProcessedByService = "reports"
        };

        // Assert
        response.StatusCode.Should().Be(200);
        response.Headers.Should().ContainKey("Cache-Control").WhoseValue.Should().Be("max-age=3600");
        response.Body.Should().BeEquivalentTo(new { data = "cached-result" });
        response.ProcessingTimeMs.Should().Be(5);
        response.FromCache.Should().BeTrue();
        response.ProcessedByService.Should().Be("reports");
    }

    [Fact]
    public void TranslateResponse_WithNullOptionalFields_ShouldHandleCorrectly()
    {
        // Arrange & Act
        var response = new TranslateResponse
        {
            StatusCode = 500,
            Headers = null,
            Body = null,
            ProcessingTimeMs = 2000,
            FromCache = false,
            ProcessedByService = null
        };

        // Assert
        response.StatusCode.Should().Be(500);
        response.Headers.Should().BeNull();
        response.Body.Should().BeNull();
        response.ProcessingTimeMs.Should().Be(2000);
        response.FromCache.Should().BeFalse();
        response.ProcessedByService.Should().BeNull();
    }

    [Fact]
    public void TranslateResponse_WithEmptyHeaders_ShouldHandleCorrectly()
    {
        // Arrange & Act
        var response = new TranslateResponse
        {
            StatusCode = 204,
            Headers = new Dictionary<string, string>(),
            ProcessingTimeMs = 75
        };

        // Assert
        response.StatusCode.Should().Be(204);
        response.Headers.Should().NotBeNull().And.BeEmpty();
        response.Body.Should().BeNull();
        response.ProcessingTimeMs.Should().Be(75);
        response.FromCache.Should().BeFalse();
        response.ProcessedByService.Should().BeNull();
    }

    [Theory]
    [InlineData(200)]
    [InlineData(201)]
    [InlineData(400)]
    [InlineData(404)]
    [InlineData(500)]
    public void TranslateResponse_WithVariousStatusCodes_ShouldAcceptValues(int statusCode)
    {
        // Arrange & Act
        var response = new TranslateResponse
        {
            StatusCode = statusCode
        };

        // Assert
        response.StatusCode.Should().Be(statusCode);
    }

    [Fact]
    public void TranslateResponse_RecordEquality_ShouldWorkCorrectly()
    {
        // Arrange
        var response1 = new TranslateResponse
        {
            StatusCode = 200,
            Headers = new Dictionary<string, string> { { "X-Test", "value" } },
            Body = new { id = 123 },
            ProcessingTimeMs = 100,
            FromCache = true,
            ProcessedByService = "analysis"
        };

        var response2 = new TranslateResponse
        {
            StatusCode = 200,
            Headers = new Dictionary<string, string> { { "X-Test", "value" } },
            Body = new { id = 123 },
            ProcessingTimeMs = 100,
            FromCache = true,
            ProcessedByService = "analysis"
        };

        var response3 = new TranslateResponse
        {
            StatusCode = 201,
            Headers = new Dictionary<string, string> { { "X-Test", "value" } },
            Body = new { id = 123 },
            ProcessingTimeMs = 100,
            FromCache = true,
            ProcessedByService = "analysis"
        };

        // Act & Assert
        response1.Should().NotBeSameAs(response2);
        response1.Should().NotBe(response3);
    }

    [Fact]
    public void TranslateResponse_WithComplexBody_ShouldHandleCorrectly()
    {
        // Arrange
        var complexBody = new
        {
            users = new[]
            {
                new { id = 1, name = "John Doe", email = "john@example.com" },
                new { id = 2, name = "Jane Smith", email = "jane@example.com" }
            },
            pagination = new { page = 1, total = 2, hasNext = false }
        };

        // Act
        var response = new TranslateResponse
        {
            StatusCode = 200,
            Headers = new Dictionary<string, string>
            {
                { "Content-Type", "application/json" },
                { "X-Total-Count", "2" }
            },
            Body = complexBody,
            ProcessingTimeMs = 350,
            FromCache = false,
            ProcessedByService = "users"
        };

        // Assert
        response.StatusCode.Should().Be(200);
        response.Headers.Should().HaveCount(2);
        response.Headers!["Content-Type"].Should().Be("application/json");
        response.Headers["X-Total-Count"].Should().Be("2");
        response.Body.Should().BeEquivalentTo(complexBody);
        response.ProcessingTimeMs.Should().Be(350);
        response.FromCache.Should().BeFalse();
        response.ProcessedByService.Should().Be("users");
    }

    [Fact]
    public void TranslateResponse_WithLargeProcessingTime_ShouldHandleCorrectly()
    {
        // Arrange & Act
        var response = new TranslateResponse
        {
            StatusCode = 200,
            ProcessingTimeMs = long.MaxValue
        };

        // Assert
        response.ProcessingTimeMs.Should().Be(long.MaxValue);
    }

    [Fact]
    public void TranslateResponse_DefaultValues_ShouldBeCorrect()
    {
        // Arrange & Act
        var response = new TranslateResponse
        {
            StatusCode = 200
        };

        // Assert
        response.FromCache.Should().BeFalse(); // Default value should be false
        response.ProcessingTimeMs.Should().Be(0); // Default value for long
    }
}
