using Xunit;
using FluentAssertions;
using Gateway.Services;
using Gateway.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Yarp.ReverseProxy.Forwarder;
using System.Net.Http;
using NSubstitute;
using Microsoft.AspNetCore.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Text.Json;
using Microsoft.AspNetCore.Http.Features;
using System.Text;

namespace Gateway.UnitTests.Services
{
    public class RequestTranslator_ForwardAsyncExtendedTests
    {
        private readonly RequestTranslator _translator;
        private readonly ICacheService _mockCacheService;
        private readonly IMetricsService _mockMetricsService;
        private readonly IResiliencePolicyService _mockResiliencePolicyService;
        private readonly ILogger<RequestTranslator> _mockLogger;
        private readonly IHttpForwarder _mockForwarder;
        private readonly HttpMessageInvoker _httpClient;
        private readonly GateOptions _options;

        public RequestTranslator_ForwardAsyncExtendedTests()
        {
            _options = new GateOptions
            {
                EnableCaching = true,
                CacheExpirationMinutes = 5,
                DefaultTimeoutSeconds = 10,
                Services = new Dictionary<string, string>
                {
                    { "users", "http://localhost:5001" },
                    { "reports", "https://api.reports.com:8443/v1" }
                },
                AllowedRoutes = new List<AllowedRoute>
                {
                    new AllowedRoute { Service = "users", Methods = new[] { "GET", "POST" }, PathPrefix = "/api/users" },
                    new AllowedRoute { Service = "reports", Methods = new[] { "GET", "POST", "PUT" }, PathPrefix = "/api/reports" }
                }
            };
            _mockCacheService = Substitute.For<ICacheService>();
            _mockMetricsService = Substitute.For<IMetricsService>();
            _mockResiliencePolicyService = Substitute.For<IResiliencePolicyService>();
            _mockLogger = Substitute.For<ILogger<RequestTranslator>>();
            _mockForwarder = Substitute.For<IHttpForwarder>();
            _httpClient = new HttpClient();

            // Configurar el mock del ResiliencePolicyService
            _mockResiliencePolicyService.GetConfigForService(Arg.Any<string>())
                .Returns(new ResiliencePolicyConfig
                {
                    RetryCount = 3,
                    OverallTimeout = TimeSpan.FromSeconds(30),
                    CircuitBreakerThreshold = 5,
                    CircuitBreakerSamplingDuration = 60,
                    CircuitBreakerDuration = TimeSpan.FromSeconds(30)
                });

            _translator = new RequestTranslator(
                Options.Create(_options),
                _mockForwarder,
                _httpClient,
                _mockCacheService,
                _mockMetricsService,
                _mockResiliencePolicyService,
                _mockLogger
            );
        }

        [Fact]
        public async Task ForwardAsync_ShouldSetCorrelationId_WhenNotPresent()
        {
            // Arrange
            var req = new TranslateRequest { Service = "users", Method = "GET", Path = "/api/users/1" };
            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();
            _mockForwarder.SendAsync(
                Arg.Any<HttpContext>(),
                Arg.Any<string>(),
                Arg.Any<HttpMessageInvoker>(),
                Arg.Any<ForwarderRequestConfig>(),
                Arg.Any<HttpTransformer>()
            ).Returns(callInfo => new ValueTask<ForwarderError>(ForwarderError.None));

            // Act
            await _translator.ForwardAsync(context, req, CancellationToken.None);

            // Assert
            context.Request.Headers.Should().ContainKey("X-Correlation-ID");
            context.Request.Headers["X-Correlation-ID"].Should().NotBeEmpty();
        }

        [Fact]
        public async Task ForwardAsync_ShouldPreserveExistingCorrelationId()
        {
            // Arrange
            var req = new TranslateRequest
            {
                Service = "users",
                Method = "GET",
                Path = "/api/users/1",
                Headers = new Dictionary<string, string> { { "X-Correlation-ID", "test-correlation-123" } }
            };
            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();
            _mockForwarder.SendAsync(
                Arg.Any<HttpContext>(),
                Arg.Any<string>(),
                Arg.Any<HttpMessageInvoker>(),
                Arg.Any<ForwarderRequestConfig>(),
                Arg.Any<HttpTransformer>()
            ).Returns(callInfo => new ValueTask<ForwarderError>(ForwarderError.None));

            // Act
            await _translator.ForwardAsync(context, req, CancellationToken.None);

            // Assert
            context.Request.Headers["X-Correlation-ID"].ToString().Should().Be("test-correlation-123");
        }

        [Theory]
        [InlineData(ForwarderError.RequestTimedOut, 504)]
        [InlineData(ForwarderError.NoAvailableDestinations, 502)]
        [InlineData(ForwarderError.RequestBodyDestination, 502)]
        public async Task ForwardAsync_ShouldMapDifferentForwarderErrorsCorrectly(ForwarderError forwarderError, int expectedStatusCode)
        {
            // Arrange
            var req = new TranslateRequest { Service = "users", Method = "GET", Path = "/api/users/1" };
            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();
            _mockForwarder.SendAsync(
                Arg.Any<HttpContext>(),
                Arg.Any<string>(),
                Arg.Any<HttpMessageInvoker>(),
                Arg.Any<ForwarderRequestConfig>(),
                Arg.Any<HttpTransformer>()
            ).Returns(callInfo => new ValueTask<ForwarderError>(forwarderError));

            // Act
            await _translator.ForwardAsync(context, req, CancellationToken.None);

            // Assert
            context.Response.StatusCode.Should().Be(expectedStatusCode);
        }

        [Fact]
        public async Task ForwardAsync_ShouldHandlePostRequestWithBody()
        {
            // Arrange
            var req = new TranslateRequest
            {
                Service = "users",
                Method = "POST",
                Path = "/api/users",
                Body = new { name = "Test User", email = "test@example.com" }
            };
            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();
            _mockForwarder.SendAsync(
                Arg.Any<HttpContext>(),
                Arg.Any<string>(),
                Arg.Any<HttpMessageInvoker>(),
                Arg.Any<ForwarderRequestConfig>(),
                Arg.Any<HttpTransformer>()
            ).Returns(callInfo => new ValueTask<ForwarderError>(ForwarderError.None));

            // Act
            await _translator.ForwardAsync(context, req, CancellationToken.None);

            // Assert
            context.Request.ContentType.Should().Be("application/json");
            context.Request.ContentLength.Should().BeGreaterThan(0);
        }

        [Fact]
        public async Task ForwardAsync_ShouldHandleGetRequestWithoutBody()
        {
            // Arrange
            var req = new TranslateRequest { Service = "users", Method = "GET", Path = "/api/users" };
            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();
            _mockForwarder.SendAsync(
                Arg.Any<HttpContext>(),
                Arg.Any<string>(),
                Arg.Any<HttpMessageInvoker>(),
                Arg.Any<ForwarderRequestConfig>(),
                Arg.Any<HttpTransformer>()
            ).Returns(callInfo => new ValueTask<ForwarderError>(ForwarderError.None));

            // Act
            await _translator.ForwardAsync(context, req, CancellationToken.None);

            // Assert
            context.Request.ContentLength.Should().Be(0);
            context.Request.Body.Should().BeSameAs(Stream.Null);
        }

        [Fact]
        public async Task ForwardAsync_ShouldFilterForbiddenHeaders()
        {
            // Arrange
            var req = new TranslateRequest
            {
                Service = "users",
                Method = "GET",
                Path = "/api/users/1",
                Headers = new Dictionary<string, string>
                {
                    { "Authorization", "Bearer token123" },
                    { "Host", "malicious-host.com" }, // Should be filtered
                    { "Content-Length", "999" }, // Should be filtered
                    { "X-Custom-Header", "allowed-value" }
                }
            };
            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();
            _mockForwarder.SendAsync(
                Arg.Any<HttpContext>(),
                Arg.Any<string>(),
                Arg.Any<HttpMessageInvoker>(),
                Arg.Any<ForwarderRequestConfig>(),
                Arg.Any<HttpTransformer>()
            ).Returns(callInfo => new ValueTask<ForwarderError>(ForwarderError.None));

            // Act
            await _translator.ForwardAsync(context, req, CancellationToken.None);

            // Assert
            context.Request.Headers.Should().ContainKey("Authorization");
            context.Request.Headers.Should().ContainKey("X-Custom-Header");
            context.Request.Headers.Should().NotContainKey("Host"); // Filtered out
        }

        [Fact]
        public async Task ForwardAsync_ShouldIncludeErrorHeadersInResponse()
        {
            // Arrange
            var req = new TranslateRequest { Service = "users", Method = "GET", Path = "/api/users/1" };
            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();
            context.Request.Headers["X-Correlation-ID"] = "test-correlation-456";

            _mockForwarder.SendAsync(
                Arg.Any<HttpContext>(),
                Arg.Any<string>(),
                Arg.Any<HttpMessageInvoker>(),
                Arg.Any<ForwarderRequestConfig>(),
                Arg.Any<HttpTransformer>()
            ).Returns(callInfo => new ValueTask<ForwarderError>(ForwarderError.Request));

            // Act
            await _translator.ForwardAsync(context, req, CancellationToken.None);
            context.Response.Body.Seek(0, SeekOrigin.Begin);
            var responseBody = await new StreamReader(context.Response.Body).ReadToEndAsync();

            // Assert
            context.Response.StatusCode.Should().Be(400);
            var errorResponse = JsonSerializer.Deserialize<JsonElement>(responseBody);
            errorResponse.GetProperty("error").GetProperty("message").GetString().Should().Contain("Gateway forwarding error");
        }

        [Fact]
        public async Task ForwardAsync_ShouldRecordMetricsForSuccessfulRequest()
        {
            // Arrange
            var req = new TranslateRequest { Service = "users", Method = "GET", Path = "/api/users/1" };
            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();
            context.Response.StatusCode = 200;
            _mockForwarder.SendAsync(
                Arg.Any<HttpContext>(),
                Arg.Any<string>(),
                Arg.Any<HttpMessageInvoker>(),
                Arg.Any<ForwarderRequestConfig>(),
                Arg.Any<HttpTransformer>()
            ).Returns(callInfo => new ValueTask<ForwarderError>(ForwarderError.None));

            // Act
            await _translator.ForwardAsync(context, req, CancellationToken.None);

            // Assert
            _mockMetricsService.Received(1).RecordRequest(
                req.Service,
                req.Method,
                200,
                Arg.Any<double>()
            );
        }

        [Fact]
        public async Task ForwardAsync_ShouldRecordMetricsForErrorRequest()
        {
            // Arrange
            var req = new TranslateRequest { Service = "users", Method = "GET", Path = "/api/users/1" };
            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();
            _mockForwarder.SendAsync(
                Arg.Any<HttpContext>(),
                Arg.Any<string>(),
                Arg.Any<HttpMessageInvoker>(),
                Arg.Any<ForwarderRequestConfig>(),
                Arg.Any<HttpTransformer>()
            ).Returns(callInfo => new ValueTask<ForwarderError>(ForwarderError.RequestTimedOut));

            // Act
            await _translator.ForwardAsync(context, req, CancellationToken.None);

            // Assert
            _mockMetricsService.Received(1).RecordRequest(
                req.Service,
                req.Method,
                504, // GatewayTimeout
                Arg.Any<double>()
            );
        }
    }
}
