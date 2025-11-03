using Microsoft.AspNetCore.Mvc;
using Gateway.Models.Swagger.Users;
using Gateway.Models.Swagger.Shared;
using Swashbuckle.AspNetCore.Annotations;

namespace Gateway.Controllers;

/// <summary>
/// Controller proxy para documentación de Users API en Swagger.
/// Este controller NO implementa lógica real - solo documenta los endpoints del microservicio Users.
/// Las peticiones reales son manejadas por YARP reverse proxy.
/// IMPORTANTE: Las rutas deben coincidir con las rutas reales para que Swagger las documente correctamente.
/// YARP interceptará las peticiones HTTP reales antes de que lleguen a estos métodos.
/// 
/// **🔹 CONSUMO A TRAVÉS DEL GATEWAY:**
/// Todos los endpoints de esta API deben consumirse a través del endpoint universal del Gateway:
/// 
/// **POST /api/v1/translate**
/// 
/// **Ejemplo de request:**
/// ```json
/// {
///   "service": "users",
///   "method": "POST",
///   "path": "/api/users/auth/login",
///   "headers": {
///     "Content-Type": "application/json"
///   },
///   "body": "{\"email\":\"user@example.com\",\"password\":\"Pass123\"}"
/// }
/// ```
/// 
/// Los endpoints documentados aquí muestran la estructura de **path**, **method** y **body** 
/// que debes usar en el campo correspondiente del request a /api/v1/translate.
/// </summary>
[ApiController]
[Route("api/users")]
[ApiExplorerSettings(GroupName = "users", IgnoreApi = false)]
[Produces("application/json")]
[SwaggerTag("Endpoints de autenticación y gestión de usuarios del microservicio Users")]
public class UsersProxyController : ControllerBase
{
    // ============================================================================
    // SECCIÓN: AUTENTICACIÓN (5 endpoints)
    // ============================================================================

    /// <summary>
    /// Registrar un nuevo usuario en el sistema
    /// </summary>
    /// <remarks>
    /// Crea una nueva cuenta de usuario con email y contraseña.
    /// 
    /// **Flujo:**
    /// 1. Valida formato de email (único en sistema)
    /// 2. Valida contraseña (mín 8 caracteres, mayúsculas, minúsculas, números)
    /// 3. Verifica que contraseñas coincidan
    /// 4. Crea usuario con rol "User" por defecto
    /// 5. Envía email de verificación
    /// 6. Retorna token JWT para autenticación inmediata
    /// 
    /// **Validaciones:**
    /// - Email debe ser válido y único
    /// - Contraseña: mínimo 8 caracteres
    /// - Contraseña y confirmación deben coincidir
    /// - Debe aceptar términos y condiciones
    /// 
    /// **Headers requeridos:**
    /// - `X-Gateway-Secret`: Secreto del gateway (manejado automáticamente)
    /// 
    /// **Ejemplo de request:**
    /// ```json
    /// {
    ///   "email": "usuario@example.com",
    ///   "password": "MiPassword123",
    ///   "confirmPassword": "MiPassword123",
    ///   "displayName": "Usuario Ejemplo",
    ///   "acceptTerms": true
    /// }
    /// ```
    /// 
    /// **🔹 CONSUMO A TRAVÉS DEL GATEWAY /api/v1/translate:**
    /// ```json
    /// {
    ///   "service": "users",
    ///   "method": "POST",
    ///   "path": "/api/users/auth/register",
    ///   "headers": {
    ///     "Content-Type": "application/json"
    ///   },
    ///   "body": "{\"email\":\"usuario@example.com\",\"password\":\"MiPassword123\",\"confirmPassword\":\"MiPassword123\",\"displayName\":\"Usuario Ejemplo\",\"acceptTerms\":true}"
    /// }
    /// ```
    /// 
    /// **Ejemplo de respuesta exitosa:**
    /// ```json
    /// {
    ///   "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    ///   "tokenType": "Bearer",
    ///   "expiresIn": 86400,
    ///   "expiresAt": "2025-10-26T12:00:00Z",
    ///   "user": {
    ///     "userId": "123e4567-e89b-12d3-a456-426614174000",
    ///     "email": "usuario@example.com",
    ///     "displayName": "Usuario Ejemplo",
    ///     "roles": ["User"],
    ///     "emailVerified": false
    ///   }
    /// }
    /// ```
    /// </remarks>
    /// <param name="request">Datos de registro del nuevo usuario</param>
    /// <returns>Token JWT y datos básicos del usuario creado</returns>
    /// <response code="201">Usuario registrado exitosamente. Retorna token JWT y datos del usuario.</response>
    /// <response code="400">Datos de registro inválidos. Revise los errores de validación.</response>
    /// <response code="403">Gateway Secret inválido o ausente.</response>
    /// <response code="409">El email ya está registrado en el sistema.</response>
    /// <response code="500">Error interno del servidor al procesar el registro.</response>
    [HttpPost("auth/register")]
    [SwaggerOperation(
        OperationId = "RegisterUser",
        Summary = "Registrar nuevo usuario",
        Description = "Crea una cuenta de usuario nueva y retorna token JWT para autenticación inmediata",
        Tags = new[] { "AUTH" }
    )]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public IActionResult Register([FromBody] RegisterRequest request)
    {
        // Este controller es solo para documentación Swagger.
        // Las peticiones reales son manejadas por YARP reverse proxy.
        throw new NotImplementedException("Este endpoint es manejado por el reverse proxy YARP");
    }

    /// <summary>
    /// Iniciar sesión con credenciales existentes
    /// </summary>
    /// <remarks>
    /// Autentica un usuario con email y contraseña, retornando un token JWT.
    /// 
    /// **Flujo:**
    /// 1. Valida credenciales (email + password)
    /// 2. Verifica que usuario esté activo
    /// 3. Genera token JWT con información del usuario
    /// 4. Registra último login (IP, timestamp, dispositivo)
    /// 5. Retorna token y datos del usuario
    /// 
    /// **Tipos de token:**
    /// - **RememberMe = false**: Token válido por 24 horas (86,400 segundos)
    /// - **RememberMe = true**: Token válido por 30 días (2,592,000 segundos)
    /// 
    /// **Headers requeridos:**
    /// - `X-Gateway-Secret`: Secreto del gateway (manejado automáticamente)
    /// 
    /// **Uso del token retornado:**
    /// 
    /// En requests subsecuentes, incluir en header:
    /// ```
    /// Authorization: Bearer {token}
    /// ```
    /// 
    /// **Ejemplo de request:**
    /// ```json
    /// {
    ///   "email": "usuario@example.com",
    ///   "password": "MiPassword123",
    ///   "rememberMe": false
    /// }
    /// ```
    /// 
    /// **🔹 CONSUMO A TRAVÉS DEL GATEWAY /api/v1/translate:**
    /// ```json
    /// {
    ///   "service": "users",
    ///   "method": "POST",
    ///   "path": "/api/users/auth/login",
    ///   "headers": {
    ///     "Content-Type": "application/json"
    ///   },
    ///   "body": "{\"email\":\"usuario@example.com\",\"password\":\"MiPassword123\",\"rememberMe\":false}"
    /// }
    /// ```
    /// 
    /// **Ejemplo de respuesta exitosa:**
    /// ```json
    /// {
    ///   "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    ///   "tokenType": "Bearer",
    ///   "expiresIn": 86400,
    ///   "expiresAt": "2025-10-26T12:00:00Z",
    ///   "refreshToken": "abc123xyz...",
    ///   "user": {
    ///     "userId": "123e4567-e89b-12d3-a456-426614174000",
    ///     "email": "usuario@example.com",
    ///     "displayName": "Usuario Ejemplo",
    ///     "roles": ["User", "Analyst"],
    ///     "emailVerified": true,
    ///     "createdAt": "2025-01-15T10:00:00Z"
    ///   }
    /// }
    /// ```
    /// </remarks>
    /// <param name="request">Credenciales de inicio de sesión</param>
    /// <returns>Token JWT válido y datos del usuario autenticado</returns>
    /// <response code="200">Login exitoso. Retorna token JWT y datos del usuario.</response>
    /// <response code="400">Credenciales inválidas o incompletas.</response>
    /// <response code="401">Email o contraseña incorrectos.</response>
    /// <response code="403">Gateway Secret inválido o cuenta de usuario deshabilitada.</response>
    /// <response code="500">Error interno del servidor al procesar el login.</response>
    [HttpPost("auth/login")]
    [SwaggerOperation(
        OperationId = "LoginUser",
        Summary = "Iniciar sesión",
        Description = "Autentica un usuario con email y contraseña, retornando token JWT válido",
        Tags = new[] { "AUTH" }
    )]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public IActionResult Login([FromBody] LoginRequest request)
    {
        throw new NotImplementedException("Este endpoint es manejado por el reverse proxy YARP");
    }

    /// <summary>
    /// Cerrar sesión e invalidar token JWT
    /// </summary>
    /// <remarks>
    /// Invalida el token JWT actual, opcionalmente cerrando todas las sesiones del usuario.
    /// 
    /// **Flujo:**
    /// 1. Extrae token del header Authorization (o del body)
    /// 2. Invalida token en lista negra (blacklist)
    /// 3. Opcionalmente invalida todos los tokens del usuario
    /// 4. Registra evento de logout para auditoría
    /// 
    /// **Opciones:**
    /// - **LogoutAllDevices = false**: Solo cierra sesión actual (1 token)
    /// - **LogoutAllDevices = true**: Cierra TODAS las sesiones del usuario (todos los dispositivos)
    /// 
    /// **Headers requeridos:**
    /// - `X-Gateway-Secret`: Secreto del gateway
    /// - `Authorization: Bearer {token}`: Token JWT a invalidar
    /// 
    /// **Ejemplo de request (logout simple):**
    /// ```json
    /// {
    ///   "logoutAllDevices": false
    /// }
    /// ```
    /// 
    /// **Ejemplo de request (logout en todos los dispositivos):**
    /// ```json
    /// {
    ///   "logoutAllDevices": true
    /// }
    /// ```
    /// 
    /// **🔹 CONSUMO A TRAVÉS DEL GATEWAY /api/v1/translate:**
    /// ```json
    /// {
    ///   "service": "users",
    ///   "method": "POST",
    ///   "path": "/api/users/auth/logout",
    ///   "headers": {
    ///     "Content-Type": "application/json",
    ///     "Authorization": "Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
    ///   },
    ///   "body": "{\"logoutAllDevices\":false}"
    /// }
    /// ```
    /// 
    /// **Ejemplo de respuesta exitosa:**
    /// ```json
    /// {
    ///   "message": "Sesión cerrada exitosamente",
    ///   "allDevicesLoggedOut": false,
    ///   "sessionsTerminated": 1
    /// }
    /// ```
    /// 
    /// **Después del logout:**
    /// - El token ya no será válido para requests
    /// - Intentar usarlo retornará error 401 Unauthorized
    /// - Debe hacer login nuevamente para obtener nuevo token
    /// </remarks>
    /// <param name="request">Opciones de cierre de sesión</param>
    /// <returns>Confirmación de sesión cerrada</returns>
    /// <response code="200">Logout exitoso. Token(s) invalidado(s) correctamente.</response>
    /// <response code="400">Request inválido.</response>
    /// <response code="401">Token JWT inválido, expirado o ya invalidado.</response>
    /// <response code="403">Gateway Secret inválido.</response>
    /// <response code="500">Error interno del servidor al procesar el logout.</response>
    [HttpPost("auth/logout")]
    [SwaggerOperation(
        OperationId = "LogoutUser",
        Summary = "Cerrar sesión",
        Description = "Invalida el token JWT actual, opcionalmente cerrando todas las sesiones del usuario en todos los dispositivos",
        Tags = new[] { "AUTH" }
    )]
    [ProducesResponseType(typeof(LogoutResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public IActionResult Logout([FromBody] LogoutRequest request)
    {
        throw new NotImplementedException("Este endpoint es manejado por el reverse proxy YARP");
    }

    /// <summary>
    /// Restablecer contraseña de usuario
    /// </summary>
    /// <remarks>
    /// Permite restablecer/cambiar la contraseña de un usuario mediante su email.
    /// 
    /// **Uso típico:**
    /// - Usuario olvidó su contraseña
    /// - Cambio de contraseña forzado por administrador
    /// - Reseteo por motivos de seguridad
    /// 
    /// **Flujo recomendado:**
    /// 1. Usuario solicita reset (por email/teléfono)
    /// 2. Sistema envía código de verificación
    /// 3. Usuario confirma con código
    /// 4. Llama este endpoint con nueva contraseña
    /// 
    /// **Seguridad:**
    /// - La contraseña se hashea antes de guardar
    /// - Todas las sesiones activas se invalidan (logout global)
    /// - Se actualiza timestamp `UpdatedAt`
    /// 
    /// **Ejemplo de request:**
    /// ```json
    /// {
    ///   "email": "usuario@example.com",
    ///   "newPassword": "NuevaPassword123!"
    /// }
    /// ```
    /// 
    /// **🔹 CONSUMO A TRAVÉS DEL GATEWAY /api/v1/translate:**
    /// ```json
    /// {
    ///   "service": "users",
    ///   "method": "POST",
    ///   "path": "/api/users/auth/reset-password",
    ///   "headers": {
    ///     "Content-Type": "application/json"
    ///   },
    ///   "body": "{\"email\":\"usuario@example.com\",\"newPassword\":\"NuevaPassword123!\"}"
    /// }
    /// ```
    /// </remarks>
    /// <param name="request">Email del usuario y nueva contraseña</param>
    /// <returns>Confirmación de cambio</returns>
    /// <response code="200">Contraseña actualizada exitosamente.</response>
    /// <response code="400">Email o contraseña inválidos (formato).</response>
    /// <response code="404">Usuario no encontrado.</response>
    /// <response code="500">Error interno del servidor.</response>
    [HttpPost("auth/reset-password")]
    [SwaggerOperation(
        OperationId = "ResetPassword",
        Summary = "Restablecer contraseña",
        Description = "Cambia la contraseña de un usuario e invalida todas sus sesiones. Requiere email y nueva contraseña.",
        Tags = new[] { "AUTH" }
    )]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public IActionResult ResetPassword([FromBody] ResetPasswordRequest request)
    {
        throw new NotImplementedException("Este endpoint es manejado por el reverse proxy YARP");
    }

    /// <summary>
    /// Confirmar email de usuario
    /// </summary>
    /// <remarks>
    /// Marca el email de un usuario como confirmado después de la verificación.
    /// 
    /// **Flujo típico:**
    /// 1. Usuario se registra → EmailConfirmed = false
    /// 2. Sistema envía correo con link/código de confirmación
    /// 3. Usuario hace clic o ingresa código
    /// 4. Se llama este endpoint con userId
    /// 5. EmailConfirmed se actualiza a true
    /// 
    /// **Implicaciones:**
    /// - Algunos endpoints pueden requerir email confirmado
    /// - Aumenta confianza en la identidad del usuario
    /// - Habilita notificaciones por email
    /// 
    /// **Ejemplo de uso:**
    /// ```
    /// POST /api/users/auth/confirm-email/42
    /// ```
    /// 
    /// **🔹 CONSUMO A TRAVÉS DEL GATEWAY /api/v1/translate:**
    /// ```json
    /// {
    ///   "service": "users",
    ///   "method": "POST",
    ///   "path": "/api/users/auth/confirm-email/42",
    ///   "headers": {
    ///     "Content-Type": "application/json"
    ///   }
    /// }
    /// ```
    /// </remarks>
    /// <param name="userId">ID del usuario a confirmar</param>
    /// <returns>Confirmación de la operación</returns>
    /// <response code="200">Email confirmado exitosamente.</response>
    /// <response code="404">Usuario no encontrado.</response>
    /// <response code="500">Error interno del servidor.</response>
    [HttpPost("auth/confirm-email/{userId}")]
    [SwaggerOperation(
        OperationId = "ConfirmEmail",
        Summary = "Confirmar email",
        Description = "Marca el email de un usuario como confirmado tras verificación.",
        Tags = new[] { "AUTH" }
    )]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public IActionResult ConfirmEmail([FromRoute] int userId)
    {
        throw new NotImplementedException("Este endpoint es manejado por el reverse proxy YARP");
    }

    // ============================================================================
    // SECCIÓN: GESTIÓN DE USUARIOS (10 endpoints)
    // ============================================================================

    /// <summary>
    /// Listar todos los usuarios del sistema
    /// </summary>
    /// <remarks>
    /// Recupera la lista completa de todos los usuarios registrados en el sistema.
    /// 
    /// **Restricciones:**
    /// - Solo administradores pueden listar todos los usuarios
    /// - Usuarios normales recibirán 403 Forbidden
    /// 
    /// **Headers requeridos:**
    /// - `X-Gateway-Secret`: Secreto del gateway
    /// - `Authorization: Bearer {token}`: Token JWT válido con rol Admin
    /// 
    /// **🔹 CONSUMO A TRAVÉS DEL GATEWAY /api/v1/translate:**
    /// ```json
    /// {
    ///   "service": "users",
    ///   "method": "GET",
    ///   "path": "/api/users",
    ///   "headers": {
    ///     "Authorization": "Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
    ///   }
    /// }
    /// ```
    /// 
    /// **Ejemplo de respuesta exitosa:**
    /// ```json
    /// {
    ///   "users": [
    ///     {
    ///       "userId": "123e4567-e89b-12d3-a456-426614174000",
    ///       "email": "admin@example.com",
    ///       "displayName": "Admin User",
    ///       "role": "Admin",
    ///       "isActive": true
    ///     },
    ///     {
    ///       "userId": "987fcdeb-51a2-43f1-b456-426614174001",
    ///       "email": "user@example.com",
    ///       "displayName": "Regular User",
    ///       "role": "User",
    ///       "isActive": true
    ///     }
    ///   ],
    ///   "total": 2
    /// }
    /// ```
    /// </remarks>
    /// <returns>Lista completa de usuarios</returns>
    /// <response code="200">Usuarios recuperados exitosamente.</response>
    /// <response code="401">Token JWT inválido o expirado.</response>
    /// <response code="403">No tiene permisos de administrador.</response>
    /// <response code="500">Error interno del servidor.</response>
    [HttpGet]
    [SwaggerOperation(
        OperationId = "GetAllUsers",
        Summary = "Listar todos los usuarios",
        Description = "Retorna lista completa de todos los usuarios del sistema. Requiere rol de administrador.",
        Tags = new[] { "USERS" }
    )]
    [ProducesResponseType(typeof(UsersListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public IActionResult GetAllUsers()
    {
        throw new NotImplementedException("Este endpoint es manejado por el reverse proxy YARP");
    }

    /// <summary>
    /// Obtener información de un usuario específico por ID
    /// </summary>
    /// <remarks>
    /// Retorna información completa de un usuario específico.
    /// 
    /// **Permisos:**
    /// - **Usuarios normales**: Solo pueden ver su propio perfil (su userId)
    /// - **Administradores**: Pueden ver cualquier perfil
    /// 
    /// **Headers requeridos:**
    /// - `X-Gateway-Secret`: Secreto del gateway
    /// - `Authorization: Bearer {token}`: Token JWT válido
    /// 
    /// **🔹 CONSUMO A TRAVÉS DEL GATEWAY /api/v1/translate:**
    /// ```json
    /// {
    ///   "service": "users",
    ///   "method": "GET",
    ///   "path": "/api/users/123e4567-e89b-12d3-a456-426614174000",
    ///   "headers": {
    ///     "Authorization": "Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
    ///   }
    /// }
    /// ```
    /// 
    /// **Ejemplo de respuesta exitosa:**
    /// ```json
    /// {
    ///   "userId": "123e4567-e89b-12d3-a456-426614174000",
    ///   "email": "usuario@example.com",
    ///   "displayName": "Usuario Ejemplo",
    ///   "fullName": "Juan Pérez García",
    ///   "profilePictureUrl": "https://cdn.example.com/avatars/user123.jpg",
    ///   "roles": ["User", "Analyst"],
    ///   "isActive": true,
    ///   "emailVerified": true,
    ///   "createdAt": "2025-01-15T10:00:00Z",
    ///   "updatedAt": "2025-10-20T15:30:00Z",
    ///   "lastLoginAt": "2025-10-25T09:00:00Z"
    /// }
    /// ```
    /// </remarks>
    /// <param name="id">ID único del usuario (GUID)</param>
    /// <returns>Información completa del usuario</returns>
    /// <response code="200">Usuario encontrado. Retorna datos completos.</response>
    /// <response code="401">Token JWT inválido o expirado.</response>
    /// <response code="403">No tiene permisos para ver este usuario (solo admin o propio perfil).</response>
    /// <response code="404">Usuario no encontrado con el ID especificado.</response>
    /// <response code="500">Error interno del servidor.</response>
    [HttpGet("{id}")]
    [SwaggerOperation(
        OperationId = "GetUserById",
        Summary = "Obtener usuario por ID",
        Description = "Retorna información completa de un usuario específico. Usuarios normales solo pueden ver su propio perfil.",
        Tags = new[] { "USERS" }
    )]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public IActionResult GetUserById([FromRoute] string id)
    {
        throw new NotImplementedException("Este endpoint es manejado por el reverse proxy YARP");
    }

    /// <summary>
    /// Obtener usuario por email
    /// </summary>
    /// <remarks>
    /// Busca y retorna información de un usuario utilizando su dirección de email.
    /// 
    /// **Restricciones:**
    /// - Usuarios normales solo pueden consultar su propio email
    /// - Administradores pueden consultar cualquier email
    /// 
    /// **Headers requeridos:**
    /// - `X-Gateway-Secret`: Secreto del gateway
    /// - `Authorization: Bearer {token}`: Token JWT válido
    /// 
    /// **Ejemplo de uso:**
    /// ```
    /// GET /api/users/by-email?email=usuario@example.com
    /// ```
    /// 
    /// **🔹 CONSUMO A TRAVÉS DEL GATEWAY /api/v1/translate:**
    /// ```json
    /// {
    ///   "service": "users",
    ///   "method": "GET",
    ///   "path": "/api/users/by-email",
    ///   "query": {
    ///     "email": "usuario@example.com"
    ///   },
    ///   "headers": {
    ///     "Authorization": "Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
    ///   }
    /// }
    /// ```
    /// </remarks>
    /// <param name="email">Email del usuario a buscar</param>
    /// <returns>Información del usuario</returns>
    /// <response code="200">Usuario encontrado.</response>
    /// <response code="401">Token JWT inválido o expirado.</response>
    /// <response code="403">No tiene permisos para consultar este email.</response>
    /// <response code="404">Usuario no encontrado con ese email.</response>
    /// <response code="500">Error interno del servidor.</response>
    [HttpGet("by-email")]
    [SwaggerOperation(
        OperationId = "GetUserByEmail",
        Summary = "Obtener usuario por email",
        Description = "Busca usuario por dirección de email. Usuarios solo pueden consultar su propio email.",
        Tags = new[] { "USERS" }
    )]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public IActionResult GetUserByEmail([FromQuery] string email)
    {
        throw new NotImplementedException("Este endpoint es manejado por el reverse proxy YARP");
    }

    /// <summary>
    /// Crear un nuevo usuario (solo administradores)
    /// </summary>
    /// <remarks>
    /// Crea un nuevo usuario en el sistema. **Requiere rol de Administrador.**
    /// 
    /// **Diferencias con /auth/register:**
    /// - Register: Usuario se auto-registra (público)
    /// - Create: Admin crea usuario (privado, requiere autenticación)
    /// - Create permite asignar roles específicos
    /// - Create puede forzar cambio de contraseña en primer login
    /// 
    /// **Permisos requeridos:**
    /// - Rol: `Admin`
    /// 
    /// **Flujo:**
    /// 1. Valida que usuario autenticado sea Admin
    /// 2. Verifica que email sea único
    /// 3. Crea usuario con roles especificados
    /// 4. Opcionalmente envía email de bienvenida
    /// 5. Opcionalmente marca para cambio de contraseña obligatorio
    /// 
    /// **Headers requeridos:**
    /// - `X-Gateway-Secret`: Secreto del gateway
    /// - `Authorization: Bearer {token}`: Token JWT con rol Admin
    /// 
    /// **Ejemplo de request:**
    /// ```json
    /// {
    ///   "email": "nuevo.usuario@example.com",
    ///   "password": "TempPassword123",
    ///   "displayName": "Nuevo Usuario",
    ///   "fullName": "María González López",
    ///   "roles": ["User", "Analyst"],
    ///   "sendWelcomeEmail": true,
    ///   "requirePasswordChange": true
    /// }
    /// ```
    /// 
    /// **🔹 CONSUMO A TRAVÉS DEL GATEWAY /api/v1/translate:**
    /// ```json
    /// {
    ///   "service": "users",
    ///   "method": "POST",
    ///   "path": "/api/users",
    ///   "headers": {
    ///     "Content-Type": "application/json",
    ///     "Authorization": "Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
    ///   },
    ///   "body": "{\"email\":\"nuevo.usuario@example.com\",\"password\":\"TempPassword123\",\"displayName\":\"Nuevo Usuario\",\"fullName\":\"María González López\",\"roles\":[\"User\",\"Analyst\"],\"sendWelcomeEmail\":true,\"requirePasswordChange\":true}"
    /// }
    /// ```
    /// 
    /// **Ejemplo de respuesta exitosa:**
    /// ```json
    /// {
    ///   "success": true,
    ///   "message": "Usuario creado exitosamente",
    ///   "user": {
    ///     "userId": "456e7890-f12g-34h5-i678-901234567890",
    ///     "email": "nuevo.usuario@example.com",
    ///     "displayName": "Nuevo Usuario",
    ///     "roles": ["User", "Analyst"],
    ///     "isActive": true,
    ///     "emailVerified": false
    ///   }
    /// }
    /// ```
    /// </remarks>
    /// <param name="request">Datos del usuario a crear</param>
    /// <returns>Confirmación de usuario creado con sus datos</returns>
    /// <response code="201">Usuario creado exitosamente.</response>
    /// <response code="400">Datos inválidos o email ya existe.</response>
    /// <response code="401">Token JWT inválido o expirado.</response>
    /// <response code="403">No tiene permisos de administrador para crear usuarios.</response>
    /// <response code="500">Error interno del servidor al crear usuario.</response>
    [HttpPost]
    [SwaggerOperation(
        OperationId = "CreateUser",
        Summary = "Crear usuario (admin)",
        Description = "Crea un nuevo usuario en el sistema. Solo disponible para administradores.",
        Tags = new[] { "USERS" }
    )]
    [ProducesResponseType(typeof(UserOperationResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public IActionResult CreateUser([FromBody] CreateUserRequest request)
    {
        throw new NotImplementedException("Este endpoint es manejado por el reverse proxy YARP");
    }

    /// <summary>
    /// Actualizar información de un usuario
    /// </summary>
    /// <remarks>
    /// Actualiza datos del perfil de un usuario (nombre, email, foto de perfil).
    /// 
    /// **Permisos:**
    /// - **Usuarios normales**: Solo pueden actualizar su propio perfil
    /// - **Administradores**: Pueden actualizar cualquier perfil
    /// 
    /// **Campos actualizables:**
    /// - DisplayName: Nombre visible
    /// - FullName: Nombre completo
    /// - Email: Correo electrónico (requiere re-verificación)
    /// - ProfilePictureUrl: URL de foto de perfil
    /// 
    /// **Nota sobre cambio de email:**
    /// - Al cambiar email, se envía verificación al nuevo email
    /// - El email anterior sigue activo hasta confirmar el nuevo
    /// - EmailVerified se marca como false hasta confirmar
    /// 
    /// **Headers requeridos:**
    /// - `X-Gateway-Secret`: Secreto del gateway
    /// - `Authorization: Bearer {token}`: Token JWT válido
    /// 
    /// **Ejemplo de request (actualización parcial):**
    /// ```json
    /// {
    ///   "displayName": "Nuevo Nombre",
    ///   "fullName": "Juan Carlos Pérez García"
    /// }
    /// ```
    /// 
    /// **🔹 CONSUMO A TRAVÉS DEL GATEWAY /api/v1/translate:**
    /// ```json
    /// {
    ///   "service": "users",
    ///   "method": "PUT",
    ///   "path": "/api/users/123e4567-e89b-12d3-a456-426614174000",
    ///   "headers": {
    ///     "Content-Type": "application/json",
    ///     "Authorization": "Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
    ///   },
    ///   "body": "{\"displayName\":\"Nuevo Nombre\",\"fullName\":\"Juan Carlos Pérez García\"}"
    /// }
    /// ```
    /// 
    /// **Ejemplo de respuesta exitosa:**
    /// ```json
    /// {
    ///   "success": true,
    ///   "message": "Usuario actualizado exitosamente",
    ///   "user": {
    ///     "userId": "123e4567-e89b-12d3-a456-426614174000",
    ///     "email": "usuario@example.com",
    ///     "displayName": "Nuevo Nombre",
    ///     "fullName": "Juan Carlos Pérez García",
    ///     "updatedAt": "2025-10-25T12:00:00Z"
    ///   }
    /// }
    /// ```
    /// </remarks>
    /// <param name="id">ID del usuario a actualizar</param>
    /// <param name="request">Datos a actualizar (campos opcionales)</param>
    /// <returns>Confirmación de actualización con datos actualizados</returns>
    /// <response code="200">Usuario actualizado exitosamente.</response>
    /// <response code="400">Datos inválidos o email ya existe.</response>
    /// <response code="401">Token JWT inválido o expirado.</response>
    /// <response code="403">No tiene permisos para actualizar este usuario.</response>
    /// <response code="404">Usuario no encontrado.</response>
    /// <response code="500">Error interno del servidor.</response>
    [HttpPut("{id}")]
    [SwaggerOperation(
        OperationId = "UpdateUser",
        Summary = "Actualizar usuario",
        Description = "Actualiza información del perfil de usuario. Usuarios normales solo pueden actualizar su propio perfil.",
        Tags = new[] { "USERS" }
    )]
    [ProducesResponseType(typeof(UserOperationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public IActionResult UpdateUser([FromRoute] string id, [FromBody] UpdateUserRequest request)
    {
        throw new NotImplementedException("Este endpoint es manejado por el reverse proxy YARP");
    }

    /// <summary>
    /// Actualizar parcialmente un usuario
    /// </summary>
    /// <remarks>
    /// Actualiza solo los campos especificados del perfil de usuario (actualización parcial).
    /// 
    /// **Diferencias con PUT:**
    /// - **PATCH**: Actualiza solo los campos enviados (parcial)
    /// - **PUT**: Actualiza todos los campos (completo)
    /// 
    /// **Restricciones:**
    /// - Usuarios normales: Solo su propio perfil
    /// - Administradores: Cualquier usuario
    /// 
    /// **Headers requeridos:**
    /// - `X-Gateway-Secret`: Secreto del gateway
    /// - `Authorization: Bearer {token}`: Token JWT válido
    /// 
    /// **Campos actualizables:**
    /// - `displayName`: Nombre para mostrar
    /// - `fullName`: Nombre completo
    /// - `profilePictureUrl`: URL de foto de perfil
    /// - `emailVerified`: Estado de verificación (solo admin)
    /// - `isActive`: Estado activo/inactivo (solo admin)
    /// 
    /// **Ejemplo de request (actualización parcial):**
    /// ```json
    /// {
    ///   "displayName": "Nuevo Nombre",
    ///   "profilePictureUrl": "https://cdn.example.com/new-avatar.jpg"
    /// }
    /// ```
    /// 
    /// **🔹 CONSUMO A TRAVÉS DEL GATEWAY /api/v1/translate:**
    /// ```json
    /// {
    ///   "service": "users",
    ///   "method": "PATCH",
    ///   "path": "/api/users/123e4567-e89b-12d3-a456-426614174000",
    ///   "headers": {
    ///     "Content-Type": "application/json",
    ///     "Authorization": "Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
    ///   },
    ///   "body": "{\"displayName\":\"Nuevo Nombre\",\"profilePictureUrl\":\"https://cdn.example.com/new-avatar.jpg\"}"
    /// }
    /// ```
    /// </remarks>
    /// <param name="id">ID único del usuario (GUID)</param>
    /// <param name="request">Campos a actualizar (solo los especificados)</param>
    /// <returns>Usuario actualizado</returns>
    /// <response code="200">Usuario actualizado exitosamente.</response>
    /// <response code="400">Datos de entrada inválidos.</response>
    /// <response code="401">Token JWT inválido o expirado.</response>
    /// <response code="403">No tiene permisos para actualizar este usuario.</response>
    /// <response code="404">Usuario no encontrado.</response>
    /// <response code="500">Error interno del servidor.</response>
    [HttpPatch("{id}")]
    [SwaggerOperation(
        OperationId = "PatchUser",
        Summary = "Actualizar usuario parcialmente",
        Description = "Actualiza solo los campos especificados del usuario (PATCH). Usuarios solo pueden actualizar su propio perfil.",
        Tags = new[] { "USERS" }
    )]
    [ProducesResponseType(typeof(UserOperationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public IActionResult PatchUser([FromRoute] string id, [FromBody] PatchUserRequest request)
    {
        throw new NotImplementedException("Este endpoint es manejado por el reverse proxy YARP");
    }

    /// <summary>
    /// Eliminar un usuario del sistema
    /// </summary>
    /// <remarks>
    /// Elimina (o desactiva) un usuario del sistema. **Requiere rol de Administrador.**
    /// 
    /// **Permisos requeridos:**
    /// - Rol: `Admin`
    /// 
    /// **Comportamiento:**
    /// - **Soft delete**: Usuario se marca como inactivo (`isActive = false`)
    /// - Datos se conservan para auditoría
    /// - Usuario no puede iniciar sesión
    /// - Tokens activos se invalidan
    /// - Se puede reactivar posteriormente
    /// 
    /// **Alternativa (hard delete):**
    /// - Usar query parameter `?permanent=true` para eliminación permanente
    /// - Elimina completamente el registro (no recomendado)
    /// - Solo admin con permisos especiales
    /// 
    /// **Headers requeridos:**
    /// - `X-Gateway-Secret`: Secreto del gateway
    /// - `Authorization: Bearer {token}`: Token JWT con rol Admin
    /// 
    /// **🔹 CONSUMO A TRAVÉS DEL GATEWAY /api/v1/translate:**
    /// ```json
    /// {
    ///   "service": "users",
    ///   "method": "DELETE",
    ///   "path": "/api/users/123e4567-e89b-12d3-a456-426614174000",
    ///   "headers": {
    ///     "Authorization": "Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
    ///   }
    /// }
    /// ```
    /// 
    /// **Ejemplo de respuesta exitosa:**
    /// ```json
    /// {
    ///   "success": true,
    ///   "message": "Usuario desactivado exitosamente",
    ///   "user": {
    ///     "userId": "123e4567-e89b-12d3-a456-426614174000",
    ///     "email": "usuario@example.com",
    ///     "isActive": false
    ///   }
    /// }
    /// ```
    /// 
    /// **Restricciones:**
    /// - No se puede eliminar a sí mismo (admin)
    /// - No se puede eliminar último admin del sistema
    /// </remarks>
    /// <param name="id">ID del usuario a eliminar</param>
    /// <returns>Confirmación de eliminación</returns>
    /// <response code="200">Usuario eliminado/desactivado exitosamente.</response>
    /// <response code="401">Token JWT inválido o expirado.</response>
    /// <response code="403">No tiene permisos de administrador para eliminar usuarios.</response>
    /// <response code="404">Usuario no encontrado.</response>
    /// <response code="409">No se puede eliminar (ej: último admin, auto-eliminación).</response>
    /// <response code="500">Error interno del servidor.</response>
    [HttpDelete("{id}")]
    [SwaggerOperation(
        OperationId = "DeleteUser",
        Summary = "Eliminar usuario",
        Description = "Desactiva un usuario del sistema (soft delete). Solo disponible para administradores.",
        Tags = new[] { "USERS" }
    )]
    [ProducesResponseType(typeof(UserOperationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public IActionResult DeleteUser([FromRoute] string id)
    {
        throw new NotImplementedException("Este endpoint es manejado por el reverse proxy YARP");
    }

    /// <summary>
    /// Eliminar usuario por email
    /// </summary>
    /// <remarks>
    /// Elimina (desactiva) un usuario utilizando su dirección de email.
    /// 
    /// **Comportamiento:**
    /// - Soft delete: Usuario se marca como inactivo
    /// - No elimina el registro de la BD
    /// - Tokens JWT activos se invalidan
    /// 
    /// **Restricciones:**
    /// - Solo administradores
    /// - No puede eliminar su propio email
    /// - No puede eliminar el último admin
    /// 
    /// **Headers requeridos:**
    /// - `X-Gateway-Secret`: Secreto del gateway
    /// - `Authorization: Bearer {token}`: Token JWT con rol Admin
    /// 
    /// **Ejemplo de uso:**
    /// ```
    /// DELETE /api/users/by-email/usuario@example.com
    /// ```
    /// 
    /// **🔹 CONSUMO A TRAVÉS DEL GATEWAY /api/v1/translate:**
    /// ```json
    /// {
    ///   "service": "users",
    ///   "method": "DELETE",
    ///   "path": "/api/users/by-email/usuario@example.com",
    ///   "headers": {
    ///     "Authorization": "Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
    ///   }
    /// }
    /// ```
    /// </remarks>
    /// <param name="email">Email del usuario a eliminar</param>
    /// <returns>Confirmación de eliminación</returns>
    /// <response code="200">Usuario eliminado exitosamente.</response>
    /// <response code="401">Token JWT inválido o expirado.</response>
    /// <response code="403">No tiene permisos de administrador.</response>
    /// <response code="404">Usuario no encontrado con ese email.</response>
    /// <response code="409">No se puede eliminar (ej: último admin, auto-eliminación).</response>
    /// <response code="500">Error interno del servidor.</response>
    [HttpDelete("by-email/{email}")]
    [SwaggerOperation(
        OperationId = "DeleteUserByEmail",
        Summary = "Eliminar usuario por email",
        Description = "Elimina usuario utilizando su email. Solo disponible para administradores.",
        Tags = new[] { "USERS" }
    )]
    [ProducesResponseType(typeof(UserOperationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public IActionResult DeleteUserByEmail([FromRoute] string email)
    {
        throw new NotImplementedException("Este endpoint es manejado por el reverse proxy YARP");
    }

    /// <summary>
    /// Eliminar TODOS los datos del sistema (PELIGROSO)
    /// </summary>
    /// <remarks>
    /// ⚠️ **ADVERTENCIA: OPERACIÓN DESTRUCTIVA E IRREVERSIBLE** ⚠️
    /// 
    /// Elimina TODOS los registros de las tablas:
    /// - **USERS**: Todos los usuarios
    /// - **PREFERENCES**: Todas las preferencias
    /// - **SESSIONS**: Todas las sesiones activas
    /// 
    /// **Casos de uso:**
    /// - Limpieza completa del sistema
    /// - Reset de base de datos en desarrollo/testing
    /// - Preparación para migración
    /// 
    /// **Restricciones:**
    /// - Solo super-administradores
    /// - Requiere confirmación adicional
    /// - Se recomienda backup previo
    /// 
    /// **Headers requeridos:**
    /// - `X-Gateway-Secret`: Secreto del gateway
    /// - `Authorization: Bearer {token}`: Token JWT con rol SuperAdmin
    /// - `X-Confirm-Delete`: "YES_DELETE_ALL_DATA" (confirmación obligatoria)
    /// 
    /// **🔹 CONSUMO A TRAVÉS DEL GATEWAY /api/v1/translate:**
    /// ```json
    /// {
    ///   "service": "users",
    ///   "method": "DELETE",
    ///   "path": "/api/users/all-data",
    ///   "headers": {
    ///     "Authorization": "Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    ///     "X-Confirm-Delete": "YES_DELETE_ALL_DATA"
    ///   }
    /// }
    /// ```
    /// </remarks>
    /// <returns>Confirmación de eliminación masiva</returns>
    /// <response code="200">Todos los datos eliminados exitosamente.</response>
    /// <response code="401">Token JWT inválido o expirado.</response>
    /// <response code="403">No tiene permisos de super-administrador.</response>
    /// <response code="428">Falta header de confirmación X-Confirm-Delete.</response>
    /// <response code="500">Error interno del servidor.</response>
    [HttpDelete("all-data")]
    [SwaggerOperation(
        OperationId = "DeleteAllData",
        Summary = "Eliminar TODOS los datos (PELIGROSO)",
        Description = "⚠️ ELIMINA TODOS los usuarios, preferencias y sesiones. IRREVERSIBLE. Solo super-admin.",
        Tags = new[] { "USERS" }
    )]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status428PreconditionRequired)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public IActionResult DeleteAllData()
    {
        throw new NotImplementedException("Este endpoint es manejado por el reverse proxy YARP");
    }

    // ============================================================================
    // SECCIÓN: PREFERENCIAS DE USUARIO (5 endpoints)
    // ============================================================================

    /// <summary>
    /// Obtener preferencias del usuario autenticado
    /// </summary>
    /// <remarks>
    /// Retorna todas las preferencias personalizadas del usuario actual.
    /// 
    /// **Preferencias incluidas:**
    /// - **Tema**: light, dark, auto (según sistema operativo)
    /// - **Idioma**: es, en, fr (ISO 639-1)
    /// - **Timezone**: America/Mexico_City, Europe/Madrid, etc. (IANA)
    /// - **Notificaciones**: Email, Push, Resúmenes diarios/semanales
    /// - **Accesibilidad**: Lector de pantalla, tamaño de fuente, alto contraste
    /// - **Análisis**: Nivel WCAG, profundidad de escaneo, timeouts
    /// - **Reportes**: Formato predeterminado, gráficos, nivel de detalle
    /// 
    /// **Headers requeridos:**
    /// - `X-Gateway-Secret`: Secreto del gateway
    /// - `Authorization: Bearer {token}`: Token JWT válido
    /// 
    /// **Ejemplo de respuesta exitosa:**
    /// ```json
    /// {
    ///   "userId": "123e4567-e89b-12d3-a456-426614174000",
    ///   "theme": "dark",
    ///   "language": "es",
    ///   "timezone": "America/Mexico_City",
    ///   "notifications": {
    ///     "emailEnabled": true,
    ///     "pushEnabled": true,
    ///     "notifyOnAnalysisComplete": true,
    ///     "dailySummary": false
    ///   },
    ///   "accessibility": {
    ///     "screenReaderMode": false,
    ///     "fontSize": "large",
    ///     "highContrast": true,
    ///     "reduceMotion": false
    ///   },
    ///   "analysis": {
    ///     "defaultWcagLevel": "AA",
    ///     "includeWarnings": true,
    ///     "maxScanDepth": 5
    ///   },
    ///   "reports": {
    ///     "defaultFormat": "pdf",
    ///     "includeCharts": true,
    ///     "detailLevel": "standard"
    ///   }
    /// }
    /// ```
    /// 
    /// **Valores predeterminados:**
    /// Si el usuario no ha configurado preferencias, se retornan valores por defecto del sistema.
    /// 
    /// **🔹 CONSUMO A TRAVÉS DEL GATEWAY /api/v1/translate:**
    /// ```json
    /// {
    ///   "service": "users",
    ///   "method": "GET",
    ///   "path": "/api/users/preferences",
    ///   "headers": {
    ///     "Authorization": "Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
    ///   }
    /// }
    /// ```
    /// </remarks>
    /// <returns>Preferencias completas del usuario</returns>
    /// <response code="200">Preferencias recuperadas exitosamente.</response>
    /// <response code="401">Token JWT inválido o expirado.</response>
    /// <response code="403">Gateway Secret inválido.</response>
    /// <response code="500">Error interno del servidor.</response>
    [HttpGet("preferences")]
    [SwaggerOperation(
        OperationId = "GetUserPreferences",
        Summary = "Obtener preferencias",
        Description = "Retorna todas las preferencias personalizadas del usuario autenticado (tema, idioma, notificaciones, accesibilidad, etc.)",
        Tags = new[] { "PREFERENCES" }
    )]
    [ProducesResponseType(typeof(UserPreferencesDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public IActionResult GetPreferences()
    {
        throw new NotImplementedException("Este endpoint es manejado por el reverse proxy YARP");
    }

    /// <summary>
    /// Actualizar preferencias del usuario autenticado
    /// </summary>
    /// <remarks>
    /// Actualiza una o más preferencias del usuario. Solo se actualizan los campos proporcionados.
    /// 
    /// **Actualización parcial:**
    /// - Puedes enviar solo los campos que deseas cambiar
    /// - Los demás campos mantienen su valor actual
    /// - No es necesario enviar todas las preferencias
    /// 
    /// **Validaciones:**
    /// - **Theme**: Solo valores: 'light', 'dark', 'auto'
    /// - **Language**: Código ISO 639-1 (2 letras): 'es', 'en', 'fr'
    /// - **Timezone**: Timezone válido IANA
    /// - **WcagLevel**: Solo valores: 'A', 'AA', 'AAA'
    /// - **FontSize**: Solo valores: 'normal', 'large', 'x-large'
    /// - **ReportFormat**: Solo valores: 'pdf', 'html', 'json', 'csv'
    /// 
    /// **Headers requeridos:**
    /// - `X-Gateway-Secret`: Secreto del gateway
    /// - `Authorization: Bearer {token}`: Token JWT válido
    /// 
    /// **Ejemplo de request (actualización parcial):**
    /// ```json
    /// {
    ///   "theme": "dark",
    ///   "language": "es",
    ///   "notifications": {
    ///     "emailEnabled": true,
    ///     "dailySummary": true
    ///   }
    /// }
    /// ```
    /// 
    /// **Ejemplo de request (actualización completa):**
    /// ```json
    /// {
    ///   "theme": "dark",
    ///   "language": "es",
    ///   "timezone": "America/Mexico_City",
    ///   "notifications": {
    ///     "emailEnabled": true,
    ///     "pushEnabled": true,
    ///     "notifyOnAnalysisComplete": true,
    ///     "dailySummary": true
    ///   },
    ///   "accessibility": {
    ///     "screenReaderMode": false,
    ///     "fontSize": "large",
    ///     "highContrast": true
    ///   },
    ///   "analysis": {
    ///     "defaultWcagLevel": "AA",
    ///     "includeWarnings": true
    ///   },
    ///   "reports": {
    ///     "defaultFormat": "pdf",
    ///     "includeCharts": true
    ///   }
    /// }
    /// ```
    /// 
    /// **🔹 CONSUMO A TRAVÉS DEL GATEWAY /api/v1/translate:**
    /// ```json
    /// {
    ///   "service": "users",
    ///   "method": "PUT",
    ///   "path": "/api/users/preferences",
    ///   "headers": {
    ///     "Content-Type": "application/json",
    ///     "Authorization": "Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
    ///   },
    ///   "body": "{\"theme\":\"dark\",\"language\":\"es\",\"notifications\":{\"emailEnabled\":true,\"dailySummary\":true}}"
    /// }
    /// ```
    /// 
    /// **Ejemplo de respuesta exitosa:**
    /// ```json
    /// {
    ///   "success": true,
    ///   "message": "Preferencias actualizadas exitosamente",
    ///   "preferences": {
    ///     "userId": "123e4567-e89b-12d3-a456-426614174000",
    ///     "theme": "dark",
    ///     "language": "es",
    ///     "updatedAt": "2025-10-25T12:00:00Z"
    ///   }
    /// }
    /// ```
    /// </remarks>
    /// <param name="request">Preferencias a actualizar (campos opcionales)</param>
    /// <returns>Confirmación con preferencias actualizadas completas</returns>
    /// <response code="200">Preferencias actualizadas exitosamente.</response>
    /// <response code="400">Valores de preferencias inválidos.</response>
    /// <response code="401">Token JWT inválido o expirado.</response>
    /// <response code="403">Gateway Secret inválido.</response>
    /// <response code="500">Error interno del servidor.</response>
    [HttpPut("preferences")]
    [SwaggerOperation(
        OperationId = "UpdateUserPreferences",
        Summary = "Actualizar preferencias",
        Description = "Actualiza preferencias del usuario (tema, idioma, notificaciones, etc.). Solo se actualizan los campos proporcionados.",
        Tags = new[] { "PREFERENCES" }
    )]
    [ProducesResponseType(typeof(PreferencesOperationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public IActionResult UpdatePreferences([FromBody] UpdatePreferencesRequest request)
    {
        throw new NotImplementedException("Este endpoint es manejado por el reverse proxy YARP");
    }

    /// <summary>
    /// Obtener preferencias de usuario por email
    /// </summary>
    /// <remarks>
    /// Recupera las preferencias de un usuario específico mediante su email.
    /// 
    /// **Restricciones:**
    /// - Usuarios: Solo sus propias preferencias
    /// - Administradores: Preferencias de cualquier usuario
    /// 
    /// **Headers requeridos:**
    /// - `X-Gateway-Secret`: Secreto del gateway
    /// - `Authorization: Bearer {token}`: Token JWT válido
    /// 
    /// **Retorna:**
    /// - Preferencias completas (WCAG, idioma, tema, formato reportes, etc.)
    /// - Información básica del usuario asociado
    /// - Timestamps de creación y última actualización
    /// 
    /// **Ejemplo de respuesta:**
    /// ```json
    /// {
    ///   "preferences": {
    ///     "id": 42,
    ///     "userId": 15,
    ///     "wcagVersion": "2.1",
    ///     "wcagLevel": "AA",
    ///     "language": "es",
    ///     "visualTheme": "dark",
    ///     "reportFormat": "pdf",
    ///     "notificationsEnabled": true,
    ///     "aiResponseLevel": "intermediate",
    ///     "fontSize": 16
    ///   }
    /// }
    /// ```
    /// 
    /// **🔹 CONSUMO A TRAVÉS DEL GATEWAY /api/v1/translate:**
    /// ```json
    /// {
    ///   "service": "users",
    ///   "method": "GET",
    ///   "path": "/api/users/preferences/by-user/usuario@example.com",
    ///   "headers": {
    ///     "Authorization": "Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
    ///   }
    /// }
    /// ```
    /// </remarks>
    /// <param name="email">Email del usuario</param>
    /// <returns>Preferencias del usuario</returns>
    /// <response code="200">Preferencias encontradas exitosamente.</response>
    /// <response code="401">Token JWT inválido o expirado.</response>
    /// <response code="403">No tiene permisos para ver estas preferencias.</response>
    /// <response code="404">Preferencias no encontradas para este usuario.</response>
    /// <response code="500">Error interno del servidor.</response>
    [HttpGet("preferences/by-user/{email}")]
    [SwaggerOperation(
        OperationId = "GetPreferencesByEmail",
        Summary = "Obtener preferencias por email",
        Description = "Recupera preferencias de un usuario por su email. Usuarios solo pueden ver sus propias preferencias.",
        Tags = new[] { "PREFERENCES" }
    )]
    [ProducesResponseType(typeof(PreferenceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public IActionResult GetPreferencesByEmail([FromRoute] string email)
    {
        throw new NotImplementedException("Este endpoint es manejado por el reverse proxy YARP");
    }

    /// <summary>
    /// Crear preferencias para un usuario
    /// </summary>
    /// <remarks>
    /// Crea las preferencias iniciales para un usuario que aún no las tiene.
    /// 
    /// **Uso típico:**
    /// - Primer uso después del registro
    /// - Restaurar preferencias eliminadas
    /// - Migración de datos
    /// 
    /// **Restricciones:**
    /// - Usuarios: Solo sus propias preferencias
    /// - Administradores: Preferencias de cualquier usuario
    /// - Un usuario solo puede tener un conjunto de preferencias
    /// 
    /// **Valores por defecto:**
    /// - wcagVersion: "2.1"
    /// - wcagLevel: "AA"
    /// - language: "es"
    /// - visualTheme: "light"
    /// - reportFormat: "pdf"
    /// - notificationsEnabled: true
    /// - aiResponseLevel: "intermediate"
    /// - fontSize: 14
    /// 
    /// **Ejemplo de request:**
    /// ```json
    /// {
    ///   "userId": 15,
    ///   "wcagVersion": "2.2",
    ///   "wcagLevel": "AAA",
    ///   "language": "en",
    ///   "visualTheme": "dark",
    ///   "reportFormat": "html",
    ///   "notificationsEnabled": false,
    ///   "aiResponseLevel": "detailed",
    ///   "fontSize": 18
    /// }
    /// ```
    /// 
    /// **🔹 CONSUMO A TRAVÉS DEL GATEWAY /api/v1/translate:**
    /// ```json
    /// {
    ///   "service": "users",
    ///   "method": "POST",
    ///   "path": "/api/users/preferences",
    ///   "headers": {
    ///     "Content-Type": "application/json",
    ///     "Authorization": "Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
    ///   },
    ///   "body": "{\"userId\":15,\"wcagVersion\":\"2.2\",\"wcagLevel\":\"AAA\",\"language\":\"en\",\"visualTheme\":\"dark\",\"reportFormat\":\"html\",\"notificationsEnabled\":false,\"aiResponseLevel\":\"detailed\",\"fontSize\":18}"
    /// }
    /// ```
    /// </remarks>
    /// <param name="request">Datos de preferencias a crear</param>
    /// <returns>Preferencias creadas</returns>
    /// <response code="201">Preferencias creadas exitosamente.</response>
    /// <response code="400">Valores de preferencias inválidos.</response>
    /// <response code="401">Token JWT inválido o expirado.</response>
    /// <response code="403">No tiene permisos para crear preferencias.</response>
    /// <response code="409">El usuario ya tiene preferencias.</response>
    /// <response code="500">Error interno del servidor.</response>
    [HttpPost("preferences")]
    [SwaggerOperation(
        OperationId = "CreatePreferences",
        Summary = "Crear preferencias",
        Description = "Crea preferencias iniciales para un usuario. No puede crear duplicados.",
        Tags = new[] { "PREFERENCES" }
    )]
    [ProducesResponseType(typeof(PreferenceResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public IActionResult CreatePreferences([FromBody] CreatePreferenceRequest request)
    {
        throw new NotImplementedException("Este endpoint es manejado por el reverse proxy YARP");
    }

    /// <summary>
    /// Eliminar preferencias de usuario
    /// </summary>
    /// <remarks>
    /// Elimina las preferencias de un usuario por su ID.
    /// 
    /// **⚠️ ADVERTENCIA:**
    /// - Esta operación es IRREVERSIBLE
    /// - El usuario volverá a valores por defecto del sistema
    /// - Puede afectar la experiencia de usuario
    /// 
    /// **Uso típico:**
    /// - Usuario solicita "resetear preferencias"
    /// - Limpieza de datos antes de eliminación de cuenta
    /// - Resolución de problemas de configuración corrupta
    /// 
    /// **Restricciones:**
    /// - Usuarios: Solo sus propias preferencias
    /// - Administradores: Preferencias de cualquier usuario
    /// 
    /// **Headers requeridos:**
    /// - `X-Gateway-Secret`: Secreto del gateway
    /// - `Authorization: Bearer {token}`: Token JWT válido
    /// 
    /// **Efecto:**
    /// - Las preferencias se eliminan de la base de datos
    /// - El usuario puede crear nuevas preferencias después
    /// - No afecta sesiones activas
    /// 
    /// **🔹 CONSUMO A TRAVÉS DEL GATEWAY /api/v1/translate:**
    /// ```json
    /// {
    ///   "service": "users",
    ///   "method": "DELETE",
    ///   "path": "/api/users/preferences/42",
    ///   "headers": {
    ///     "Authorization": "Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
    ///   }
    /// }
    /// ```
    /// </remarks>
    /// <param name="id">ID de preferencias a eliminar</param>
    /// <returns>Confirmación de eliminación</returns>
    /// <response code="200">Preferencias eliminadas exitosamente.</response>
    /// <response code="401">Token JWT inválido o expirado.</response>
    /// <response code="403">No tiene permisos para eliminar estas preferencias.</response>
    /// <response code="404">Preferencias no encontradas.</response>
    /// <response code="500">Error interno del servidor.</response>
    [HttpDelete("preferences/{id}")]
    [SwaggerOperation(
        OperationId = "DeletePreferences",
        Summary = "Eliminar preferencias",
        Description = "Elimina preferencias de un usuario por ID. Operación irreversible.",
        Tags = new[] { "PREFERENCES" }
    )]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public IActionResult DeletePreferences([FromRoute] int id)
    {
        throw new NotImplementedException("Este endpoint es manejado por el reverse proxy YARP");
    }

    // ============================================================================
    // SECCIÓN: GESTIÓN DE SESIONES (5 endpoints)
    // ============================================================================

    /// <summary>
    /// Listar todas las sesiones del sistema
    /// </summary>
    /// <remarks>
    /// Recupera todas las sesiones activas e inactivas de todos los usuarios.
    /// 
    /// **Restricciones:**
    /// - Solo administradores
    /// - Incluye sesiones de todos los usuarios
    /// 
    /// **Headers requeridos:**
    /// - `X-Gateway-Secret`: Secreto del gateway
    /// - `Authorization: Bearer {token}`: Token JWT con rol Admin
    /// 
    /// **Información retornada por sesión:**
    /// - ID de sesión
    /// - ID de usuario
    /// - Token JWT (parcial por seguridad)
    /// - Fecha de creación
    /// - Fecha de expiración
    /// - Estado (activa/expirada)
    /// - Información de dispositivo
    /// - IP de origen
    /// 
    /// **🔹 CONSUMO A TRAVÉS DEL GATEWAY /api/v1/translate:**
    /// ```json
    /// {
    ///   "service": "users",
    ///   "method": "GET",
    ///   "path": "/api/users/sessions",
    ///   "headers": {
    ///     "Authorization": "Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
    ///   }
    /// }
    /// ```
    /// </remarks>
    /// <returns>Lista completa de sesiones</returns>
    /// <response code="200">Sesiones recuperadas exitosamente.</response>
    /// <response code="401">Token JWT inválido o expirado.</response>
    /// <response code="403">No tiene permisos de administrador.</response>
    /// <response code="500">Error interno del servidor.</response>
    [HttpGet("sessions")]
    [SwaggerOperation(
        OperationId = "GetAllSessions",
        Summary = "Listar todas las sesiones",
        Description = "Retorna lista completa de sesiones de todos los usuarios. Requiere rol de administrador.",
        Tags = new[] { "SESSIONS" }
    )]
    [ProducesResponseType(typeof(SessionsListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public IActionResult GetAllSessions()
    {
        throw new NotImplementedException("Este endpoint es manejado por el reverse proxy YARP");
    }

    /// <summary>
    /// Obtener sesiones de un usuario específico
    /// </summary>
    /// <remarks>
    /// Recupera todas las sesiones (activas e inactivas) de un usuario específico.
    /// 
    /// **Restricciones:**
    /// - Usuarios normales: Solo sus propias sesiones
    /// - Administradores: Sesiones de cualquier usuario
    /// 
    /// **Headers requeridos:**
    /// - `X-Gateway-Secret`: Secreto del gateway
    /// - `Authorization: Bearer {token}`: Token JWT válido
    /// 
    /// **Información retornada:**
    /// - Lista de todas las sesiones del usuario
    /// - Incluye sesiones activas y expiradas
    /// - Información de dispositivo y ubicación
    /// - Timestamps de creación y expiración
    /// 
    /// **🔹 CONSUMO A TRAVÉS DEL GATEWAY /api/v1/translate:**
    /// ```json
    /// {
    ///   "service": "users",
    ///   "method": "GET",
    ///   "path": "/api/users/sessions/user/123e4567-e89b-12d3-a456-426614174000",
    ///   "headers": {
    ///     "Authorization": "Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
    ///   }
    /// }
    /// ```
    /// </remarks>
    /// <param name="userId">ID del usuario</param>
    /// <returns>Lista de sesiones del usuario</returns>
    /// <response code="200">Sesiones recuperadas exitosamente.</response>
    /// <response code="401">Token JWT inválido o expirado.</response>
    /// <response code="403">No tiene permisos para ver sesiones de este usuario.</response>
    /// <response code="404">Usuario no encontrado.</response>
    /// <response code="500">Error interno del servidor.</response>
    [HttpGet("sessions/user/{userId}")]
    [SwaggerOperation(
        OperationId = "GetSessionsByUserId",
        Summary = "Obtener sesiones por usuario",
        Description = "Retorna todas las sesiones de un usuario específico. Usuarios solo pueden ver sus propias sesiones.",
        Tags = new[] { "SESSIONS" }
    )]
    [ProducesResponseType(typeof(SessionsListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public IActionResult GetSessionsByUserId([FromRoute] string userId)
    {
        throw new NotImplementedException("Este endpoint es manejado por el reverse proxy YARP");
    }

    /// <summary>
    /// Listar todas las sesiones activas del usuario
    /// </summary>
    /// <remarks>
    /// Retorna información de todas las sesiones activas (tokens JWT válidos) del usuario autenticado.
    /// 
    /// **Información de cada sesión:**
    /// - **SessionId**: Identificador único de la sesión
    /// - **TokenPreview**: Últimos 8 caracteres del token (para identificación)
    /// - **CreatedAt**: Fecha de creación (login)
    /// - **ExpiresAt**: Fecha de expiración del token
    /// - **LastActivityAt**: Última actividad con ese token
    /// - **IpAddress**: IP desde donde se inició sesión (parcialmente enmascarada)
    /// - **UserAgent**: Información del navegador/dispositivo
    /// - **DeviceType**: desktop, mobile, tablet, unknown
    /// - **Location**: Ubicación estimada (ciudad, país)
    /// - **IsCurrentSession**: true si es el token usado en este request
    /// 
    /// **Casos de uso:**
    /// - Ver dispositivos con sesión activa
    /// - Identificar sesiones sospechosas
    /// - Cerrar sesiones específicas remotamente
    /// - Auditoría de accesos
    /// 
    /// **Headers requeridos:**
    /// - `X-Gateway-Secret`: Secreto del gateway
    /// - `Authorization: Bearer {token}`: Token JWT válido
    /// 
    /// **Ejemplo de respuesta exitosa:**
    /// ```json
    /// {
    ///   "sessions": [
    ///     {
    ///       "sessionId": "sess-abc123",
    ///       "tokenPreview": "...xyz789",
    ///       "createdAt": "2025-10-25T09:00:00Z",
    ///       "expiresAt": "2025-10-26T09:00:00Z",
    ///       "lastActivityAt": "2025-10-25T12:30:00Z",
    ///       "ipAddress": "192.168.1.xxx",
    ///       "userAgent": "Mozilla/5.0 (Windows NT 10.0; Win64; x64)...",
    ///       "deviceType": "desktop",
    ///       "location": "Ciudad de México, México",
    ///       "isCurrentSession": true,
    ///       "isActive": true
    ///     },
    ///     {
    ///       "sessionId": "sess-def456",
    ///       "tokenPreview": "...uvw456",
    ///       "createdAt": "2025-10-24T15:00:00Z",
    ///       "expiresAt": "2025-10-25T15:00:00Z",
    ///       "lastActivityAt": "2025-10-24T18:00:00Z",
    ///       "ipAddress": "10.0.0.xxx",
    ///       "userAgent": "Mozilla/5.0 (iPhone; CPU iPhone OS 15_0)...",
    ///       "deviceType": "mobile",
    ///       "location": "Guadalajara, México",
    ///       "isCurrentSession": false,
    ///       "isActive": true
    ///     }
    ///   ],
    ///   "totalSessions": 2,
    ///   "currentSessionId": "sess-abc123",
    ///   "retrievedAt": "2025-10-25T12:35:00Z"
    /// }
    /// ```
    /// 
    /// **Acciones posteriores:**
    /// - Usar `/api/users/sessions/revoke` para cerrar sesión específica
    /// - Usar `/api/users/auth/logout` con `logoutAllDevices: true` para cerrar todas
    /// 
    /// **🔹 CONSUMO A TRAVÉS DEL GATEWAY /api/v1/translate:**
    /// ```json
    /// {
    ///   "service": "users",
    ///   "method": "GET",
    ///   "path": "/api/users/sessions/active",
    ///   "headers": {
    ///     "Authorization": "Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
    ///   }
    /// }
    /// ```
    /// </remarks>
    /// <returns>Lista de sesiones activas del usuario</returns>
    /// <response code="200">Sesiones recuperadas exitosamente.</response>
    /// <response code="401">Token JWT inválido o expirado.</response>
    /// <response code="403">Gateway Secret inválido.</response>
    /// <response code="500">Error interno del servidor.</response>
    [HttpGet("sessions/active")]
    [SwaggerOperation(
        OperationId = "GetActiveSessions",
        Summary = "Listar sesiones activas",
        Description = "Retorna lista de todas las sesiones activas (tokens JWT válidos) del usuario autenticado, incluyendo información de dispositivo y ubicación",
        Tags = new[] { "SESSIONS" }
    )]
    [ProducesResponseType(typeof(ActiveSessionsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public IActionResult GetActiveSessions()
    {
        throw new NotImplementedException("Este endpoint es manejado por el reverse proxy YARP");
    }

    /// <summary>
    /// Eliminar una sesión específica
    /// </summary>
    /// <remarks>
    /// Elimina (cierra) una sesión específica por su ID, invalidando el token JWT asociado.
    /// 
    /// **Uso típico:**
    /// - Cerrar sesión en un dispositivo específico
    /// - Revocar acceso de una sesión comprometida
    /// - Forzar re-autenticación
    /// 
    /// **Restricciones:**
    /// - Usuarios: Solo sus propias sesiones
    /// - Administradores: Cualquier sesión
    /// 
    /// **Headers requeridos:**
    /// - `X-Gateway-Secret`: Secreto del gateway
    /// - `Authorization: Bearer {token}`: Token JWT válido
    /// 
    /// **Efecto:**
    /// - Token JWT se invalida inmediatamente
    /// - Usuario debe re-autenticarse en ese dispositivo
    /// - Otras sesiones no se ven afectadas
    /// 
    /// **🔹 CONSUMO A TRAVÉS DEL GATEWAY /api/v1/translate:**
    /// ```json
    /// {
    ///   "service": "users",
    ///   "method": "DELETE",
    ///   "path": "/api/users/sessions/sess-abc123",
    ///   "headers": {
    ///     "Authorization": "Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
    ///   }
    /// }
    /// ```
    /// </remarks>
    /// <param name="sessionId">ID de la sesión a eliminar</param>
    /// <returns>Confirmación de eliminación</returns>
    /// <response code="200">Sesión eliminada exitosamente.</response>
    /// <response code="401">Token JWT inválido o expirado.</response>
    /// <response code="403">No tiene permisos para eliminar esta sesión.</response>
    /// <response code="404">Sesión no encontrada.</response>
    /// <response code="500">Error interno del servidor.</response>
    [HttpDelete("sessions/{sessionId}")]
    [SwaggerOperation(
        OperationId = "DeleteSession",
        Summary = "Eliminar sesión",
        Description = "Elimina una sesión específica e invalida su token JWT. Usuarios solo pueden eliminar sus propias sesiones.",
        Tags = new[] { "SESSIONS" }
    )]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public IActionResult DeleteSession([FromRoute] string sessionId)
    {
        throw new NotImplementedException("Este endpoint es manejado por el reverse proxy YARP");
    }

    /// <summary>
    /// Eliminar todas las sesiones de un usuario
    /// </summary>
    /// <remarks>
    /// Elimina TODAS las sesiones de un usuario específico, cerrando su sesión en todos los dispositivos.
    /// 
    /// **Uso típico:**
    /// - Usuario cambió contraseña (cerrar sesión en todos los dispositivos)
    /// - Cuenta comprometida (revocar todo acceso)
    /// - Usuario solicita "cerrar sesión en todos lados"
    /// 
    /// **Restricciones:**
    /// - Usuarios: Solo sus propias sesiones
    /// - Administradores: Sesiones de cualquier usuario
    /// 
    /// **Headers requeridos:**
    /// - `X-Gateway-Secret`: Secreto del gateway
    /// - `Authorization: Bearer {token}`: Token JWT válido
    /// 
    /// **Efecto:**
    /// - TODOS los tokens JWT del usuario se invalidan
    /// - Usuario debe re-autenticarse en TODOS los dispositivos
    /// - Incluye la sesión actual
    /// 
    /// **Ejemplo de respuesta:**
    /// ```json
    /// {
    ///   "success": true,
    ///   "message": "5 sesiones eliminadas exitosamente",
    ///   "sessionsDeleted": 5
    /// }
    /// ```
    /// 
    /// **🔹 CONSUMO A TRAVÉS DEL GATEWAY /api/v1/translate:**
    /// ```json
    /// {
    ///   "service": "users",
    ///   "method": "DELETE",
    ///   "path": "/api/users/sessions/by-user/123e4567-e89b-12d3-a456-426614174000",
    ///   "headers": {
    ///     "Authorization": "Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
    ///   }
    /// }
    /// ```
    /// </remarks>
    /// <param name="userId">ID del usuario</param>
    /// <returns>Confirmación con cantidad de sesiones eliminadas</returns>
    /// <response code="200">Sesiones eliminadas exitosamente.</response>
    /// <response code="401">Token JWT inválido o expirado.</response>
    /// <response code="403">No tiene permisos para eliminar sesiones de este usuario.</response>
    /// <response code="404">Usuario no encontrado o sin sesiones activas.</response>
    /// <response code="500">Error interno del servidor.</response>
    [HttpDelete("sessions/by-user/{userId}")]
    [SwaggerOperation(
        OperationId = "DeleteSessionsByUserId",
        Summary = "Eliminar todas las sesiones de un usuario",
        Description = "Elimina TODAS las sesiones de un usuario, cerrando sesión en todos los dispositivos. Usuarios solo pueden eliminar sus propias sesiones.",
        Tags = new[] { "SESSIONS" }
    )]
    [ProducesResponseType(typeof(SessionsDeletionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public IActionResult DeleteSessionsByUserId([FromRoute] string userId)
    {
        throw new NotImplementedException("Este endpoint es manejado por el reverse proxy YARP");
    }

    // ============================================================================
    // SECCIÓN: OBSERVABILIDAD Y MONITOREO (4 endpoints)
    // ============================================================================

    /// <summary>
    /// Health check completo del microservicio Users
    /// </summary>
    /// <remarks>
    /// Verifica el estado de salud completo del microservicio, incluyendo:
    /// - Conectividad con base de datos PostgreSQL
    /// - Estado del servidor de aplicación
    /// - Checks adicionales configurados
    /// 
    /// **Estados posibles:**
    /// - `Healthy`: Todos los componentes funcionando correctamente
    /// - `Degraded`: Algunos componentes con problemas pero el servicio sigue operativo
    /// - `Unhealthy`: Componentes críticos fallando, servicio no operativo
    /// 
    /// **Uso:**
    /// - Monitoreo de salud del microservicio
    /// - Orchestrator health checks (Kubernetes, Docker Swarm)
    /// - Dashboards de observabilidad
    /// 
    /// **Respuesta JSON detallada con:**
    /// - Status general
    /// - Duración del check
    /// - Detalles por componente (DB, cache, etc.)
    /// 
    /// **🔹 CONSUMO A TRAVÉS DEL GATEWAY /api/v1/translate:**
    /// ```json
    /// {
    ///   "service": "users",
    ///   "method": "GET",
    ///   "path": "/health"
    /// }
    /// ```
    /// </remarks>
    /// <returns>Estado de salud del microservicio</returns>
    /// <response code="200">Microservicio saludable (Healthy)</response>
    /// <response code="503">Microservicio no saludable (Unhealthy o Degraded)</response>
    [HttpGet("/users-service/health")]
    [ApiExplorerSettings(GroupName = "users")]
    [SwaggerOperation(
        OperationId = "GetUsersHealth",
        Summary = "Health check completo del microservicio Users",
        Description = "Verifica el estado de salud completo incluyendo base de datos y componentes críticos.",
        Tags = new[] { "OBSERVABILITY" }
    )]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public IActionResult GetHealth()
    {
        throw new NotImplementedException("Este endpoint es manejado por el reverse proxy YARP");
    }

    /// <summary>
    /// Liveness probe - verifica que el microservicio esté ejecutándose
    /// </summary>
    /// <remarks>
    /// Endpoint ligero que verifica que el proceso del microservicio está vivo y puede responder requests.
    /// 
    /// **Uso:**
    /// - Kubernetes liveness probe
    /// - Docker health check
    /// - Load balancer health check
    /// 
    /// **Diferencia con /health:**
    /// - `/health/live`: Solo verifica que el proceso responde (muy rápido)
    /// - `/health`: Verifica también dependencias como DB (más lento)
    /// 
    /// **Política de reinicio:**
    /// Si este endpoint falla, Kubernetes/Docker pueden reiniciar el contenedor.
    /// 
    /// **🔹 CONSUMO A TRAVÉS DEL GATEWAY /api/v1/translate:**
    /// ```json
    /// {
    ///   "service": "users",
    ///   "method": "GET",
    ///   "path": "/health/live"
    /// }
    /// ```
    /// </remarks>
    /// <returns>200 OK si el microservicio está vivo</returns>
    /// <response code="200">Microservicio ejecutándose correctamente</response>
    /// <response code="503">Microservicio no responde</response>
    [HttpGet("/users-service/health/live")]
    [ApiExplorerSettings(GroupName = "users")]
    [SwaggerOperation(
        OperationId = "GetUsersLiveness",
        Summary = "Liveness probe del microservicio Users",
        Description = "Verifica que el proceso esté vivo y respondiendo. Usado por orchestrators para decidir reinicios.",
        Tags = new[] { "OBSERVABILITY" }
    )]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public IActionResult GetLiveness()
    {
        throw new NotImplementedException("Este endpoint es manejado por el reverse proxy YARP");
    }

    /// <summary>
    /// Readiness probe - verifica que el microservicio esté listo para aceptar tráfico
    /// </summary>
    /// <remarks>
    /// Endpoint que verifica que el microservicio está completamente inicializado y listo para procesar requests.
    /// 
    /// **Verifica:**
    /// - Conexión a base de datos establecida
    /// - Migraciones aplicadas
    /// - Dependencias críticas disponibles
    /// - Warming-up completado
    /// 
    /// **Uso:**
    /// - Kubernetes readiness probe
    /// - Load balancer backend health
    /// - Service mesh routing decisions
    /// 
    /// **Diferencia con /health/live:**
    /// - `/health/live`: ¿El proceso está vivo?
    /// - `/health/ready`: ¿El proceso puede procesar requests?
    /// 
    /// **Política de tráfico:**
    /// Si este endpoint falla, el orchestrator dejará de enviar tráfico pero NO reiniciará el contenedor.
    /// 
    /// **🔹 CONSUMO A TRAVÉS DEL GATEWAY /api/v1/translate:**
    /// ```json
    /// {
    ///   "service": "users",
    ///   "method": "GET",
    ///   "path": "/health/ready"
    /// }
    /// ```
    /// </remarks>
    /// <returns>200 OK si el microservicio está listo para recibir tráfico</returns>
    /// <response code="200">Microservicio listo para aceptar requests</response>
    /// <response code="503">Microservicio aún inicializando o con problemas</response>
    [HttpGet("/users-service/health/ready")]
    [ApiExplorerSettings(GroupName = "users")]
    [SwaggerOperation(
        OperationId = "GetUsersReadiness",
        Summary = "Readiness probe del microservicio Users",
        Description = "Verifica que el microservicio esté completamente inicializado y listo para procesar requests.",
        Tags = new[] { "OBSERVABILITY" }
    )]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public IActionResult GetReadiness()
    {
        throw new NotImplementedException("Este endpoint es manejado por el reverse proxy YARP");
    }

    /// <summary>
    /// Métricas de Prometheus del microservicio Users
    /// </summary>
    /// <remarks>
    /// Expone métricas en formato Prometheus para monitoreo y observabilidad.
    /// 
    /// **Métricas incluidas:**
    /// - Métricas HTTP: requests totales, duración, errores
    /// - Métricas de aplicación: usuarios activos, logins, registros
    /// - Métricas de base de datos: queries, conexiones, errores
    /// - Métricas de .NET: GC, memoria, threads
    /// 
    /// **Formato:**
    /// ```
    /// # HELP http_requests_total Total HTTP requests
    /// # TYPE http_requests_total counter
    /// http_requests_total{method="GET",endpoint="/api/users",status="200"} 1234
    /// 
    /// # HELP users_active_total Total active users
    /// # TYPE users_active_total gauge
    /// users_active_total 567
    /// ```
    /// 
    /// **Integración:**
    /// - Prometheus scraping
    /// - Grafana dashboards
    /// - Alerting rules
    /// 
    /// **Content-Type:** `text/plain; version=0.0.4`
    /// 
    /// **🔹 CONSUMO A TRAVÉS DEL GATEWAY /api/v1/translate:**
    /// ```json
    /// {
    ///   "service": "users",
    ///   "method": "GET",
    ///   "path": "/metrics"
    /// }
    /// ```
    /// </remarks>
    /// <returns>Métricas en formato Prometheus</returns>
    /// <response code="200">Métricas exportadas exitosamente</response>
    [HttpGet("/users-service/metrics")]
    [ApiExplorerSettings(GroupName = "users")]
    [SwaggerOperation(
        OperationId = "GetUsersMetrics",
        Summary = "Métricas de Prometheus del microservicio Users",
        Description = "Expone métricas en formato Prometheus para scraping y monitoreo.",
        Tags = new[] { "OBSERVABILITY" }
    )]
    [Produces("text/plain")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetMetrics()
    {
        throw new NotImplementedException("Este endpoint es manejado por el reverse proxy YARP");
    }
}
