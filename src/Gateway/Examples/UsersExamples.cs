using Gateway.Models.Swagger.Users;
using Swashbuckle.AspNetCore.Filters;

namespace Gateway.Examples;

// ============================================================================
// AUTH EXAMPLES
// ============================================================================

/// <summary>
/// Ejemplo de registro de usuario nuevo
/// </summary>
public class RegisterRequestExample : IExamplesProvider<RegisterRequest>
{
    public RegisterRequest GetExamples()
    {
        return new RegisterRequest
        {
            Email = "maria.garcia@example.com",
            Password = "SecurePass123!",
            ConfirmPassword = "SecurePass123!",
            DisplayName = "María García",
            AcceptTerms = true
        };
    }
}

/// <summary>
/// Ejemplo de inicio de sesión
/// </summary>
public class LoginRequestExample : IExamplesProvider<LoginRequest>
{
    public LoginRequest GetExamples()
    {
        return new LoginRequest
        {
            Email = "usuario@example.com",
            Password = "MiPassword123",
            RememberMe = false
        };
    }
}

/// <summary>
/// Ejemplo de inicio de sesión con "Recordarme"
/// </summary>
public class LoginRequestRememberMeExample : IExamplesProvider<LoginRequest>
{
    public LoginRequest GetExamples()
    {
        return new LoginRequest
        {
            Email = "usuario@example.com",
            Password = "MiPassword123",
            RememberMe = true // Token válido por 30 días
        };
    }
}

/// <summary>
/// Ejemplo de cierre de sesión simple (solo dispositivo actual)
/// </summary>
public class LogoutRequestExample : IExamplesProvider<LogoutRequest>
{
    public LogoutRequest GetExamples()
    {
        return new LogoutRequest
        {
            Token = null, // Opcional - se toma del header Authorization
            LogoutAllDevices = false
        };
    }
}

/// <summary>
/// Ejemplo de cierre de sesión en todos los dispositivos
/// </summary>
public class LogoutAllDevicesRequestExample : IExamplesProvider<LogoutRequest>
{
    public LogoutRequest GetExamples()
    {
        return new LogoutRequest
        {
            Token = null,
            LogoutAllDevices = true // Cierra TODAS las sesiones del usuario
        };
    }
}

/// <summary>
/// Ejemplo de solicitud de recuperación de contraseña
/// </summary>
public class ForgotPasswordRequestExample : IExamplesProvider<ForgotPasswordRequest>
{
    public ForgotPasswordRequest GetExamples()
    {
        return new ForgotPasswordRequest
        {
            Email = "usuario@example.com"
        };
    }
}

/// <summary>
/// Ejemplo de reseteo de contraseña con token
/// </summary>
public class ResetPasswordRequestExample : IExamplesProvider<ResetPasswordRequest>
{
    public ResetPasswordRequest GetExamples()
    {
        return new ResetPasswordRequest
        {
            Email = "usuario@example.com",
            ResetToken = "123456",
            NewPassword = "NuevaPassword456!",
            ConfirmNewPassword = "NuevaPassword456!"
        };
    }
}

// ============================================================================
// USER MANAGEMENT EXAMPLES
// ============================================================================

/// <summary>
/// Ejemplo de creación de usuario por administrador
/// </summary>
public class CreateUserRequestExample : IExamplesProvider<CreateUserRequest>
{
    public CreateUserRequest GetExamples()
    {
        return new CreateUserRequest
        {
            Email = "nuevo.analista@example.com",
            Password = "TempPassword123!",
            DisplayName = "Juan Pérez",
            FullName = "Juan Carlos Pérez Sánchez",
            Roles = new List<string> { "User", "Analyst" },
            SendWelcomeEmail = true,
            RequirePasswordChange = true
        };
    }
}

/// <summary>
/// Ejemplo de creación de usuario administrador
/// </summary>
public class CreateAdminUserRequestExample : IExamplesProvider<CreateUserRequest>
{
    public CreateUserRequest GetExamples()
    {
        return new CreateUserRequest
        {
            Email = "admin.nuevo@example.com",
            Password = "AdminPass789!",
            DisplayName = "Laura Martínez",
            FullName = "Laura Patricia Martínez Rodríguez",
            Roles = new List<string> { "User", "Admin" },
            SendWelcomeEmail = true,
            RequirePasswordChange = false
        };
    }
}

/// <summary>
/// Ejemplo de actualización de perfil de usuario
/// </summary>
public class UpdateUserRequestExample : IExamplesProvider<UpdateUserRequest>
{
    public UpdateUserRequest GetExamples()
    {
        return new UpdateUserRequest
        {
            DisplayName = "María García Actualizado",
            FullName = "María Elena García López de la Torre",
            Email = null, // No cambiar email en este ejemplo
            ProfilePictureUrl = "https://cdn.example.com/avatars/maria-garcia-new.jpg"
        };
    }
}

/// <summary>
/// Ejemplo de actualización solo de nombre visible
/// </summary>
public class UpdateUserDisplayNameOnlyExample : IExamplesProvider<UpdateUserRequest>
{
    public UpdateUserRequest GetExamples()
    {
        return new UpdateUserRequest
        {
            DisplayName = "Nuevo Nombre Visible",
            FullName = null,
            Email = null,
            ProfilePictureUrl = null
        };
    }
}

/// <summary>
/// Ejemplo de cambio de email
/// </summary>
public class UpdateUserEmailExample : IExamplesProvider<UpdateUserRequest>
{
    public UpdateUserRequest GetExamples()
    {
        return new UpdateUserRequest
        {
            DisplayName = null,
            FullName = null,
            Email = "nuevo.email@example.com",
            ProfilePictureUrl = null
        };
    }
}

/// <summary>
/// Ejemplo de cambio de contraseña
/// </summary>
public class ChangePasswordRequestExample : IExamplesProvider<ChangePasswordRequest>
{
    public ChangePasswordRequest GetExamples()
    {
        return new ChangePasswordRequest
        {
            CurrentPassword = "PasswordActual123",
            NewPassword = "NuevaPassword456!",
            ConfirmNewPassword = "NuevaPassword456!",
            LogoutOtherSessions = false
        };
    }
}

/// <summary>
/// Ejemplo de búsqueda de usuarios con filtros
/// </summary>
public class UserSearchParamsExample : IExamplesProvider<UserSearchParams>
{
    public UserSearchParams GetExamples()
    {
        return new UserSearchParams
        {
            SearchTerm = "garcía",
            Role = "Analyst",
            IsActive = true,
            EmailVerified = true,
            PageNumber = 1,
            PageSize = 20,
            SortBy = "CreatedAt"
        };
    }
}

/// <summary>
/// Ejemplo de búsqueda simple por nombre
/// </summary>
public class UserSearchSimpleExample : IExamplesProvider<UserSearchParams>
{
    public UserSearchParams GetExamples()
    {
        return new UserSearchParams
        {
            SearchTerm = "juan",
            Role = null,
            IsActive = null,
            EmailVerified = null,
            PageNumber = 1,
            PageSize = 10,
            SortBy = "DisplayName"
        };
    }
}

// ============================================================================
// PREFERENCES EXAMPLES
// ============================================================================

/// <summary>
/// Ejemplo de actualización completa de preferencias
/// </summary>
public class UpdatePreferencesCompleteExample : IExamplesProvider<UpdatePreferencesRequest>
{
    public UpdatePreferencesRequest GetExamples()
    {
        return new UpdatePreferencesRequest
        {
            Theme = "dark",
            Language = "es",
            Timezone = "America/Mexico_City",
            Notifications = new NotificationPreferences
            {
                EmailEnabled = true,
                PushEnabled = true,
                NotifyOnAnalysisComplete = true,
                DailySummary = true,
                WeeklySummary = false
            },
            Accessibility = new AccessibilityPreferences
            {
                ScreenReaderMode = false,
                HighContrast = true,
                FontSize = "large",
                ReduceMotion = false
            },
            Analysis = new AnalysisPreferences
            {
                DefaultWcagLevel = "AA",
                IncludeWarnings = true,
                MaxScanDepth = 5
            },
            Reports = new ReportPreferences
            {
                DefaultFormat = "pdf",
                IncludeCharts = true,
                IncludeScreenshots = true,
                DetailLevel = "standard"
            }
        };
    }
}

/// <summary>
/// Ejemplo de cambio solo de tema e idioma
/// </summary>
public class UpdatePreferencesThemeLanguageExample : IExamplesProvider<UpdatePreferencesRequest>
{
    public UpdatePreferencesRequest GetExamples()
    {
        return new UpdatePreferencesRequest
        {
            Theme = "dark",
            Language = "en",
            Timezone = null,
            Notifications = null,
            Accessibility = null,
            Analysis = null,
            Reports = null
        };
    }
}

/// <summary>
/// Ejemplo de configuración de accesibilidad
/// </summary>
public class UpdatePreferencesAccessibilityExample : IExamplesProvider<UpdatePreferencesRequest>
{
    public UpdatePreferencesRequest GetExamples()
    {
        return new UpdatePreferencesRequest
        {
            Theme = null,
            Language = null,
            Timezone = null,
            Notifications = null,
            Accessibility = new AccessibilityPreferences
            {
                ScreenReaderMode = true,
                HighContrast = true,
                FontSize = "x-large",
                ReduceMotion = true
            },
            Analysis = null,
            Reports = null
        };
    }
}

/// <summary>
/// Ejemplo de configuración de notificaciones
/// </summary>
public class UpdatePreferencesNotificationsExample : IExamplesProvider<UpdatePreferencesRequest>
{
    public UpdatePreferencesRequest GetExamples()
    {
        return new UpdatePreferencesRequest
        {
            Theme = null,
            Language = null,
            Timezone = null,
            Notifications = new NotificationPreferences
            {
                EmailEnabled = true,
                PushEnabled = false,
                NotifyOnAnalysisComplete = true,
                DailySummary = true,
                WeeklySummary = true
            },
            Accessibility = null,
            Analysis = null,
            Reports = null
        };
    }
}

/// <summary>
/// Ejemplo de configuración de análisis WCAG
/// </summary>
public class UpdatePreferencesAnalysisExample : IExamplesProvider<UpdatePreferencesRequest>
{
    public UpdatePreferencesRequest GetExamples()
    {
        return new UpdatePreferencesRequest
        {
            Theme = null,
            Language = null,
            Timezone = null,
            Notifications = null,
            Accessibility = null,
            Analysis = new AnalysisPreferences
            {
                DefaultWcagLevel = "AAA", // Nivel más estricto
                IncludeWarnings = true,
                MaxScanDepth = 10 // Análisis profundo
            },
            Reports = null
        };
    }
}

/// <summary>
/// Ejemplo de configuración de reportes
/// </summary>
public class UpdatePreferencesReportsExample : IExamplesProvider<UpdatePreferencesRequest>
{
    public UpdatePreferencesRequest GetExamples()
    {
        return new UpdatePreferencesRequest
        {
            Theme = null,
            Language = null,
            Timezone = null,
            Notifications = null,
            Accessibility = null,
            Analysis = null,
            Reports = new ReportPreferences
            {
                DefaultFormat = "html", // HTML interactivo
                IncludeCharts = true,
                IncludeScreenshots = true,
                DetailLevel = "detailed" // Nivel detallado
            }
        };
    }
}

// ============================================================================
// SESSION MANAGEMENT EXAMPLES
// ============================================================================

/// <summary>
/// Ejemplo de solicitud de revocación de sesión específica
/// </summary>
public class RevokeSessionRequestExample : IExamplesProvider<RevokeSessionRequest>
{
    public RevokeSessionRequest GetExamples()
    {
        return new RevokeSessionRequest
        {
            SessionId = "sess-abc123def456",
            Reason = "Sesión sospechosa desde ubicación desconocida"
        };
    }
}

/// <summary>
/// Ejemplo de revocación sin razón específica
/// </summary>
public class RevokeSessionSimpleExample : IExamplesProvider<RevokeSessionRequest>
{
    public RevokeSessionRequest GetExamples()
    {
        return new RevokeSessionRequest
        {
            SessionId = "sess-xyz789uvw012",
            Reason = null
        };
    }
}

/// <summary>
/// Ejemplo de revocación de todas las sesiones excepto la actual
/// </summary>
public class RevokeOtherSessionsRequestExample : IExamplesProvider<RevokeOtherSessionsRequest>
{
    public RevokeOtherSessionsRequest GetExamples()
    {
        return new RevokeOtherSessionsRequest
        {
            ConfirmRevocation = true,
            Reason = "Cambio de contraseña - cerrar sesión en otros dispositivos"
        };
    }
}

/// <summary>
/// Ejemplo de token de actualización (refresh token)
/// </summary>
public class RefreshTokenRequestExample : IExamplesProvider<RefreshTokenRequest>
{
    public RefreshTokenRequest GetExamples()
    {
        return new RefreshTokenRequest
        {
            RefreshToken = "abc123def456ghi789jkl012mno345pqr678stu901vwx234yz"
        };
    }
}
