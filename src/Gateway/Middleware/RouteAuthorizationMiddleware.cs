using Gateway.Models;
using Microsoft.Extensions.Options;

namespace Gateway.Middleware;

/// <summary>
/// Middleware que verifica si una ruta requiere autenticación basándose en la configuración de AllowedRoutes.
/// Este middleware debe ejecutarse ENTRE UseAuthentication() y UseAuthorization().
/// Para rutas con requiresAuth=false, permite el acceso sin JWT.
/// Para rutas con requiresAuth=true, verifica que el usuario esté autenticado.
/// </summary>
public class RouteAuthorizationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RouteAuthorizationMiddleware> _logger;
    private readonly GateOptions _options;

    public RouteAuthorizationMiddleware(
        RequestDelegate next,
        ILogger<RouteAuthorizationMiddleware> logger,
        IOptions<GateOptions> options)
    {
        _next = next;
        _logger = logger;
        _options = options.Value;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? string.Empty;
        var method = context.Request.Method;

        // Rutas públicas del sistema (health, metrics, swagger)
        var systemPublicPaths = new[]
        {
            "/health", "/health/live", "/health/ready",
            "/metrics",
            "/gateway/metrics",
            "/info",
            "/error",
            "/cache/",
            "/swagger" // Incluye /swagger, /swagger/, /swagger/index.html, /swagger/*/swagger.json, etc.
        };

        if (systemPublicPaths.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
        {
            _logger.LogDebug("System public route {Path} - allowing access", path);
            await _next(context);
            return;
        }

        // Buscar si esta ruta está configurada en AllowedRoutes
        var matchedRoute = _options.AllowedRoutes?.FirstOrDefault(route =>
            path.StartsWith(route.PathPrefix, StringComparison.OrdinalIgnoreCase) &&
            route.Methods.Contains(method, StringComparer.OrdinalIgnoreCase));

        if (matchedRoute != null)
        {
            if (!matchedRoute.RequiresAuth)
            {
                // Ruta pública configurada - permitir sin autenticación
                _logger.LogInformation(
                    "=== PUBLIC ROUTE === {Method} {Path} (requiresAuth=false) - bypassing JWT check",
                    method, path);
                await _next(context);
                return;
            }
            else
            {
                // Ruta protegida - verificar autenticación
                if (context.User?.Identity?.IsAuthenticated != true)
                {
                    _logger.LogWarning(
                        "=== PROTECTED ROUTE === {Method} {Path} (requiresAuth=true) - user NOT authenticated - returning 401",
                        method, path);
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    await context.Response.WriteAsJsonAsync(new
                    {
                        error = "Unauthorized",
                        message = "This endpoint requires authentication. Please provide a valid JWT token.",
                        path = path,
                        timestamp = DateTime.UtcNow
                    });
                    return;
                }

                _logger.LogDebug(
                    "Protected route {Method} {Path} - user authenticated - allowing access",
                    method, path);
            }
        }
        else
        {
            // Ruta NO configurada - denegar por defecto (seguridad)
            _logger.LogWarning(
                "Route {Method} {Path} NOT found in AllowedRoutes - denying access (default deny)",
                method, path);
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new
            {
                error = "Forbidden",
                message = "This route is not configured in the gateway. Please contact the administrator.",
                path = path,
                timestamp = DateTime.UtcNow
            });
            return;
        }

        await _next(context);
    }
}
