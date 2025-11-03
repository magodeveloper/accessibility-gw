namespace Gateway.Models.Swagger.Analysis;

/// <summary>
/// Respuesta de análisis de accesibilidad
/// </summary>
public class AnalysisResponse
{
    /// <summary>ID único del análisis</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>URL analizada</summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>ID del usuario que solicitó el análisis</summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>Herramienta utilizada (axe-core, pa11y, lighthouse)</summary>
    public string Tool { get; set; } = string.Empty;

    /// <summary>Estado del análisis</summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>Fecha de creación</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Fecha de actualización</summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>Fecha de completado</summary>
    public DateTime? CompletedAt { get; set; }
}

/// <summary>
/// Solicitud para crear un análisis
/// </summary>
public class CreateAnalysisRequest
{
    /// <summary>URL a analizar</summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>ID del usuario</summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>Herramienta a utilizar</summary>
    public string Tool { get; set; } = "axe-core";

    /// <summary>Estándar WCAG (WCAG2A, WCAG2AA, WCAG2AAA)</summary>
    public string Standard { get; set; } = "WCAG2AA";
}

/// <summary>
/// Detalle de error de accesibilidad
/// </summary>
public class ErrorDetailResponse
{
    /// <summary>ID único del error</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>ID del resultado asociado</summary>
    public string ResultId { get; set; } = string.Empty;

    /// <summary>Código del error WCAG</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Tipo de error</summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>Mensaje descriptivo</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>Selector CSS del elemento</summary>
    public string Selector { get; set; } = string.Empty;

    /// <summary>Severidad (Critical, Serious, Moderate, Minor)</summary>
    public string Severity { get; set; } = string.Empty;

    /// <summary>HTML del elemento afectado</summary>
    public string? Html { get; set; }
}

/// <summary>
/// Solicitud para crear un error
/// </summary>
public class CreateErrorRequest
{
    /// <summary>ID del resultado</summary>
    public string ResultId { get; set; } = string.Empty;

    /// <summary>Código del error</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Tipo de error</summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>Mensaje</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>Selector CSS</summary>
    public string Selector { get; set; } = string.Empty;

    /// <summary>Severidad</summary>
    public string Severity { get; set; } = string.Empty;

    /// <summary>HTML del elemento (opcional)</summary>
    public string? Html { get; set; }
}

/// <summary>
/// Respuesta de resultado de análisis
/// </summary>
public class ResultResponse
{
    /// <summary>ID único del resultado</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>ID del análisis asociado</summary>
    public string AnalysisId { get; set; } = string.Empty;

    /// <summary>Nivel de conformidad WCAG</summary>
    public string Level { get; set; } = string.Empty;

    /// <summary>Severidad general</summary>
    public string Severity { get; set; } = string.Empty;

    /// <summary>Total de violaciones encontradas</summary>
    public int Violations { get; set; }

    /// <summary>Total de pruebas pasadas</summary>
    public int Passes { get; set; }

    /// <summary>Total de pruebas incompletas</summary>
    public int Incomplete { get; set; }

    /// <summary>Total de pruebas no aplicables</summary>
    public int Inapplicable { get; set; }

    /// <summary>Puntuación de accesibilidad (0-100)</summary>
    public decimal Score { get; set; }

    /// <summary>Fecha de creación</summary>
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Solicitud para crear un resultado
/// </summary>
public class CreateResultRequest
{
    /// <summary>ID del análisis</summary>
    public string AnalysisId { get; set; } = string.Empty;

    /// <summary>Nivel WCAG</summary>
    public string Level { get; set; } = string.Empty;

    /// <summary>Severidad</summary>
    public string Severity { get; set; } = string.Empty;

    /// <summary>Violaciones</summary>
    public int Violations { get; set; }

    /// <summary>Pasadas</summary>
    public int Passes { get; set; }

    /// <summary>Incompletas</summary>
    public int Incomplete { get; set; }

    /// <summary>No aplicables</summary>
    public int Inapplicable { get; set; }

    /// <summary>Puntuación</summary>
    public decimal Score { get; set; }
}
