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
            // 1. HTML Encode para prevenir XSS
            var sanitized = HttpUtility.HtmlEncode(input);

            // 2. Detectar y remover patrones XSS conocidos
            if (XssPattern.IsMatch(sanitized))
            {
                _logger.LogWarning("Potential XSS attempt detected in input: {Input}",
                    input.Length > 100 ? input[..100] + "..." : input);
                sanitized = XssPattern.Replace(sanitized, "");
            }

            // 3. Detectar inyección SQL
            if (SqlInjectionPattern.IsMatch(sanitized))
            {
                _logger.LogWarning("Potential SQL injection attempt detected in input: {Input}",
                    input.Length > 100 ? input[..100] + "..." : input);
                // No removemos, solo loggeamos para análisis
            }

            return sanitized;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sanitizing input string");
            return HttpUtility.HtmlEncode(input); // Fallback seguro
        }
    }

    public string SanitizeApiPath(string path)
    {
        if (string.IsNullOrEmpty(path))
            return "/";

        try
        {
            // 1. Normalizar path
            var sanitized = path.Trim();

            // 2. Verificar path traversal
            if (PathTraversalPattern.IsMatch(sanitized))
            {
                _logger.LogWarning("Path traversal attempt detected: {Path}", path);
                throw new SecurityException("Invalid path: path traversal detected");
            }

            // 3. Asegurar que comience con /api/
            if (!sanitized.StartsWith("/api/", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Invalid API path format: {Path}", path);
                throw new ArgumentException("Path must start with /api/");
            }

            // 4. Validar caracteres permitidos
            if (!Regex.IsMatch(sanitized, @"^/api/[a-zA-Z0-9\-_/]*$"))
            {
                _logger.LogWarning("Invalid characters in API path: {Path}", path);
                throw new ArgumentException("Path contains invalid characters");
            }

            return sanitized;
        }
        catch (Exception ex) when (!(ex is SecurityException || ex is ArgumentException))
        {
            _logger.LogError(ex, "Error sanitizing API path: {Path}", path);
            throw new ArgumentException("Invalid path format");
        }
    }

    public Dictionary<string, string> SanitizeQueryParameters(Dictionary<string, string> queryParams)
    {
        var sanitized = new Dictionary<string, string>();

        foreach (var param in queryParams.Take(20)) // Limitar a 20 parámetros
        {
            try
            {
                // Sanitizar key y value
                var sanitizedKey = SanitizeString(param.Key);
                var sanitizedValue = SanitizeString(param.Value);

                // Validar longitud
                if (sanitizedKey.Length > 100 || sanitizedValue.Length > 1000)
                {
                    _logger.LogWarning("Query parameter too long: {Key}", param.Key);
                    continue;
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

        return allowedServices.Contains(serviceName, StringComparer.OrdinalIgnoreCase);
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