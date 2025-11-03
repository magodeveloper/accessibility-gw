using Xunit;
using NSubstitute;
using Gateway.Models;
using System.Net.Http;
using System.Threading;
using Gateway.Services;
using FluentAssertions;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Yarp.ReverseProxy.Forwarder;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Gateway.UnitTests.Services
{
    public class RequestTranslatorTests_ProcessRequest
    {
        private readonly RequestTranslator _translator;
        private readonly ICacheService _mockCacheService;
        private readonly IMetricsService _mockMetricsService;
        private readonly ILogger<RequestTranslator> _mockLogger;
        private readonly IHttpForwarder _mockForwarder;
        private readonly HttpMessageInvoker _httpClient;
        private readonly GateOptions _options;

        public RequestTranslatorTests_ProcessRequest()
        {
            _options = new GateOptions
            {
                EnableCaching = true,
                CacheExpirationMinutes = 5,
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
                _mockLogger,
                Substitute.For<IHostEnvironment>()
            );
        }

        [Fact]
        public async Task ProcessRequestAsync_ShouldReturnCachedResult_WhenCacheHit()
        {
            // Arrange
            var req = new TranslateRequest { Service = "users", Method = "GET", Path = "/api/users/1" };
            var context = new DefaultHttpContext();
            var cachedResponse = new TranslateResponse { StatusCode = 200, FromCache = false };
            var cachedResult = new TranslateResult { Response = cachedResponse };
            _mockCacheService.GetAsync<TranslateResult>(Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<TranslateResult?>(cachedResult));

            // Act
            var result = await _translator.ProcessRequestAsync(context, req, CancellationToken.None);

            // Assert
            result.Response.Should().NotBeNull();
            result.Response!.FromCache.Should().BeTrue();
            await _mockCacheService.Received(1).GetAsync<TranslateResult>(Arg.Any<string>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ProcessRequestAsync_ShouldCallForward_WhenCacheMiss()
        {
            // Arrange
            var req = new TranslateRequest { Service = "users", Method = "GET", Path = "/api/users/2" };
            var context = new DefaultHttpContext();
            _mockCacheService.GetAsync<TranslateResult>(Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<TranslateResult?>(null));
            _mockCacheService.GenerateCacheKey(Arg.Any<TranslateRequest>()).Returns("cache-key");
            _mockCacheService.SetAsync(Arg.Any<string>(), Arg.Any<TranslateResult>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
                .Returns(Task.CompletedTask);
            _mockMetricsService.StartActivity(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
                .Returns((Activity?)null);
            // No es necesario configurar RecordRequest si no se verifica comportamiento
            // Simular ForwardRequestAsync devolviendo una respuesta exitosa
            // Como no podemos mockear el método privado, configuramos el contexto para que la llamada real devuelva éxito
            context.Response.StatusCode = 200;

            // Act
            var result = await _translator.ProcessRequestAsync(context, req, CancellationToken.None);

            // Assert
            result.Response.Should().NotBeNull();
            await _mockCacheService.Received(1).SetAsync(Arg.Any<string>(), Arg.Any<TranslateResult>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ProcessRequestAsync_ShouldReturnError_WhenExceptionThrown()
        {
            // Arrange
            var req = new TranslateRequest { Service = "users", Method = "GET", Path = "/api/users/3" };
            var context = new DefaultHttpContext();
            _mockCacheService.GetAsync<TranslateResult>(Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromException<TranslateResult?>(new Exception("Test exception")));
            _mockMetricsService.StartActivity(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
                .Returns((Activity?)null);
            // No es necesario configurar RecordRequest si no se verifica comportamiento

            // Act
            var result = await _translator.ProcessRequestAsync(context, req, CancellationToken.None);

            // Assert
            result.Error.Should().NotBeNull();
            result.Error!.StatusCode.Should().Be(500);
            result.Error.Message.Should().Be("Internal gateway error");
        }
    }
}
