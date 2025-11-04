using Xunit;
using NSubstitute;
using Gateway.Models;
using System.Net.Http;
using FluentAssertions;
using Gateway.Services;
using System.Threading;
using System.Text.Json;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Yarp.ReverseProxy.Forwarder;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Gateway.UnitTests.Services
{
    public class RequestTranslator_ForwardRequestAsyncTests
    {
        private readonly RequestTranslator _translator;
        private readonly ICacheService _mockCacheService;
        private readonly IMetricsService _mockMetricsService;
        private readonly ILogger<RequestTranslator> _mockLogger;
        private readonly IHttpForwarder _mockForwarder;
        private readonly HttpMessageInvoker _httpClient;
        private readonly GateOptions _options;

        public RequestTranslator_ForwardRequestAsyncTests()
        {
            _options = new GateOptions
            {
                EnableCaching = true,
                CacheExpirationMinutes = 5,
                DefaultTimeoutSeconds = 10,
                Services = new Dictionary<string, string> { { "users", "http://localhost:5001" } },
                AllowedRoutes = new List<AllowedRoute> { new AllowedRoute { Service = "users", Methods = new[] { "GET" }, PathPrefix = "/api/users" } }
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
        public async Task ForwardRequestAsync_ShouldReturnError_WhenForwarderReturnsError()
        {
            // Arrange
            var req = new TranslateRequest { Service = "users", Method = "GET", Path = "/api/users/1" };
            var context = new DefaultHttpContext();
            _mockForwarder.SendAsync(
                Arg.Any<HttpContext>(),
                Arg.Any<string>(),
                Arg.Any<HttpMessageInvoker>(),
                Arg.Any<ForwarderRequestConfig>(),
                Arg.Any<HttpTransformer>()
            ).Returns(callInfo => new ValueTask<ForwarderError>(ForwarderError.Request));

            // Act
            var result = await InvokeForwardRequestAsync(context, req);

            // Assert
            result.Error.Should().NotBeNull();
            result.Error!.Message.Should().Contain("Gateway forwarding error");
            result.Error.StatusCode.Should().Be(502); // BadGateway - error de conexión al backend
        }

        [Fact]
        public async Task ForwardRequestAsync_ShouldReturnSuccess_WhenForwarderReturnsNone()
        {
            // Arrange
            var req = new TranslateRequest { Service = "users", Method = "GET", Path = "/api/users/1" };
            var context = new DefaultHttpContext();
            _mockForwarder.SendAsync(
                Arg.Any<HttpContext>(),
                Arg.Any<string>(),
                Arg.Any<HttpMessageInvoker>(),
                Arg.Any<ForwarderRequestConfig>(),
                Arg.Any<HttpTransformer>()
            ).Returns(callInfo => new ValueTask<ForwarderError>(ForwarderError.None));
            context.Response.StatusCode = 200;

            // Act
            var result = await InvokeForwardRequestAsync(context, req);

            // Assert
            result.Response.Should().NotBeNull();
            result.Response!.StatusCode.Should().Be(200);
        }

        private async Task<TranslateResult> InvokeForwardRequestAsync(HttpContext context, TranslateRequest req)
        {
            // Usar reflexión para invocar el método privado
            var method = typeof(RequestTranslator).GetMethod("ForwardRequestAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            return await (Task<TranslateResult>)method!.Invoke(_translator, new object[] { context, req, CancellationToken.None })!;
        }
    }
}
