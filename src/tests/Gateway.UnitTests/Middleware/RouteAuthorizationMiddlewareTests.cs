using Moq;
using Xunit;
using Gateway.Models;
using FluentAssertions;
using System.Text.Json;
using Gateway.Middleware;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Gateway.UnitTests.Middleware
{
    /// <summary>
    /// Tests para RouteAuthorizationMiddleware - Autorización basada en configuración de rutas
    /// Target: >80% coverage
    /// </summary>
    public class RouteAuthorizationMiddlewareTests
    {
        private readonly Mock<ILogger<RouteAuthorizationMiddleware>> _loggerMock;
        private readonly Mock<RequestDelegate> _nextMock;
        private readonly DefaultHttpContext _context;

        public RouteAuthorizationMiddlewareTests()
        {
            _loggerMock = new Mock<ILogger<RouteAuthorizationMiddleware>>();
            _nextMock = new Mock<RequestDelegate>();
            _context = new DefaultHttpContext();
            _context.Response.Body = new MemoryStream();
        }

        #region System Public Routes Tests

        [Theory]
        [InlineData("/health")]
        [InlineData("/health/live")]
        [InlineData("/health/ready")]
        [InlineData("/metrics")]
        [InlineData("/gateway/metrics")]
        [InlineData("/swagger")]
        [InlineData("/swagger/")]
        [InlineData("/swagger/index.html")]
        [InlineData("/swagger/users/swagger.json")]
        public async Task InvokeAsync_SystemPublicRoutes_ShouldAllowAccess(string path)
        {
            // Arrange
            var options = CreateOptions(new List<AllowedRoute>());
            var middleware = new RouteAuthorizationMiddleware(_nextMock.Object, _loggerMock.Object, options);
            _context.Request.Path = path;
            _context.Request.Method = "GET";

            // Act
            await middleware.InvokeAsync(_context);

            // Assert
            _nextMock.Verify(next => next(_context), Times.Once);
        }

        [Fact]
        public async Task InvokeAsync_HealthRoute_WithoutAuthentication_ShouldAllow()
        {
            // Arrange
            var options = CreateOptions(new List<AllowedRoute>());
            var middleware = new RouteAuthorizationMiddleware(_nextMock.Object, _loggerMock.Object, options);
            _context.Request.Path = "/health";
            _context.Request.Method = "GET";
            _context.User = new ClaimsPrincipal(); // No authentication

            // Act
            await middleware.InvokeAsync(_context);

            // Assert
            _nextMock.Verify(next => next(_context), Times.Once);
        }

        #endregion

        #region Public Routes (requiresAuth=false) Tests

        [Fact]
        public async Task InvokeAsync_PublicRoute_WithoutAuthentication_ShouldAllow()
        {
            // Arrange
            var routes = new List<AllowedRoute>
            {
                new AllowedRoute { Service = "test-service",
                    PathPrefix = "/api/public",
                    Methods = new[] { "GET", "POST" },
                    RequiresAuth = false
                }
            };
            var options = CreateOptions(routes);
            var middleware = new RouteAuthorizationMiddleware(_nextMock.Object, _loggerMock.Object, options);
            _context.Request.Path = "/api/public/data";
            _context.Request.Method = "GET";
            _context.User = new ClaimsPrincipal(); // No authentication

            // Act
            await middleware.InvokeAsync(_context);

            // Assert
            _nextMock.Verify(next => next(_context), Times.Once);
        }

        [Fact]
        public async Task InvokeAsync_PublicRoute_WithAuthentication_ShouldAlsoAllow()
        {
            // Arrange
            var routes = new List<AllowedRoute>
            {
                new AllowedRoute { Service = "test-service",
                    PathPrefix = "/api/public",
                    Methods = new[] { "GET" },
                    RequiresAuth = false
                }
            };
            var options = CreateOptions(routes);
            var middleware = new RouteAuthorizationMiddleware(_nextMock.Object, _loggerMock.Object, options);
            _context.Request.Path = "/api/public/info";
            _context.Request.Method = "GET";

            // Set authenticated user
            var claims = new[] { new Claim(ClaimTypes.NameIdentifier, "user-123") };
            var identity = new ClaimsIdentity(claims, "Bearer");
            _context.User = new ClaimsPrincipal(identity);

            // Act
            await middleware.InvokeAsync(_context);

            // Assert
            _nextMock.Verify(next => next(_context), Times.Once);
        }

        [Fact]
        public async Task InvokeAsync_PublicRoute_PostMethod_ShouldAllow()
        {
            // Arrange
            var routes = new List<AllowedRoute>
            {
                new AllowedRoute { Service = "test-service",
                    PathPrefix = "/api/register",
                    Methods = new[] { "POST" },
                    RequiresAuth = false
                }
            };
            var options = CreateOptions(routes);
            var middleware = new RouteAuthorizationMiddleware(_nextMock.Object, _loggerMock.Object, options);
            _context.Request.Path = "/api/register";
            _context.Request.Method = "POST";

            // Act
            await middleware.InvokeAsync(_context);

            // Assert
            _nextMock.Verify(next => next(_context), Times.Once);
        }

        #endregion

        #region Protected Routes (requiresAuth=true) Tests

        [Fact]
        public async Task InvokeAsync_ProtectedRoute_WithAuthentication_ShouldAllow()
        {
            // Arrange
            var routes = new List<AllowedRoute>
            {
                new AllowedRoute { Service = "test-service",
                    PathPrefix = "/api/users",
                    Methods = new[] { "GET", "POST" },
                    RequiresAuth = true
                }
            };
            var options = CreateOptions(routes);
            var middleware = new RouteAuthorizationMiddleware(_nextMock.Object, _loggerMock.Object, options);
            _context.Request.Path = "/api/users/profile";
            _context.Request.Method = "GET";

            // Set authenticated user
            var claims = new[] { new Claim(ClaimTypes.NameIdentifier, "user-456") };
            var identity = new ClaimsIdentity(claims, "Bearer");
            _context.User = new ClaimsPrincipal(identity);

            // Act
            await middleware.InvokeAsync(_context);

            // Assert
            _nextMock.Verify(next => next(_context), Times.Once);
        }

        [Fact]
        public async Task InvokeAsync_ProtectedRoute_WithoutAuthentication_ShouldReturn401()
        {
            // Arrange
            var routes = new List<AllowedRoute>
            {
                new AllowedRoute { Service = "test-service",
                    PathPrefix = "/api/admin",
                    Methods = new[] { "GET" },
                    RequiresAuth = true
                }
            };
            var options = CreateOptions(routes);
            var middleware = new RouteAuthorizationMiddleware(_nextMock.Object, _loggerMock.Object, options);
            _context.Request.Path = "/api/admin/dashboard";
            _context.Request.Method = "GET";
            _context.User = new ClaimsPrincipal(); // No authentication

            // Act
            await middleware.InvokeAsync(_context);

            // Assert
            _context.Response.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
            _nextMock.Verify(next => next(_context), Times.Never);

            // Verify response body
            _context.Response.Body.Seek(0, SeekOrigin.Begin);
            var responseBody = await new StreamReader(_context.Response.Body).ReadToEndAsync();
            responseBody.Should().Contain("Unauthorized");
            responseBody.Should().Contain("requires authentication");
        }

        [Fact]
        public async Task InvokeAsync_ProtectedRoute_WithNullUser_ShouldReturn401()
        {
            // Arrange
            var routes = new List<AllowedRoute>
            {
                new AllowedRoute { Service = "test-service",
                    PathPrefix = "/api/secure",
                    Methods = new[] { "POST" },
                    RequiresAuth = true
                }
            };
            var options = CreateOptions(routes);
            var middleware = new RouteAuthorizationMiddleware(_nextMock.Object, _loggerMock.Object, options);
            _context.Request.Path = "/api/secure/data";
            _context.Request.Method = "POST";
            _context.User = null!; // Null user

            // Act
            await middleware.InvokeAsync(_context);

            // Assert
            _context.Response.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
            _nextMock.Verify(next => next(_context), Times.Never);
        }

        [Fact]
        public async Task InvokeAsync_ProtectedRoute_UnauthenticatedIdentity_ShouldReturn401()
        {
            // Arrange
            var routes = new List<AllowedRoute>
            {
                new AllowedRoute { Service = "test-service",
                    PathPrefix = "/api/protected",
                    Methods = new[] { "GET" },
                    RequiresAuth = true
                }
            };
            var options = CreateOptions(routes);
            var middleware = new RouteAuthorizationMiddleware(_nextMock.Object, _loggerMock.Object, options);
            _context.Request.Path = "/api/protected/resource";
            _context.Request.Method = "GET";

            // User with unauthenticated identity
            var identity = new ClaimsIdentity(); // No authenticationType
            _context.User = new ClaimsPrincipal(identity);

            // Act
            await middleware.InvokeAsync(_context);

            // Assert
            _context.Response.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
        }

        #endregion

        #region Route Not Configured Tests

        [Fact]
        public async Task InvokeAsync_RouteNotConfigured_ShouldReturn403()
        {
            // Arrange
            var routes = new List<AllowedRoute>
            {
                new AllowedRoute { Service = "test-service",
                    PathPrefix = "/api/allowed",
                    Methods = new[] { "GET" },
                    RequiresAuth = false
                }
            };
            var options = CreateOptions(routes);
            var middleware = new RouteAuthorizationMiddleware(_nextMock.Object, _loggerMock.Object, options);
            _context.Request.Path = "/api/notconfigured";
            _context.Request.Method = "GET";

            // Act
            await middleware.InvokeAsync(_context);

            // Assert
            _context.Response.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
            _nextMock.Verify(next => next(_context), Times.Never);

            // Verify response body
            _context.Response.Body.Seek(0, SeekOrigin.Begin);
            var responseBody = await new StreamReader(_context.Response.Body).ReadToEndAsync();
            responseBody.Should().Contain("Forbidden");
            responseBody.Should().Contain("not configured");
        }

        [Fact]
        public async Task InvokeAsync_MethodNotAllowedForRoute_ShouldReturn403()
        {
            // Arrange
            var routes = new List<AllowedRoute>
            {
                new AllowedRoute { Service = "test-service",
                    PathPrefix = "/api/data",
                    Methods = new[] { "GET" }, // Only GET allowed
                    RequiresAuth = false
                }
            };
            var options = CreateOptions(routes);
            var middleware = new RouteAuthorizationMiddleware(_nextMock.Object, _loggerMock.Object, options);
            _context.Request.Path = "/api/data";
            _context.Request.Method = "POST"; // POST not in allowed methods

            // Act
            await middleware.InvokeAsync(_context);

            // Assert
            _context.Response.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
            _nextMock.Verify(next => next(_context), Times.Never);
        }

        [Fact]
        public async Task InvokeAsync_RouteNotConfigured_EvenWithAuthentication_ShouldReturn403()
        {
            // Arrange
            var routes = new List<AllowedRoute>
            {
                new AllowedRoute { Service = "test-service",
                    PathPrefix = "/api/configured",
                    Methods = new[] { "GET" },
                    RequiresAuth = true
                }
            };
            var options = CreateOptions(routes);
            var middleware = new RouteAuthorizationMiddleware(_nextMock.Object, _loggerMock.Object, options);
            _context.Request.Path = "/api/unknown";
            _context.Request.Method = "GET";

            // Even with authenticated user
            var claims = new[] { new Claim(ClaimTypes.NameIdentifier, "admin") };
            var identity = new ClaimsIdentity(claims, "Bearer");
            _context.User = new ClaimsPrincipal(identity);

            // Act
            await middleware.InvokeAsync(_context);

            // Assert
            _context.Response.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
        }

        #endregion

        #region Path Matching Tests

        [Fact]
        public async Task InvokeAsync_PathPrefix_ShouldMatchChildPaths()
        {
            // Arrange
            var routes = new List<AllowedRoute>
            {
                new AllowedRoute { Service = "test-service",
                    PathPrefix = "/api/users",
                    Methods = new[] { "GET" },
                    RequiresAuth = false
                }
            };
            var options = CreateOptions(routes);
            var middleware = new RouteAuthorizationMiddleware(_nextMock.Object, _loggerMock.Object, options);
            _context.Request.Path = "/api/users/123/profile";
            _context.Request.Method = "GET";

            // Act
            await middleware.InvokeAsync(_context);

            // Assert
            _nextMock.Verify(next => next(_context), Times.Once);
        }

        [Fact]
        public async Task InvokeAsync_PathMatching_ShouldBeCaseInsensitive()
        {
            // Arrange
            var routes = new List<AllowedRoute>
            {
                new AllowedRoute { Service = "test-service",
                    PathPrefix = "/api/data",
                    Methods = new[] { "GET" },
                    RequiresAuth = false
                }
            };
            var options = CreateOptions(routes);
            var middleware = new RouteAuthorizationMiddleware(_nextMock.Object, _loggerMock.Object, options);
            _context.Request.Path = "/API/DATA/item";
            _context.Request.Method = "GET";

            // Act
            await middleware.InvokeAsync(_context);

            // Assert
            _nextMock.Verify(next => next(_context), Times.Once);
        }

        [Fact]
        public async Task InvokeAsync_MethodMatching_ShouldBeCaseInsensitive()
        {
            // Arrange
            var routes = new List<AllowedRoute>
            {
                new AllowedRoute { Service = "test-service",
                    PathPrefix = "/api/test",
                    Methods = new[] { "post", "get" },
                    RequiresAuth = false
                }
            };
            var options = CreateOptions(routes);
            var middleware = new RouteAuthorizationMiddleware(_nextMock.Object, _loggerMock.Object, options);
            _context.Request.Path = "/api/test";
            _context.Request.Method = "POST"; // Uppercase

            // Act
            await middleware.InvokeAsync(_context);

            // Assert
            _nextMock.Verify(next => next(_context), Times.Once);
        }

        #endregion

        #region Multiple Routes Tests

        [Fact]
        public async Task InvokeAsync_MultipleRoutes_ShouldMatchFirstMatchingRoute()
        {
            // Arrange
            var routes = new List<AllowedRoute>
            {
                new AllowedRoute { Service = "test-service",
                    PathPrefix = "/api",
                    Methods = new[] { "GET" },
                    RequiresAuth = true
                },
                new AllowedRoute { Service = "test-service",
                    PathPrefix = "/api/public",
                    Methods = new[] { "GET" },
                    RequiresAuth = false
                }
            };
            var options = CreateOptions(routes);
            var middleware = new RouteAuthorizationMiddleware(_nextMock.Object, _loggerMock.Object, options);
            _context.Request.Path = "/api/data";
            _context.Request.Method = "GET";
            _context.User = new ClaimsPrincipal(); // No auth

            // Act
            await middleware.InvokeAsync(_context);

            // Assert - Should match first route which requires auth
            _context.Response.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
        }

        [Fact]
        public async Task InvokeAsync_DifferentMethodsForSameRoute_ShouldEvaluateIndependently()
        {
            // Arrange
            var routes = new List<AllowedRoute>
            {
                new AllowedRoute { Service = "test-service",
                    PathPrefix = "/api/resource",
                    Methods = new[] { "GET" },
                    RequiresAuth = false
                },
                new AllowedRoute { Service = "test-service",
                    PathPrefix = "/api/resource",
                    Methods = new[] { "POST", "PUT", "DELETE" },
                    RequiresAuth = true
                }
            };
            var options = CreateOptions(routes);
            var middleware = new RouteAuthorizationMiddleware(_nextMock.Object, _loggerMock.Object, options);

            // Test GET (public)
            _context.Request.Path = "/api/resource";
            _context.Request.Method = "GET";
            _context.User = new ClaimsPrincipal();

            // Act
            await middleware.InvokeAsync(_context);

            // Assert
            _nextMock.Verify(next => next(_context), Times.Once);
        }

        #endregion

        #region Edge Cases Tests

        [Fact]
        public async Task InvokeAsync_EmptyPath_ShouldReturn403()
        {
            // Arrange
            var routes = new List<AllowedRoute>
            {
                new AllowedRoute { Service = "test-service",
                    PathPrefix = "/api",
                    Methods = new[] { "GET" },
                    RequiresAuth = false
                }
            };
            var options = CreateOptions(routes);
            var middleware = new RouteAuthorizationMiddleware(_nextMock.Object, _loggerMock.Object, options);
            _context.Request.Path = "";
            _context.Request.Method = "GET";

            // Act
            await middleware.InvokeAsync(_context);

            // Assert
            _context.Response.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
        }

        [Fact]
        public async Task InvokeAsync_RootPath_WithoutConfiguration_ShouldReturn403()
        {
            // Arrange
            var routes = new List<AllowedRoute>();
            var options = CreateOptions(routes);
            var middleware = new RouteAuthorizationMiddleware(_nextMock.Object, _loggerMock.Object, options);
            _context.Request.Path = "/";
            _context.Request.Method = "GET";

            // Act
            await middleware.InvokeAsync(_context);

            // Assert
            _context.Response.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
        }

        [Fact]
        public async Task InvokeAsync_NullAllowedRoutes_ShouldReturn403ForNonSystemRoutes()
        {
            // Arrange
            var options = CreateOptions(null); // Null routes
            var middleware = new RouteAuthorizationMiddleware(_nextMock.Object, _loggerMock.Object, options);
            _context.Request.Path = "/api/test";
            _context.Request.Method = "GET";

            // Act
            await middleware.InvokeAsync(_context);

            // Assert
            _context.Response.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
        }

        [Fact]
        public async Task InvokeAsync_EmptyAllowedRoutes_ShouldReturn403ForNonSystemRoutes()
        {
            // Arrange
            var options = CreateOptions(new List<AllowedRoute>()); // Empty list
            var middleware = new RouteAuthorizationMiddleware(_nextMock.Object, _loggerMock.Object, options);
            _context.Request.Path = "/api/test";
            _context.Request.Method = "GET";

            // Act
            await middleware.InvokeAsync(_context);

            // Assert
            _context.Response.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
        }

        #endregion

        #region Real-World Scenarios Tests

        [Fact]
        public async Task InvokeAsync_CompleteGatewayScenario_PublicEndpoints_ShouldWork()
        {
            // Arrange - Simular configuración real del gateway
            var routes = new List<AllowedRoute>
            {
                new AllowedRoute { Service = "test-service",
                    PathPrefix = "/api/users/login",
                    Methods = new[] { "POST" },
                    RequiresAuth = false
                },
                new AllowedRoute { Service = "test-service",
                    PathPrefix = "/api/users/register",
                    Methods = new[] { "POST" },
                    RequiresAuth = false
                }
            };
            var options = CreateOptions(routes);
            var middleware = new RouteAuthorizationMiddleware(_nextMock.Object, _loggerMock.Object, options);

            // Test login
            _context.Request.Path = "/api/users/login";
            _context.Request.Method = "POST";
            await middleware.InvokeAsync(_context);
            _nextMock.Verify(next => next(_context), Times.Once);

            // Test register
            var context2 = new DefaultHttpContext();
            context2.Response.Body = new MemoryStream();
            context2.Request.Path = "/api/users/register";
            context2.Request.Method = "POST";
            await middleware.InvokeAsync(context2);
            _nextMock.Verify(next => next(context2), Times.Once);
        }

        [Fact]
        public async Task InvokeAsync_CompleteGatewayScenario_ProtectedEndpoints_ShouldRequireAuth()
        {
            // Arrange
            var routes = new List<AllowedRoute>
            {
                new AllowedRoute { Service = "test-service",
                    PathPrefix = "/api/users/profile",
                    Methods = new[] { "GET", "PUT" },
                    RequiresAuth = true
                },
                new AllowedRoute { Service = "test-service",
                    PathPrefix = "/api/admin",
                    Methods = new[] { "GET", "POST", "DELETE" },
                    RequiresAuth = true
                }
            };
            var options = CreateOptions(routes);
            var middleware = new RouteAuthorizationMiddleware(_nextMock.Object, _loggerMock.Object, options);

            // Test protected route without auth - should fail
            _context.Request.Path = "/api/users/profile";
            _context.Request.Method = "GET";
            _context.User = new ClaimsPrincipal();
            await middleware.InvokeAsync(_context);
            _context.Response.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);

            // Test same route with auth - should succeed
            var context2 = new DefaultHttpContext();
            context2.Response.Body = new MemoryStream();
            context2.Request.Path = "/api/users/profile";
            context2.Request.Method = "GET";
            var claims = new[] { new Claim(ClaimTypes.NameIdentifier, "user-123") };
            var identity = new ClaimsIdentity(claims, "Bearer");
            context2.User = new ClaimsPrincipal(identity);
            await middleware.InvokeAsync(context2);
            _nextMock.Verify(next => next(context2), Times.Once);
        }

        [Fact]
        public async Task InvokeAsync_MixedConfiguration_ShouldHandleCorrectly()
        {
            // Arrange - Mix of public and protected routes
            var routes = new List<AllowedRoute>
            {
                new AllowedRoute { Service = "test-service", PathPrefix = "/api/public", Methods = new[] { "GET" }, RequiresAuth = false },
                new AllowedRoute { Service = "test-service", PathPrefix = "/api/private", Methods = new[] { "GET" }, RequiresAuth = true },
                new AllowedRoute { Service = "test-service", PathPrefix = "/api/mixed", Methods = new[] { "GET" }, RequiresAuth = false },
                new AllowedRoute { Service = "test-service", PathPrefix = "/api/mixed", Methods = new[] { "POST" }, RequiresAuth = true }
            };
            var options = CreateOptions(routes);
            var middleware = new RouteAuthorizationMiddleware(_nextMock.Object, _loggerMock.Object, options);

            // Test public GET
            var context1 = new DefaultHttpContext();
            context1.Response.Body = new MemoryStream();
            context1.Request.Path = "/api/public/data";
            context1.Request.Method = "GET";
            await middleware.InvokeAsync(context1);
            _nextMock.Verify(next => next(context1), Times.Once);

            // Test private GET without auth
            var context2 = new DefaultHttpContext();
            context2.Response.Body = new MemoryStream();
            context2.Request.Path = "/api/private/data";
            context2.Request.Method = "GET";
            await middleware.InvokeAsync(context2);
            context2.Response.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);

            // Test mixed GET (public)
            var context3 = new DefaultHttpContext();
            context3.Response.Body = new MemoryStream();
            context3.Request.Path = "/api/mixed/data";
            context3.Request.Method = "GET";
            await middleware.InvokeAsync(context3);
            _nextMock.Verify(next => next(context3), Times.Once);

            // Test mixed POST without auth (protected)
            var context4 = new DefaultHttpContext();
            context4.Response.Body = new MemoryStream();
            context4.Request.Path = "/api/mixed/data";
            context4.Request.Method = "POST";
            await middleware.InvokeAsync(context4);
            context4.Response.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
        }

        #endregion

        #region Response Format Tests

        [Fact]
        public async Task InvokeAsync_UnauthorizedResponse_ShouldIncludePathAndTimestamp()
        {
            // Arrange
            var routes = new List<AllowedRoute>
            {
                new AllowedRoute { Service = "test-service",
                    PathPrefix = "/api/secure",
                    Methods = new[] { "GET" },
                    RequiresAuth = true
                }
            };
            var options = CreateOptions(routes);
            var middleware = new RouteAuthorizationMiddleware(_nextMock.Object, _loggerMock.Object, options);
            _context.Request.Path = "/api/secure/resource";
            _context.Request.Method = "GET";

            // Act
            await middleware.InvokeAsync(_context);

            // Assert
            _context.Response.Body.Seek(0, SeekOrigin.Begin);
            var responseBody = await new StreamReader(_context.Response.Body).ReadToEndAsync();
            var response = JsonDocument.Parse(responseBody);

            response.RootElement.GetProperty("error").GetString().Should().Be("Unauthorized");
            response.RootElement.GetProperty("path").GetString().Should().Be("/api/secure/resource");
            response.RootElement.GetProperty("timestamp").ValueKind.Should().Be(JsonValueKind.String);
        }

        [Fact]
        public async Task InvokeAsync_ForbiddenResponse_ShouldIncludePathAndTimestamp()
        {
            // Arrange
            var routes = new List<AllowedRoute>();
            var options = CreateOptions(routes);
            var middleware = new RouteAuthorizationMiddleware(_nextMock.Object, _loggerMock.Object, options);
            _context.Request.Path = "/api/notconfigured";
            _context.Request.Method = "GET";

            // Act
            await middleware.InvokeAsync(_context);

            // Assert
            _context.Response.Body.Seek(0, SeekOrigin.Begin);
            var responseBody = await new StreamReader(_context.Response.Body).ReadToEndAsync();
            var response = JsonDocument.Parse(responseBody);

            response.RootElement.GetProperty("error").GetString().Should().Be("Forbidden");
            response.RootElement.GetProperty("path").GetString().Should().Be("/api/notconfigured");
            response.RootElement.GetProperty("timestamp").ValueKind.Should().Be(JsonValueKind.String);
        }

        #endregion

        #region Helper Methods

        private static IOptions<GateOptions> CreateOptions(List<AllowedRoute>? allowedRoutes)
        {
            var gateOptions = new GateOptions
            {
                AllowedRoutes = allowedRoutes ?? new List<AllowedRoute>()
            };
            return Options.Create(gateOptions);
        }

        #endregion
    }
}
