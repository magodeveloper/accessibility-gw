using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Gateway.Services;

/// <summary>
/// Health check personalizado para verificar la conectividad con los microservicios
/// </summary>
public sealed class ServiceHealthCheck : IHealthCheck
{
    private readonly HttpClient _httpClient;
    private readonly string _serviceName;
    private readonly string _serviceUrl;
    private readonly ILogger<ServiceHealthCheck> _logger;

    public ServiceHealthCheck(HttpClient httpClient, string serviceName, string serviceUrl, ILogger<ServiceHealthCheck> logger)
    {
        _httpClient = httpClient;
        _serviceName = serviceName;
        _serviceUrl = serviceUrl;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            // Intentar conexión con el servicio
            var response = await _httpClient.GetAsync($"{_serviceUrl}/health", cancellationToken);

            stopwatch.Stop();

            var data = new Dictionary<string, object>
            {
                ["service"] = _serviceName,
                ["url"] = _serviceUrl,
                ["responseTime"] = stopwatch.ElapsedMilliseconds,
                ["statusCode"] = (int)response.StatusCode
            };

            if (response.IsSuccessStatusCode)
            {
                return HealthCheckResult.Healthy($"Service {_serviceName} is healthy", data);
            }
            else
            {
                return HealthCheckResult.Unhealthy($"Service {_serviceName} returned {response.StatusCode}", null, data);
            }
        }
        catch (TaskCanceledException)
        {
            return HealthCheckResult.Unhealthy($"Service {_serviceName} timed out", null, new Dictionary<string, object>
            {
                ["service"] = _serviceName,
                ["url"] = _serviceUrl,
                ["error"] = "Timeout"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed for service {ServiceName}", _serviceName);
            return HealthCheckResult.Unhealthy($"Service {_serviceName} is unhealthy: {ex.Message}", ex, new Dictionary<string, object>
            {
                ["service"] = _serviceName,
                ["url"] = _serviceUrl,
                ["error"] = ex.Message
            });
        }
    }
}

/// <summary>
/// Factory para crear health checks de servicios
/// </summary>
public sealed class ServiceHealthCheckFactory
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ServiceHealthCheck> _logger;

    public ServiceHealthCheckFactory(IHttpClientFactory httpClientFactory, ILogger<ServiceHealthCheck> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public IHealthCheck Create(string serviceName, string serviceUrl)
    {
        var httpClient = _httpClientFactory.CreateClient($"health-{serviceName}");
        httpClient.Timeout = TimeSpan.FromSeconds(10);

        return new ServiceHealthCheck(httpClient, serviceName, serviceUrl, _logger);
    }
}
