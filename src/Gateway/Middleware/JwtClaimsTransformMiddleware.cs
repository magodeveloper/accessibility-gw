using System.Security.Claims;
using Microsoft.Extensions.Configuration;

namespace Gateway.Middleware;

/// <summary>
/// Middleware que extrae claims del JWT y los propaga como headers HTTP
/// para que los microservicios downstream puedan consumirlos sin validar JWT.
/// También agrega un header X-Gateway-Secret para prevenir acceso directo a los microservicios.
/// </summary>
public class JwtClaimsTransformMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<JwtClaimsTransformMiddleware> _logger;
    private readonly string? _gatewaySecret;

    public JwtClaimsTransformMiddleware(
        RequestDelegate next,
        ILogger<JwtClaimsTransformMiddleware> logger,
        IConfiguration configuration)
    {
        _next = next;
        _logger = logger;
        _gatewaySecret = configuration["Gate:Secret"] ?? configuration["Gateway:Secret"] ?? configuration["GATEWAY_SECRET"];
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Agregar Gateway Secret si está configurado (para todos los requests)
        if (!string.IsNullOrEmpty(_gatewaySecret))
        {
            context.Request.Headers["X-Gateway-Secret"] = _gatewaySecret;
            _logger.LogInformation("=== JWT MIDDLEWARE === Added X-Gateway-Secret header: '{Secret}' (length: {Length})",
                _gatewaySecret, _gatewaySecret.Length);
        }
        else
        {
            _logger.LogWarning("=== JWT MIDDLEWARE === Gateway secret is NULL or empty!");
        }

        // Solo procesar claims si el usuario está autenticado
        if (context.User.Identity?.IsAuthenticated == true)
        {
            _logger.LogInformation("=== JWT MIDDLEWARE === User IS authenticated");

            try
            {
                // Extraer claims del JWT
                var userId = GetClaimValue(context.User, ClaimTypes.NameIdentifier, "sub", "userId");
                var email = GetClaimValue(context.User, ClaimTypes.Email, "email");
                var role = GetClaimValue(context.User, ClaimTypes.Role, "role");
                var userName = GetClaimValue(context.User, ClaimTypes.Name, "name", "userName");

                // Agregar headers para microservicios downstream
                if (!string.IsNullOrEmpty(userId))
                {
                    context.Request.Headers["X-User-Id"] = userId;
                    _logger.LogDebug("Added X-User-Id header: {UserId}", userId);
                }

                if (!string.IsNullOrEmpty(email))
                {
                    context.Request.Headers["X-User-Email"] = email;
                    _logger.LogDebug("Added X-User-Email header: {Email}", email);
                }

                if (!string.IsNullOrEmpty(role))
                {
                    context.Request.Headers["X-User-Role"] = role;
                    _logger.LogDebug("Added X-User-Role header: {Role}", role);
                }

                if (!string.IsNullOrEmpty(userName))
                {
                    context.Request.Headers["X-User-Name"] = userName;
                    _logger.LogDebug("Added X-User-Name header: {UserName}", userName);
                }

                _logger.LogInformation(
                    "JWT claims transformed - UserId: {UserId}, Email: {Email}, Role: {Role}",
                    userId, email, role);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting JWT claims");
                // No bloqueamos la request, solo logueamos el error
            }
        }
        else
        {
            _logger.LogDebug("Request without authentication, skipping claims transformation");
        }

        await _next(context);
    }

    /// <summary>
    /// Obtiene el valor de un claim probando múltiples nombres posibles
    /// </summary>
    private string? GetClaimValue(ClaimsPrincipal user, params string[] claimTypes)
    {
        foreach (var claimType in claimTypes)
        {
            var claim = user.FindFirst(claimType);
            if (claim != null && !string.IsNullOrWhiteSpace(claim.Value))
            {
                return claim.Value;
            }
        }
        return null;
    }
}
