# 🔐 Guía de Seguridad

> Implementación de seguridad del Gateway incluyendo autenticación, autorización, y mejores prácticas.

## 📋 Tabla de Contenidos

- [Autenticación JWT](#autenticación-jwt)
- [Autorización](#autorización)
- [Rate Limiting](#rate-limiting)
- [Headers de Seguridad](#headers-de-seguridad)
- [Validación de Input](#validación-de-input)
- [Seguridad en Docker](#seguridad-en-docker)
- [Mejores Prácticas](#mejores-prácticas)

---

## 🔑 Autenticación JWT

### Flujo de Autenticación

```mermaid
sequenceDiagram
    participant C as Cliente
    participant GW as Gateway
    participant U as Users API
    participant R as Redis

    C->>GW: POST /api/auth/login
    GW->>U: Validar credenciales
    U-->>GW: Usuario válido + claims
    GW->>GW: Generar JWT + Refresh Token
    GW->>R: Guardar sesión
    GW-->>C: Tokens (access + refresh)

    Note over C,R: Request subsecuente

    C->>GW: GET /api/users + Bearer Token
    GW->>GW: Validar JWT
    alt Token válido
        GW->>U: Forward request
        U-->>GW: Response
        GW-->>C: Response
    else Token inválido/expirado
        GW-->>C: 401 Unauthorized
    end
```

### Configuración JWT

```csharp
// Program.cs
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,

            ValidIssuer = builder.Configuration["JWT:Issuer"],
            ValidAudience = builder.Configuration["JWT:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["JWT:Secret"])),

            ClockSkew = TimeSpan.FromMinutes(5),
            RequireExpirationTime = true,
            RequireSignedTokens = true
        };

        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
                {
                    context.Response.Headers.Add("Token-Expired", "true");
                }
                return Task.CompletedTask;
            },

            OnTokenValidated = context =>
            {
                // Log successful authentication
                var userId = context.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                // Log audit trail
                return Task.CompletedTask;
            },

            OnChallenge = context =>
            {
                context.HandleResponse();
                context.Response.StatusCode = 401;
                context.Response.ContentType = "application/json";
                var result = JsonSerializer.Serialize(new
                {
                    error = "unauthorized",
                    message = "Invalid or missing authentication token"
                });
                return context.Response.WriteAsync(result);
            }
        };
    });
```

### Generar JWT Secret Segura

```powershell
# PowerShell Script: Generate-JwtSecretKey.ps1
<#
.SYNOPSIS
    Genera una clave secreta segura para JWT
#>

function Generate-JwtSecretKey {
    param(
        [int]$KeySizeBytes = 32  # 256 bits
    )

    try {
        $bytes = New-Object Byte[] $KeySizeBytes
        $rng = [Security.Cryptography.RandomNumberGenerator]::Create()
        $rng.GetBytes($bytes)

        $base64 = [Convert]::ToBase64String($bytes)
        $hex = [BitConverter]::ToString($bytes) -replace '-', ''

        Write-Host "✅ JWT Secret Keys Generadas:" -ForegroundColor Green
        Write-Host ""
        Write-Host "Base64 (recomendado para .env):" -ForegroundColor Cyan
        Write-Host $base64
        Write-Host ""
        Write-Host "Hexadecimal:" -ForegroundColor Cyan
        Write-Host $hex
        Write-Host ""
        Write-Host "Longitud: $KeySizeBytes bytes ($($KeySizeBytes * 8) bits)" -ForegroundColor Yellow

        # Validar longitud mínima
        if ($KeySizeBytes -lt 32) {
            Write-Warning "⚠️ Se recomienda al menos 32 bytes (256 bits) para producción"
        }

        return $base64
    }
    catch {
        Write-Error "Error generando clave: $_"
        return $null
    }
}

# Ejecutar
Generate-JwtSecretKey
```

### Uso de JWT en Requests

```bash
# 1. Login y obtener token
curl -X POST http://localhost:8100/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "user@example.com",
    "password": "SecurePass123!"
  }'

# Response:
# {
#   "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
#   "refreshToken": "def50200...",
#   "expiresIn": 3600
# }

# 2. Usar token en requests protegidos
curl -X GET http://localhost:8100/api/users/profile \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."

# 3. Renovar token expirado
curl -X POST http://localhost:8100/api/auth/refresh \
  -H "Content-Type: application/json" \
  -d '{
    "refreshToken": "def50200..."
  }'
```

---

## 🛡️ Autorización

### Políticas de Autorización

```csharp
builder.Services.AddAuthorization(options =>
{
    // Política: Usuario autenticado
    options.AddPolicy("Authenticated", policy =>
        policy.RequireAuthenticatedUser());

    // Política: Rol Admin
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireRole("Admin"));

    // Política: Claim específico
    options.AddPolicy("CanAccessReports", policy =>
        policy.RequireClaim("Permission", "Reports.Read"));

    // Política: Múltiples requisitos
    options.AddPolicy("PremiumUser", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireClaim("Subscription", "Premium", "Enterprise");
        policy.RequireRole("User", "PowerUser");
    });

    // Política personalizada
    options.AddPolicy("CanAnalyze", policy =>
        policy.Requirements.Add(new MinimumAnalysisQuotaRequirement(10)));
});
```

### Aplicar Autorización

```csharp
// En endpoints específicos
app.MapGet("/api/admin/users", [Authorize(Policy = "AdminOnly")]
    async (IUserService userService) =>
{
    return await userService.GetAllUsersAsync();
});

// En rutas YARP con metadata
app.MapReverseProxy(proxyPipeline =>
{
    proxyPipeline.UseAuthorization();
});
```

---

## ⚡ Rate Limiting

### Configuración Avanzada

```csharp
builder.Services.AddRateLimiter(options =>
{
    // 1. Política por IP
    options.AddFixedWindowLimiter("per-ip", opt =>
    {
        opt.PermitLimit = 100;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = 10;
    });

    // 2. Política por Usuario Autenticado
    options.AddSlidingWindowLimiter("per-user", opt =>
    {
        opt.PermitLimit = 500;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.SegmentsPerWindow = 6;
        opt.QueueLimit = 20;
    });

    // 3. Política para Login (muy restrictiva)
    options.AddFixedWindowLimiter("login", opt =>
    {
        opt.PermitLimit = 5;
        opt.Window = TimeSpan.FromMinutes(5);
        opt.QueueLimit = 0;
    });

    // 4. Política para operaciones costosas
    options.AddTokenBucketLimiter("heavy-operations", opt =>
    {
        opt.TokenLimit = 10;
        opt.ReplenishmentPeriod = TimeSpan.FromMinutes(1);
        opt.TokensPerPeriod = 2;
        opt.AutoReplenishment = true;
    });

    // Limiter global
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(
        httpContext =>
        {
            var user = httpContext.User.Identity?.Name;

            if (!string.IsNullOrEmpty(user))
            {
                // Usuario autenticado: límite más alto
                return RateLimitPartition.GetSlidingWindowLimiter(
                    user,
                    _ => new SlidingWindowRateLimiterOptions
                    {
                        PermitLimit = 500,
                        Window = TimeSpan.FromMinutes(1),
                        SegmentsPerWindow = 6
                    });
            }
            else
            {
                // Usuario anónimo: límite más bajo
                var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                return RateLimitPartition.GetFixedWindowLimiter(
                    ip,
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 50,
                        Window = TimeSpan.FromMinutes(1)
                    });
            }
        });

    // Handler cuando se excede el límite
    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.StatusCode = 429;
        context.HttpContext.Response.Headers["Retry-After"] = "60";

        await context.HttpContext.Response.WriteAsJsonAsync(new
        {
            error = "rate_limit_exceeded",
            message = "Too many requests. Please try again later.",
            retryAfter = 60
        }, cancellationToken: token);
    };
});

// Aplicar middleware
app.UseRateLimiter();
```

### Aplicar por Endpoint

```csharp
// En rutas específicas
app.MapPost("/api/auth/login", [EnableRateLimiting("login")]
    async (LoginRequest request) =>
{
    // Login logic
});

app.MapPost("/api/analysis/scan", [EnableRateLimiting("heavy-operations")]
    async (ScanRequest request) =>
{
    // Analysis logic
});
```

---

## 🛡️ Headers de Seguridad

### Implementación Completa

```csharp
app.Use(async (context, next) =>
{
    var headers = context.Response.Headers;

    // Prevenir MIME type sniffing
    headers["X-Content-Type-Options"] = "nosniff";

    // Protección contra clickjacking
    headers["X-Frame-Options"] = "DENY";

    // XSS Protection (legacy pero útil)
    headers["X-XSS-Protection"] = "1; mode=block";

    // HSTS - Forzar HTTPS
    if (!context.Request.IsHttps)
    {
        headers["Strict-Transport-Security"] =
            "max-age=31536000; includeSubDomains; preload";
    }

    // Content Security Policy
    headers["Content-Security-Policy"] = string.Join("; ", new[]
    {
        "default-src 'self'",
        "script-src 'self' 'unsafe-inline' 'unsafe-eval'",
        "style-src 'self' 'unsafe-inline'",
        "img-src 'self' data: https:",
        "font-src 'self'",
        "connect-src 'self'",
        "frame-ancestors 'none'",
        "base-uri 'self'",
        "form-action 'self'"
    });

    // Referrer Policy
    headers["Referrer-Policy"] = "strict-origin-when-cross-origin";

    // Permissions Policy (Feature Policy)
    headers["Permissions-Policy"] = string.Join(", ", new[]
    {
        "geolocation=()",
        "microphone=()",
        "camera=()",
        "payment=()",
        "usb=()",
        "magnetometer=()"
    });

    // Remove server header
    headers.Remove("Server");
    headers.Remove("X-Powered-By");

    await next();
});
```

---

## ✅ Validación de Input

### Sanitización de Requests

```csharp
public class InputSanitizationService
{
    private readonly ILogger<InputSanitizationService> _logger;

    public InputSanitizationService(ILogger<InputSanitizationService> logger)
    {
        _logger = logger;
    }

    public string SanitizeString(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        // Remover caracteres peligrosos
        var sanitized = input
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;")
            .Replace("'", "&#x27;")
            .Replace("/", "&#x2F;");

        // Limitar longitud
        const int maxLength = 1000;
        if (sanitized.Length > maxLength)
        {
            sanitized = sanitized.Substring(0, maxLength);
            _logger.LogWarning("Input truncated to {MaxLength} characters", maxLength);
        }

        return sanitized;
    }

    public bool IsValidUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return false;

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return false;

        // Solo HTTP/HTTPS
        return uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps;
    }

    public bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }
}
```

### Middleware de Validación

```csharp
public class InputValidationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<InputValidationMiddleware> _logger;

    public InputValidationMiddleware(
        RequestDelegate next,
        ILogger<InputValidationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Validar tamaño del body
        if (context.Request.ContentLength > 10 * 1024 * 1024) // 10MB
        {
            context.Response.StatusCode = 413;
            await context.Response.WriteAsJsonAsync(new
            {
                error = "payload_too_large",
                message = "Request body exceeds maximum size of 10MB"
            });
            return;
        }

        // Validar content type
        if (context.Request.Method == "POST" || context.Request.Method == "PUT")
        {
            var contentType = context.Request.ContentType;
            if (!IsAllowedContentType(contentType))
            {
                context.Response.StatusCode = 415;
                await context.Response.WriteAsJsonAsync(new
                {
                    error = "unsupported_media_type",
                    message = "Content-Type not supported"
                });
                return;
            }
        }

        await _next(context);
    }

    private bool IsAllowedContentType(string contentType)
    {
        if (string.IsNullOrEmpty(contentType))
            return false;

        var allowedTypes = new[]
        {
            "application/json",
            "application/x-www-form-urlencoded",
            "multipart/form-data"
        };

        return allowedTypes.Any(type =>
            contentType.StartsWith(type, StringComparison.OrdinalIgnoreCase));
    }
}
```

---

## 🐳 Seguridad en Docker

### Dockerfile Seguro

```dockerfile
# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build
WORKDIR /src

# Copiar solo archivos necesarios
COPY ["src/Gateway/Gateway.csproj", "Gateway/"]
COPY ["Directory.Packages.props", "./"]
RUN dotnet restore "Gateway/Gateway.csproj"

COPY src/ .
RUN dotnet build "Gateway/Gateway.csproj" -c Release --no-restore

# Publish stage
FROM build AS publish
RUN dotnet publish "Gateway/Gateway.csproj" -c Release \
    --no-build -o /app/publish --self-contained false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS final
WORKDIR /app

# Crear usuario no-root
RUN addgroup -S appgroup && adduser -S appuser -G appgroup

# Instalar dependencias mínimas
RUN apk add --no-cache curl

# Copiar archivos publicados
COPY --from=publish /app/publish .

# Configurar permisos
RUN chown -R appuser:appgroup /app

# Cambiar a usuario no-root
USER appuser

# Puerto
EXPOSE 8100

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD curl --fail http://localhost:8100/health/live || exit 1

ENTRYPOINT ["dotnet", "Gateway.dll"]
```

### Docker Compose Seguro

```yaml
version: "3.8"

services:
  accessibility-gateway:
    build:
      context: .
      dockerfile: Dockerfile
    container_name: accessibility-gw-prod

    # Security options
    security_opt:
      - no-new-privileges:true
    read_only: true

    # Tmpfs para temporales
    tmpfs:
      - /tmp
      - /app/logs

    # Capabilities mínimas
    cap_drop:
      - ALL
    cap_add:
      - NET_BIND_SERVICE

    # Resource limits
    deploy:
      resources:
        limits:
          cpus: "1.0"
          memory: 512M
        reservations:
          cpus: "0.5"
          memory: 256M

    # Environment
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - TZ=America/Mexico_City

    # Networks
    networks:
      - accessibility-shared

    # Health check
    healthcheck:
      test: ["CMD", "curl", "--fail", "http://localhost:8100/health/live"]
      interval: 30s
      timeout: 10s
      retries: 3
      start_period: 40s
```

---

## 📋 Mejores Prácticas

### Checklist de Seguridad

#### Autenticación y Autorización

- [ ] ✅ JWT con secret key segura (mínimo 256 bits)
- [ ] ✅ Tokens con expiración razonable (< 1 hora)
- [ ] ✅ Refresh tokens implementados
- [ ] ✅ Logout invalida tokens en servidor
- [ ] ✅ Políticas de autorización granulares

#### Rate Limiting

- [ ] ✅ Rate limiting global implementado
- [ ] ✅ Límites específicos para endpoints sensibles
- [ ] ✅ Diferentes límites para usuarios autenticados vs anónimos
- [ ] ✅ Respuestas 429 con Retry-After header

#### Headers de Seguridad

- [ ] ✅ Todos los security headers configurados
- [ ] ✅ HSTS habilitado en producción
- [ ] ✅ CSP configurada apropiadamente
- [ ] ✅ Headers de servidor removidos

#### Input Validation

- [ ] ✅ Validación de todos los inputs
- [ ] ✅ Sanitización de strings
- [ ] ✅ Límites de tamaño de requests
- [ ] ✅ Content-Type validation

#### Docker Security

- [ ] ✅ Contenedores ejecutan como no-root
- [ ] ✅ Filesystem read-only donde sea posible
- [ ] ✅ Capabilities mínimas
- [ ] ✅ Resource limits configurados
- [ ] ✅ Secrets no en código/Dockerfile

#### Logging y Monitoreo

- [ ] ✅ Audit trail de autenticación
- [ ] ✅ Logging de errores de seguridad
- [ ] ✅ Monitoreo de rate limiting
- [ ] ✅ Alertas de intentos de ataque

---

## 🔍 Testing de Seguridad

### Tests Automatizados

```csharp
[Fact]
public async Task Endpoint_RequiresAuthentication_Returns401()
{
    // Arrange
    var client = _factory.CreateClient();

    // Act
    var response = await client.GetAsync("/api/users/profile");

    // Assert
    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
}

[Fact]
public async Task RateLimit_Exceeded_Returns429()
{
    // Arrange
    var client = _factory.CreateClient();
    var endpoint = "/api/auth/login";

    // Act - Hacer más requests que el límite
    for (int i = 0; i < 10; i++)
    {
        await client.PostAsync(endpoint, new StringContent("{}"));
    }

    var response = await client.PostAsync(endpoint, new StringContent("{}"));

    // Assert
    Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
    Assert.True(response.Headers.Contains("Retry-After"));
}
```

### Herramientas de Análisis

```powershell
# OWASP Dependency Check
dotnet list package --vulnerable

# Security Audit
dotnet audit

# Docker security scan
docker scout cves accessibility-gw:latest
```

---

## 📚 Referencias

- [OWASP Top 10](https://owasp.org/www-project-top-ten/)
- [JWT Best Practices](https://datatracker.ietf.org/doc/html/rfc8725)
- [ASP.NET Core Security](https://learn.microsoft.com/en-us/aspnet/core/security/)
- [Docker Security](https://docs.docker.com/engine/security/)

---

[⬅️ Volver al README](../README.new.md)
