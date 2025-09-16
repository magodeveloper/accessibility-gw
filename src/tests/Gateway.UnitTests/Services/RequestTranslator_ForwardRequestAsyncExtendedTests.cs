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
    public class RequestTranslator_ForwardRequestAsyncExtendedTests
    {
        private readonly RequestTranslator _translator;
        private readonly ICacheService _mockCacheService;
        private readonly IMetricsService _mockMetricsService;
        private readonly ILogger<RequestTranslator> _mockLogger;
        private readonly IHttpForwarder _mockForwarder;
        private readonly HttpMessageInvoker _httpClient;
        private readonly GateOptions _options;

        public RequestTranslator_ForwardRequestAsyncExtendedTests()
        {
            _options = new GateOptions
            {
                EnableCaching = true,
                CacheExpirationMinutes = 5,
                DefaultTimeoutSeconds = 10,
                Services = new Dictionary<string, string>
                {
                    { "users", "http://localhost:5001/api" },
                    { "reports", "https://api.reports.com:8443/v1" },
                    { "analysis", "http://analysis-service.local:3000" }
                },
                AllowedRoutes = new List<AllowedRoute>
                {
                    new AllowedRoute { Service = "users", Methods = new[] { "GET", "POST", "PUT", "DELETE" }, PathPrefix = "/api/users" },
                    new AllowedRoute { Service = "reports", Methods = new[] { "GET", "POST" }, PathPrefix = "/api/reports" },
                    new AllowedRoute { Service = "analysis", Methods = new[] { "POST" }, PathPrefix = "/api/analyze" }
                }
            };
            _mockCacheService = Substitute.For<ICacheService>();
            _mockMetricsService = Substitute.For<IMetricsService>();
            _mockLogger = Substitute.For<ILogger<RequestTranslator>>();
            _mockForwarder = Substitute.For<IHttpForwarder>();
            _httpClient = new HttpClient();
            _translator = new RequestTranslator(
                Options.Create(_options),
                _mockForwarder,
                _httpClient,
                _mockCacheService,
                _mockMetricsService,
                Substitute.For<IResiliencePolicyService>(),
                _mockLogger
            );
        }

        [Fact]
        public async Task ForwardRequestAsync_ShouldBuildCorrectTargetUrl_WithQueryParameters()
        {
            // Arrange
            var req = new TranslateRequest
            {
                Service = "users",
                Method = "GET",
                Path = "/api/users/search",
                Query = new Dictionary<string, string>
                {
                    { "q", "john" },
                    { "page", "1" },
                    { "limit", "10" }
                }
            };
            var context = new DefaultHttpContext();
            string? capturedUrl = null;

            _mockForwarder.SendAsync(
                Arg.Any<HttpContext>(),
                Arg.Do<string>(url => capturedUrl = url),
                Arg.Any<HttpMessageInvoker>(),
                Arg.Any<ForwarderRequestConfig>(),
                Arg.Any<HttpTransformer>()
            ).Returns(callInfo => new ValueTask<ForwarderError>(ForwarderError.None));

            // Act
            var result = await InvokeForwardRequestAsync(context, req);

            // Assert
            capturedUrl.Should().NotBeNull();
            capturedUrl!.Should().Contain("q=john");
            capturedUrl!.Should().Contain("page=1");
            capturedUrl!.Should().Contain("limit=10");
        }

        [Fact]
        public async Task ForwardRequestAsync_ShouldBuildCorrectTargetUrl_WithoutQueryParameters()
        {
            // Arrange
            var req = new TranslateRequest
            {
                Service = "users",
                Method = "GET",
                Path = "/api/users/123"
            };
            var context = new DefaultHttpContext();
            string? capturedUrl = null;

            _mockForwarder.SendAsync(
                Arg.Any<HttpContext>(),
                Arg.Do<string>(url => capturedUrl = url),
                Arg.Any<HttpMessageInvoker>(),
                Arg.Any<ForwarderRequestConfig>(),
                Arg.Any<HttpTransformer>()
            ).Returns(callInfo => new ValueTask<ForwarderError>(ForwarderError.None));

            // Act
            var result = await InvokeForwardRequestAsync(context, req);

            // Assert
            capturedUrl.Should().NotBeNull();
            capturedUrl!.Should().Be("http://localhost:5001/api/api/users/123");
        }

        [Fact]
        public async Task ForwardRequestAsync_ShouldHandleHttpsService()
        {
            // Arrange
            var req = new TranslateRequest
            {
                Service = "reports",
                Method = "GET",
                Path = "/api/reports/monthly"
            };
            var context = new DefaultHttpContext();
            string? capturedUrl = null;

            _mockForwarder.SendAsync(
                Arg.Any<HttpContext>(),
                Arg.Do<string>(url => capturedUrl = url),
                Arg.Any<HttpMessageInvoker>(),
                Arg.Any<ForwarderRequestConfig>(),
                Arg.Any<HttpTransformer>()
            ).Returns(callInfo => new ValueTask<ForwarderError>(ForwarderError.None));

            // Act
            var result = await InvokeForwardRequestAsync(context, req);

            // Assert
            capturedUrl.Should().NotBeNull();
            capturedUrl!.Should().StartWith("https://api.reports.com:8443");
        }

        [Fact]
        public async Task ForwardRequestAsync_ShouldSetCorrectRequestMethod()
        {
            // Arrange
            var req = new TranslateRequest
            {
                Service = "users",
                Method = "DELETE",
                Path = "/api/users/123"
            };
            var context = new DefaultHttpContext();
            var httpRequestFeature = new HttpRequestFeature();
            context.Features.Set<IHttpRequestFeature>(httpRequestFeature);

            _mockForwarder.SendAsync(
                Arg.Any<HttpContext>(),
                Arg.Any<string>(),
                Arg.Any<HttpMessageInvoker>(),
                Arg.Any<ForwarderRequestConfig>(),
                Arg.Any<HttpTransformer>()
            ).Returns(callInfo => new ValueTask<ForwarderError>(ForwarderError.None));

            // Act
            var result = await InvokeForwardRequestAsync(context, req);

            // Assert
            httpRequestFeature.Method.Should().Be("DELETE");
        }

        [Fact]
        public async Task ForwardRequestAsync_ShouldSetJsonBodyForPostRequest()
        {
            // Arrange
            var testBody = new { name = "Test User", age = 25 };
            var req = new TranslateRequest
            {
                Service = "users",
                Method = "POST",
                Path = "/api/users",
                Body = testBody
            };
            var context = new DefaultHttpContext();

            _mockForwarder.SendAsync(
                Arg.Any<HttpContext>(),
                Arg.Any<string>(),
                Arg.Any<HttpMessageInvoker>(),
                Arg.Any<ForwarderRequestConfig>(),
                Arg.Any<HttpTransformer>()
            ).Returns(callInfo => new ValueTask<ForwarderError>(ForwarderError.None));

            // Act
            var result = await InvokeForwardRequestAsync(context, req);

            // Assert
            context.Request.ContentType.Should().Be("application/json");
            context.Request.ContentLength.Should().BeGreaterThan(0);

            // Verificar que el body contiene JSON serializado
            context.Request.Body.Seek(0, SeekOrigin.Begin);
            var bodyContent = await new StreamReader(context.Request.Body).ReadToEndAsync();
            bodyContent.Should().Contain("Test User");
            bodyContent.Should().Contain("25");
        }

        [Fact]
        public async Task ForwardRequestAsync_ShouldClearBodyForGetRequest()
        {
            // Arrange
            var req = new TranslateRequest
            {
                Service = "users",
                Method = "GET",
                Path = "/api/users"
            };
            var context = new DefaultHttpContext();

            _mockForwarder.SendAsync(
                Arg.Any<HttpContext>(),
                Arg.Any<string>(),
                Arg.Any<HttpMessageInvoker>(),
                Arg.Any<ForwarderRequestConfig>(),
                Arg.Any<HttpTransformer>()
            ).Returns(callInfo => new ValueTask<ForwarderError>(ForwarderError.None));

            // Act
            var result = await InvokeForwardRequestAsync(context, req);

            // Assert
            context.Request.Body.Should().BeSameAs(Stream.Null);
            context.Request.ContentLength.Should().Be(0);
        }

        [Fact]
        public async Task ForwardRequestAsync_ShouldReturnSuccessResponse_WithHeaders()
        {
            // Arrange
            var req = new TranslateRequest { Service = "users", Method = "GET", Path = "/api/users/1" };
            var context = new DefaultHttpContext();
            context.Response.StatusCode = 200;
            context.Response.Headers["X-Custom-Header"] = "custom-value";
            context.Response.Headers["Content-Type"] = "application/json";

            _mockForwarder.SendAsync(
                Arg.Any<HttpContext>(),
                Arg.Any<string>(),
                Arg.Any<HttpMessageInvoker>(),
                Arg.Any<ForwarderRequestConfig>(),
                Arg.Any<HttpTransformer>()
            ).Returns(callInfo => new ValueTask<ForwarderError>(ForwarderError.None));

            // Act
            var result = await InvokeForwardRequestAsync(context, req);

            // Assert
            result.Response!.Should().NotBeNull();
            result.Response!.StatusCode.Should().Be(200);
            result.Response!.Headers.Should().ContainKey("X-Custom-Header");
            result.Response!.Headers.Should().ContainKey("Content-Type");
            result.Response!.Headers!["X-Custom-Header"]!.Should().Be("custom-value");
        }

        [Fact]
        public async Task ForwardRequestAsync_ShouldIncludeCorrelationIdInError()
        {
            // Arrange
            var req = new TranslateRequest { Service = "users", Method = "GET", Path = "/api/users/1" };
            var context = new DefaultHttpContext();
            context.Request.Headers["X-Correlation-ID"] = "test-correlation-789";

            _mockForwarder.SendAsync(
                Arg.Any<HttpContext>(),
                Arg.Any<string>(),
                Arg.Any<HttpMessageInvoker>(),
                Arg.Any<ForwarderRequestConfig>(),
                Arg.Any<HttpTransformer>()
            ).Returns(callInfo => new ValueTask<ForwarderError>(ForwarderError.NoAvailableDestinations));

            // Act
            var result = await InvokeForwardRequestAsync(context, req);

            // Assert
            result.Error.Should().NotBeNull();
            result.Error!.CorrelationId.Should().Be("test-correlation-789");
        }

        [Theory]
        [InlineData("PUT")]
        [InlineData("PATCH")]
        public async Task ForwardRequestAsync_ShouldHandleBodyMethodsCorrectly(string method)
        {
            // Arrange
            var testBody = new { data = "test-data", id = 42 };
            var req = new TranslateRequest
            {
                Service = "users",
                Method = method,
                Path = "/api/users/42",
                Body = testBody
            };
            var context = new DefaultHttpContext();
            var httpRequestFeature = new HttpRequestFeature();
            context.Features.Set<IHttpRequestFeature>(httpRequestFeature);

            _mockForwarder.SendAsync(
                Arg.Any<HttpContext>(),
                Arg.Any<string>(),
                Arg.Any<HttpMessageInvoker>(),
                Arg.Any<ForwarderRequestConfig>(),
                Arg.Any<HttpTransformer>()
            ).Returns(callInfo => new ValueTask<ForwarderError>(ForwarderError.None));

            // Act
            var result = await InvokeForwardRequestAsync(context, req);

            // Assert
            httpRequestFeature.Method.Should().Be(method);
            context.Request.ContentType.Should().Be("application/json");
            context.Request.ContentLength.Should().BeGreaterThan(0);
        }

        private async Task<TranslateResult> InvokeForwardRequestAsync(HttpContext context, TranslateRequest req)
        {
            // Usar reflexión para invocar el método privado
            var method = typeof(RequestTranslator).GetMethod("ForwardRequestAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var result = method!.Invoke(_translator, new object[] { context, req, CancellationToken.None });
            return await (Task<TranslateResult>)result!;
        }
    }
}
