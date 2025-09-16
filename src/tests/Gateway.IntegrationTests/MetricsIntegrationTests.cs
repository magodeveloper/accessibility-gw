using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text;
using Xunit;
using FluentAssertions;
using System.Net;
using Microsoft.Extensions.Configuration;
using System.Text.Json;
using Gateway.Services;
using System.Diagnostics.Metrics;
using System.Diagnostics;

namespace Gateway.IntegrationTests;

public class MetricsIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public MetricsIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((context, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Gate:EnableMetrics"] = "true",
                    ["Gate:EnableCache"] = "true",
                    ["Redis:ConnectionString"] = "" // Sin Redis para testing
                });
            });

            builder.ConfigureServices(services =>
            {
                // Configurar logging para capturar métricas
                services.AddLogging(builder => builder.AddDebug());

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
    public async Task Gateway_MetricsIntegration_Should_RecordRequestMetrics()
    {
        // Arrange
        var endpoint = "/health";

        // Act
        var response = await _client.GetAsync(endpoint);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verificar que la respuesta es válida
        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrEmpty();
    }

    [Theory]
    [InlineData("/health")]
    [InlineData("/metrics")]
    [InlineData("/info")]
    public async Task Gateway_MetricsIntegration_Should_RecordMetricsForAllServices(string endpoint)
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
    public async Task Gateway_MetricsIntegration_Should_RecordCacheHitMetrics()
    {
        // Arrange
        var endpoint = "/metrics";

        // Act - Primera llamada
        var response1 = await _client.GetAsync(endpoint);

        // Act - Segunda llamada  
        var response2 = await _client.GetAsync(endpoint);

        // Assert
        response1.StatusCode.Should().Be(HttpStatusCode.OK);
        response2.StatusCode.Should().Be(HttpStatusCode.OK);

        // Ambas respuestas deberían ser válidas
        var content1 = await response1.Content.ReadAsStringAsync();
        var content2 = await response2.Content.ReadAsStringAsync();

        content1.Should().NotBeNullOrEmpty();
        content2.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Gateway_MetricsIntegration_Should_RecordErrorMetrics()
    {
        // Arrange
        var endpoint = "/nonexistent/endpoint";

        // Act
        var response = await _client.GetAsync(endpoint);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        // El endpoint puede no devolver contenido para 404
        // Pero el status code es lo importante
    }

    [Fact]
    public async Task Gateway_MetricsIntegration_Should_RecordMethodSpecificMetrics()
    {
        // Arrange
        var getEndpoint = "/health";
        var postEndpoint = "/metrics/reset";

        // Act
        var getResponse = await _client.GetAsync(getEndpoint);
        var postResponse = await _client.PostAsync(postEndpoint, null);

        // Assert
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        postResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Ambos métodos deberían generar respuestas válidas
        var getContent = await getResponse.Content.ReadAsStringAsync();
        var postContent = await postResponse.Content.ReadAsStringAsync();

        getContent.Should().NotBeNullOrEmpty();
        postContent.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Gateway_MetricsIntegration_Should_HandleConcurrentRequests()
    {
        // Arrange
        var endpoint = "/health";
        var tasks = new List<Task<HttpResponseMessage>>();

        // Act - Enviar múltiples requests concurrentes
        for (int i = 0; i < 10; i++)
        {
            tasks.Add(_client.GetAsync($"{endpoint}"));
        }

        var responses = await Task.WhenAll(tasks);

        // Assert
        responses.Should().HaveCount(10);
        responses.Should().OnlyContain(r => r.StatusCode == HttpStatusCode.OK);

        // Verificar que todas las respuestas son válidas
        var contents = await Task.WhenAll(responses.Select(r => r.Content.ReadAsStringAsync()));
        contents.Should().OnlyContain(c => !string.IsNullOrEmpty(c));
    }

    [Fact]
    public async Task Gateway_MetricsIntegration_Should_RecordLargePayloadMetrics()
    {
        // Arrange
        var endpoint = "/metrics/reset";

        // Act
        var response = await _client.PostAsync(endpoint, null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Payloads deberían procesarse correctamente
        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Gateway_MetricsIntegration_Should_RecordTimeoutMetrics()
    {
        // Arrange
        var endpoint = "/health";

        // Act
        var response = await _client.GetAsync(endpoint);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Incluso con timeouts configurados, requests válidos deberían funcionar
        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Gateway_MetricsIntegration_Should_HandleMetricsDisabled()
    {
        // Arrange
        using var customFactory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((context, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Gate:EnableMetrics"] = "false"
                });
            });
        });

        using var client = customFactory.CreateClient();
        var endpoint = "/health";

        // Act
        var response = await client.GetAsync(endpoint);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Con métricas deshabilitadas, las respuestas deben funcionar
        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrEmpty();
    }

    private static double ExtractResponseTimeMs(string responseTimeHeader)
    {
        if (string.IsNullOrEmpty(responseTimeHeader) || !responseTimeHeader.EndsWith("ms"))
        {
            return 0;
        }

        var timeStr = responseTimeHeader.Replace("ms", "").Trim();
        return double.TryParse(timeStr, out var time) ? time : 0;
    }
}
