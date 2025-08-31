using Polly;
using Serilog;
using Gateway;
using System.Net;
using Gateway.Models;
using Gateway.Services;
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
// Nota: Las políticas de reintentos y circuit breaker están comentadas temporalmente
// hasta que se resuelvan las dependencias de Polly

builder.Services.AddHttpClient();
builder.Services.AddHttpForwarder();
builder.Services.AddHttpClient("DefaultClient", (sp, client) =>
{
    var gateOptions = sp.GetRequiredService<IOptions<GateOptions>>().Value;
    client.Timeout = TimeSpan.FromSeconds(gateOptions.DefaultTimeoutSeconds);
})
.ConfigurePrimaryHttpMessageHandler(_ => new SocketsHttpHandler());
//.AddPolicyHandler(GetRetryPolicy())
//.AddPolicyHandler(GetCircuitBreakerPolicy());

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
builder.Services.AddSingleton<RequestTranslator>();
builder.Services.AddSingleton<CacheService>();
builder.Services.AddSingleton<MetricsService>();
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
        healthChecksBuilder.AddCheck<ServiceHealthCheck>(
            $"service-{service.Key}",
            tags: new[] { "ready", service.Key });
    }
}

// Health check para Redis si está configurado
if (!string.IsNullOrEmpty(redisConnectionString))
{
    healthChecksBuilder.AddRedis(redisConnectionString, name: "redis", tags: new[] { "ready" });
}

// UI de Health Checks
builder.Services.AddHealthChecksUI(opt =>
{
    opt.SetEvaluationTimeInSeconds(30);
    opt.MaximumHistoryEntriesPerEndpoint(50);
    opt.SetApiMaxActiveRequests(2);
    opt.AddHealthCheckEndpoint("Gateway API", "/health");
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

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Accessibility Gateway API v1");
        c.RoutePrefix = "swagger"; // Swagger UI en /swagger
        c.DisplayRequestDuration();
        c.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.List);
        c.EnableFilter();
        c.EnableDeepLinking();
    });
}

// Comentado para desarrollo local sin HTTPS
// app.UseHsts();
// app.UseHttpsRedirection();
app.UseCors("AllowedOrigins");

if (!string.IsNullOrWhiteSpace(authority))
{
    app.UseAuthentication();
    app.UseAuthorization();
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

            // Intentar mapear la ruta a un servicio conocido
            string? targetService = null;

            if (path.StartsWith("/api/v1/users") || path.StartsWith("/api/auth"))
                targetService = "users";
            else if (path.StartsWith("/api/Report"))
                targetService = "reports";
            else if (path.StartsWith("/api/Analysis"))
                targetService = "analysis";
            else if (path.StartsWith("/api/analyze"))
                targetService = "middleware";

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
    TranslateRequest req,
    RequestTranslator translator,
    IOptions<GateOptions> opts,
    HttpContext http) =>
{
    // Validación de tamaño de payload
    var max = opts.Value.MaxPayloadSizeBytes;
    if (http.Request.ContentLength is long len && len > max)
        return Results.Problem("Payload too large", statusCode: StatusCodes.Status413PayloadTooLarge);

    // Validaciones de ACL
    if (!translator.IsAllowed(req))
        return Results.Problem("Route not allowed by ACL", statusCode: StatusCodes.Status403Forbidden);

    // Propagar Authorization del cliente
    if (http.Request.Headers.TryGetValue("Authorization", out var bearer))
    {
        http.Request.Headers["Authorization"] = bearer.ToString();
    }

    await translator.ForwardAsync(http, req, http.RequestAborted);
    return Results.Empty;
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

// Endpoint directo para cada servicio (alternativo)
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
    var metricsService = sp.GetRequiredService<MetricsService>();

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
app.MapGet("/metrics", (MetricsService metricsService) =>
{
    var metrics = metricsService.GetMetrics();
    return Results.Ok(metrics);
})
.WithName("GetMetrics")
.WithSummary("Métricas del gateway")
.WithDescription("Obtiene las métricas de rendimiento y uso del gateway")
.WithTags("Metrics")
.CacheOutput();

app.MapPost("/metrics/reset", (MetricsService metricsService) =>
{
    metricsService.ResetMetrics();
    return Results.Ok(new { message = "Metrics reset successfully", timestamp = DateTimeOffset.UtcNow });
})
.WithName("ResetMetrics")
.WithSummary("Reinicia las métricas")
.WithDescription("Reinicia todas las métricas del gateway")
.WithTags("Metrics");

// --- Gestión de caché ---
app.MapDelete("/cache/{service}", async (string service, CacheService cacheService) =>
{
    await cacheService.InvalidateServiceCacheAsync(service);
    return Results.Ok(new { message = $"Cache invalidated for service: {service}" });
})
.WithName("InvalidateCache")
.WithSummary("Invalida caché por servicio")
.WithDescription("Invalida el caché para un servicio específico")
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

// Hacer el tipo Program parcial para los tests
public partial class Program { }