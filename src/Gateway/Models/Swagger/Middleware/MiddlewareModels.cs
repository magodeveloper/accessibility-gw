namespace Gateway.Models.Swagger.Middleware;

/// <summary>
/// Solicitud de análisis de accesibilidad
/// </summary>
public class AnalyzeRequest
{
    /// <summary>URL a analizar</summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>Herramienta de análisis (axe-core, pa11y, lighthouse, ibm-aat)</summary>
    public string Tool { get; set; } = "axe-core";

    /// <summary>Estándar WCAG (WCAG2A, WCAG2AA, WCAG2AAA)</summary>
    public string Standard { get; set; } = "WCAG2AA";

    /// <summary>Incluir captura de pantalla</summary>
    public bool IncludeScreenshot { get; set; } = false;

    /// <summary>Timeout en milisegundos</summary>
    public int Timeout { get; set; } = 30000;

    /// <summary>ID del usuario solicitante (opcional)</summary>
    public string? UserId { get; set; }
}

/// <summary>
/// Respuesta de análisis de accesibilidad
/// </summary>
public class AnalyzeResponse
{
    /// <summary>ID del análisis generado</summary>
    public string AnalysisId { get; set; } = string.Empty;

    /// <summary>URL analizada</summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>Herramienta utilizada</summary>
    public string Tool { get; set; } = string.Empty;

    /// <summary>Estado del análisis</summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>Total de violaciones encontradas</summary>
    public int Violations { get; set; }

    /// <summary>Total de pruebas pasadas</summary>
    public int Passes { get; set; }

    /// <summary>Total de pruebas incompletas</summary>
    public int Incomplete { get; set; }

    /// <summary>Total de pruebas no aplicables</summary>
    public int Inapplicable { get; set; }

    /// <summary>Timestamp del análisis</summary>
    public DateTime Timestamp { get; set; }

    /// <summary>Duración del análisis en milisegundos</summary>
    public int Duration { get; set; }

    /// <summary>URL de la captura de pantalla (si se solicitó)</summary>
    public string? ScreenshotUrl { get; set; }
}

/// <summary>
/// Respuesta de health check del middleware
/// </summary>
public class HealthResponse
{
    /// <summary>Estado de salud (Healthy, Degraded, Unhealthy)</summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>Timestamp del health check</summary>
    public DateTime Timestamp { get; set; }

    /// <summary>Tiempo de actividad en segundos</summary>
    public long Uptime { get; set; }

    /// <summary>Versión del middleware</summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>Detalles adicionales del estado</summary>
    public Dictionary<string, object>? Details { get; set; }
}
