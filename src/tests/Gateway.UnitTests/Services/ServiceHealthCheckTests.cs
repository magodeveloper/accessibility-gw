using Xunit;
using System.Net;
using NSubstitute;
using Gateway.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute.ExceptionExtensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Gateway.UnitTests.Services
{
    public class ServiceHealthCheckTests
    {
        private readonly HttpClient _mockHttpClient;
        private readonly ILogger<ServiceHealthCheck> _mockLogger;
        private readonly ServiceHealthCheck _serviceHealthCheck;
        private readonly string _serviceName = "users";
        private readonly string _serviceUrl = "http://users-api:8080";

        public ServiceHealthCheckTests()
        {
            _mockLogger = Substitute.For<ILogger<ServiceHealthCheck>>();

            // Crear HttpClient real con HttpMessageHandler mock
            var mockHandler = Substitute.For<HttpMessageHandler>();
            _mockHttpClient = new HttpClient(mockHandler)
            {
                BaseAddress = new Uri("http://test.com")
            };

            _serviceHealthCheck = new ServiceHealthCheck(_mockHttpClient, _serviceName, _serviceUrl, _mockLogger);
        }

        [Fact]
        public async Task CheckHealthAsync_WithSuccessfulResponse_ShouldReturnHealthy()
        {
            // Arrange
            var mockHandler = new MockHttpMessageHandler(HttpStatusCode.OK, "OK");
            using var httpClient = new HttpClient(mockHandler);
            var healthCheck = new ServiceHealthCheck(httpClient, _serviceName, _serviceUrl, _mockLogger);

            // Act
            var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

            // Assert
            result.Status.Should().Be(HealthStatus.Healthy);
            result.Description.Should().Be($"Service {_serviceName} is healthy");
            result.Data.Should().ContainKey("service").WhoseValue.Should().Be(_serviceName);
            result.Data.Should().ContainKey("url").WhoseValue.Should().Be(_serviceUrl);
            result.Data.Should().ContainKey("responseTime");
            result.Data.Should().ContainKey("statusCode").WhoseValue.Should().Be(200);
        }

        [Theory]
        [InlineData(HttpStatusCode.BadRequest)]
        [InlineData(HttpStatusCode.NotFound)]
        [InlineData(HttpStatusCode.InternalServerError)]
        [InlineData(HttpStatusCode.ServiceUnavailable)]
        public async Task CheckHealthAsync_WithErrorResponse_ShouldReturnUnhealthy(HttpStatusCode statusCode)
        {
            // Arrange
            var mockHandler = new MockHttpMessageHandler(statusCode, "Error");
            using var httpClient = new HttpClient(mockHandler);
            var healthCheck = new ServiceHealthCheck(httpClient, _serviceName, _serviceUrl, _mockLogger);

            // Act
            var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

            // Assert
            result.Status.Should().Be(HealthStatus.Unhealthy);
            result.Description.Should().Be($"Service {_serviceName} returned {statusCode}");
            result.Data.Should().ContainKey("service").WhoseValue.Should().Be(_serviceName);
            result.Data.Should().ContainKey("url").WhoseValue.Should().Be(_serviceUrl);
            result.Data.Should().ContainKey("responseTime");
            result.Data.Should().ContainKey("statusCode").WhoseValue.Should().Be((int)statusCode);
        }

        [Fact]
        public async Task CheckHealthAsync_WithTimeout_ShouldReturnUnhealthyWithTimeoutInfo()
        {
            // Arrange
            var mockHandler = new MockHttpMessageHandler(new TaskCanceledException("Timeout"));
            using var httpClient = new HttpClient(mockHandler);
            var healthCheck = new ServiceHealthCheck(httpClient, _serviceName, _serviceUrl, _mockLogger);

            // Act
            var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

            // Assert
            result.Status.Should().Be(HealthStatus.Unhealthy);
            result.Description.Should().Be($"Service {_serviceName} timed out");
            result.Data.Should().ContainKey("service").WhoseValue.Should().Be(_serviceName);
            result.Data.Should().ContainKey("url").WhoseValue.Should().Be(_serviceUrl);
            result.Data.Should().ContainKey("error").WhoseValue.Should().Be("Timeout");
        }

        [Fact]
        public async Task CheckHealthAsync_WithHttpRequestException_ShouldReturnUnhealthyWithExceptionInfo()
        {
            // Arrange
            var exception = new HttpRequestException("Connection refused");
            var mockHandler = new MockHttpMessageHandler(exception);
            using var httpClient = new HttpClient(mockHandler);
            var healthCheck = new ServiceHealthCheck(httpClient, _serviceName, _serviceUrl, _mockLogger);

            // Act
            var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

            // Assert
            result.Status.Should().Be(HealthStatus.Unhealthy);
            result.Description.Should().Be($"Service {_serviceName} is unhealthy: {exception.Message}");
            result.Exception.Should().Be(exception);
            result.Data.Should().ContainKey("service").WhoseValue.Should().Be(_serviceName);
            result.Data.Should().ContainKey("url").WhoseValue.Should().Be(_serviceUrl);
            result.Data.Should().ContainKey("error").WhoseValue.Should().Be(exception.Message);
        }

        [Fact]
        public async Task CheckHealthAsync_WithGeneralException_ShouldReturnUnhealthy()
        {
            // Arrange
            var exception = new InvalidOperationException("Unexpected error");
            var mockHandler = new MockHttpMessageHandler(exception);
            using var httpClient = new HttpClient(mockHandler);
            var healthCheck = new ServiceHealthCheck(httpClient, _serviceName, _serviceUrl, _mockLogger);

            // Act
            var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

            // Assert
            result.Status.Should().Be(HealthStatus.Unhealthy);
            result.Description.Should().Be($"Service {_serviceName} is unhealthy: {exception.Message}");
            result.Exception.Should().Be(exception);
        }

        [Fact]
        public async Task CheckHealthAsync_WithCancellationToken_ShouldUseToken()
        {
            // Arrange
            var cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;

            var mockHandler = new MockHttpMessageHandler(HttpStatusCode.OK, "OK");
            using var httpClient = new HttpClient(mockHandler);
            var healthCheck = new ServiceHealthCheck(httpClient, _serviceName, _serviceUrl, _mockLogger);

            // Act
            var result = await healthCheck.CheckHealthAsync(new HealthCheckContext(), cancellationToken);

            // Assert
            result.Status.Should().Be(HealthStatus.Healthy);
            mockHandler.RequestUri.Should().Be($"{_serviceUrl}/health");
        }

        [Fact]
        public async Task CheckHealthAsync_ShouldMeasureResponseTime()
        {
            // Arrange
            var mockHandler = new MockHttpMessageHandler(HttpStatusCode.OK, "OK", delayMs: 100);
            using var httpClient = new HttpClient(mockHandler);
            var healthCheck = new ServiceHealthCheck(httpClient, _serviceName, _serviceUrl, _mockLogger);

            // Act
            var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

            // Assert
            result.Status.Should().Be(HealthStatus.Healthy);
            result.Data.Should().ContainKey("responseTime");

            var responseTime = (long)result.Data["responseTime"];
            responseTime.Should().BeGreaterThan(90); // Permitir algo de variabilidad
        }

        [Fact]
        public async Task CheckHealthAsync_ShouldMakeRequestToCorrectUrl()
        {
            // Arrange
            var mockHandler = new MockHttpMessageHandler(HttpStatusCode.OK, "OK");
            using var httpClient = new HttpClient(mockHandler);
            var healthCheck = new ServiceHealthCheck(httpClient, _serviceName, _serviceUrl, _mockLogger);

            // Act
            await healthCheck.CheckHealthAsync(new HealthCheckContext());

            // Assert
            mockHandler.RequestUri.Should().Be($"{_serviceUrl}/health");
        }
    }

    public class ServiceHealthCheckFactoryTests
    {
        private readonly IHttpClientFactory _mockHttpClientFactory;
        private readonly ILogger<ServiceHealthCheck> _mockLogger;
        private readonly ServiceHealthCheckFactory _factory;

        public ServiceHealthCheckFactoryTests()
        {
            _mockHttpClientFactory = Substitute.For<IHttpClientFactory>();
            _mockLogger = Substitute.For<ILogger<ServiceHealthCheck>>();
            _factory = new ServiceHealthCheckFactory(_mockHttpClientFactory, _mockLogger);
        }

        [Fact]
        public void Create_ShouldCreateHealthCheckWithCorrectParameters()
        {
            // Arrange
            var serviceName = "users";
            var serviceUrl = "http://users-api:8080";
            var mockHttpClient = new HttpClient();

            _mockHttpClientFactory.CreateClient($"health-{serviceName}")
                .Returns(mockHttpClient);

            // Act
            var healthCheck = _factory.Create(serviceName, serviceUrl);

            // Assert
            healthCheck.Should().NotBeNull();
            healthCheck.Should().BeOfType<ServiceHealthCheck>();

            // Verificar que se creó el HttpClient con el nombre correcto
            _mockHttpClientFactory.Received(1).CreateClient($"health-{serviceName}");
        }

        [Fact]
        public void Create_ShouldConfigureHttpClientTimeout()
        {
            // Arrange
            var serviceName = "users";
            var serviceUrl = "http://users-api:8080";
            var mockHttpClient = new HttpClient();

            _mockHttpClientFactory.CreateClient($"health-{serviceName}")
                .Returns(mockHttpClient);

            // Act
            _factory.Create(serviceName, serviceUrl);

            // Assert
            mockHttpClient.Timeout.Should().Be(TimeSpan.FromSeconds(10));
        }

        [Theory]
        [InlineData("users", "http://users-api:8080")]
        [InlineData("reports", "http://reports-api:8081")]
        [InlineData("analysis", "http://analysis-api:8082")]
        public void Create_WithDifferentServices_ShouldCreateCorrectHttpClientName(string serviceName, string serviceUrl)
        {
            // Arrange
            var mockHttpClient = new HttpClient();
            _mockHttpClientFactory.CreateClient(Arg.Any<string>()).Returns(mockHttpClient);

            // Act
            _factory.Create(serviceName, serviceUrl);

            // Assert
            _mockHttpClientFactory.Received(1).CreateClient($"health-{serviceName}");
        }
    }

    // Helper class para mockear HttpMessageHandler
    public class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode? _statusCode;
        private readonly string? _content;
        private readonly Exception? _exception;
        private readonly int _delayMs;

        public string? RequestUri { get; private set; }
        public CancellationToken CancellationTokenUsed { get; private set; }

        public MockHttpMessageHandler(HttpStatusCode statusCode, string content, int delayMs = 0)
        {
            _statusCode = statusCode;
            _content = content;
            _delayMs = delayMs;
        }

        public MockHttpMessageHandler(Exception exception)
        {
            _exception = exception;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestUri = request.RequestUri?.ToString();
            CancellationTokenUsed = cancellationToken;

            if (_delayMs > 0)
            {
                await Task.Delay(_delayMs, cancellationToken);
            }

            if (_exception != null)
            {
                throw _exception;
            }

            return new HttpResponseMessage(_statusCode!.Value)
            {
                Content = new StringContent(_content ?? string.Empty)
            };
        }
    }
}
