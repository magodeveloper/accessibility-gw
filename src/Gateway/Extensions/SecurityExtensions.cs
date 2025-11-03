namespace Gateway.Extensions;

/// <summary>
/// Extensiones para configurar Security Headers
/// </summary>
public static class SecurityExtensions
{
    /// <summary>
    /// Configura middleware de security headers
    /// </summary>
    public static IApplicationBuilder UseSecurityHeaders(
        this IApplicationBuilder app,
        IWebHostEnvironment environment)
    {
        app.Use(async (context, next) =>
        {
            // Security headers esenciales para protección
            context.Response.Headers["X-Content-Type-Options"] = "nosniff";
            context.Response.Headers["X-Frame-Options"] = "DENY";
            context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
            context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";

            // HSTS solo en producción con HTTPS
            if (environment.IsProduction())
            {
                context.Response.Headers["Strict-Transport-Security"] =
                    "max-age=31536000; includeSubDomains; preload";
            }

            // Content Security Policy adaptado para API Gateway con Swagger
            var cspPolicy = environment.IsDevelopment()
                ? "default-src 'self'; script-src 'self' 'unsafe-inline' 'unsafe-eval'; style-src 'self' 'unsafe-inline'; img-src 'self' data:; font-src 'self' data:"
                : "default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'; img-src 'self' data:; font-src 'self'";

            context.Response.Headers["Content-Security-Policy"] = cspPolicy;

            // Remover headers que revelan información del servidor
            context.Response.Headers.Remove("Server");
            context.Response.Headers.Remove("X-Powered-By");

            await next();
        });

        return app;
    }
}
