using Gateway.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Caching.Distributed;

namespace Gateway.Extensions;

/// <summary>
/// Extensiones para configurar caché distribuido
/// </summary>
public static class CacheExtensions
{
    /// <summary>
    /// Configura caché distribuido (Redis o Memory)
    /// </summary>
    public static IServiceCollection AddGatewayCache(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var redisConnectionString = configuration.GetSection("Redis")["ConnectionString"];

        if (!string.IsNullOrEmpty(redisConnectionString))
        {
            // Usar Redis como caché distribuido
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisConnectionString;
                options.InstanceName = "AccessibilityGateway";
            });

            Console.WriteLine($"Distributed cache configured with Redis: {redisConnectionString}");
        }
        else
        {
            // Fallback a Memory Cache
            services.AddMemoryCache();
            services.AddSingleton<IDistributedCache, MemoryDistributedCache>();

            Console.WriteLine("Redis not configured. Using in-memory distributed cache (not recommended for production)");
        }

        // Output Cache
        services.AddOutputCache(o =>
        {
            o.AddBasePolicy(b => b.Expire(TimeSpan.FromSeconds(10)));
            o.AddPolicy("LongCache", b => b.Expire(TimeSpan.FromMinutes(5)));
        });

        return services;
    }
}
