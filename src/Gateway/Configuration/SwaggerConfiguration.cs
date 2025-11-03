using Microsoft.OpenApi;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using Swashbuckle.AspNetCore.Filters;
using System.Reflection;

namespace Gateway.Configuration;

/// <summary>
/// Configuración centralizada de Swagger para el Gateway.
/// Documenta todos los microservicios a través de un único punto de entrada.
/// </summary>
public static class SwaggerConfiguration
{
    /// <summary>
    /// Agrega la configuración de Swagger con soporte para múltiples documentos OpenAPI.
    /// </summary>
    public static IServiceCollection AddSwaggerDocumentation(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();

        services.AddSwaggerGen(options =>
        {
            // ============================================================
            // VERSIÓN DE OPENAPI (FORZAR 3.0.1 PARA COMPATIBILIDAD)
            // ============================================================
            options.DocumentFilter<OpenApiVersionDocumentFilter>();

            // ============================================================
            // HABILITAR EJEMPLOS (IExamplesProvider)
            // ============================================================
            options.EnableAnnotations();
            options.ExampleFilters();

            // ============================================================
            // HABILITAR COMENTARIOS XML
            // ============================================================
            var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
            {
                options.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
            }

            // ============================================================
            // DOCUMENTO 1: USERS API
            // ============================================================
            options.SwaggerDoc("users", new OpenApiInfo
            {
                Title = "Users API (via Gateway)",
                Version = "v1.0",
                Description = @"
**API de Gestión de Usuarios** - Documentación centralizada a través del Gateway

Este documento describe todos los endpoints del microservicio Users accesibles vía Gateway.

### 🔐 Autenticación
Todos los endpoints (excepto login/register) requieren:
1. **Gateway-Secret**: Header `X-Gateway-Secret` con el secreto compartido
2. **JWT Token**: Header `Authorization: Bearer {token}`

### 📡 Base URL
- **Gateway**: `http://localhost:8101`
- **Microservicio directo**: `http://localhost:8081` (no recomendado)

### 🎯 Categorías de Endpoints
- **AUTH**: Registro, Login, Logout (3 endpoints)
- **USERS**: CRUD de usuarios (4 endpoints)
- **PREFERENCES**: Gestión de preferencias (2 endpoints)
- **SESSIONS**: Control de sesiones (1 endpoint)

### 📝 Notas Importantes
- Las rutas se invocan a través del Gateway en `/api/users/*`
- El Gateway valida el secreto y reenvía requests al microservicio
- Los JWT tokens son generados por el microservicio Users
",
                Contact = new OpenApiContact
                {
                    Name = "Accessibility Platform Team",
                    Email = "support@accessibility-platform.com"
                }
            });

            // ============================================================
            // DOCUMENTO 2: ANALYSIS API
            // ============================================================
            options.SwaggerDoc("analysis", new OpenApiInfo
            {
                Title = "Analysis API (via Gateway)",
                Version = "v1.0",
                Description = @"
**API de Análisis de Accesibilidad** - Documentación centralizada a través del Gateway

Este documento describe todos los endpoints del microservicio Analysis accesibles vía Gateway.

### 🔐 Autenticación
Requiere:
1. **Gateway-Secret**: Header `X-Gateway-Secret`
2. **JWT Token**: Header `Authorization: Bearer {token}`

### 📡 Base URL
- **Gateway**: `http://localhost:8101`
- **Microservicio directo**: `http://localhost:8082` (no recomendado)

### 🎯 Funcionalidades
- Análisis WCAG de sitios web
- Validación de accesibilidad
- Reportes de conformidad
- Estadísticas de análisis
",
                Contact = new OpenApiContact
                {
                    Name = "Accessibility Platform Team",
                    Email = "support@accessibility-platform.com"
                }
            });

            // ============================================================
            // DOCUMENTO 3: REPORTS API
            // ============================================================
            options.SwaggerDoc("reports", new OpenApiInfo
            {
                Title = "Reports API (via Gateway)",
                Version = "v1.0",
                Description = @"
**API de Generación de Reportes** - Documentación centralizada a través del Gateway

Este documento describe todos los endpoints del microservicio Reports accesibles vía Gateway.

### 🔐 Autenticación
Requiere:
1. **Gateway-Secret**: Header `X-Gateway-Secret`
2. **JWT Token**: Header `Authorization: Bearer {token}`

### 📡 Base URL
- **Gateway**: `http://localhost:8101`
- **Microservicio directo**: `http://localhost:8083` (no recomendado)

### 🎯 Funcionalidades
- Generación de reportes PDF
- Exportación de datos
- Plantillas personalizadas
- Historial de reportes
",
                Contact = new OpenApiContact
                {
                    Name = "Accessibility Platform Team",
                    Email = "support@accessibility-platform.com"
                }
            });

            // ============================================================
            // DOCUMENTO 4: MIDDLEWARE API
            // ============================================================
            options.SwaggerDoc("middleware", new OpenApiInfo
            {
                Title = "Middleware API (via Gateway)",
                Version = "v1.0",
                Description = @"
**API Middleware Node.js** - Documentación centralizada a través del Gateway

Este documento describe todos los endpoints del middleware Node.js accesibles vía Gateway.

### 🔐 Autenticación
Requiere:
1. **Gateway-Secret**: Header `X-Gateway-Secret`
2. **JWT Token**: Header `Authorization: Bearer {token}`

### 📡 Base URL
- **Gateway**: `http://localhost:8101`
- **Middleware directo**: `http://localhost:3001` (no recomendado)

### 🎯 Funcionalidades
- Orquestación de servicios
- Transformación de datos
- Caché inteligente
",
                Contact = new OpenApiContact
                {
                    Name = "Accessibility Platform Team",
                    Email = "support@accessibility-platform.com"
                }
            });

            // ============================================================
            // DOCUMENTO 5: GATEWAY API
            // ============================================================
            options.SwaggerDoc("gateway", new OpenApiInfo
            {
                Title = "Gateway API",
                Version = "v1.0",
                Description = @"
**API del Gateway** - Endpoints propios del Gateway

Este documento describe los endpoints nativos del Gateway (health checks, métricas, etc.).

### 📡 Base URL
- **Gateway**: `http://localhost:8101`

### 🎯 Funcionalidades
- Health checks
- Métricas de rendimiento
- Estado del sistema
",
                Contact = new OpenApiContact
                {
                    Name = "Accessibility Platform Team",
                    Email = "support@accessibility-platform.com"
                }
            });

            // ============================================================
            // SEGURIDAD: JWT Bearer Authentication
            // ============================================================
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "Bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = @"
**Autenticación JWT Bearer Token**

Para obtener un token:
1. Registra un usuario en `/api/users/auth/register`
2. Inicia sesión en `/api/users/auth/login`
3. Copia el token JWT recibido
4. Haz clic en 'Authorize' arriba
5. Ingresa: `Bearer {tu-token}`
6. Haz clic en 'Authorize' y 'Close'

**Ejemplo:**
```
Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

**Nota:** No incluyas las llaves `{}`, solo el token.
"
            });

            options.AddSecurityDefinition("GatewaySecret", new OpenApiSecurityScheme
            {
                Name = "X-Gateway-Secret",
                Type = SecuritySchemeType.ApiKey,
                In = ParameterLocation.Header,
                Description = @"
**Gateway Secret - Autenticación de Servicio a Servicio**

Este header es requerido para todas las peticiones al Gateway.

**Valor:** `VGhpc0lzQVNlY3JldEtleUZvckdhdGV3YXkyMDI0`

El Gateway valida este secreto antes de reenviar peticiones a los microservicios.

**⚠️ IMPORTANTE:** 
- Este secreto debe mantenerse confidencial
- Solo para comunicación Gateway <-> Microservicios
- En producción usar variables de entorno
"
            });

            // Aplicar seguridad globalmente
            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                },
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "GatewaySecret"
                        }
                    },
                    Array.Empty<string>()
                }
            });

            // Ordenar acciones por método HTTP
            options.OrderActionsBy(apiDesc =>
                $"{apiDesc.GroupName}_{apiDesc.RelativePath}_{apiDesc.HttpMethod}");

            // Personalizar IDs de operación (solo para Controllers, Minimal APIs no tienen 'action')
            options.CustomOperationIds(apiDesc =>
            {
                if (apiDesc.ActionDescriptor.RouteValues.TryGetValue("action", out var actionName) &&
                    apiDesc.ActionDescriptor.RouteValues.TryGetValue("controller", out var controllerName))
                {
                    return $"{controllerName}_{actionName}";
                }
                return apiDesc.RelativePath?.Replace("/", "_").TrimStart('_');
            });

            // ============================================================
            // CRITERIO DE INCLUSIÓN POR DOCUMENTO (CRÍTICO)
            // ============================================================
            // Este predicado determina qué endpoints se incluyen en cada documento Swagger
            options.DocInclusionPredicate((documentName, apiDescription) =>
            {
                // Obtener el GroupName del endpoint (asignado vía [ApiExplorerSettings] o .WithGroupName())
                var groupName = apiDescription.GroupName;

                // DEBUG: Logging para diagnóstico
                Console.WriteLine($"[DocInclusion] Doc:{documentName} | Path:{apiDescription.RelativePath} | GroupName:{groupName ?? "NULL"}");

                // Incluir el endpoint SI su GroupName coincide con el nombre del documento
                // Si no tiene GroupName, no incluir en ningún documento
                var shouldInclude = groupName != null && groupName.Equals(documentName, StringComparison.OrdinalIgnoreCase);

                if (shouldInclude)
                {
                    Console.WriteLine($"  ✅ INCLUIDO en documento '{documentName}'");
                }

                return shouldInclude;
            });

            // Filtro para documentar respuestas comunes
            options.OperationFilter<CommonResponsesOperationFilter>();
        });

        // ============================================================
        // REGISTRAR EJEMPLOS (IExamplesProvider)
        // ============================================================
        services.AddSwaggerExamplesFromAssemblyOf<Program>();

        return services;
    }

    /// <summary>
    /// Configura el middleware de Swagger UI con soporte para múltiples documentos.
    /// </summary>
    public static IApplicationBuilder UseSwaggerDocumentation(this IApplicationBuilder app)
    {
        // Configurar Swagger para usar OpenAPI 3.x
        app.UseSwagger(options =>
        {
            // NOTA: La propiedad OpenApiVersion fue removida en versiones recientes de Swashbuckle
            // El formato OpenAPI 3.0 se usa por defecto

            // Middleware personalizado para modificar el JSON antes de enviarlo
            options.PreSerializeFilters.Add((swaggerDoc, httpReq) =>
            {
                // Intentar forzar OpenAPI 3.0.1 para mejor compatibilidad con Swagger UI
                var specVersionProp = swaggerDoc.GetType().GetProperty("SpecVersion");
                if (specVersionProp != null && specVersionProp.CanWrite)
                {
                    try
                    {
                        var openApiSpecVersionType = Type.GetType("Microsoft.OpenApi.Models.OpenApiSpecVersion, Microsoft.OpenApi");
                        if (openApiSpecVersionType != null)
                        {
                            var version301Field = openApiSpecVersionType.GetField("OpenApi3_0");
                            if (version301Field != null)
                            {
                                var version301Value = version301Field.GetValue(null);
                                specVersionProp.SetValue(swaggerDoc, version301Value);
                            }
                        }
                    }
                    catch
                    {
                        // Si falla, continuar con la versión predeterminada
                    }
                }
            });
        });

        app.UseSwaggerUI(options =>
        {
            // Configurar un endpoint para cada documento
            options.SwaggerEndpoint("/swagger/users/swagger.json", "🔐 Users API");
            options.SwaggerEndpoint("/swagger/analysis/swagger.json", "📊 Analysis API");
            options.SwaggerEndpoint("/swagger/reports/swagger.json", "📄 Reports API");
            options.SwaggerEndpoint("/swagger/middleware/swagger.json", "⚙️ Middleware API");
            options.SwaggerEndpoint("/swagger/gateway/swagger.json", "🚪 Gateway API");

            // Configuración de UI
            options.RoutePrefix = "swagger"; // Accesible en /swagger
            options.DocumentTitle = "Accessibility Platform - API Gateway";
            options.DefaultModelsExpandDepth(2);
            options.DefaultModelRendering(Swashbuckle.AspNetCore.SwaggerUI.ModelRendering.Model);
            options.DisplayRequestDuration();
            options.EnableDeepLinking();
            options.EnableFilter();
            options.ShowExtensions();
            options.EnableValidator();

            // Configuración de visualización
            options.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.None);
            options.EnableTryItOutByDefault();
        });

        return app;
    }
}

/// <summary>
/// Filtro para agregar respuestas HTTP comunes a todas las operaciones.
/// </summary>
public class CommonResponsesOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        // Agregar respuesta 401 si no existe
        if (!operation.Responses.ContainsKey("401"))
        {
            operation.Responses.Add("401", new OpenApiResponse
            {
                Description = "No autorizado - Token JWT inválido o expirado"
            });
        }

        // Agregar respuesta 403 si no existe
        if (!operation.Responses.ContainsKey("403"))
        {
            operation.Responses.Add("403", new OpenApiResponse
            {
                Description = "Prohibido - Gateway Secret inválido o permisos insuficientes"
            });
        }

        // Agregar respuesta 500 si no existe
        if (!operation.Responses.ContainsKey("500"))
        {
            operation.Responses.Add("500", new OpenApiResponse
            {
                Description = "Error interno del servidor"
            });
        }
    }
}

/// <summary>
/// Filtro para forzar la versión de OpenAPI a 3.0.1 por compatibilidad con Swagger UI.
/// </summary>
public class OpenApiVersionDocumentFilter : IDocumentFilter
{
    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        // Forzar OpenAPI 3.0.1 para máxima compatibilidad con Swagger UI 3.x
        // Swagger UI puede tener problemas con OpenAPI 3.0.4
        var specVersionField = swaggerDoc.GetType().GetProperty("SpecVersion",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

        if (specVersionField != null)
        {
            // Intentar establecer la versión a 3.0.1
            var versionType = specVersionField.PropertyType;
            var parseMethod = versionType.GetMethod("Parse", new[] { typeof(string) });
            if (parseMethod != null)
            {
                try
                {
                    var version301 = parseMethod.Invoke(null, new object[] { "3.0.1" });
                    specVersionField.SetValue(swaggerDoc, version301);
                }
                catch
                {
                    // Si falla, continuar con la versión predeterminada
                }
            }
        }
    }
}
