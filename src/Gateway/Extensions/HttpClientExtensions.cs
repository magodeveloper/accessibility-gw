using Polly;
using System.Net;
using Gateway.Services;
using Gateway.Configuration;
using Polly.Extensions.Http;
using Microsoft.Extensions.Options;

namespace Gateway.Extensions;

/// <summary>
/// Extensiones para configurar HttpClient con políticas de resiliencia
/// </summary>
public static class HttpClientExtensions
{
    /// <summary>
    /// Configura HttpClient con políticas de resiliencia (Retry y Circuit Breaker)
    /// </summary>
    public static IServiceCollection AddResilientHttpClient(
        this IServiceCollection services)
    {
        // HttpClient básico
        services.AddHttpClient();
        services.AddHttpForwarder();

        // HttpClient con políticas de resiliencia
        services.AddHttpClient("DefaultClient", (sp, client) =>
        {
            var gateOptions = sp.GetRequiredService<IOptions<GateOptions>>().Value;
            client.Timeout = TimeSpan.FromSeconds(gateOptions.DefaultTimeoutSeconds);
        })
        .ConfigurePrimaryHttpMessageHandler(_ => new SocketsHttpHandler())
        .AddPolicyHandler(GetRetryPolicy())
        .AddPolicyHandler(GetCircuitBreakerPolicy());

        Console.WriteLine("HttpClient with resilience policies configured");
        return services;
    }

    /// <summary>
    /// Configura servicios personalizados del Gateway
    /// </summary>
    public static IServiceCollection AddGatewayServices(this IServiceCollection services)
    {
        // HttpMessageInvoker singleton
        services.AddSingleton<HttpMessageInvoker>(provider =>
        {
            var gateOptions = provider.GetRequiredService<IOptions<GateOptions>>().Value;
            var handler = new SocketsHttpHandler
            {
                ConnectTimeout = TimeSpan.FromSeconds(gateOptions.DefaultTimeoutSeconds)
            };
            return new HttpMessageInvoker(handler);
        });

        // Servicios del Gateway
        services.AddSingleton<IResiliencePolicyService, ResiliencePolicyService>();
        services.AddSingleton<RequestTranslator>();
        services.AddSingleton<ICacheService, CacheService>();
        services.AddSingleton<IMetricsService, MetricsService>();
        services.AddSingleton<IGatewayPrometheusMetrics, GatewayPrometheusMetrics>();
        services.AddSingleton<ServiceHealthCheckFactory>();
        services.AddScoped<ProxyHttpClient>(); // Para controllers proxy

        return services;
    }

    /// <summary>
    /// Política de reintentos con backoff exponencial
    /// </summary>
    private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(msg => msg.StatusCode == HttpStatusCode.TooManyRequests)
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    Console.WriteLine($"Retry {retryCount} after {timespan.TotalSeconds}s due to: {outcome.Exception?.Message ?? outcome.Result.StatusCode.ToString()}");
                });
    }

    /// <summary>
    /// Política de Circuit Breaker
    /// </summary>
    private static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: 5,
                durationOfBreak: TimeSpan.FromSeconds(30),
                onBreak: (outcome, duration) =>
                {
                    Console.WriteLine($"Circuit breaker opened for {duration.TotalSeconds}s due to: {outcome.Exception?.Message ?? outcome.Result.StatusCode.ToString()}");
                },
                onReset: () =>
                {
                    Console.WriteLine("Circuit breaker reset");
                });
    }
}
