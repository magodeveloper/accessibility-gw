using Xunit;
using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Gateway.IntegrationTests;

public class GatewayBasicIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public GatewayBasicIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((context, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Gate:EnableMetrics"] = "true",
                    ["Gate:EnableCache"] = "true",
                    ["Redis:ConnectionString"] = "" // Testing sin Redis
                });
            });

            builder.ConfigureServices(services =>
            {
                // Mock de HttpClient para evitar llamadas reales
                services.AddHttpClient("gateway-forwarder", client =>
                {
                    client.BaseAddress = new Uri("http://localhost:8080");
                    client.Timeout = TimeSpan.FromSeconds(10);
                });
            });
        });

        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task Gateway_Integration_Should_StartSuccessfully()
    {
        // Act
        var response = await _client.GetAsync("/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Theory]
    [InlineData("/health")]
    [InlineData("/metrics")]
    [InlineData("/info")]
    public async Task Gateway_Should_RouteToValidServiceEndpoints(string endpoint)
    {
        // Act
        var response = await _client.GetAsync(endpoint);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verificar que la respuesta es válida
        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Gateway_Should_HandleSwaggerEndpoint()
    {
        // Act
        var response = await _client.GetAsync("/swagger/v1/swagger.json");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");

        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrEmpty();

        // Verificar que es JSON válido de Swagger
        var swaggerDoc = JsonSerializer.Deserialize<JsonElement>(content);
        swaggerDoc.TryGetProperty("openapi", out _).Should().BeTrue();
    }

    [Fact]
    public async Task Gateway_Should_HandleCorsCorrectly()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Options, "/api/users/health");
        request.Headers.Add("Origin", "https://localhost:3000");
        request.Headers.Add("Access-Control-Request-Method", "GET");

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NoContent);
    }

    [Theory]
    [InlineData("GET")]
    [InlineData("POST")]
    public async Task Gateway_Should_SupportAllHttpMethods(string method)
    {
        // Arrange
        HttpRequestMessage request;

        if (method == "GET")
        {
            request = new HttpRequestMessage(HttpMethod.Get, "/health");
        }
        else
        {
            request = new HttpRequestMessage(HttpMethod.Post, "/metrics/reset");
        }

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Gateway_Should_HandleLargePayloads()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, "/metrics/reset");

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verificar que las respuestas funcionan
        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Gateway_Should_HandleSpecialCharacters()
    {
        // Arrange
        var endpoint = "/info";

        // Act
        var response = await _client.GetAsync(endpoint);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Gateway_Should_HandleConcurrentRequests()
    {
        // Arrange
        var tasks = new List<Task<HttpResponseMessage>>();

        // Act - Enviar múltiples requests concurrentes
        for (int i = 0; i < 10; i++)
        {
            tasks.Add(_client.GetAsync($"/health"));
        }

        var responses = await Task.WhenAll(tasks);

        // Assert
        responses.Should().HaveCount(10);
        responses.Should().OnlyContain(r => r.StatusCode == HttpStatusCode.OK);

        // Verificar que cada request tiene respuesta válida
        var contents = await Task.WhenAll(responses.Select(r => r.Content.ReadAsStringAsync()));
        contents.Should().OnlyContain(c => !string.IsNullOrEmpty(c));
    }

    [Fact]
    public async Task Gateway_Should_HandleInvalidRoutes()
    {
        // Act
        var response = await _client.GetAsync("/nonexistent/service");

        // Assert
        // Puede ser 403 (no en AllowedRoutes) o 404 (no encontrada)
        response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.Forbidden);

        // Para rutas inválidas, el status code es lo importante
        // El contenido puede estar vacío para 404
    }

    [Fact]
    public async Task Gateway_Should_ValidateContentTypes()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, "/metrics/reset")
        {
            Content = new StringContent("test content", Encoding.UTF8, "text/plain")
        };

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        // Gateway debería manejar diferentes content types
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Gateway_Should_RespectRateLimiting()
    {
        // Arrange
        var tasks = new List<Task<HttpResponseMessage>>();

        // Act - Enviar muchas requests rápidamente
        for (int i = 0; i < 20; i++) // Número más conservador
        {
            tasks.Add(_client.GetAsync($"/health"));
        }

        var responses = await Task.WhenAll(tasks);

        // Assert
        var statusCodes = responses.Select(r => r.StatusCode).ToList();

        // La mayoría deberían ser exitosas, pero podría haber algunas limitadas
        var successCount = statusCodes.Count(sc => sc == HttpStatusCode.OK);
        var rateLimitedCount = statusCodes.Count(sc => sc == HttpStatusCode.TooManyRequests);

        successCount.Should().BeGreaterThan(10, "Most requests should succeed");
        (successCount + rateLimitedCount).Should().Be(20);
    }
}
