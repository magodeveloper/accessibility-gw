namespace Gateway.Models.Swagger.Reports;

/// <summary>
/// Respuesta de reporte de accesibilidad
/// </summary>
public class ReportResponse
{
    /// <summary>ID único del reporte</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>ID del análisis asociado</summary>
    public string AnalysisId { get; set; } = string.Empty;

    /// <summary>Formato del reporte (PDF, HTML, JSON, CSV)</summary>
    public string Format { get; set; } = string.Empty;

    /// <summary>Título del reporte</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>URL del archivo generado</summary>
    public string? FileUrl { get; set; }

    /// <summary>Tamaño del archivo en bytes</summary>
    public long FileSize { get; set; }

    /// <summary>Fecha de generación</summary>
    public DateTime GeneratedAt { get; set; }

    /// <summary>Estado del reporte (Pending, Generated, Failed)</summary>
    public string Status { get; set; } = string.Empty;
}

/// <summary>
/// Solicitud para crear un reporte
/// </summary>
public class CreateReportRequest
{
    /// <summary>ID del análisis</summary>
    public string AnalysisId { get; set; } = string.Empty;

    /// <summary>Formato deseado (PDF, HTML, JSON, CSV)</summary>
    public string Format { get; set; } = "PDF";

    /// <summary>Título del reporte (opcional)</summary>
    public string? Title { get; set; }

    /// <summary>Incluir detalles completos</summary>
    public bool IncludeDetails { get; set; } = true;

    /// <summary>Incluir capturas de pantalla</summary>
    public bool IncludeScreenshots { get; set; } = false;
}

/// <summary>
/// Respuesta de entrada del historial
/// </summary>
public class HistoryResponse
{
    /// <summary>ID único de la entrada</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>ID del usuario</summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>ID del análisis</summary>
    public string AnalysisId { get; set; } = string.Empty;

    /// <summary>ID del reporte generado</summary>
    public string? ReportId { get; set; }

    /// <summary>Acción realizada</summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>Descripción de la acción</summary>
    public string? Description { get; set; }

    /// <summary>Fecha de la acción</summary>
    public DateTime Timestamp { get; set; }

    /// <summary>Metadata adicional (JSON)</summary>
    public string? Metadata { get; set; }
}

/// <summary>
/// Solicitud para crear entrada en historial
/// </summary>
public class CreateHistoryRequest
{
    /// <summary>ID del usuario</summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>ID del análisis</summary>
    public string AnalysisId { get; set; } = string.Empty;

    /// <summary>ID del reporte (opcional)</summary>
    public string? ReportId { get; set; }

    /// <summary>Acción realizada</summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>Descripción (opcional)</summary>
    public string? Description { get; set; }

    /// <summary>Metadata adicional (opcional)</summary>
    public string? Metadata { get; set; }
}
