using System.Text.RegularExpressions;
using System.Web;
using System.Text;

namespace Gateway.Services;

/// <summary>
/// Servicio para sanitización y validación avanzada de inputs
/// </summary>
public interface IInputSanitizationService
{
    /// <summary>
    /// Sanitiza una cadena de texto contra XSS
    /// </summary>
    string SanitizeString(string input);

    /// <summary>
    /// Valida y sanitiza un path de API
    /// </summary>
    string SanitizeApiPath(string path);

    /// <summary>
    /// Sanitiza parámetros de query
    /// </summary>
    Dictionary<string, string> SanitizeQueryParameters(Dictionary<string, string> queryParams);

    /// <summary>
    /// Valida headers HTTP seguros
    /// </summary>
    Dictionary<string, string> ValidateAndSanitizeHeaders(Dictionary<string, string> headers);

    /// <summary>
    /// Valida que un servicio esté en la lista de servicios permitidos
    /// </summary>
    bool IsValidService(string serviceName, IEnumerable<string> allowedServices);
}

/// <summary>
/// Implementación del servicio de sanitización
/// </summary>
public class InputSanitizationService : IInputSanitizationService
{
    private readonly ILogger<InputSanitizationService> _logger;

    // Patrones de seguridad
    private static readonly Regex XssPattern = new(@"<script[^>]*>.*?</script>|javascript:|vbscript:|on\w+\s*=",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex SqlInjectionPattern = new(@"(\b(ALTER|CREATE|DELETE|DROP|EXEC(UTE)?|INSERT( +INTO)?|MERGE|SELECT|UPDATE|UNION( +ALL)?)\b)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex PathTraversalPattern = new(@"\.\.|\/\.\.|\.\/|\.\.\%2F|\%2F\.\.|\.%2F",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // Headers permitidos (whitelist)
    private static readonly HashSet<string> AllowedHeaders = new(StringComparer.OrdinalIgnoreCase)
    {
        "Accept", "Accept-Encoding", "Accept-Language", "Authorization", "Content-Type",
        "Content-Length", "User-Agent", "X-Requested-With", "X-Correlation-ID",
        "X-API-Version", "Cache-Control", "If-None-Match", "If-Modified-Since"
    };

    public InputSanitizationService(ILogger<InputSanitizationService> logger)
    {
        _logger = logger;
    }

    public string SanitizeString(string input)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        try
        {
            // 1. Trim leading and trailing spaces ONLY if it's not just whitespace
            var trimmed = string.IsNullOrWhiteSpace(input) ? input : input.Trim();

            // 2. HTML Encode para prevenir XSS
            var sanitized = HttpUtility.HtmlEncode(trimmed);

            // 3. Detectar y remover patrones XSS conocidos
            if (XssPattern.IsMatch(sanitized))
            {
                _logger.LogWarning("Potential XSS attempt detected in input: {Input}",
                    trimmed.Length > 100 ? trimmed[..100] + "..." : trimmed);
                sanitized = XssPattern.Replace(sanitized, "");
            }

            // 4. Detectar inyección SQL
            if (SqlInjectionPattern.IsMatch(sanitized))
            {
                _logger.LogWarning("Potential SQL injection attempt detected in input: {Input}",
                    trimmed.Length > 100 ? trimmed[..100] + "..." : trimmed);
                // No removemos, solo loggeamos para análisis
            }

            return sanitized;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sanitizing input string");
            return HttpUtility.HtmlEncode(input); // Fallback seguro sin trim
        }
    }

    public string SanitizeApiPath(string path)
    {
        // Handle null, empty or whitespace - return empty string as tests expect
        if (string.IsNullOrWhiteSpace(path))
            return string.Empty;

        try
        {
            // 1. Normalizar path y trim
            var sanitized = path.Trim();

            // 2. Si después del trim queda vacío, retornar string vacío
            if (string.IsNullOrEmpty(sanitized))
                return string.Empty;

            // 3. Verificar path traversal ANTES de otras validaciones
            // Log warning and return empty for path traversal (defensive approach)
            if (PathTraversalPattern.IsMatch(sanitized))
            {
                _logger.LogWarning("Path traversal attempt detected: {Path}", path);
                return string.Empty;
            }

            // 4. Detectar y remover query strings con ataques (XSS, SQL Injection)
            var queryStringIndex = sanitized.IndexOf('?');
            if (queryStringIndex >= 0)
            {
                var queryString = sanitized.Substring(queryStringIndex);

                // Check for attacks in query string
                if (XssPattern.IsMatch(queryString) || SqlInjectionPattern.IsMatch(queryString))
                {
                    _logger.LogWarning("Attack detected in query string, removing: {Path}", path);
                    sanitized = sanitized.Substring(0, queryStringIndex);
                }
            }

            // 5. Normalizar múltiples slashes consecutivos
            sanitized = Regex.Replace(sanitized, @"/{2,}", "/");

            // 6. Asegurar que comience con /
            if (!sanitized.StartsWith("/"))
            {
                sanitized = "/" + sanitized;
            }

            return sanitized;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sanitizing API path: {Path}", path);
            return string.Empty;
        }
    }

    public Dictionary<string, string> SanitizeQueryParameters(Dictionary<string, string> queryParams)
    {
        // Handle null input
        if (queryParams == null)
        {
            _logger.LogDebug("Null query parameters received, returning empty dictionary");
            return new Dictionary<string, string>();
        }

        var sanitized = new Dictionary<string, string>();

        foreach (var param in queryParams.Take(20)) // Limitar a 20 parámetros
        {
            try
            {
                // Skip null or empty keys
                if (string.IsNullOrEmpty(param.Key))
                {
                    _logger.LogDebug("Skipping query parameter with null or empty key");
                    continue;
                }

                // Validate key length first (before sanitization)
                if (param.Key.Length > 100)
                {
                    _logger.LogWarning("Query parameter key too long: {Key}", param.Key.Length);
                    continue;
                }

                // Sanitizar key y value (pero no validar longitud del value aún)
                var sanitizedKey = SanitizeString(param.Key);
                var sanitizedValue = SanitizeString(param.Value ?? string.Empty);

                // Truncate value if longer than 1000 chars (don't skip it)
                if (sanitizedValue.Length > 1000)
                {
                    _logger.LogWarning("Query parameter value too long: {Key}", param.Key);
                    sanitizedValue = sanitizedValue.Substring(0, 1000);
                }

                sanitized[sanitizedKey] = sanitizedValue;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sanitizing query parameter: {Key}", param.Key);
                // Continuar con otros parámetros
            }
        }

        return sanitized;
    }

    public Dictionary<string, string> ValidateAndSanitizeHeaders(Dictionary<string, string> headers)
    {
        // Handle null input
        if (headers == null)
        {
            _logger.LogDebug("Null headers received, returning empty dictionary");
            return new Dictionary<string, string>();
        }

        var sanitized = new Dictionary<string, string>();

        foreach (var header in headers.Take(30)) // Limitar a 30 headers
        {
            try
            {
                // Solo permitir headers seguros
                if (!AllowedHeaders.Contains(header.Key))
                {
                    _logger.LogDebug("Header not in whitelist, skipping: {Header}", header.Key);
                    continue;
                }

                // Sanitizar valor del header
                var sanitizedValue = SanitizeString(header.Value);

                // Validar longitud
                if (sanitizedValue.Length > 2048) // Headers HTTP no deberían ser muy largos
                {
                    _logger.LogWarning("Header value too long: {Header}", header.Key);
                    continue;
                }

                sanitized[header.Key] = sanitizedValue;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing header: {Header}", header.Key);
                // Continuar con otros headers
            }
        }

        return sanitized;
    }

    public bool IsValidService(string serviceName, IEnumerable<string> allowedServices)
    {
        if (string.IsNullOrWhiteSpace(serviceName))
            return false;

        // Handle null allowedServices
        if (allowedServices == null)
        {
            _logger.LogWarning("Null allowedServices list provided to IsValidService");
            return false;
        }

        // Case-sensitive comparison
        return allowedServices.Contains(serviceName, StringComparer.Ordinal);
    }
}

/// <summary>
/// Excepción para errores de seguridad
/// </summary>
public class SecurityException : Exception
{
    public SecurityException(string message) : base(message) { }
    public SecurityException(string message, Exception innerException) : base(message, innerException) { }
}