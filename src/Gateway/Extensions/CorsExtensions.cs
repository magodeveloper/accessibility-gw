namespace Gateway.Extensions;

/// <summary>
/// Extensiones para configurar CORS
/// </summary>
public static class CorsExtensions
{
    /// <summary>
    /// Configura políticas de CORS para el Gateway
    /// </summary>
    public static IServiceCollection AddGatewayCors(this IServiceCollection services)
    {
        services.AddCors(options =>
        {
            options.AddPolicy("AllowedOrigins", policy =>
            {
                policy.AllowAnyOrigin()
                      .AllowAnyMethod()
                      .AllowAnyHeader();
            });
        });

        return services;
    }
}
