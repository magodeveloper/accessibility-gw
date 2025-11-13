using Microsoft.AspNetCore.Mvc;
using Gateway.Models.Swagger.Shared;
using Gateway.Models.Swagger.Intelligence;
using Swashbuckle.AspNetCore.Annotations;

namespace Gateway.Controllers;

/// <summary>
/// Controller proxy para documentación de Intelligence API en Swagger.
/// Este controller NO implementa lógica real - solo documenta los endpoints del microservicio Intelligence.
/// Las peticiones reales son manejadas por YARP reverse proxy.
/// 
/// **🔹 CONSUMO A TRAVÉS DEL GATEWAY:**
/// Todos los endpoints de esta API deben consumirse a través del endpoint universal:
/// 
/// **POST /api/v1/translate**
/// 
/// **Ejemplo - Generar recomendaciones:**
/// ```json
/// {
///   "service": "intelligence",
///   "method": "POST",
///   "path": "/api/v1/AIRecommendations/generate",
///   "headers": {
///     "Content-Type": "application/json",
///     "Authorization": "Bearer {token}"
///   },
///   "body": "{\"analysisData\":{...},\"userLevel\":\"intermediate\"}"
/// }
/// ```
/// 
/// Los endpoints documentados aquí muestran la estructura de **path**, **method** y **body**.
/// </summary>
[ApiController]
[Route("api/v1/AIRecommendations")]
[ApiExplorerSettings(GroupName = "intelligence", IgnoreApi = false)]
[Produces("application/json")]
[SwaggerTag("Endpoints de inteligencia artificial para recomendaciones de accesibilidad")]
public class IntelligenceProxyController : ControllerBase
{
    // ============================================================================
    // SECCIÓN: RECOMENDACIONES DE IA (4 endpoints)
    // ============================================================================

    /// <summary>
    /// Generar recomendaciones de IA para un análisis completo
    /// </summary>
    /// <remarks>
    /// **🔹 CONSUMO A TRAVÉS DEL GATEWAY /api/v1/translate:**
    /// ```json
    /// {
    ///   "service": "intelligence",
    ///   "method": "POST",
    ///   "path": "/api/v1/AIRecommendations/generate",
    ///   "headers": {
    ///     "Content-Type": "application/json",
    ///     "Authorization": "Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
    ///   },
    ///   "body": "{\"analysisData\":{\"analysisId\":\"123\",\"url\":\"https://example.com\",\"results\":\"...\"},\"userLevel\":\"intermediate\",\"userLanguage\":\"es\",\"maxRecommendations\":10}"
    /// }
    /// ```
    /// 
    /// **Niveles de IA disponibles:**
    /// - **basic**: Lenguaje simple, analogías, sin código técnico
    /// - **intermediate**: Balance entre técnico y accesible, ejemplos de código
    /// - **advanced**: Análisis experto, referencias W3C, múltiples soluciones
    /// 
    /// **Ejemplo de análisis completo:**
    /// ```json
    /// {
    ///   "analysisData": {
    ///     "analysisId": "analysis-123",
    ///     "url": "https://mi-sitio.com",
    ///     "analysisDate": "2025-11-13T10:00:00Z",
    ///     "results": "{\"violations\":[...],\"passes\":[...],\"incomplete\":[...]}"
    ///   },
    ///   "userLevel": "intermediate",
    ///   "userLanguage": "es",
    ///   "maxRecommendations": 10
    /// }
    /// ```
    /// </remarks>
    [HttpPost("generate")]
    [SwaggerOperation(
        OperationId = "GenerateAIRecommendations",
        Summary = "Generar recomendaciones de IA",
        Description = "Genera recomendaciones personalizadas de accesibilidad usando IA generativa basándose en los resultados del análisis",
        Tags = new[] { "AI RECOMMENDATIONS" }
    )]
    [ProducesResponseType(typeof(AIRecommendationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public IActionResult GenerateRecommendations([FromBody] AIRecommendationRequest request)
    {
        throw new NotImplementedException("Este endpoint es manejado por el reverse proxy YARP");
    }

    /// <summary>
    /// Obtener recomendaciones cacheadas por ID de análisis
    /// </summary>
    /// <remarks>
    /// **🔹 CONSUMO A TRAVÉS DEL GATEWAY /api/v1/translate:**
    /// ```json
    /// {
    ///   "service": "intelligence",
    ///   "method": "GET",
    ///   "path": "/api/v1/AIRecommendations/analysis/123",
    ///   "headers": {
    ///     "Authorization": "Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
    ///   }
    /// }
    /// ```
    /// 
    /// Recupera recomendaciones previamente generadas desde la caché (Redis).
    /// Si no existen en caché, retorna 404 y se debe generar nuevamente.
    /// </remarks>
    [HttpGet("analysis/{analysisId}")]
    [SwaggerOperation(
        OperationId = "GetCachedRecommendations",
        Summary = "Obtener recomendaciones cacheadas",
        Description = "Recupera recomendaciones de IA desde la caché si existen",
        Tags = new[] { "AI RECOMMENDATIONS" }
    )]
    [ProducesResponseType(typeof(AIRecommendationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public IActionResult GetCachedRecommendations([FromRoute] string analysisId)
    {
        throw new NotImplementedException("Este endpoint es manejado por el reverse proxy YARP");
    }

    /// <summary>
    /// Generar recomendación para un error específico
    /// </summary>
    /// <remarks>
    /// **🔹 CONSUMO A TRAVÉS DEL GATEWAY /api/v1/translate:**
    /// ```json
    /// {
    ///   "service": "intelligence",
    ///   "method": "POST",
    ///   "path": "/api/v1/AIRecommendations/single",
    ///   "headers": {
    ///     "Content-Type": "application/json",
    ///     "Authorization": "Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
    ///   },
    ///   "body": "{\"error\":{\"code\":\"color-contrast\",\"message\":\"Contraste insuficiente\",\"impact\":\"serious\"},\"userLevel\":\"intermediate\",\"language\":\"es\"}"
    /// }
    /// ```
    /// 
    /// **Ejemplo de error específico:**
    /// ```json
    /// {
    ///   "error": {
    ///     "code": "color-contrast",
    ///     "message": "El texto no tiene suficiente contraste con el fondo",
    ///     "impact": "serious",
    ///     "wcagCriterion": "1.4.3",
    ///     "htmlContext": "<div style='color:#999;background:#fff'>Texto</div>"
    ///   },
    ///   "userLevel": "intermediate",
    ///   "language": "es"
    /// }
    /// ```
    /// </remarks>
    [HttpPost("single")]
    [SwaggerOperation(
        OperationId = "GenerateSingleRecommendation",
        Summary = "Generar recomendación para error específico",
        Description = "Genera una recomendación detallada de IA para un error individual de accesibilidad",
        Tags = new[] { "AI RECOMMENDATIONS" }
    )]
    [ProducesResponseType(typeof(AIRecommendation), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public IActionResult GenerateSingleRecommendation([FromBody] SingleRecommendationRequest request)
    {
        throw new NotImplementedException("Este endpoint es manejado por el reverse proxy YARP");
    }

    /// <summary>
    /// Health check del servicio de IA
    /// </summary>
    /// <remarks>
    /// **🔹 CONSUMO A TRAVÉS DEL GATEWAY /api/v1/translate:**
    /// ```json
    /// {
    ///   "service": "intelligence",
    ///   "method": "GET",
    ///   "path": "/api/v1/AIRecommendations/health",
    ///   "headers": {}
    /// }
    /// ```
    /// 
    /// Verifica el estado del servicio Intelligence API y la conectividad con OpenAI.
    /// </remarks>
    [HttpGet("health")]
    [SwaggerOperation(
        OperationId = "IntelligenceHealthCheck",
        Summary = "Health check del servicio de IA",
        Description = "Verifica el estado del servicio Intelligence API",
        Tags = new[] { "AI RECOMMENDATIONS" }
    )]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public IActionResult HealthCheck()
    {
        throw new NotImplementedException("Este endpoint es manejado por el reverse proxy YARP");
    }
}
