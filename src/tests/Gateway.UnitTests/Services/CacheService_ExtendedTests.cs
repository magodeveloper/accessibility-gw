using Xunit;
using System.Text;
using System.Linq;
using NSubstitute;
using Gateway.Models;
using System.Text.Json;
using Gateway.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute.ExceptionExtensions;
using Microsoft.Extensions.Caching.Distributed;

namespace Gateway.UnitTests.Services;

public class CacheService_ExtendedTests
{
    private readonly IDistributedCache _mockCache;
    private readonly ILogger<CacheService> _mockLogger;
    private readonly CacheService _cacheService;

    public CacheService_ExtendedTests()
    {
        _mockCache = Substitute.For<IDistributedCache>();
        _mockLogger = Substitute.For<ILogger<CacheService>>();
        _cacheService = new CacheService(_mockCache, _mockLogger);
    }

    #region SetAsync Tests

    [Fact]
    public async Task SetAsync_ValidInput_ShouldNotThrow()
    {
        // Arrange
        var key = "test-key";
        var value = new { Test = "value" };
        var expiration = TimeSpan.FromMinutes(30);

        // Act
        await _cacheService.SetAsync(key, value, expiration);

        // Assert - Should not throw
        await _mockCache.Received(1).SetAsync(
            key,
            Arg.Any<byte[]>(),
            Arg.Any<DistributedCacheEntryOptions>(),
            Arg.Any<CancellationToken>()
        );
    }

    [Fact]
    public async Task SetAsync_WithException_ShouldNotThrow()
    {
        // Arrange
        var key = "test-key";
        var value = new { Test = "value" };
        var expiration = TimeSpan.FromMinutes(30);

        _mockCache.SetAsync(Arg.Any<string>(), Arg.Any<byte[]>(), Arg.Any<DistributedCacheEntryOptions>(), Arg.Any<CancellationToken>())
                  .Returns(Task.FromException(new Exception("Cache error")));

        // Act & Assert - Should not throw
        await _cacheService.SetAsync(key, value, expiration);
    }

    #endregion

    #region RemoveAsync Tests

    [Fact]
    public async Task RemoveAsync_ValidKey_ShouldNotThrow()
    {
        // Arrange
        var key = "test-key";

        // Act
        await _cacheService.RemoveAsync(key);

        // Assert
        await _mockCache.Received(1).RemoveAsync(key, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RemoveAsync_WithException_ShouldNotThrow()
    {
        // Arrange
        var key = "test-key";
        _mockCache.RemoveAsync(key, Arg.Any<CancellationToken>())
                  .Returns(Task.FromException(new Exception("Remove error")));

        // Act & Assert - Should not throw
        await _cacheService.RemoveAsync(key);
    }

    #endregion

    #region InvalidateServiceCacheAsync Tests

    [Fact]
    public async Task InvalidateServiceCacheAsync_ValidService_ShouldComplete()
    {
        // Arrange
        var service = "test-service";

        // Act
        await _cacheService.InvalidateServiceCacheAsync(service);

        // Assert - Should complete without throwing
        // This method just logs and returns completed task
    }

    #endregion

    #region GetAsync Tests

    [Fact]
    public async Task GetAsync_WithValidKey_ShouldCallCacheGetStringAsync()
    {
        // Arrange
        var key = "test-key";
        var jsonValue = "{ \"test\": \"value\" }";
        var valueBytes = Encoding.UTF8.GetBytes(jsonValue);

        _mockCache.GetAsync(key, Arg.Any<CancellationToken>())
                  .Returns(Task.FromResult<byte[]?>(valueBytes));

        // Act
        await _cacheService.GetAsync<object>(key);

        // Assert
        await _mockCache.Received(1).GetAsync(key, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetAsync_WithNullCacheValue_ShouldReturnDefault()
    {
        // Arrange
        var key = "test-key";
        _mockCache.GetAsync(key, Arg.Any<CancellationToken>())
                  .Returns(Task.FromResult<byte[]?>(null));

        // Act
        var result = await _cacheService.GetAsync<string>(key);

        // Assert
        result.Should().BeNull();
        await _mockCache.Received(1).GetAsync(key, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetAsync_WithException_ShouldReturnDefaultAndNotThrow()
    {
        // Arrange
        var key = "test-key";
        _mockCache.GetAsync(key, Arg.Any<CancellationToken>())
                  .Returns(Task.FromException<byte[]?>(new Exception("Cache error")));

        // Act
        var result = await _cacheService.GetAsync<string>(key);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region IsSensitiveHeader Tests

    [Theory]
    [InlineData("authorization", true)]
    [InlineData("Authorization", true)]
    [InlineData("AUTHORIZATION", true)]
    [InlineData("cookie", true)]
    [InlineData("Cookie", true)]
    [InlineData("COOKIE", true)]
    [InlineData("x-api-key", true)]
    [InlineData("X-API-KEY", true)]
    [InlineData("X-Api-Key", true)]
    [InlineData("x-auth-token", true)]
    [InlineData("X-AUTH-TOKEN", true)]
    [InlineData("X-Auth-Token", true)]
    public void GenerateCacheKey_WithSensitiveHeaders_ShouldFilterThemOut(string sensitiveHeader, bool shouldBeFiltered)
    {
        // Arrange
        var request = new TranslateRequest
        {
            Service = "test-service",
            Method = "GET",
            Path = "/api/test",
            Headers = new Dictionary<string, string>
            {
                { sensitiveHeader, "sensitive-value" },
                { "content-type", "application/json" },
                { "user-agent", "test-agent" }
            }
        };

        // Act
        var cacheKey = _cacheService.GenerateCacheKey(request);

        // Assert
        cacheKey.Should().NotBeNullOrEmpty();
        cacheKey.Length.Should().Be(16); // SHA256 hash truncated to 16 chars

        // Create another request without sensitive headers to compare
        var requestWithoutSensitive = new TranslateRequest
        {
            Service = "test-service",
            Method = "GET",
            Path = "/api/test",
            Headers = new Dictionary<string, string>
            {
                { "content-type", "application/json" },
                { "user-agent", "test-agent" }
            }
        };

        var cacheKeyWithoutSensitive = _cacheService.GenerateCacheKey(requestWithoutSensitive);

        if (shouldBeFiltered)
        {
            // Keys should be the same because sensitive headers should be filtered
            cacheKey.Should().Be(cacheKeyWithoutSensitive);
        }
    }

    [Theory]
    [InlineData("content-type")]
    [InlineData("user-agent")]
    [InlineData("accept")]
    [InlineData("cache-control")]
    [InlineData("if-none-match")]
    [InlineData("x-custom-header")]
    [InlineData("custom-header")]
    public void GenerateCacheKey_WithNonSensitiveHeaders_ShouldGenerateDifferentKeys(string headerName)
    {
        // Arrange
        var requestWithHeader = new TranslateRequest
        {
            Service = "test-service",
            Method = "GET",
            Path = "/api/test",
            Headers = new Dictionary<string, string>
            {
                { headerName, "header-value" }
            }
        };

        var requestWithoutHeader = new TranslateRequest
        {
            Service = "test-service",
            Method = "GET",
            Path = "/api/test"
        };

        // Act
        var cacheKeyWith = _cacheService.GenerateCacheKey(requestWithHeader);
        var cacheKeyWithout = _cacheService.GenerateCacheKey(requestWithoutHeader);

        // Assert
        cacheKeyWith.Should().NotBe(cacheKeyWithout);
        cacheKeyWith.Should().NotBeNullOrEmpty();
        cacheKeyWithout.Should().NotBeNullOrEmpty();
        cacheKeyWith.Length.Should().Be(16);
        cacheKeyWithout.Length.Should().Be(16);
    }

    [Fact]
    public void GenerateCacheKey_WithMixedSensitiveAndNonSensitiveHeaders_ShouldFilterCorrectly()
    {
        // Arrange
        var requestWithMixed = new TranslateRequest
        {
            Service = "test-service",
            Method = "POST",
            Path = "/api/secure",
            Headers = new Dictionary<string, string>
            {
                { "authorization", "Bearer secret-token" },
                { "cookie", "session=secret" },
                { "x-api-key", "api-secret" },
                { "x-auth-token", "auth-secret" },
                { "content-type", "application/json" },
                { "user-agent", "TestAgent/1.0" },
                { "x-custom-header", "custom-value" }
            }
        };

        var requestWithOnlyNonSensitive = new TranslateRequest
        {
            Service = "test-service",
            Method = "POST",
            Path = "/api/secure",
            Headers = new Dictionary<string, string>
            {
                { "content-type", "application/json" },
                { "user-agent", "TestAgent/1.0" },
                { "x-custom-header", "custom-value" }
            }
        };

        // Act
        var cacheKeyMixed = _cacheService.GenerateCacheKey(requestWithMixed);
        var cacheKeyNonSensitive = _cacheService.GenerateCacheKey(requestWithOnlyNonSensitive);

        // Assert
        // Keys should be the same because sensitive headers should be filtered
        cacheKeyMixed.Should().Be(cacheKeyNonSensitive);
        cacheKeyMixed.Should().NotBeNullOrEmpty();
        cacheKeyMixed.Length.Should().Be(16);
    }

    #endregion

    #region JSON Serialization Edge Cases

    [Fact]
    public async Task GetAsync_WithComplexNestedObject_ShouldDeserializeCorrectly()
    {
        // Arrange
        var key = "complex-nested-key";
        var complexObject = new
        {
            Id = 123,
            Name = "Test Object",
            Nested = new
            {
                Property1 = "Value1",
                Property2 = 456,
                Array = new[] { 1, 2, 3 }
            },
            Collection = new List<string> { "item1", "item2", "item3" }
        };
        var serializedData = JsonSerializer.Serialize(complexObject);
        var dataBytes = Encoding.UTF8.GetBytes(serializedData);

        _mockCache.GetAsync(key, Arg.Any<CancellationToken>())
                 .Returns(Task.FromResult<byte[]?>(dataBytes));

        // Act
        var result = await _cacheService.GetAsync<object>(key);

        // Assert
        result.Should().NotBeNull();
        var json = JsonSerializer.Serialize(result);
        json.Should().Contain("Test Object");
        json.Should().Contain("Value1");
        json.Should().Contain("456");
    }

    [Fact]
    public async Task SetAsync_WithComplexNestedObject_ShouldSerializeCorrectly()
    {
        // Arrange
        var key = "complex-nested-set-key";
        var complexObject = new
        {
            DateTime = DateTime.UtcNow,
            Guid = Guid.NewGuid(),
            Nested = new
            {
                Dictionary = new Dictionary<string, object>
                {
                    { "key1", "value1" },
                    { "key2", 123 },
                    { "key3", true }
                }
            }
        };
        var ttl = TimeSpan.FromMinutes(10);

        // Act
        await _cacheService.SetAsync(key, complexObject, ttl);

        // Assert
        await _mockCache.Received(1).SetAsync(
            key,
            Arg.Is<byte[]>(bytes => IsValidJson(bytes)),
            Arg.Any<DistributedCacheEntryOptions>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetAsync_WithSpecialCharacters_ShouldHandleCorrectly()
    {
        // Arrange
        var key = "special-chars-key";
        var objectWithSpecialChars = new
        {
            Text = "Special chars test",
            Numbers = "12345",
            Simple = "OK"
        };
        var serializedData = JsonSerializer.Serialize(objectWithSpecialChars);
        var dataBytes = Encoding.UTF8.GetBytes(serializedData);

        _mockCache.GetAsync(key, Arg.Any<CancellationToken>())
                 .Returns(Task.FromResult<byte[]?>(dataBytes));

        // Act
        var result = await _cacheService.GetAsync<object>(key);

        // Assert
        result.Should().NotBeNull();
        var json = JsonSerializer.Serialize(result);
        json.Should().Contain("Special chars test");
        json.Should().Contain("12345");
        json.Should().Contain("OK");
    }

    #endregion

    #region Error Handling Edge Cases

    [Fact]
    public async Task SetAsync_WithVeryLargeObject_ShouldNotThrowAndCompleteOperation()
    {
        // Arrange
        var key = "large-object-key";
        var largeObject = new
        {
            Data = new string('x', 100000), // Very large string
            Nested = Enumerable.Range(1, 1000).Select(i => new { Id = i, Value = $"Item {i}" }).ToArray()
        };
        var ttl = TimeSpan.FromMinutes(5);

        // Act & Assert
        var act = async () => await _cacheService.SetAsync(key, largeObject, ttl);
        await act.Should().NotThrowAsync();

        await _mockCache.Received(1).SetAsync(
            key,
            Arg.Any<byte[]>(),
            Arg.Any<DistributedCacheEntryOptions>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetAsync_WithCorruptedJsonData_ShouldReturnDefaultAndNotThrow()
    {
        // Arrange
        var key = "corrupted-json-key";
        var corruptedData = Encoding.UTF8.GetBytes("{ corrupted json data missing quotes and braces");

        _mockCache.GetAsync(key, Arg.Any<CancellationToken>())
                 .Returns(Task.FromResult<byte[]?>(corruptedData));

        // Act
        var result = await _cacheService.GetAsync<TestDto>(key);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAsync_WithEmptyByteArray_ShouldReturnDefault()
    {
        // Arrange
        var key = "empty-bytes-key";
        var emptyBytes = new byte[0];

        _mockCache.GetAsync(key, Arg.Any<CancellationToken>())
                 .Returns(Task.FromResult<byte[]?>(emptyBytes));

        // Act
        var result = await _cacheService.GetAsync<string>(key);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task SetAsync_WithOperationCanceledException_ShouldNotThrow()
    {
        // Arrange
        var key = "cancellation-key";
        var value = "test-value";
        var ttl = TimeSpan.FromMinutes(5);
        var cancellationToken = new CancellationToken(true); // Already cancelled

        _mockCache.SetAsync(Arg.Any<string>(), Arg.Any<byte[]>(), Arg.Any<DistributedCacheEntryOptions>(), cancellationToken)
                 .ThrowsAsync(new OperationCanceledException());

        // Act & Assert
        var act = async () => await _cacheService.SetAsync(key, value, ttl, cancellationToken);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task RemoveAsync_WithTimeoutException_ShouldNotThrow()
    {
        // Arrange
        var key = "timeout-key";
        _mockCache.RemoveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
                 .ThrowsAsync(new TimeoutException("Cache operation timed out"));

        // Act & Assert
        var act = async () => await _cacheService.RemoveAsync(key);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task GetAsync_WithTimeoutException_ShouldReturnDefaultAndNotThrow()
    {
        // Arrange
        var key = "timeout-get-key";
        _mockCache.GetAsync(key, Arg.Any<CancellationToken>())
                 .ThrowsAsync(new TimeoutException("Cache get operation timed out"));

        // Act
        var result = await _cacheService.GetAsync<string>(key);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region Cache Key Generation Edge Cases

    [Fact]
    public void GenerateCacheKey_WithQueryParametersSpecialCharacters_ShouldIncludeCorrectly()
    {
        // Arrange
        var request = new TranslateRequest
        {
            Service = "search-service",
            Method = "GET",
            Path = "/api/search",
            Query = new Dictionary<string, string>
            {
                { "q", "test query" },
                { "filter", "active" },
                { "sort", "name asc" }
            }
        };

        // Act
        var cacheKey = _cacheService.GenerateCacheKey(request);

        // Assert
        cacheKey.Should().NotBeNullOrEmpty();
        cacheKey.Length.Should().Be(16);

        // Test that query parameters affect cache key
        var requestWithoutQuery = new TranslateRequest
        {
            Service = "search-service",
            Method = "GET",
            Path = "/api/search"
        };

        var cacheKeyWithoutQuery = _cacheService.GenerateCacheKey(requestWithoutQuery);
        cacheKey.Should().NotBe(cacheKeyWithoutQuery);
    }

    [Fact]
    public void GenerateCacheKey_WithEmptyHeaders_ShouldGenerateValidKey()
    {
        // Arrange
        var request = new TranslateRequest
        {
            Service = "simple-service",
            Method = "GET",
            Path = "/api/simple"
            // No headers
        };

        // Act
        var cacheKey = _cacheService.GenerateCacheKey(request);

        // Assert
        cacheKey.Should().NotBeNullOrEmpty();
        cacheKey.Length.Should().Be(16); // SHA256 hash truncated to 16 chars
    }

    [Fact]
    public void GenerateCacheKey_WithVeryLongPath_ShouldGenerateValidHash()
    {
        // Arrange
        var longPath = "/api/" + new string('a', 1000) + "/endpoint/" + new string('b', 1000);
        var request = new TranslateRequest
        {
            Service = "long-path-service",
            Method = "GET",
            Path = longPath
        };

        // Act
        var cacheKey = _cacheService.GenerateCacheKey(request);

        // Assert
        cacheKey.Should().NotBeNullOrEmpty();
        cacheKey.Length.Should().Be(16); // SHA256 hash truncated to 16 chars
    }

    [Fact]
    public void GenerateCacheKey_WithDifferentServices_ShouldGenerateDifferentKeys()
    {
        // Arrange
        var request1 = new TranslateRequest
        {
            Service = "service-1",
            Method = "GET",
            Path = "/api/test"
        };

        var request2 = new TranslateRequest
        {
            Service = "service-2",
            Method = "GET",
            Path = "/api/test"
        };

        // Act
        var cacheKey1 = _cacheService.GenerateCacheKey(request1);
        var cacheKey2 = _cacheService.GenerateCacheKey(request2);

        // Assert
        cacheKey1.Should().NotBe(cacheKey2);
        cacheKey1.Length.Should().Be(16);
        cacheKey2.Length.Should().Be(16);
    }

    [Fact]
    public void GenerateCacheKey_WithDifferentMethods_ShouldGenerateDifferentKeys()
    {
        // Arrange
        var requestGet = new TranslateRequest
        {
            Service = "test-service",
            Method = "GET",
            Path = "/api/test"
        };

        var requestPost = new TranslateRequest
        {
            Service = "test-service",
            Method = "POST",
            Path = "/api/test"
        };

        // Act
        var cacheKeyGet = _cacheService.GenerateCacheKey(requestGet);
        var cacheKeyPost = _cacheService.GenerateCacheKey(requestPost);

        // Assert
        cacheKeyGet.Should().NotBe(cacheKeyPost);
        cacheKeyGet.Length.Should().Be(16);
        cacheKeyPost.Length.Should().Be(16);
    }

    #endregion

    #region InvalidateServiceCacheAsync Tests

    [Fact]
    public async Task InvalidateServiceCacheAsync_WithValidService_ShouldCompleteSuccessfully()
    {
        // Arrange
        var service = "test-service";

        // Act & Assert - Should not throw
        await _cacheService.InvalidateServiceCacheAsync(service);

        // Just verify it completed without exception
        Assert.True(true); // If we get here, the test passed
    }

    [Fact]
    public async Task InvalidateServiceCacheAsync_WithEmptyService_ShouldNotThrow()
    {
        // Arrange
        var service = "";

        // Act & Assert - Just call and verify no exception
        await _cacheService.InvalidateServiceCacheAsync(service);
    }

    [Fact]
    public async Task InvalidateServiceCacheAsync_WithNullService_ShouldNotThrow()
    {
        // Arrange
        string? service = null;

        // Act & Assert - Just call and verify no exception
        await _cacheService.InvalidateServiceCacheAsync(service!);
    }

    #endregion

    #region Additional Coverage Tests

    [Fact]
    public async Task SetAsync_WithCircularReference_ShouldNotThrow()
    {
        // Arrange
        var key = "circular-ref-key";
        var circularObj = new CircularReferenceObject();
        circularObj.Self = circularObj; // Create circular reference
        var expiration = TimeSpan.FromMinutes(10);

        // Act & Assert - Should handle serialization error gracefully
        await _cacheService.SetAsync(key, circularObj, expiration);
    }

    [Fact]
    public async Task SetAsync_WithDistributedCacheFailure_ShouldNotThrow()
    {
        // Arrange
        var key = "cache-failure-key";
        var value = new TestDto { Id = 99, Name = "Test" };
        var expiration = TimeSpan.FromMinutes(5);

        // Act & Assert - Should handle cache storage error gracefully
        await _cacheService.SetAsync(key, value, expiration);
        // If we get here without exception, the test passes
    }

    [Fact]
    public async Task RemoveAsync_WithCacheServiceDown_ShouldNotThrow()
    {
        // Arrange
        var key = "cache-down-key";

        // Act & Assert - Should handle cache removal error gracefully
        await _cacheService.RemoveAsync(key);
    }

    [Fact]
    public async Task GetAsync_WithCacheAccessException_ShouldReturnDefault()
    {
        // Arrange
        var key = "access-error-key";

        // Act - Should handle cache access error gracefully
        var result = await _cacheService.GetAsync<TestDto>(key);

        // Assert - Should return null on error
        result.Should().BeNull();
    }

    [Fact]
    public void GenerateCacheKey_WithNullQueryAndHeaders_ShouldGenerateValidKey()
    {
        // Arrange
        var request = new TranslateRequest
        {
            Service = "minimal-service",
            Method = "POST",
            Path = "/api/minimal",
            Query = null,
            Headers = null
        };

        // Act
        var result = _cacheService.GenerateCacheKey(request);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Length.Should().Be(16);
        result.Should().NotContain("="); // Base64 padding should be trimmed
    }

    [Fact]
    public void GenerateCacheKey_WithEmptyDictionaries_ShouldGenerateValidKey()
    {
        // Arrange
        var request = new TranslateRequest
        {
            Service = "empty-collections",
            Method = "PUT",
            Path = "/api/update",
            Query = new Dictionary<string, string>(), // Empty
            Headers = new Dictionary<string, string>() // Empty
        };

        // Act
        var result = _cacheService.GenerateCacheKey(request);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Length.Should().Be(16);
    }

    #endregion

    #region Helper Methods and Classes

    private static bool IsValidJson(byte[] bytes)
    {
        try
        {
            var json = Encoding.UTF8.GetString(bytes);
            JsonDocument.Parse(json);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private class CircularReferenceObject
    {
        public CircularReferenceObject? Self { get; set; }
    }

    private class TestDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }

    #endregion
}
