# 🚪 Accessibility Gateway

> **API Gateway empresarial avanzado desarrollado en .NET 9 que actúa como punto de entrada único para la plataforma de análisis de accesibilidad web. Proporciona enrutamiento inteligente con YARP, sistema de caché distribuido con Redis, autenticación JWT, rate limiting, monitoreo avanzado y gestión centralizada de microservicios.**

<div align="center">

[![.NET 9.0](https://img.shields.io/badge/.NET-9.0-512BD4?style=for-the-badge&logo=.net)](https://dotnet.microsoft.com/en-us/download/dotnet/9.0)
[![YARP](https://img.shields.io/badge/YARP-Reverse_Proxy-5C2D91?style=for-the-badge&logo=.net)](https://microsoft.github.io/reverse-proxy/)
[![Docker](https://img.shields.io/badge/Docker-Ready-2496ED?style=for-the-badge&logo=docker)](https://www.docker.com/)
[![Redis](https://img.shields.io/badge/Redis-Cache-DC382D?style=for-the-badge&logo=redis)](https://redis.io/)
[![JWT](https://img.shields.io/badge/JWT-Auth-000000?style=for-the-badge&logo=json-web-tokens)](https://jwt.io/)

[![Tests](https://img.shields.io/badge/Tests-108_passing-brightgreen.svg?style=flat-square)](https://github.com/magodeveloper/accessibility-gw)
[![Coverage](https://img.shields.io/badge/Coverage-92.5%25-green.svg?style=flat-square)](https://github.com/magodeveloper/accessibility-gw)
[![Security](https://img.shields.io/badge/Security-A+-green.svg?style=flat-square)](https://github.com/magodeveloper/accessibility-gw)
[![Build](https://img.shields.io/badge/CI%2FCD-Passing-green.svg?style=flat-square)](https://github.com/magodeveloper/accessibility-gw/actions)
[![License](https://img.shields.io/badge/License-MIT-yellow.svg?style=flat-square)](https://opensource.org/licenses/MIT)

</div>

---

## 📋 Tabla de Contenidos

- [✨ Características](#-características)
- [🏗️ Arquitectura](#️-arquitectura)
- [📚 Documentación Adicional](#-documentación-adicional)
- [🚀 Quick Start](#-quick-start)
- [📡 API Reference](#-api-reference)
- [🧪 Testing](#-testing)
- [🐳 Docker & Deployment](#-docker--deployment)
- [🛠️ Stack Tecnológico](#️-stack-tecnológico)
- [🤝 Contribución](#-contribución)

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

```
┌───────────────────────────────────────────────────┐
│     ACCESSIBILITY GATEWAY (PORT 8100)             │
│                                                   │
│  ┌─────────────────────────────────────────────┐ │
│  │    YARP Reverse Proxy + Middleware         │ │
│  │  (Auth, Rate Limit, CORS, Security)        │ │
│  └──────────────┬──────────────────────────────┘ │
│                 │                                 │
│                 ▼                                 │
│  ┌─────────────────────────────────────────────┐ │
│  │        Service Router & Load Balancer      │ │
│  │   (Health Check + Circuit Breaker)         │ │
│  └──────────────┬──────────────────────────────┘ │
│                 │                                 │
│        ┌────────┴────────┐                        │
│        ▼                 ▼                        │
│  ┌──────────┐    ┌──────────────┐                │
│  │  Redis   │    │   Metrics    │                │
│  │  Cache   │    │  Prometheus  │                │
│  └──────────┘    └──────────────┘                │
└────────────────────┬──────────────────────────────┘
                     │ REST API / Docker Network
                     ▼
          ┌──────────────────────┐
          │  Microservicios      │
          ├──────────────────────┤
          │  ms-users (8081)     │
          │  ms-analysis (8082)  │
          │  ms-reports (8083)   │
          │  middleware (3001)   │
          └──────────────────────┘
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

> 💡 **Tip:** Si eres nuevo en el proyecto, empieza por [CONFIGURATION.md](docs/CONFIGURATION.md) para el setup inicial, luego consulta [ARCHITECTURE.md](docs/ARCHITECTURE.md) para entender el diseño técnico.

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

## 🧪 Testing

### Estado de Cobertura

**Estado General:** ✅ 108/108 tests exitosos (100%)  
**Cobertura Total:** 92.5%

| Categoría             | Tests        | Cobertura | Estado |
| --------------------- | ------------ | --------- | ------ |
| **Unit Tests**        | 96           | 94.2%     | ✅     |
| **Integration Tests** | 12           | 88.1%     | ✅     |
| **Load Tests (K6)**   | 6 escenarios | N/A       | ✅     |

### Ejecutar Tests

```bash
# Todos los tests con cobertura
.\manage-tests.ps1 full

# Solo unit tests
.\manage-tests.ps1 test -Filter "UnitTests"

# Tests de integración
.\manage-tests.ps1 test -Filter "IntegrationTests"

# Load testing con K6
cd src/tests/Gateway.Load
.\manage-load-tests.ps1 smoke

# Ver dashboard
Start-Process test-dashboard.html
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

## 🛠️ Stack Tecnológico

### Core

- **.NET 9.0** - Framework principal
- **C# 13** - Lenguaje de programación
- **ASP.NET Core** - Web framework

### Componentes Principales

- **YARP 2.2.0** - Reverse Proxy
- **StackExchange.Redis 2.8.16** - Cliente Redis
- **Serilog 4.1.0** - Logging estructurado
- **Prometheus.NET 8.2.1** - Métricas
- **Polly 8.5.0** - Resiliencia y circuit breaker
- **FluentValidation 11.11.0** - Validación de input
- **Swashbuckle 7.2.0** - Documentación OpenAPI

### Testing

- **xUnit 2.9.2** - Framework de testing
- **NSubstitute 5.3.0** - Mocking
- **Coverlet 6.0.2** - Cobertura de código
- **K6** - Load testing

### Infrastructure

- **Docker 24.0+** - Containerización
- **Redis 7.2** - Cache distribuido
- **Prometheus** - Métricas
- **Grafana** - Visualización

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

## 🤝 Contribución

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

Este proyecto está bajo la Licencia MIT. Ver archivo `LICENSE` para más detalles.

---

<div align="center">

**🚪 Accessibility Gateway - API Gateway Empresarial**

[⭐ Star este proyecto](../../) • [🐛 Reportar Bug](../../issues) • [💡 Solicitar Feature](../../issues)

**📅 Última actualización:** Noviembre 2025

</div>
