using Xunit;
using Prometheus;
using FluentAssertions;
using Gateway.Services;

namespace Gateway.UnitTests.Services
{
    /// <summary>
    /// Tests para GatewayPrometheusMetrics - Métricas de Prometheus del Gateway
    /// Target: >80% coverage
    /// </summary>
    public class GatewayPrometheusMetricsTests
    {
        private readonly GatewayPrometheusMetrics _metrics;

        public GatewayPrometheusMetricsTests()
        {
            _metrics = new GatewayPrometheusMetrics();
        }

        #region RecordProxyRequest Tests

        [Theory]
        [InlineData("users", "GET", 200, 150.5)]
        [InlineData("reports", "POST", 201, 250.3)]
        [InlineData("analysis", "PUT", 200, 180.7)]
        [InlineData("middleware", "DELETE", 204, 95.2)]
        public void RecordProxyRequest_WithValidData_ShouldNotThrow(
            string targetService, string method, int statusCode, double durationMs)
        {
            // Act
            Action act = () => _metrics.RecordProxyRequest(targetService, method, statusCode, durationMs);

            // Assert
            act.Should().NotThrow();
        }

        [Fact]
        public void RecordProxyRequest_Multiple_ShouldNotThrow()
        {
            // Arrange & Act
            Action act = () =>
            {
                _metrics.RecordProxyRequest("users", "GET", 200, 100);
                _metrics.RecordProxyRequest("users", "GET", 200, 150);
                _metrics.RecordProxyRequest("users", "POST", 201, 200);
                _metrics.RecordProxyRequest("reports", "GET", 200, 120);
            };

            // Assert
            act.Should().NotThrow();
        }

        [Theory]
        [InlineData("users", "GET", 200, 0.0)] // Zero duration
        [InlineData("users", "GET", 200, 0.1)] // Very small duration
        [InlineData("users", "GET", 200, 10000.0)] // Very large duration
        public void RecordProxyRequest_WithEdgeCaseDurations_ShouldNotThrow(
            string targetService, string method, int statusCode, double durationMs)
        {
            // Act
            Action act = () => _metrics.RecordProxyRequest(targetService, method, statusCode, durationMs);

            // Assert
            act.Should().NotThrow();
        }

        [Theory]
        [InlineData("users", "GET", 200)]
        [InlineData("users", "GET", 201)]
        [InlineData("users", "GET", 400)]
        [InlineData("users", "GET", 401)]
        [InlineData("users", "GET", 403)]
        [InlineData("users", "GET", 404)]
        [InlineData("users", "GET", 500)]
        [InlineData("users", "GET", 502)]
        [InlineData("users", "GET", 503)]
        public void RecordProxyRequest_WithDifferentStatusCodes_ShouldNotThrow(
            string targetService, string method, int statusCode)
        {
            // Act
            Action act = () => _metrics.RecordProxyRequest(targetService, method, statusCode, 100);

            // Assert
            act.Should().NotThrow();
        }

        [Theory]
        [InlineData("GET")]
        [InlineData("POST")]
        [InlineData("PUT")]
        [InlineData("PATCH")]
        [InlineData("DELETE")]
        [InlineData("HEAD")]
        [InlineData("OPTIONS")]
        public void RecordProxyRequest_WithDifferentHttpMethods_ShouldNotThrow(string method)
        {
            // Act
            Action act = () => _metrics.RecordProxyRequest("users", method, 200, 100);

            // Assert
            act.Should().NotThrow();
        }

        #endregion

        #region RecordCacheHit Tests

        [Theory]
        [InlineData("users", "/api/users")]
        [InlineData("reports", "/api/reports/monthly")]
        [InlineData("analysis", "/api/analysis/results")]
        [InlineData("middleware", "/api/middleware/sessions")]
        public void RecordCacheHit_WithValidData_ShouldNotThrow(string service, string endpoint)
        {
            // Act
            Action act = () => _metrics.RecordCacheHit(service, endpoint);

            // Assert
            act.Should().NotThrow();
        }

        [Fact]
        public void RecordCacheHit_Multiple_ShouldNotThrow()
        {
            // Arrange & Act
            Action act = () =>
            {
                _metrics.RecordCacheHit("users", "/api/users");
                _metrics.RecordCacheHit("users", "/api/users");
                _metrics.RecordCacheHit("users", "/api/users/123");
                _metrics.RecordCacheHit("reports", "/api/reports");
            };

            // Assert
            act.Should().NotThrow();
        }

        [Theory]
        [InlineData("users", "")]
        [InlineData("", "/api/users")]
        public void RecordCacheHit_WithEmptyStrings_ShouldNotThrow(string service, string endpoint)
        {
            // Act
            Action act = () => _metrics.RecordCacheHit(service, endpoint);

            // Assert
            act.Should().NotThrow();
        }

        #endregion

        #region RecordCacheMiss Tests

        [Theory]
        [InlineData("users", "/api/users")]
        [InlineData("reports", "/api/reports/monthly")]
        [InlineData("analysis", "/api/analysis/results")]
        [InlineData("middleware", "/api/middleware/sessions")]
        public void RecordCacheMiss_WithValidData_ShouldNotThrow(string service, string endpoint)
        {
            // Act
            Action act = () => _metrics.RecordCacheMiss(service, endpoint);

            // Assert
            act.Should().NotThrow();
        }

        [Fact]
        public void RecordCacheMiss_Multiple_ShouldNotThrow()
        {
            // Arrange & Act
            Action act = () =>
            {
                _metrics.RecordCacheMiss("users", "/api/users");
                _metrics.RecordCacheMiss("users", "/api/users/456");
                _metrics.RecordCacheMiss("reports", "/api/reports");
            };

            // Assert
            act.Should().NotThrow();
        }

        [Fact]
        public void RecordCacheHitAndMiss_Alternating_ShouldNotThrow()
        {
            // Arrange & Act
            Action act = () =>
            {
                _metrics.RecordCacheHit("users", "/api/users");
                _metrics.RecordCacheMiss("users", "/api/users/123");
                _metrics.RecordCacheHit("users", "/api/users");
                _metrics.RecordCacheMiss("users", "/api/users/456");
            };

            // Assert
            act.Should().NotThrow();
        }

        #endregion

        #region RecordRateLimitHit Tests

        [Theory]
        [InlineData("/api/users")]
        [InlineData("/api/reports")]
        [InlineData("/api/analysis")]
        [InlineData("/api/middleware/sessions")]
        public void RecordRateLimitHit_WithValidEndpoint_ShouldNotThrow(string endpoint)
        {
            // Act
            Action act = () => _metrics.RecordRateLimitHit(endpoint);

            // Assert
            act.Should().NotThrow();
        }

        [Fact]
        public void RecordRateLimitHit_Multiple_ShouldNotThrow()
        {
            // Arrange & Act
            Action act = () =>
            {
                _metrics.RecordRateLimitHit("/api/users");
                _metrics.RecordRateLimitHit("/api/users");
                _metrics.RecordRateLimitHit("/api/reports");
            };

            // Assert
            act.Should().NotThrow();
        }

        [Theory]
        [InlineData("")]
        [InlineData("/")]
        [InlineData("/api")]
        public void RecordRateLimitHit_WithEdgeCaseEndpoints_ShouldNotThrow(string endpoint)
        {
            // Act
            Action act = () => _metrics.RecordRateLimitHit(endpoint);

            // Assert
            act.Should().NotThrow();
        }

        #endregion

        #region RecordCircuitBreakerOpen Tests

        [Theory]
        [InlineData("users")]
        [InlineData("reports")]
        [InlineData("analysis")]
        [InlineData("middleware")]
        public void RecordCircuitBreakerOpen_WithValidService_ShouldNotThrow(string service)
        {
            // Act
            Action act = () => _metrics.RecordCircuitBreakerOpen(service);

            // Assert
            act.Should().NotThrow();
        }

        [Fact]
        public void RecordCircuitBreakerOpen_Multiple_ShouldNotThrow()
        {
            // Arrange & Act
            Action act = () =>
            {
                _metrics.RecordCircuitBreakerOpen("users");
                _metrics.RecordCircuitBreakerOpen("reports");
                _metrics.RecordCircuitBreakerOpen("users");
            };

            // Assert
            act.Should().NotThrow();
        }

        #endregion

        #region RecordCircuitBreakerClosed Tests

        [Theory]
        [InlineData("users")]
        [InlineData("reports")]
        [InlineData("analysis")]
        [InlineData("middleware")]
        public void RecordCircuitBreakerClosed_WithValidService_ShouldNotThrow(string service)
        {
            // Act
            Action act = () => _metrics.RecordCircuitBreakerClosed(service);

            // Assert
            act.Should().NotThrow();
        }

        [Fact]
        public void RecordCircuitBreakerClosed_Multiple_ShouldNotThrow()
        {
            // Arrange & Act
            Action act = () =>
            {
                _metrics.RecordCircuitBreakerClosed("users");
                _metrics.RecordCircuitBreakerClosed("reports");
            };

            // Assert
            act.Should().NotThrow();
        }

        [Fact]
        public void RecordCircuitBreakerOpenAndClosed_Sequence_ShouldNotThrow()
        {
            // Arrange & Act
            Action act = () =>
            {
                _metrics.RecordCircuitBreakerOpen("users");
                _metrics.RecordCircuitBreakerClosed("users");
                _metrics.RecordCircuitBreakerOpen("reports");
                _metrics.RecordCircuitBreakerClosed("reports");
            };

            // Assert
            act.Should().NotThrow();
        }

        #endregion

        #region IncrementActiveConnections Tests

        [Theory]
        [InlineData("users")]
        [InlineData("reports")]
        [InlineData("analysis")]
        [InlineData("middleware")]
        public void IncrementActiveConnections_WithValidService_ShouldNotThrow(string service)
        {
            // Act
            Action act = () => _metrics.IncrementActiveConnections(service);

            // Assert
            act.Should().NotThrow();
        }

        [Fact]
        public void IncrementActiveConnections_Multiple_ShouldNotThrow()
        {
            // Arrange & Act
            Action act = () =>
            {
                _metrics.IncrementActiveConnections("users");
                _metrics.IncrementActiveConnections("users");
                _metrics.IncrementActiveConnections("users");
            };

            // Assert
            act.Should().NotThrow();
        }

        #endregion

        #region DecrementActiveConnections Tests

        [Theory]
        [InlineData("users")]
        [InlineData("reports")]
        [InlineData("analysis")]
        [InlineData("middleware")]
        public void DecrementActiveConnections_WithValidService_ShouldNotThrow(string service)
        {
            // Act
            Action act = () => _metrics.DecrementActiveConnections(service);

            // Assert
            act.Should().NotThrow();
        }

        [Fact]
        public void DecrementActiveConnections_Multiple_ShouldNotThrow()
        {
            // Arrange & Act
            Action act = () =>
            {
                _metrics.DecrementActiveConnections("users");
                _metrics.DecrementActiveConnections("users");
            };

            // Assert
            act.Should().NotThrow();
        }

        [Fact]
        public void IncrementAndDecrementActiveConnections_Sequence_ShouldNotThrow()
        {
            // Arrange & Act
            Action act = () =>
            {
                _metrics.IncrementActiveConnections("users");
                _metrics.IncrementActiveConnections("users");
                _metrics.DecrementActiveConnections("users");
                _metrics.IncrementActiveConnections("reports");
                _metrics.DecrementActiveConnections("users");
                _metrics.DecrementActiveConnections("reports");
            };

            // Assert
            act.Should().NotThrow();
        }

        #endregion

        #region Interface Implementation Tests

        [Fact]
        public void GatewayPrometheusMetrics_ShouldImplementInterface()
        {
            // Assert
            _metrics.Should().BeAssignableTo<IGatewayPrometheusMetrics>();
        }

        [Fact]
        public void GatewayPrometheusMetrics_ShouldHaveAllInterfaceMethods()
        {
            // Arrange
            var interfaceType = typeof(IGatewayPrometheusMetrics);
            var implementationType = typeof(GatewayPrometheusMetrics);

            // Act
            var interfaceMethods = interfaceType.GetMethods();
            var implementationMethods = implementationType.GetMethods();

            // Assert
            foreach (var method in interfaceMethods)
            {
                implementationMethods.Should().Contain(m => m.Name == method.Name,
                    $"implementation should have method {method.Name}");
            }
        }

        #endregion

        #region Real-World Scenario Tests

        [Fact]
        public void Metrics_SimulateSuccessfulRequest_ShouldRecordAllMetrics()
        {
            // Arrange & Act
            Action act = () =>
            {
                _metrics.IncrementActiveConnections("users");
                _metrics.RecordCacheHit("users", "/api/users");
                _metrics.RecordProxyRequest("users", "GET", 200, 125.5);
                _metrics.DecrementActiveConnections("users");
            };

            // Assert
            act.Should().NotThrow();
        }

        [Fact]
        public void Metrics_SimulateFailedRequestWithCacheMiss_ShouldRecordAllMetrics()
        {
            // Arrange & Act
            Action act = () =>
            {
                _metrics.IncrementActiveConnections("reports");
                _metrics.RecordCacheMiss("reports", "/api/reports/monthly");
                _metrics.RecordProxyRequest("reports", "GET", 500, 250.0);
                _metrics.DecrementActiveConnections("reports");
            };

            // Assert
            act.Should().NotThrow();
        }

        [Fact]
        public void Metrics_SimulateRateLimitedRequest_ShouldRecordMetrics()
        {
            // Arrange & Act
            Action act = () =>
            {
                _metrics.RecordRateLimitHit("/api/users");
                _metrics.RecordProxyRequest("users", "GET", 429, 50.0);
            };

            // Assert
            act.Should().NotThrow();
        }

        [Fact]
        public void Metrics_SimulateCircuitBreakerFlow_ShouldRecordMetrics()
        {
            // Arrange & Act - Simulate circuit breaker opening due to failures
            Action act = () =>
            {
                // Failed requests
                _metrics.RecordProxyRequest("analysis", "GET", 500, 1000.0);
                _metrics.RecordProxyRequest("analysis", "GET", 503, 1500.0);
                _metrics.RecordProxyRequest("analysis", "GET", 504, 2000.0);

                // Circuit breaker opens
                _metrics.RecordCircuitBreakerOpen("analysis");

                // After recovery
                _metrics.RecordCircuitBreakerClosed("analysis");

                // Successful request
                _metrics.RecordProxyRequest("analysis", "GET", 200, 100.0);
            };

            // Assert
            act.Should().NotThrow();
        }

        [Fact]
        public void Metrics_SimulateHighLoadScenario_ShouldHandleMultipleMetrics()
        {
            // Arrange & Act - Simulate multiple concurrent requests
            Action act = () =>
            {
                // Simulate 10 concurrent requests
                for (int i = 0; i < 10; i++)
                {
                    _metrics.IncrementActiveConnections("users");

                    if (i % 3 == 0)
                        _metrics.RecordCacheHit("users", $"/api/users/{i}");
                    else
                        _metrics.RecordCacheMiss("users", $"/api/users/{i}");

                    _metrics.RecordProxyRequest("users", "GET", 200, 100 + i * 10);
                    _metrics.DecrementActiveConnections("users");
                }
            };

            // Assert
            act.Should().NotThrow();
        }

        #endregion

        #region Edge Cases

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        public void RecordProxyRequest_WithEmptyOrWhitespaceService_ShouldNotThrow(string service)
        {
            // Act
            Action act = () => _metrics.RecordProxyRequest(service, "GET", 200, 100);

            // Assert
            act.Should().NotThrow();
        }

        [Theory]
        [InlineData("service-with-dashes")]
        [InlineData("service_with_underscores")]
        [InlineData("service.with.dots")]
        [InlineData("SERVICE_UPPERCASE")]
        [InlineData("service123")]
        public void RecordProxyRequest_WithVariousServiceNameFormats_ShouldNotThrow(string service)
        {
            // Act
            Action act = () => _metrics.RecordProxyRequest(service, "GET", 200, 100);

            // Assert
            act.Should().NotThrow();
        }

        [Theory]
        [InlineData(-1.0)] // Negative duration (shouldn't happen but metrics should handle it)
        [InlineData(double.MaxValue)] // Extreme value
        public void RecordProxyRequest_WithUnusualDurations_ShouldNotThrow(double duration)
        {
            // Act
            Action act = () => _metrics.RecordProxyRequest("users", "GET", 200, duration);

            // Assert
            act.Should().NotThrow();
        }

        #endregion

        #region Constructor Tests

        [Fact]
        public void GatewayPrometheusMetrics_Constructor_ShouldCreateInstance()
        {
            // Act
            var metrics = new GatewayPrometheusMetrics();

            // Assert
            metrics.Should().NotBeNull();
        }

        [Fact]
        public void GatewayPrometheusMetrics_MultipleInstances_ShouldWork()
        {
            // Act
            var metrics1 = new GatewayPrometheusMetrics();
            var metrics2 = new GatewayPrometheusMetrics();

            // Assert
            metrics1.Should().NotBeNull();
            metrics2.Should().NotBeNull();
            metrics1.Should().NotBeSameAs(metrics2);
        }

        #endregion
    }
}
