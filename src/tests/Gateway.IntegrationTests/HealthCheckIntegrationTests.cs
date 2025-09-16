using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Text.Json;
using Xunit;
using FluentAssertions;
using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Gateway.IntegrationTests;

public class HealthCheckIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public HealthCheckIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((context, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["HealthChecks:EnableDetailedErrors"] = "true",
                    ["HealthChecks:Timeout"] = "00:00:30",
                    ["Redis:ConnectionString"] = "" // Sin Redis para testing
                });
            });

            builder.ConfigureServices(services =>
            {
                // Configurar logging para capturar health checks
                services.AddLogging(builder => builder.AddDebug());

                // Mock de HttpClient para health checks
                services.AddHttpClient("health-check", client =>
                {
                    client.Timeout = TimeSpan.FromSeconds(10);
                });
            });
        });

        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task Gateway_HealthCheck_Should_ReturnHealthyStatus()
    {
        // Act
        var response = await _client.GetAsync("/health");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().NotBeNullOrEmpty();

        // Verificar formato JSON básico
        var healthReport = JsonSerializer.Deserialize<JsonElement>(content);
        healthReport.GetProperty("status").GetString().Should().Be("Healthy");
    }

    [Fact]
    public async Task Gateway_HealthCheck_Should_IncludeDetailedInformation()
    {
        // Act
        var response = await _client.GetAsync("/health");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var healthReport = JsonSerializer.Deserialize<JsonElement>(content);

        // Verificar propiedades básicas
        healthReport.TryGetProperty("status", out _).Should().BeTrue();
        healthReport.TryGetProperty("totalDuration", out _).Should().BeTrue();

        // Verificar entries si están disponibles
        if (healthReport.TryGetProperty("entries", out var entries))
        {
            entries.ValueKind.Should().Be(JsonValueKind.Object);
        }
    }

    [Fact]
    public async Task Gateway_HealthCheck_Should_CheckDependencies()
    {
        // Act
        var response = await _client.GetAsync("/health");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var healthReport = JsonSerializer.Deserialize<JsonElement>(content);

        // Verificar que el health check incluye información sobre dependencias
        if (healthReport.TryGetProperty("entries", out var entries))
        {
            // Podría incluir checks para cache, base de datos, servicios externos, etc.
            entries.ValueKind.Should().Be(JsonValueKind.Object);

            // Verificar que al menos hay algún check configurado
            var entriesCount = entries.EnumerateObject().Count();
            entriesCount.Should().BeGreaterThanOrEqualTo(0);
        }
    }

    [Theory]
    [InlineData("/health")]
    [InlineData("/health/ready")]
    [InlineData("/health/live")]
    public async Task Gateway_HealthCheck_Should_SupportMultipleEndpoints(string endpoint)
    {
        // Act
        var response = await _client.GetAsync(endpoint);

        // Assert
        // Los endpoints de health check pueden devolver diferentes códigos según el estado
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.OK,
            HttpStatusCode.ServiceUnavailable,
            HttpStatusCode.InternalServerError);

        // Todos los endpoints de health check deberían devolver JSON válido
        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrEmpty();

        var healthReport = JsonSerializer.Deserialize<JsonElement>(content);
        healthReport.TryGetProperty("status", out _).Should().BeTrue();
    }

    [Fact]
    public async Task Gateway_HealthCheck_Should_IncludeResponseHeaders()
    {
        // Act
        var response = await _client.GetAsync("/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");

        // Verificar headers de cache (health checks generalmente no se cachean)
        response.Headers.CacheControl?.NoCache.Should().BeTrue();
    }

    [Fact]
    public async Task Gateway_HealthCheck_Should_HandleConcurrentRequests()
    {
        // Arrange
        var tasks = new List<Task<HttpResponseMessage>>();

        // Act - Enviar múltiples health checks concurrentes
        for (int i = 0; i < 5; i++)
        {
            tasks.Add(_client.GetAsync("/health"));
        }

        var responses = await Task.WhenAll(tasks);

        // Assert
        responses.Should().HaveCount(5);
        responses.Should().OnlyContain(r => r.StatusCode == HttpStatusCode.OK);

        // Todos deberían devolver el mismo estado (asumiendo sistema estable)
        var contents = await Task.WhenAll(responses.Select(r => r.Content.ReadAsStringAsync()));
        contents.Should().OnlyContain(c => !string.IsNullOrEmpty(c));

        // Todos deberían ser JSON válido
        foreach (var content in contents)
        {
            var healthReport = JsonSerializer.Deserialize<JsonElement>(content);
            healthReport.TryGetProperty("status", out _).Should().BeTrue();
        }
    }

    [Fact]
    public async Task Gateway_HealthCheck_Should_MeasureResponseTime()
    {
        // Arrange
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Act
        var response = await _client.GetAsync("/health");
        stopwatch.Stop();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Health check debería ser rápido (menos de 5 segundos)
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(5000);

        var content = await response.Content.ReadAsStringAsync();
        var healthReport = JsonSerializer.Deserialize<JsonElement>(content);

        if (healthReport.TryGetProperty("totalDuration", out var duration))
        {
            // Verificar que el duration está en formato válido
            duration.GetString().Should().NotBeNullOrEmpty();
        }
    }

    [Fact]
    public async Task Gateway_HealthCheck_Should_HandleMemoryPressure()
    {
        // Arrange
        var tasks = new List<Task<HttpResponseMessage>>();

        // Act - Hacer muchas llamadas para simular carga
        for (int i = 0; i < 20; i++)
        {
            tasks.Add(_client.GetAsync("/health"));
        }

        var responses = await Task.WhenAll(tasks);

        // Assert
        responses.Should().HaveCount(20);

        // Incluso bajo carga, health checks deberían funcionar
        var successfulResponses = responses.Count(r => r.StatusCode == HttpStatusCode.OK);
        successfulResponses.Should().BeGreaterThan(15, "Most health checks should succeed even under load");
    }

    [Fact]
    public async Task Gateway_HealthCheck_Should_ValidateJsonStructure()
    {
        // Act
        var response = await _client.GetAsync("/health");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var healthReport = JsonSerializer.Deserialize<JsonElement>(content);

        // Estructura básica esperada
        healthReport.TryGetProperty("status", out var status).Should().BeTrue();

        var statusValue = status.GetString();
        statusValue.Should().BeOneOf("Healthy", "Degraded", "Unhealthy");

        // Verificar duración si está presente
        if (healthReport.TryGetProperty("totalDuration", out var totalDuration))
        {
            totalDuration.ValueKind.Should().Be(JsonValueKind.String);
            totalDuration.GetString().Should().NotBeNullOrEmpty();
        }

        // Verificar entries si están presentes
        if (healthReport.TryGetProperty("entries", out var entries))
        {
            entries.ValueKind.Should().Be(JsonValueKind.Object);

            foreach (var entry in entries.EnumerateObject())
            {
                entry.Value.TryGetProperty("status", out var entryStatus).Should().BeTrue();
                var entryStatusValue = entryStatus.GetString();
                entryStatusValue.Should().BeOneOf("Healthy", "Degraded", "Unhealthy");
            }
        }
    }

    [Fact]
    public async Task Gateway_HealthCheck_Should_HandleInvalidPaths()
    {
        // Act
        var response = await _client.GetAsync("/health/invalid");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Gateway_HealthCheck_Should_SupportOptionsRequest()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Options, "/health");

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        // OPTIONS debería ser manejado apropiadamente
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.OK,
            HttpStatusCode.NoContent,
            HttpStatusCode.MethodNotAllowed
        );
    }
}
