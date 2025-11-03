using System.Text;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace Gateway.Extensions;

/// <summary>
/// Extensiones para configurar autenticación JWT
/// </summary>
public static class AuthenticationExtensions
{
    /// <summary>
    /// Configura autenticación y autorización JWT
    /// </summary>
    public static IServiceCollection AddJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        var jwtSection = configuration.GetSection("JwtSettings");
        var secretKey = jwtSection["SecretKey"];
        var validIssuer = jwtSection["Issuer"];
        var validAudience = jwtSection["Audience"];

        // DEBUG: Log para diagnosticar problema de tests
        Console.WriteLine($"=== JWT CONFIG DEBUG === SecretKey: '{secretKey?.Substring(0, Math.Min(10, secretKey?.Length ?? 0))}...' | Issuer: '{validIssuer}' | Audience: '{validAudience}'");

        // Validar que los secrets estén configurados
        if (string.IsNullOrWhiteSpace(secretKey) || secretKey == "OVERRIDE_WITH_USER_SECRETS")
        {
            if (!environment.IsDevelopment() && environment.EnvironmentName != "Test")
            {
                throw new InvalidOperationException(
                    "JWT SecretKey is required in production. Set it via environment variables or Azure Key Vault.");
            }
            Console.WriteLine("=== JWT DISABLED === Authentication will not be configured");
            Console.WriteLine("JWT SecretKey not configured. Authentication will be disabled.");
            return services;
        }

        // Configurar Authentication
        services.AddAuthentication("Bearer")
            .AddJwtBearer("Bearer", opt =>
            {
                opt.RequireHttpsMetadata = environment.IsProduction();
                opt.SaveToken = true;
                opt.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = jwtSection.GetValue<bool>("ValidateIssuer", true),
                    ValidateAudience = jwtSection.GetValue<bool>("ValidateAudience", true),
                    ValidateLifetime = jwtSection.GetValue<bool>("ValidateLifetime", true),
                    ValidateIssuerSigningKey = jwtSection.GetValue<bool>("ValidateIssuerSigningKey", true),
                    ValidIssuer = validIssuer,
                    ValidAudience = validAudience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
                    ClockSkew = TimeSpan.FromMinutes(1)
                };

                // Logging para debug
                opt.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        var contextLogger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                        contextLogger.LogError("JWT Authentication failed: {Error}", context.Exception.Message);
                        return Task.CompletedTask;
                    },
                    OnTokenValidated = context =>
                    {
                        var contextLogger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                        contextLogger.LogInformation("JWT Token validated successfully for user: {User}",
                            context.Principal?.Identity?.Name ?? "Unknown");
                        return Task.CompletedTask;
                    }
                };
            });

        // Configurar Authorization
        services.AddAuthorization(options =>
        {
            // NOTA: FallbackPolicy comentado porque bloqueaba rutas públicas (requiresAuth=false)
            // La autorización se maneja vía RouteAuthorizationMiddleware que lee AllowedRoutes

            // Política para administradores
            options.AddPolicy("AdminOnly", policy =>
                policy.RequireClaim("role", "admin"));

            // Política para usuarios activos
            options.AddPolicy("ActiveUser", policy =>
                policy.RequireClaim("status", "active"));
        });

        // Configurar logging detallado
        services.AddLogging(logging =>
        {
            logging.AddConsole();
            logging.AddDebug();
            logging.SetMinimumLevel(LogLevel.Debug); // Para ver logs del middleware JWT
        });

        Console.WriteLine("JWT Authentication configured successfully");
        return services;
    }
}
