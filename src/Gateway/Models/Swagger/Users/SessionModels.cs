using System.ComponentModel.DataAnnotations;
using Swashbuckle.AspNetCore.Annotations;

namespace Gateway.Models.Swagger.Users;

/// <summary>
/// DTO de sesión activa de usuario
/// </summary>
[SwaggerSchema(Description = "Información de una sesión activa (token JWT válido)")]
public class SessionDto
{
    /// <summary>
    /// ID único de la sesión
    /// </summary>
    [SwaggerSchema(Description = "Identificador único de la sesión")]
    public string SessionId { get; set; } = string.Empty;

    /// <summary>
    /// ID del usuario propietario de la sesión
    /// </summary>
    [SwaggerSchema(Description = "Usuario asociado a esta sesión")]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Token JWT de la sesión (parcial por seguridad)
    /// </summary>
    [SwaggerSchema(Description = "Últimos 8 caracteres del token (para identificación)")]
    public string TokenPreview { get; set; } = string.Empty;

    /// <summary>
    /// Fecha de creación de la sesión (login)
    /// </summary>
    [SwaggerSchema(Description = "Momento en que se creó el token", Format = "date-time")]
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Fecha de expiración del token
    /// </summary>
    [SwaggerSchema(Description = "Momento en que expirará el token", Format = "date-time")]
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Fecha del último uso del token
    /// </summary>
    [SwaggerSchema(Description = "Última actividad registrada con este token", Format = "date-time")]
    public DateTime LastActivityAt { get; set; }

    /// <summary>
    /// Dirección IP desde donde se inició la sesión
    /// </summary>
    [SwaggerSchema(Description = "IP del dispositivo (enmascarada parcialmente)")]
    public string IpAddress { get; set; } = string.Empty;

    /// <summary>
    /// User-Agent del navegador/cliente
    /// </summary>
    [SwaggerSchema(Description = "Información del navegador o aplicación")]
    public string UserAgent { get; set; } = string.Empty;

    /// <summary>
    /// Tipo de dispositivo detectado
    /// </summary>
    [SwaggerSchema(Description = "Tipo de dispositivo: 'desktop', 'mobile', 'tablet', 'unknown'")]
    public string DeviceType { get; set; } = "unknown";

    /// <summary>
    /// Ubicación geográfica estimada (ciudad/país)
    /// </summary>
    [SwaggerSchema(Description = "Ubicación aproximada basada en IP (ej: 'Ciudad de México, México')")]
    public string? Location { get; set; }

    /// <summary>
    /// Indica si es la sesión actual
    /// </summary>
    [SwaggerSchema(Description = "True si este es el token usado en el request actual")]
    public bool IsCurrentSession { get; set; }

    /// <summary>
    /// Indica si el token está activo o revocado
    /// </summary>
    [SwaggerSchema(Description = "False si el token fue invalidado manualmente")]
    public bool IsActive { get; set; }
}

/// <summary>
/// Respuesta de lista de sesiones activas
/// </summary>
[SwaggerSchema(Description = "Lista de todas las sesiones activas del usuario")]
public class ActiveSessionsResponse
{
    /// <summary>
    /// Lista de sesiones activas
    /// </summary>
    [SwaggerSchema(Description = "Sesiones con tokens válidos y no expirados")]
    public List<SessionDto> Sessions { get; set; } = new();

    /// <summary>
    /// Número total de sesiones activas
    /// </summary>
    [SwaggerSchema(Description = "Cantidad de dispositivos/tokens activos")]
    public int TotalSessions { get; set; }

    /// <summary>
    /// ID de la sesión actual
    /// </summary>
    [SwaggerSchema(Description = "SessionId del token usado en este request")]
    public string CurrentSessionId { get; set; } = string.Empty;

    /// <summary>
    /// Fecha de consulta
    /// </summary>
    [SwaggerSchema(Description = "Timestamp del reporte", Format = "date-time")]
    public DateTime RetrievedAt { get; set; }
}

/// <summary>
/// Modelo de solicitud para revocar sesión específica
/// </summary>
[SwaggerSchema(Description = "Datos para invalidar una sesión específica")]
public class RevokeSessionRequest
{
    /// <summary>
    /// ID de la sesión a revocar
    /// </summary>
    [Required(ErrorMessage = "El ID de sesión es requerido")]
    [SwaggerSchema(Description = "SessionId obtenido de la lista de sesiones activas")]
    public string SessionId { get; set; } = string.Empty;

    /// <summary>
    /// Motivo de revocación (opcional, para auditoría)
    /// </summary>
    [StringLength(200, ErrorMessage = "El motivo no puede exceder 200 caracteres")]
    [SwaggerSchema(Description = "Razón de cierre de sesión (ej: 'dispositivo perdido')")]
    public string? Reason { get; set; }
}

/// <summary>
/// Respuesta de revocación de sesión
/// </summary>
[SwaggerSchema(Description = "Confirmación de invalidación de sesión")]
public class RevokeSessionResponse
{
    /// <summary>
    /// Indica si la sesión fue revocada exitosamente
    /// </summary>
    [SwaggerSchema(Description = "True si el token fue invalidado correctamente")]
    public bool Success { get; set; }

    /// <summary>
    /// Mensaje descriptivo
    /// </summary>
    [SwaggerSchema(Description = "Mensaje de confirmación o error")]
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// ID de la sesión revocada
    /// </summary>
    [SwaggerSchema(Description = "SessionId que fue invalidado")]
    public string SessionId { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp de revocación
    /// </summary>
    [SwaggerSchema(Description = "Momento exacto de la revocación", Format = "date-time")]
    public DateTime RevokedAt { get; set; }
}

/// <summary>
/// Modelo de solicitud para revocar todas las sesiones excepto la actual
/// </summary>
[SwaggerSchema(Description = "Datos para cerrar todas las sesiones excepto la actual")]
public class RevokeOtherSessionsRequest
{
    /// <summary>
    /// Confirmación explícita de la acción
    /// </summary>
    [Required]
    [SwaggerSchema(Description = "Debe ser true para confirmar la acción")]
    public bool ConfirmRevocation { get; set; }

    /// <summary>
    /// Motivo de revocación masiva
    /// </summary>
    [StringLength(200, ErrorMessage = "El motivo no puede exceder 200 caracteres")]
    [SwaggerSchema(Description = "Razón del cierre masivo (ej: 'sospecha de acceso no autorizado')")]
    public string? Reason { get; set; }
}

/// <summary>
/// Respuesta de revocación masiva de sesiones
/// </summary>
[SwaggerSchema(Description = "Resultado de invalidación de múltiples sesiones")]
public class RevokeOtherSessionsResponse
{
    /// <summary>
    /// Indica si la operación fue exitosa
    /// </summary>
    [SwaggerSchema(Description = "True si todas las sesiones se revocaron correctamente")]
    public bool Success { get; set; }

    /// <summary>
    /// Mensaje descriptivo
    /// </summary>
    [SwaggerSchema(Description = "Mensaje de confirmación")]
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Número de sesiones revocadas
    /// </summary>
    [SwaggerSchema(Description = "Cantidad de tokens invalidados")]
    public int SessionsRevoked { get; set; }

    /// <summary>
    /// IDs de las sesiones revocadas
    /// </summary>
    [SwaggerSchema(Description = "Lista de SessionIds invalidados")]
    public List<string> RevokedSessionIds { get; set; } = new();

    /// <summary>
    /// Timestamp de la operación
    /// </summary>
    [SwaggerSchema(Description = "Momento de la revocación masiva", Format = "date-time")]
    public DateTime RevokedAt { get; set; }
}

/// <summary>
/// Estadísticas de sesiones de usuario
/// </summary>
[SwaggerSchema(Description = "Resumen de actividad de sesiones del usuario")]
public class SessionStatistics
{
    /// <summary>
    /// ID del usuario
    /// </summary>
    [SwaggerSchema(Description = "Usuario consultado")]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Total de sesiones activas actualmente
    /// </summary>
    [SwaggerSchema(Description = "Tokens válidos y no expirados")]
    public int ActiveSessions { get; set; }

    /// <summary>
    /// Total de sesiones en los últimos 30 días
    /// </summary>
    [SwaggerSchema(Description = "Logins en el último mes")]
    public int SessionsLast30Days { get; set; }

    /// <summary>
    /// Fecha del último login
    /// </summary>
    [SwaggerSchema(Description = "Última autenticación exitosa", Format = "date-time")]
    public DateTime? LastLoginAt { get; set; }

    /// <summary>
    /// IP del último login
    /// </summary>
    [SwaggerSchema(Description = "Dirección IP del último acceso")]
    public string? LastLoginIp { get; set; }

    /// <summary>
    /// Ubicación del último login
    /// </summary>
    [SwaggerSchema(Description = "Ubicación aproximada del último acceso")]
    public string? LastLoginLocation { get; set; }

    /// <summary>
    /// Dispositivo más usado
    /// </summary>
    [SwaggerSchema(Description = "Tipo de dispositivo más frecuente (desktop/mobile/tablet)")]
    public string MostUsedDevice { get; set; } = "unknown";

    /// <summary>
    /// Número de logins fallidos en últimos 7 días
    /// </summary>
    [SwaggerSchema(Description = "Intentos de login incorrectos recientes")]
    public int FailedLoginAttempts { get; set; }

    /// <summary>
    /// Fecha de generación de estadísticas
    /// </summary>
    [SwaggerSchema(Description = "Timestamp del reporte", Format = "date-time")]
    public DateTime GeneratedAt { get; set; }
}

/// <summary>
/// Respuesta con lista de sesiones
/// </summary>
[SwaggerSchema(Description = "Lista de sesiones (todas o de un usuario específico)")]
public class SessionsListResponse
{
    /// <summary>
    /// Lista de sesiones
    /// </summary>
    [SwaggerSchema(Description = "Sesiones recuperadas")]
    public List<SessionDto> Sessions { get; set; } = new();

    /// <summary>
    /// Número total de sesiones
    /// </summary>
    [SwaggerSchema(Description = "Cantidad total de sesiones")]
    public int TotalCount { get; set; }

    /// <summary>
    /// Mensaje adicional (opcional)
    /// </summary>
    [SwaggerSchema(Description = "Información adicional sobre la consulta")]
    public string? Message { get; set; }
}

/// <summary>
/// Respuesta de eliminación de sesiones múltiples
/// </summary>
[SwaggerSchema(Description = "Confirmación de eliminación masiva de sesiones")]
public class SessionsDeletionResponse
{
    /// <summary>
    /// Indica si la operación fue exitosa
    /// </summary>
    [SwaggerSchema(Description = "True si todas las sesiones se eliminaron correctamente")]
    public bool Success { get; set; }

    /// <summary>
    /// Mensaje descriptivo
    /// </summary>
    [SwaggerSchema(Description = "Mensaje de confirmación")]
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Número de sesiones eliminadas
    /// </summary>
    [SwaggerSchema(Description = "Cantidad de sesiones cerradas")]
    public int SessionsDeleted { get; set; }
}

