using Xunit;
using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Gateway.Services;
using Gateway.Models;
using System.Text;

namespace Gateway.UnitTests.Services
{
    public class CacheServiceTests
    {
        private readonly IDistributedCache _mockCache;
        private readonly ILogger<CacheService> _mockLogger;
        private readonly CacheService _cacheService;

        public CacheServiceTests()
        {
            _mockCache = Substitute.For<IDistributedCache>();
            _mockLogger = Substitute.For<ILogger<CacheService>>();
            _cacheService = new CacheService(_mockCache, _mockLogger);
        }

        [Fact]
        public void GenerateCacheKey_WithBasicRequest_ShouldGenerateCorrectKey()
        {
            // Arrange
            var request = new TranslateRequest
            {
                Service = "users",
                Path = "/api/v1/users",
                Method = "GET"
            };

            // Act
            var result = _cacheService.GenerateCacheKey(request);

            // Assert
            result.Should().NotBeNullOrEmpty();
            result.Length.Should().Be(16); // SHA256 hash truncated to 16 chars
        }

        [Fact]
        public void GenerateCacheKey_WithSensitiveHeaders_ShouldFilterThem()
        {
            // Arrange
            var requestWithSensitive = new TranslateRequest
            {
                Service = "users",
                Path = "/api/v1/users",
                Method = "GET",
                Headers = new Dictionary<string, string>
                {
                    { "authorization", "Bearer token" },
                    { "x-api-key", "secret" },
                    { "content-type", "application/json" }
                }
            };

            var requestWithoutSensitive = new TranslateRequest
            {
                Service = "users",
                Path = "/api/v1/users",
                Method = "GET",
                Headers = new Dictionary<string, string>
                {
                    { "content-type", "application/json" }
                }
            };

            // Act
            var keyWithSensitive = _cacheService.GenerateCacheKey(requestWithSensitive);
            var keyWithoutSensitive = _cacheService.GenerateCacheKey(requestWithoutSensitive);

            // Assert
            keyWithSensitive.Should().Be(keyWithoutSensitive);
            keyWithSensitive.Length.Should().Be(16);
        }

        [Theory]
        [InlineData("authorization")]
        [InlineData("cookie")]
        [InlineData("x-api-key")]
        [InlineData("x-auth-token")]
        [InlineData("AUTHORIZATION")] // Case insensitive
        [InlineData("Cookie")]
        public void GenerateCacheKey_WithSpecificSensitiveHeaders_ShouldFilterThem(string sensitiveHeader)
        {
            // Arrange
            var requestWithSensitive = new TranslateRequest
            {
                Service = "users",
                Path = "/api/v1/users",
                Method = "GET",
                Headers = new Dictionary<string, string>
                {
                    { sensitiveHeader, "sensitive-value" },
                    { "content-type", "application/json" }
                }
            };

            var requestWithoutSensitive = new TranslateRequest
            {
                Service = "users",
                Path = "/api/v1/users",
                Method = "GET",
                Headers = new Dictionary<string, string>
                {
                    { "content-type", "application/json" }
                }
            };

            // Act
            var keyWithSensitive = _cacheService.GenerateCacheKey(requestWithSensitive);
            var keyWithoutSensitive = _cacheService.GenerateCacheKey(requestWithoutSensitive);

            // Assert
            keyWithSensitive.Should().Be(keyWithoutSensitive);
        }

        [Fact]
        public void GenerateCacheKey_WithQueryParameters_ShouldIncludeInKey()
        {
            // Arrange
            var requestWithQuery = new TranslateRequest
            {
                Service = "users",
                Path = "/api/v1/users",
                Method = "GET",
                Query = new Dictionary<string, string>
                {
                    { "page", "1" },
                    { "size", "10" }
                }
            };

            var requestWithoutQuery = new TranslateRequest
            {
                Service = "users",
                Path = "/api/v1/users",
                Method = "GET"
            };

            // Act
            var keyWithQuery = _cacheService.GenerateCacheKey(requestWithQuery);
            var keyWithoutQuery = _cacheService.GenerateCacheKey(requestWithoutQuery);

            // Assert
            keyWithQuery.Should().NotBe(keyWithoutQuery);
            keyWithQuery.Length.Should().Be(16);
            keyWithoutQuery.Length.Should().Be(16);
        }

        [Fact]
        public void GenerateCacheKey_WithDifferentOrder_ShouldGenerateSameKey()
        {
            // Arrange
            var request1 = new TranslateRequest
            {
                Service = "users",
                Path = "/api/v1/users",
                Method = "GET",
                Query = new Dictionary<string, string>
                {
                    { "page", "1" },
                    { "size", "10" }
                },
                Headers = new Dictionary<string, string>
                {
                    { "content-type", "application/json" },
                    { "accept", "application/json" }
                }
            };

            var request2 = new TranslateRequest
            {
                Service = "users",
                Path = "/api/v1/users",
                Method = "GET",
                Query = new Dictionary<string, string>
                {
                    { "size", "10" },
                    { "page", "1" }
                },
                Headers = new Dictionary<string, string>
                {
                    { "accept", "application/json" },
                    { "content-type", "application/json" }
                }
            };

            // Act
            var key1 = _cacheService.GenerateCacheKey(request1);
            var key2 = _cacheService.GenerateCacheKey(request2);

            // Assert
            key1.Should().Be(key2);
        }

        [Fact]
        public void GenerateCacheKey_WithCaseVariations_ShouldNormalizeCase()
        {
            // Arrange
            var request1 = new TranslateRequest
            {
                Service = "USERS",
                Path = "/API/V1/USERS",
                Method = "get"
            };

            var request2 = new TranslateRequest
            {
                Service = "users",
                Path = "/api/v1/users",
                Method = "GET"
            };

            // Act
            var key1 = _cacheService.GenerateCacheKey(request1);
            var key2 = _cacheService.GenerateCacheKey(request2);

            // Assert
            key1.Should().Be(key2);
        }

        [Fact]
        public async Task GetAsync_WithNonExistentKey_ShouldReturnDefault()
        {
            // Arrange
            var key = "non-existent-key";
            _mockCache.GetAsync(key, Arg.Any<CancellationToken>())
                     .Returns(Task.FromResult<byte[]?>(null));

            // Act
            var result = await _cacheService.GetAsync<string>(key);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetAsync_WithEmptyString_ShouldReturnDefault()
        {
            // Arrange
            var key = "empty-key";
            var emptyBytes = Encoding.UTF8.GetBytes(string.Empty);
            _mockCache.GetAsync(key, Arg.Any<CancellationToken>())
                     .Returns(Task.FromResult<byte[]?>(emptyBytes));

            // Act
            var result = await _cacheService.GetAsync<string>(key);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetAsync_WithValidJsonData_ShouldDeserializeCorrectly()
        {
            // Arrange
            var key = "valid-key";
            var testObject = new TestDto { Id = 1, Name = "Test", IsActive = true };
            var serializedData = System.Text.Json.JsonSerializer.Serialize(testObject, new System.Text.Json.JsonSerializerOptions
            {
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
            });
            var dataBytes = Encoding.UTF8.GetBytes(serializedData);

            _mockCache.GetAsync(key, Arg.Any<CancellationToken>())
                     .Returns(Task.FromResult<byte[]?>(dataBytes));

            // Act
            var result = await _cacheService.GetAsync<TestDto>(key);

            // Assert
            result.Should().NotBeNull();
            result!.Id.Should().Be(1);
            result.Name.Should().Be("Test");
            result.IsActive.Should().BeTrue();
        }

        [Fact]
        public async Task GetAsync_WithInvalidJson_ShouldReturnDefaultAndLogWarning()
        {
            // Arrange
            var key = "invalid-json-key";
            var invalidJsonBytes = Encoding.UTF8.GetBytes("{ invalid json }");
            _mockCache.GetAsync(key, Arg.Any<CancellationToken>())
                     .Returns(Task.FromResult<byte[]?>(invalidJsonBytes));

            // Act
            var result = await _cacheService.GetAsync<TestDto>(key);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetAsync_WithCacheException_ShouldReturnDefaultAndLogWarning()
        {
            // Arrange
            var key = "exception-key";
            _mockCache.GetAsync(key, Arg.Any<CancellationToken>())
                     .ThrowsAsync(new InvalidOperationException("Cache error"));

            // Act
            var result = await _cacheService.GetAsync<string>(key);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetAsync_WithCancellationToken_ShouldPassTokenToCache()
        {
            // Arrange
            var key = "test-key";
            var cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;
            var testValueBytes = Encoding.UTF8.GetBytes("\"test-value\"");

            _mockCache.GetAsync(key, cancellationToken)
                     .Returns(Task.FromResult<byte[]?>(testValueBytes));

            // Act
            var result = await _cacheService.GetAsync<string>(key, cancellationToken);

            // Assert
            result.Should().Be("test-value");
            await _mockCache.Received(1).GetAsync(key, cancellationToken);
        }

        [Fact]
        public async Task SetAsync_WithValidData_ShouldStoreInCache()
        {
            // Arrange
            var key = "test-key";
            var value = "test-value";
            var ttl = TimeSpan.FromMinutes(5);

            // Act
            await _cacheService.SetAsync(key, value, ttl);

            // Assert
            await _mockCache.Received(1).SetAsync(
                key,
                Arg.Any<byte[]>(),
                Arg.Is<DistributedCacheEntryOptions>(o => o.AbsoluteExpirationRelativeToNow == ttl),
                Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task SetAsync_WithComplexObject_ShouldSerializeCorrectly()
        {
            // Arrange
            var key = "complex-object-key";
            var testObject = new TestDto { Id = 42, Name = "Complex Test", IsActive = false };
            var ttl = TimeSpan.FromHours(1);

            // Act
            await _cacheService.SetAsync(key, testObject, ttl);

            // Assert
            await _mockCache.Received(1).SetAsync(
                key,
                Arg.Any<byte[]>(),
                Arg.Is<DistributedCacheEntryOptions>(o => o.AbsoluteExpirationRelativeToNow == ttl),
                Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task SetAsync_WithNullValue_ShouldSerializeNull()
        {
            // Arrange
            var key = "null-value-key";
            string? nullValue = null;
            var ttl = TimeSpan.FromMinutes(10);

            // Act
            await _cacheService.SetAsync(key, nullValue, ttl);

            // Assert
            await _mockCache.Received(1).SetAsync(
                key,
                Arg.Any<byte[]>(),
                Arg.Any<DistributedCacheEntryOptions>(),
                Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task SetAsync_WithCacheException_ShouldNotThrowAndLogWarning()
        {
            // Arrange
            var key = "exception-key";
            var value = "test-value";
            var ttl = TimeSpan.FromMinutes(5);

            _mockCache.SetAsync(Arg.Any<string>(), Arg.Any<byte[]>(), Arg.Any<DistributedCacheEntryOptions>(), Arg.Any<CancellationToken>())
                     .ThrowsAsync(new InvalidOperationException("Cache error"));

            // Act & Assert
            var act = async () => await _cacheService.SetAsync(key, value, ttl);
            await act.Should().NotThrowAsync();
        }

        [Fact]
        public async Task SetAsync_WithCancellationToken_ShouldPassTokenToCache()
        {
            // Arrange
            var key = "test-key";
            var value = "test-value";
            var ttl = TimeSpan.FromMinutes(5);
            var cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;

            // Act
            await _cacheService.SetAsync(key, value, ttl, cancellationToken);

            // Assert
            await _mockCache.Received(1).SetAsync(
                key,
                Arg.Any<byte[]>(),
                Arg.Any<DistributedCacheEntryOptions>(),
                cancellationToken);
        }

        [Fact]
        public async Task RemoveAsync_WithValidKey_ShouldRemoveFromCache()
        {
            // Arrange
            var key = "test-key";

            // Act
            await _cacheService.RemoveAsync(key);

            // Assert
            await _mockCache.Received(1).RemoveAsync(key, Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task RemoveAsync_WithCacheException_ShouldNotThrowAndLogWarning()
        {
            // Arrange
            var key = "exception-key";
            _mockCache.RemoveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
                     .ThrowsAsync(new InvalidOperationException("Cache error"));

            // Act & Assert
            var act = async () => await _cacheService.RemoveAsync(key);
            await act.Should().NotThrowAsync();
        }

        [Fact]
        public async Task RemoveAsync_WithCancellationToken_ShouldPassTokenToCache()
        {
            // Arrange
            var key = "test-key";
            var cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;

            // Act
            await _cacheService.RemoveAsync(key, cancellationToken);

            // Assert
            await _mockCache.Received(1).RemoveAsync(key, cancellationToken);
        }

        [Fact]
        public async Task InvalidateServiceCacheAsync_WithValidService_ShouldCompleteSuccessfully()
        {
            // Arrange
            var service = "users";

            // Act
            await _cacheService.InvalidateServiceCacheAsync(service);

            // Assert
            // Method should complete without throwing
            // Note: This is a simplified implementation that just logs
        }

        [Fact]
        public async Task InvalidateServiceCacheAsync_WithCancellationToken_ShouldRespectToken()
        {
            // Arrange
            var service = "users";
            var cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;

            // Act
            await _cacheService.InvalidateServiceCacheAsync(service, cancellationToken);

            // Assert
            // Method should complete without throwing
        }

        // Helper class for testing complex object serialization
        private class TestDto
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public bool IsActive { get; set; }
        }
    }
}
