using System.ComponentModel.DataAnnotations;

namespace Gateway.Models;

/// <summary>
/// Modelo para traducir peticiones HTTP a través del gateway
/// </summary>
public sealed class TranslateRequest
{
    /// <summary>
    /// Nombre del servicio de destino (users, reports, analysis, middleware)
    /// </summary>
    [Required]
    public required string Service { get; init; }

    /// <summary>
    /// Método HTTP (GET, POST, PUT, PATCH, DELETE)
    /// </summary>
    [Required]
    public required string Method { get; init; }

    /// <summary>
    /// Ruta del endpoint (ej: /api/users/123)
    /// </summary>
    [Required]
    public required string Path { get; init; }

    /// <summary>
    /// Parámetros de consulta (query string)
    /// </summary>
    public IDictionary<string, string>? Query { get; init; }

    /// <summary>
    /// Headers HTTP personalizados
    /// </summary>
    public IDictionary<string, string>? Headers { get; init; }

    /// <summary>
    /// Cuerpo de la petición (para POST, PUT, PATCH)
    /// </summary>
    public object? Body { get; init; }

    /// <summary>
    /// Indica si se debe usar caché para esta petición (solo GET)
    /// </summary>
    public bool? UseCache { get; init; }

    /// <summary>
    /// Tiempo de expiración del caché en minutos (opcional)
    /// </summary>
    public int? CacheExpirationMinutes { get; init; }
}

/// <summary>
/// Respuesta del gateway después de procesar la petición
/// </summary>
public sealed record TranslateResponse
{
    /// <summary>
    /// Código de estado HTTP
    /// </summary>
    public required int StatusCode { get; init; }

    /// <summary>
    /// Headers de respuesta
    /// </summary>
    public IDictionary<string, string>? Headers { get; init; }

    /// <summary>
    /// Cuerpo de la respuesta
    /// </summary>
    public object? Body { get; init; }

    /// <summary>
    /// Tiempo de procesamiento en milisegundos
    /// </summary>
    public long ProcessingTimeMs { get; init; }

    /// <summary>
    /// Indica si la respuesta vino del caché
    /// </summary>
    public bool FromCache { get; init; } = false;

    /// <summary>
    /// Servicio que procesó la petición
    /// </summary>
    public string? ProcessedByService { get; init; }
}

/// <summary>
/// Información de error del gateway
/// </summary>
public sealed class TranslateError
{
    /// <summary>
    /// Mensaje de error
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// Código de error HTTP
    /// </summary>
    public int? StatusCode { get; init; }

    /// <summary>
    /// Headers adicionales de error
    /// </summary>
    public IDictionary<string, string>? Headers { get; init; }

    /// <summary>
    /// Detalles adicionales del error
    /// </summary>
    public string? Details { get; init; }

    /// <summary>
    /// ID de correlación para tracking
    /// </summary>
    public string? CorrelationId { get; init; }
}

/// <summary>
/// Resultado completo de la traducción
/// </summary>
public sealed record TranslateResult
{
    /// <summary>
    /// Respuesta exitosa
    /// </summary>
    public TranslateResponse? Response { get; init; }

    /// <summary>
    /// Error ocurrido (null si no hubo error)
    /// </summary>
    public TranslateError? Error { get; init; }

    /// <summary>
    /// Indica si la operación fue exitosa
    /// </summary>
    public bool Success => Error == null;
}

/// <summary>
/// Modelo para peticiones de salud del gateway
/// </summary>
public sealed class HealthCheckRequest
{
    /// <summary>
    /// Realizar verificación profunda de todos los servicios
    /// </summary>
    public bool Deep { get; init; } = false;

    /// <summary>
    /// Incluir métricas detalladas
    /// </summary>
    public bool IncludeMetrics { get; init; } = false;
}

/// <summary>
/// Respuesta del health check
/// </summary>
public sealed record HealthCheckResponse
{
    /// <summary>
    /// Estado general (Healthy, Unhealthy, Degraded)
    /// </summary>
    public required string Status { get; init; }

    /// <summary>
    /// Tiempo total de verificación
    /// </summary>
    public TimeSpan TotalDuration { get; init; }

    /// <summary>
    /// Estado de cada servicio
    /// </summary>
    public Dictionary<string, ServiceHealthStatus> Services { get; init; } = new();

    /// <summary>
    /// Métricas adicionales
    /// </summary>
    public Dictionary<string, object>? Metrics { get; init; }
}

/// <summary>
/// Estado de salud de un servicio específico
/// </summary>
public sealed class ServiceHealthStatus
{
    /// <summary>
    /// Estado del servicio
    /// </summary>
    public required string Status { get; init; }

    /// <summary>
    /// Descripción del estado
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Tiempo de respuesta
    /// </summary>
    public TimeSpan Duration { get; init; }

    /// <summary>
    /// Datos adicionales del servicio
    /// </summary>
    public Dictionary<string, object>? Data { get; init; }
}