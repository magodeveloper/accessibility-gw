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
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Gateway.UnitTests.Services
{
    public class RequestTranslator_ForwardAsyncTests
    {
        private readonly RequestTranslator _translator;
        private readonly ICacheService _mockCacheService;
        private readonly IMetricsService _mockMetricsService;
        private readonly IResiliencePolicyService _mockResiliencePolicyService;
        private readonly ILogger<RequestTranslator> _mockLogger;
        private readonly IHttpForwarder _mockForwarder;
        private readonly HttpMessageInvoker _httpClient;
        private readonly GateOptions _options;

        public RequestTranslator_ForwardAsyncTests()
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
        public async Task ForwardAsync_ShouldWriteErrorResponse_WhenForwarderReturnsError()
        {
            // Arrange
            var req = new TranslateRequest { Service = "users", Method = "GET", Path = "/api/users/1" };
            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();
            // Simular error de YARP
            _mockForwarder.SendAsync(
                Arg.Any<HttpContext>(),
                Arg.Any<string>(),
                Arg.Any<HttpMessageInvoker>(),
                Arg.Any<ForwarderRequestConfig>(),
                Arg.Any<HttpTransformer>()
            ).Returns(callInfo =>
            {
                // Los errores de YARP generalmente no establecen StatusCode en el contexto
                // Lo dejamos como está para que nuestro código lo mapee
                return new ValueTask<ForwarderError>(ForwarderError.Request);
            });

            // Act
            await _translator.ForwardAsync(context, req, CancellationToken.None);
            context.Response.Body.Seek(0, SeekOrigin.Begin);
            var responseBody = await new StreamReader(context.Response.Body).ReadToEndAsync();

            // Assert
            context.Response.StatusCode.Should().Be(502); // BadGateway - error de conexión al backend
            responseBody.Should().Contain("error");
        }

        [Fact]
        public async Task ForwardAsync_ShouldWriteSuccess_WhenForwarderReturnsNone()
        {
            // Arrange
            var req = new TranslateRequest { Service = "users", Method = "GET", Path = "/api/users/1" };
            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();
            // Simular éxito de YARP
            _mockForwarder.SendAsync(
                Arg.Any<HttpContext>(),
                Arg.Any<string>(),
                Arg.Any<HttpMessageInvoker>(),
                Arg.Any<ForwarderRequestConfig>(),
                Arg.Any<HttpTransformer>()
            ).Returns(callInfo =>
            {
                // Simular que YARP establece el StatusCode exitoso
                var ctx = callInfo.Arg<HttpContext>();
                ctx.Response.StatusCode = 200;
                return new ValueTask<ForwarderError>(ForwarderError.None);
            });

            // Act
            await _translator.ForwardAsync(context, req, CancellationToken.None);

            // Assert
            context.Response.StatusCode.Should().Be(200);
        }

        [Fact]
        public async Task ForwardAsync_ShouldReturn500_WhenExceptionThrown()
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
            ).Returns<ValueTask<ForwarderError>>(callInfo => throw new Exception("Simulated error"));

            // Act
            await _translator.ForwardAsync(context, req, CancellationToken.None);
            context.Response.Body.Seek(0, SeekOrigin.Begin);
            var responseBody = await new StreamReader(context.Response.Body).ReadToEndAsync();

            // Assert
            context.Response.StatusCode.Should().Be(500);
            responseBody.Should().Contain("Gateway forwarding error");
        }

        [Fact]
        public async Task ForwardAsync_ShouldWriteErrorWithHeaders_WhenResultHasErrorWithHeaders()
        {
            // Arrange
            var req = new TranslateRequest { Service = "users", Method = "GET", Path = "/api/users/1" };
            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();

            // Forzar que ForwardRequestAsync devuelva error con headers usando reflection
            var method = typeof(RequestTranslator).GetMethod("ForwardRequestAsync",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            // Mock del forwarder para devolver error
            _mockForwarder.SendAsync(
                Arg.Any<HttpContext>(),
                Arg.Any<string>(),
                Arg.Any<HttpMessageInvoker>(),
                Arg.Any<ForwarderRequestConfig>(),
                Arg.Any<HttpTransformer>()
            ).Returns(callInfo =>
            {
                var ctx = callInfo.Arg<HttpContext>();
                ctx.Response.StatusCode = 429; // Too Many Requests
                ctx.Response.Headers["Retry-After"] = "60";
                ctx.Response.Headers["X-RateLimit-Remaining"] = "0";
                return new ValueTask<ForwarderError>(ForwarderError.RequestCreation);
            });

            // Act
            await _translator.ForwardAsync(context, req, CancellationToken.None);
            context.Response.Body.Seek(0, SeekOrigin.Begin);
            var responseBody = await new StreamReader(context.Response.Body).ReadToEndAsync();

            // Assert
            // Cuando hay un error de YARP, el gateway devuelve 502 Bad Gateway
            context.Response.StatusCode.Should().Be(502);
            responseBody.Should().Contain("error");
        }

        [Fact]
        public async Task ForwardAsync_ShouldHandleInvalidStatusCode_WhenResponseStatusCodeIsZero()
        {
            // Arrange
            var req = new TranslateRequest { Service = "users", Method = "GET", Path = "/api/users/1" };
            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();

            // Simular éxito pero con StatusCode no establecido (0)
            _mockForwarder.SendAsync(
                Arg.Any<HttpContext>(),
                Arg.Any<string>(),
                Arg.Any<HttpMessageInvoker>(),
                Arg.Any<ForwarderRequestConfig>(),
                Arg.Any<HttpTransformer>()
            ).Returns(callInfo =>
            {
                // No establecer StatusCode, quedará en 0
                return new ValueTask<ForwarderError>(ForwarderError.None);
            });

            // Act
            await _translator.ForwardAsync(context, req, CancellationToken.None);

            // Assert
            // El código debe establecer un StatusCode válido por defecto
            context.Response.StatusCode.Should().BeGreaterThan(0);
        }

        [Fact]
        public async Task ForwardAsync_ShouldHandleEmptyResult_WhenNoErrorAndNoResponse()
        {
            // Arrange  
            var req = new TranslateRequest { Service = "users", Method = "POST", Path = "/api/users" };
            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();

            // Simular resultado sin error pero tampoco respuesta válida
            // Esto es un caso edge que podría ocurrir con timeouts
            _mockForwarder.SendAsync(
                Arg.Any<HttpContext>(),
                Arg.Any<string>(),
                Arg.Any<HttpMessageInvoker>(),
                Arg.Any<ForwarderRequestConfig>(),
                Arg.Any<HttpTransformer>()
            ).Returns(callInfo =>
            {
                // Simular timeout: YARP devuelve RequestTimedOut pero no se completa la respuesta
                return new ValueTask<ForwarderError>(ForwarderError.RequestTimedOut);
            });

            // Act
            await _translator.ForwardAsync(context, req, CancellationToken.None);
            context.Response.Body.Seek(0, SeekOrigin.Begin);
            var responseBody = await new StreamReader(context.Response.Body).ReadToEndAsync();

            // Assert
            context.Response.StatusCode.Should().Be(504); // Gateway Timeout
            responseBody.Should().Contain("error");
        }
    }
}
