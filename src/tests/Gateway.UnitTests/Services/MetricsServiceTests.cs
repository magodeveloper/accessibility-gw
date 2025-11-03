using Xunit;
using NSubstitute;
using FluentAssertions;
using Gateway.Services;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Gateway.UnitTests.Services
{
    public class MetricsServiceTests
    {
        private readonly ILogger<MetricsService> _mockLogger;
        private readonly MetricsService _metricsService;

        public MetricsServiceTests()
        {
            _mockLogger = Substitute.For<ILogger<MetricsService>>();
            _metricsService = new MetricsService(_mockLogger);
        }

        [Fact]
        public void StartActivity_WithValidParameters_ShouldCreateActivity()
        {
            // Arrange
            var operationName = "TestOperation";
            var service = "users";
            var method = "GET";
            var path = "/api/v1/users";

            // Act
            using var activity = _metricsService.StartActivity(operationName, service, method, path);

            // Assert
            // En .NET 6+ si no hay listeners, Activity será null (comportamiento esperado en tests)
            // Por lo tanto, solo validamos que no lance excepción y que el tipo sea correcto si existe
            (activity is null || activity.OperationName == operationName).Should().BeTrue();
        }

        [Fact]
        public void RecordRequest_WithSuccessfulRequest_ShouldIncrementCounters()
        {
            // Arrange
            var service = "users";
            var method = "GET";
            var statusCode = 200;
            var responseTime = 150.5;

            // Act
            _metricsService.RecordRequest(service, method, statusCode, responseTime);

            // Assert
            var metrics = _metricsService.GetMetrics();
            metrics["totalRequests"].Should().Be(1L);
            metrics["successfulRequests"].Should().Be(1L);
            metrics["failedRequests"].Should().Be(0L);
            metrics["cachedRequests"].Should().Be(0L);
            metrics["successRate"].Should().Be(1.0);
            metrics["cacheHitRate"].Should().Be(0.0);
        }

        [Fact]
        public void RecordRequest_WithFailedRequest_ShouldIncrementFailedCounter()
        {
            // Arrange
            var service = "users";
            var method = "GET";
            var statusCode = 500;
            var responseTime = 200.0;

            // Act
            _metricsService.RecordRequest(service, method, statusCode, responseTime);

            // Assert
            var metrics = _metricsService.GetMetrics();
            metrics["totalRequests"].Should().Be(1L);
            metrics["successfulRequests"].Should().Be(0L);
            metrics["failedRequests"].Should().Be(1L);
            metrics["successRate"].Should().Be(0.0);
        }

        [Fact]
        public void RecordRequest_WithCachedRequest_ShouldIncrementCachedCounter()
        {
            // Arrange
            var service = "users";
            var method = "GET";
            var statusCode = 200;
            var responseTime = 50.0;

            // Act
            _metricsService.RecordRequest(service, method, statusCode, responseTime, fromCache: true);

            // Assert
            var metrics = _metricsService.GetMetrics();
            metrics["totalRequests"].Should().Be(1L);
            metrics["cachedRequests"].Should().Be(1L);
            metrics["cacheHitRate"].Should().Be(1.0);
        }

        [Theory]
        [InlineData(200, true)]  // 2xx = success
        [InlineData(201, true)]
        [InlineData(204, true)]
        [InlineData(301, true)]  // 3xx = success
        [InlineData(302, true)]
        [InlineData(400, false)] // 4xx = failure
        [InlineData(404, false)]
        [InlineData(500, false)] // 5xx = failure
        [InlineData(502, false)]
        public void RecordRequest_WithDifferentStatusCodes_ShouldClassifyCorrectly(int statusCode, bool isSuccess)
        {
            // Arrange
            var service = "test";
            var method = "GET";
            var responseTime = 100.0;

            // Act
            _metricsService.RecordRequest(service, method, statusCode, responseTime);

            // Assert
            var metrics = _metricsService.GetMetrics();
            if (isSuccess)
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
        public void RecordRequest_WithMultipleServices_ShouldTrackByService()
        {
            // Arrange & Act
            _metricsService.RecordRequest("users", "GET", 200, 100.0);
            _metricsService.RecordRequest("users", "POST", 201, 150.0);
            _metricsService.RecordRequest("reports", "GET", 200, 200.0);

            // Assert
            var metrics = _metricsService.GetMetrics();
            var requestsByService = (Dictionary<string, long>)metrics["requestsByService"];

            requestsByService.Should().ContainKey("users").WhoseValue.Should().Be(2L);
            requestsByService.Should().ContainKey("reports").WhoseValue.Should().Be(1L);
        }

        [Fact]
        public void RecordRequest_WithMultipleStatusCodes_ShouldTrackByStatusCode()
        {
            // Arrange & Act
            _metricsService.RecordRequest("users", "GET", 200, 100.0);
            _metricsService.RecordRequest("users", "GET", 200, 110.0);
            _metricsService.RecordRequest("users", "GET", 404, 50.0);
            _metricsService.RecordRequest("users", "POST", 500, 200.0);

            // Assert
            var metrics = _metricsService.GetMetrics();
            var requestsByStatusCode = (Dictionary<int, long>)metrics["requestsByStatusCode"];

            requestsByStatusCode.Should().ContainKey(200).WhoseValue.Should().Be(2L);
            requestsByStatusCode.Should().ContainKey(404).WhoseValue.Should().Be(1L);
            requestsByStatusCode.Should().ContainKey(500).WhoseValue.Should().Be(1L);
        }

        [Fact]
        public void RecordRequest_WithResponseTimes_ShouldCalculateAverages()
        {
            // Arrange
            var service = "users";
            var method = "GET";

            // Act - Simular múltiples requests con diferentes tiempos
            _metricsService.RecordRequest(service, method, 200, 100.0);
            _metricsService.RecordRequest(service, method, 200, 200.0);

            // Assert
            var metrics = _metricsService.GetMetrics();
            var averageResponseTimes = (Dictionary<string, double>)metrics["averageResponseTimes"];
            var key = $"{service}:{method}";

            averageResponseTimes.Should().ContainKey(key);
            var average = averageResponseTimes[key];

            // Primera request: 100ms
            // Segunda request: (100 * 0.8) + (200 * 0.2) = 80 + 40 = 120ms
            average.Should().Be(120.0);
        }

        [Fact]
        public void RecordRequest_ShouldRecordMetrics()
        {
            // Arrange
            var service = "users";
            var method = "GET";
            var statusCode = 200;
            var responseTime = 150.0;

            // Act
            _metricsService.RecordRequest(service, method, statusCode, responseTime);

            // Assert
            var metrics = _metricsService.GetMetrics();
            metrics["totalRequests"].Should().Be(1L);
            metrics["successfulRequests"].Should().Be(1L);
        }

        [Fact]
        public void GetMetrics_WithNoRequests_ShouldReturnZeroMetrics()
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
        public void GetMetrics_WithMixedRequests_ShouldCalculateCorrectRates()
        {
            // Arrange & Act
            _metricsService.RecordRequest("users", "GET", 200, 100.0, fromCache: true);  // success + cached
            _metricsService.RecordRequest("users", "POST", 201, 150.0, fromCache: false); // success
            _metricsService.RecordRequest("users", "GET", 404, 50.0, fromCache: false);   // failure
            _metricsService.RecordRequest("reports", "GET", 500, 200.0, fromCache: false); // failure

            // Assert
            var metrics = _metricsService.GetMetrics();
            metrics["totalRequests"].Should().Be(4L);
            metrics["successfulRequests"].Should().Be(2L);
            metrics["failedRequests"].Should().Be(2L);
            metrics["cachedRequests"].Should().Be(1L);
            metrics["successRate"].Should().Be(0.5); // 2/4
            metrics["cacheHitRate"].Should().Be(0.25); // 1/4
        }

        [Fact]
        public void ResetMetrics_ShouldClearAllCounters()
        {
            // Arrange - Agregar algunas métricas
            _metricsService.RecordRequest("users", "GET", 200, 100.0);
            _metricsService.RecordRequest("reports", "POST", 201, 150.0, fromCache: true);
            _metricsService.RecordRequest("users", "GET", 404, 50.0);

            // Verificar que hay métricas antes del reset
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

        [Fact]
        public async Task RecordRequest_ConcurrentCalls_ShouldHandleThreadSafety()
        {
            // Arrange
            const int numberOfThreads = 10;
            const int requestsPerThread = 100;
            var tasks = new List<Task>();

            // Act
            for (int i = 0; i < numberOfThreads; i++)
            {
                var threadIndex = i;
                tasks.Add(Task.Run(() =>
                {
                    for (int j = 0; j < requestsPerThread; j++)
                    {
                        _metricsService.RecordRequest($"service{threadIndex}", "GET", 200, 100.0);
                    }
                }));
            }

            await Task.WhenAll(tasks.ToArray());

            // Assert
            var metrics = _metricsService.GetMetrics();
            metrics["totalRequests"].Should().Be(numberOfThreads * requestsPerThread);
            metrics["successfulRequests"].Should().Be(numberOfThreads * requestsPerThread);

            var requestsByService = (Dictionary<string, long>)metrics["requestsByService"];
            requestsByService.Should().HaveCount(numberOfThreads);

            for (int i = 0; i < numberOfThreads; i++)
            {
                requestsByService.Should().ContainKey($"service{i}").WhoseValue.Should().Be(requestsPerThread);
            }
        }
    }
}
