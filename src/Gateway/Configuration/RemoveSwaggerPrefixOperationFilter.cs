using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Gateway.Configuration;

/// <summary>
/// Operation Filter que remueve el prefijo "_swagger" de las rutas en la documentación Swagger.
/// Esto permite que los controllers proxy usen rutas internas diferentes a las reales,
/// evitando conflictos con YARP pero mostrando las rutas correctas en Swagger UI.
/// </summary>
public class RemoveSwaggerPrefixOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        // Obtener el path actual
        var apiDescription = context.ApiDescription;
        var relativePath = apiDescription.RelativePath;

        if (string.IsNullOrEmpty(relativePath))
            return;

        // Si la ruta contiene el prefijo "_swagger/", lo removemos
        if (relativePath.Contains("_swagger/"))
        {
            // Modificar la ruta en el contexto
            var cleanPath = "/" + relativePath.Replace("_swagger/", "");

            // Nota: No podemos modificar directamente operation.Path aquí porque se asigna después
            // La limpieza real se hace en el DocumentFilter
        }
    }
}

/// <summary>
/// Document Filter que limpia el prefijo "_swagger/" de todas las rutas en el documento final.
/// Se ejecuta después de que todas las operaciones han sido procesadas.
/// </summary>
public class RemoveSwaggerPrefixDocumentFilter : IDocumentFilter
{
    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        // Crear un nuevo diccionario de paths sin el prefijo "_swagger/"
        var cleanPaths = new OpenApiPaths();

        foreach (var path in swaggerDoc.Paths)
        {
            // Remover el prefijo "_swagger/" de cada path
            var cleanPath = path.Key.Replace("/_swagger/", "/");
            cleanPaths.Add(cleanPath, path.Value);
        }

        // Reemplazar los paths originales con los limpios
        swaggerDoc.Paths = cleanPaths;
    }
}
