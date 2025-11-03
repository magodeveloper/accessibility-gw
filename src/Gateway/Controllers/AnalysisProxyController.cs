using Microsoft.AspNetCore.Mvc;
using Gateway.Models.Swagger.Shared;
using Gateway.Models.Swagger.Analysis;
using Swashbuckle.AspNetCore.Annotations;

namespace Gateway.Controllers;

/// <summary>
/// Controller proxy para documentación de Analysis API en Swagger.
/// Este controller SOLO documenta - NUNCA se ejecuta. YARP maneja las peticiones reales.
/// 
/// **🔹 CONSUMO A TRAVÉS DEL GATEWAY:**
/// Todos los endpoints de esta API deben consumirse a través del endpoint universal:
/// 
/// **POST /api/v1/translate**
/// 
/// **Ejemplo - Crear análisis:**
/// ```json
/// {
///   "service": "analysis",
///   "method": "POST",
///   "path": "/api/Analysis",
///   "headers": {
///     "Content-Type": "application/json",
///     "Authorization": "Bearer {token}"
///   },
///   "body": "{\"url\":\"https://example.com\",\"tool\":\"axe-core\"}"
/// }
/// ```
/// 
/// Los endpoints documentados aquí muestran la estructura de **path**, **method** y **body**.
/// </summary>
[ApiController]
[Route("api")]
[ApiExplorerSettings(GroupName = "analysis", IgnoreApi = false)]
[Produces("application/json")]
[SwaggerTag("Endpoints de análisis de accesibilidad, gestión de errores y resultados")]
public class AnalysisProxyController : ControllerBase
{
    // ============================================================================
    // SECCIÓN: ANÁLISIS (9 endpoints)
    // ============================================================================

    /// <summary>
    /// Obtener todos los análisis de accesibilidad
    /// </summary>
    /// <remarks>
    /// **🔹 CONSUMO A TRAVÉS DEL GATEWAY /api/v1/translate:**
    /// ```json
    /// {
    ///   "service": "analysis",
    ///   "method": "GET",
    ///   "path": "/api/Analysis",
    ///   "headers": {
    ///     "Authorization": "Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
    ///   }
    /// }
    /// ```
    /// </remarks>
    [HttpGet("Analysis")]
    [SwaggerOperation(
        OperationId = "GetAllAnalysis",
        Summary = "Obtener todos los análisis",
        Description = "Recupera la lista completa de análisis de accesibilidad realizados",
        Tags = new[] { "ANALYSIS" }
    )]
    [ProducesResponseType(typeof(IEnumerable<AnalysisResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public IActionResult GetAllAnalysis()
    {
        throw new NotImplementedException("Este endpoint es solo para documentación. YARP maneja las peticiones reales.");
    }

    /// <summary>
    /// Crear un nuevo análisis de accesibilidad
    /// </summary>
    /// <remarks>
    /// **🔹 CONSUMO A TRAVÉS DEL GATEWAY /api/v1/translate:**
    /// ```json
    /// {
    ///   "service": "analysis",
    ///   "method": "POST",
    ///   "path": "/api/Analysis",
    ///   "headers": {
    ///     "Content-Type": "application/json",
    ///     "Authorization": "Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
    ///   },
    ///   "body": "{\"url\":\"https://example.com\",\"tool\":\"axe-core\",\"wcagLevel\":\"AA\"}"
    /// }
    /// ```
    /// </remarks>
    [HttpPost("Analysis")]
    [SwaggerOperation(
        OperationId = "CreateAnalysis",
        Summary = "Crear nuevo análisis",
        Description = "Inicia un nuevo análisis de accesibilidad para una URL específica",
        Tags = new[] { "ANALYSIS" }
    )]
    [ProducesResponseType(typeof(AnalysisResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public IActionResult CreateAnalysis([FromBody] CreateAnalysisRequest request)
    {
        throw new NotImplementedException("Este endpoint es solo para documentación. YARP maneja las peticiones reales.");
    }

    /// <summary>
    /// Obtener análisis por ID de usuario
    /// </summary>
    /// <remarks>
    /// **🔹 CONSUMO A TRAVÉS DEL GATEWAY /api/v1/translate:**
    /// ```json
    /// {
    ///   "service": "analysis",
    ///   "method": "GET",
    ///   "path": "/api/Analysis/by-user",
    ///   "query": {
    ///     "userId": "123e4567-e89b-12d3-a456-426614174000"
    ///   },
    ///   "headers": {
    ///     "Authorization": "Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
    ///   }
    /// }
    /// ```
    /// </remarks>
    [HttpGet("Analysis/by-user")]
    [SwaggerOperation(
        OperationId = "GetAnalysisByUser",
        Summary = "Obtener análisis por usuario",
        Description = "Recupera todos los análisis realizados por un usuario específico",
        Tags = new[] { "ANALYSIS" }
    )]
    [ProducesResponseType(typeof(IEnumerable<AnalysisResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public IActionResult GetAnalysisByUser([FromQuery] string userId)
    {
        throw new NotImplementedException("Este endpoint es solo para documentación. YARP maneja las peticiones reales.");
    }

    /// <summary>
    /// Obtener análisis por fecha
    /// </summary>
    /// <remarks>
    /// **🔹 CONSUMO A TRAVÉS DEL GATEWAY /api/v1/translate:**
    /// ```json
    /// {
    ///   "service": "analysis",
    ///   "method": "GET",
    ///   "path": "/api/Analysis/by-date",
    ///   "query": {
    ///     "startDate": "2025-01-01",
    ///     "endDate": "2025-12-31"
    ///   },
    ///   "headers": {
    ///     "Authorization": "Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
    ///   }
    /// }
    /// ```
    /// </remarks>
    [HttpGet("Analysis/by-date")]
    [SwaggerOperation(
        OperationId = "GetAnalysisByDate",
        Summary = "Obtener análisis por fecha",
        Description = "Recupera análisis filtrados por rango de fechas",
        Tags = new[] { "ANALYSIS" }
    )]
    [ProducesResponseType(typeof(IEnumerable<AnalysisResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public IActionResult GetAnalysisByDate([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
    {
        throw new NotImplementedException("Este endpoint es solo para documentación. YARP maneja las peticiones reales.");
    }    /// <summary>
         /// Obtener análisis por herramienta
         /// </summary>
         /// <remarks>
         /// **🔹 CONSUMO A TRAVÉS DEL GATEWAY /api/v1/translate:**
         /// ```json
         /// {
         ///   "service": "analysis",
         ///   "method": "GET",
         ///   "path": "/api/Analysis/by-tool",
         ///   "query": {
         ///     "tool": "axe-core"
         ///   },
         ///   "headers": {
         ///     "Authorization": "Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
         ///   }
         /// }
         /// ```
         /// </remarks>
    [HttpGet("Analysis/by-tool")]
    [SwaggerOperation(
        OperationId = "GetAnalysisByTool",
        Summary = "Obtener análisis por herramienta",
        Description = "Recupera análisis realizados con una herramienta específica (axe-core, Pa11y, etc.)",
        Tags = new[] { "ANALYSIS" }
    )]
    [ProducesResponseType(typeof(IEnumerable<AnalysisResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public IActionResult GetAnalysisByTool([FromQuery] string tool)
    {
        throw new NotImplementedException("Este endpoint es manejado por el reverse proxy YARP");
    }

    /// <summary>
    /// Obtener análisis por estado
    /// </summary>
    /// <remarks>
    /// **🔹 CONSUMO A TRAVÉS DEL GATEWAY /api/v1/translate:**
    /// ```json
    /// {
    ///   "service": "analysis",
    ///   "method": "GET",
    ///   "path": "/api/Analysis/by-status",
    ///   "query": {
    ///     "status": "Completed"
    ///   },
    ///   "headers": {
    ///     "Authorization": "Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
    ///   }
    /// }
    /// ```
    /// </remarks>
    [HttpGet("Analysis/by-status")]
    [SwaggerOperation(
        OperationId = "GetAnalysisByStatus",
        Summary = "Obtener análisis por estado",
        Description = "Recupera análisis filtrados por estado (Pending, InProgress, Completed, Failed)",
        Tags = new[] { "ANALYSIS" }
    )]
    [ProducesResponseType(typeof(IEnumerable<AnalysisResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public IActionResult GetAnalysisByStatus([FromQuery] string status)
    {
        throw new NotImplementedException("Este endpoint es manejado por el reverse proxy YARP");
    }

    /// <summary>
    /// Obtener análisis específico por ID
    /// </summary>
    /// <remarks>
    /// **🔹 CONSUMO A TRAVÉS DEL GATEWAY /api/v1/translate:**
    /// ```json
    /// {
    ///   "service": "analysis",
    ///   "method": "GET",
    ///   "path": "/api/Analysis/123",
    ///   "headers": {
    ///     "Authorization": "Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
    ///   }
    /// }
    /// ```
    /// </remarks>
    [HttpGet("Analysis/{id}")]
    [SwaggerOperation(
        OperationId = "GetAnalysisById",
        Summary = "Obtener análisis por ID",
        Description = "Recupera un análisis específico mediante su identificador único",
        Tags = new[] { "ANALYSIS" }
    )]
    [ProducesResponseType(typeof(AnalysisResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public IActionResult GetAnalysisById(string id)
    {
        throw new NotImplementedException("Este endpoint es manejado por el reverse proxy YARP");
    }

    /// <summary>
    /// Eliminar análisis por ID
    /// </summary>
    /// <remarks>
    /// **🔹 CONSUMO A TRAVÉS DEL GATEWAY /api/v1/translate:**
    /// ```json
    /// {
    ///   "service": "analysis",
    ///   "method": "DELETE",
    ///   "path": "/api/Analysis/123",
    ///   "headers": {
    ///     "Authorization": "Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
    ///   }
    /// }
    /// ```
    /// </remarks>
    [HttpDelete("Analysis/{id}")]
    [SwaggerOperation(
        OperationId = "DeleteAnalysis",
        Summary = "Eliminar análisis",
        Description = "Elimina un análisis específico y todos sus datos asociados",
        Tags = new[] { "ANALYSIS" }
    )]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public IActionResult DeleteAnalysis(string id)
    {
        throw new NotImplementedException("Este endpoint es manejado por el reverse proxy YARP");
    }

    /// <summary>
    /// Eliminar todos los análisis
    /// </summary>
    /// <remarks>
    /// **🔹 CONSUMO A TRAVÉS DEL GATEWAY /api/v1/translate:**
    /// ```json
    /// {
    ///   "service": "analysis",
    ///   "method": "DELETE",
    ///   "path": "/api/Analysis/all",
    ///   "headers": {
    ///     "Authorization": "Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
    ///   }
    /// }
    /// ```
    /// </remarks>
    [HttpDelete("Analysis/all")]
    [SwaggerOperation(
        OperationId = "DeleteAllAnalysis",
        Summary = "Eliminar todos los análisis",
        Description = "Elimina todos los análisis del sistema (usar con precaución)",
        Tags = new[] { "ANALYSIS" }
    )]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public IActionResult DeleteAllAnalysis()
    {
        throw new NotImplementedException("Este endpoint es manejado por el reverse proxy YARP");
    }

    // ============================================================================
    // SECCIÓN: ERRORES (6 endpoints)
    // ============================================================================

    /// <summary>
    /// Obtener todos los errores de accesibilidad
    /// </summary>
    /// <remarks>
    /// **🔹 CONSUMO A TRAVÉS DEL GATEWAY /api/v1/translate:**
    /// ```json
    /// {
    ///   "service": "analysis",
    ///   "method": "GET",
    ///   "path": "/api/Error",
    ///   "headers": {
    ///     "Authorization": "Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
    ///   }
    /// }
    /// ```
    /// </remarks>
    [HttpGet("Error")]
    [SwaggerOperation(
        OperationId = "GetAllErrors",
        Summary = "Obtener todos los errores",
        Description = "Recupera la lista completa de errores de accesibilidad detectados",
        Tags = new[] { "ERRORS" }
    )]
    [ProducesResponseType(typeof(IEnumerable<ErrorDetailResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public IActionResult GetAllErrors()
    {
        throw new NotImplementedException("Este endpoint es manejado por el reverse proxy YARP");
    }

    /// <summary>
    /// Crear un nuevo error de accesibilidad
    /// </summary>
    /// <remarks>
    /// **🔹 CONSUMO A TRAVÉS DEL GATEWAY /api/v1/translate:**
    /// ```json
    /// {
    ///   "service": "analysis",
    ///   "method": "POST",
    ///   "path": "/api/Error",
    ///   "headers": {
    ///     "Content-Type": "application/json",
    ///     "Authorization": "Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
    ///   },
    ///   "body": "{\"resultId\":123,\"errorType\":\"contrast\",\"severity\":\"critical\",\"wcagLevel\":\"AA\"}"
    /// }
    /// ```
    /// </remarks>
    [HttpPost("Error")]
    [SwaggerOperation(
        OperationId = "CreateError",
        Summary = "Crear nuevo error",
        Description = "Registra un nuevo error de accesibilidad detectado",
        Tags = new[] { "ERRORS" }
    )]
    [ProducesResponseType(typeof(ErrorDetailResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public IActionResult CreateError([FromBody] CreateErrorRequest request)
    {
        throw new NotImplementedException("Este endpoint es manejado por el reverse proxy YARP");
    }

    /// <summary>
    /// Obtener error específico por ID
    /// </summary>
    /// <remarks>
    /// **🔹 CONSUMO A TRAVÉS DEL GATEWAY /api/v1/translate:**
    /// ```json
    /// {
    ///   "service": "analysis",
    ///   "method": "GET",
    ///   "path": "/api/Error/456",
    ///   "headers": {
    ///     "Authorization": "Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
    ///   }
    /// }
    /// ```
    /// </remarks>
    [HttpGet("Error/{id}")]
    [SwaggerOperation(
        OperationId = "GetErrorById",
        Summary = "Obtener error por ID",
        Description = "Recupera un error específico mediante su identificador único",
        Tags = new[] { "ERRORS" }
    )]
    [ProducesResponseType(typeof(ErrorDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public IActionResult GetErrorById(string id)
    {
        throw new NotImplementedException("Este endpoint es manejado por el reverse proxy YARP");
    }

    /// <summary>
    /// Eliminar error por ID
    /// </summary>
    /// <remarks>
    /// **🔹 CONSUMO A TRAVÉS DEL GATEWAY /api/v1/translate:**
    /// ```json
    /// {
    ///   "service": "analysis",
    ///   "method": "DELETE",
    ///   "path": "/api/Error/456",
    ///   "headers": {
    ///     "Authorization": "Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
    ///   }
    /// }
    /// ```
    /// </remarks>
    [HttpDelete("Error/{id}")]
    [SwaggerOperation(
        OperationId = "DeleteError",
        Summary = "Eliminar error",
        Description = "Elimina un error específico del sistema",
        Tags = new[] { "ERRORS" }
    )]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public IActionResult DeleteError(string id)
    {
        throw new NotImplementedException("Este endpoint es manejado por el reverse proxy YARP");
    }

    /// <summary>
    /// Obtener errores por ID de resultado
    /// </summary>
    /// <remarks>
    /// **🔹 CONSUMO A TRAVÉS DEL GATEWAY /api/v1/translate:**
    /// ```json
    /// {
    ///   "service": "analysis",
    ///   "method": "GET",
    ///   "path": "/api/Error/by-result",
    ///   "query": {
    ///     "resultId": "789"
    ///   },
    ///   "headers": {
    ///     "Authorization": "Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
    ///   }
    /// }
    /// ```
    /// </remarks>
    [HttpGet("Error/by-result")]
    [SwaggerOperation(
        OperationId = "GetErrorsByResult",
        Summary = "Obtener errores por resultado",
        Description = "Recupera todos los errores asociados a un resultado específico",
        Tags = new[] { "ERRORS" }
    )]
    [ProducesResponseType(typeof(IEnumerable<ErrorDetailResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public IActionResult GetErrorsByResult([FromQuery] string resultId)
    {
        throw new NotImplementedException("Este endpoint es manejado por el reverse proxy YARP");
    }

    /// <summary>
    /// Eliminar todos los errores
    /// </summary>
    /// <remarks>
    /// **🔹 CONSUMO A TRAVÉS DEL GATEWAY /api/v1/translate:**
    /// ```json
    /// {
    ///   "service": "analysis",
    ///   "method": "DELETE",
    ///   "path": "/api/Error/all",
    ///   "headers": {
    ///     "Authorization": "Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
    ///   }
    /// }
    /// ```
    /// </remarks>
    [HttpDelete("Error/all")]
    [SwaggerOperation(
        OperationId = "DeleteAllErrors",
        Summary = "Eliminar todos los errores",
        Description = "Elimina todos los errores del sistema (usar con precaución)",
        Tags = new[] { "ERRORS" }
    )]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public IActionResult DeleteAllErrors()
    {
        throw new NotImplementedException("Este endpoint es manejado por el reverse proxy YARP");
    }

    // ============================================================================
    // SECCIÓN: RESULTADOS (7 endpoints)
    // ============================================================================

    /// <summary>
    /// Obtener todos los resultados
    /// </summary>
    /// <remarks>
    /// **🔹 CONSUMO A TRAVÉS DEL GATEWAY /api/v1/translate:**
    /// ```json
    /// {
    ///   "service": "analysis",
    ///   "method": "GET",
    ///   "path": "/api/Result",
    ///   "headers": {
    ///     "Authorization": "Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
    ///   }
    /// }
    /// ```
    /// </remarks>
    [HttpGet("Result")]
    [SwaggerOperation(
        OperationId = "GetAllResults",
        Summary = "Obtener todos los resultados",
        Description = "Recupera la lista completa de resultados de análisis",
        Tags = new[] { "RESULTS" }
    )]
    [ProducesResponseType(typeof(IEnumerable<ResultResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public IActionResult GetAllResults()
    {
        throw new NotImplementedException("Este endpoint es manejado por el reverse proxy YARP");
    }

    /// <summary>
    /// Crear un nuevo resultado
    /// </summary>
    /// <remarks>
    /// **🔹 CONSUMO A TRAVÉS DEL GATEWAY /api/v1/translate:**
    /// ```json
    /// {
    ///   "service": "analysis",
    ///   "method": "POST",
    ///   "path": "/api/Result",
    ///   "headers": {
    ///     "Content-Type": "application/json",
    ///     "Authorization": "Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
    ///   },
    ///   "body": "{\"analysisId\":123,\"wcagLevel\":\"AA\",\"severity\":\"serious\",\"passed\":false}"
    /// }
    /// ```
    /// </remarks>
    [HttpPost("Result")]
    [SwaggerOperation(
        OperationId = "CreateResult",
        Summary = "Crear nuevo resultado",
        Description = "Registra un nuevo resultado de análisis de accesibilidad",
        Tags = new[] { "RESULTS" }
    )]
    [ProducesResponseType(typeof(ResultResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public IActionResult CreateResult([FromBody] CreateResultRequest request)
    {
        throw new NotImplementedException("Este endpoint es manejado por el reverse proxy YARP");
    }

    /// <summary>
    /// Obtener resultados por nivel de conformidad
    /// </summary>
    /// <remarks>
    /// **🔹 CONSUMO A TRAVÉS DEL GATEWAY /api/v1/translate:**
    /// ```json
    /// {
    ///   "service": "analysis",
    ///   "method": "GET",
    ///   "path": "/api/Result/by-level",
    ///   "query": {
    ///     "level": "AA"
    ///   },
    ///   "headers": {
    ///     "Authorization": "Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
    ///   }
    /// }
    /// ```
    /// </remarks>
    [HttpGet("Result/by-level")]
    [SwaggerOperation(
        OperationId = "GetResultsByLevel",
        Summary = "Obtener resultados por nivel",
        Description = "Recupera resultados filtrados por nivel de conformidad WCAG (A, AA, AAA)",
        Tags = new[] { "RESULTS" }
    )]
    [ProducesResponseType(typeof(IEnumerable<ResultResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public IActionResult GetResultsByLevel([FromQuery] string level)
    {
        throw new NotImplementedException("Este endpoint es manejado por el reverse proxy YARP");
    }

    /// <summary>
    /// Obtener resultados por severidad
    /// </summary>
    /// <remarks>
    /// **🔹 CONSUMO A TRAVÉS DEL GATEWAY /api/v1/translate:**
    /// ```json
    /// {
    ///   "service": "analysis",
    ///   "method": "GET",
    ///   "path": "/api/Result/by-severity",
    ///   "query": {
    ///     "severity": "critical"
    ///   },
    ///   "headers": {
    ///     "Authorization": "Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
    ///   }
    /// }
    /// ```
    /// </remarks>
    [HttpGet("Result/by-severity")]
    [SwaggerOperation(
        OperationId = "GetResultsBySeverity",
        Summary = "Obtener resultados por severidad",
        Description = "Recupera resultados filtrados por severidad (Critical, Serious, Moderate, Minor)",
        Tags = new[] { "RESULTS" }
    )]
    [ProducesResponseType(typeof(IEnumerable<ResultResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public IActionResult GetResultsBySeverity([FromQuery] string severity)
    {
        throw new NotImplementedException("Este endpoint es manejado por el reverse proxy YARP");
    }

    /// <summary>
    /// Obtener resultados por análisis
    /// </summary>
    /// <remarks>
    /// **🔹 CONSUMO A TRAVÉS DEL GATEWAY /api/v1/translate:**
    /// ```json
    /// {
    ///   "service": "analysis",
    ///   "method": "GET",
    ///   "path": "/api/Result/by-analysis",
    ///   "query": {
    ///     "analysisId": "123"
    ///   },
    ///   "headers": {
    ///     "Authorization": "Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
    ///   }
    /// }
    /// ```
    /// </remarks>
    [HttpGet("Result/by-analysis")]
    [SwaggerOperation(
        OperationId = "GetResultsByAnalysis",
        Summary = "Obtener resultados por análisis",
        Description = "Recupera todos los resultados asociados a un análisis específico",
        Tags = new[] { "RESULTS" }
    )]
    [ProducesResponseType(typeof(IEnumerable<ResultResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public IActionResult GetResultsByAnalysis([FromQuery] string analysisId)
    {
        throw new NotImplementedException("Este endpoint es manejado por el reverse proxy YARP");
    }

    /// <summary>
    /// Obtener resultado específico por ID
    /// </summary>
    /// <remarks>
    /// **🔹 CONSUMO A TRAVÉS DEL GATEWAY /api/v1/translate:**
    /// ```json
    /// {
    ///   "service": "analysis",
    ///   "method": "GET",
    ///   "path": "/api/Result/789",
    ///   "headers": {
    ///     "Authorization": "Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
    ///   }
    /// }
    /// ```
    /// </remarks>
    [HttpGet("Result/{id}")]
    [SwaggerOperation(
        OperationId = "GetResultById",
        Summary = "Obtener resultado por ID",
        Description = "Recupera un resultado específico mediante su identificador único",
        Tags = new[] { "RESULTS" }
    )]
    [ProducesResponseType(typeof(ResultResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public IActionResult GetResultById(string id)
    {
        throw new NotImplementedException("Este endpoint es manejado por el reverse proxy YARP");
    }

    /// <summary>
    /// Eliminar resultado por ID
    /// </summary>
    /// <remarks>
    /// **🔹 CONSUMO A TRAVÉS DEL GATEWAY /api/v1/translate:**
    /// ```json
    /// {
    ///   "service": "analysis",
    ///   "method": "DELETE",
    ///   "path": "/api/Result/789",
    ///   "headers": {
    ///     "Authorization": "Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
    ///   }
    /// }
    /// ```
    /// </remarks>
    [HttpDelete("Result/{id}")]
    [SwaggerOperation(
        OperationId = "DeleteResult",
        Summary = "Eliminar resultado",
        Description = "Elimina un resultado específico del sistema",
        Tags = new[] { "RESULTS" }
    )]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public IActionResult DeleteResult(string id)
    {
        throw new NotImplementedException("Este endpoint es manejado por el reverse proxy YARP");
    }

    /// <summary>
    /// Eliminar todos los resultados
    /// </summary>
    /// <remarks>
    /// **🔹 CONSUMO A TRAVÉS DEL GATEWAY /api/v1/translate:**
    /// ```json
    /// {
    ///   "service": "analysis",
    ///   "method": "DELETE",
    ///   "path": "/api/Result/all",
    ///   "headers": {
    ///     "Authorization": "Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
    ///   }
    /// }
    /// ```
    /// </remarks>
    [HttpDelete("Result/all")]
    [SwaggerOperation(
        OperationId = "DeleteAllResults",
        Summary = "Eliminar todos los resultados",
        Description = "Elimina todos los resultados del sistema (usar con precaución)",
        Tags = new[] { "RESULTS" }
    )]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public IActionResult DeleteAllResults()
    {
        throw new NotImplementedException("Este endpoint es manejado por el reverse proxy YARP");
    }

    // ============================================================================
    // SECCIÓN: OBSERVABILIDAD Y MONITOREO (4 endpoints)
    // ============================================================================

    /// <summary>
    /// Health check completo del microservicio Analysis
    /// </summary>
    /// <remarks>
    /// **🔹 CONSUMO A TRAVÉS DEL GATEWAY /api/v1/translate:**
    /// ```json
    /// {
    ///   "service": "analysis",
    ///   "method": "GET",
    ///   "path": "/health"
    /// }
    /// ```
    /// </remarks>
    [HttpGet("/analysis-service/health")]
    [ApiExplorerSettings(GroupName = "analysis")]
    [SwaggerOperation(
        OperationId = "GetAnalysisHealth",
        Summary = "Health check completo del microservicio Analysis",
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
    /// **🔹 CONSUMO A TRAVÉS DEL GATEWAY /api/v1/translate:**
    /// ```json
    /// {
    ///   "service": "analysis",
    ///   "method": "GET",
    ///   "path": "/health/live"
    /// }
    /// ```
    /// </remarks>
    [HttpGet("/analysis-service/health/live")]
    [ApiExplorerSettings(GroupName = "analysis")]
    [SwaggerOperation(
        OperationId = "GetAnalysisLiveness",
        Summary = "Liveness probe del microservicio Analysis",
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
    /// **🔹 CONSUMO A TRAVÉS DEL GATEWAY /api/v1/translate:**
    /// ```json
    /// {
    ///   "service": "analysis",
    ///   "method": "GET",
    ///   "path": "/health/ready"
    /// }
    /// ```
    /// </remarks>
    [HttpGet("/analysis-service/health/ready")]
    [ApiExplorerSettings(GroupName = "analysis")]
    [SwaggerOperation(
        OperationId = "GetAnalysisReadiness",
        Summary = "Readiness probe del microservicio Analysis",
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
    /// Métricas de Prometheus del microservicio Analysis
    /// </summary>
    /// <remarks>
    /// **🔹 CONSUMO A TRAVÉS DEL GATEWAY /api/v1/translate:**
    /// ```json
    /// {
    ///   "service": "analysis",
    ///   "method": "GET",
    ///   "path": "/metrics"
    /// }
    /// ```
    /// </remarks>
    [HttpGet("/analysis-service/metrics")]
    [ApiExplorerSettings(GroupName = "analysis")]
    [SwaggerOperation(
        OperationId = "GetAnalysisMetrics",
        Summary = "Métricas de Prometheus del microservicio Analysis",
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
