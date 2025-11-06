# ⚙️ Guía de Configuración

> Configuración completa del Gateway incluyendo variables de entorno, archivos de configuración y opciones avanzadas.

## 📋 Tabla de Contenidos

- [Variables de Entorno](#variables-de-entorno)
- [Archivos de Configuración](#archivos-de-configuración)
- [Configuración YARP](#configuración-yarp)
- [Configuración de Redis](#configuración-de-redis)
- [Configuración JWT](#configuración-jwt)
- [Rate Limiting](#rate-limiting)
- [CORS](#cors)

---

## 🔧 Variables de Entorno

### Setup Inicial

```bash
# 1. Copiar template
cp .env.example .env

# 2. Editar con tus valores
notepad .env  # Windows
```

### Variables por Categoría

#### 🚀 Aplicación Base

```env
# Entorno de ejecución
ASPNETCORE_ENVIRONMENT=Development

# URLs y puertos
ASPNETCORE_URLS=http://+:8100
ASPNETCORE_HTTP_PORTS=8100

# Gateway específico
GATEWAY_PORT=8100
GATEWAY_ENVIRONMENT=Development
GATEWAY_REQUEST_TIMEOUT_SECONDS=30
GATEWAY_MAX_REQUEST_BODY_SIZE=52428800
```

#### 🗄️ Redis Cache

```env
# Conexión Redis
REDIS_CONNECTION_STRING=localhost:6379
REDIS_DATABASE=0
REDIS_INSTANCE_NAME=AccessibilityGateway

# Configuración de caché
CACHE_DEFAULT_TTL_MINUTES=30
CACHE_ENABLED=true
CACHE_FALLBACK_TO_MEMORY=true
```

#### 🔐 JWT Autenticación

```env
# Configuración JWT
JWT_SECRET=tu-clave-secreta-muy-segura-aqui-minimo-32-caracteres
JWT_ISSUER=AccessibilityGateway
JWT_AUDIENCE=AccessibilityClients
JWT_EXPIRY_MINUTES=60
JWT_REFRESH_EXPIRY_DAYS=7

# Validación
JWT_VALIDATE_ISSUER=true
JWT_VALIDATE_AUDIENCE=true
JWT_VALIDATE_LIFETIME=true
JWT_CLOCK_SKEW_MINUTES=5
```

#### 🌐 Microservicios Backend

```env
# URLs internas de microservicios
GATE__SERVICES__USERS=http://msusers-api:8081
GATE__SERVICES__REPORTS=http://msreports-api:8083
GATE__SERVICES__ANALYSIS=http://msanalysis-api:8082
GATE__SERVICES__MIDDLEWARE=http://accessibility-mw:3001

# Health check endpoints
HEALTH_CHECK_USERS=/health
HEALTH_CHECK_REPORTS=/health
HEALTH_CHECK_ANALYSIS=/health
HEALTH_CHECK_MIDDLEWARE=/health
```

#### 🏥 Health Checks

```env
# Configuración de health checks
HEALTH_CHECK_INTERVAL=30
HEALTH_CHECK_TIMEOUT=10
HEALTH_CHECK_FAILURE_THRESHOLD=3
HEALTH_CHECK_SUCCESS_THRESHOLD=2
HEALTH_CHECK_ENABLED=true
```

#### 📊 Logging

```env
# Niveles: Trace, Debug, Information, Warning, Error, Critical
LOG_LEVEL=Information
LOG_FILE_PATH=logs/gateway.log
LOG_FILE_SIZE_LIMIT_BYTES=10485760
LOG_RETENTION_DAYS=7

# Serilog específico
SERILOG_MINIMUM_LEVEL=Information
SERILOG_WRITE_TO_CONSOLE=true
SERILOG_WRITE_TO_FILE=true
```

#### ⚡ Rate Limiting

```env
# Límites globales
RATE_LIMIT_REQUESTS_PER_MINUTE=100
RATE_LIMIT_BURST_SIZE=20
RATE_LIMIT_ENABLED=true

# Por endpoint
RATE_LIMIT_AUTH_REQUESTS_PER_MINUTE=10
RATE_LIMIT_ANALYSIS_REQUESTS_PER_MINUTE=50
```

#### 🌐 CORS

```env
# Orígenes permitidos (separados por coma)
CORS_ALLOWED_ORIGINS=http://localhost:3000,http://localhost:8100
CORS_ALLOWED_METHODS=GET,POST,PUT,DELETE,OPTIONS,PATCH
CORS_ALLOWED_HEADERS=*
CORS_ALLOW_CREDENTIALS=true
CORS_MAX_AGE_SECONDS=3600
```

#### ⏰ Timeouts y Circuit Breaker

```env
# Circuit breaker
CIRCUIT_BREAKER_FAILURE_THRESHOLD=5
CIRCUIT_BREAKER_TIMEOUT_SECONDS=30
CIRCUIT_BREAKER_RETRY_ATTEMPTS=3
CIRCUIT_BREAKER_BREAK_DURATION_SECONDS=60

# Timeouts
HTTP_CLIENT_TIMEOUT_SECONDS=30
PROXY_TIMEOUT_SECONDS=60
```

---

## 📄 Archivos de Configuración

### appsettings.json (Base)

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Yarp": "Information"
    }
  },
  "AllowedHosts": "*",
  "GateOptions": {
    "Services": {
      "Users": "http://localhost:8081",
      "Reports": "http://localhost:8083",
      "Analysis": "http://localhost:8082",
      "Middleware": "http://localhost:3001"
    },
    "Cache": {
      "DefaultTtlMinutes": 30,
      "Enabled": true
    }
  }
}
```

### appsettings.Development.json

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.AspNetCore": "Information",
      "Yarp": "Debug"
    }
  },
  "GateOptions": {
    "Services": {
      "Users": "http://localhost:8081",
      "Reports": "http://localhost:8083",
      "Analysis": "http://localhost:8082",
      "Middleware": "http://localhost:3001"
    },
    "Cache": {
      "FallbackToMemory": true
    }
  },
  "Redis": {
    "ConnectionString": "localhost:6379",
    "Database": 0
  }
}
```

### appsettings.Production.json

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "GateOptions": {
    "Services": {
      "Users": "http://msusers-api:8081",
      "Reports": "http://msreports-api:8083",
      "Analysis": "http://msanalysis-api:8082",
      "Middleware": "http://accessibility-mw:3001"
    },
    "Cache": {
      "FallbackToMemory": false
    }
  },
  "Redis": {
    "ConnectionString": "redis:6379",
    "Database": 0,
    "InstanceName": "AccessibilityGateway:"
  }
}
```

---

## 🎯 Configuración YARP

### Rutas Básicas

```json
{
  "ReverseProxy": {
    "Routes": {
      "users-route": {
        "ClusterId": "users-cluster",
        "Match": {
          "Path": "/api/v1/users/{**catch-all}"
        },
        "Transforms": [
          {
            "PathPattern": "/api/v1/users/{**catch-all}"
          },
          {
            "RequestHeader": "X-Gateway-Source",
            "Set": "accessibility-gw"
          }
        ]
      },
      "analysis-route": {
        "ClusterId": "analysis-cluster",
        "Match": {
          "Path": "/api/Analysis/{**catch-all}"
        }
      },
      "reports-route": {
        "ClusterId": "reports-cluster",
        "Match": {
          "Path": "/api/Report/{**catch-all}"
        }
      },
      "middleware-route": {
        "ClusterId": "middleware-cluster",
        "Match": {
          "Path": "/api/analyze/{**catch-all}"
        }
      }
    }
  }
}
```

### Clusters con Health Checks

```json
{
  "ReverseProxy": {
    "Clusters": {
      "users-cluster": {
        "Destinations": {
          "destination1": {
            "Address": "http://msusers-api:8081/"
          }
        },
        "HealthCheck": {
          "Active": {
            "Enabled": true,
            "Interval": "00:00:30",
            "Timeout": "00:00:05",
            "Policy": "ConsecutiveFailures",
            "Path": "/health"
          },
          "Passive": {
            "Enabled": true,
            "Policy": "TransportFailureRate",
            "ReactivationPeriod": "00:01:00"
          }
        },
        "HttpClient": {
          "DangerousAcceptAnyServerCertificate": false,
          "ActivityTimeout": "00:01:00",
          "RequestTimeout": "00:00:30"
        }
      }
    }
  }
}
```

---

## 💾 Configuración de Redis

### Configuración Básica en Program.cs

```csharp
// Redis como caché distribuido
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration["Redis:ConnectionString"];
    options.InstanceName = builder.Configuration["Redis:InstanceName"];
});

// Fallback a memoria local
builder.Services.AddMemoryCache();
```

### Configuración Avanzada

```csharp
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration["Redis:ConnectionString"];
    options.InstanceName = "AccessibilityGateway:";

    options.ConfigurationOptions = new ConfigurationOptions
    {
        EndPoints = { "redis:6379" },
        ConnectTimeout = 5000,
        SyncTimeout = 5000,
        AbortOnConnectFail = false,
        ConnectRetry = 3,
        KeepAlive = 60,
        DefaultDatabase = 0
    };
});
```

### Docker Compose Redis

```yaml
redis:
  image: redis:7-alpine
  command: >
    redis-server 
    --appendonly yes 
    --appendfsync everysec
    --maxmemory 256mb
    --maxmemory-policy allkeys-lru
    --tcp-keepalive 60
    --timeout 0
    --save 900 1 300 10
  ports:
    - "6379:6379"
  volumes:
    - redis-data:/data
  healthcheck:
    test: ["CMD", "redis-cli", "ping"]
    interval: 10s
    timeout: 5s
    retries: 3
```

---

## 🔐 Configuración JWT

### Configuración en Program.cs

```csharp
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
            ClockSkew = TimeSpan.FromMinutes(5)
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
            }
        };
    });
```

### Generar Secret Key

```powershell
# PowerShell - Generar clave segura
$bytes = New-Object Byte[] 32
[Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($bytes)
$secret = [Convert]::ToBase64String($bytes)
Write-Host "JWT_SECRET=$secret"
```

---

## ⚡ Rate Limiting

### Configuración Básica

```csharp
builder.Services.AddRateLimiter(options =>
{
    // Política global
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(
        httpContext => RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.User?.Identity?.Name
                ?? httpContext.Connection.RemoteIpAddress?.ToString()
                ?? "anonymous",
            factory: partition => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 100,
                QueueLimit = 10,
                Window = TimeSpan.FromMinutes(1)
            }));

    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.StatusCode = 429;
        await context.HttpContext.Response.WriteAsync(
            "Too many requests. Please try again later.", token);
    };
});
```

### Políticas por Endpoint

```csharp
builder.Services.AddRateLimiter(options =>
{
    // Autenticación - Estricta
    options.AddFixedWindowLimiter("auth", configureOptions =>
    {
        configureOptions.PermitLimit = 10;
        configureOptions.Window = TimeSpan.FromMinutes(1);
        configureOptions.QueueLimit = 0;
    });

    // API - Normal
    options.AddFixedWindowLimiter("api", configureOptions =>
    {
        configureOptions.PermitLimit = 100;
        configureOptions.Window = TimeSpan.FromMinutes(1);
        configureOptions.QueueLimit = 20;
    });

    // Premium - Alta capacidad
    options.AddSlidingWindowLimiter("premium", configureOptions =>
    {
        configureOptions.PermitLimit = 500;
        configureOptions.Window = TimeSpan.FromMinutes(1);
        configureOptions.SegmentsPerWindow = 6;
    });
});
```

---

## 🌐 CORS

### Configuración Básica

```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowedOrigins", policy =>
    {
        policy.WithOrigins(
                builder.Configuration["CORS:AllowedOrigins"]
                    .Split(',', StringSplitOptions.RemoveEmptyEntries))
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// Aplicar
app.UseCors("AllowedOrigins");
```

### Configuración Avanzada

```csharp
builder.Services.AddCors(options =>
{
    // Política estricta para producción
    options.AddPolicy("Production", policy =>
    {
        policy.WithOrigins("https://yourdomain.com", "https://www.yourdomain.com")
              .WithMethods("GET", "POST", "PUT", "DELETE")
              .WithHeaders("Authorization", "Content-Type", "X-Requested-With")
              .AllowCredentials()
              .SetPreflightMaxAge(TimeSpan.FromHours(1));
    });

    // Política permisiva para desarrollo
    options.AddPolicy("Development", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Usar según entorno
if (app.Environment.IsDevelopment())
{
    app.UseCors("Development");
}
else
{
    app.UseCors("Production");
}
```

---

## 🔧 Troubleshooting de Configuración

### Verificar Variables Cargadas

```powershell
# Ver todas las variables de configuración
.\manage-gateway.ps1 verify -Full

# Verificar configuración específica
dotnet run --project src/Gateway -- --urls http://localhost:8100
```

### Problemas Comunes

| Problema                | Causa                       | Solución                                  |
| ----------------------- | --------------------------- | ----------------------------------------- |
| **Redis no conecta**    | ConnectionString incorrecta | Verificar `REDIS_CONNECTION_STRING`       |
| **JWT inválido**        | Secret key incorrecta       | Regenerar con script                      |
| **CORS errors**         | Orígenes no permitidos      | Actualizar `CORS_ALLOWED_ORIGINS`         |
| **Rate limit muy bajo** | Configuración restrictiva   | Aumentar `RATE_LIMIT_REQUESTS_PER_MINUTE` |
| **Timeouts**            | Valores muy bajos           | Ajustar `*_TIMEOUT_SECONDS`               |

---

## 📚 Referencias

- [ASP.NET Core Configuration](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/)
- [YARP Configuration](https://microsoft.github.io/reverse-proxy/articles/config-files.html)
- [Redis Configuration](https://redis.io/docs/management/config/)
- [JWT Best Practices](https://datatracker.ietf.org/doc/html/rfc8725)

---

[⬅️ Volver al README](../README.new.md)
