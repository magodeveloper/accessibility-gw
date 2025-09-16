using Xunit;
using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Gateway.Services;
using Gateway.Models;

namespace Gateway.UnitTests.Services;

public class CacheService_AdditionalTests
{
    private readonly IDistributedCache _mockCache;
    private readonly ILogger<CacheService> _mockLogger;
    private readonly CacheService _cacheService;

    public CacheService_AdditionalTests()
    {
        _mockCache = Substitute.For<IDistributedCache>();
        _mockLogger = Substitute.For<ILogger<CacheService>>();
        _cacheService = new CacheService(_mockCache, _mockLogger);
    }

    [Fact]
    public async Task RemoveAsync_ShouldCallCacheRemove()
    {
        // Arrange
        const string key = "test-key";

        // Act
        await _cacheService.RemoveAsync(key);

        // Assert
        await _mockCache.Received(1).RemoveAsync(key, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task InvalidateServiceCacheAsync_ShouldCompleteSuccessfully()
    {
        // Arrange
        const string service = "test-service";

        // Act
        var act = async () => await _cacheService.InvalidateServiceCacheAsync(service);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Theory]
    [InlineData("authorization")]
    [InlineData("Authorization")]
    [InlineData("AUTHORIZATION")]
    [InlineData("x-api-key")]
    [InlineData("X-API-KEY")]
    [InlineData("cookie")]
    [InlineData("Cookie")]
    [InlineData("COOKIE")]
    [InlineData("x-auth-token")]
    [InlineData("X-AUTH-TOKEN")]
    public void GenerateCacheKey_WithSensitiveHeaders_ShouldExcludeThem(string sensitiveHeader)
    {
        // Arrange
        var request1 = new TranslateRequest
        {
            Service = "test-service",
            Method = "GET",
            Path = "/test",
            Headers = new Dictionary<string, string>
            {
                [sensitiveHeader] = "sensitive-value",
                ["Content-Type"] = "application/json"
            }
        };

        var request2 = new TranslateRequest
        {
            Service = "test-service",
            Method = "GET",
            Path = "/test",
            Headers = new Dictionary<string, string>
            {
                ["Content-Type"] = "application/json"
            }
        };

        // Act
        var cacheKey1 = _cacheService.GenerateCacheKey(request1);
        var cacheKey2 = _cacheService.GenerateCacheKey(request2);

        // Assert
        cacheKey1.Should().Be(cacheKey2, "sensitive headers should be excluded from cache key generation");
    }

    [Fact]
    public void GenerateCacheKey_WithEmptyHeaders_ShouldReturnDeterministicKey()
    {
        // Arrange
        var request = new TranslateRequest
        {
            Service = "test-service",
            Method = "GET",
            Path = "/test",
            Headers = new Dictionary<string, string>()
        };

        // Act
        var cacheKey1 = _cacheService.GenerateCacheKey(request);
        var cacheKey2 = _cacheService.GenerateCacheKey(request);

        // Assert
        cacheKey1.Should().Be(cacheKey2, "cache key generation should be deterministic");
        cacheKey1.Should().NotBeEmpty();
    }

    [Fact]
    public void GenerateCacheKey_WithDifferentHeaderOrder_ShouldReturnSameKey()
    {
        // Arrange
        var request1 = new TranslateRequest
        {
            Service = "test-service",
            Method = "GET",
            Path = "/test",
            Headers = new Dictionary<string, string>
            {
                ["Content-Type"] = "application/json",
                ["Accept"] = "application/json"
            }
        };

        var request2 = new TranslateRequest
        {
            Service = "test-service",
            Method = "GET",
            Path = "/test",
            Headers = new Dictionary<string, string>
            {
                ["Accept"] = "application/json",
                ["Content-Type"] = "application/json"
            }
        };

        // Act
        var cacheKey1 = _cacheService.GenerateCacheKey(request1);
        var cacheKey2 = _cacheService.GenerateCacheKey(request2);

        // Assert
        cacheKey1.Should().Be(cacheKey2, "cache key should be independent of header order");
    }

    [Fact]
    public void GenerateCacheKey_WithNullHeaders_ShouldReturnDeterministicKey()
    {
        // Arrange
        var request = new TranslateRequest
        {
            Service = "test-service",
            Method = "GET",
            Path = "/test",
            Headers = null
        };

        // Act
        var cacheKey1 = _cacheService.GenerateCacheKey(request);
        var cacheKey2 = _cacheService.GenerateCacheKey(request);

        // Assert
        cacheKey1.Should().Be(cacheKey2, "cache key generation should be deterministic even with null headers");
        cacheKey1.Should().NotBeEmpty();
    }

    [Fact]
    public void GenerateCacheKey_WithDifferentServices_ShouldReturnDifferentKeys()
    {
        // Arrange
        var request1 = new TranslateRequest
        {
            Service = "users",
            Method = "GET",
            Path = "/test"
        };

        var request2 = new TranslateRequest
        {
            Service = "reports",
            Method = "GET",
            Path = "/test"
        };

        // Act
        var cacheKey1 = _cacheService.GenerateCacheKey(request1);
        var cacheKey2 = _cacheService.GenerateCacheKey(request2);

        // Assert
        cacheKey1.Should().NotBe(cacheKey2, "different services should generate different cache keys");
    }

    [Fact]
    public void GenerateCacheKey_WithDifferentMethods_ShouldReturnDifferentKeys()
    {
        // Arrange
        var request1 = new TranslateRequest
        {
            Service = "test-service",
            Method = "GET",
            Path = "/test"
        };

        var request2 = new TranslateRequest
        {
            Service = "test-service",
            Method = "POST",
            Path = "/test"
        };

        // Act
        var cacheKey1 = _cacheService.GenerateCacheKey(request1);
        var cacheKey2 = _cacheService.GenerateCacheKey(request2);

        // Assert
        cacheKey1.Should().NotBe(cacheKey2, "different methods should generate different cache keys");
    }

    [Fact]
    public void GenerateCacheKey_WithDifferentPaths_ShouldReturnDifferentKeys()
    {
        // Arrange
        var request1 = new TranslateRequest
        {
            Service = "test-service",
            Method = "GET",
            Path = "/api/users"
        };

        var request2 = new TranslateRequest
        {
            Service = "test-service",
            Method = "GET",
            Path = "/api/reports"
        };

        // Act
        var cacheKey1 = _cacheService.GenerateCacheKey(request1);
        var cacheKey2 = _cacheService.GenerateCacheKey(request2);

        // Assert
        cacheKey1.Should().NotBe(cacheKey2, "different paths should generate different cache keys");
    }

    [Fact]
    public void GenerateCacheKey_WithQueryParameters_ShouldIncludeThemInKey()
    {
        // Arrange
        var request1 = new TranslateRequest
        {
            Service = "test-service",
            Method = "GET",
            Path = "/test",
            Query = new Dictionary<string, string>
            {
                ["page"] = "1",
                ["size"] = "10"
            }
        };

        var request2 = new TranslateRequest
        {
            Service = "test-service",
            Method = "GET",
            Path = "/test",
            Query = new Dictionary<string, string>
            {
                ["page"] = "2",
                ["size"] = "10"
            }
        };

        // Act
        var cacheKey1 = _cacheService.GenerateCacheKey(request1);
        var cacheKey2 = _cacheService.GenerateCacheKey(request2);

        // Assert
        cacheKey1.Should().NotBe(cacheKey2, "different query parameters should generate different cache keys");
    }

    [Fact]
    public void GenerateCacheKey_WithDifferentQueryOrder_ShouldReturnSameKey()
    {
        // Arrange
        var request1 = new TranslateRequest
        {
            Service = "test-service",
            Method = "GET",
            Path = "/test",
            Query = new Dictionary<string, string>
            {
                ["page"] = "1",
                ["size"] = "10"
            }
        };

        var request2 = new TranslateRequest
        {
            Service = "test-service",
            Method = "GET",
            Path = "/test",
            Query = new Dictionary<string, string>
            {
                ["size"] = "10",
                ["page"] = "1"
            }
        };

        // Act
        var cacheKey1 = _cacheService.GenerateCacheKey(request1);
        var cacheKey2 = _cacheService.GenerateCacheKey(request2);

        // Assert
        cacheKey1.Should().Be(cacheKey2, "query parameter order should not affect cache key");
    }

    [Fact]
    public void GenerateCacheKey_ShouldReturn16CharacterKey()
    {
        // Arrange
        var request = new TranslateRequest
        {
            Service = "test-service",
            Method = "GET",
            Path = "/test"
        };

        // Act
        var cacheKey = _cacheService.GenerateCacheKey(request);

        // Assert
        cacheKey.Should().HaveLength(16, "cache key should be truncated to 16 characters");
    }
}
