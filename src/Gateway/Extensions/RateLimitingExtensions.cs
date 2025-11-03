using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace Gateway.Extensions;

/// <summary>
/// Extensiones para configurar Rate Limiting
/// </summary>
public static class RateLimitingExtensions
{
    /// <summary>
    /// Configura políticas de rate limiting
    /// </summary>
    public static IServiceCollection AddGatewayRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(opt =>
        {
            // Política global (más restrictiva)
            opt.AddTokenBucketLimiter("global", o =>
            {
                o.TokenLimit = 100;
                o.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                o.QueueLimit = 200;
                o.ReplenishmentPeriod = TimeSpan.FromSeconds(1);
                o.TokensPerPeriod = 50;
                o.AutoReplenishment = true;
            });

            // Política para endpoints públicos (más permisiva)
            opt.AddTokenBucketLimiter("public", o =>
            {
                o.TokenLimit = 200;
                o.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                o.QueueLimit = 100;
                o.ReplenishmentPeriod = TimeSpan.FromSeconds(1);
                o.TokensPerPeriod = 100;
                o.AutoReplenishment = true;
            });
        });

        return services;
    }
}
