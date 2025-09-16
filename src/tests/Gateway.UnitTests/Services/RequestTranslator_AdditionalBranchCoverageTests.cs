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
using Microsoft.AspNetCore.Http.Features;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Collections.Generic;

namespace Gateway.UnitTests.Services
{
    /// <summary>
    /// Tests adicionales para mejorar cobertura de ramas en RequestTranslator
    /// Enfocado en cubrir condiciones específicas no cubiertas anteriormente
    /// </summary>
    public class RequestTranslator_AdditionalBranchCoverageTests
    {
        private readonly RequestTranslator _translator;
        private readonly ICacheService _mockCacheService;
        private readonly IMetricsService _mockMetricsService;
        private readonly IResiliencePolicyService _mockResiliencePolicyService;
        private readonly ILogger<RequestTranslator> _mockLogger;
        private readonly IHttpForwarder _mockForwarder;
        private readonly HttpMessageInvoker _httpClient;
        private readonly GateOptions _options;

        public RequestTranslator_AdditionalBranchCoverageTests()
        {
            _options = new GateOptions
            {
                EnableCaching = true,
                CacheExpirationMinutes = 5,
                DefaultTimeoutSeconds = 30,
                Services = new Dictionary<string, string>
                {
                    { "users", "http://localhost:5001" },
                    { "reports", "http://localhost:5002" }
                },
                AllowedRoutes = new List<AllowedRoute>
                {
                    new AllowedRoute { Service = "users", Methods = new[] { "GET", "POST", "PUT", "DELETE" }, PathPrefix = "/api/users" },
                    new AllowedRoute { Service = "reports", Methods = new[] { "GET", "POST" }, PathPrefix = "/api/reports" }
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

        #region Cache Branch Coverage Tests

        [Fact]
        public async Task ProcessRequestAsync_POST_ShouldSkipCacheCheck()
        {
            // Arrange
            var req = new TranslateRequest { Service = "users", Method = "POST", Path = "/api/users", Body = new { name = "test" } };
            var context = new DefaultHttpContext();
            _mockMetricsService.StartActivity(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
                .Returns((Activity?)null);

            // Act
            var result = await _translator.ProcessRequestAsync(context, req, CancellationToken.None);

            // Assert - No debe llamar a cache para métodos POST
            await _mockCacheService.DidNotReceive().GetAsync<TranslateResult>(Arg.Any<string>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ProcessRequestAsync_GET_WithCachingDisabled_ShouldSkipCache()
        {
            // Arrange
            var disabledCacheOptions = new GateOptions
            {
                EnableCaching = false, // Cache deshabilitado
                Services = _options.Services,
                AllowedRoutes = _options.AllowedRoutes
            };
            var translatorNoCaching = new RequestTranslator(
                Options.Create(disabledCacheOptions),
                _mockForwarder,
                _httpClient,
                _mockCacheService,
                _mockMetricsService,
                _mockResiliencePolicyService,
                _mockLogger
            );

            var req = new TranslateRequest { Service = "users", Method = "GET", Path = "/api/users/1" };
            var context = new DefaultHttpContext();
            _mockMetricsService.StartActivity(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
                .Returns((Activity?)null);

            // Act
            var result = await translatorNoCaching.ProcessRequestAsync(context, req, CancellationToken.None);

            // Assert
            await _mockCacheService.DidNotReceive().GetAsync<TranslateResult>(Arg.Any<string>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ProcessRequestAsync_GET_WithUseCacheFalse_ShouldSkipCache()
        {
            // Arrange
            var req = new TranslateRequest
            {
                Service = "users",
                Method = "GET",
                Path = "/api/users/1",
                UseCache = false // Explícitamente deshabilitado en la request
            };
            var context = new DefaultHttpContext();
            _mockMetricsService.StartActivity(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
                .Returns((Activity?)null);

            // Act
            var result = await _translator.ProcessRequestAsync(context, req, CancellationToken.None);

            // Assert
            await _mockCacheService.DidNotReceive().GetAsync<TranslateResult>(Arg.Any<string>(), Arg.Any<CancellationToken>());
        }

        #endregion

        #region Cache Storage Branch Coverage Tests

        [Fact]
        public async Task ProcessRequestAsync_GET_WithNon2xxResponse_ShouldNotCache()
        {
            // Arrange
            var req = new TranslateRequest { Service = "users", Method = "GET", Path = "/api/users/404" };
            var context = new DefaultHttpContext();

            _mockCacheService.GetAsync<TranslateResult>(Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<TranslateResult?>(null)); // Cache miss
            _mockCacheService.GenerateCacheKey(Arg.Any<TranslateRequest>()).Returns("cache-key");
            _mockMetricsService.StartActivity(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
                .Returns((Activity?)null);

            // Simular respuesta 404 (no cacheable)
            context.Response.StatusCode = 404;

            // Act
            var result = await _translator.ProcessRequestAsync(context, req, CancellationToken.None);

            // Assert - No debe guardar en cache respuestas de error
            await _mockCacheService.DidNotReceive().SetAsync(Arg.Any<string>(), Arg.Any<TranslateResult>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ProcessRequestAsync_GET_WithSuccessResponse_ShouldCache()
        {
            // Arrange
            var req = new TranslateRequest { Service = "users", Method = "GET", Path = "/api/users/200" };
            var context = new DefaultHttpContext();

            _mockCacheService.GetAsync<TranslateResult>(Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<TranslateResult?>(null)); // Cache miss
            _mockCacheService.GenerateCacheKey(Arg.Any<TranslateRequest>()).Returns("cache-key");
            _mockCacheService.SetAsync(Arg.Any<string>(), Arg.Any<TranslateResult>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
                .Returns(Task.CompletedTask);
            _mockMetricsService.StartActivity(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
                .Returns((Activity?)null);

            // Simular respuesta 200 (cacheable)
            context.Response.StatusCode = 200;

            // Act
            var result = await _translator.ProcessRequestAsync(context, req, CancellationToken.None);

            // Assert - Debe guardar en cache respuestas exitosas
            await _mockCacheService.Received(1).SetAsync(Arg.Any<string>(), Arg.Any<TranslateResult>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>());
        }

        #endregion

        #region Error Handling Branch Coverage Tests

        [Fact]
        public async Task ProcessRequestAsync_WithForwardingError_ShouldReturnErrorResult()
        {
            // Arrange
            var req = new TranslateRequest { Service = "users", Method = "POST", Path = "/api/users/error" };
            var context = new DefaultHttpContext();

            _mockMetricsService.StartActivity(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
                .Returns((Activity?)null);

            // Simular error en forwarding configurando forwarder para devolver error
            _mockForwarder.SendAsync(
                Arg.Any<HttpContext>(),
                Arg.Any<string>(),
                Arg.Any<HttpMessageInvoker>(),
                Arg.Any<ForwarderRequestConfig>(),
                Arg.Any<HttpTransformer>())
                .Returns(ForwarderError.RequestTimedOut);

            // Act
            var result = await _translator.ProcessRequestAsync(context, req, CancellationToken.None);

            // Assert
            result.Success.Should().BeFalse();
            result.Error.Should().NotBeNull();
            result.Error!.StatusCode.Should().Be(504); // Gateway Timeout
        }

        [Fact]
        public async Task ProcessRequestAsync_WithDifferentForwarderErrors_ShouldReturnCorrectStatusCodes()
        {
            // Test múltiples tipos de errores del forwarder
            var testCases = new[]
            {
                new { Error = ForwarderError.Request, ExpectedStatus = 400 },
                new { Error = ForwarderError.RequestTimedOut, ExpectedStatus = 504 },
                new { Error = ForwarderError.NoAvailableDestinations, ExpectedStatus = 502 }
            };

            foreach (var testCase in testCases)
            {
                // Arrange
                var req = new TranslateRequest { Service = "users", Method = "GET", Path = "/api/users/test" };
                var context = new DefaultHttpContext();

                _mockMetricsService.StartActivity(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
                    .Returns((Activity?)null);

                _mockForwarder.SendAsync(
                    Arg.Any<HttpContext>(),
                    Arg.Any<string>(),
                    Arg.Any<HttpMessageInvoker>(),
                    Arg.Any<ForwarderRequestConfig>(),
                    Arg.Any<HttpTransformer>())
                    .Returns(testCase.Error);

                // Act
                var result = await _translator.ProcessRequestAsync(context, req, CancellationToken.None);

                // Assert
                result.Error.Should().NotBeNull($"Error should not be null for {testCase.Error}");
                result.Error!.StatusCode.Should().Be(testCase.ExpectedStatus, $"Status code should be {testCase.ExpectedStatus} for {testCase.Error}");
            }
        }

        #endregion

        #region Custom Cache Expiration Tests

        [Fact]
        public async Task ProcessRequestAsync_WithCustomCacheExpiration_ShouldUseCustomValue()
        {
            // Arrange
            var customExpirationMinutes = 10;
            var req = new TranslateRequest
            {
                Service = "users",
                Method = "GET",
                Path = "/api/users/custom-cache",
                CacheExpirationMinutes = customExpirationMinutes
            };
            var context = new DefaultHttpContext();

            _mockCacheService.GetAsync<TranslateResult>(Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<TranslateResult?>(null)); // Cache miss
            _mockCacheService.GenerateCacheKey(Arg.Any<TranslateRequest>()).Returns("cache-key");
            _mockMetricsService.StartActivity(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
                .Returns((Activity?)null);

            context.Response.StatusCode = 200;

            // Act
            var result = await _translator.ProcessRequestAsync(context, req, CancellationToken.None);

            // Assert - Debe usar el valor personalizado de expiración
            await _mockCacheService.Received(1).SetAsync(
                Arg.Any<string>(),
                Arg.Any<TranslateResult>(),
                Arg.Is<TimeSpan>(ts => ts.TotalMinutes == customExpirationMinutes),
                Arg.Any<CancellationToken>());
        }

        #endregion

        #region Different HTTP Methods Tests

        [Theory]
        [InlineData("PUT")]
        [InlineData("DELETE")]
        [InlineData("PATCH")]
        public async Task ProcessRequestAsync_WithDifferentMethods_ShouldProcessCorrectly(string method)
        {
            // Arrange
            var req = new TranslateRequest
            {
                Service = "users",
                Method = method,
                Path = "/api/users/1",
                Body = method == "DELETE" ? null : new { data = "test" }
            };
            var context = new DefaultHttpContext();

            _mockMetricsService.StartActivity(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
                .Returns((Activity?)null);

            context.Response.StatusCode = 200;

            // Act
            var result = await _translator.ProcessRequestAsync(context, req, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            // Para métodos no GET, no debe intentar leer cache
            await _mockCacheService.DidNotReceive().GetAsync<TranslateResult>(Arg.Any<string>(), Arg.Any<CancellationToken>());
        }

        #endregion

        #region Metrics Recording Tests

        [Fact]
        public async Task ProcessRequestAsync_WithCacheHit_ShouldRecordCacheMetrics()
        {
            // Arrange
            var req = new TranslateRequest { Service = "users", Method = "GET", Path = "/api/users/cached" };
            var context = new DefaultHttpContext();
            var cachedResponse = new TranslateResponse { StatusCode = 200, FromCache = false };
            var cachedResult = new TranslateResult { Response = cachedResponse };

            _mockCacheService.GetAsync<TranslateResult>(Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<TranslateResult?>(cachedResult));
            _mockMetricsService.StartActivity(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
                .Returns((Activity?)null);

            // Act
            var result = await _translator.ProcessRequestAsync(context, req, CancellationToken.None);

            // Assert
            _mockMetricsService.Received(1).RecordRequest(
                req.Service,
                req.Method,
                200,
                Arg.Any<double>(),
                true); // isCached = true
        }

        [Fact]
        public async Task ProcessRequestAsync_WithCacheMiss_ShouldRecordNonCacheMetrics()
        {
            // Arrange
            var req = new TranslateRequest { Service = "users", Method = "GET", Path = "/api/users/not-cached" };
            var context = new DefaultHttpContext();

            _mockCacheService.GetAsync<TranslateResult>(Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<TranslateResult?>(null)); // Cache miss
            _mockMetricsService.StartActivity(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
                .Returns((Activity?)null);

            context.Response.StatusCode = 200;

            // Act
            var result = await _translator.ProcessRequestAsync(context, req, CancellationToken.None);

            // Assert
            _mockMetricsService.Received(1).RecordRequest(
                req.Service,
                req.Method,
                200,
                Arg.Any<double>(),
                false); // isCached = false (no está especificado como parámetro)
        }

        #endregion

        #region Casos de Error y Manejo de Excepciones - Nuevos Tests para Cobertura

        [Fact]
        public async Task ProcessRequestAsync_WithForwarderError_Request_ShouldReturnBadRequest()
        {
            // Arrange
            var req = new TranslateRequest { Service = "users", Method = "POST", Path = "/api/users" };
            var context = new DefaultHttpContext();
            var httpRequestFeature = new HttpRequestFeature();
            context.Features.Set<IHttpRequestFeature>(httpRequestFeature);

            _mockCacheService.GetAsync<TranslateResult>(Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<TranslateResult?>(null));

            _mockForwarder.SendAsync(
                Arg.Any<HttpContext>(),
                Arg.Any<string>(),
                Arg.Any<HttpMessageInvoker>(),
                Arg.Any<ForwarderRequestConfig>(),
                Arg.Any<HttpTransformer>())
                .Returns(new ValueTask<ForwarderError>(ForwarderError.Request));

            // Act
            var result = await InvokeForwardRequestAsync(context, req);

            // Assert
            result.Should().NotBeNull();
            result.Error.Should().NotBeNull();
            result.Error!.StatusCode.Should().Be(400); // BadRequest
            result.Error.Message.Should().Contain("Gateway forwarding error: Request");
        }

        [Fact]
        public async Task ProcessRequestAsync_WithForwarderError_RequestTimedOut_ShouldReturnGatewayTimeout()
        {
            // Arrange
            var req = new TranslateRequest { Service = "users", Method = "POST", Path = "/api/users" };
            var context = new DefaultHttpContext();
            var httpRequestFeature = new HttpRequestFeature();
            context.Features.Set<IHttpRequestFeature>(httpRequestFeature);

            _mockCacheService.GetAsync<TranslateResult>(Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<TranslateResult?>(null));

            _mockForwarder.SendAsync(
                Arg.Any<HttpContext>(),
                Arg.Any<string>(),
                Arg.Any<HttpMessageInvoker>(),
                Arg.Any<ForwarderRequestConfig>(),
                Arg.Any<HttpTransformer>())
                .Returns(new ValueTask<ForwarderError>(ForwarderError.RequestTimedOut));

            // Act
            var result = await InvokeForwardRequestAsync(context, req);

            // Assert
            result.Should().NotBeNull();
            result.Error.Should().NotBeNull();
            result.Error!.StatusCode.Should().Be(504); // GatewayTimeout
            result.Error.Message.Should().Contain("Gateway forwarding error: RequestTimedOut");
        }

        [Fact]
        public async Task ProcessRequestAsync_WithForwarderError_NoAvailableDestinations_ShouldReturnBadGateway()
        {
            // Arrange
            var req = new TranslateRequest { Service = "users", Method = "POST", Path = "/api/users" };
            var context = new DefaultHttpContext();
            var httpRequestFeature = new HttpRequestFeature();
            context.Features.Set<IHttpRequestFeature>(httpRequestFeature);

            _mockCacheService.GetAsync<TranslateResult>(Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<TranslateResult?>(null));

            _mockForwarder.SendAsync(
                Arg.Any<HttpContext>(),
                Arg.Any<string>(),
                Arg.Any<HttpMessageInvoker>(),
                Arg.Any<ForwarderRequestConfig>(),
                Arg.Any<HttpTransformer>())
                .Returns(new ValueTask<ForwarderError>(ForwarderError.NoAvailableDestinations));

            // Act
            var result = await InvokeForwardRequestAsync(context, req);

            // Assert
            result.Should().NotBeNull();
            result.Error.Should().NotBeNull();
            result.Error!.StatusCode.Should().Be(502); // BadGateway
            result.Error.Message.Should().Contain("Gateway forwarding error: NoAvailableDestinations");
        }

        [Fact]
        public async Task ProcessRequestAsync_WithUnknownForwarderError_ShouldReturnBadGateway()
        {
            // Arrange
            var req = new TranslateRequest { Service = "users", Method = "POST", Path = "/api/users" };
            var context = new DefaultHttpContext();
            var httpRequestFeature = new HttpRequestFeature();
            context.Features.Set<IHttpRequestFeature>(httpRequestFeature);

            _mockCacheService.GetAsync<TranslateResult>(Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<TranslateResult?>(null));

            _mockForwarder.SendAsync(
                Arg.Any<HttpContext>(),
                Arg.Any<string>(),
                Arg.Any<HttpMessageInvoker>(),
                Arg.Any<ForwarderRequestConfig>(),
                Arg.Any<HttpTransformer>())
                .Returns(new ValueTask<ForwarderError>((ForwarderError)999)); // Error desconocido

            // Act
            var result = await InvokeForwardRequestAsync(context, req);

            // Assert
            result.Should().NotBeNull();
            result.Error.Should().NotBeNull();
            result.Error!.StatusCode.Should().Be(502); // BadGateway por defecto
            result.Error.Message.Should().Contain("Gateway forwarding error:");
        }

        [Fact]
        public async Task ProcessRequestAsync_WhenExceptionOccurs_ShouldReturnInternalServerError()
        {
            // Arrange
            var req = new TranslateRequest { Service = "users", Method = "GET", Path = "/api/users" };
            var context = new DefaultHttpContext();

            _mockCacheService.GetAsync<TranslateResult>(Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromException<TranslateResult?>(new InvalidOperationException("Cache service error")));

            // Act
            var result = await _translator.ProcessRequestAsync(context, req, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Error.Should().NotBeNull();
            result.Error!.StatusCode.Should().Be(500);
            result.Error.Message.Should().Be("Internal gateway error");
            result.Error.Details.Should().Be("Cache service error");
            result.Error.CorrelationId.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task ForwardAsync_WhenErrorOccurs_ShouldWriteErrorToResponse()
        {
            // Arrange
            var req = new TranslateRequest { Service = "users", Method = "POST", Path = "/api/users" };
            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();

            _mockForwarder.SendAsync(
                Arg.Any<HttpContext>(),
                Arg.Any<string>(),
                Arg.Any<HttpMessageInvoker>(),
                Arg.Any<ForwarderRequestConfig>(),
                Arg.Any<HttpTransformer>())
                .Returns(new ValueTask<ForwarderError>(ForwarderError.Request));

            // Act
            await _translator.ForwardAsync(context, req, CancellationToken.None);

            // Assert
            context.Response.StatusCode.Should().Be(400);
            context.Response.ContentType.Should().Be("application/json");
        }

        [Fact]
        public async Task ForwardAsync_WhenErrorHasHeaders_ShouldAddHeadersToResponse()
        {
            // Arrange
            var req = new TranslateRequest { Service = "users", Method = "POST", Path = "/api/users" };
            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();

            _mockForwarder.SendAsync(
                Arg.Any<HttpContext>(),
                Arg.Any<string>(),
                Arg.Any<HttpMessageInvoker>(),
                Arg.Any<ForwarderRequestConfig>(),
                Arg.Any<HttpTransformer>())
                .Returns(new ValueTask<ForwarderError>(ForwarderError.Request));

            // Act
            await _translator.ForwardAsync(context, req, CancellationToken.None);

            // Assert
            context.Response.StatusCode.Should().Be(400);
            _mockMetricsService.Received(1).RecordRequest(
                req.Service,
                req.Method,
                400,
                Arg.Any<double>());
        }

        [Fact]
        public async Task ForwardAsync_WhenSuccessful_ShouldRecordSuccessfulMetrics()
        {
            // Arrange
            var req = new TranslateRequest { Service = "users", Method = "POST", Path = "/api/users" };
            var context = new DefaultHttpContext();
            context.Response.StatusCode = 200;

            _mockForwarder.SendAsync(
                Arg.Any<HttpContext>(),
                Arg.Any<string>(),
                Arg.Any<HttpMessageInvoker>(),
                Arg.Any<ForwarderRequestConfig>(),
                Arg.Any<HttpTransformer>(),
                Arg.Any<CancellationToken>())
                .Returns(ForwarderError.None);

            // Act
            await _translator.ForwardAsync(context, req, CancellationToken.None);

            // Assert
            _mockMetricsService.Received(1).RecordRequest(
                req.Service,
                req.Method,
                200,
                Arg.Any<double>());
        }

        [Fact]
        public async Task ForwardAsync_WhenExceptionOccurs_ShouldHandleException()
        {
            // Arrange
            var req = new TranslateRequest { Service = "users", Method = "POST", Path = "/api/users" };
            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();

            _mockForwarder.SendAsync(
                Arg.Any<HttpContext>(),
                Arg.Any<string>(),
                Arg.Any<HttpMessageInvoker>(),
                Arg.Any<ForwarderRequestConfig>(),
                Arg.Any<HttpTransformer>())
                .Returns(ValueTask.FromException<ForwarderError>(new HttpRequestException("Network error")));

            // Act
            await _translator.ForwardAsync(context, req, CancellationToken.None);

            // Assert
            context.Response.StatusCode.Should().Be(500);
            context.Response.ContentType.Should().Be("application/json");
            _mockMetricsService.Received(1).RecordRequest(
                req.Service,
                req.Method,
                500,
                Arg.Any<double>());
        }

        #endregion

        #region Casos de Caché - Tests adicionales

        [Fact]
        public async Task ProcessRequestAsync_WithCachingDisabled_ShouldNotUseCache()
        {
            // Arrange
            // Creamos una nueva instancia del translator con caching deshabilitado
            var disabledCacheOptions = new GateOptions
            {
                EnableCaching = false,
                CacheExpirationMinutes = 5,
                DefaultTimeoutSeconds = 30,
                Services = new Dictionary<string, string>
                {
                    { "users", "http://localhost:5001" }
                },
                AllowedRoutes = new List<AllowedRoute>
                {
                    new AllowedRoute { Service = "users", Methods = new[] { "GET" }, PathPrefix = "/api/users" }
                }
            };

            var mockOptions = Substitute.For<IOptions<GateOptions>>();
            mockOptions.Value.Returns(disabledCacheOptions);

            var translatorWithDisabledCache = new RequestTranslator(
                mockOptions,
                _mockForwarder,
                _httpClient,
                _mockCacheService,
                _mockMetricsService,
                _mockResiliencePolicyService,
                _mockLogger);

            var req = new TranslateRequest { Service = "users", Method = "GET", Path = "/api/users" };
            var context = new DefaultHttpContext();
            context.Response.StatusCode = 200;

            _mockForwarder.SendAsync(
                Arg.Any<HttpContext>(),
                Arg.Any<string>(),
                Arg.Any<HttpMessageInvoker>(),
                Arg.Any<ForwarderRequestConfig>(),
                Arg.Any<HttpTransformer>(),
                Arg.Any<CancellationToken>())
                .Returns(ForwarderError.None);

            // Act
            var result = await translatorWithDisabledCache.ProcessRequestAsync(context, req, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Response.Should().NotBeNull();
            result.Response!.FromCache.Should().BeFalse();

            // No debe intentar obtener del caché
            await _mockCacheService.DidNotReceive().GetAsync<TranslateResult>(Arg.Any<string>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ProcessRequestAsync_WithUseCacheFalse_ShouldNotUseCache()
        {
            // Arrange
            var req = new TranslateRequest
            {
                Service = "users",
                Method = "GET",
                Path = "/api/users",
                UseCache = false
            };
            var context = new DefaultHttpContext();
            context.Response.StatusCode = 200;

            _mockForwarder.SendAsync(
                Arg.Any<HttpContext>(),
                Arg.Any<string>(),
                Arg.Any<HttpMessageInvoker>(),
                Arg.Any<ForwarderRequestConfig>(),
                Arg.Any<HttpTransformer>(),
                Arg.Any<CancellationToken>())
                .Returns(ForwarderError.None);

            // Act
            var result = await _translator.ProcessRequestAsync(context, req, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Response.Should().NotBeNull();
            result.Response!.FromCache.Should().BeFalse();

            // No debe intentar obtener del caché
            await _mockCacheService.DidNotReceive().GetAsync<TranslateResult>(Arg.Any<string>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ProcessRequestAsync_WithNonGetMethod_ShouldNotUseCache()
        {
            // Arrange
            var req = new TranslateRequest { Service = "users", Method = "POST", Path = "/api/users" };
            var context = new DefaultHttpContext();
            context.Response.StatusCode = 201;

            _mockForwarder.SendAsync(
                Arg.Any<HttpContext>(),
                Arg.Any<string>(),
                Arg.Any<HttpMessageInvoker>(),
                Arg.Any<ForwarderRequestConfig>(),
                Arg.Any<HttpTransformer>(),
                Arg.Any<CancellationToken>())
                .Returns(ForwarderError.None);

            // Act
            var result = await _translator.ProcessRequestAsync(context, req, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Response.Should().NotBeNull();
            result.Response!.FromCache.Should().BeFalse();

            // No debe intentar obtener del caché para métodos no-GET
            await _mockCacheService.DidNotReceive().GetAsync<TranslateResult>(Arg.Any<string>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ProcessRequestAsync_WithErrorResponse_ShouldNotCacheError()
        {
            // Arrange
            var req = new TranslateRequest { Service = "users", Method = "GET", Path = "/api/users/notfound" };
            var context = new DefaultHttpContext();

            _mockCacheService.GetAsync<TranslateResult>(Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<TranslateResult?>(null));

            _mockForwarder.SendAsync(
                Arg.Any<HttpContext>(),
                Arg.Any<string>(),
                Arg.Any<HttpMessageInvoker>(),
                Arg.Any<ForwarderRequestConfig>(),
                Arg.Any<HttpTransformer>(),
                Arg.Any<CancellationToken>())
                .Returns(ForwarderError.None);

            context.Response.StatusCode = 404; // Error response

            // Act
            var result = await _translator.ProcessRequestAsync(context, req, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Response.Should().NotBeNull();
            result.Response!.StatusCode.Should().Be(404);

            // No debe guardar en caché respuestas de error
            await _mockCacheService.DidNotReceive().SetAsync(
                Arg.Any<string>(),
                Arg.Any<TranslateResult>(),
                Arg.Any<TimeSpan>(),
                Arg.Any<CancellationToken>());
        }

        #endregion

        #region Tests para casos de Query Parameters y Headers

        [Fact]
        public async Task ProcessRequestAsync_WithQueryParameters_ShouldBuildCorrectUrl()
        {
            // Arrange
            var req = new TranslateRequest
            {
                Service = "users",
                Method = "GET",
                Path = "/api/users",
                Query = new Dictionary<string, string>
                {
                    { "page", "1" },
                    { "limit", "10" }
                }
            };
            var context = new DefaultHttpContext();
            context.Response.StatusCode = 200;

            _mockCacheService.GetAsync<TranslateResult>(Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<TranslateResult?>(null));

            _mockForwarder.SendAsync(
                Arg.Any<HttpContext>(),
                Arg.Any<string>(),
                Arg.Any<HttpMessageInvoker>(),
                Arg.Any<ForwarderRequestConfig>(),
                Arg.Any<HttpTransformer>())
                .Returns(new ValueTask<ForwarderError>(ForwarderError.None));

            // Act
            var result = await _translator.ProcessRequestAsync(context, req, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Response.Should().NotBeNull();

            // Verificar que YARP fue llamado con la URL correcta
            await _mockForwarder.Received(1).SendAsync(
                Arg.Any<HttpContext>(),
                Arg.Is<string>(url => url.Contains("page=1") && url.Contains("limit=10")),
                Arg.Any<HttpMessageInvoker>(),
                Arg.Any<ForwarderRequestConfig>(),
                Arg.Any<HttpTransformer>());
        }

        [Fact]
        public async Task ProcessRequestAsync_WithCustomHeaders_ShouldAddAllowedHeaders()
        {
            // Arrange
            var req = new TranslateRequest
            {
                Service = "users",
                Method = "POST",
                Path = "/api/users",
                Headers = new Dictionary<string, string>
                {
                    { "Authorization", "Bearer token123" },
                    { "X-Custom-Header", "custom-value" },
                    { "Host", "malicious-host" }, // Este debe ser filtrado
                    { "Content-Length", "100" } // Este debe ser filtrado
                }
            };
            var context = new DefaultHttpContext();
            context.Response.StatusCode = 201;

            _mockForwarder.SendAsync(
                Arg.Any<HttpContext>(),
                Arg.Any<string>(),
                Arg.Any<HttpMessageInvoker>(),
                Arg.Any<ForwarderRequestConfig>(),
                Arg.Any<HttpTransformer>(),
                Arg.Any<CancellationToken>())
                .Returns(ForwarderError.None);

            // Act
            var result = await _translator.ProcessRequestAsync(context, req, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Response.Should().NotBeNull();

            // Los headers permitidos deberían estar en el contexto
            context.Request.Headers.Should().ContainKey("Authorization");
            context.Request.Headers.Should().ContainKey("X-Custom-Header");
            // Los headers prohibidos no deberían estar
            context.Request.Headers.Should().NotContainKey("Host");
            context.Request.Headers.Should().NotContainKey("Content-Length");
        }

        [Fact]
        public async Task ProcessRequestAsync_WithDeleteMethod_ShouldSetEmptyBody()
        {
            // Arrange
            var req = new TranslateRequest { Service = "users", Method = "DELETE", Path = "/api/users/1" };
            var context = new DefaultHttpContext();
            context.Response.StatusCode = 204;

            _mockForwarder.SendAsync(
                Arg.Any<HttpContext>(),
                Arg.Any<string>(),
                Arg.Any<HttpMessageInvoker>(),
                Arg.Any<ForwarderRequestConfig>(),
                Arg.Any<HttpTransformer>(),
                Arg.Any<CancellationToken>())
                .Returns(ForwarderError.None);

            // Act
            var result = await _translator.ProcessRequestAsync(context, req, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            context.Request.ContentLength.Should().Be(0);
            // Verificar que el body es vacío en lugar de comparar con Stream.Null
            context.Request.Body.Length.Should().Be(0);
        }

        [Fact]
        public async Task ProcessRequestAsync_WithPostMethodAndBody_ShouldSetJsonBody()
        {
            // Arrange
            var requestBody = new { name = "John", email = "john@example.com" };
            var req = new TranslateRequest
            {
                Service = "users",
                Method = "POST",
                Path = "/api/users",
                Body = requestBody
            };
            var context = new DefaultHttpContext();
            context.Response.StatusCode = 201;

            _mockForwarder.SendAsync(
                Arg.Any<HttpContext>(),
                Arg.Any<string>(),
                Arg.Any<HttpMessageInvoker>(),
                Arg.Any<ForwarderRequestConfig>(),
                Arg.Any<HttpTransformer>(),
                Arg.Any<CancellationToken>())
                .Returns(ForwarderError.None);

            // Act
            var result = await _translator.ProcessRequestAsync(context, req, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            context.Request.ContentLength.Should().BeGreaterThan(0);
            context.Request.ContentType.Should().Be("application/json");
        }

        [Fact]
        public async Task ProcessRequestAsync_WithCustomCacheExpiration_ShouldUseCustomExpiration()
        {
            // Arrange
            var customExpirationMinutes = 15;
            var req = new TranslateRequest
            {
                Service = "users",
                Method = "GET",
                Path = "/api/users/cached",
                CacheExpirationMinutes = customExpirationMinutes
            };
            var context = new DefaultHttpContext();
            context.Response.StatusCode = 200;

            _mockCacheService.GetAsync<TranslateResult>(Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<TranslateResult?>(null));

            _mockForwarder.SendAsync(
                Arg.Any<HttpContext>(),
                Arg.Any<string>(),
                Arg.Any<HttpMessageInvoker>(),
                Arg.Any<ForwarderRequestConfig>(),
                Arg.Any<HttpTransformer>(),
                Arg.Any<CancellationToken>())
                .Returns(ForwarderError.None);

            // Act
            var result = await _translator.ProcessRequestAsync(context, req, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Response.Should().NotBeNull();

            // Verificar que se usó la expiración personalizada
            await _mockCacheService.Received(1).SetAsync(
                Arg.Any<string>(),
                Arg.Any<TranslateResult>(),
                Arg.Is<TimeSpan>(ts => ts.TotalMinutes == customExpirationMinutes),
                Arg.Any<CancellationToken>());
        }

        #endregion

        private async Task<TranslateResult> InvokeForwardRequestAsync(HttpContext context, TranslateRequest req)
        {
            // Usar reflexión para invocar el método privado
            var method = typeof(RequestTranslator).GetMethod("ForwardRequestAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            return await (Task<TranslateResult>)method!.Invoke(_translator, new object[] { context, req, CancellationToken.None })!;
        }
    }
}
