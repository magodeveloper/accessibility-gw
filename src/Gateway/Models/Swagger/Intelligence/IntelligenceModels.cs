namespace Gateway.Models.Swagger.Intelligence;

/// <summary>
/// Request para generar recomendaciones de IA
/// </summary>
public class AIRecommendationRequest
{
    /// <summary>
    /// Datos del análisis de accesibilidad
    /// </summary>
    public required AnalysisDataDto AnalysisData { get; set; }

    /// <summary>
    /// Nivel de respuesta de IA (basic, intermediate, advanced)
    /// </summary>
    public string UserLevel { get; set; } = "intermediate";

    /// <summary>
    /// Idioma de las recomendaciones (es, en)
    /// </summary>
    public string UserLanguage { get; set; } = "es";

    /// <summary>
    /// Número máximo de recomendaciones a generar
    /// </summary>
    public int MaxRecommendations { get; set; } = 10;
}

/// <summary>
/// Response con las recomendaciones de IA generadas
/// </summary>
public class AIRecommendationResponse
{
    /// <summary>
    /// Lista de recomendaciones generadas
    /// </summary>
    public required List<AIRecommendation> Recommendations { get; set; }

    /// <summary>
    /// Resumen ejecutivo de las recomendaciones
    /// </summary>
    public required AIRecommendationsSummary Summary { get; set; }

    /// <summary>
    /// Estadísticas del análisis
    /// </summary>
    public required AIStatistics Statistics { get; set; }

    /// <summary>
    /// Clave de caché (si aplica)
    /// </summary>
    public string? CacheKey { get; set; }
}

/// <summary>
/// Recomendación individual de IA
/// </summary>
public class AIRecommendation
{
    /// <summary>
    /// ID único de la recomendación
    /// </summary>
    public required string Id { get; set; }

    /// <summary>
    /// Título de la recomendación
    /// </summary>
    public required string Title { get; set; }

    /// <summary>
    /// Descripción detallada
    /// </summary>
    public required string Description { get; set; }

    /// <summary>
    /// Severidad (critical, serious, moderate, minor)
    /// </summary>
    public required string Severity { get; set; }

    /// <summary>
    /// Categoría (color-contrast, aria, forms, etc.)
    /// </summary>
    public required string Category { get; set; }

    /// <summary>
    /// Prioridad numérica (1-10)
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    /// Ejemplo de código correcto
    /// </summary>
    public string? CodeExample { get; set; }

    /// <summary>
    /// Recursos adicionales
    /// </summary>
    public List<AIResource>? Resources { get; set; }

    /// <summary>
    /// Criterio WCAG relacionado
    /// </summary>
    public string? WcagCriterion { get; set; }

    /// <summary>
    /// Nivel WCAG (A, AA, AAA)
    /// </summary>
    public string? WcagLevel { get; set; }
}

/// <summary>
/// Recurso adicional para aprender más
/// </summary>
public class AIResource
{
    /// <summary>
    /// Título del recurso
    /// </summary>
    public required string Title { get; set; }

    /// <summary>
    /// URL del recurso
    /// </summary>
    public required string Url { get; set; }

    /// <summary>
    /// Tipo de recurso (documentation, tutorial, article, etc.)
    /// </summary>
    public string Type { get; set; } = "documentation";
}

/// <summary>
/// Resumen ejecutivo de las recomendaciones
/// </summary>
public class AIRecommendationsSummary
{
    /// <summary>
    /// Número total de problemas
    /// </summary>
    public int TotalIssues { get; set; }

    /// <summary>
    /// Número de problemas críticos
    /// </summary>
    public int CriticalIssues { get; set; }

    /// <summary>
    /// Número de problemas serios
    /// </summary>
    public int SeriousIssues { get; set; }

    /// <summary>
    /// Número de problemas moderados
    /// </summary>
    public int ModerateIssues { get; set; }

    /// <summary>
    /// Número de problemas menores
    /// </summary>
    public int MinorIssues { get; set; }

    /// <summary>
    /// Mensaje de resumen general
    /// </summary>
    public string? GeneralSummary { get; set; }

    /// <summary>
    /// Recomendación principal
    /// </summary>
    public string? MainRecommendation { get; set; }
}

/// <summary>
/// Estadísticas del análisis
/// </summary>
public class AIStatistics
{
    /// <summary>
    /// Puntuación de conformidad (0-100)
    /// </summary>
    public double ComplianceScore { get; set; }

    /// <summary>
    /// Nivel de conformidad WCAG (A, AA, AAA)
    /// </summary>
    public string? ComplianceLevel { get; set; }

    /// <summary>
    /// Número de criterios cumplidos
    /// </summary>
    public int PassedCriteria { get; set; }

    /// <summary>
    /// Número total de criterios evaluados
    /// </summary>
    public int TotalCriteria { get; set; }
}

/// <summary>
/// Datos del análisis de accesibilidad
/// </summary>
public class AnalysisDataDto
{
    /// <summary>
    /// ID del análisis
    /// </summary>
    public required string AnalysisId { get; set; }

    /// <summary>
    /// URL analizada
    /// </summary>
    public required string Url { get; set; }

    /// <summary>
    /// Fecha del análisis
    /// </summary>
    public DateTime AnalysisDate { get; set; }

    /// <summary>
    /// Resultados del análisis (JSON string)
    /// </summary>
    public required string Results { get; set; }
}

/// <summary>
/// Request para generar recomendación de un error específico
/// </summary>
public class SingleRecommendationRequest
{
    /// <summary>
    /// Información del error
    /// </summary>
    public required ErrorInfo Error { get; set; }

    /// <summary>
    /// Nivel de respuesta (basic, intermediate, advanced)
    /// </summary>
    public string UserLevel { get; set; } = "intermediate";

    /// <summary>
    /// Idioma (es, en)
    /// </summary>
    public string Language { get; set; } = "es";
}

/// <summary>
/// Información del error
/// </summary>
public class ErrorInfo
{
    /// <summary>
    /// Código del error
    /// </summary>
    public required string Code { get; set; }

    /// <summary>
    /// Mensaje del error
    /// </summary>
    public required string Message { get; set; }

    /// <summary>
    /// Impacto (critical, serious, moderate, minor)
    /// </summary>
    public required string Impact { get; set; }

    /// <summary>
    /// Criterio WCAG relacionado
    /// </summary>
    public string? WcagCriterion { get; set; }

    /// <summary>
    /// Contexto HTML
    /// </summary>
    public string? HtmlContext { get; set; }
}
