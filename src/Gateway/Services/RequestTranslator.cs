using System.Net;
using System.Text;
using Gateway.Models;
using System.Text.Json;
using System.Net.Http.Headers;
using Yarp.ReverseProxy.Forwarder;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Http.Extensions;
using System.Diagnostics;

namespace Gateway.Services;

public sealed class RequestTranslator
{
    private static readonly HashSet<string> _allowedMethods =
        new(new[] { "GET", "POST", "PUT", "PATCH", "DELETE" }, StringComparer.OrdinalIgnoreCase);

    private readonly GateOptions _opts;
    private readonly IHttpForwarder _forwarder;
    private readonly HttpMessageInvoker _httpClient;
    private readonly CacheService _cacheService;
    private readonly MetricsService _metricsService;
    private readonly ILogger<RequestTranslator> _logger;
    private readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    public RequestTranslator(
        IOptions<GateOptions> opts,
        IHttpForwarder forwarder,
        HttpMessageInvoker httpClient,
        CacheService cacheService,
        MetricsService metricsService,
        ILogger<RequestTranslator> logger)
    {
        _opts = opts.Value;
        _forwarder = forwarder;
        _httpClient = httpClient;
        _cacheService = cacheService;
        _metricsService = metricsService;
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
                result.Response = result.Response with
                {
                    ProcessingTimeMs = stopwatch.ElapsedMilliseconds,
                    ProcessedByService = req.Service
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
        var result = await ProcessRequestAsync(context, req, ct);

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
        }
        // Si es exitoso, YARP ya escribió la respuesta
    }

    private async Task<TranslateResult> ForwardRequestAsync(HttpContext context, TranslateRequest req, CancellationToken ct)
    {
        var baseUrl = _opts.Services[req.Service].TrimEnd('/');
        var targetUri = new Uri(BuildTargetUrl(baseUrl, req.Path, req.Query));

        var transformer = HttpTransformer.Default;
        var act = req.Method.ToUpperInvariant();

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

        // Preparar body
        if (act is "GET" or "DELETE")
        {
            context.Request.Body = Stream.Null;
            context.Request.ContentLength = 0;
        }
        else if (req.Body != null)
        {
            var json = JsonSerializer.Serialize(req.Body, _json);
            var bytes = Encoding.UTF8.GetBytes(json);
            context.Request.Body = new MemoryStream(bytes);
            context.Request.ContentLength = bytes.Length;
            context.Request.ContentType ??= "application/json";
        }

        var requestConfig = new ForwarderRequestConfig
        {
            ActivityTimeout = TimeSpan.FromSeconds(_opts.DefaultTimeoutSeconds)
        };

        var error = await _forwarder.SendAsync(context, targetUri, _httpClient, requestConfig, transformer);

        if (error != ForwarderError.None)
        {
            var status = error switch
            {
                ForwarderError.Request => HttpStatusCode.BadRequest,
                ForwarderError.Timeout or ForwarderError.RequestTimedOut => HttpStatusCode.GatewayTimeout,
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
}