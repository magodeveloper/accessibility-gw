using System.ComponentModel.DataAnnotations;
using Swashbuckle.AspNetCore.Annotations;

namespace Gateway.Models.Swagger.Shared;

/// <summary>
/// Modelo estándar de respuesta de error
/// </summary>
[SwaggerSchema(Description = "Respuesta de error estandarizada para todos los endpoints")]
public class ErrorResponse
{
    /// <summary>
    /// Código de estado HTTP del error
    /// </summary>
    [SwaggerSchema(Description = "Código HTTP (400, 401, 403, 404, 500, etc.)")]
    public int StatusCode { get; set; }

    /// <summary>
    /// Tipo de error
    /// </summary>
    [SwaggerSchema(Description = "Tipo de error: 'ValidationError', 'AuthenticationError', 'AuthorizationError', 'NotFoundError', 'ServerError', etc.")]
    public string ErrorType { get; set; } = string.Empty;

    /// <summary>
    /// Mensaje principal del error
    /// </summary>
    [SwaggerSchema(Description = "Descripción general del error")]
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Detalles adicionales del error (opcional)
    /// </summary>
    [SwaggerSchema(Description = "Información técnica adicional sobre el error")]
    public string? Details { get; set; }

    /// <summary>
    /// Lista de errores de validación (para errores 400)
    /// </summary>
    [SwaggerSchema(Description = "Errores de validación específicos por campo")]
    public Dictionary<string, List<string>>? ValidationErrors { get; set; }

    /// <summary>
    /// Código de error interno para tracking
    /// </summary>
    [SwaggerSchema(Description = "Código único para rastreo interno (ej: 'USR-001', 'AUTH-005')")]
    public string? ErrorCode { get; set; }

    /// <summary>
    /// ID de correlación del request (para debugging)
    /// </summary>
    [SwaggerSchema(Description = "Identificador único de la petición para soporte técnico")]
    public string? CorrelationId { get; set; }

    /// <summary>
    /// Timestamp del error
    /// </summary>
    [SwaggerSchema(Description = "Momento exacto en que ocurrió el error", Format = "date-time")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Path del endpoint donde ocurrió el error
    /// </summary>
    [SwaggerSchema(Description = "Ruta del endpoint que generó el error")]
    public string? Path { get; set; }

    /// <summary>
    /// Método HTTP usado
    /// </summary>
    [SwaggerSchema(Description = "Método HTTP (GET, POST, PUT, DELETE, etc.)")]
    public string? Method { get; set; }

    /// <summary>
    /// Sugerencia de acción para el usuario/cliente
    /// </summary>
    [SwaggerSchema(Description = "Mensaje amigable con sugerencia de cómo resolver el error")]
    public string? Suggestion { get; set; }

    /// <summary>
    /// Link a documentación relevante (opcional)
    /// </summary>
    [SwaggerSchema(Description = "URL a documentación o guía de solución", Format = "uri")]
    public string? DocumentationUrl { get; set; }
}

/// <summary>
/// Ejemplos de ErrorResponse para diferentes códigos HTTP
/// </summary>
public static class ErrorResponseExamples
{
    /// <summary>
    /// Error 400 - Validación fallida
    /// </summary>
    public static ErrorResponse BadRequest => new()
    {
        StatusCode = 400,
        ErrorType = "ValidationError",
        Message = "La solicitud contiene datos inválidos",
        Details = "Uno o más campos no cumplen con los requisitos de validación",
        ValidationErrors = new Dictionary<string, List<string>>
        {
            ["Email"] = new List<string> { "El formato del email es inválido", "El email ya está registrado" },
            ["Password"] = new List<string> { "La contraseña debe tener al menos 8 caracteres", "Debe incluir al menos una mayúscula" }
        },
        ErrorCode = "VAL-001",
        Timestamp = DateTime.UtcNow,
        Path = "/api/users/auth/register",
        Method = "POST",
        Suggestion = "Revise los campos indicados y corrija los errores de validación",
        DocumentationUrl = "https://docs.api.com/errors/validation"
    };

    /// <summary>
    /// Error 401 - No autenticado
    /// </summary>
    public static ErrorResponse Unauthorized => new()
    {
        StatusCode = 401,
        ErrorType = "AuthenticationError",
        Message = "No se proporcionó autenticación válida",
        Details = "El token JWT está ausente, es inválido o ha expirado",
        ErrorCode = "AUTH-001",
        Timestamp = DateTime.UtcNow,
        Path = "/api/users/profile",
        Method = "GET",
        Suggestion = "Inicie sesión nuevamente para obtener un token válido",
        DocumentationUrl = "https://docs.api.com/authentication"
    };

    /// <summary>
    /// Error 403 - No autorizado
    /// </summary>
    public static ErrorResponse Forbidden => new()
    {
        StatusCode = 403,
        ErrorType = "AuthorizationError",
        Message = "No tiene permisos para acceder a este recurso",
        Details = "Su rol de usuario no incluye los permisos necesarios para esta operación",
        ErrorCode = "AUTH-002",
        Timestamp = DateTime.UtcNow,
        Path = "/api/users/admin/list",
        Method = "GET",
        Suggestion = "Contacte al administrador para solicitar los permisos necesarios",
        DocumentationUrl = "https://docs.api.com/authorization"
    };

    /// <summary>
    /// Error 403 - Gateway Secret inválido
    /// </summary>
    public static ErrorResponse ForbiddenGatewaySecret => new()
    {
        StatusCode = 403,
        ErrorType = "GatewayAuthenticationError",
        Message = "Gateway Secret inválido o ausente",
        Details = "El header X-Gateway-Secret no es válido",
        ErrorCode = "GW-001",
        Timestamp = DateTime.UtcNow,
        Suggestion = "Asegúrese de incluir el Gateway Secret correcto en el header X-Gateway-Secret",
        DocumentationUrl = "https://docs.api.com/gateway-authentication"
    };

    /// <summary>
    /// Error 404 - Recurso no encontrado
    /// </summary>
    public static ErrorResponse NotFound => new()
    {
        StatusCode = 404,
        ErrorType = "NotFoundError",
        Message = "El recurso solicitado no existe",
        Details = "No se encontró ningún usuario con el ID especificado",
        ErrorCode = "USR-404",
        Timestamp = DateTime.UtcNow,
        Path = "/api/users/123e4567-e89b-12d3-a456-426614174000",
        Method = "GET",
        Suggestion = "Verifique que el ID del recurso sea correcto",
        DocumentationUrl = "https://docs.api.com/errors/not-found"
    };

    /// <summary>
    /// Error 409 - Conflicto
    /// </summary>
    public static ErrorResponse Conflict => new()
    {
        StatusCode = 409,
        ErrorType = "ConflictError",
        Message = "El recurso ya existe",
        Details = "Ya existe un usuario registrado con este email",
        ErrorCode = "USR-409",
        Timestamp = DateTime.UtcNow,
        Path = "/api/users/auth/register",
        Method = "POST",
        Suggestion = "Use un email diferente o recupere la contraseña de la cuenta existente"
    };

    /// <summary>
    /// Error 429 - Demasiadas solicitudes
    /// </summary>
    public static ErrorResponse TooManyRequests => new()
    {
        StatusCode = 429,
        ErrorType = "RateLimitError",
        Message = "Ha excedido el límite de solicitudes",
        Details = "Máximo 100 requests por minuto. Intente nuevamente en 60 segundos",
        ErrorCode = "RATE-001",
        Timestamp = DateTime.UtcNow,
        Suggestion = "Espere un momento antes de realizar más solicitudes"
    };

    /// <summary>
    /// Error 500 - Error interno del servidor
    /// </summary>
    public static ErrorResponse InternalServerError => new()
    {
        StatusCode = 500,
        ErrorType = "ServerError",
        Message = "Ocurrió un error interno en el servidor",
        Details = "Error inesperado al procesar la solicitud",
        ErrorCode = "SRV-500",
        CorrelationId = Guid.NewGuid().ToString(),
        Timestamp = DateTime.UtcNow,
        Path = "/api/users/preferences",
        Method = "PUT",
        Suggestion = "Intente nuevamente más tarde. Si el problema persiste, contacte a soporte técnico con el ID de correlación",
        DocumentationUrl = "https://docs.api.com/support"
    };

    /// <summary>
    /// Error 502 - Bad Gateway
    /// </summary>
    public static ErrorResponse BadGateway => new()
    {
        StatusCode = 502,
        ErrorType = "GatewayError",
        Message = "El servicio de backend no está disponible",
        Details = "El microservicio Users no respondió o devolvió una respuesta inválida",
        ErrorCode = "GW-502",
        CorrelationId = Guid.NewGuid().ToString(),
        Timestamp = DateTime.UtcNow,
        Suggestion = "El servicio está temporalmente no disponible. Intente nuevamente en unos minutos"
    };

    /// <summary>
    /// Error 503 - Servicio no disponible
    /// </summary>
    public static ErrorResponse ServiceUnavailable => new()
    {
        StatusCode = 503,
        ErrorType = "ServiceUnavailableError",
        Message = "El servicio está temporalmente no disponible",
        Details = "El sistema está en mantenimiento programado",
        ErrorCode = "SRV-503",
        Timestamp = DateTime.UtcNow,
        Suggestion = "El servicio volverá a estar disponible pronto. Intente más tarde"
    };
}
