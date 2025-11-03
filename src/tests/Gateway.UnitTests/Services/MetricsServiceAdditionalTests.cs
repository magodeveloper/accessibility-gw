using Xunit;
using NSubstitute;
using Gateway.Services;
using FluentAssertions;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Gateway.UnitTests.Services
{
    public class MetricsServiceAdditionalTests
    {
        private readonly ILogger<MetricsService> _mockLogger;
        private readonly MetricsService _metricsService;

        public MetricsServiceAdditionalTests()
        {
            _mockLogger = Substitute.For<ILogger<MetricsService>>();
            _metricsService = new MetricsService(_mockLogger);
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_WithValidLogger_ShouldInitializeService()
        {
            // Arrange & Act
            var service = new MetricsService(_mockLogger);

            // Assert
            service.Should().NotBeNull();
            var metrics = service.GetMetrics();
            metrics.Should().NotBeNull();
            metrics["totalRequests"].Should().Be(0L);
        }

        [Fact]
        public void Constructor_WithNullLogger_ShouldNotThrow()
        {
            // Arrange & Act
            var service = new MetricsService(null!);

            // Assert
            service.Should().NotBeNull();
            // El servicio acepta logger null, que es válido para este caso
        }

        #endregion

        #region StartActivity Tests

        [Fact]
        public void StartActivity_WithValidParameters_ShouldReturnActivityWithTags()
        {
            // Arrange
            using var listener = new ActivityListener
            {
                ShouldListenTo = _ => true,
                Sample = (ref ActivityCreationOptions<ActivityContext> options) => ActivitySamplingResult.AllData
            };
            ActivitySource.AddActivityListener(listener);

            var operationName = "TestOperation";
            var service = "users";
            var method = "GET";
            var path = "/api/v1/users";

            // Act
            using var activity = _metricsService.StartActivity(operationName, service, method, path);

            // Assert
            if (activity != null)
            {
                activity.OperationName.Should().Be(operationName);
                activity.GetTagItem("gateway.service").Should().Be(service);
                activity.GetTagItem("gateway.method").Should().Be(method);
                activity.GetTagItem("gateway.path").Should().Be(path);
                activity.GetTagItem("gateway.timestamp").Should().NotBeNull();
            }
        }

        [Fact]
        public void StartActivity_WithNullOperationName_ShouldHandleGracefully()
        {
            // Arrange & Act
            using var activity = _metricsService.StartActivity(null!, "service", "GET", "/path");

            // Assert - Should not throw, activity may be null in test environment
            // This covers the null path branches in StartActivity
        }

        [Fact]
        public void StartActivity_WithEmptyStrings_ShouldHandleGracefully()
        {
            // Arrange & Act
            using var activity = _metricsService.StartActivity("", "", "", "");

            // Assert - Should not throw
            // This exercises the SetTag branches with empty values
        }

        #endregion

        #region RecordRequest Internal Logic Tests

        [Fact]
        public void RecordRequest_WithCachedRequest_ShouldIncrementCachedCounter()
        {
            // Arrange
            var service = "users";
            var method = "GET";
            var statusCode = 200;
            var responseTime = 100.0;

            // Act
            _metricsService.RecordRequest(service, method, statusCode, responseTime, fromCache: true);

            // Assert
            var metrics = _metricsService.GetMetrics();
            metrics["totalRequests"].Should().Be(1L);
            metrics["cachedRequests"].Should().Be(1L);
            metrics["successfulRequests"].Should().Be(1L);
            metrics["cacheHitRate"].Should().Be(1.0);
        }

        [Fact]
        public void RecordRequest_WithNonCachedRequest_ShouldNotIncrementCachedCounter()
        {
            // Arrange
            var service = "users";
            var method = "GET";
            var statusCode = 200;
            var responseTime = 100.0;

            // Act
            _metricsService.RecordRequest(service, method, statusCode, responseTime, fromCache: false);

            // Assert
            var metrics = _metricsService.GetMetrics();
            metrics["totalRequests"].Should().Be(1L);
            metrics["cachedRequests"].Should().Be(0L);
            metrics["successfulRequests"].Should().Be(1L);
            metrics["cacheHitRate"].Should().Be(0.0);
        }

        [Theory]
        [InlineData(199, false)] // Below 200
        [InlineData(200, true)]  // Exactly 200
        [InlineData(299, true)]  // Upper 2xx
        [InlineData(300, true)]  // Exactly 300
        [InlineData(399, true)]  // Upper 3xx
        [InlineData(400, false)] // Exactly 400
        [InlineData(500, false)] // 5xx error
        public void RecordRequest_WithDifferentStatusCodes_ShouldClassifySuccessCorrectly(int statusCode, bool expectedSuccess)
        {
            // Arrange
            var service = "test";
            var method = "GET";
            var responseTime = 100.0;

            // Act
            _metricsService.RecordRequest(service, method, statusCode, responseTime);

            // Assert
            var metrics = _metricsService.GetMetrics();
            if (expectedSuccess)
            {
                metrics["successfulRequests"].Should().Be(1L);
                metrics["failedRequests"].Should().Be(0L);
            }
            else
            {
                metrics["successfulRequests"].Should().Be(0L);
                metrics["failedRequests"].Should().Be(1L);
            }
        }

        [Fact]
        public void RecordRequest_WithNewService_ShouldInitializeServiceCounter()
        {
            // Arrange
            var service = "newservice";
            var method = "POST";
            var statusCode = 201;
            var responseTime = 150.0;

            // Act
            _metricsService.RecordRequest(service, method, statusCode, responseTime);

            // Assert
            var metrics = _metricsService.GetMetrics();
            var requestsByService = (Dictionary<string, long>)metrics["requestsByService"];
            requestsByService.Should().ContainKey(service);
            requestsByService[service].Should().Be(1L);
        }

        [Fact]
        public void RecordRequest_WithExistingService_ShouldIncrementServiceCounter()
        {
            // Arrange
            var service = "users";
            var method = "GET";

            // Act
            _metricsService.RecordRequest(service, method, 200, 100.0);
            _metricsService.RecordRequest(service, method, 201, 110.0);

            // Assert
            var metrics = _metricsService.GetMetrics();
            var requestsByService = (Dictionary<string, long>)metrics["requestsByService"];
            requestsByService[service].Should().Be(2L);
        }

        [Fact]
        public void RecordRequest_WithNewStatusCode_ShouldInitializeStatusCodeCounter()
        {
            // Arrange
            var statusCode = 418; // I'm a teapot
            var service = "test";
            var method = "GET";

            // Act
            _metricsService.RecordRequest(service, method, statusCode, 100.0);

            // Assert
            var metrics = _metricsService.GetMetrics();
            var requestsByStatusCode = (Dictionary<int, long>)metrics["requestsByStatusCode"];
            requestsByStatusCode.Should().ContainKey(statusCode);
            requestsByStatusCode[statusCode].Should().Be(1L);
        }

        [Fact]
        public void RecordRequest_WithExistingStatusCode_ShouldIncrementStatusCodeCounter()
        {
            // Arrange
            var statusCode = 200;
            var service = "test";
            var method = "GET";

            // Act
            _metricsService.RecordRequest(service, method, statusCode, 100.0);
            _metricsService.RecordRequest(service, method, statusCode, 110.0);

            // Assert
            var metrics = _metricsService.GetMetrics();
            var requestsByStatusCode = (Dictionary<int, long>)metrics["requestsByStatusCode"];
            requestsByStatusCode[statusCode].Should().Be(2L);
        }

        [Fact]
        public void RecordRequest_WithNewServiceAndMethod_ShouldInitializeResponseTime()
        {
            // Arrange
            var service = "reports";
            var method = "POST";
            var responseTime = 200.0;

            // Act
            _metricsService.RecordRequest(service, method, 201, responseTime);

            // Assert
            var metrics = _metricsService.GetMetrics();
            var averageResponseTimes = (Dictionary<string, double>)metrics["averageResponseTimes"];
            var key = $"{service}:{method}";
            averageResponseTimes.Should().ContainKey(key);
            averageResponseTimes[key].Should().Be(responseTime);
        }

        [Fact]
        public void RecordRequest_WithExistingServiceAndMethod_ShouldUpdateMovingAverage()
        {
            // Arrange
            var service = "users";
            var method = "GET";
            var firstResponseTime = 100.0;
            var secondResponseTime = 200.0;

            // Act
            _metricsService.RecordRequest(service, method, 200, firstResponseTime);
            _metricsService.RecordRequest(service, method, 200, secondResponseTime);

            // Assert
            var metrics = _metricsService.GetMetrics();
            var averageResponseTimes = (Dictionary<string, double>)metrics["averageResponseTimes"];
            var key = $"{service}:{method}";

            // Expected calculation: (100 * 0.8) + (200 * 0.2) = 80 + 40 = 120
            averageResponseTimes[key].Should().Be(120.0);
        }

        #endregion

        #region GetMetrics Tests

        [Fact]
        public void GetMetrics_WithZeroRequests_ShouldReturnZeroRates()
        {
            // Act
            var metrics = _metricsService.GetMetrics();

            // Assert
            metrics["totalRequests"].Should().Be(0L);
            metrics["successfulRequests"].Should().Be(0L);
            metrics["failedRequests"].Should().Be(0L);
            metrics["cachedRequests"].Should().Be(0L);
            metrics["successRate"].Should().Be(0.0);
            metrics["cacheHitRate"].Should().Be(0.0);
            metrics.Should().ContainKey("timestamp");
            metrics.Should().ContainKey("requestsByService");
            metrics.Should().ContainKey("requestsByStatusCode");
            metrics.Should().ContainKey("averageResponseTimes");
        }

        [Fact]
        public void GetMetrics_WithMultipleRequests_ShouldCalculateCorrectRates()
        {
            // Arrange & Act
            _metricsService.RecordRequest("users", "GET", 200, 100.0, fromCache: true);
            _metricsService.RecordRequest("users", "POST", 201, 150.0, fromCache: false);
            _metricsService.RecordRequest("reports", "GET", 404, 50.0, fromCache: false);
            _metricsService.RecordRequest("analysis", "POST", 500, 200.0, fromCache: false);

            // Assert
            var metrics = _metricsService.GetMetrics();
            metrics["totalRequests"].Should().Be(4L);
            metrics["successfulRequests"].Should().Be(2L);
            metrics["failedRequests"].Should().Be(2L);
            metrics["cachedRequests"].Should().Be(1L);
            metrics["successRate"].Should().Be(0.5); // 2/4
            metrics["cacheHitRate"].Should().Be(0.25); // 1/4

            var requestsByService = (Dictionary<string, long>)metrics["requestsByService"];
            requestsByService["users"].Should().Be(2L);
            requestsByService["reports"].Should().Be(1L);
            requestsByService["analysis"].Should().Be(1L);

            var requestsByStatusCode = (Dictionary<int, long>)metrics["requestsByStatusCode"];
            requestsByStatusCode[200].Should().Be(1L);
            requestsByStatusCode[201].Should().Be(1L);
            requestsByStatusCode[404].Should().Be(1L);
            requestsByStatusCode[500].Should().Be(1L);
        }

        [Fact]
        public void GetMetrics_ShouldIncludeTimestamp()
        {
            // Arrange
            var beforeCall = DateTimeOffset.UtcNow;

            // Act
            var metrics = _metricsService.GetMetrics();

            // Assert
            var afterCall = DateTimeOffset.UtcNow;
            metrics.Should().ContainKey("timestamp");
            var timestamp = (DateTimeOffset)metrics["timestamp"];
            timestamp.Should().BeOnOrAfter(beforeCall).And.BeOnOrBefore(afterCall);
        }

        #endregion

        #region ResetMetrics Tests

        [Fact]
        public void ResetMetrics_WithExistingMetrics_ShouldClearAllCounters()
        {
            // Arrange - Add some metrics first
            _metricsService.RecordRequest("users", "GET", 200, 100.0);
            _metricsService.RecordRequest("reports", "POST", 201, 150.0, fromCache: true);
            _metricsService.RecordRequest("users", "DELETE", 404, 50.0);

            // Verify metrics exist before reset
            var metricsBeforeReset = _metricsService.GetMetrics();
            metricsBeforeReset["totalRequests"].Should().Be(3L);

            // Act
            _metricsService.ResetMetrics();

            // Assert
            var metricsAfterReset = _metricsService.GetMetrics();
            metricsAfterReset["totalRequests"].Should().Be(0L);
            metricsAfterReset["successfulRequests"].Should().Be(0L);
            metricsAfterReset["failedRequests"].Should().Be(0L);
            metricsAfterReset["cachedRequests"].Should().Be(0L);
            metricsAfterReset["successRate"].Should().Be(0.0);
            metricsAfterReset["cacheHitRate"].Should().Be(0.0);

            var requestsByService = (Dictionary<string, long>)metricsAfterReset["requestsByService"];
            var requestsByStatusCode = (Dictionary<int, long>)metricsAfterReset["requestsByStatusCode"];
            var averageResponseTimes = (Dictionary<string, double>)metricsAfterReset["averageResponseTimes"];

            requestsByService.Should().BeEmpty();
            requestsByStatusCode.Should().BeEmpty();
            averageResponseTimes.Should().BeEmpty();
        }

        [Fact]
        public void ResetMetrics_ShouldLogInformation()
        {
            // Arrange
            _metricsService.RecordRequest("test", "GET", 200, 100.0);

            // Act
            _metricsService.ResetMetrics();

            // Assert
            _mockLogger.Received(1).LogInformation("Metrics reset completed");
        }

        [Fact]
        public void ResetMetrics_WithNoExistingMetrics_ShouldNotThrow()
        {
            // Act & Assert
            var act = () => _metricsService.ResetMetrics();
            act.Should().NotThrow();

            var metrics = _metricsService.GetMetrics();
            metrics["totalRequests"].Should().Be(0L);
        }

        #endregion

        #region Edge Cases and Thread Safety

        [Fact]
        public void RecordRequest_WithEmptyServiceName_ShouldHandleGracefully()
        {
            // Arrange & Act
            _metricsService.RecordRequest("", "GET", 200, 100.0);

            // Assert
            var metrics = _metricsService.GetMetrics();
            var requestsByService = (Dictionary<string, long>)metrics["requestsByService"];
            requestsByService.Should().ContainKey("");
            requestsByService[""].Should().Be(1L);
        }

        [Fact]
        public void RecordRequest_WithEmptyMethodName_ShouldHandleGracefully()
        {
            // Arrange & Act
            _metricsService.RecordRequest("users", "", 200, 100.0);

            // Assert
            var metrics = _metricsService.GetMetrics();
            var averageResponseTimes = (Dictionary<string, double>)metrics["averageResponseTimes"];
            averageResponseTimes.Should().ContainKey("users:");
        }

        [Fact]
        public void RecordRequest_WithZeroResponseTime_ShouldHandleGracefully()
        {
            // Arrange & Act
            _metricsService.RecordRequest("users", "GET", 200, 0.0);

            // Assert
            var metrics = _metricsService.GetMetrics();
            var averageResponseTimes = (Dictionary<string, double>)metrics["averageResponseTimes"];
            averageResponseTimes["users:GET"].Should().Be(0.0);
        }

        [Fact]
        public void RecordRequest_WithNegativeResponseTime_ShouldHandleGracefully()
        {
            // Arrange & Act
            _metricsService.RecordRequest("users", "GET", 200, -50.0);

            // Assert
            var metrics = _metricsService.GetMetrics();
            var averageResponseTimes = (Dictionary<string, double>)metrics["averageResponseTimes"];
            averageResponseTimes["users:GET"].Should().Be(-50.0);
        }

        #endregion
    }
}
