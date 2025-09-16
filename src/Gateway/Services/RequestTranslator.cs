using System.Net;
using System.Text;
using Gateway.Models;
using System.Text.Json;
using System.Diagnostics;
using System.Net.Http.Headers;
using Yarp.ReverseProxy.Forwarder;
using Yarp.ReverseProxy.Transforms;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http.Extensions;

namespace Gateway.Services;

public sealed class RequestTranslator
{
    private static readonly HashSet<string> _allowedMethods =
        new(new[] { "GET", "POST", "PUT", "PATCH", "DELETE" }, StringComparer.OrdinalIgnoreCase);

    private readonly GateOptions _opts;
    private readonly IHttpForwarder _forwarder;
    private readonly HttpMessageInvoker _httpClient;
    private readonly ICacheService _cacheService;
    private readonly IMetricsService _metricsService;
    private readonly IResiliencePolicyService _resiliencePolicyService;
    private readonly ILogger<RequestTranslator> _logger;
    private readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    public RequestTranslator(
        IOptions<GateOptions> opts,
        IHttpForwarder forwarder,
        HttpMessageInvoker httpClient,
    ICacheService cacheService,
    IMetricsService metricsService,
        IResiliencePolicyService resiliencePolicyService,
        ILogger<RequestTranslator> logger)
    {
        _opts = opts.Value;
        _forwarder = forwarder;
        _httpClient = httpClient;
        _cacheService = cacheService;
        _metricsService = metricsService;
        _resiliencePolicyService = resiliencePolicyService;
        _logger = logger;
    }

    public bool IsAllowed(TranslateRequest req)
    {
        if (!_allowedMethods.Contains(req.Method))
        {
            _logger.LogWarning("Method {Method} not allowed", req.Method);
            return false;
        }

        if (!_opts.Services.ContainsKey(req.Service))
        {
            _logger.LogWarning("Service {Service} not configured", req.Service);
            return false;
        }

        var matched = _opts.AllowedRoutes.Any(a =>
            string.Equals(a.Service, req.Service, StringComparison.OrdinalIgnoreCase) &&
            a.Methods.Any(m => string.Equals(m, req.Method, StringComparison.OrdinalIgnoreCase)) &&
            req.Path.StartsWith(a.PathPrefix, StringComparison.OrdinalIgnoreCase));

        if (!matched)
        {
            _logger.LogWarning("Route {Service}:{Method}:{Path} not allowed by ACL", req.Service, req.Method, req.Path);
        }

        return matched;
    }

    public async Task<TranslateResult> ProcessRequestAsync(HttpContext context, TranslateRequest req, CancellationToken ct)
    {
        using var activity = _metricsService.StartActivity("gateway.request", req.Service, req.Method, req.Path);
        var stopwatch = Stopwatch.StartNew();

        try
        {
            _logger.LogInformation("Processing request {Service}:{Method}:{Path}", req.Service, req.Method, req.Path);

            // Verificar caché para peticiones GET
            if (req.Method.Equals("GET", StringComparison.OrdinalIgnoreCase) &&
                _opts.EnableCaching &&
                req.UseCache != false)
            {
                var cacheKey = _cacheService.GenerateCacheKey(req);
                var cachedResult = await _cacheService.GetAsync<TranslateResult>(cacheKey, ct);

                if (cachedResult?.Response != null)
                {
                    stopwatch.Stop();
                    _metricsService.RecordRequest(req.Service, req.Method, cachedResult.Response.StatusCode, stopwatch.Elapsed.TotalMilliseconds, true);

                    var cachedResponse = cachedResult.Response with
                    {
                        FromCache = true,
                        ProcessingTimeMs = stopwatch.ElapsedMilliseconds
                    };

                    _logger.LogDebug("Serving cached response for {Service}:{Method}:{Path}", req.Service, req.Method, req.Path);
                    return new TranslateResult { Response = cachedResponse };
                }
            }

            // Procesar petición
            var result = await ForwardRequestAsync(context, req, ct);
            stopwatch.Stop();

            // Guardar en caché si es exitoso y es GET
            if (result.Success &&
                req.Method.Equals("GET", StringComparison.OrdinalIgnoreCase) &&
                _opts.EnableCaching &&
                req.UseCache != false &&
                result.Response!.StatusCode >= 200 && result.Response.StatusCode < 300)
            {
                var cacheKey = _cacheService.GenerateCacheKey(req);
                var expiration = TimeSpan.FromMinutes(req.CacheExpirationMinutes ?? _opts.CacheExpirationMinutes);
                await _cacheService.SetAsync(cacheKey, result, expiration, ct);
            }

            // Registrar métricas
            var statusCode = result.Response?.StatusCode ?? result.Error?.StatusCode ?? 500;
            _metricsService.RecordRequest(req.Service, req.Method, statusCode, stopwatch.Elapsed.TotalMilliseconds);

            if (result.Response != null)
            {
                result = result with
                {
                    Response = result.Response with
                    {
                        ProcessingTimeMs = stopwatch.ElapsedMilliseconds,
                        ProcessedByService = req.Service
                    }
                };
            }

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Error processing request {Service}:{Method}:{Path}", req.Service, req.Method, req.Path);

            _metricsService.RecordRequest(req.Service, req.Method, 500, stopwatch.Elapsed.TotalMilliseconds);

            return new TranslateResult
            {
                Error = new TranslateError
                {
                    Message = "Internal gateway error",
                    StatusCode = 500,
                    Details = ex.Message,
                    CorrelationId = Activity.Current?.Id ?? Guid.NewGuid().ToString()
                }
            };
        }
    }

    public async Task ForwardAsync(HttpContext context, TranslateRequest req, CancellationToken ct)
    {
        using var activity = _metricsService.StartActivity("gateway.forward", req.Service, req.Method, req.Path);
        var stopwatch = Stopwatch.StartNew();

        try
        {
            _logger.LogInformation("Forwarding request {Service}:{Method}:{Path}", req.Service, req.Method, req.Path);

            // Log políticas de resiliencia configuradas
            var config = _resiliencePolicyService.GetConfigForService(req.Service);
            _logger.LogDebug("Using resilience config for {Service}: RetryCount={RetryCount}, Timeout={Timeout}s",
                req.Service, config.RetryCount, config.OverallTimeout.TotalSeconds);

            // Llamar directamente al método que maneja YARP (mantiene compatibilidad)
            var result = await ForwardRequestAsync(context, req, ct);

            stopwatch.Stop();

            _logger.LogInformation("ForwardRequestAsync result: Error={Error}, Response={Response}, ResponseStatusCode={StatusCode}",
                result.Error?.Message, result.Response != null, result.Response?.StatusCode);

            if (result.Error != null)
            {
                context.Response.StatusCode = result.Error.StatusCode ?? 500;
                context.Response.ContentType = "application/json";

                if (result.Error.Headers != null)
                {
                    foreach (var header in result.Error.Headers)
                    {
                        context.Response.Headers[header.Key] = header.Value;
                    }
                }

                var errorResponse = new { error = result.Error };
                await context.Response.WriteAsync(JsonSerializer.Serialize(errorResponse, _json), ct);

                _metricsService.RecordRequest(req.Service, req.Method, result.Error.StatusCode ?? 500, stopwatch.Elapsed.TotalMilliseconds);
            }
            else if (result.Response != null)
            {
                // Caso exitoso: la respuesta ya fue escrita por YARP en ForwardRequestAsync
                // El StatusCode ya debe estar establecido por YARP, pero si no es válido, usar el del resultado
                if (context.Response.StatusCode == 0 || !IsValidStatusCode(context.Response.StatusCode))
                {
                    context.Response.StatusCode = result.Response.StatusCode;
                }

                _logger.LogInformation("Request forwarded successfully {Service}:{Method}:{Path}", req.Service, req.Method, req.Path);
                _metricsService.RecordRequest(req.Service, req.Method, context.Response.StatusCode, stopwatch.Elapsed.TotalMilliseconds);
            }
            else
            {
                // Caso donde no hay error ni response - algo inesperado
                context.Response.StatusCode = 500;
                context.Response.ContentType = "application/json";
                var errorResponse = new { error = new { message = "Unexpected empty result from ForwardRequestAsync" } };
                await context.Response.WriteAsync(JsonSerializer.Serialize(errorResponse, _json), ct);
                _metricsService.RecordRequest(req.Service, req.Method, 500, stopwatch.Elapsed.TotalMilliseconds);
            }
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            Console.WriteLine($"=== EXCEPTION IN FORWARDASYNC === {ex.Message}");
            Console.WriteLine($"=== EXCEPTION STACK TRACE === {ex.StackTrace}");
            _logger.LogError(ex, "Error forwarding request {Service}:{Method}:{Path}", req.Service, req.Method, req.Path);

            context.Response.StatusCode = 500;
            context.Response.ContentType = "application/json";
            var errorResponse = new { error = new { message = "Gateway forwarding error", details = ex.Message } };
            await context.Response.WriteAsync(JsonSerializer.Serialize(errorResponse, _json), ct);

            _metricsService.RecordRequest(req.Service, req.Method, 500, stopwatch.Elapsed.TotalMilliseconds);
        }
    }

    private async Task<TranslateResult> ForwardRequestAsync(HttpContext context, TranslateRequest req, CancellationToken ct)
    {
        var baseUrl = _opts.Services[req.Service].TrimEnd('/');
        var targetUri = new Uri(BuildTargetUrl(baseUrl, req.Path, req.Query));

        Console.WriteLine($"=== FORWARD REQUEST DEBUG ===");
        Console.WriteLine($"Service: {req.Service}");
        Console.WriteLine($"BaseURL: {baseUrl}");
        Console.WriteLine($"Path: {req.Path}");
        Console.WriteLine($"TargetURI: {targetUri}");

        // Crear un transformer personalizado para preservar el header Host
        // Extraer el host de la URL del servicio desde la configuración
        var actualServiceUrl = new Uri(_opts.Services[req.Service]);
        var actualExpectedHost = actualServiceUrl.Authority;

        Console.WriteLine($"=== SERVICE URL === {_opts.Services[req.Service]}");
        Console.WriteLine($"=== EXPECTED HOST === {actualExpectedHost}");

        var act = req.Method.ToUpperInvariant();

        // Preparar el body como string si existe
        string? bodyString = null;
        if (req.Body != null && act != "GET" && act != "DELETE")
        {
            if (req.Body is string stringBody)
            {
                bodyString = stringBody;
            }
            else
            {
                bodyString = JsonSerializer.Serialize(req.Body, _json);
            }
            Console.WriteLine($"=== BODY PREPARED FOR TRANSFORMER === Length: {bodyString.Length}");
        }

        var transformer = new CustomHostTransformer(actualExpectedHost, targetUri.ToString(), bodyString);

        // Limpiar headers conflictivos
        context.Request.Headers.Remove("Host");

        // Copiar headers permitidos desde el payload
        if (req.Headers is not null)
        {
            foreach (var kv in req.Headers)
            {
                if (!IsSensitiveOrForbiddenHeader(kv.Key))
                {
                    context.Request.Headers[kv.Key] = kv.Value;
                }
            }
        }

        // Establecer correlation ID si no existe
        if (!context.Request.Headers.ContainsKey("X-Correlation-ID"))
        {
            context.Request.Headers["X-Correlation-ID"] = Activity.Current?.Id ?? Guid.NewGuid().ToString();
        }

        // Sustituir método HTTP
        context.Features.Get<IHttpRequestFeature>()!.Method = act;

        // Para métodos GET y DELETE, limpiar el body
        if (act is "GET" or "DELETE")
        {
            context.Request.Body = Stream.Null;
            context.Request.ContentLength = 0;
        }
        // Para otros métodos, el body será manejado por el CustomHostTransformer

        var requestConfig = new ForwarderRequestConfig
        {
            ActivityTimeout = TimeSpan.FromSeconds(_opts.DefaultTimeoutSeconds)
        };

        Console.WriteLine($"=== CALLING YARP FORWARDER ===");
        Console.WriteLine($"TargetURI: {targetUri}");

        var error = await _forwarder.SendAsync(context, targetUri.ToString(), _httpClient, requestConfig, transformer);

        Console.WriteLine($"=== YARP FORWARDER RESULT ===");
        Console.WriteLine($"Error: {error}");
        Console.WriteLine($"Response StatusCode: {context.Response.StatusCode}");
        Console.WriteLine($"Response Headers: {string.Join(", ", context.Response.Headers.Select(h => $"{h.Key}:{h.Value}"))}");

        _logger.LogInformation("YARP SendAsync result: Error={Error}, ContextStatusCode={StatusCode}", error, context.Response.StatusCode);

        if (error != ForwarderError.None)
        {
            var status = error switch
            {
                ForwarderError.Request => HttpStatusCode.BadRequest,
                ForwarderError.RequestTimedOut => HttpStatusCode.GatewayTimeout,
                ForwarderError.NoAvailableDestinations => HttpStatusCode.BadGateway,
                _ => HttpStatusCode.BadGateway
            };

            return new TranslateResult
            {
                Error = new TranslateError
                {
                    Message = $"Gateway forwarding error: {error}",
                    StatusCode = (int)status,
                    CorrelationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault()
                }
            };
        }

        // Si llegamos aquí, la petición fue exitosa
        return new TranslateResult
        {
            Response = new TranslateResponse
            {
                StatusCode = context.Response.StatusCode,
                Headers = context.Response.Headers.ToDictionary(h => h.Key, h => h.Value.ToString()),
                ProcessingTimeMs = 0 // Se actualizará en ProcessRequestAsync
            }
        };
    }

    private static string BuildTargetUrl(string baseUrl, string path, IDictionary<string, string>? query)
    {
        var b = new UriBuilder($"{baseUrl}{path}");
        if (query is { Count: > 0 })
        {
            var qb = new QueryBuilder(query);
            b.Query = qb.ToQueryString().Value;
        }
        return b.Uri.ToString();
    }

    private static bool IsSensitiveOrForbiddenHeader(string headerName)
    {
        var forbiddenHeaders = new[]
        {
            "host",
            "content-length",
            "transfer-encoding",
            "connection",
            "upgrade",
            "proxy-connection",
            "proxy-authenticate",
            "proxy-authorization",
            "te",
            "trailer"
        };

        return forbiddenHeaders.Contains(headerName.ToLowerInvariant());
    }

    private static bool IsValidStatusCode(int statusCode)
    {
        return statusCode >= 100 && statusCode < 600;
    }
}

public class CustomHostTransformer : HttpTransformer
{
    private readonly string _expectedHost;
    private readonly string _targetUri;
    private readonly string? _requestBody;

    public CustomHostTransformer(string expectedHost, string targetUri, string? requestBody = null)
    {
        _expectedHost = expectedHost;
        _targetUri = targetUri;
        _requestBody = requestBody;
    }

    public override async ValueTask TransformRequestAsync(HttpContext httpContext, HttpRequestMessage proxyRequest, string destinationPrefix, CancellationToken cancellationToken)
    {
        await base.TransformRequestAsync(httpContext, proxyRequest, destinationPrefix, cancellationToken);

        // Forzar el header Host correcto
        proxyRequest.Headers.Host = _expectedHost;

        // Establecer la URI de destino manualmente
        proxyRequest.RequestUri = new Uri(_targetUri);

        // Si tenemos un body personalizado, usarlo
        if (!string.IsNullOrEmpty(_requestBody) && proxyRequest.Method != HttpMethod.Get && proxyRequest.Method != HttpMethod.Delete)
        {
            var content = new StringContent(_requestBody, System.Text.Encoding.UTF8, "application/json");
            proxyRequest.Content = content;
            Console.WriteLine($"=== CUSTOM TRANSFORMER SET BODY === Length: {_requestBody.Length}");
        }

        Console.WriteLine($"=== CUSTOM TRANSFORMER SET HOST === {_expectedHost}");
        Console.WriteLine($"=== PROXY REQUEST URI === {proxyRequest.RequestUri}");
        Console.WriteLine($"=== PROXY REQUEST METHOD === {proxyRequest.Method}");
        Console.WriteLine($"=== PROXY REQUEST HEADERS === {string.Join(", ", proxyRequest.Headers.Select(h => $"{h.Key}:{string.Join(",", h.Value)}"))}");
    }
}