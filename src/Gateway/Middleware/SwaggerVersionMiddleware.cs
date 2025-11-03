using System.Text;
using System.Text.Json;

namespace Gateway.Middleware;

/// <summary>
/// Middleware para modificar los documentos Swagger/OpenAPI antes de servirlos.
/// Cambia la versión de OpenAPI de 3.0.4 a 3.0.1 para compatibilidad con Swagger UI.
/// </summary>
public class SwaggerVersionMiddleware
{
    private readonly RequestDelegate _next;

    public SwaggerVersionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Solo procesar peticiones a documentos Swagger JSON
        if (context.Request.Path.StartsWithSegments("/swagger") &&
            context.Request.Path.Value!.EndsWith("swagger.json", StringComparison.OrdinalIgnoreCase))
        {
            // Capturar la respuesta original
            var originalBodyStream = context.Response.Body;

            using var responseBody = new MemoryStream();
            context.Response.Body = responseBody;

            // Llamar al siguiente middleware
            await _next(context);

            // Leer la respuesta
            responseBody.Seek(0, SeekOrigin.Begin);
            var text = await new StreamReader(responseBody).ReadToEndAsync();

            // Modificar el JSON: cambiar openapi: "3.0.4" por openapi: "3.0.1"
            if (!string.IsNullOrEmpty(text) && text.Contains("\"openapi\""))
            {
                // Reemplazar la versión de OpenAPI
                text = text.Replace("\"openapi\": \"3.0.4\"", "\"openapi\": \"3.0.1\"");
                text = text.Replace("\"openapi\":\"3.0.4\"", "\"openapi\":\"3.0.1\"");
            }

            // Escribir la respuesta modificada
            var modifiedBytes = Encoding.UTF8.GetBytes(text);
            context.Response.Body = originalBodyStream;
            context.Response.ContentLength = modifiedBytes.Length;
            await context.Response.Body.WriteAsync(modifiedBytes);
        }
        else
        {
            // Para otras peticiones, continuar normalmente
            await _next(context);
        }
    }
}

/// <summary>
/// Extensión para agregar el middleware a la pipeline.
/// </summary>
public static class SwaggerVersionMiddlewareExtensions
{
    public static IApplicationBuilder UseSwaggerVersionMiddleware(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<SwaggerVersionMiddleware>();
    }
}