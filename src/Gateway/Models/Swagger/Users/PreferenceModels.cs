using System.ComponentModel.DataAnnotations;
using Swashbuckle.AspNetCore.Annotations;

namespace Gateway.Models.Swagger.Users;

/// <summary>
/// DTO completo de preferencias de usuario
/// </summary>
[SwaggerSchema(Description = "Configuración de preferencias personalizadas del usuario")]
public class UserPreferencesDto
{
    /// <summary>
    /// ID del usuario propietario de las preferencias
    /// </summary>
    [SwaggerSchema(Description = "Identificador del usuario")]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Tema de interfaz preferido
    /// </summary>
    [SwaggerSchema(Description = "Tema visual: 'light', 'dark', 'auto' (según SO)")]
    public string Theme { get; set; } = "light";

    /// <summary>
    /// Idioma de la interfaz
    /// </summary>
    [SwaggerSchema(Description = "Código de idioma ISO 639-1 (ej: 'es', 'en', 'fr')")]
    public string Language { get; set; } = "es";

    /// <summary>
    /// Zona horaria del usuario
    /// </summary>
    [SwaggerSchema(Description = "Timezone IANA (ej: 'America/Mexico_City', 'Europe/Madrid')")]
    public string Timezone { get; set; } = "UTC";

    /// <summary>
    /// Configuración de notificaciones
    /// </summary>
    [SwaggerSchema(Description = "Preferencias de notificaciones")]
    public NotificationPreferences Notifications { get; set; } = new();

    /// <summary>
    /// Configuración de accesibilidad
    /// </summary>
    [SwaggerSchema(Description = "Opciones de accesibilidad personalizadas")]
    public AccessibilityPreferences Accessibility { get; set; } = new();

    /// <summary>
    /// Configuración de análisis (para analistas)
    /// </summary>
    [SwaggerSchema(Description = "Preferencias específicas para análisis de accesibilidad")]
    public AnalysisPreferences? Analysis { get; set; }

    /// <summary>
    /// Preferencias de visualización de reportes
    /// </summary>
    [SwaggerSchema(Description = "Configuración de reportes y visualizaciones")]
    public ReportPreferences Reports { get; set; } = new();

    /// <summary>
    /// Fecha de última actualización de preferencias
    /// </summary>
    [SwaggerSchema(Description = "Última modificación de preferencias", Format = "date-time")]
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Configuración de notificaciones
/// </summary>
[SwaggerSchema(Description = "Preferencias de notificaciones por canal")]
public class NotificationPreferences
{
    /// <summary>
    /// Habilitar notificaciones por email
    /// </summary>
    [SwaggerSchema(Description = "Recibir notificaciones por correo electrónico")]
    public bool EmailEnabled { get; set; } = true;

    /// <summary>
    /// Habilitar notificaciones push en navegador
    /// </summary>
    [SwaggerSchema(Description = "Recibir notificaciones push en el navegador")]
    public bool PushEnabled { get; set; } = true;

    /// <summary>
    /// Notificar al completarse un análisis
    /// </summary>
    [SwaggerSchema(Description = "Notificar cuando un análisis termine")]
    public bool NotifyOnAnalysisComplete { get; set; } = true;

    /// <summary>
    /// Notificar al generarse un reporte
    /// </summary>
    [SwaggerSchema(Description = "Notificar cuando un reporte esté listo")]
    public bool NotifyOnReportGenerated { get; set; } = true;

    /// <summary>
    /// Notificar cambios importantes en el sistema
    /// </summary>
    [SwaggerSchema(Description = "Recibir notificaciones de actualizaciones del sistema")]
    public bool NotifySystemUpdates { get; set; } = true;

    /// <summary>
    /// Resumen diario por email
    /// </summary>
    [SwaggerSchema(Description = "Enviar resumen diario de actividad")]
    public bool DailySummary { get; set; } = false;

    /// <summary>
    /// Resumen semanal por email
    /// </summary>
    [SwaggerSchema(Description = "Enviar resumen semanal de análisis y reportes")]
    public bool WeeklySummary { get; set; } = false;
}

/// <summary>
/// Configuración de accesibilidad
/// </summary>
[SwaggerSchema(Description = "Opciones de accesibilidad de la interfaz")]
public class AccessibilityPreferences
{
    /// <summary>
    /// Habilitar lector de pantalla
    /// </summary>
    [SwaggerSchema(Description = "Optimizar interfaz para lectores de pantalla")]
    public bool ScreenReaderMode { get; set; } = false;

    /// <summary>
    /// Tamaño de fuente aumentado
    /// </summary>
    [SwaggerSchema(Description = "Escala de fuente: 'normal', 'large', 'x-large'")]
    public string FontSize { get; set; } = "normal";

    /// <summary>
    /// Alto contraste
    /// </summary>
    [SwaggerSchema(Description = "Activar modo de alto contraste")]
    public bool HighContrast { get; set; } = false;

    /// <summary>
    /// Reducir animaciones
    /// </summary>
    [SwaggerSchema(Description = "Deshabilitar animaciones y transiciones")]
    public bool ReduceMotion { get; set; } = false;

    /// <summary>
    /// Navegación por teclado resaltada
    /// </summary>
    [SwaggerSchema(Description = "Resaltar elementos enfocados con teclado")]
    public bool KeyboardHighlight { get; set; } = true;
}

/// <summary>
/// Configuración de análisis
/// </summary>
[SwaggerSchema(Description = "Preferencias para análisis de accesibilidad")]
public class AnalysisPreferences
{
    /// <summary>
    /// Nivel WCAG predeterminado
    /// </summary>
    [SwaggerSchema(Description = "Nivel de conformidad por defecto: 'A', 'AA', 'AAA'")]
    public string DefaultWcagLevel { get; set; } = "AA";

    /// <summary>
    /// Incluir advertencias en análisis
    /// </summary>
    [SwaggerSchema(Description = "Incluir issues de severidad 'warning' además de 'error'")]
    public bool IncludeWarnings { get; set; } = true;

    /// <summary>
    /// Profundidad máxima de escaneo
    /// </summary>
    [SwaggerSchema(Description = "Niveles de profundidad en estructura de página (1-10)")]
    public int MaxScanDepth { get; set; } = 5;

    /// <summary>
    /// Timeout de análisis en segundos
    /// </summary>
    [SwaggerSchema(Description = "Tiempo máximo de ejecución de análisis (30-300 seg)")]
    public int AnalysisTimeout { get; set; } = 120;

    /// <summary>
    /// Generar reporte automáticamente tras análisis
    /// </summary>
    [SwaggerSchema(Description = "Crear reporte PDF automáticamente al finalizar")]
    public bool AutoGenerateReport { get; set; } = false;
}

/// <summary>
/// Configuración de reportes
/// </summary>
[SwaggerSchema(Description = "Preferencias de generación y visualización de reportes")]
public class ReportPreferences
{
    /// <summary>
    /// Formato de reporte predeterminado
    /// </summary>
    [SwaggerSchema(Description = "Formato por defecto: 'pdf', 'html', 'json', 'csv'")]
    public string DefaultFormat { get; set; } = "pdf";

    /// <summary>
    /// Incluir gráficos en reportes
    /// </summary>
    [SwaggerSchema(Description = "Agregar gráficos y visualizaciones")]
    public bool IncludeCharts { get; set; } = true;

    /// <summary>
    /// Incluir screenshots de issues
    /// </summary>
    [SwaggerSchema(Description = "Capturar screenshots de problemas encontrados")]
    public bool IncludeScreenshots { get; set; } = true;

    /// <summary>
    /// Nivel de detalle de reportes
    /// </summary>
    [SwaggerSchema(Description = "Detalle: 'summary' (resumen), 'standard', 'detailed' (completo)")]
    public string DetailLevel { get; set; } = "standard";

    /// <summary>
    /// Idioma de reportes
    /// </summary>
    [SwaggerSchema(Description = "Idioma del contenido del reporte (ej: 'es', 'en')")]
    public string ReportLanguage { get; set; } = "es";
}

/// <summary>
/// Modelo de solicitud para actualizar preferencias
/// </summary>
[SwaggerSchema(Description = "Datos para actualizar preferencias de usuario")]
public class UpdatePreferencesRequest
{
    /// <summary>
    /// Tema de interfaz
    /// </summary>
    [RegularExpression("^(light|dark|auto)$", ErrorMessage = "Tema inválido. Valores permitidos: light, dark, auto")]
    [SwaggerSchema(Description = "Tema visual (opcional): 'light', 'dark', 'auto'")]
    public string? Theme { get; set; }

    /// <summary>
    /// Idioma de interfaz
    /// </summary>
    [RegularExpression("^[a-z]{2}$", ErrorMessage = "Código de idioma inválido. Use formato ISO 639-1 (2 letras)")]
    [SwaggerSchema(Description = "Código de idioma ISO 639-1 (opcional): 'es', 'en', 'fr'")]
    public string? Language { get; set; }

    /// <summary>
    /// Zona horaria
    /// </summary>
    [SwaggerSchema(Description = "Timezone IANA (opcional): 'America/Mexico_City', 'UTC'")]
    public string? Timezone { get; set; }

    /// <summary>
    /// Configuración de notificaciones
    /// </summary>
    [SwaggerSchema(Description = "Preferencias de notificaciones (opcional)")]
    public NotificationPreferences? Notifications { get; set; }

    /// <summary>
    /// Configuración de accesibilidad
    /// </summary>
    [SwaggerSchema(Description = "Opciones de accesibilidad (opcional)")]
    public AccessibilityPreferences? Accessibility { get; set; }

    /// <summary>
    /// Configuración de análisis
    /// </summary>
    [SwaggerSchema(Description = "Preferencias de análisis (opcional, solo para analistas)")]
    public AnalysisPreferences? Analysis { get; set; }

    /// <summary>
    /// Configuración de reportes
    /// </summary>
    [SwaggerSchema(Description = "Preferencias de reportes (opcional)")]
    public ReportPreferences? Reports { get; set; }
}

/// <summary>
/// Respuesta de operación sobre preferencias
/// </summary>
[SwaggerSchema(Description = "Confirmación de actualización de preferencias")]
public class PreferencesOperationResponse
{
    /// <summary>
    /// Indica si la operación fue exitosa
    /// </summary>
    [SwaggerSchema(Description = "True si las preferencias se actualizaron correctamente")]
    public bool Success { get; set; }

    /// <summary>
    /// Mensaje descriptivo
    /// </summary>
    [SwaggerSchema(Description = "Mensaje de confirmación")]
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Preferencias actualizadas completas
    /// </summary>
    [SwaggerSchema(Description = "Estado actual de todas las preferencias")]
    public UserPreferencesDto? Preferences { get; set; }
}

/// <summary>
/// Respuesta con preferencias de usuario
/// </summary>
[SwaggerSchema(Description = "Preferencias recuperadas de un usuario")]
public class PreferenceResponse
{
    /// <summary>
    /// Preferencias del usuario
    /// </summary>
    [SwaggerSchema(Description = "Configuración de preferencias")]
    public UserPreferencesDto Preferences { get; set; } = new();

    /// <summary>
    /// Mensaje adicional (opcional)
    /// </summary>
    [SwaggerSchema(Description = "Información adicional")]
    public string? Message { get; set; }
}

/// <summary>
/// Solicitud para crear preferencias
/// </summary>
[SwaggerSchema(Description = "Datos para crear preferencias iniciales de usuario")]
public class CreatePreferenceRequest
{
    /// <summary>
    /// ID del usuario
    /// </summary>
    [Required(ErrorMessage = "El ID de usuario es requerido")]
    [SwaggerSchema(Description = "Identificador del usuario para crear preferencias")]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Versión de WCAG (opcional, por defecto "2.1")
    /// </summary>
    [SwaggerSchema(Description = "Versión WCAG: '2.0', '2.1', '2.2'")]
    public string? WcagVersion { get; set; }

    /// <summary>
    /// Nivel de WCAG (opcional, por defecto "AA")
    /// </summary>
    [SwaggerSchema(Description = "Nivel WCAG: 'A', 'AA', 'AAA'")]
    public string? WcagLevel { get; set; }

    /// <summary>
    /// Idioma (opcional, por defecto "es")
    /// </summary>
    [SwaggerSchema(Description = "Código de idioma: 'es', 'en'")]
    public string? Language { get; set; }

    /// <summary>
    /// Tema visual (opcional, por defecto "light")
    /// </summary>
    [SwaggerSchema(Description = "Tema: 'light', 'dark'")]
    public string? VisualTheme { get; set; }

    /// <summary>
    /// Formato de reporte (opcional, por defecto "pdf")
    /// </summary>
    [SwaggerSchema(Description = "Formato: 'pdf', 'html', 'json', 'excel'")]
    public string? ReportFormat { get; set; }

    /// <summary>
    /// Notificaciones habilitadas (opcional, por defecto true)
    /// </summary>
    [SwaggerSchema(Description = "Habilitar notificaciones")]
    public bool? NotificationsEnabled { get; set; }

    /// <summary>
    /// Nivel de respuesta de IA (opcional, por defecto "intermediate")
    /// </summary>
    [SwaggerSchema(Description = "Nivel: 'basic', 'intermediate', 'detailed'")]
    public string? AiResponseLevel { get; set; }

    /// <summary>
    /// Tamaño de fuente (opcional, por defecto 14)
    /// </summary>
    [Range(10, 24, ErrorMessage = "El tamaño de fuente debe estar entre 10 y 24")]
    [SwaggerSchema(Description = "Tamaño de fuente en píxeles (10-24)")]
    public int? FontSize { get; set; }
}

