using Polly;
using Polly.Extensions.Http;
using Polly.CircuitBreaker;
using Polly.Timeout;
using System.Net;

namespace Gateway.Services;

/// <summary>
/// Configuración de políticas de resiliencia para diferentes tipos de servicios
/// </summary>
public class ResiliencePolicyConfig
{
    public int RetryCount { get; set; } = 3;
    public TimeSpan BaseDelay { get; set; } = TimeSpan.FromMilliseconds(100);
    public TimeSpan MaxDelay { get; set; } = TimeSpan.FromSeconds(30);
    public TimeSpan TimeoutPerTry { get; set; } = TimeSpan.FromSeconds(10);
    public TimeSpan OverallTimeout { get; set; } = TimeSpan.FromSeconds(30);

    // Circuit Breaker
    public int CircuitBreakerThreshold { get; set; } = 5;
    public TimeSpan CircuitBreakerDuration { get; set; } = TimeSpan.FromSeconds(30);
    public int CircuitBreakerSamplingDuration { get; set; } = 60;

    // Jitter para evitar thundering herd
    public bool UseJitter { get; set; } = true;

    // Códigos HTTP que deben causar retry
    public HttpStatusCode[] RetryStatusCodes { get; set; } =
    {
        HttpStatusCode.RequestTimeout,
        HttpStatusCode.InternalServerError,
        HttpStatusCode.BadGateway,
        HttpStatusCode.ServiceUnavailable,
        HttpStatusCode.GatewayTimeout,
        HttpStatusCode.TooManyRequests
    };
}

/// <summary>
/// Servicio para crear y gestionar políticas de resiliencia
/// </summary>
public interface IResiliencePolicyService
{
    /// <summary>
    /// Obtiene la política combinada para un servicio específico
    /// </summary>
    IAsyncPolicy<HttpResponseMessage> GetPolicyForService(string serviceName);

    /// <summary>
    /// Obtiene configuración específica para un servicio
    /// </summary>
    ResiliencePolicyConfig GetConfigForService(string serviceName);

    /// <summary>
    /// Registra métricas de políticas de resiliencia
    /// </summary>
    void RecordPolicyExecution(string serviceName, string policyType, bool success, TimeSpan duration);
}

/// <summary>
/// Implementación del servicio de políticas de resiliencia
/// </summary>
public class ResiliencePolicyService : IResiliencePolicyService
{
    private readonly ILogger<ResiliencePolicyService> _logger;
    private readonly IMetricsService _metricsService;
    private readonly IConfiguration _configuration;
    private readonly Dictionary<string, IAsyncPolicy<HttpResponseMessage>> _policies;
    private readonly Dictionary<string, ResiliencePolicyConfig> _configs;

    public ResiliencePolicyService(
        ILogger<ResiliencePolicyService> logger,
        IMetricsService metricsService,
        IConfiguration configuration)
    {
        _logger = logger;
        _metricsService = metricsService;
        _configuration = configuration;
        _policies = new Dictionary<string, IAsyncPolicy<HttpResponseMessage>>();
        _configs = new Dictionary<string, ResiliencePolicyConfig>();

        InitializePolicies();
    }

    private void InitializePolicies()
    {
        // Configuraciones por defecto para diferentes tipos de servicios
        var defaultConfig = new ResiliencePolicyConfig();

        // Configuración para servicios críticos (más agresiva)
        var criticalConfig = new ResiliencePolicyConfig
        {
            RetryCount = 5,
            BaseDelay = TimeSpan.FromMilliseconds(50),
            MaxDelay = TimeSpan.FromSeconds(10),
            TimeoutPerTry = TimeSpan.FromSeconds(15),
            OverallTimeout = TimeSpan.FromSeconds(45),
            CircuitBreakerThreshold = 3,
            CircuitBreakerDuration = TimeSpan.FromSeconds(60)
        };

        // Configuración para servicios de análisis (más tolerante)
        var analysisConfig = new ResiliencePolicyConfig
        {
            RetryCount = 2,
            BaseDelay = TimeSpan.FromMilliseconds(200),
            MaxDelay = TimeSpan.FromSeconds(60),
            TimeoutPerTry = TimeSpan.FromSeconds(30),
            OverallTimeout = TimeSpan.FromMinutes(2),
            CircuitBreakerThreshold = 8,
            CircuitBreakerDuration = TimeSpan.FromMinutes(2)
        };

        // Asignar configuraciones por servicio
        _configs["users"] = criticalConfig;
        _configs["reports"] = defaultConfig;
        _configs["analysis"] = analysisConfig;
        _configs["middleware"] = analysisConfig;
        _configs["default"] = defaultConfig;

        // Crear políticas para cada servicio
        foreach (var config in _configs)
        {
            _policies[config.Key] = CreateCombinedPolicy(config.Key, config.Value);
        }
    }

    private IAsyncPolicy<HttpResponseMessage> CreateCombinedPolicy(string serviceName, ResiliencePolicyConfig config)
    {
        // 1. Política de Timeout por intento
        var timeoutPerTryPolicy = Policy.TimeoutAsync<HttpResponseMessage>(
            config.TimeoutPerTry,
            Polly.Timeout.TimeoutStrategy.Pessimistic,
            onTimeoutAsync: async (context, timeout, task) =>
            {
                _logger.LogWarning("Timeout per try reached for service {ServiceName} after {Timeout}ms",
                    serviceName, timeout.TotalMilliseconds);
                RecordPolicyExecution(serviceName, "TimeoutPerTry", false, timeout);
                await Task.CompletedTask;
            });

        // 2. Política de Retry con backoff exponencial y jitter
        var retryPolicy = Policy
            .HandleResult<HttpResponseMessage>(r => config.RetryStatusCodes.Contains(r.StatusCode))
            .Or<HttpRequestException>()
            .Or<TaskCanceledException>()
            .Or<TimeoutRejectedException>()
            .WaitAndRetryAsync(
                retryCount: config.RetryCount,
                sleepDurationProvider: retryAttempt =>
                {
                    var delay = TimeSpan.FromMilliseconds(Math.Pow(2, retryAttempt) * config.BaseDelay.TotalMilliseconds);

                    // Aplicar jitter para evitar thundering herd
                    if (config.UseJitter)
                    {
                        var jitter = Random.Shared.NextDouble() * 0.1; // ±10%
                        delay = TimeSpan.FromMilliseconds(delay.TotalMilliseconds * (1 + jitter));
                    }

                    // Respetar el delay máximo
                    return delay > config.MaxDelay ? config.MaxDelay : delay;
                },
                onRetryAsync: async (outcome, timespan, retryCount, context) =>
                {
                    _logger.LogWarning("Retrying request to service {ServiceName}. Attempt {RetryCount}/{MaxRetries}. Delay: {Delay}ms. Reason: {Reason}",
                        serviceName, retryCount, config.RetryCount, timespan.TotalMilliseconds,
                        outcome.Exception?.Message ?? outcome.Result?.StatusCode.ToString());

                    RecordPolicyExecution(serviceName, "Retry", false, timespan);
                    await Task.CompletedTask;
                });

        // 3. Circuit Breaker
        var circuitBreakerPolicy = Policy
            .HandleResult<HttpResponseMessage>(r =>
                r.StatusCode == HttpStatusCode.InternalServerError ||
                r.StatusCode == HttpStatusCode.BadGateway ||
                r.StatusCode == HttpStatusCode.ServiceUnavailable ||
                r.StatusCode == HttpStatusCode.GatewayTimeout)
            .Or<HttpRequestException>()
            .Or<TaskCanceledException>()
            .Or<TimeoutRejectedException>()
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: config.CircuitBreakerThreshold,
                durationOfBreak: config.CircuitBreakerDuration,
                onBreak: (exception, duration) =>
                {
                    _logger.LogError("Circuit breaker opened for service {ServiceName} for {Duration}ms. Reason: {Reason}",
                        serviceName, duration.TotalMilliseconds,
                        exception.Exception?.Message ?? exception.Result?.StatusCode.ToString());
                    RecordPolicyExecution(serviceName, "CircuitBreakerOpen", false, duration);
                },
                onReset: () =>
                {
                    _logger.LogInformation("Circuit breaker reset for service {ServiceName}", serviceName);
                    RecordPolicyExecution(serviceName, "CircuitBreakerReset", true, TimeSpan.Zero);
                },
                onHalfOpen: () =>
                {
                    _logger.LogInformation("Circuit breaker half-open for service {ServiceName}", serviceName);
                    RecordPolicyExecution(serviceName, "CircuitBreakerHalfOpen", true, TimeSpan.Zero);
                });

        // 4. Timeout general
        var overallTimeoutPolicy = Policy.TimeoutAsync<HttpResponseMessage>(
            config.OverallTimeout,
            Polly.Timeout.TimeoutStrategy.Pessimistic,
            onTimeoutAsync: async (context, timeout, task) =>
            {
                _logger.LogError("Overall timeout reached for service {ServiceName} after {Timeout}ms",
                    serviceName, timeout.TotalMilliseconds);
                RecordPolicyExecution(serviceName, "OverallTimeout", false, timeout);
                await Task.CompletedTask;
            });

        // Combinar todas las políticas en el orden correcto:
        // OverallTimeout -> CircuitBreaker -> Retry -> TimeoutPerTry
        return Policy.WrapAsync(overallTimeoutPolicy, circuitBreakerPolicy, retryPolicy, timeoutPerTryPolicy);
    }

    public IAsyncPolicy<HttpResponseMessage> GetPolicyForService(string serviceName)
    {
        return _policies.TryGetValue(serviceName, out var policy)
            ? policy
            : _policies["default"];
    }

    public ResiliencePolicyConfig GetConfigForService(string serviceName)
    {
        return _configs.TryGetValue(serviceName, out var config)
            ? config
            : _configs["default"];
    }

    public void RecordPolicyExecution(string serviceName, string policyType, bool success, TimeSpan duration)
    {
        try
        {
            _metricsService.RecordResiliencePolicyExecution(serviceName, policyType, success, duration);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording resilience policy metrics for service {ServiceName}", serviceName);
        }
    }
}

/// <summary>
/// Extensiones para métricas de políticas de resiliencia
/// </summary>
public static class MetricsServiceExtensions
{
    public static void RecordResiliencePolicyExecution(this IMetricsService metricsService,
        string serviceName, string policyType, bool success, TimeSpan duration)
    {
        // Esta extensión se implementará cuando tengamos el sistema de métricas completo
        // Por ahora es un placeholder para futuras implementaciones
    }
}