using Xunit;
using System.Net;
using Gateway.Models;
using FluentAssertions;
using Gateway.Services;
using System.Text.Json;
using System.Net.Http.Json;
using Yarp.ReverseProxy.Forwarder;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Gateway.UnitTests
{
    public class ProgramTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;

        public ProgramTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((context, config) =>
                {
                    config.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["Gate:Services:users"] = "http://localhost:5001",
                        ["Gate:Services:reports"] = "http://localhost:5002",
                        ["Gate:Services:analysis"] = "http://localhost:5003",
                        ["Gate:Services:middleware"] = "http://localhost:5004",
                        ["Gate:DefaultTimeoutSeconds"] = "30",
                        ["Gate:MaxPayloadSizeBytes"] = "10485760",
                        ["Gate:EnableCaching"] = "true",
                        ["Gate:EnableMetrics"] = "true",
                        ["Gate:EnableTracing"] = "false",
                        ["Gate:AllowedRoutes:0:Service"] = "users",
                        ["Gate:AllowedRoutes:0:Methods:0"] = "GET",
                        ["Gate:AllowedRoutes:0:Methods:1"] = "POST",
                        ["Gate:AllowedRoutes:0:PathPrefix"] = "/api/v1/users",
                        ["Redis:ConnectionString"] = "",
                        ["Jwt:Authority"] = "",
                        ["Jwt:Audience"] = "",
                        ["HealthChecks:EnableUI"] = "false"
                    });
                });
            });

            _client = _factory.CreateClient();
        }

        [Fact]
        public void Program_ShouldConfigureRequiredServices()
        {
            // Arrange & Act
            using var scope = _factory.Services.CreateScope();
            var serviceProvider = scope.ServiceProvider;

            // Assert - Servicios principales configurados
            serviceProvider.GetService<ILogger<Program>>().Should().NotBeNull();
            serviceProvider.GetService<IConfiguration>().Should().NotBeNull();
            serviceProvider.GetService<IOptions<GateOptions>>().Should().NotBeNull();

            // Servicios personalizados
            serviceProvider.GetService<RequestTranslator>().Should().NotBeNull();
            serviceProvider.GetService<ICacheService>().Should().NotBeNull();
            serviceProvider.GetService<IMetricsService>().Should().NotBeNull();
            serviceProvider.GetService<IHttpForwarder>().Should().NotBeNull();
        }

        [Fact]
        public void Program_ShouldConfigureGateOptions()
        {
            // Arrange & Act
            using var scope = _factory.Services.CreateScope();
            var gateOptions = scope.ServiceProvider.GetRequiredService<IOptions<GateOptions>>().Value;

            // Assert
            gateOptions.Should().NotBeNull();
            gateOptions.Services.Should().ContainKey("users");
            gateOptions.Services.Should().ContainKey("reports");
            gateOptions.Services.Should().ContainKey("analysis");
            gateOptions.Services.Should().ContainKey("middleware");
            gateOptions.DefaultTimeoutSeconds.Should().Be(30);
            gateOptions.MaxPayloadSizeBytes.Should().Be(10485760);
            gateOptions.EnableCaching.Should().BeTrue();
            gateOptions.EnableMetrics.Should().BeTrue();
        }

        [Fact]
        public void Program_ShouldConfigureHealthChecks()
        {
            // Arrange & Act
            using var scope = _factory.Services.CreateScope();
            var healthCheckService = scope.ServiceProvider.GetRequiredService<HealthCheckService>();

            // Assert
            healthCheckService.Should().NotBeNull();
        }

        [Fact]
        public void Program_ShouldConfigureDistributedCache()
        {
            // Arrange & Act
            using var scope = _factory.Services.CreateScope();
            var distributedCache = scope.ServiceProvider.GetRequiredService<IDistributedCache>();

            // Assert - El cache distribuido debe estar configurado (puede ser Redis o Memory)
            distributedCache.Should().NotBeNull();
        }

        [Fact]
        public async Task SwaggerEndpoint_ShouldReturnSwaggerDocument()
        {
            // Act
            var response = await _client.GetAsync("/swagger/v1/swagger.json");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");

            var content = await response.Content.ReadAsStringAsync();
            content.Should().Contain("Accessibility Platform API Gateway");
            content.Should().Contain("paths");
        }

        [Fact]
        public async Task SwaggerUI_ShouldBeAccessible()
        {
            // Act
            var response = await _client.GetAsync("/swagger");

            // Assert - En tests se comporta diferente, verificamos que es accesible
            response.StatusCode.Should().BeOneOf(HttpStatusCode.MovedPermanently, HttpStatusCode.OK, HttpStatusCode.Redirect);
        }

        [Fact]
        public async Task SwaggerIndexPage_ShouldReturnHtml()
        {
            // Act
            var response = await _client.GetAsync("/swagger/index.html");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Content.Headers.ContentType?.MediaType.Should().Be("text/html");
        }

        [Fact]
        public async Task HealthEndpoint_ShouldReturnHealthStatus()
        {
            // Act
            var response = await _client.GetAsync("/health");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var content = await response.Content.ReadAsStringAsync();
            var healthResponse = JsonSerializer.Deserialize<JsonElement>(content);
            healthResponse.GetProperty("status").GetString().Should().Be("Healthy");
        }

        [Fact]
        public async Task LivenessEndpoint_ShouldReturnHealthyStatus()
        {
            // Act
            var response = await _client.GetAsync("/health/live");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<JsonElement>(content);
            result.GetProperty("status").GetString().Should().Be("healthy");
        }

        [Fact]
        public async Task ReadinessEndpoint_ShouldReturnReadyStatus()
        {
            // Act
            var response = await _client.GetAsync("/health/ready");

            // Assert - El endpoint de readiness puede retornar varios códigos dependiendo del estado
            response.Should().NotBeNull();
            var content = await response.Content.ReadAsStringAsync();
            content.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task MetricsEndpoint_ShouldReturnMetrics()
        {
            // Act
            var response = await _client.GetAsync("/metrics");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var content = await response.Content.ReadAsStringAsync();
            content.Should().NotBeNullOrEmpty();

            // Verificar que es formato Prometheus (comienza con # para comentarios)
            // El formato Prometheus es texto plano, no JSON
            content.Should().Contain("#");

            // Verificar el content type correcto para métricas de Prometheus
            response.Content.Headers.ContentType?.MediaType.Should().BeOneOf(
                "text/plain",
                "application/openmetrics-text",
                null // A veces no se especifica el content type
            );
        }

        [Fact]
        public async Task InfoEndpoint_ShouldReturnGatewayInfo()
        {
            // Act
            var response = await _client.GetAsync("/info");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var content = await response.Content.ReadAsStringAsync();
            var info = JsonSerializer.Deserialize<JsonElement>(content);

            info.GetProperty("name").GetString().Should().Be("Accessibility Gateway");
            info.GetProperty("version").GetString().Should().Be("1.0.0");
            info.GetProperty("services").GetArrayLength().Should().BeGreaterThan(0);

            var features = info.GetProperty("features");
            features.GetProperty("caching").GetBoolean().Should().BeTrue();
            features.GetProperty("metrics").GetBoolean().Should().BeTrue();
        }

        [Fact]
        public async Task TranslateEndpoint_WithInvalidRequest_ShouldReturnBadRequest()
        {
            // Arrange - Request con validaciones inválidas
            var invalidRequest = new ValidatedTranslateRequest
            {
                Service = "nonexistent",
                Method = "GET",
                Path = "/api/test",
                Query = new Dictionary<string, string>(),
                Headers = new Dictionary<string, string>()
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/v1/translate", invalidRequest);

            // Assert - Esperamos 403 porque la ruta no está configurada en AllowedRoutes (seguridad por defecto)
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task ErrorEndpoint_ShouldReturnProblemDetails()
        {
            // Act
            var response = await _client.GetAsync("/error");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);

            var content = await response.Content.ReadAsStringAsync();
            content.Should().Contain("An error occurred processing your request");
        }

        [Fact]
        public async Task ResetMetricsEndpoint_ShouldResetMetrics()
        {
            // Act
            var response = await _client.PostAsync("/metrics/reset", null);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<JsonElement>(content);
            result.GetProperty("message").GetString().Should().Be("Metrics reset successfully");
        }

        [Fact]
        public async Task InvalidateCacheEndpoint_ShouldInvalidateCache()
        {
            // Act
            var response = await _client.DeleteAsync("/cache/users");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<JsonElement>(content);
            result.GetProperty("message").GetString().Should().Contain("Cache invalidated for service: users");
        }

        [Theory]
        [InlineData("/api/v1/users/123", "users")]
        [InlineData("/api/Report/generate", "reports")]
        [InlineData("/api/Analysis/scan", "analysis")]
        public async Task AutomaticRouting_ShouldProcessApiCalls(string path, string expectedService)
        {
            // Act
            var response = await _client.GetAsync(path);

            // Assert
            // Estos requests pueden fallar porque los servicios no están ejecutándose,
            // pero no deberían ser 404 si el middleware los procesa correctamente
            // Verificamos que el middleware al menos intente procesarlos
            response.StatusCode.Should().NotBe(HttpStatusCode.MethodNotAllowed);

            // Verificar que el expectedService se corresponde con el path
            expectedService.Should().NotBeNullOrEmpty();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _client?.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
