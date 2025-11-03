using Moq;
using Xunit;
using FluentAssertions;
using Gateway.Middleware;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace Gateway.UnitTests.Middleware
{
    /// <summary>
    /// Tests para JwtClaimsTransformMiddleware - Transformación de claims JWT a headers HTTP
    /// Target: >80% coverage
    /// </summary>
    public class JwtClaimsTransformMiddlewareTests
    {
        private readonly Mock<ILogger<JwtClaimsTransformMiddleware>> _loggerMock;
        private readonly Mock<RequestDelegate> _nextMock;
        private readonly DefaultHttpContext _context;

        public JwtClaimsTransformMiddlewareTests()
        {
            _loggerMock = new Mock<ILogger<JwtClaimsTransformMiddleware>>();
            _nextMock = new Mock<RequestDelegate>();
            _context = new DefaultHttpContext();
        }

        #region Gateway Secret Tests

        [Fact]
        public async Task InvokeAsync_WithGatewaySecretConfigured_ShouldAddSecretHeader()
        {
            // Arrange
            var configuration = CreateConfiguration(gatewaySecret: "test-secret-123");
            var middleware = new JwtClaimsTransformMiddleware(_nextMock.Object, _loggerMock.Object, configuration);

            // Act
            await middleware.InvokeAsync(_context);

            // Assert
            _context.Request.Headers["X-Gateway-Secret"].ToString().Should().Be("test-secret-123");
            _nextMock.Verify(next => next(_context), Times.Once);
        }

        [Fact]
        public async Task InvokeAsync_WithoutGatewaySecret_ShouldNotAddSecretHeader()
        {
            // Arrange
            var configuration = CreateConfiguration(gatewaySecret: null);
            var middleware = new JwtClaimsTransformMiddleware(_nextMock.Object, _loggerMock.Object, configuration);

            // Act
            await middleware.InvokeAsync(_context);

            // Assert
            _context.Request.Headers.ContainsKey("X-Gateway-Secret").Should().BeFalse();
            _nextMock.Verify(next => next(_context), Times.Once);
        }

        [Fact]
        public async Task InvokeAsync_WithEmptyGatewaySecret_ShouldNotAddSecretHeader()
        {
            // Arrange
            var configuration = CreateConfiguration(gatewaySecret: "");
            var middleware = new JwtClaimsTransformMiddleware(_nextMock.Object, _loggerMock.Object, configuration);

            // Act
            await middleware.InvokeAsync(_context);

            // Assert
            _context.Request.Headers.ContainsKey("X-Gateway-Secret").Should().BeFalse();
        }

        [Fact]
        public async Task InvokeAsync_WithGatewaySecretFromEnvironmentVariable_ShouldAddSecretHeader()
        {
            // Arrange
            var configuration = CreateConfiguration(gatewaySecretEnvVar: "env-secret-456");
            var middleware = new JwtClaimsTransformMiddleware(_nextMock.Object, _loggerMock.Object, configuration);

            // Act
            await middleware.InvokeAsync(_context);

            // Assert
            _context.Request.Headers["X-Gateway-Secret"].ToString().Should().Be("env-secret-456");
        }

        #endregion

        #region Unauthenticated Request Tests

        [Fact]
        public async Task InvokeAsync_WithUnauthenticatedUser_ShouldNotAddUserHeaders()
        {
            // Arrange
            var configuration = CreateConfiguration();
            var middleware = new JwtClaimsTransformMiddleware(_nextMock.Object, _loggerMock.Object, configuration);
            _context.User = new ClaimsPrincipal(); // No identity

            // Act
            await middleware.InvokeAsync(_context);

            // Assert
            _context.Request.Headers.ContainsKey("X-User-Id").Should().BeFalse();
            _context.Request.Headers.ContainsKey("X-User-Email").Should().BeFalse();
            _context.Request.Headers.ContainsKey("X-User-Role").Should().BeFalse();
            _context.Request.Headers.ContainsKey("X-User-Name").Should().BeFalse();
            _nextMock.Verify(next => next(_context), Times.Once);
        }

        [Fact]
        public async Task InvokeAsync_WithNullUser_ShouldNotThrow()
        {
            // Arrange
            var configuration = CreateConfiguration();
            var middleware = new JwtClaimsTransformMiddleware(_nextMock.Object, _loggerMock.Object, configuration);
            _context.User = null!;

            // Act
            Func<Task> act = async () => await middleware.InvokeAsync(_context);

            // Assert
            await act.Should().NotThrowAsync();
            _nextMock.Verify(next => next(_context), Times.Once);
        }

        #endregion

        #region Authenticated Request Tests - Standard Claims

        [Fact]
        public async Task InvokeAsync_WithAuthenticatedUser_ShouldAddAllUserHeaders()
        {
            // Arrange
            var configuration = CreateConfiguration();
            var middleware = new JwtClaimsTransformMiddleware(_nextMock.Object, _loggerMock.Object, configuration);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "user-123"),
                new Claim(ClaimTypes.Email, "user@example.com"),
                new Claim(ClaimTypes.Role, "Admin"),
                new Claim(ClaimTypes.Name, "John Doe")
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            _context.User = new ClaimsPrincipal(identity);

            // Act
            await middleware.InvokeAsync(_context);

            // Assert
            _context.Request.Headers["X-User-Id"].ToString().Should().Be("user-123");
            _context.Request.Headers["X-User-Email"].ToString().Should().Be("user@example.com");
            _context.Request.Headers["X-User-Role"].ToString().Should().Be("Admin");
            _context.Request.Headers["X-User-Name"].ToString().Should().Be("John Doe");
            _nextMock.Verify(next => next(_context), Times.Once);
        }

        [Fact]
        public async Task InvokeAsync_WithOnlyUserId_ShouldAddOnlyUserIdHeader()
        {
            // Arrange
            var configuration = CreateConfiguration();
            var middleware = new JwtClaimsTransformMiddleware(_nextMock.Object, _loggerMock.Object, configuration);

            var claims = new[] { new Claim(ClaimTypes.NameIdentifier, "user-456") };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            _context.User = new ClaimsPrincipal(identity);

            // Act
            await middleware.InvokeAsync(_context);

            // Assert
            _context.Request.Headers["X-User-Id"].ToString().Should().Be("user-456");
            _context.Request.Headers.ContainsKey("X-User-Email").Should().BeFalse();
            _context.Request.Headers.ContainsKey("X-User-Role").Should().BeFalse();
            _context.Request.Headers.ContainsKey("X-User-Name").Should().BeFalse();
        }

        #endregion

        #region Authenticated Request Tests - Alternative Claim Types

        [Theory]
        [InlineData("sub")] // JWT standard
        [InlineData("userId")] // Alternative
        public async Task InvokeAsync_WithAlternativeUserIdClaim_ShouldAddUserIdHeader(string claimType)
        {
            // Arrange
            var configuration = CreateConfiguration();
            var middleware = new JwtClaimsTransformMiddleware(_nextMock.Object, _loggerMock.Object, configuration);

            var claims = new[] { new Claim(claimType, "user-789") };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            _context.User = new ClaimsPrincipal(identity);

            // Act
            await middleware.InvokeAsync(_context);

            // Assert
            _context.Request.Headers["X-User-Id"].ToString().Should().Be("user-789");
        }

        [Theory]
        [InlineData("email")]
        [InlineData(ClaimTypes.Email)]
        public async Task InvokeAsync_WithDifferentEmailClaimTypes_ShouldAddEmailHeader(string claimType)
        {
            // Arrange
            var configuration = CreateConfiguration();
            var middleware = new JwtClaimsTransformMiddleware(_nextMock.Object, _loggerMock.Object, configuration);

            var claims = new[] { new Claim(claimType, "test@example.com") };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            _context.User = new ClaimsPrincipal(identity);

            // Act
            await middleware.InvokeAsync(_context);

            // Assert
            _context.Request.Headers["X-User-Email"].ToString().Should().Be("test@example.com");
        }

        [Theory]
        [InlineData("role")]
        [InlineData(ClaimTypes.Role)]
        public async Task InvokeAsync_WithDifferentRoleClaimTypes_ShouldAddRoleHeader(string claimType)
        {
            // Arrange
            var configuration = CreateConfiguration();
            var middleware = new JwtClaimsTransformMiddleware(_nextMock.Object, _loggerMock.Object, configuration);

            var claims = new[] { new Claim(claimType, "User") };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            _context.User = new ClaimsPrincipal(identity);

            // Act
            await middleware.InvokeAsync(_context);

            // Assert
            _context.Request.Headers["X-User-Role"].ToString().Should().Be("User");
        }

        [Theory]
        [InlineData("name")]
        [InlineData("userName")]
        [InlineData(ClaimTypes.Name)]
        public async Task InvokeAsync_WithDifferentNameClaimTypes_ShouldAddNameHeader(string claimType)
        {
            // Arrange
            var configuration = CreateConfiguration();
            var middleware = new JwtClaimsTransformMiddleware(_nextMock.Object, _loggerMock.Object, configuration);

            var claims = new[] { new Claim(claimType, "Jane Smith") };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            _context.User = new ClaimsPrincipal(identity);

            // Act
            await middleware.InvokeAsync(_context);

            // Assert
            _context.Request.Headers["X-User-Name"].ToString().Should().Be("Jane Smith");
        }

        #endregion

        #region Edge Cases - Empty and Whitespace Claims

        [Fact]
        public async Task InvokeAsync_WithEmptyClaimValues_ShouldNotAddHeaders()
        {
            // Arrange
            var configuration = CreateConfiguration();
            var middleware = new JwtClaimsTransformMiddleware(_nextMock.Object, _loggerMock.Object, configuration);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, ""),
                new Claim(ClaimTypes.Email, ""),
                new Claim(ClaimTypes.Role, ""),
                new Claim(ClaimTypes.Name, "")
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            _context.User = new ClaimsPrincipal(identity);

            // Act
            await middleware.InvokeAsync(_context);

            // Assert
            _context.Request.Headers.ContainsKey("X-User-Id").Should().BeFalse();
            _context.Request.Headers.ContainsKey("X-User-Email").Should().BeFalse();
            _context.Request.Headers.ContainsKey("X-User-Role").Should().BeFalse();
            _context.Request.Headers.ContainsKey("X-User-Name").Should().BeFalse();
        }

        [Fact]
        public async Task InvokeAsync_WithWhitespaceClaimValues_ShouldNotAddHeaders()
        {
            // Arrange
            var configuration = CreateConfiguration();
            var middleware = new JwtClaimsTransformMiddleware(_nextMock.Object, _loggerMock.Object, configuration);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "   "),
                new Claim(ClaimTypes.Email, "  "),
                new Claim(ClaimTypes.Role, " "),
                new Claim(ClaimTypes.Name, "    ")
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            _context.User = new ClaimsPrincipal(identity);

            // Act
            await middleware.InvokeAsync(_context);

            // Assert
            _context.Request.Headers.ContainsKey("X-User-Id").Should().BeFalse();
            _context.Request.Headers.ContainsKey("X-User-Email").Should().BeFalse();
            _context.Request.Headers.ContainsKey("X-User-Role").Should().BeFalse();
            _context.Request.Headers.ContainsKey("X-User-Name").Should().BeFalse();
        }

        #endregion

        #region Multiple Claims Priority Tests

        [Fact]
        public async Task InvokeAsync_WithMultipleUserIdClaims_ShouldUseFirstNonEmpty()
        {
            // Arrange
            var configuration = CreateConfiguration();
            var middleware = new JwtClaimsTransformMiddleware(_nextMock.Object, _loggerMock.Object, configuration);

            // Priority: NameIdentifier, sub, userId
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "id-from-nameidentifier"),
                new Claim("sub", "id-from-sub"),
                new Claim("userId", "id-from-userId")
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            _context.User = new ClaimsPrincipal(identity);

            // Act
            await middleware.InvokeAsync(_context);

            // Assert
            _context.Request.Headers["X-User-Id"].ToString().Should().Be("id-from-nameidentifier");
        }

        [Fact]
        public async Task InvokeAsync_WithOnlySecondaryUserIdClaim_ShouldUseIt()
        {
            // Arrange
            var configuration = CreateConfiguration();
            var middleware = new JwtClaimsTransformMiddleware(_nextMock.Object, _loggerMock.Object, configuration);

            var claims = new[] { new Claim("sub", "id-from-sub") };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            _context.User = new ClaimsPrincipal(identity);

            // Act
            await middleware.InvokeAsync(_context);

            // Assert
            _context.Request.Headers["X-User-Id"].ToString().Should().Be("id-from-sub");
        }

        #endregion

        #region Exception Handling Tests

        [Fact]
        public async Task InvokeAsync_WhenClaimExtractionThrows_ShouldLogErrorAndContinue()
        {
            // Arrange
            var configuration = CreateConfiguration();
            var middleware = new JwtClaimsTransformMiddleware(_nextMock.Object, _loggerMock.Object, configuration);

            // Create a mock identity that throws when accessing claims
            var mockIdentity = new Mock<ClaimsIdentity>();
            mockIdentity.Setup(i => i.IsAuthenticated).Returns(true);
            mockIdentity.Setup(i => i.FindFirst(It.IsAny<string>())).Throws<InvalidOperationException>();

            _context.User = new ClaimsPrincipal(mockIdentity.Object);

            // Act
            Func<Task> act = async () => await middleware.InvokeAsync(_context);

            // Assert
            await act.Should().NotThrowAsync();
            _nextMock.Verify(next => next(_context), Times.Once);
        }

        #endregion

        #region Combined Scenarios Tests

        [Fact]
        public async Task InvokeAsync_WithGatewaySecretAndAuthenticatedUser_ShouldAddAllHeaders()
        {
            // Arrange
            var configuration = CreateConfiguration(gatewaySecret: "secret-789");
            var middleware = new JwtClaimsTransformMiddleware(_nextMock.Object, _loggerMock.Object, configuration);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "user-999"),
                new Claim(ClaimTypes.Email, "combined@example.com"),
                new Claim(ClaimTypes.Role, "SuperAdmin"),
                new Claim(ClaimTypes.Name, "Combined User")
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            _context.User = new ClaimsPrincipal(identity);

            // Act
            await middleware.InvokeAsync(_context);

            // Assert
            _context.Request.Headers["X-Gateway-Secret"].ToString().Should().Be("secret-789");
            _context.Request.Headers["X-User-Id"].ToString().Should().Be("user-999");
            _context.Request.Headers["X-User-Email"].ToString().Should().Be("combined@example.com");
            _context.Request.Headers["X-User-Role"].ToString().Should().Be("SuperAdmin");
            _context.Request.Headers["X-User-Name"].ToString().Should().Be("Combined User");
        }

        [Fact]
        public async Task InvokeAsync_WithGatewaySecretAndUnauthenticatedUser_ShouldAddOnlySecret()
        {
            // Arrange
            var configuration = CreateConfiguration(gatewaySecret: "secret-only");
            var middleware = new JwtClaimsTransformMiddleware(_nextMock.Object, _loggerMock.Object, configuration);
            _context.User = new ClaimsPrincipal(); // Not authenticated

            // Act
            await middleware.InvokeAsync(_context);

            // Assert
            _context.Request.Headers["X-Gateway-Secret"].ToString().Should().Be("secret-only");
            _context.Request.Headers.ContainsKey("X-User-Id").Should().BeFalse();
            _context.Request.Headers.ContainsKey("X-User-Email").Should().BeFalse();
            _context.Request.Headers.ContainsKey("X-User-Role").Should().BeFalse();
            _context.Request.Headers.ContainsKey("X-User-Name").Should().BeFalse();
        }

        #endregion

        #region Real-World Scenarios

        [Fact]
        public async Task InvokeAsync_SimulateAdminRequest_ShouldProcessCorrectly()
        {
            // Arrange
            var configuration = CreateConfiguration(gatewaySecret: "prod-secret");
            var middleware = new JwtClaimsTransformMiddleware(_nextMock.Object, _loggerMock.Object, configuration);

            var claims = new[]
            {
                new Claim("sub", "admin-001"),
                new Claim("email", "admin@company.com"),
                new Claim("role", "Administrator"),
                new Claim("name", "System Admin")
            };
            var identity = new ClaimsIdentity(claims, "Bearer");
            _context.User = new ClaimsPrincipal(identity);

            // Act
            await middleware.InvokeAsync(_context);

            // Assert
            _context.Request.Headers["X-Gateway-Secret"].Should().NotBeNullOrEmpty();
            _context.Request.Headers["X-User-Id"].ToString().Should().Be("admin-001");
            _context.Request.Headers["X-User-Email"].ToString().Should().Be("admin@company.com");
            _context.Request.Headers["X-User-Role"].ToString().Should().Be("Administrator");
            _context.Request.Headers["X-User-Name"].ToString().Should().Be("System Admin");
        }

        [Fact]
        public async Task InvokeAsync_SimulateRegularUserRequest_ShouldProcessCorrectly()
        {
            // Arrange
            var configuration = CreateConfiguration(gatewaySecret: "prod-secret");
            var middleware = new JwtClaimsTransformMiddleware(_nextMock.Object, _loggerMock.Object, configuration);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "user-12345"),
                new Claim(ClaimTypes.Email, "user@example.com"),
                new Claim(ClaimTypes.Role, "User")
            };
            var identity = new ClaimsIdentity(claims, "Bearer");
            _context.User = new ClaimsPrincipal(identity);

            // Act
            await middleware.InvokeAsync(_context);

            // Assert
            _context.Request.Headers["X-User-Id"].ToString().Should().Be("user-12345");
            _context.Request.Headers["X-User-Email"].ToString().Should().Be("user@example.com");
            _context.Request.Headers["X-User-Role"].ToString().Should().Be("User");
        }

        [Fact]
        public async Task InvokeAsync_SimulatePublicEndpoint_ShouldOnlyAddGatewaySecret()
        {
            // Arrange
            var configuration = CreateConfiguration(gatewaySecret: "gateway-key");
            var middleware = new JwtClaimsTransformMiddleware(_nextMock.Object, _loggerMock.Object, configuration);
            // No user authentication for public endpoint

            // Act
            await middleware.InvokeAsync(_context);

            // Assert
            _context.Request.Headers["X-Gateway-Secret"].ToString().Should().Be("gateway-key");
            _context.Request.Headers.ContainsKey("X-User-Id").Should().BeFalse();
        }

        #endregion

        #region Middleware Pipeline Tests

        [Fact]
        public async Task InvokeAsync_ShouldAlwaysCallNextMiddleware()
        {
            // Arrange
            var configuration = CreateConfiguration();
            var middleware = new JwtClaimsTransformMiddleware(_nextMock.Object, _loggerMock.Object, configuration);

            // Act
            await middleware.InvokeAsync(_context);

            // Assert
            _nextMock.Verify(next => next(_context), Times.Once);
        }

        [Fact]
        public async Task InvokeAsync_MultipleSequentialCalls_ShouldProcessEachIndependently()
        {
            // Arrange
            var configuration = CreateConfiguration(gatewaySecret: "test-secret");
            var middleware = new JwtClaimsTransformMiddleware(_nextMock.Object, _loggerMock.Object, configuration);

            // Act - First call with authenticated user
            var context1 = new DefaultHttpContext();
            var claims1 = new[] { new Claim("sub", "user-1") };
            context1.User = new ClaimsPrincipal(new ClaimsIdentity(claims1, "Auth"));
            await middleware.InvokeAsync(context1);

            // Act - Second call with different user
            var context2 = new DefaultHttpContext();
            var claims2 = new[] { new Claim("sub", "user-2") };
            context2.User = new ClaimsPrincipal(new ClaimsIdentity(claims2, "Auth"));
            await middleware.InvokeAsync(context2);

            // Assert
            context1.Request.Headers["X-User-Id"].ToString().Should().Be("user-1");
            context2.Request.Headers["X-User-Id"].ToString().Should().Be("user-2");
            _nextMock.Verify(next => next(It.IsAny<HttpContext>()), Times.Exactly(2));
        }

        #endregion

        #region Helper Methods

        private static IConfiguration CreateConfiguration(
            string? gatewaySecret = null,
            string? gatewaySecretEnvVar = null)
        {
            var configData = new Dictionary<string, string?>();

            if (gatewaySecret != null)
            {
                configData["Gateway:Secret"] = gatewaySecret;
            }

            if (gatewaySecretEnvVar != null)
            {
                configData["GATEWAY_SECRET"] = gatewaySecretEnvVar;
            }

            return new ConfigurationBuilder()
                .AddInMemoryCollection(configData)
                .Build();
        }

        #endregion
    }
}
