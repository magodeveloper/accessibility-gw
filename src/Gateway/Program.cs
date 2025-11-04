using Polly;
using Serilog;
using Gateway;
using System.Net;
using Gateway.Models;
using Gateway.Services;
using Gateway.Middleware;
using System.Text.Json;
using Polly.Extensions.Http;
using System.Net.Http.Headers;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Yarp.ReverseProxy.Forwarder;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.RateLimiting;
using Swashbuckle.AspNetCore.Annotations;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using System.ComponentModel.DataAnnotations;

var builder = WebApplication.CreateBuilder(args);

// --- Logging (Serilog) ---
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("MachineName", Environment.MachineName)
    .WriteTo.Console()
    .WriteTo.File("./logs/gateway-.log", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 7)
    .CreateLogger();

builder.Host.UseSerilog();

// --- Configuraciones ---
builder.Services.Configure<GateOptions>(builder.Configuration.GetSection("Gate"));
builder.Services.Configure<HealthChecksOptions>(builder.Configuration.GetSection("HealthChecks"));
builder.Services.Configure<RedisOptions>(builder.Configuration.GetSection("Redis"));
builder.Services.AddOptions();

// --- Servicios principales ---
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Accessibility Platform API Gateway",
        Version = "v1.0.0",
        Description = """
        API Gateway completo para la plataforma de accesibilidad que incluye todos los endpoints 
        de los microservicios y el middleware de análisis.
        
        ## Arquitectura
        
        El gateway actúa como punto de entrada único para:
        - **accessibility-ms-users**: Gestión de usuarios y autenticación
        - **accessibility-ms-reports**: Generación y gestión de reportes
        - **accessibility-ms-analysis**: Análisis de accesibilidad 
        - **accessibility-mw**: Middleware de análisis con herramientas especializadas
        
        ## Autenticación
        
        La mayoría de endpoints requieren autenticación JWT. Incluye el token en el header:
        ```
        Authorization: Bearer <jwt-token>
        ```
        
        ## Rate Limiting
        
        - **General**: 100 req/min por IP
        - **Análisis**: 20 req/min por IP (endpoints computacionalmente intensivos)
        
        ## Caché
        
        Los endpoints GET pueden ser cacheados por el gateway. Usa el parámetro `useCache=false` para evitar caché.
        """,
        Contact = new OpenApiContact
        {
            Name = "Accessibility Team",
            Email = "accessibility@company.com"
        },
        License = new OpenApiLicense
        {
            Name = "MIT License"
        }
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
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
        }
    });

    // Incluir comentarios XML si existen
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }

    // Configurar tags para mejor organización  
    // c.EnableAnnotations(); // Comentado temporalmente
    c.DocInclusionPredicate((name, api) => true);
});

// --- Autenticación JWT ---
var jwtSection = builder.Configuration.GetSection("Jwt");
var authority = jwtSection["Authority"];
var audience = jwtSection["Audience"];

if (!string.IsNullOrWhiteSpace(authority))
{
    builder.Services.AddAuthentication("Bearer")
        .AddJwtBearer("Bearer", opt =>
        {
            opt.Authority = authority;
            opt.Audience = audience;
            opt.RequireHttpsMetadata = builder.Environment.IsProduction();
            opt.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
            {
                ValidateIssuer = jwtSection.GetValue<bool>("ValidateIssuer", true),
                ValidateAudience = jwtSection.GetValue<bool>("ValidateAudience", true),
                ValidateLifetime = jwtSection.GetValue<bool>("ValidateLifetime", true),
                ValidateIssuerSigningKey = jwtSection.GetValue<bool>("ValidateIssuerSigningKey", true),
                ClockSkew = TimeSpan.FromMinutes(1)
            };
        });

    builder.Services.AddAuthorization();
}

// --- Validación de Input Avanzada ---
builder.Services.AddSingleton<IInputSanitizationService, InputSanitizationService>();

// Configurar validación automática de modelos
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();

        var errors = context.ModelState
            .Where(x => x.Value?.Errors.Count > 0)
            .SelectMany(x => x.Value!.Errors.Select(e => new { Field = x.Key, Error = e.ErrorMessage }))
            .ToList();

        logger.LogWarning("Model validation failed for {Path}. Errors: {@Errors}",
            context.HttpContext.Request.Path, errors);

        var problemDetails = new ValidationProblemDetails(context.ModelState)
        {
            Title = "One or more validation errors occurred.",
            Status = StatusCodes.Status400BadRequest,
            Detail = "Please check the errors and try again.",
            Instance = context.HttpContext.Request.Path
        };

        return new BadRequestObjectResult(problemDetails);
    };
});

// --- Rate Limiting ---
builder.Services.AddRateLimiter(opt =>
{
    opt.AddTokenBucketLimiter("global", o =>
    {
        o.TokenLimit = 100;
        o.QueueProcessingOrder = System.Threading.RateLimiting.QueueProcessingOrder.OldestFirst;
        o.QueueLimit = 200;
        o.ReplenishmentPeriod = TimeSpan.FromSeconds(1);
        o.TokensPerPeriod = 50;
        o.AutoReplenishment = true;
    });

    opt.AddTokenBucketLimiter("public", o =>
    {
        o.TokenLimit = 200;
        o.QueueProcessingOrder = System.Threading.RateLimiting.QueueProcessingOrder.OldestFirst;
        o.QueueLimit = 100;
        o.ReplenishmentPeriod = TimeSpan.FromSeconds(1);
        o.TokensPerPeriod = 100;
        o.AutoReplenishment = true;
    });
});

// --- Caché distribuido ---
var redisConnectionString = builder.Configuration.GetSection("Redis")["ConnectionString"];
if (!string.IsNullOrEmpty(redisConnectionString))
{
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = redisConnectionString;
        options.InstanceName = "AccessibilityGateway";
    });
}
else
{
    builder.Services.AddMemoryCache();
    builder.Services.AddSingleton<Microsoft.Extensions.Caching.Distributed.IDistributedCache,
    Microsoft.Extensions.Caching.Distributed.MemoryDistributedCache>();
}

// --- Output Cache ---
builder.Services.AddOutputCache(o =>
{
    o.AddBasePolicy(b => b.Expire(TimeSpan.FromSeconds(10)));
    o.AddPolicy("LongCache", b => b.Expire(TimeSpan.FromMinutes(5)));
});

// --- HttpClient con políticas de resiliencia ---
// Políticas de reintentos y circuit breaker usando Polly para HttpClient tradicional

builder.Services.AddHttpClient();
builder.Services.AddHttpForwarder();
builder.Services.AddHttpClient("DefaultClient", (sp, client) =>
{
    var gateOptions = sp.GetRequiredService<IOptions<GateOptions>>().Value;
    client.Timeout = TimeSpan.FromSeconds(gateOptions.DefaultTimeoutSeconds);
})
.ConfigurePrimaryHttpMessageHandler(_ => new SocketsHttpHandler())
.AddPolicyHandler(GetRetryPolicy())
.AddPolicyHandler(GetCircuitBreakerPolicy());

// --- Servicios personalizados ---
builder.Services.AddSingleton<HttpMessageInvoker>(provider =>
{
    var gateOptions = provider.GetRequiredService<IOptions<GateOptions>>().Value;
    var handler = new SocketsHttpHandler
    {
        ConnectTimeout = TimeSpan.FromSeconds(gateOptions.DefaultTimeoutSeconds)
    };
    return new HttpMessageInvoker(handler);
});
builder.Services.AddSingleton<IResiliencePolicyService, ResiliencePolicyService>();
builder.Services.AddSingleton<RequestTranslator>();
builder.Services.AddSingleton<ICacheService, CacheService>();
builder.Services.AddSingleton<IMetricsService, MetricsService>();
builder.Services.AddSingleton<ServiceHealthCheckFactory>();

// --- Health Checks ---
var healthChecksBuilder = builder.Services.AddHealthChecks();

// Health check básico
healthChecksBuilder.AddCheck("self", () => HealthCheckResult.Healthy(), new[] { "live" });

// Health checks para servicios
var gateConfig = builder.Configuration.GetSection("Gate").Get<GateOptions>();
if (gateConfig?.Services != null)
{
    foreach (var service in gateConfig.Services)
    {
        var serviceName = service.Key;
        var serviceUrl = service.Value; // El Value es directamente el string de la URL

        // Registrar el health check usando AddTypeActivatedCheck con el constructor correcto
        healthChecksBuilder.AddTypeActivatedCheck<ServiceHealthCheck>(
            $"service-{serviceName}",
            failureStatus: null,
            tags: new[] { "ready", serviceName },
            args: new object[] { serviceName, serviceUrl });
    }
}// Health check para Redis si está configurado
if (!string.IsNullOrEmpty(redisConnectionString))
{
    healthChecksBuilder.AddRedis(redisConnectionString, name: "redis", tags: new[] { "ready" });
}

// UI de Health Checks - Configuración mejorada para evitar concurrencia
builder.Services.AddHealthChecksUI(opt =>
{
    opt.SetEvaluationTimeInSeconds(60); // Aumentar intervalo para reducir concurrencia
    opt.MaximumHistoryEntriesPerEndpoint(25); // Reducir historial para menor carga
    opt.SetApiMaxActiveRequests(1); // Una sola request activa para evitar "Sequence contains more than one element"
    opt.AddHealthCheckEndpoint("Gateway API", "/health");

    // Configuración adicional para mejorar estabilidad
    opt.SetMinimumSecondsBetweenFailureNotifications(300); // 5 minutos entre notificaciones
})
.AddInMemoryStorage();

// --- CORS ---
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowedOrigins", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// --- Security Headers Middleware ---
app.Use(async (context, next) =>
{
    // Security headers esenciales para protección
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";

    // HSTS solo en producción con HTTPS
    if (app.Environment.IsProduction())
    {
        context.Response.Headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains; preload";
    }

    // Content Security Policy adaptado para API Gateway con Swagger
    var cspPolicy = app.Environment.IsDevelopment()
        ? "default-src 'self'; script-src 'self' 'unsafe-inline' 'unsafe-eval'; style-src 'self' 'unsafe-inline'; img-src 'self' data:; font-src 'self' data:"
        : "default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'; img-src 'self' data:; font-src 'self'";

    context.Response.Headers["Content-Security-Policy"] = cspPolicy;

    // Remover headers que revelan información del servidor
    context.Response.Headers.Remove("Server");
    context.Response.Headers.Remove("X-Powered-By");

    await next();
});

// --- Middleware pipeline ---
app.UseSerilogRequestLogging(options =>
{
    options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
        diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
        diagnosticContext.Set("UserAgent", httpContext.Request.Headers["User-Agent"].FirstOrDefault());
        diagnosticContext.Set("CorrelationId", httpContext.Request.Headers["X-Correlation-ID"].FirstOrDefault());
    };
});

// Habilitar Swagger tanto en Development como en Production para facilitar testing
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Accessibility Gateway API v1");
    c.RoutePrefix = "swagger"; // Swagger UI en /swagger
    c.DisplayRequestDuration();
    c.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.List);
    c.EnableFilter();
    c.EnableDeepLinking();

    // En producción, mostrar advertencia sobre el uso de Swagger
    if (app.Environment.IsProduction())
    {
        c.DocumentTitle = "Accessibility Gateway API (PRODUCTION) - Use with caution";
    }
});

// Redirección específica para mejorar UX de Swagger
app.MapGet("/swagger", () => Results.Redirect("/swagger/index.html", true));

// Comentado para desarrollo local sin HTTPS
// app.UseHsts();
// app.UseHttpsRedirection();
app.UseCors("AllowedOrigins");

if (!string.IsNullOrWhiteSpace(authority))
{
    app.UseAuthentication();
}

app.UseMiddleware<RouteAuthorizationMiddleware>();

if (!string.IsNullOrWhiteSpace(authority))
{
    app.UseAuthorization();
    app.UseMiddleware<JwtClaimsTransformMiddleware>();
}

app.UseRateLimiter();
app.UseOutputCache();

// Middleware para enrutamiento automático de API
app.Use(async (context, next) =>
{
    var path = context.Request.Path.Value;
    var method = context.Request.Method;

    Console.WriteLine($"=== MIDDLEWARE DEBUG === {method} {path}");

    // Solo manejar rutas que empiecen con /api/ y no sean rutas internas del gateway
    if (path?.StartsWith("/api/") == true &&
        !path.StartsWith("/api/v1/translate") &&
        !path.StartsWith("/api/v1/services/"))
    {
        Console.WriteLine($"=== INTERCEPTED API REQUEST === {method} {path}");

        var translator = context.RequestServices.GetService<RequestTranslator>();
        if (translator != null)
        {
            Console.WriteLine("=== TRANSLATOR SERVICE OBTAINED ===");

            // Obtener configuración de rutas desde appsettings.json
            var gateOptions = context.RequestServices.GetService<IOptions<GateOptions>>();
            string? targetService = null;

            if (gateOptions?.Value?.AllowedRoutes != null)
            {
                // Buscar la ruta que coincida con el path y método actual
                var matchedRoute = gateOptions.Value.AllowedRoutes.FirstOrDefault(route =>
                    path.StartsWith(route.PathPrefix, StringComparison.OrdinalIgnoreCase) &&
                    route.Methods.Any(m => string.Equals(m, method, StringComparison.OrdinalIgnoreCase)));

                if (matchedRoute != null)
                {
                    targetService = matchedRoute.Service;
                    Console.WriteLine($"=== ROUTE MATCHED === PathPrefix: {matchedRoute.PathPrefix}, Service: {matchedRoute.Service}");
                }
            }

            // Fallback a mapeo hardcodeado si no se encuentra en configuración
            if (targetService == null)
            {
                if (path.StartsWith("/api/v1/users") || path.StartsWith("/api/auth", StringComparison.OrdinalIgnoreCase))
                    targetService = "users";
                else if (path.StartsWith("/api/Report"))
                    targetService = "reports";
                else if (path.StartsWith("/api/Analysis"))
                    targetService = "analysis";
                else if (path.StartsWith("/api/analyze"))
                    targetService = "middleware";
            }

            Console.WriteLine($"=== MAPPED TO SERVICE === {targetService ?? "null"}");

            if (targetService != null)
            {
                var translateRequest = new TranslateRequest
                {
                    Service = targetService,
                    Method = method,
                    Path = path,
                    Query = context.Request.Query.ToDictionary(q => q.Key, q => q.Value.ToString()),
                    Headers = context.Request.Headers
                        .Where(h => !h.Key.StartsWith(":"))
                        .ToDictionary(h => h.Key, h => h.Value.ToString())
                };

                Console.WriteLine($"=== TRANSLATE REQUEST CREATED === {translateRequest.Service}:{translateRequest.Method}:{translateRequest.Path}");

                if (translator.IsAllowed(translateRequest))
                {
                    Console.WriteLine("=== REQUEST ALLOWED BY ACL ===");
                    try
                    {
                        // Usar ForwardAsync en lugar de ProcessRequestAsync
                        // ForwardAsync maneja tanto errores como respuestas exitosas
                        Console.WriteLine("=== CALLING FORWARDASYNC ===");
                        await translator.ForwardAsync(context, translateRequest, context.RequestAborted);
                        Console.WriteLine("=== FORWARDASYNC COMPLETED ===");
                        return;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"=== EXCEPTION IN FORWARDASYNC === {ex.Message}");
                        context.Response.StatusCode = 500;
                        context.Response.ContentType = "application/json";
                        await context.Response.WriteAsync($"{{\"error\":\"Gateway error: {ex.Message}\"}}", context.RequestAborted);
                        return;
                    }
                }
                else
                {
                    Console.WriteLine($"=== REQUEST NOT ALLOWED BY ACL === {targetService}:{method}:{path}");
                }
            }
        }
        else
        {
            Console.WriteLine("=== TRANSLATOR SERVICE IS NULL ===");
        }
    }
    else
    {
        Console.WriteLine($"=== REQUEST BYPASSED === {method} {path}");
    }

    Console.WriteLine("=== CALLING NEXT MIDDLEWARE ===");
    await next();
});

// --- Endpoints de la API ---

app.MapPost("/api/v1/translate", async (
    ValidatedTranslateRequest validatedReq,
    RequestTranslator translator,
    IOptions<GateOptions> opts,
    IInputSanitizationService sanitizer,
    HttpContext http) =>
{
    try
    {
        Console.WriteLine($"=== TRANSLATE ENDPOINT DEBUG ===");
        Console.WriteLine($"Service: {validatedReq.Service}");
        Console.WriteLine($"Method: {validatedReq.Method}");
        Console.WriteLine($"Path: {validatedReq.Path}");
        Console.WriteLine($"Body: {validatedReq.Body}");
        Console.WriteLine($"Body Length: {validatedReq.Body?.Length ?? 0}");
        Console.WriteLine($"Content-Length: {http.Request.ContentLength}");

        // 1. Validación de tamaño de payload
        var max = opts.Value.MaxPayloadSizeBytes;
        if (http.Request.ContentLength is long len && len > max)
            return Results.Problem("Payload too large", statusCode: StatusCodes.Status413PayloadTooLarge);

        // 2. Sanitización avanzada de inputs
        var sanitizedPath = sanitizer.SanitizeApiPath(validatedReq.Path);
        var sanitizedQuery = sanitizer.SanitizeQueryParameters(validatedReq.Query);
        var sanitizedHeaders = sanitizer.ValidateAndSanitizeHeaders(validatedReq.Headers);

        // 3. Validación de servicio permitido
        var allowedServices = opts.Value.Services?.Keys ?? Enumerable.Empty<string>();
        if (!sanitizer.IsValidService(validatedReq.Service, allowedServices))
        {
            return Results.Problem($"Service '{validatedReq.Service}' is not configured",
                statusCode: StatusCodes.Status404NotFound);
        }

        // 4. Crear request sanitizado
        var sanitizedReq = new TranslateRequest
        {
            Service = validatedReq.Service,
            Method = validatedReq.Method.ToUpperInvariant(),
            Path = sanitizedPath,
            Query = sanitizedQuery,
            Headers = sanitizedHeaders,
            Body = validatedReq.Body // ¡SOLUCIÓN: Asignar el Body!
        };

        Console.WriteLine($"=== SANITIZED REQUEST ===");
        Console.WriteLine($"Service: {sanitizedReq.Service}");
        Console.WriteLine($"Method: {sanitizedReq.Method}");
        Console.WriteLine($"Path: {sanitizedReq.Path}");
        Console.WriteLine($"Body: {sanitizedReq.Body}");
        Console.WriteLine($"Body Type: {sanitizedReq.Body?.GetType().Name ?? "null"}");
        Console.WriteLine($"=== END SANITIZED REQUEST ===");

        // 5. Validaciones de ACL
        if (!translator.IsAllowed(sanitizedReq))
            return Results.Problem("Route not allowed by ACL", statusCode: StatusCodes.Status403Forbidden);

        // 6. Propagar Authorization del cliente (sanitizado)
        if (http.Request.Headers.TryGetValue("Authorization", out var bearer))
        {
            var sanitizedBearer = sanitizer.SanitizeString(bearer.ToString());
            sanitizedReq.Headers["Authorization"] = sanitizedBearer;
        }

        // 7. Forward con request sanitizado
        await translator.ForwardAsync(http, sanitizedReq, http.RequestAborted);
        return Results.Empty;
    }
    catch (SecurityException ex)
    {
        return Results.Problem(ex.Message, statusCode: StatusCodes.Status400BadRequest);
    }
    catch (ArgumentException ex)
    {
        return Results.Problem(ex.Message, statusCode: StatusCodes.Status400BadRequest);
    }
})
.WithName("TranslateRequest")
.WithSummary("Traduce y envía peticiones a microservicios")
.WithDescription("Permite enviar peticiones HTTP a través del gateway hacia los microservicios configurados")
.WithTags("Gateway")
.RequireRateLimiting("global")
.Produces(StatusCodes.Status200OK)
.Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
.Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
.Produces<ProblemDetails>(StatusCodes.Status413PayloadTooLarge)
.Produces<ProblemDetails>(StatusCodes.Status502BadGateway)
.ProducesValidationProblem();

// Endpoint directo para cada servicio - SIN path adicional
app.MapMethods("/api/v1/services/{service}",
    new[] { "GET", "POST", "PUT", "PATCH", "DELETE" },
    async (string service, HttpContext context, RequestTranslator translator) =>
{
    var request = new TranslateRequest
    {
        Service = service,
        Method = context.Request.Method,
        Path = $"/api/{service}",
        Query = context.Request.Query.ToDictionary(q => q.Key, q => q.Value.ToString()),
        Headers = context.Request.Headers
            .Where(h => !h.Key.StartsWith(":"))
            .ToDictionary(h => h.Key, h => h.Value.ToString())
    };

    if (!translator.IsAllowed(request))
        return Results.Problem("Route not allowed", statusCode: StatusCodes.Status403Forbidden);

    await translator.ForwardAsync(context, request, context.RequestAborted);
    return Results.Empty;
})
.WithName("DirectServiceCallNoPath")
.WithSummary("Llamada directa a servicio sin path")
.WithDescription("Permite llamadas directas a servicios usando la ruta /api/v1/services/{service}")
.WithTags("Gateway", "Direct")
.RequireRateLimiting("public");

// Endpoint directo para cada servicio - CON path adicional
app.MapMethods("/api/v1/services/{service}/{**path}",
    new[] { "GET", "POST", "PUT", "PATCH", "DELETE" },
    async (string service, string path, HttpContext context, RequestTranslator translator) =>
{
    var request = new TranslateRequest
    {
        Service = service,
        Method = context.Request.Method,
        Path = $"/api/{path}",
        Query = context.Request.Query.ToDictionary(q => q.Key, q => q.Value.ToString()),
        Headers = context.Request.Headers
            .Where(h => !h.Key.StartsWith(":"))
            .ToDictionary(h => h.Key, h => h.Value.ToString())
    };

    if (!translator.IsAllowed(request))
        return Results.Problem("Route not allowed", statusCode: StatusCodes.Status403Forbidden);

    await translator.ForwardAsync(context, request, context.RequestAborted);
    return Results.Empty;
})
.WithName("DirectServiceCall")
.WithSummary("Llamada directa a servicio")
.WithDescription("Permite llamadas directas a servicios usando la ruta /api/v1/services/{service}/{path}")
.WithTags("Gateway", "Direct")
.RequireRateLimiting("public");

// --- Health Checks ---
app.MapGet("/health", async ([FromServices] IServiceProvider sp, [FromQuery] bool deep = false, [FromQuery] bool includeMetrics = false) =>
{
    var healthCheckService = sp.GetRequiredService<HealthCheckService>();
    var metricsService = sp.GetRequiredService<IMetricsService>();

    var options = new HealthCheckOptions
    {
        Predicate = deep ? _ => true : check => check.Tags.Contains("live")
    };

    var result = await healthCheckService.CheckHealthAsync(options.Predicate);

    var response = new HealthCheckResponse
    {
        Status = result.Status.ToString(),
        TotalDuration = result.TotalDuration,
        Services = result.Entries.ToDictionary(
            kvp => kvp.Key,
            kvp => new ServiceHealthStatus
            {
                Status = kvp.Value.Status.ToString(),
                Description = kvp.Value.Description,
                Duration = kvp.Value.Duration,
                Data = kvp.Value.Data?.ToDictionary(d => d.Key, d => d.Value)
            }
        )
    };

    if (includeMetrics)
    {
        response = response with { Metrics = metricsService.GetMetrics() };
    }

    return result.Status == HealthStatus.Healthy
        ? Results.Ok(response)
        : Results.Json(response, statusCode: 503);
})
.WithName("HealthCheck")
.WithSummary("Verificación de salud del gateway")
.WithDescription("Endpoint para verificar el estado de salud del gateway y sus dependencias")
.WithTags("Health")
.CacheOutput("LongCache");

app.MapGet("/health/live", async (IServiceProvider sp) =>
{
    var healthCheckService = sp.GetRequiredService<HealthCheckService>();
    var result = await healthCheckService.CheckHealthAsync(check => check.Tags.Contains("live"));

    return result.Status == HealthStatus.Healthy
        ? Results.Ok(new { status = "healthy", timestamp = DateTimeOffset.UtcNow })
        : Results.Json(new { status = "unhealthy", timestamp = DateTimeOffset.UtcNow }, statusCode: 503);
})
.WithName("LivenessCheck")
.WithSummary("Verificación de vida")
.WithTags("Health");

app.MapGet("/health/ready", async (IServiceProvider sp) =>
{
    var healthCheckService = sp.GetRequiredService<HealthCheckService>();
    var result = await healthCheckService.CheckHealthAsync(check => check.Tags.Contains("ready"));

    return result.Status == HealthStatus.Healthy
        ? Results.Ok(new { status = "ready", timestamp = DateTimeOffset.UtcNow })
        : Results.Json(new { status = "not ready", timestamp = DateTimeOffset.UtcNow }, statusCode: 503);
})
.WithName("ReadinessCheck")
.WithSummary("Verificación de preparación")
.WithTags("Health");

// --- Métricas ---
app.MapGet("/metrics", ([FromServices] IMetricsService metricsService) =>
{
    var metrics = metricsService.GetMetrics();
    return Results.Ok(metrics);
})
.WithName("GetMetrics")
.WithSummary("Métricas del gateway")
.WithDescription("Obtiene las métricas de rendimiento y uso del gateway")
.WithTags("Metrics")
.CacheOutput();

app.MapPost("/metrics/reset", ([FromServices] IMetricsService metricsService) =>
{
    metricsService.ResetMetrics();
    return Results.Ok(new { message = "Metrics reset successfully", timestamp = DateTimeOffset.UtcNow });
})
.WithName("ResetMetrics")
.WithSummary("Reinicia las métricas")
.WithDescription("Reinicia todas las métricas del gateway")
.WithTags("Metrics");

// --- Gestión de caché ---
app.MapDelete("/cache/{service}", async (
    string service,
    [FromServices] ICacheService cacheService,
    [FromServices] IInputSanitizationService sanitizer,
    [FromServices] IOptions<GateOptions> opts) =>
{
    try
    {
        // 1. Validar y sanitizar el nombre del servicio
        var sanitizedService = sanitizer.SanitizeString(service);

        // 2. Validar que el servicio esté configurado
        var allowedServices = opts.Value.Services?.Keys ?? Enumerable.Empty<string>();
        if (!sanitizer.IsValidService(sanitizedService, allowedServices))
        {
            return Results.Problem($"Service '{sanitizedService}' is not configured",
                statusCode: StatusCodes.Status404NotFound);
        }

        // 3. Invalidar caché
        await cacheService.InvalidateServiceCacheAsync(sanitizedService);
        return Results.Ok(new { message = $"Cache invalidated for service: {sanitizedService}" });
    }
    catch (Exception)
    {
        return Results.Problem("Error invalidating cache", statusCode: StatusCodes.Status500InternalServerError);
    }
})
.WithName("InvalidateCache")
.WithSummary("Invalida caché por servicio")
.WithDescription("Invalida el caché para un servicio específico configurado")
.WithTags("Cache");

// --- Información del gateway ---
app.MapGet("/info", (IConfiguration config) =>
{
    var gateConfig = config.GetSection("Gate").Get<GateOptions>();
    return Results.Ok(new
    {
        name = "Accessibility Gateway",
        version = "1.0.0",
        environment = app.Environment.EnvironmentName,
        services = gateConfig?.Services?.Keys.ToArray() ?? Array.Empty<string>(),
        features = new
        {
            caching = gateConfig?.EnableCaching ?? false,
            metrics = gateConfig?.EnableMetrics ?? false,
            tracing = gateConfig?.EnableTracing ?? false
        },
        timestamp = DateTimeOffset.UtcNow
    });
})
.WithName("GetGatewayInfo")
.WithSummary("Información del gateway")
.WithDescription("Información general sobre el gateway y su configuración")
.WithTags("Info")
.CacheOutput("LongCache");

// Health Checks UI
app.MapHealthChecksUI();

// Manejo de errores globales
app.UseExceptionHandler("/error");
app.Map("/error", () => Results.Problem("An error occurred processing your request"));

app.Run();

// Políticas de resiliencia de Polly
static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .WaitAndRetryAsync(
            retryCount: 3,
            sleepDurationProvider: retryAttempt =>
                TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)) // Exponential backoff
                + TimeSpan.FromMilliseconds(Random.Shared.Next(0, 100)), // Jitter
            onRetry: (outcome, timespan, retryCount, context) =>
            {
                var logger = context.GetValueOrDefault("logger") as Microsoft.Extensions.Logging.ILogger;
                logger?.LogWarning("Retry attempt {RetryCount} for {OperationKey} in {Delay}ms",
                    retryCount, context.OperationKey, timespan.TotalMilliseconds);
            });
}

static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .CircuitBreakerAsync(
            handledEventsAllowedBeforeBreaking: 3,
            durationOfBreak: TimeSpan.FromSeconds(30),
            onBreak: (exception, timespan) =>
            {
                Console.WriteLine($"Circuit breaker opened for {timespan.TotalSeconds} seconds due to {exception.GetType().Name}");
            },
            onReset: () =>
            {
                Console.WriteLine("Circuit breaker reset");
            });
}

// Hacer el tipo Program parcial para los tests
public partial class Program { }