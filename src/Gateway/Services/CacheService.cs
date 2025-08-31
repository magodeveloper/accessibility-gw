using Gateway.Models;
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;

namespace Gateway.Services;

/// <summary>
/// Servicio para manejo de caché distribuido
/// </summary>
public sealed class CacheService
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<CacheService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public CacheService(IDistributedCache cache, ILogger<CacheService> logger)
    {
        _cache = cache;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };
    }

    /// <summary>
    /// Genera una clave de caché basada en la petición
    /// </summary>
    public string GenerateCacheKey(TranslateRequest request)
    {
        var keyData = new
        {
            service = request.Service.ToLowerInvariant(),
            method = request.Method.ToUpperInvariant(),
            path = request.Path.ToLowerInvariant(),
            query = request.Query?.OrderBy(x => x.Key).ToArray(),
            // No incluimos headers sensibles en la clave
            headers = request.Headers?
                .Where(h => !IsSensitiveHeader(h.Key))
                .OrderBy(x => x.Key)
                .ToArray()
        };

        var json = JsonSerializer.Serialize(keyData, _jsonOptions);
        var hash = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(json));
        return Convert.ToBase64String(hash)[..16]; // Usamos solo 16 chars para la clave
    }

    /// <summary>
    /// Obtiene un valor del caché
    /// </summary>
    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var cachedValue = await _cache.GetStringAsync(key, cancellationToken);
            if (string.IsNullOrEmpty(cachedValue))
                return default;

            return JsonSerializer.Deserialize<T>(cachedValue, _jsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error retrieving from cache with key: {CacheKey}", key);
            return default;
        }
    }

    /// <summary>
    /// Almacena un valor en el caché
    /// </summary>
    public async Task SetAsync<T>(string key, T value, TimeSpan expiration, CancellationToken cancellationToken = default)
    {
        try
        {
            var json = JsonSerializer.Serialize(value, _jsonOptions);
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration
            };

            await _cache.SetStringAsync(key, json, options, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error storing to cache with key: {CacheKey}", key);
        }
    }

    /// <summary>
    /// Remueve un valor del caché
    /// </summary>
    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            await _cache.RemoveAsync(key, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error removing from cache with key: {CacheKey}", key);
        }
    }

    /// <summary>
    /// Invalida el caché para un servicio específico
    /// </summary>
    public Task InvalidateServiceCacheAsync(string service, CancellationToken cancellationToken = default)
    {
        // Nota: Esta es una implementación simplificada
        // En producción se podría usar Redis con patrones para invalidación masiva
        _logger.LogInformation("Cache invalidation requested for service: {Service}", service);
        // TODO: Implementar invalidación por patrón si es necesario
        return Task.CompletedTask;
    }

    private static bool IsSensitiveHeader(string headerName)
    {
        var sensitiveHeaders = new[]
        {
            "authorization",
            "cookie",
            "x-api-key",
            "x-auth-token"
        };

        return sensitiveHeaders.Contains(headerName.ToLowerInvariant());
    }
}