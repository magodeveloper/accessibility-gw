using Gateway.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Gateway.Extensions;

/// <summary>
/// Extensiones para configurar Health Checks
/// </summary>
public static class HealthChecksExtensions
{
    /// <summary>
    /// Configura health checks para el Gateway y microservicios
    /// </summary>
    public static IServiceCollection AddGatewayHealthChecks(
        this IServiceCollection services,
        IConfiguration configuration,
        string? redisConnectionString)
    {
        var healthChecksBuilder = services.AddHealthChecks();

        // Health check básico
        healthChecksBuilder.AddCheck("self", () => HealthCheckResult.Healthy(), new[] { "live" });

        // Configuración de health checks
        var healthChecksConfig = configuration.GetSection("HealthChecks").Get<HealthChecksOptions>();
        var healthTimeout = healthChecksConfig?.UnhealthyTimeoutSeconds ?? 10;

        // Health checks para microservicios desde Gate.Services
        var servicesConfig = configuration.GetSection("Gate:Services").Get<Dictionary<string, string>>();
        if (servicesConfig != null)
        {
            foreach (var service in servicesConfig)
            {
                var serviceName = service.Key;
                var serviceUrl = service.Value;
                var healthUrl = $"{serviceUrl}/health";

                // IMPORTANTE: Usamos Degraded en vez de Unhealthy para que no detenga el Gateway
                healthChecksBuilder.AddUrlGroup(
                    new Uri(healthUrl),
                    name: $"{serviceName}-health",
                    failureStatus: HealthStatus.Degraded,
                    tags: new[] { "microservice", serviceName, "ready" },
                    timeout: TimeSpan.FromSeconds(healthTimeout),
                    configurePrimaryHttpMessageHandler: _ => new SocketsHttpHandler
                    {
                        ConnectTimeout = TimeSpan.FromSeconds(healthTimeout),
                        PooledConnectionLifetime = TimeSpan.FromMinutes(2)
                    },
                    configureClient: (_, client) =>
                    {
                        client.Timeout = TimeSpan.FromSeconds(healthTimeout);
                        client.DefaultRequestHeaders.Add("User-Agent", "Gateway-HealthCheck/1.0");
                    }
                );

                Console.WriteLine($"Registered health check for {serviceName} at {healthUrl}");
            }
        }

        // Health check para Redis si está configurado
        if (!string.IsNullOrEmpty(redisConnectionString))
        {
            healthChecksBuilder.AddRedis(
                redisConnectionString,
                name: "redis",
                failureStatus: HealthStatus.Degraded,
                tags: new[] { "ready" }
            );
        }

        // UI de Health Checks
        services.AddHealthChecksUI(opt =>
        {
            var healthChecksConfig = configuration.GetSection("HealthChecks").Get<HealthChecksOptions>();
            opt.SetEvaluationTimeInSeconds(healthChecksConfig?.CheckIntervalSeconds ?? 60);
            opt.MaximumHistoryEntriesPerEndpoint(25);
            opt.SetApiMaxActiveRequests(1); // Evitar "Sequence contains more than one element"
            opt.AddHealthCheckEndpoint("Gateway API", "/health");

            // Agregar health checks de microservicios dinámicamente
            var servicesConfig = configuration.GetSection("Gate:Services").Get<Dictionary<string, string>>();
            if (servicesConfig != null)
            {
                foreach (var service in servicesConfig)
                {
                    var serviceName = service.Key;
                    var serviceUrl = service.Value;
                    var healthUrl = $"{serviceUrl}/health";

                    opt.AddHealthCheckEndpoint($"{serviceName} - Health", healthUrl);

                    // Liveness y readiness si el servicio los soporta
                    opt.AddHealthCheckEndpoint($"{serviceName} - Liveness", $"{serviceUrl}/health/live");
                    opt.AddHealthCheckEndpoint($"{serviceName} - Readiness", $"{serviceUrl}/health/ready");
                }
            }

            opt.SetMinimumSecondsBetweenFailureNotifications(300); // 5 minutos
        })
        .AddInMemoryStorage();

        return services;
    }
}
