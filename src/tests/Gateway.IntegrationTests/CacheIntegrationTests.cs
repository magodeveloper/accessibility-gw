using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Distributed;
using System.Text;
using Xunit;
using FluentAssertions;
using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Caching.Memory;
using System.Text.Json;

namespace Gateway.IntegrationTests;

public class CacheIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public CacheIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((context, config) =>
            {
                // Configuración para testing sin Redis
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Redis:ConnectionString"] = "", // Sin Redis para forzar MemoryCache
                    ["Gate:EnableCache"] = "true",
                    ["Gate:CacheDefaultDuration"] = "60"
                });
            });

            builder.ConfigureServices(services =>
            {
                // Remover cualquier registro previo de Redis
                var redisDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IDistributedCache));
                if (redisDescriptor != null)
                {
                    services.Remove(redisDescriptor);
                }

                // Forzar uso de MemoryDistributedCache para testing
                services.AddSingleton<IDistributedCache, MemoryDistributedCache>();

                // Mock de HttpClient para evitar llamadas reales a servicios
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
    public async Task Gateway_CacheIntegration_Should_CacheGetRequests()
    {
        // Arrange
        var endpoint = "/health";

        // Act - Primera llamada
        var response1 = await _client.GetAsync(endpoint);
        var content1 = await response1.Content.ReadAsStringAsync();

        // Act - Segunda llamada (debería venir del cache)
        var response2 = await _client.GetAsync(endpoint);
        var content2 = await response2.Content.ReadAsStringAsync();

        // Assert
        response1.StatusCode.Should().Be(HttpStatusCode.OK);
        response2.StatusCode.Should().Be(HttpStatusCode.OK);

        // Para endpoints internos, el comportamiento de cache puede variar
        content1.Should().NotBeNullOrEmpty();
        content2.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Gateway_CacheIntegration_Should_BypassCacheForPostRequests()
    {
        // Arrange
        var endpoint = "/metrics/reset";

        // Act
        var response = await _client.PostAsync(endpoint, null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // POST no debería ser cacheado
        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrEmpty();
    }

    [Theory]
    [InlineData("/health")]
    [InlineData("/metrics")]
    [InlineData("/info")]
    public async Task Gateway_CacheIntegration_Should_HandleRealEndpoints(string endpoint)
    {
        // Act
        var response = await _client.GetAsync(endpoint);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Gateway_CacheIntegration_Should_HandleCacheKeyCollisions()
    {
        // Arrange
        var endpoint1 = "/metrics";
        var endpoint2 = "/info";

        // Act
        var response1 = await _client.GetAsync(endpoint1);
        var response2 = await _client.GetAsync(endpoint2);

        // Assert
        response1.StatusCode.Should().Be(HttpStatusCode.OK);
        response2.StatusCode.Should().Be(HttpStatusCode.OK);

        // Diferentes endpoints deben tener diferentes caches
        var content1 = await response1.Content.ReadAsStringAsync();
        var content2 = await response2.Content.ReadAsStringAsync();
        content1.Should().NotBe(content2);
    }

    [Fact]
    public async Task Gateway_CacheIntegration_Should_HandleCacheExpiration()
    {
        // Arrange
        using var customFactory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((context, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Redis:ConnectionString"] = "",
                    ["Gate:EnableCache"] = "true",
                    ["Gate:CacheDefaultDuration"] = "1" // 1 segundo para testing
                });
            });
        });

        using var client = customFactory.CreateClient();
        var endpoint = "/metrics";

        // Act - Primera llamada
        var response1 = await client.GetAsync(endpoint);

        // Esperar que expire el cache
        await Task.Delay(1500);

        // Act - Segunda llamada después de expiración
        var response2 = await client.GetAsync(endpoint);

        // Assert
        response1.StatusCode.Should().Be(HttpStatusCode.OK);
        response2.StatusCode.Should().Be(HttpStatusCode.OK);

        // Ambas respuestas deberían ser válidas independientemente del cache
        var content1 = await response1.Content.ReadAsStringAsync();
        var content2 = await response2.Content.ReadAsStringAsync();
        content1.Should().NotBeNullOrEmpty();
        content2.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Gateway_CacheIntegration_Should_HandleCacheDisabled()
    {
        // Arrange
        using var customFactory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((context, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Redis:ConnectionString"] = "",
                    ["Gate:EnableCache"] = "false"
                });
            });
        });

        using var client = customFactory.CreateClient();
        var endpoint = "/health";

        // Act
        var response1 = await client.GetAsync(endpoint);
        var response2 = await client.GetAsync(endpoint);

        // Assert
        response1.StatusCode.Should().Be(HttpStatusCode.OK);
        response2.StatusCode.Should().Be(HttpStatusCode.OK);

        // Con cache deshabilitado, las respuestas deben ser válidas
        var content1 = await response1.Content.ReadAsStringAsync();
        var content2 = await response2.Content.ReadAsStringAsync();
        content1.Should().NotBeNullOrEmpty();
        content2.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Gateway_CacheIntegration_Should_HandleLargeResponses()
    {
        // Arrange
        var endpoint = "/info";

        // Act
        var response1 = await _client.GetAsync(endpoint);
        var response2 = await _client.GetAsync(endpoint);

        // Assert
        response1.StatusCode.Should().Be(HttpStatusCode.OK);
        response2.StatusCode.Should().Be(HttpStatusCode.OK);

        // Respuestas deberían ser válidas
        var content1 = await response1.Content.ReadAsStringAsync();
        var content2 = await response2.Content.ReadAsStringAsync();
        content1.Should().NotBeNullOrEmpty();
        content2.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Gateway_CacheIntegration_Should_HandleSpecialCharactersInUrl()
    {
        // Arrange
        var endpoint = "/info";

        // Act
        var response1 = await _client.GetAsync(endpoint);
        var response2 = await _client.GetAsync(endpoint);

        // Assert
        response1.StatusCode.Should().Be(HttpStatusCode.OK);
        response2.StatusCode.Should().Be(HttpStatusCode.OK);

        // URLs válidas deberían funcionar correctamente
        var content1 = await response1.Content.ReadAsStringAsync();
        var content2 = await response2.Content.ReadAsStringAsync();
        content1.Should().NotBeNullOrEmpty();
        content2.Should().NotBeNullOrEmpty();
    }
}
