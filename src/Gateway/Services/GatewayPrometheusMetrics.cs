using Prometheus;

namespace Gateway.Services;

/// <summary>
/// Servicio para exponer métricas de Prometheus del Gateway
/// </summary>
public interface IGatewayPrometheusMetrics
{
    void RecordProxyRequest(string targetService, string method, int statusCode, double durationMs);
    void RecordCacheHit(string service, string endpoint);
    void RecordCacheMiss(string service, string endpoint);
    void RecordRateLimitHit(string endpoint);
    void RecordCircuitBreakerOpen(string service);
    void RecordCircuitBreakerClosed(string service);
    void IncrementActiveConnections(string service);
    void DecrementActiveConnections(string service);
}

public class GatewayPrometheusMetrics : IGatewayPrometheusMetrics
{
    // Contadores
    private static readonly Counter ProxyRequests = Metrics
        .CreateCounter(
            "gateway_proxy_requests_total",
            "Total number of proxy requests",
            new CounterConfiguration
            {
                LabelNames = new[] { "target_service", "method", "status_code" }
            });

    private static readonly Counter CacheHits = Metrics
        .CreateCounter(
            "gateway_cache_hits_total",
            "Total number of cache hits",
            new CounterConfiguration
            {
                LabelNames = new[] { "service", "endpoint" }
            });

    private static readonly Counter CacheMisses = Metrics
        .CreateCounter(
            "gateway_cache_misses_total",
            "Total number of cache misses",
            new CounterConfiguration
            {
                LabelNames = new[] { "service", "endpoint" }
            });

    private static readonly Counter RateLimitHits = Metrics
        .CreateCounter(
            "gateway_rate_limit_hits_total",
            "Total number of rate limit hits",
            new CounterConfiguration
            {
                LabelNames = new[] { "endpoint" }
            });

    private static readonly Counter CircuitBreakerOpens = Metrics
        .CreateCounter(
            "gateway_circuit_breaker_opens_total",
            "Total number of circuit breaker opens",
            new CounterConfiguration
            {
                LabelNames = new[] { "service" }
            });

    private static readonly Counter CircuitBreakerCloses = Metrics
        .CreateCounter(
            "gateway_circuit_breaker_closes_total",
            "Total number of circuit breaker closes",
            new CounterConfiguration
            {
                LabelNames = new[] { "service" }
            });

    // Histogramas para latencias
    private static readonly Histogram ProxyDuration = Metrics
        .CreateHistogram(
            "gateway_proxy_duration_milliseconds",
            "Duration of proxy requests in milliseconds",
            new HistogramConfiguration
            {
                LabelNames = new[] { "target_service", "method", "status_code" },
                Buckets = Histogram.ExponentialBuckets(10, 2, 10) // 10ms a ~10s
            });

    // Gauges para métricas instantáneas
    private static readonly Gauge ActiveConnections = Metrics
        .CreateGauge(
            "gateway_active_connections",
            "Current number of active connections to backend services",
            new GaugeConfiguration
            {
                LabelNames = new[] { "service" }
            });

    public void RecordProxyRequest(string targetService, string method, int statusCode, double durationMs)
    {
        ProxyRequests
            .WithLabels(targetService, method, statusCode.ToString())
            .Inc();

        ProxyDuration
            .WithLabels(targetService, method, statusCode.ToString())
            .Observe(durationMs);
    }

    public void RecordCacheHit(string service, string endpoint)
    {
        CacheHits
            .WithLabels(service, endpoint)
            .Inc();
    }

    public void RecordCacheMiss(string service, string endpoint)
    {
        CacheMisses
            .WithLabels(service, endpoint)
            .Inc();
    }

    public void RecordRateLimitHit(string endpoint)
    {
        RateLimitHits
            .WithLabels(endpoint)
            .Inc();
    }

    public void RecordCircuitBreakerOpen(string service)
    {
        CircuitBreakerOpens
            .WithLabels(service)
            .Inc();
    }

    public void RecordCircuitBreakerClosed(string service)
    {
        CircuitBreakerCloses
            .WithLabels(service)
            .Inc();
    }

    public void IncrementActiveConnections(string service)
    {
        ActiveConnections
            .WithLabels(service)
            .Inc();
    }

    public void DecrementActiveConnections(string service)
    {
        ActiveConnections
            .WithLabels(service)
            .Dec();
    }
}
