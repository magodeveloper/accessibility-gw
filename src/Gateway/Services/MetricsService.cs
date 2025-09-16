using System.Diagnostics;

namespace Gateway.Services;

/// <summary>
/// Servicio para manejo de métricas y telemetría
/// </summary>
public sealed class MetricsService : IMetricsService
{
    private readonly ILogger<MetricsService> _logger;
    private static readonly ActivitySource ActivitySource = new("AccessibilityGateway");

    // Contadores para métricas
    private long _totalRequests;
    private long _successfulRequests;
    private long _failedRequests;
    private long _cachedRequests;
    private readonly Dictionary<string, long> _requestsByService = new();
    private readonly Dictionary<int, long> _requestsByStatusCode = new();
    private readonly Dictionary<string, double> _averageResponseTimes = new();

    public MetricsService(ILogger<MetricsService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Inicia el tracking de una petición
    /// </summary>
    public Activity? StartActivity(string operationName, string service, string method, string path)
    {
        var activity = ActivitySource.StartActivity(operationName);
        activity?.SetTag("gateway.service", service);
        activity?.SetTag("gateway.method", method);
        activity?.SetTag("gateway.path", path);
        activity?.SetTag("gateway.timestamp", DateTimeOffset.UtcNow.ToString("O"));

        return activity;
    }

    /// <summary>
    /// Registra una petición completada
    /// </summary>
    public void RecordRequest(string service, string method, int statusCode, double responseTimeMs, bool fromCache = false)
    {
        Interlocked.Increment(ref _totalRequests);

        if (fromCache)
        {
            Interlocked.Increment(ref _cachedRequests);
        }

        if (statusCode >= 200 && statusCode < 400)
        {
            Interlocked.Increment(ref _successfulRequests);
        }
        else
        {
            Interlocked.Increment(ref _failedRequests);
        }

        // Registrar por servicio
        lock (_requestsByService)
        {
            if (!_requestsByService.ContainsKey(service))
                _requestsByService[service] = 0;
            _requestsByService[service]++;
        }

        // Registrar por código de estado
        lock (_requestsByStatusCode)
        {
            if (!_requestsByStatusCode.ContainsKey(statusCode))
                _requestsByStatusCode[statusCode] = 0;
            _requestsByStatusCode[statusCode]++;
        }

        // Actualizar tiempo promedio de respuesta
        var key = $"{service}:{method}";
        lock (_averageResponseTimes)
        {
            if (!_averageResponseTimes.ContainsKey(key))
            {
                _averageResponseTimes[key] = responseTimeMs;
            }
            else
            {
                // Media móvil simple
                _averageResponseTimes[key] = (_averageResponseTimes[key] * 0.8) + (responseTimeMs * 0.2);
            }
        }

        _logger.LogDebug("Request recorded: {Service} {Method} -> {StatusCode} in {ResponseTime}ms (Cache: {FromCache})",
            service, method, statusCode, responseTimeMs, fromCache);
    }

    /// <summary>
    /// Obtiene las métricas actuales
    /// </summary>
    public Dictionary<string, object> GetMetrics()
    {
        var metrics = new Dictionary<string, object>
        {
            ["totalRequests"] = _totalRequests,
            ["successfulRequests"] = _successfulRequests,
            ["failedRequests"] = _failedRequests,
            ["cachedRequests"] = _cachedRequests,
            ["successRate"] = _totalRequests > 0 ? (double)_successfulRequests / _totalRequests : 0.0,
            ["cacheHitRate"] = _totalRequests > 0 ? (double)_cachedRequests / _totalRequests : 0.0,
            ["timestamp"] = DateTimeOffset.UtcNow
        };

        lock (_requestsByService)
        {
            metrics["requestsByService"] = new Dictionary<string, long>(_requestsByService);
        }

        lock (_requestsByStatusCode)
        {
            metrics["requestsByStatusCode"] = new Dictionary<int, long>(_requestsByStatusCode);
        }

        lock (_averageResponseTimes)
        {
            metrics["averageResponseTimes"] = new Dictionary<string, double>(_averageResponseTimes);
        }

        return metrics;
    }

    /// <summary>
    /// Resetea todas las métricas
    /// </summary>
    public void ResetMetrics()
    {
        Interlocked.Exchange(ref _totalRequests, 0);
        Interlocked.Exchange(ref _successfulRequests, 0);
        Interlocked.Exchange(ref _failedRequests, 0);
        Interlocked.Exchange(ref _cachedRequests, 0);

        lock (_requestsByService)
        {
            _requestsByService.Clear();
        }

        lock (_requestsByStatusCode)
        {
            _requestsByStatusCode.Clear();
        }

        lock (_averageResponseTimes)
        {
            _averageResponseTimes.Clear();
        }

        _logger.LogInformation("Metrics reset completed");
    }
}