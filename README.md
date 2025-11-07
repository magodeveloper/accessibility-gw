# 🚪 Accessibility Gateway

> **API Gateway empresarial avanzado desarrollado en .NET 9 que actúa como punto de entrada único para la plataforma de análisis de accesibilidad web. Proporciona enrutamiento inteligente con YARP, sistema de caché distribuido con Redis, autenticación JWT, rate limiting, monitoreo avanzado y gestión centralizada de microservicios.**

<div align="center">

[![.NET 9.0](https://img.shields.io/badge/.NET-9.0-512BD4?style=for-the-badge&logo=.net)](https://dotnet.microsoft.com/en-us/download/dotnet/9.0)
[![C# 13](https://img.shields.io/badge/C%23-13-239120?style=for-the-badge&logo=c-sharp)](https://docs.microsoft.com/en-us/dotnet/csharp/)
[![YARP 2.2](https://img.shields.io/badge/YARP-2.2-5C2D91?style=for-the-badge&logo=.net)](https://microsoft.github.io/reverse-proxy/)
[![Docker](https://img.shields.io/badge/Docker-Alpine-2496ED?style=for-the-badge&logo=docker)](https://www.docker.com/)
[![Redis 7.2](https://img.shields.io/badge/Redis-7.2-DC382D?style=for-the-badge&logo=redis)](https://redis.io/)

[![Tests](https://img.shields.io/badge/Tests-435_passing-brightgreen.svg?style=flat-square)](https://github.com/magodeveloper/accessibility-gw)
[![Coverage](https://img.shields.io/badge/Coverage-91.94%25-green.svg?style=flat-square)](https://github.com/magodeveloper/accessibility-gw)
[![Vulnerabilities](https://img.shields.io/badge/Vulnerabilities-0-brightgreen.svg?style=flat-square)](https://github.com/magodeveloper/accessibility-gw)
[![Build](https://img.shields.io/badge/CI%2FCD-Passing-green.svg?style=flat-square)](https://github.com/magodeveloper/accessibility-gw/actions)
[![License](https://img.shields.io/badge/License-Proprietary-yellow.svg?style=flat-square)](LICENSE)

</div>

---

## 📋 Tabla de Contenidos

- [✨ Características](#-características)
- [🏗️ Arquitectura](#️-arquitectura)
- [📚 Documentación Adicional](#-documentación-adicional)
- [🚀 Quick Start](#-quick-start)
- [📡 API Reference](#-api-reference)
- [💚 Health Checks](#-health-checks)
- [🔒 Seguridad](#-seguridad)
- [🧪 Testing](#-testing)
- [🐳 Docker & Deployment](#-docker--deployment)
- [📊 Monitoreo y Métricas](#-monitoreo-y-métricas)
- [🔄 CI/CD](#-cicd)
- [🛠️ Stack Tecnológico](#️-stack-tecnológico)
- [🛠️ Scripts de Gestión](#️-scripts-de-gestión)
- [🔧 Troubleshooting Rápido](#-troubleshooting-rápido)
- [🤝 Contribución](#-contribución)
- [📄 Licencia](#-licencia)

---

## ✨ Características

- ✅ **Routing inteligente** - YARP Reverse Proxy con load balancing
- ✅ **Cache distribuido** - Redis con fallback a memoria local
- ✅ **Autenticación JWT** - Sistema completo de tokens con validación
- ✅ **Rate Limiting** - Control de tráfico configurable por endpoint
- ✅ **Health Checks** - Monitoreo automático de microservicios
- ✅ **Métricas Prometheus** - Observabilidad completa con dashboards
- ✅ **Logging estructurado** - Serilog con correlación de requests
- ✅ **Security headers** - CORS, CSP, HSTS automáticos
- ✅ **Circuit Breaker** - Resiliencia con políticas Polly
- ✅ **High Performance** - Optimizado para alta concurrencia

---

## 🏗️ Arquitectura

```mermaid
graph TB
    Client[Cliente Web/Mobile] -->|HTTP/HTTPS| Gateway[API Gateway :8100]

    subgraph "Gateway Layer"
        Gateway --> Middleware[Middleware Pipeline]
        Middleware --> Auth[JWT Authentication]
        Middleware --> RateLimit[Rate Limiting]
        Middleware --> CORS[CORS Policy]
        Middleware --> Security[Security Headers]

        Auth --> Router[YARP Router]
        RateLimit --> Router
        CORS --> Router
        Security --> Router

        Router --> LoadBalancer[Load Balancer]
        LoadBalancer --> CircuitBreaker[Circuit Breaker]

        Gateway -.->|Cache Check| Redis[(Redis Cache<br/>:6379)]
        Gateway -.->|Metrics| Prometheus[Prometheus<br/>:9090]
        Gateway -.->|Logs| Serilog[Structured Logs]
    end

    subgraph "Microservices Layer"
        CircuitBreaker -->|Proxy| MSUsers[ms-users<br/>:8081]
        CircuitBreaker -->|Proxy| MSAnalysis[ms-analysis<br/>:8082]
        CircuitBreaker -->|Proxy| MSReports[ms-reports<br/>:8083]
        CircuitBreaker -->|Proxy| Middleware2[middleware<br/>:3001]

        MSUsers -.-> MySQL1[(MySQL<br/>Users DB)]
        MSAnalysis -.-> MySQL2[(MySQL<br/>Analysis DB)]
        MSReports -.-> MySQL3[(MySQL<br/>Reports DB)]
    end

    subgraph "Monitoring Layer"
        Prometheus --> Grafana[Grafana Dashboards<br/>:3000]
        Serilog --> Logs[Log Aggregation]
    end

    style Gateway fill:#512BD4,stroke:#333,stroke-width:3px,color:#fff
    style Redis fill:#DC382D,stroke:#333,stroke-width:2px,color:#fff
    style Prometheus fill:#E6522C,stroke:#333,stroke-width:2px,color:#fff
    style MSUsers fill:#2D9CDB,stroke:#333,stroke-width:2px,color:#fff
    style MSAnalysis fill:#27AE60,stroke:#333,stroke-width:2px,color:#fff
    style MSReports fill:#F2994A,stroke:#333,stroke-width:2px,color:#fff
    style Middleware2 fill:#9B51E0,stroke:#333,stroke-width:2px,color:#fff
```

### Componentes Principales

| Componente          | Responsabilidad                    | Tecnología           |
| ------------------- | ---------------------------------- | -------------------- |
| **YARP Proxy**      | Routing y forwarding de requests   | YARP 2.2+            |
| **Auth Module**     | Validación JWT y control de acceso | ASP.NET Identity     |
| **Cache Service**   | Cache distribuido con fallback     | StackExchange.Redis  |
| **Health Monitor**  | Health checks de microservicios    | ASP.NET HealthChecks |
| **Metrics Service** | Recolección de métricas            | Prometheus.NET       |
| **Logger**          | Logging estructurado               | Serilog              |
| **Circuit Breaker** | Resiliencia y fallback             | Polly                |

### Flujo de Request

```mermaid
sequenceDiagram
    participant Client
    participant Gateway
    participant Redis
    participant Auth
    participant Microservice
    participant MySQL

    Client->>Gateway: HTTP Request
    Gateway->>Gateway: Rate Limit Check
    alt Rate limit exceeded
        Gateway-->>Client: 429 Too Many Requests
    end

    Gateway->>Auth: Validate JWT Token
    alt Invalid token
        Auth-->>Gateway: 401 Unauthorized
        Gateway-->>Client: 401 Unauthorized
    end

    Gateway->>Redis: Check Cache
    alt Cache hit
        Redis-->>Gateway: Cached Response
        Gateway-->>Client: 200 OK (from cache)
    else Cache miss
        Gateway->>Microservice: Forward Request
        Microservice->>MySQL: Query Database
        MySQL-->>Microservice: Data
        Microservice-->>Gateway: Response
        Gateway->>Redis: Store in Cache
        Gateway-->>Client: 200 OK
    end
```

---

## 📚 Documentación Adicional

Para información técnica detallada, consulta la documentación especializada:

| Documento                                            | Descripción                                                                     |
| ---------------------------------------------------- | ------------------------------------------------------------------------------- |
| [🏗️ **ARCHITECTURE.md**](docs/ARCHITECTURE.md)       | Arquitectura técnica detallada, patrones de diseño y decisiones arquitectónicas |
| [⚙️ **CONFIGURATION.md**](docs/CONFIGURATION.md)     | Configuración completa: variables de entorno, appsettings, Redis y JWT          |
| [🔒 **SECURITY.md**](docs/SECURITY.md)               | JWT Authentication, Rate Limiting, CORS y Security Headers                      |
| [💾 **CACHE.md**](docs/CACHE.md)                     | Sistema de caché Redis: estrategias, fallback y optimización                    |
| [📡 **API.md**](docs/API.md)                         | Referencia completa de endpoints, request/response y códigos de error           |
| [🧪 **TESTING.md**](docs/TESTING.md)                 | Guía de testing: unit, integration, load testing con K6                         |
| [🐳 **DOCKER.md**](docs/DOCKER.md)                   | Docker Compose, multi-stage builds, networking y volumes                        |
| [📊 **MONITORING.md**](docs/MONITORING.md)           | Prometheus metrics, health checks, logging y Grafana dashboards                 |
| [🛠️ **SCRIPTS.md**](docs/SCRIPTS.md)                 | Documentación de scripts PowerShell de gestión                                  |
| [🔧 **TROUBLESHOOTING.md**](docs/TROUBLESHOOTING.md) | Solución de problemas comunes, logs y debugging                                 |
| [💻 **DEVELOPMENT.md**](docs/DEVELOPMENT.md)         | Guía completa para desarrolladores: setup, workflow, convenciones y debugging   |
| [🤝 **CONTRIBUTING.md**](CONTRIBUTING.md)            | Guía de contribución: reportar bugs, solicitar features, crear PRs              |
| [📝 **CHANGELOG.md**](CHANGELOG.md)                  | Historial de cambios y releases del proyecto                                    |

> 💡 **Tip:** Si eres nuevo en el proyecto, empieza por [DEVELOPMENT.md](docs/DEVELOPMENT.md) para el setup completo, luego consulta [ARCHITECTURE.md](docs/ARCHITECTURE.md) para entender el diseño técnico. Para contribuir, lee [CONTRIBUTING.md](CONTRIBUTING.md).

---

## 🚀 Quick Start

### Requisitos

- **.NET 9.0 SDK** (LTS)
- **Docker & Docker Compose** (para microservicios)
- **Redis 7.2+** (Docker lo provee)
- **PowerShell 7.4+** (para scripts de gestión)
- **Git**

### Instalación con Docker (Recomendado)

```bash
# 1. Clonar repositorio
git clone https://github.com/magodeveloper/accessibility-gw.git
cd accessibility-gw

# 2. Crear red Docker compartida
docker network create accessibility-shared

# 3. Configurar entorno
cp .env.example .env.development
# Editar .env.development con tus configuraciones

# 4. Iniciar servicios
docker compose -f docker-compose.dev.yml up -d

# 5. Verificar funcionamiento
curl http://localhost:8100/health
```

### Instalación Local

```bash
# 1. Clonar repositorio
git clone https://github.com/magodeveloper/accessibility-gw.git
cd accessibility-gw

# 2. Instalar dependencias
dotnet restore

# 3. Configurar entorno
cp .env.example .env.development

# 4. Iniciar Redis
docker run -d --name redis -p 6379:6379 redis:7-alpine

# 5. Compilar y ejecutar
dotnet build
dotnet run --project src/Gateway

# 6. Verificar
curl http://localhost:8100/health
```

### Desarrollo Local

```bash
# Modo desarrollo con hot-reload
dotnet watch run --project src/Gateway

# Ejecutar tests
.\manage-tests.ps1 test

# Ver cobertura
.\manage-tests.ps1 coverage -OpenReport

# Linting y verificación
dotnet format
```

### Verificación de Instalación

```bash
# Health check detallado
curl http://localhost:8100/health/ready

# Listar rutas disponibles
curl http://localhost:8100/api/routes

# Métricas Prometheus
curl http://localhost:8100/metrics

# Estado del cache
curl http://localhost:8100/cache/stats
```

---

## 📡 API Reference

### Endpoints Principales

| Método | Endpoint            | Descripción                      | Auth           |
| ------ | ------------------- | -------------------------------- | -------------- |
| GET    | `/health`           | Health check general             | No             |
| GET    | `/health/live`      | Liveness probe                   | No             |
| GET    | `/health/ready`     | Readiness probe                  | No             |
| GET    | `/metrics`          | Métricas Prometheus              | No             |
| GET    | `/cache/stats`      | Estadísticas de cache            | No             |
| POST   | `/api/v1/translate` | Traducir request a microservicio | Gateway Secret |

### Rutas Proxy

El Gateway hace proxy de las siguientes rutas a los microservicios:

| Prefijo               | Microservicio | Puerto | Descripción                  |
| --------------------- | ------------- | ------ | ---------------------------- |
| `/api/Auth/**`        | ms-users      | 8081   | Autenticación y autorización |
| `/api/users/**`       | ms-users      | 8081   | Gestión de usuarios          |
| `/api/preferences/**` | ms-users      | 8081   | Preferencias de usuario      |
| `/api/sessions/**`    | ms-users      | 8081   | Gestión de sesiones          |
| `/api/Analysis/**`    | ms-analysis   | 8082   | Análisis de accesibilidad    |
| `/api/Result/**`      | ms-analysis   | 8082   | Resultados de análisis       |
| `/api/Error/**`       | ms-analysis   | 8082   | Errores de accesibilidad     |
| `/api/Report/**`      | ms-reports    | 8083   | Generación de reportes       |
| `/api/History/**`     | ms-reports    | 8083   | Historial de reportes        |
| `/api/analyze/**`     | middleware    | 3001   | Análisis directo             |

> 📖 Para detalles completos de cada endpoint, consulta [API.md](docs/API.md)

---

## 💚 Health Checks

El Gateway proporciona tres endpoints de health checks diseñados para diferentes propósitos:

### Endpoints de Health

| Endpoint            | Propósito             | Uso                           |
| ------------------- | --------------------- | ----------------------------- |
| `GET /health`       | Health check completo | Monitoreo general del sistema |
| `GET /health/live`  | Liveness probe        | Kubernetes/Docker liveness    |
| `GET /health/ready` | Readiness probe       | Kubernetes/Docker readiness   |

### 1. Health Check Completo (`/health`)

Verifica el estado de todos los componentes del sistema:

```bash
curl http://localhost:8100/health
```

**Respuesta exitosa (200 OK):**

```json
{
  "status": "Healthy",
  "timestamp": "2025-11-06T10:30:00Z",
  "duration": "00:00:00.1234567",
  "checks": {
    "redis": "Healthy",
    "database": "Healthy",
    "microservices": {
      "ms-users": "Healthy",
      "ms-analysis": "Healthy",
      "ms-reports": "Healthy",
      "middleware": "Healthy"
    }
  },
  "resources": {
    "memoryUsed": "512 MB",
    "cpuUsage": "15%",
    "uptime": "2d 5h 30m"
  }
}
```

**Respuesta degradada (503 Service Unavailable):**

```json
{
  "status": "Unhealthy",
  "timestamp": "2025-11-06T10:30:00Z",
  "checks": {
    "redis": "Unhealthy",
    "microservices": {
      "ms-users": "Degraded"
    }
  }
}
```

### 2. Liveness Probe (`/health/live`)

Indica si el Gateway está vivo y puede procesar requests:

```bash
curl http://localhost:8100/health/live
```

**Respuesta (200 OK):**

```json
{
  "status": "Healthy",
  "message": "Gateway is alive"
}
```

**Uso en Kubernetes:**

```yaml
livenessProbe:
  httpGet:
    path: /health/live
    port: 8080
  initialDelaySeconds: 10
  periodSeconds: 30
  timeoutSeconds: 5
  failureThreshold: 3
```

### 3. Readiness Probe (`/health/ready`)

Indica si el Gateway está listo para recibir tráfico:

```bash
curl http://localhost:8100/health/ready
```

**Respuesta lista (200 OK):**

```json
{
  "status": "Healthy",
  "message": "Gateway is ready to serve traffic",
  "dependencies": {
    "redis": "Connected",
    "microservices": "Available"
  }
}
```

**Respuesta no lista (503 Service Unavailable):**

```json
{
  "status": "Unhealthy",
  "message": "Gateway is not ready",
  "dependencies": {
    "redis": "Disconnected",
    "microservices": "Unavailable"
  }
}
```

**Uso en Kubernetes:**

```yaml
readinessProbe:
  httpGet:
    path: /health/ready
    port: 8080
  initialDelaySeconds: 15
  periodSeconds: 10
  timeoutSeconds: 5
  failureThreshold: 3
```

### Monitoreo Automatizado

```bash
# Script de monitoreo continuo
watch -n 5 'curl -s http://localhost:8100/health | jq'

# PowerShell
while ($true) {
    Invoke-RestMethod http://localhost:8100/health | ConvertTo-Json
    Start-Sleep -Seconds 5
}

# Verificar health en Docker
docker inspect --format='{{json .State.Health}}' accessibility-gateway | jq
```

### Health Check con Docker Compose

```yaml
# docker-compose.yml
services:
  accessibility-gateway:
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:8080/health/live"]
      interval: 30s
      timeout: 10s
      retries: 3
      start_period: 40s
```

---

## 🔒 Seguridad

### Autenticación JWT

El Gateway valida tokens JWT Bearer en cada request protegido:

```bash
# Header requerido
Authorization: Bearer eyJhbGciOiJIUzI1NiIs...
```

**Token JWT Payload:**

```json
{
  "sub": "user-123",
  "email": "user@example.com",
  "iss": "https://api.accessibility.company.com",
  "aud": "accessibility-app",
  "exp": 1730897400,
  "roles": ["user", "analyzer"]
}
```

**Configuración JWT (appsettings.json):**

```json
{
  "Jwt": {
    "SecretKey": "<64-char-secret>",
    "Issuer": "https://api.accessibility.company.com",
    "Audience": "accessibility-app",
    "ExpirationMinutes": 60
  }
}
```

### Gateway Secret Validation

El Gateway valida que las peticiones a microservicios internos incluyan un **shared secret**:

```bash
# Header X-Gateway-Secret requerido para requests internos
X-Gateway-Secret: <GATEWAY_SECRET_VALUE>
```

**Funcionamiento:**

1. El Gateway valida el JWT del cliente
2. El Gateway agrega el header `X-Gateway-Secret` antes de hacer proxy
3. El microservicio valida el Gateway Secret
4. Si el secret no coincide → 403 Forbidden

**Configuración (appsettings.json):**

```json
{
  "Gateway": {
    "SecretKey": "<64-char-gateway-secret>",
    "ValidationEnabled": true
  }
}
```

**Endpoints exceptuados** (no requieren Gateway Secret):

- `GET /health` - Health check general
- `GET /health/live` - Liveness probe
- `GET /health/ready` - Readiness probe
- `GET /metrics` - Métricas Prometheus

> ⚠️ **Importante**: El `GATEWAY_SECRET` debe coincidir exactamente entre el Gateway y todos los microservicios. En producción, este valor **DEBE** ser único, seguro y de al menos 64 caracteres.

### Generar Secretos Seguros

```powershell
# PowerShell - Generar JWT Secret
.\Generate-JwtSecretKey.ps1

# Generar manualmente con Node.js
node -e "console.log(require('crypto').randomBytes(64).toString('base64'))"

# Generar con .NET
dotnet run --project src/Tools/SecretGenerator
```

### Rate Limiting

Protección contra abuso de API:

```json
{
  "RateLimiting": {
    "PermitLimit": 100,
    "Window": "00:01:00",
    "QueueLimit": 10
  }
}
```

**Configuración por endpoint:**

- Endpoints públicos: 100 requests/minuto
- Endpoints autenticados: 1000 requests/minuto
- Análisis: 10 requests/minuto (operación costosa)

### CORS Policy

```json
{
  "Cors": {
    "AllowedOrigins": [
      "https://app.accessibility.company.com",
      "https://admin.accessibility.company.com"
    ],
    "AllowedMethods": ["GET", "POST", "PUT", "DELETE", "OPTIONS"],
    "AllowedHeaders": ["Content-Type", "Authorization", "X-Request-ID"],
    "AllowCredentials": true,
    "MaxAge": 86400
  }
}
```

### Security Headers

El Gateway agrega automáticamente headers de seguridad:

```
X-Content-Type-Options: nosniff
X-Frame-Options: DENY
X-XSS-Protection: 1; mode=block
Strict-Transport-Security: max-age=31536000; includeSubDomains
Content-Security-Policy: default-src 'self'
Referrer-Policy: strict-origin-when-cross-origin
```

### Mejores Prácticas

1. **Secretos**: NUNCA commitear `.env.development` o `.env.production`
2. **Rotación**: Rotar JWT y Gateway secrets cada 90 días
3. **HTTPS**: Usar HTTPS en producción (certificados SSL/TLS)
4. **Logs**: No loguear tokens o secrets en producción
5. **Secrets Management**: Usar Azure Key Vault, AWS Secrets Manager o similar

> 📖 Para guía completa de seguridad, consulta [SECURITY.md](docs/SECURITY.md)

---

## 🧪 Testing

### Estado de Cobertura

**Estado General:** ✅ 435/435 tests exitosos (100%)  
**Cobertura Total:** 91.94% (líneas), 90.51% (branches)

| Categoría             | Tests | Cobertura | Estado |
| --------------------- | ----- | --------- | ------ |
| **Unit Tests**        | 423   | 92.1%     | ✅     |
| **Integration Tests** | 12    | 88.3%     | ✅     |
| **Load Tests (K6)**   | 6     | N/A       | ✅     |

### Desglose por Proyecto

| Proyecto        | Tests | Cobertura | Líneas      | Branches    |
| --------------- | ----- | --------- | ----------- | ----------- |
| **Gateway**     | 108   | 92.78%    | 93.21% / 97 | 91.67% / 12 |
| **ms-users**    | 435   | 91.94%    | 91.94% / 97 | 90.51% / 97 |
| **ms-analysis** | TBD   | TBD       | TBD         | TBD         |
| **ms-reports**  | TBD   | TBD       | TBD         | TBD         |

### Ejecutar Tests

#### Con Script PowerShell (Recomendado)

```bash
# Tests completos con cobertura y dashboard
.\manage-tests.ps1 full -OpenDashboard

# Solo tests unitarios
.\manage-tests.ps1 test

# Solo cobertura
.\manage-tests.ps1 coverage -OpenReport

# Tests + cobertura + reporte
.\manage-tests.ps1 full

# Limpiar resultados anteriores
.\manage-tests.ps1 clean
```

#### Con dotnet CLI

```bash
# Todos los tests con cobertura
dotnet test --collect:"XPlat Code Coverage" --results-directory ./TestResults

# Solo unit tests
dotnet test --filter "FullyQualifiedName~UnitTests"

# Tests de integración
dotnet test --filter "FullyQualifiedName~IntegrationTests"

# Tests con verbosidad
dotnet test --logger "console;verbosity=detailed"

# Generar reporte de cobertura HTML
reportgenerator \
  -reports:TestResults/**/coverage.cobertura.xml \
  -targetdir:coverage \
  -reporttypes:Html
```

### Load Testing con K6

```bash
# Navegar a directorio de load tests
cd src/tests/Gateway.Load

# Test de humo (smoke test)
.\manage-load-tests.ps1 smoke

# Test de carga normal
.\manage-load-tests.ps1 load

# Test de stress
.\manage-load-tests.ps1 stress

# Test de spike
.\manage-load-tests.ps1 spike

# Todos los escenarios
.\manage-load-tests.ps1 all
```

**Escenarios de Load Testing:**

| Escenario | VUs | Duración | Requests/s | Objetivo                  |
| --------- | --- | -------- | ---------- | ------------------------- |
| Smoke     | 1   | 30s      | ~10        | Verificar funcionalidad   |
| Load      | 50  | 5m       | ~500       | Carga normal              |
| Stress    | 200 | 10m      | ~2000      | Encontrar límites         |
| Spike     | 500 | 2m       | ~5000      | Picos repentinos de carga |
| Soak      | 100 | 30m      | ~1000      | Estabilidad a largo plazo |

### Dashboard de Tests

El script `manage-tests.ps1` genera un dashboard HTML interactivo con:

- ✅ Resumen de tests (passing/failing)
- 📊 Gráficos de cobertura por proyecto
- 📈 Tendencias históricas
- 🔍 Detalles por archivo y clase
- ⚡ Performance de tests

```bash
# Abrir dashboard automáticamente
.\manage-tests.ps1 full -OpenDashboard

# O abrir manualmente
Start-Process test-dashboard.html
```

### Coverage Targets

| Métrica   | Target | Actual |
| --------- | ------ | ------ |
| Lines     | >90%   | 91.94% |
| Branches  | >85%   | 90.51% |
| Functions | >90%   | TBD    |

### Verificación Continua

```bash
# Pre-commit hook (ejecutar antes de commit)
.\manage-tests.ps1 full

# Verificar que no haya regresión
git diff master --stat

# CI/CD ejecuta automáticamente en push
git push origin feature/my-feature
```

> 📖 Para guía completa de testing, consulta [TESTING.md](docs/TESTING.md)

---

## 🐳 Docker & Deployment

### Docker Compose

```bash
# Desarrollo
docker compose -f docker-compose.dev.yml up -d

# Producción
docker compose up -d

# Ver logs
docker compose logs -f accessibility-gateway

# Detener servicios
docker compose down
```

### Build Manual

```bash
# Build imagen
docker build -t accessibility-gateway:latest .

# Run contenedor
docker run -d \
  --name gateway \
  -p 8100:8080 \
  --network accessibility-shared \
  -e ASPNETCORE_ENVIRONMENT=Production \
  accessibility-gateway:latest
```

### Verificación de Deploy

```bash
# Estado de contenedores
docker ps | grep accessibility

# Health check
curl http://localhost:8100/health/ready

# Logs en tiempo real
docker logs -f accessibility-gateway
```

> 📖 Para guía completa de Docker, consulta [DOCKER.md](docs/DOCKER.md)

---

## 📊 Monitoreo y Métricas

### Prometheus Metrics

El Gateway expone métricas en formato Prometheus:

```bash
# Obtener todas las métricas
curl http://localhost:8100/metrics
```

**Métricas principales:**

```prometheus
# Request rate
http_requests_total{method="GET",endpoint="/api/users",status="200"} 1543

# Request duration (histograma)
http_request_duration_seconds_bucket{endpoint="/api/users",le="0.1"} 1420
http_request_duration_seconds_bucket{endpoint="/api/users",le="0.5"} 1530
http_request_duration_seconds_sum{endpoint="/api/users"} 156.3
http_request_duration_seconds_count{endpoint="/api/users"} 1543

# Cache hits/misses
cache_hits_total{cache="redis"} 8234
cache_misses_total{cache="redis"} 423

# Circuit breaker state
circuit_breaker_state{service="ms-users"} 0  # 0=Closed, 1=Open, 2=HalfOpen

# Active connections
active_connections{protocol="http"} 45

# Memory usage
process_working_set_bytes 524288000

# Gateway uptime
process_start_time_seconds 1699267200
```

### Stack de Monitoreo

```bash
# Iniciar stack completo (Prometheus + Grafana)
.\manage-monitoring.ps1 start

# Ver estado
.\manage-monitoring.ps1 status

# Detener stack
.\manage-monitoring.ps1 stop

# Ver logs
.\manage-monitoring.ps1 logs
```

**URLs de acceso:**

- Prometheus: http://localhost:9090
- Grafana: http://localhost:3000 (admin/admin)
- Alertmanager: http://localhost:9093

### Dashboards Grafana

El proyecto incluye dashboards pre-configurados:

1. **Gateway Overview** - Métricas generales del gateway
2. **Request Analytics** - Análisis de requests por endpoint
3. **Cache Performance** - Estadísticas de Redis
4. **Microservices Health** - Estado de servicios downstream
5. **Error Tracking** - Monitoreo de errores 4xx/5xx

**Importar dashboards:**

```bash
# Dashboards en ./monitoring/grafana/dashboards/
# Se importan automáticamente al iniciar Grafana
```

### Logging Estructurado

**Configuración Serilog (appsettings.json):**

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "System": "Warning"
      }
    },
    "WriteTo": [
      {
        "Name": "Console",
        "Args": {
          "outputTemplate": "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}"
        }
      },
      {
        "Name": "File",
        "Args": {
          "path": "logs/gateway-.log",
          "rollingInterval": "Day",
          "retainedFileCountLimit": 7,
          "outputTemplate": "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}"
        }
      }
    ]
  }
}
```

**Logs estructurados con correlación:**

```json
{
  "timestamp": "2025-11-06T10:30:15.123Z",
  "level": "Information",
  "messageTemplate": "HTTP {Method} {Path} responded {StatusCode} in {Elapsed}ms",
  "properties": {
    "Method": "GET",
    "Path": "/api/users",
    "StatusCode": 200,
    "Elapsed": 45.3,
    "RequestId": "0HN7FKQVQP4KT:00000001",
    "UserId": "user-123",
    "ClientIp": "192.168.1.100"
  }
}
```

**Ver logs en tiempo real:**

```bash
# Docker logs
docker compose logs -f accessibility-gateway

# Logs del archivo
tail -f logs/gateway-20251106.log

# Filtrar por nivel
docker compose logs | grep "ERROR"

# Buscar por RequestId
docker compose logs | grep "0HN7FKQVQP4KT"
```

### Alertas

**Configuración de alertas en Prometheus (prometheus/alerts.yml):**

```yaml
groups:
  - name: gateway_alerts
    interval: 30s
    rules:
      - alert: HighErrorRate
        expr: rate(http_requests_total{status=~"5.."}[5m]) > 0.05
        for: 5m
        labels:
          severity: critical
        annotations:
          summary: "High error rate detected"
          description: "Error rate is {{ $value }} requests/sec"

      - alert: ServiceDown
        expr: up{job="gateway"} == 0
        for: 1m
        labels:
          severity: critical
        annotations:
          summary: "Gateway is down"

      - alert: HighLatency
        expr: http_request_duration_seconds_bucket{le="1.0"} / http_request_duration_seconds_count < 0.95
        for: 5m
        labels:
          severity: warning
        annotations:
          summary: "High latency detected"
```

> 📖 Para guía completa de monitoreo, consulta [MONITORING.md](docs/MONITORING.md)

---

## 🔄 CI/CD

### GitHub Actions

El proyecto usa GitHub Actions para CI/CD automatizado:

#### Workflow: Build & Test (`.github/workflows/ci.yml`)

```yaml
name: CI Pipeline

on:
  push:
    branches: [master, develop]
  pull_request:
    branches: [master]

jobs:
  build-and-test:
    runs-on: ubuntu-latest

    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET 9
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: "9.0.x"

      - name: Restore dependencies
        run: dotnet restore

      - name: Build
        run: dotnet build --no-restore --configuration Release

      - name: Run tests
        run: dotnet test --no-build --configuration Release --collect:"XPlat Code Coverage"

      - name: Upload coverage to Codecov
        uses: codecov/codecov-action@v3
        with:
          files: ./TestResults/**/coverage.cobertura.xml

      - name: Generate coverage report
        run: |
          dotnet tool install -g dotnet-reportgenerator-globaltool
          reportgenerator -reports:TestResults/**/coverage.cobertura.xml -targetdir:coverage -reporttypes:Html

      - name: Upload coverage report
        uses: actions/upload-artifact@v3
        with:
          name: coverage-report
          path: coverage/
```

#### Workflow: Docker Build (`.github/workflows/docker.yml`)

```yaml
name: Docker Build & Push

on:
  push:
    branches: [master]
    tags: ["v*"]

jobs:
  docker:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - name: Docker meta
        id: meta
        uses: docker/metadata-action@v5
        with:
          images: magodeveloper/accessibility-gateway
          tags: |
            type=ref,event=branch
            type=semver,pattern={{version}}
            type=sha

      - name: Login to Docker Hub
        uses: docker/login-action@v3
        with:
          username: ${{ secrets.DOCKER_USERNAME }}
          password: ${{ secrets.DOCKER_PASSWORD }}

      - name: Build and push
        uses: docker/build-push-action@v5
        with:
          context: .
          push: true
          tags: ${{ steps.meta.outputs.tags }}
          cache-from: type=registry,ref=magodeveloper/accessibility-gateway:latest
          cache-to: type=inline
```

### Pre-commit Hooks

```bash
# Instalar hooks
git config core.hooksPath .githooks

# O copiar manualmente
cp .githooks/pre-commit .git/hooks/pre-commit
chmod +x .git/hooks/pre-commit
```

**Pre-commit script (`.githooks/pre-commit`):**

```bash
#!/bin/bash
echo "Running pre-commit checks..."

# Format code
dotnet format

# Run tests
dotnet test --no-build

# Check coverage threshold
dotnet test --collect:"XPlat Code Coverage" /p:Threshold=90

if [ $? -ne 0 ]; then
    echo "❌ Pre-commit checks failed"
    exit 1
fi

echo "✅ Pre-commit checks passed"
```

### Comandos CI/CD

```bash
# Verificación local completa (simula CI)
.\manage-tests.ps1 full

# Build de producción
dotnet build -c Release

# Publicar aplicación
dotnet publish -c Release -o ./publish

# Crear imagen Docker con tag
docker build -t accessibility-gateway:v1.0.0 .

# Push a registry
docker push magodeveloper/accessibility-gateway:v1.0.0
```

### Deployment

```bash
# Deploy a Azure Container Instances
az container create \
  --resource-group accessibility-rg \
  --name accessibility-gateway \
  --image magodeveloper/accessibility-gateway:latest \
  --ports 8080 \
  --environment-variables \
    ASPNETCORE_ENVIRONMENT=Production \
    JWT_SECRET_KEY=$JWT_SECRET

# Deploy a AWS ECS
aws ecs update-service \
  --cluster accessibility-cluster \
  --service gateway-service \
  --force-new-deployment

# Deploy a Kubernetes
kubectl apply -f k8s/deployment.yml
kubectl rollout status deployment/accessibility-gateway
```

> 📖 Para guía completa de CI/CD, consulta documentación de GitHub Actions en `.github/workflows/`

---

## 🛠️ Stack Tecnológico

### Core

- **.NET 9.0.0** - Framework principal (LTS)
- **C# 13** - Lenguaje de programación
- **ASP.NET Core 9.0** - Web framework
- **YARP 2.2.0** - Yet Another Reverse Proxy

### Librerías Principales

#### Proxy y Routing

- **Yarp.ReverseProxy 2.2.0** - Reverse proxy engine
- **Microsoft.AspNetCore.HttpOverrides 2.2.0** - Proxy headers

#### Cache y Persistencia

- **StackExchange.Redis 2.8.16** - Cliente Redis
- **Microsoft.Extensions.Caching.Memory 9.0.0** - In-memory cache fallback
- **Microsoft.Extensions.Caching.StackExchangeRedis 9.0.0** - Redis cache provider

#### Autenticación y Seguridad

- **Microsoft.AspNetCore.Authentication.JwtBearer 9.0.0** - JWT authentication
- **System.IdentityModel.Tokens.Jwt 8.2.1** - JWT token handling
- **BCrypt.Net-Next 4.0.3** - Password hashing

#### Logging y Monitoreo

- **Serilog.AspNetCore 8.0.3** - Structured logging
- **Serilog.Sinks.Console 6.0.0** - Console logging
- **Serilog.Sinks.File 6.0.0** - File logging
- **prometheus-net 8.2.1** - Prometheus metrics
- **prometheus-net.AspNetCore 8.2.1** - ASP.NET Core metrics

#### Resiliencia

- **Polly 8.5.0** - Resilience and transient-fault-handling
- **Polly.Extensions.Http 3.0.0** - HTTP client integration
- **Microsoft.Extensions.Http.Polly 9.0.0** - ASP.NET Core integration

#### Validación y Serialización

- **FluentValidation 11.11.0** - Fluent validation
- **FluentValidation.AspNetCore 11.3.0** - ASP.NET Core integration
- **Newtonsoft.Json 13.0.3** - JSON serialization

#### Documentación

- **Swashbuckle.AspNetCore 7.2.0** - Swagger/OpenAPI
- **Swashbuckle.AspNetCore.Annotations 7.2.0** - API annotations

#### Health Checks

- **Microsoft.Extensions.Diagnostics.HealthChecks 9.0.0** - Health check framework
- **AspNetCore.HealthChecks.Redis 9.0.1** - Redis health check
- **AspNetCore.HealthChecks.UI 9.0.0** - Health check UI

### Testing Stack

#### Test Frameworks

- **xUnit 2.9.2** - Unit testing framework
- **xUnit.runner.visualstudio 2.8.2** - Visual Studio test runner
- **Microsoft.NET.Test.Sdk 17.12.0** - Test SDK

#### Mocking y Assertions

- **NSubstitute 5.3.0** - Mocking library
- **FluentAssertions 7.0.0** - Fluent assertion library
- **Moq 4.20.72** - Alternative mocking library

#### Coverage

- **coverlet.collector 6.0.2** - Code coverage collector
- **coverlet.msbuild 6.0.2** - MSBuild coverage integration
- **ReportGenerator 5.4.1** - Coverage report generator

#### Integration Testing

- **Microsoft.AspNetCore.Mvc.Testing 9.0.0** - Integration test support
- **Microsoft.EntityFrameworkCore.InMemory 9.0.0** - In-memory database for tests
- **Testcontainers 4.2.0** - Docker containers for tests

#### Load Testing

- **k6 0.54.0** - Load testing tool
- **Artillery 2.0.20** - Alternative load testing

### Infrastructure

#### Containerization

- **Docker 24.0+** - Container runtime
- **Docker Compose 2.23+** - Multi-container orchestration

#### Cache y Storage

- **Redis 7.2-alpine** - In-memory cache
- **MySQL 8.4** - Database (para microservicios)

#### Monitoreo

- **Prometheus 2.54.1** - Metrics collection
- **Grafana 11.3.1** - Metrics visualization
- **Alertmanager 0.27.0** - Alert management

### Development Tools

- **PowerShell 7.4+** - Scripting y automatización
- **Git 2.43+** - Control de versiones
- **Visual Studio 2022** - IDE (17.8+)
- **Visual Studio Code** - Editor ligero
- **JetBrains Rider 2024.3** - IDE alternativo
- **Postman 11.0+** - API testing
- **Docker Desktop 4.25+** - Docker para Windows/Mac

### CI/CD

- **GitHub Actions** - CI/CD automation
- **Docker Hub** - Container registry
- **Azure DevOps** - Alternative CI/CD (opcional)

### Versiones Mínimas Requeridas

| Herramienta       | Versión Mínima | Recomendada |
| ----------------- | -------------- | ----------- |
| .NET SDK          | 9.0.0          | 9.0.100     |
| Docker            | 24.0           | 27.4.0      |
| Docker Compose    | 2.23           | 2.31.0      |
| Redis             | 7.0            | 7.2         |
| PowerShell        | 7.4            | 7.4.6       |
| Node.js (para K6) | 20.0           | 20.19.2     |
| Git               | 2.40           | 2.47.1      |

---

## 🛠️ Scripts de Gestión

El proyecto incluye scripts PowerShell para facilitar la gestión:

| Script                  | Descripción                     |
| ----------------------- | ------------------------------- |
| `manage-tests.ps1`      | Gestión de tests y cobertura    |
| `manage-monitoring.ps1` | Gestión del stack de monitoreo  |
| `manage-network.ps1`    | Gestión de red Docker           |
| `cleanup-project.ps1`   | Limpieza de archivos temporales |

### Ejemplos de Uso

```bash
# Tests completos con dashboard
.\manage-tests.ps1 full -OpenDashboard

# Iniciar monitoreo (Prometheus + Grafana)
.\manage-monitoring.ps1 start

# Verificar red Docker
.\manage-network.ps1 check

# Limpiar proyecto
.\cleanup-project.ps1 -All
```

> 📖 Para documentación completa de scripts, consulta [SCRIPTS.md](docs/SCRIPTS.md)

---

## 🔧 Troubleshooting

### Problemas Comunes

| Problema               | Solución                                                   |
| ---------------------- | ---------------------------------------------------------- |
| Puerto 8100 en uso     | `docker compose down` y verificar procesos                 |
| Redis no conecta       | Verificar `docker ps` y reiniciar contenedor               |
| Health checks fallando | Verificar microservicios con `.\manage-network.ps1 status` |
| Tests fallando         | Limpiar y rebuildar: `dotnet clean && dotnet build`        |

### Logs y Debugging

```bash
# Ver logs del Gateway
docker compose logs -f accessibility-gateway

# Ver logs de Redis
docker compose logs redis

# Verificar estado completo
.\manage-monitoring.ps1 status
```

> 📖 Para guía completa de troubleshooting, consulta [TROUBLESHOOTING.md](docs/TROUBLESHOOTING.md)

---

## 🔧 Troubleshooting Rápido

### Error 403: Gateway Secret Inválido

**Síntoma:** `"error": { "code": "INVALID_GATEWAY_SECRET" }`

**Solución:**

```bash
# 1. Verificar secret en .env.development
cat .env.development | grep GATEWAY_SECRET

# 2. Verificar configuración en appsettings.Development.json
cat src/Gateway/appsettings.Development.json | grep GatewaySecret

# 3. Verificar que el secret coincida en todos los microservicios
docker compose exec ms-users env | grep GATEWAY_SECRET
docker compose exec ms-analysis env | grep GATEWAY_SECRET
docker compose exec ms-reports env | grep GATEWAY_SECRET

# 4. Reiniciar contenedores para aplicar cambios
docker compose down
docker compose -f docker-compose.dev.yml up -d

# 5. Verificar logs
docker compose logs accessibility-gateway | grep "Gateway"
```

### Error 401: Token JWT Inválido

**Síntoma:** `401 Unauthorized` en requests protegidos

**Solución:**

```bash
# 1. Verificar que el endpoint requiera autenticación
curl http://localhost:8100/api/users
# Si devuelve 401, el endpoint está protegido correctamente

# 2. Obtener token válido haciendo login
$response = Invoke-RestMethod -Method POST -Uri "http://localhost:8100/api/Auth/login" `
  -ContentType "application/json" `
  -Body '{"email":"admin@example.com","password":"Admin123!"}'

$token = $response.token

# 3. Usar token en request
curl -H "Authorization: Bearer $token" http://localhost:8100/api/users

# 4. Verificar que el token no haya expirado
# Decodificar token en https://jwt.io

# 5. Verificar configuración JWT
cat src/Gateway/appsettings.Development.json | grep -A 5 "Jwt"
```

### Puerto 8100 Ya en Uso

**Síntoma:** `Error: address already in use: 0.0.0.0:8100`

**Solución:**

```bash
# Windows PowerShell
Get-Process -Id (Get-NetTCPConnection -LocalPort 8100).OwningProcess
Stop-Process -Id <PID>

# O detener todos los contenedores
docker compose down
docker compose -f docker-compose.dev.yml down

# Verificar que el puerto esté libre
netstat -ano | findstr :8100
```

### Redis No Conecta

**Síntoma:** `Error connecting to Redis` en logs

**Solución:**

```bash
# 1. Verificar que Redis esté corriendo
docker ps | grep redis

# 2. Si no está corriendo, iniciarlo
docker compose -f docker-compose.dev.yml up -d redis

# 3. Verificar conexión manual
docker exec -it redis redis-cli ping
# Debe responder: PONG

# 4. Verificar configuración de conexión
cat .env.development | grep REDIS

# 5. Reiniciar Gateway
docker compose restart accessibility-gateway

# 6. Verificar logs
docker compose logs redis
docker compose logs accessibility-gateway | grep "Redis"
```

### Health Checks Fallando

**Síntoma:** `GET /health/ready` devuelve 503 Service Unavailable

**Solución:**

```bash
# 1. Verificar estado completo
curl http://localhost:8100/health | jq

# 2. Verificar cada microservicio individualmente
curl http://localhost:8081/health  # ms-users
curl http://localhost:8082/health  # ms-analysis
curl http://localhost:8083/health  # ms-reports
curl http://localhost:3001/health  # middleware

# 3. Verificar red Docker
docker network inspect accessibility-shared

# 4. Verificar que todos los servicios estén UP
.\manage-network.ps1 status

# 5. Reiniciar servicios que fallen
docker compose restart ms-users
docker compose restart accessibility-gateway
```

### Tests Fallando

**Síntoma:** Tests que pasaban ahora fallan

**Solución:**

```bash
# 1. Limpiar y rebuildar
dotnet clean
dotnet build

# 2. Limpiar resultados de tests anteriores
.\manage-tests.ps1 clean

# 3. Ejecutar tests con verbosidad
dotnet test --logger "console;verbosity=detailed"

# 4. Verificar dependencias
dotnet restore

# 5. Verificar que no haya archivos bloqueados
Get-Process | Where-Object {$_.Path -like "*accessibility-gw*"} | Stop-Process -Force

# 6. Ejecutar tests individualmente
dotnet test --filter "FullyQualifiedName~NombreDelTest"
```

### Docker Build Lento o Falla

**Síntoma:** `docker build` tarda mucho o falla

**Solución:**

```bash
# 1. Limpiar cache de Docker
docker builder prune -a

# 2. Build sin cache
docker build --no-cache -t accessibility-gateway:latest .

# 3. Verificar espacio en disco
docker system df

# 4. Limpiar recursos no usados
docker system prune -a --volumes

# 5. Aumentar recursos de Docker Desktop
# Settings → Resources → Memory: 4GB+, CPUs: 4+
```

### Rutas No Encontradas (404)

**Síntoma:** `404 Not Found` en rutas que deberían existir

**Solución:**

```bash
# 1. Listar rutas configuradas
curl http://localhost:8100/api/routes | jq

# 2. Verificar configuración de rutas en appsettings.json
cat src/Gateway/appsettings.Development.json | grep -A 3 "AllowedRoutes"

# 3. Verificar que la ruta esté en appsettings
# Debe estar en AllowedRoutes con requiresAuth: true/false

# 4. Si falta, agregarla y reiniciar
code src/Gateway/appsettings.Development.json
docker compose restart accessibility-gateway

# 5. Verificar logs del Gateway
docker compose logs accessibility-gateway | grep "404"
```

### Alta Latencia en Requests

**Síntoma:** Requests tardan más de 1 segundo

**Solución:**

```bash
# 1. Verificar métricas
curl http://localhost:8100/metrics | grep http_request_duration

# 2. Verificar estado de Redis cache
curl http://localhost:8100/cache/stats

# 3. Verificar recursos del sistema
docker stats

# 4. Verificar logs en busca de timeouts
docker compose logs | grep -i "timeout"

# 5. Aumentar recursos de Docker
# Settings → Resources → Memory: 8GB, CPUs: 4

# 6. Optimizar configuración de cache
# Aumentar CACHE_TTL en .env.development
```

### Logs No Aparecen

**Síntoma:** No se generan logs o no son visibles

**Solución:**

```bash
# 1. Verificar configuración de Serilog
cat src/Gateway/appsettings.Development.json | grep -A 10 "Serilog"

# 2. Verificar carpeta de logs
ls -la logs/

# 3. Verificar permisos de escritura
# Windows: Propiedades → Seguridad → Editar
# Linux: chmod 755 logs/

# 4. Ver logs de Docker directamente
docker compose logs -f accessibility-gateway

# 5. Aumentar nivel de log
# appsettings.Development.json → MinimumLevel.Default: "Debug"

# 6. Reiniciar Gateway
docker compose restart accessibility-gateway
```

### Más Troubleshooting

Para problemas más complejos, consulta:

- **[TROUBLESHOOTING.md](docs/TROUBLESHOOTING.md)** - Guía completa de troubleshooting
- **[MONITORING.md](docs/MONITORING.md)** - Debugging con Prometheus/Grafana
- **[ARCHITECTURE.md](docs/ARCHITECTURE.md)** - Entender el flujo de datos
- **GitHub Issues** - Reportar bugs o solicitar ayuda

1. Fork el proyecto
2. Crea una rama para tu feature (`git checkout -b feature/AmazingFeature`)
3. Commit tus cambios (`git commit -m 'Add some AmazingFeature'`)
4. Push a la rama (`git push origin feature/AmazingFeature`)
5. Abre un Pull Request

### Lineamientos

- Seguir las convenciones de código C# y .NET
- Incluir tests para nuevas funcionalidades
- Actualizar documentación según sea necesario
- Mantener cobertura de código >90%

---

## 📄 Licencia

**Proprietary License**

Copyright (c) 2025 Geovanny Camacho. All rights reserved.

**IMPORTANTE:** Este software y los archivos de documentación asociados (el "Software") son propiedad exclusiva de Geovanny Camacho y están protegidos por leyes de derechos de autor y disposiciones de tratados internacionales.

### TÉRMINOS Y CONDICIONES

1. **PROPIEDAD**: El Software está licenciado, no vendido. Geovanny Camacho retiene todos los derechos, títulos e intereses sobre el Software, incluidos todos los derechos de propiedad intelectual.

2. **RESTRICCIONES**: NO puede:

   - Copiar, modificar o crear trabajos derivados del Software
   - Distribuir, transferir, sublicenciar, arrendar, prestar o alquilar el Software
   - Realizar ingeniería inversa, descompilar o desensamblar el Software
   - Eliminar o alterar avisos o etiquetas propietarias del Software
   - Usar el Software con fines comerciales sin permiso explícito por escrito
   - Compartir credenciales de acceso o permitir acceso no autorizado al Software

3. **CONFIDENCIALIDAD**: El Software contiene secretos comerciales e información confidencial. Usted acepta mantener la confidencialidad del Software y no divulgarlo a terceros.

4. **TERMINACIÓN**: Esta licencia es efectiva hasta que se termine. Sus derechos bajo esta licencia terminarán automáticamente sin previo aviso si no cumple con alguno de sus términos.

5. **SIN GARANTÍA**: EL SOFTWARE SE PROPORCIONA "TAL CUAL", SIN GARANTÍA DE NINGÚN TIPO, EXPRESA O IMPLÍCITA, INCLUIDAS, ENTRE OTRAS, LAS GARANTÍAS DE COMERCIABILIDAD, IDONEIDAD PARA UN PROPÓSITO PARTICULAR Y NO INFRACCIÓN.

6. **LIMITACIÓN DE RESPONSABILIDAD**: EN NINGÚN CASO GEOVANNY CAMACHO SERÁ RESPONSABLE DE NINGÚN RECLAMO, DAÑOS U OTRA RESPONSABILIDAD, YA SEA EN UNA ACCIÓN CONTRACTUAL, AGRAVIO O DE OTRO TIPO, QUE SURJA DE, FUERA DE O EN CONEXIÓN CON EL SOFTWARE O EL USO U OTRAS TRANSACCIONES EN EL SOFTWARE.

**PARA CONSULTAS DE LICENCIA:**  
Geovanny Camacho  
Email: fgiocl@outlook.com

---

<div align="center">

**🚪 Accessibility Gateway - API Gateway Empresarial .NET 9**

[⭐ Star este proyecto](../../) • [🐛 Reportar Bug](../../issues) • [💡 Solicitar Feature](../../issues)

**Autor:** Geovanny Camacho ([fgiocl@outlook.com](mailto:fgiocl@outlook.com))  
**Última actualización:** 6 de noviembre de 2025  
**Versión:** 2.0.0  
**Tests:** 435 passing (100%)  
**Coverage:** 91.94% lines, 90.51% branches

---

**Construido con ❤️ usando .NET 9, YARP, Redis y Docker**

</div>
