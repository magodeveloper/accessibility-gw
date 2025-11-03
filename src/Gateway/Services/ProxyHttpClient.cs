using System.Text;
using System.Text.Json;

namespace Gateway.Services;

/// <summary>
/// Cliente HTTP para realizar llamadas proxy a los microservicios.
/// Utilizado por los controllers proxy para invocar los endpoints reales.
/// </summary>
public class ProxyHttpClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ProxyHttpClient> _logger;

    public ProxyHttpClient(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<ProxyHttpClient> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Realiza una petición proxy a un microservicio específico
    /// </summary>
    public async Task<IResult> ProxyRequestAsync(
        string serviceName,
        string path,
        HttpMethod method,
        object? body = null,
        Dictionary<string, string>? queryParams = null)
    {
        try
        {
            // Obtener la URL base del microservicio desde configuración
            var serviceUrl = _configuration[$"Gate:Services:{serviceName}"];
            if (string.IsNullOrEmpty(serviceUrl))
            {
                _logger.LogError("Service URL not found for: {ServiceName}", serviceName);
                return Results.Problem(
                    detail: $"Service configuration not found: {serviceName}",
                    statusCode: StatusCodes.Status503ServiceUnavailable);
            }

            // Construir URL completa
            var uriBuilder = new UriBuilder($"{serviceUrl}{path}");

            // Agregar query parameters si existen
            if (queryParams?.Any() == true)
            {
                var query = string.Join("&", queryParams.Select(kvp =>
                    $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}"));
                uriBuilder.Query = query;
            }

            var targetUrl = uriBuilder.ToString();
            _logger.LogInformation("Proxying {Method} request to: {Url}", method, targetUrl);

            // Crear cliente HTTP
            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(30);

            // Crear request message
            var request = new HttpRequestMessage(method, targetUrl);

            // Agregar body si existe
            if (body != null && (method == HttpMethod.Post || method == HttpMethod.Put || method == HttpMethod.Patch))
            {
                var jsonContent = JsonSerializer.Serialize(body, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
                request.Content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
            }

            // Agregar headers necesarios
            request.Headers.Add("User-Agent", "AccessibilityGateway/1.0");

            // Ejecutar request
            var response = await client.SendAsync(request);

            // Leer contenido de respuesta
            var responseContent = await response.Content.ReadAsStringAsync();

            // Retornar resultado apropiado basado en status code
            if (response.IsSuccessStatusCode)
            {
                var contentType = response.Content.Headers.ContentType?.MediaType ?? "application/json";

                if (contentType.Contains("json") && !string.IsNullOrWhiteSpace(responseContent))
                {
                    try
                    {
                        var jsonDocument = JsonDocument.Parse(responseContent);
                        return Results.Json(jsonDocument, statusCode: (int)response.StatusCode);
                    }
                    catch
                    {
                        // Si no es JSON válido, devolver como texto
                        return Results.Content(responseContent, contentType, statusCode: (int)response.StatusCode);
                    }
                }

                return Results.Content(responseContent, contentType, statusCode: (int)response.StatusCode);
            }
            else
            {
                _logger.LogWarning("Proxy request failed with status {StatusCode}: {Response}",
                    response.StatusCode, responseContent);

                return Results.Problem(
                    detail: responseContent,
                    statusCode: (int)response.StatusCode);
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error during proxy request to {ServiceName}{Path}", serviceName, path);
            return Results.Problem(
                detail: $"Service unavailable: {ex.Message}",
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Timeout during proxy request to {ServiceName}{Path}", serviceName, path);
            return Results.Problem(
                detail: "Request timeout",
                statusCode: StatusCodes.Status408RequestTimeout);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during proxy request to {ServiceName}{Path}", serviceName, path);
            return Results.Problem(
                detail: $"Internal error: {ex.Message}",
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// GET proxy request
    /// </summary>
    public Task<IResult> GetAsync(string serviceName, string path, Dictionary<string, string>? queryParams = null)
        => ProxyRequestAsync(serviceName, path, HttpMethod.Get, null, queryParams);

    /// <summary>
    /// POST proxy request
    /// </summary>
    public Task<IResult> PostAsync(string serviceName, string path, object body, Dictionary<string, string>? queryParams = null)
        => ProxyRequestAsync(serviceName, path, HttpMethod.Post, body, queryParams);

    /// <summary>
    /// PUT proxy request
    /// </summary>
    public Task<IResult> PutAsync(string serviceName, string path, object body, Dictionary<string, string>? queryParams = null)
        => ProxyRequestAsync(serviceName, path, HttpMethod.Put, body, queryParams);

    /// <summary>
    /// PATCH proxy request
    /// </summary>
    public Task<IResult> PatchAsync(string serviceName, string path, object body, Dictionary<string, string>? queryParams = null)
        => ProxyRequestAsync(serviceName, path, HttpMethod.Patch, body, queryParams);

    /// <summary>
    /// DELETE proxy request
    /// </summary>
    public Task<IResult> DeleteAsync(string serviceName, string path, Dictionary<string, string>? queryParams = null)
        => ProxyRequestAsync(serviceName, path, HttpMethod.Delete, null, queryParams);
}
