using Xunit;
using NSubstitute;
using Gateway.Models;
using System.Net.Http;
using FluentAssertions;
using Gateway.Services;
using System.Text.Json;
using System.Threading;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Yarp.ReverseProxy.Forwarder;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Gateway.UnitTests.Services
{
    public class RequestTranslatorTests_EdgeCases
    {
        private readonly RequestTranslator _translator;
        private readonly ICacheService _mockCacheService;
        private readonly IMetricsService _mockMetricsService;
        private readonly IResiliencePolicyService _mockResiliencePolicyService;
        private readonly ILogger<RequestTranslator> _mockLogger;
        private readonly IHttpForwarder _mockForwarder;
        private readonly HttpMessageInvoker _httpClient;
        private readonly GateOptions _options;

        public RequestTranslatorTests_EdgeCases()
        {
            _options = new GateOptions
            {
                EnableCaching = true,
                CacheExpirationMinutes = 5,
                DefaultTimeoutSeconds = 10,
                Services = new Dictionary<string, string>
                {
                    { "users", "http://localhost:5001/" }, // URL with trailing slash
                    { "reports", "https://api.reports.com" } // URL without trailing slash
                },
                AllowedRoutes = new List<AllowedRoute>
                {
                    new AllowedRoute { Service = "users", Methods = new[] { "GET", "POST" }, PathPrefix = "/api/users" },
                    new AllowedRoute { Service = "reports", Methods = new[] { "GET" }, PathPrefix = "/api/reports" }
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
                _mockLogger,
                Substitute.For<IHostEnvironment>()
            );
        }

        [Fact]
        public async Task ProcessRequestAsync_ShouldHandleCacheException_GracefullyFallback()
        {
            // Arrange
            var req = new TranslateRequest { Service = "users", Method = "GET", Path = "/api/users/1", UseCache = true };
            var context = new DefaultHttpContext();

            // Simular excepción en caché
            _mockCacheService.GetAsync<TranslateResult>(Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns<TranslateResult?>(callInfo => throw new Exception("Cache connection failed"));

            _mockForwarder.SendAsync(
                Arg.Any<HttpContext>(),
                Arg.Any<string>(),
                Arg.Any<HttpMessageInvoker>(),
                Arg.Any<ForwarderRequestConfig>(),
                Arg.Any<HttpTransformer>()
            ).Returns(callInfo => new ValueTask<ForwarderError>(ForwarderError.None));

            context.Response.StatusCode = 200;

            // Act
            var result = await _translator.ProcessRequestAsync(context, req, CancellationToken.None);

            // Assert
            result.Error.Should().NotBeNull();
            result.Error!.StatusCode.Should().Be(500);
            result.Error.Message.Should().Be("Internal gateway error");
        }

        [Fact]
        public async Task ProcessRequestAsync_ShouldNotCache_WhenUseCacheIsFalse()
        {
            // Arrange
            var req = new TranslateRequest { Service = "users", Method = "GET", Path = "/api/users/1", UseCache = false };
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
            var result = await _translator.ProcessRequestAsync(context, req, CancellationToken.None);

            // Assert
            // No debería intentar leer del caché
            _ = _mockCacheService.DidNotReceive().GetAsync<TranslateResult>(Arg.Any<string>(), Arg.Any<CancellationToken>());
            // No debería guardar en caché
            _ = _mockCacheService.DidNotReceive().SetAsync(Arg.Any<string>(), Arg.Any<TranslateResult>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ProcessRequestAsync_ShouldNotCache_Non2xxResponses()
        {
            // Arrange
            var req = new TranslateRequest { Service = "users", Method = "GET", Path = "/api/users/1", UseCache = true };
            var context = new DefaultHttpContext();

            _mockForwarder.SendAsync(
                Arg.Any<HttpContext>(),
                Arg.Any<string>(),
                Arg.Any<HttpMessageInvoker>(),
                Arg.Any<ForwarderRequestConfig>(),
                Arg.Any<HttpTransformer>()
            ).Returns(callInfo => new ValueTask<ForwarderError>(ForwarderError.None));

            context.Response.StatusCode = 404; // Error response

            // Act
            var result = await _translator.ProcessRequestAsync(context, req, CancellationToken.None);

            // Assert
            _ = _mockCacheService.DidNotReceive().SetAsync(Arg.Any<string>(), Arg.Any<TranslateResult>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ProcessRequestAsync_ShouldUseCustomCacheExpiration()
        {
            // Arrange
            var req = new TranslateRequest
            {
                Service = "users",
                Method = "GET",
                Path = "/api/users/1",
                UseCache = true,
                CacheExpirationMinutes = 15 // Custom expiration
            };
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
            var result = await _translator.ProcessRequestAsync(context, req, CancellationToken.None);

            // Assert
            _ = _mockCacheService.Received(1).SetAsync(
                Arg.Any<string>(),
                Arg.Any<TranslateResult>(),
                TimeSpan.FromMinutes(15), // Custom expiration
                Arg.Any<CancellationToken>()
            );
        }

        [Fact]
        public void IsAllowed_ShouldRejectInvalidMethods()
        {
            // Arrange
            var req = new TranslateRequest { Service = "users", Method = "INVALID", Path = "/api/users/1" };

            // Act
            var result = _translator.IsAllowed(req);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void IsAllowed_ShouldRejectUnknownService()
        {
            // Arrange
            var req = new TranslateRequest { Service = "unknown", Method = "GET", Path = "/api/users/1" };

            // Act
            var result = _translator.IsAllowed(req);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void IsAllowed_ShouldRejectUnauthorizedPath()
        {
            // Arrange
            var req = new TranslateRequest { Service = "users", Method = "GET", Path = "/api/unauthorized" };

            // Act
            var result = _translator.IsAllowed(req);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void IsAllowed_ShouldRejectUnauthorizedMethod()
        {
            // Arrange
            var req = new TranslateRequest { Service = "users", Method = "DELETE", Path = "/api/users/1" };

            // Act
            var result = _translator.IsAllowed(req);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task ProcessRequestAsync_ShouldHandleCancellation()
        {
            // Arrange
            var req = new TranslateRequest { Service = "users", Method = "GET", Path = "/api/users/1" };
            var context = new DefaultHttpContext();
            var cts = new CancellationTokenSource();
            cts.Cancel(); // Cancelar inmediatamente

            _mockForwarder.SendAsync(
                Arg.Any<HttpContext>(),
                Arg.Any<string>(),
                Arg.Any<HttpMessageInvoker>(),
                Arg.Any<ForwarderRequestConfig>(),
                Arg.Any<HttpTransformer>()
            ).Returns<ValueTask<ForwarderError>>(callInfo => throw new OperationCanceledException());

            // Act
            var result = await _translator.ProcessRequestAsync(context, req, cts.Token);

            // Assert
            result.Error.Should().NotBeNull();
            result.Error!.StatusCode.Should().Be(500);
        }

        [Fact]
        public async Task ForwardAsync_ShouldHandleNullBody()
        {
            // Arrange
            var req = new TranslateRequest
            {
                Service = "users",
                Method = "POST",
                Path = "/api/users",
                Body = null // Null body
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
            // No debería arrojar excepción
            context.Response.StatusCode.Should().Be(200);
        }
    }
}
