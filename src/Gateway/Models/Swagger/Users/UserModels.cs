using System.ComponentModel.DataAnnotations;
using Swashbuckle.AspNetCore.Annotations;

namespace Gateway.Models.Swagger.Users;

/// <summary>
/// DTO completo de usuario con toda la información
/// </summary>
[SwaggerSchema(Description = "Representación completa de un usuario en el sistema")]
public class UserDto
{
    /// <summary>
    /// ID único del usuario (GUID)
    /// </summary>
    [SwaggerSchema(Description = "Identificador único del usuario")]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Correo electrónico del usuario
    /// </summary>
    [SwaggerSchema(Description = "Email registrado", Format = "email")]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Nombre para mostrar del usuario
    /// </summary>
    [SwaggerSchema(Description = "Nombre visible en la plataforma")]
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Nombre completo del usuario (opcional)
    /// </summary>
    [SwaggerSchema(Description = "Nombre completo del usuario")]
    public string? FullName { get; set; }

    /// <summary>
    /// URL de la foto de perfil (opcional)
    /// </summary>
    [SwaggerSchema(Description = "URL de la imagen de perfil", Format = "uri")]
    public string? ProfilePictureUrl { get; set; }

    /// <summary>
    /// Roles asignados al usuario
    /// </summary>
    [SwaggerSchema(Description = "Lista de roles (ej: 'User', 'Admin', 'Analyst')")]
    public List<string> Roles { get; set; } = new();

    /// <summary>
    /// Indica si el usuario está activo
    /// </summary>
    [SwaggerSchema(Description = "False si la cuenta está deshabilitada o suspendida")]
    public bool IsActive { get; set; }

    /// <summary>
    /// Indica si el email está verificado
    /// </summary>
    [SwaggerSchema(Description = "True si el usuario confirmó su email")]
    public bool EmailVerified { get; set; }

    /// <summary>
    /// Fecha de creación de la cuenta
    /// </summary>
    [SwaggerSchema(Description = "Fecha de registro del usuario", Format = "date-time")]
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Fecha de última actualización del perfil
    /// </summary>
    [SwaggerSchema(Description = "Última modificación de datos del usuario", Format = "date-time")]
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Fecha del último inicio de sesión
    /// </summary>
    [SwaggerSchema(Description = "Última vez que el usuario se autenticó", Format = "date-time")]
    public DateTime? LastLoginAt { get; set; }
}

/// <summary>
/// Modelo de solicitud para crear un nuevo usuario (admin)
/// </summary>
[SwaggerSchema(Description = "Datos para crear un usuario (requiere rol Admin)")]
public class CreateUserRequest
{
    /// <summary>
    /// Correo electrónico del nuevo usuario
    /// </summary>
    [Required(ErrorMessage = "El email es requerido")]
    [EmailAddress(ErrorMessage = "Formato de email inválido")]
    [StringLength(100, ErrorMessage = "El email no puede exceder 100 caracteres")]
    [SwaggerSchema(Description = "Email único para el nuevo usuario", Format = "email")]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Contraseña inicial del usuario
    /// </summary>
    [Required(ErrorMessage = "La contraseña es requerida")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "La contraseña debe tener entre 8 y 100 caracteres")]
    [DataType(DataType.Password)]
    [SwaggerSchema(Description = "Contraseña inicial (el usuario puede cambiarla)", Format = "password")]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Nombre para mostrar
    /// </summary>
    [Required(ErrorMessage = "El nombre para mostrar es requerido")]
    [StringLength(50, ErrorMessage = "El nombre no puede exceder 50 caracteres")]
    [SwaggerSchema(Description = "Nombre visible del usuario")]
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Nombre completo del usuario
    /// </summary>
    [StringLength(100, ErrorMessage = "El nombre completo no puede exceder 100 caracteres")]
    [SwaggerSchema(Description = "Nombre completo opcional")]
    public string? FullName { get; set; }

    /// <summary>
    /// Roles a asignar al usuario
    /// </summary>
    [SwaggerSchema(Description = "Lista de roles iniciales (ej: ['User', 'Analyst'])")]
    public List<string> Roles { get; set; } = new() { "User" };

    /// <summary>
    /// Indica si se debe enviar email de bienvenida
    /// </summary>
    [SwaggerSchema(Description = "Si es true, envía email de bienvenida con instrucciones")]
    public bool SendWelcomeEmail { get; set; } = true;

    /// <summary>
    /// Indica si el usuario debe cambiar contraseña en primer login
    /// </summary>
    [SwaggerSchema(Description = "Forzar cambio de contraseña en primera autenticación")]
    public bool RequirePasswordChange { get; set; } = false;
}

/// <summary>
/// Modelo de solicitud para actualizar datos de usuario
/// </summary>
[SwaggerSchema(Description = "Datos modificables del perfil de usuario")]
public class UpdateUserRequest
{
    /// <summary>
    /// Nuevo nombre para mostrar
    /// </summary>
    [StringLength(50, ErrorMessage = "El nombre no puede exceder 50 caracteres")]
    [SwaggerSchema(Description = "Nuevo nombre visible (opcional)")]
    public string? DisplayName { get; set; }

    /// <summary>
    /// Nuevo nombre completo
    /// </summary>
    [StringLength(100, ErrorMessage = "El nombre completo no puede exceder 100 caracteres")]
    [SwaggerSchema(Description = "Nuevo nombre completo (opcional)")]
    public string? FullName { get; set; }

    /// <summary>
    /// Nuevo email (requiere verificación)
    /// </summary>
    [EmailAddress(ErrorMessage = "Formato de email inválido")]
    [StringLength(100, ErrorMessage = "El email no puede exceder 100 caracteres")]
    [SwaggerSchema(Description = "Cambiar email (se enviará verificación)", Format = "email")]
    public string? Email { get; set; }

    /// <summary>
    /// URL de nueva foto de perfil
    /// </summary>
    [Url(ErrorMessage = "URL inválida")]
    [StringLength(500, ErrorMessage = "La URL no puede exceder 500 caracteres")]
    [SwaggerSchema(Description = "URL de imagen de perfil", Format = "uri")]
    public string? ProfilePictureUrl { get; set; }
}

/// <summary>
/// Modelo de solicitud para cambiar contraseña
/// </summary>
[SwaggerSchema(Description = "Datos para cambiar la contraseña de un usuario autenticado")]
public class ChangePasswordRequest
{
    /// <summary>
    /// Contraseña actual del usuario
    /// </summary>
    [Required(ErrorMessage = "La contraseña actual es requerida")]
    [DataType(DataType.Password)]
    [SwaggerSchema(Description = "Contraseña actual para validar identidad", Format = "password")]
    public string CurrentPassword { get; set; } = string.Empty;

    /// <summary>
    /// Nueva contraseña deseada
    /// </summary>
    [Required(ErrorMessage = "La nueva contraseña es requerida")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "La contraseña debe tener entre 8 y 100 caracteres")]
    [DataType(DataType.Password)]
    [SwaggerSchema(Description = "Nueva contraseña segura", Format = "password")]
    public string NewPassword { get; set; } = string.Empty;

    /// <summary>
    /// Confirmación de nueva contraseña
    /// </summary>
    [Required(ErrorMessage = "La confirmación es requerida")]
    [Compare("NewPassword", ErrorMessage = "Las contraseñas no coinciden")]
    [DataType(DataType.Password)]
    [SwaggerSchema(Description = "Debe coincidir con la nueva contraseña", Format = "password")]
    public string ConfirmNewPassword { get; set; } = string.Empty;

    /// <summary>
    /// Indica si se deben cerrar otras sesiones tras cambiar contraseña
    /// </summary>
    [SwaggerSchema(Description = "Si es true, invalida tokens activos en otros dispositivos")]
    public bool LogoutOtherSessions { get; set; } = false;
}

/// <summary>
/// Respuesta de lista de usuarios paginada
/// </summary>
[SwaggerSchema(Description = "Lista paginada de usuarios")]
public class UserListResponse
{
    /// <summary>
    /// Lista de usuarios en la página actual
    /// </summary>
    [SwaggerSchema(Description = "Usuarios de la página actual")]
    public List<UserDto> Users { get; set; } = new();

    /// <summary>
    /// Número total de usuarios que cumplen el filtro
    /// </summary>
    [SwaggerSchema(Description = "Total de usuarios (todas las páginas)")]
    public int TotalCount { get; set; }

    /// <summary>
    /// Número de página actual (base 1)
    /// </summary>
    [SwaggerSchema(Description = "Página actual solicitada")]
    public int PageNumber { get; set; }

    /// <summary>
    /// Tamaño de página configurado
    /// </summary>
    [SwaggerSchema(Description = "Cantidad de usuarios por página")]
    public int PageSize { get; set; }

    /// <summary>
    /// Número total de páginas disponibles
    /// </summary>
    [SwaggerSchema(Description = "Total de páginas calculado")]
    public int TotalPages { get; set; }

    /// <summary>
    /// Indica si hay página anterior
    /// </summary>
    [SwaggerSchema(Description = "True si se puede ir a página anterior")]
    public bool HasPreviousPage { get; set; }

    /// <summary>
    /// Indica si hay página siguiente
    /// </summary>
    [SwaggerSchema(Description = "True si hay más páginas disponibles")]
    public bool HasNextPage { get; set; }
}

/// <summary>
/// Parámetros de búsqueda y filtrado de usuarios
/// </summary>
[SwaggerSchema(Description = "Filtros para listar usuarios")]
public class UserSearchParams
{
    /// <summary>
    /// Texto de búsqueda (busca en email, displayName, fullName)
    /// </summary>
    [SwaggerSchema(Description = "Buscar por email, nombre o nombre completo")]
    public string? SearchTerm { get; set; }

    /// <summary>
    /// Filtrar por rol específico
    /// </summary>
    [SwaggerSchema(Description = "Filtrar usuarios con rol específico (ej: 'Admin')")]
    public string? Role { get; set; }

    /// <summary>
    /// Filtrar por estado activo/inactivo
    /// </summary>
    [SwaggerSchema(Description = "True=activos, False=inactivos, null=todos")]
    public bool? IsActive { get; set; }

    /// <summary>
    /// Filtrar por email verificado
    /// </summary>
    [SwaggerSchema(Description = "True=verificados, False=sin verificar, null=todos")]
    public bool? EmailVerified { get; set; }

    /// <summary>
    /// Número de página (base 1)
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "El número de página debe ser mayor a 0")]
    [SwaggerSchema(Description = "Página a recuperar (default: 1)")]
    public int PageNumber { get; set; } = 1;

    /// <summary>
    /// Tamaño de página
    /// </summary>
    [Range(1, 100, ErrorMessage = "El tamaño de página debe estar entre 1 y 100")]
    [SwaggerSchema(Description = "Usuarios por página (default: 10, max: 100)")]
    public int PageSize { get; set; } = 10;

    /// <summary>
    /// Campo para ordenar resultados
    /// </summary>
    [SwaggerSchema(Description = "Campo de ordenamiento (Email, DisplayName, CreatedAt, LastLoginAt)")]
    public string? SortBy { get; set; } = "CreatedAt";

    /// <summary>
    /// Dirección de ordenamiento
    /// </summary>
    [SwaggerSchema(Description = "Dirección de ordenamiento (asc, desc)")]
    public string? SortDirection { get; set; } = "desc";
}

/// <summary>
/// Respuesta de operación exitosa sobre usuario
/// </summary>
[SwaggerSchema(Description = "Confirmación de operación exitosa")]
public class UserOperationResponse
{
    /// <summary>
    /// Indica si la operación fue exitosa
    /// </summary>
    [SwaggerSchema(Description = "True si la operación se completó correctamente")]
    public bool Success { get; set; }

    /// <summary>
    /// Mensaje descriptivo de la operación
    /// </summary>
    [SwaggerSchema(Description = "Mensaje de confirmación o detalle")]
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Datos del usuario afectado (opcional)
    /// </summary>
    [SwaggerSchema(Description = "Usuario creado/actualizado/eliminado")]
    public UserDto? User { get; set; }
}

/// <summary>
/// Respuesta simple con mensaje
/// </summary>
[SwaggerSchema(Description = "Respuesta con mensaje de confirmación")]
public class MessageResponse
{
    /// <summary>
    /// Indica si la operación fue exitosa
    /// </summary>
    [SwaggerSchema(Description = "True si la operación se completó correctamente")]
    public bool Success { get; set; }

    /// <summary>
    /// Mensaje descriptivo
    /// </summary>
    [SwaggerSchema(Description = "Mensaje de confirmación o información")]
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Respuesta con lista de usuarios
/// </summary>
[SwaggerSchema(Description = "Lista de usuarios del sistema")]
public class UsersListResponse
{
    /// <summary>
    /// Lista de usuarios
    /// </summary>
    [SwaggerSchema(Description = "Usuarios recuperados")]
    public List<UserDto> Users { get; set; } = new();

    /// <summary>
    /// Número total de usuarios
    /// </summary>
    [SwaggerSchema(Description = "Cantidad total de usuarios")]
    public int TotalCount { get; set; }

    /// <summary>
    /// Mensaje adicional (opcional)
    /// </summary>
    [SwaggerSchema(Description = "Información adicional sobre la consulta")]
    public string? Message { get; set; }
}

/// <summary>
/// Solicitud para actualización parcial de usuario
/// </summary>
[SwaggerSchema(Description = "Datos para actualización parcial (solo campos a modificar)")]
public class PatchUserRequest
{
    /// <summary>
    /// Nuevo nombre para mostrar (opcional)
    /// </summary>
    [StringLength(50, ErrorMessage = "El nombre no puede exceder 50 caracteres")]
    [SwaggerSchema(Description = "Actualizar nombre de usuario")]
    public string? DisplayName { get; set; }

    /// <summary>
    /// Nuevo nombre completo (opcional)
    /// </summary>
    [StringLength(100, ErrorMessage = "El nombre completo no puede exceder 100 caracteres")]
    [SwaggerSchema(Description = "Actualizar nombre completo")]
    public string? FullName { get; set; }

    /// <summary>
    /// Nueva URL de foto de perfil (opcional)
    /// </summary>
    [Url(ErrorMessage = "Debe ser una URL válida")]
    [SwaggerSchema(Description = "Actualizar imagen de perfil", Format = "uri")]
    public string? ProfilePictureUrl { get; set; }

    /// <summary>
    /// Nuevos roles (opcional, solo admin)
    /// </summary>
    [SwaggerSchema(Description = "Actualizar roles del usuario (requiere permisos de admin)")]
    public List<string>? Roles { get; set; }

    /// <summary>
    /// Actualizar estado activo/inactivo (opcional, solo admin)
    /// </summary>
    [SwaggerSchema(Description = "Activar o desactivar cuenta (requiere permisos de admin)")]
    public bool? IsActive { get; set; }
}

