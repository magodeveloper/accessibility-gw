using System.ComponentModel.DataAnnotations;
using Swashbuckle.AspNetCore.Annotations;

namespace Gateway.Models.Swagger.Users;

/// <summary>
/// Modelo de solicitud para registro de nuevos usuarios
/// </summary>
[SwaggerSchema(Description = "Datos requeridos para registrar un nuevo usuario en el sistema")]
public class RegisterRequest
{
    /// <summary>
    /// Correo electrónico del usuario (debe ser único en el sistema)
    /// </summary>
    [Required(ErrorMessage = "El email es requerido")]
    [EmailAddress(ErrorMessage = "Formato de email inválido")]
    [StringLength(100, ErrorMessage = "El email no puede exceder 100 caracteres")]
    [SwaggerSchema(Description = "Dirección de correo electrónico válida", Format = "email")]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Contraseña del usuario (mínimo 8 caracteres, debe incluir mayúsculas, minúsculas y números)
    /// </summary>
    [Required(ErrorMessage = "La contraseña es requerida")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "La contraseña debe tener entre 8 y 100 caracteres")]
    [DataType(DataType.Password)]
    [SwaggerSchema(Description = "Contraseña segura (mín. 8 caracteres, incluir mayúsculas, minúsculas y números)", Format = "password")]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Confirmación de contraseña (debe coincidir con Password)
    /// </summary>
    [Required(ErrorMessage = "La confirmación de contraseña es requerida")]
    [Compare("Password", ErrorMessage = "Las contraseñas no coinciden")]
    [DataType(DataType.Password)]
    [SwaggerSchema(Description = "Debe ser idéntica a la contraseña ingresada", Format = "password")]
    public string ConfirmPassword { get; set; } = string.Empty;

    /// <summary>
    /// Nombre para mostrar del usuario (opcional, se puede configurar después)
    /// </summary>
    [StringLength(50, ErrorMessage = "El nombre para mostrar no puede exceder 50 caracteres")]
    [SwaggerSchema(Description = "Nombre visible del usuario en la plataforma (opcional)")]
    public string? DisplayName { get; set; }

    /// <summary>
    /// Indica si el usuario acepta los términos y condiciones
    /// </summary>
    [Required(ErrorMessage = "Debe aceptar los términos y condiciones")]
    [SwaggerSchema(Description = "El usuario debe aceptar los términos y condiciones")]
    public bool AcceptTerms { get; set; } = false;
}

/// <summary>
/// Modelo de solicitud para inicio de sesión
/// </summary>
[SwaggerSchema(Description = "Credenciales para autenticar un usuario existente")]
public class LoginRequest
{
    /// <summary>
    /// Correo electrónico registrado del usuario
    /// </summary>
    [Required(ErrorMessage = "El email es requerido")]
    [EmailAddress(ErrorMessage = "Formato de email inválido")]
    [SwaggerSchema(Description = "Email registrado en el sistema", Format = "email")]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Contraseña del usuario
    /// </summary>
    [Required(ErrorMessage = "La contraseña es requerida")]
    [DataType(DataType.Password)]
    [SwaggerSchema(Description = "Contraseña del usuario", Format = "password")]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Indica si se debe mantener la sesión activa por más tiempo
    /// </summary>
    [SwaggerSchema(Description = "Si es true, el token tendrá mayor duración (30 días vs 24 horas)")]
    public bool RememberMe { get; set; } = false;
}

/// <summary>
/// Modelo de respuesta exitosa de login
/// </summary>
[SwaggerSchema(Description = "Datos retornados tras un inicio de sesión exitoso")]
public class LoginResponse
{
    /// <summary>
    /// Token JWT para autenticación en requests subsecuentes
    /// </summary>
    [SwaggerSchema(Description = "Token JWT Bearer para incluir en header Authorization")]
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// Tipo de token (siempre "Bearer")
    /// </summary>
    [SwaggerSchema(Description = "Tipo de token, siempre 'Bearer'")]
    public string TokenType { get; set; } = "Bearer";

    /// <summary>
    /// Tiempo en segundos hasta que el token expire
    /// </summary>
    [SwaggerSchema(Description = "Segundos hasta la expiración del token (86400 = 24h, 2592000 = 30d)")]
    public int ExpiresIn { get; set; }

    /// <summary>
    /// Timestamp de expiración del token (ISO 8601)
    /// </summary>
    [SwaggerSchema(Description = "Fecha y hora exacta de expiración del token", Format = "date-time")]
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Token de refresco para renovar el token principal (opcional)
    /// </summary>
    [SwaggerSchema(Description = "Token para renovar el JWT cuando expire (opcional)")]
    public string? RefreshToken { get; set; }

    /// <summary>
    /// Información básica del usuario autenticado
    /// </summary>
    [SwaggerSchema(Description = "Datos del usuario que inició sesión")]
    public UserBasicInfo User { get; set; } = new();
}

/// <summary>
/// Información básica del usuario (incluida en respuesta de login)
/// </summary>
[SwaggerSchema(Description = "Datos básicos del usuario autenticado")]
public class UserBasicInfo
{
    /// <summary>
    /// ID único del usuario
    /// </summary>
    [SwaggerSchema(Description = "Identificador único del usuario (GUID)")]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Email del usuario
    /// </summary>
    [SwaggerSchema(Description = "Correo electrónico del usuario", Format = "email")]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Nombre para mostrar
    /// </summary>
    [SwaggerSchema(Description = "Nombre visible del usuario")]
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Roles asignados al usuario
    /// </summary>
    [SwaggerSchema(Description = "Lista de roles del usuario (ej: 'User', 'Admin', 'Analyst')")]
    public List<string> Roles { get; set; } = new();

    /// <summary>
    /// Indica si el email está verificado
    /// </summary>
    [SwaggerSchema(Description = "True si el usuario ha confirmado su email")]
    public bool EmailVerified { get; set; }

    /// <summary>
    /// Fecha de creación de la cuenta
    /// </summary>
    [SwaggerSchema(Description = "Fecha de registro del usuario", Format = "date-time")]
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Modelo de solicitud para cerrar sesión
/// </summary>
[SwaggerSchema(Description = "Datos para invalidar un token JWT activo")]
public class LogoutRequest
{
    /// <summary>
    /// Token JWT a invalidar (opcional, se puede obtener del header Authorization)
    /// </summary>
    [SwaggerSchema(Description = "Token JWT a revocar. Si no se proporciona, se usa el token del header Authorization")]
    public string? Token { get; set; }

    /// <summary>
    /// Indica si se deben cerrar todas las sesiones del usuario
    /// </summary>
    [SwaggerSchema(Description = "Si es true, invalida todos los tokens activos del usuario en todos los dispositivos")]
    public bool LogoutAllDevices { get; set; } = false;
}

/// <summary>
/// Respuesta de logout exitoso
/// </summary>
[SwaggerSchema(Description = "Confirmación de cierre de sesión exitoso")]
public class LogoutResponse
{
    /// <summary>
    /// Mensaje de confirmación
    /// </summary>
    [SwaggerSchema(Description = "Mensaje de confirmación")]
    public string Message { get; set; } = "Sesión cerrada exitosamente";

    /// <summary>
    /// Indica si se cerraron sesiones adicionales
    /// </summary>
    [SwaggerSchema(Description = "True si se cerraron múltiples sesiones")]
    public bool AllDevicesLoggedOut { get; set; }

    /// <summary>
    /// Número de sesiones cerradas
    /// </summary>
    [SwaggerSchema(Description = "Cantidad de tokens invalidados")]
    public int SessionsTerminated { get; set; }
}

/// <summary>
/// Modelo de solicitud para refrescar token JWT
/// </summary>
[SwaggerSchema(Description = "Datos para renovar un token JWT expirado")]
public class RefreshTokenRequest
{
    /// <summary>
    /// Token JWT expirado o por expirar
    /// </summary>
    [Required(ErrorMessage = "El token es requerido")]
    [SwaggerSchema(Description = "Token JWT actual que se desea renovar")]
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// Token de refresco obtenido en el login
    /// </summary>
    [Required(ErrorMessage = "El refresh token es requerido")]
    [SwaggerSchema(Description = "Refresh token obtenido durante el login")]
    public string RefreshToken { get; set; } = string.Empty;
}

/// <summary>
/// Modelo de solicitud para recuperación de contraseña
/// </summary>
[SwaggerSchema(Description = "Solicitud de recuperación de contraseña olvidada")]
public class ForgotPasswordRequest
{
    /// <summary>
    /// Email de la cuenta a recuperar
    /// </summary>
    [Required(ErrorMessage = "El email es requerido")]
    [EmailAddress(ErrorMessage = "Formato de email inválido")]
    [SwaggerSchema(Description = "Email de la cuenta registrada", Format = "email")]
    public string Email { get; set; } = string.Empty;
}

/// <summary>
/// Modelo de solicitud para resetear contraseña
/// </summary>
[SwaggerSchema(Description = "Datos para establecer nueva contraseña con token de recuperación")]
public class ResetPasswordRequest
{
    /// <summary>
    /// Email del usuario
    /// </summary>
    [Required(ErrorMessage = "El email es requerido")]
    [EmailAddress(ErrorMessage = "Formato de email inválido")]
    [SwaggerSchema(Description = "Email de la cuenta", Format = "email")]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Token de recuperación enviado por email
    /// </summary>
    [Required(ErrorMessage = "El token es requerido")]
    [SwaggerSchema(Description = "Token de 6 dígitos recibido por email")]
    public string ResetToken { get; set; } = string.Empty;

    /// <summary>
    /// Nueva contraseña
    /// </summary>
    [Required(ErrorMessage = "La contraseña es requerida")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "La contraseña debe tener entre 8 y 100 caracteres")]
    [DataType(DataType.Password)]
    [SwaggerSchema(Description = "Nueva contraseña segura", Format = "password")]
    public string NewPassword { get; set; } = string.Empty;

    /// <summary>
    /// Confirmación de nueva contraseña
    /// </summary>
    [Required(ErrorMessage = "La confirmación de contraseña es requerida")]
    [Compare("NewPassword", ErrorMessage = "Las contraseñas no coinciden")]
    [DataType(DataType.Password)]
    [SwaggerSchema(Description = "Confirmación de la nueva contraseña", Format = "password")]
    public string ConfirmNewPassword { get; set; } = string.Empty;
}

