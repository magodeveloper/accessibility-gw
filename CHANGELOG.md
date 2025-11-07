# Changelog

Todos los cambios notables en este proyecto serán documentados en este archivo.

El formato está basado en [Keep a Changelog](https://keepachangelog.com/es-ES/1.0.0/),
y este proyecto se adhiere a [Semantic Versioning](https://semver.org/lang/es/).

---

## [Unreleased]

### Planeado

- Integración con Azure Key Vault para secrets management
- Métricas adicionales de Prometheus
- Support para OpenTelemetry
- Cache warming automático
- Rate limiting por usuario
- API versioning con header support

---

## [2.0.0] - 2025-11-06

### 🎉 Major Release

Esta versión representa una mejora significativa en documentación, testing y features de seguridad.

### Added ✨

#### Autenticación y Seguridad

- **Endpoint de Registro Público** (`POST /api/Auth/register`)
  - Registro de usuarios sin autenticación
  - Validación con FluentValidation
  - Hashing de passwords con BCrypt
  - Auto-creación de preferencias por defecto (WCAG 2.2, Level AA, español)
  - Normalización de emails a lowercase
  - Validación de emails duplicados (409 Conflict)
  - Role forzado a "user" (nunca "admin" en registro público)
  - Generación automática de nickname si no se proporciona

#### Documentación

- **README.md mejorado**:
  - Diagramas Mermaid de arquitectura
  - Sección de Health Checks detallada
  - Sección de Seguridad completa (JWT + Gateway Secret)
  - Sección de Monitoreo y Métricas
  - Sección de CI/CD
  - Stack tecnológico con 80+ librerías y versiones exactas
  - Troubleshooting rápido con 10 problemas comunes
  - Badges actualizados con datos reales
- **DEVELOPMENT.md** - Guía completa para desarrolladores
- **CONTRIBUTING.md** - Guía de contribución
- **CHANGELOG.md** - Este archivo
- **AUTH_REGISTER_ENDPOINT.md** - Documentación del endpoint de registro

#### Testing

- **11 nuevos unit tests** para endpoint de registro
  - Registro exitoso
  - Email duplicado
  - Email case-insensitive
  - Password corto
  - Nickname faltante (auto-generación)
  - Defaults correctos
  - Password hasheado
  - Email normalizado
  - Creación de preferencias
  - Passwords vacíos/whitespace

#### Scripts

- **test-user-register.ps1** - Script de prueba del endpoint de registro
  - 5 escenarios de test
  - Validación de respuestas
  - Datos de prueba (JSON files)
- **test-jwt-login.ps1** - Script actualizado para usar nuevo endpoint

#### Configuración

- Sincronización de rutas en 3 archivos appsettings (54 rutas totales)
- Configuración de ruta pública para `/api/Auth/register`
- Variables de entorno documentadas

### Changed 🔄

#### API

- **RegisterDto** ahora usa `DataAnnotations` + `FluentValidation`
- Endpoint `/api/users-with-preferences` vuelve a requerir autenticación (corregido)
- Mejora en validación de JWT tokens
- Normalización de emails a lowercase en toda la aplicación

#### Configuración

- Actualizado `appsettings.json` (54 rutas)
- Actualizado `appsettings.Development.json` (54 rutas)
- Actualizado `appsettings.Production.json` (54 rutas)
- Agregadas rutas DELETE faltantes

#### Testing

- **Coverage aumentado de 89.2% a 91.94%** (líneas)
- **Coverage de branches: 90.51%**
- **Total de tests: 435** (todos passing)
- Dashboard HTML mejorado con más métricas

#### Documentación

- README expandido de 458 a 1662 líneas (+263%)
- Diagramas visuales agregados (Mermaid)
- Ejemplos de código aumentados en 300%
- Comandos ejecutables aumentados en 300%

### Fixed 🐛

#### Autenticación

- **Rutas DELETE devolvían 403 en lugar de 401** sin JWT token
  - Causa: Faltaban en `AllowedRoutes` de appsettings
  - Solución: Agregadas a los 3 archivos appsettings
- **Error de compilación: `PreferenceLanguage` no existe**

  - Causa: Enum incorrecto usado
  - Solución: Cambiar a `Language` enum

- **Error de runtime: `defaultPreference.Id` era 0**

  - Causa: No se recargaba después de `SaveChanges()`
  - Solución: Agregar `Reload()` después de guardar

- **MySQL error: "Data too long for column 'nickname'"**
  - Causa: Timestamp completo era muy largo (14 chars)
  - Solución: Usar formato corto `MMddHHmmss` (10 chars)

#### Configuración

- **appsettings.Development.json desactualizado**
  - Tenía 48 rutas vs 52-53 en otros archivos
  - Sincronizado a 54 rutas en todos los archivos

#### Tests

- Test user no existía en database
  - Agregado script de creación dinámica de usuarios

### Security 🔒

- **Password hashing con BCrypt** en registro público
- **Validación de Gateway Secret** documentada
- **JWT Secret generation** automatizada con PowerShell
- **Email normalization** para prevenir duplicados con diferentes cases
- **Role enforcement** - Registro público solo puede crear role "user"
- **Rate limiting** documentado por endpoint
- **CORS policy** configurada y documentada
- **Security headers** automáticos (HSTS, CSP, XSS Protection)

### Performance ⚡

- Cache de Redis optimizado con TTL apropiados
- Queries de database optimizadas en registro
- Async/await usado consistentemente
- Connection pooling mejorado

### Documentation 📚

- **10 nuevos documentos** en carpeta `docs/`
- **Arquitectura visual** con diagramas Mermaid
- **Health checks** documentados (3 endpoints)
- **Troubleshooting** con 10 problemas comunes
- **CI/CD workflows** documentados
- **Stack tecnológico** completo con versiones
- **Best practices** de desarrollo
- **Guías de contribución**

---

## [1.0.0] - 2025-10-15

### Added ✨

#### Core Features

- **YARP Reverse Proxy** configurado
- **JWT Authentication** con Bearer tokens
- **Rate Limiting** por endpoint
- **Redis Cache** distribuido con fallback a memoria
- **Health Checks** para microservicios
- **Prometheus Metrics** exportadas
- **Structured Logging** con Serilog
- **Circuit Breaker** con Polly
- **CORS Policy** configurable

#### Endpoints

- `GET /health` - Health check completo
- `GET /health/live` - Liveness probe
- `GET /health/ready` - Readiness probe
- `GET /metrics` - Métricas Prometheus
- `GET /cache/stats` - Estadísticas de cache
- `POST /api/v1/translate` - Traducción de requests

#### Proxy Routes

- `/api/Auth/**` → ms-users (8081)
- `/api/users/**` → ms-users (8081)
- `/api/preferences/**` → ms-users (8081)
- `/api/Analysis/**` → ms-analysis (8082)
- `/api/Result/**` → ms-analysis (8082)
- `/api/Report/**` → ms-reports (8083)
- `/api/analyze/**` → middleware (3001)

#### Configuration

- `appsettings.json` - Configuración base
- `appsettings.Development.json` - Config desarrollo
- `appsettings.Production.json` - Config producción
- `.env.example` - Template de variables de entorno

#### Docker

- **Dockerfile** multi-stage optimizado
- **docker-compose.yml** para producción
- **docker-compose.dev.yml** para desarrollo
- **docker-compose.monitoring.yml** para stack de monitoreo
- Red Docker compartida: `accessibility-shared`

#### Testing

- **Unit Tests** con xUnit (96 tests)
- **Integration Tests** (12 tests)
- **Load Tests** con k6 (6 escenarios)
- **Coverage** >90%
- Script `manage-tests.ps1`

#### Scripts PowerShell

- `manage-tests.ps1` - Gestión de tests
- `manage-monitoring.ps1` - Stack de monitoreo
- `manage-network.ps1` - Red Docker
- `cleanup-project.ps1` - Limpieza de proyecto
- `Generate-JwtSecretKey.ps1` - Generar secrets

#### Monitoring

- **Prometheus** configurado (puerto 9090)
- **Grafana** con dashboards (puerto 3000)
- **Alertmanager** para alertas (puerto 9093)
- 5 dashboards pre-configurados

#### Documentation

- README.md completo
- API.md con referencia de endpoints
- ARCHITECTURE.md con diseño técnico
- CONFIGURATION.md con variables de entorno
- SECURITY.md con JWT y autenticación
- CACHE.md con estrategias de cache
- TESTING.md con guía de tests
- DOCKER.md con configuración Docker
- MONITORING.md con Prometheus/Grafana
- TROUBLESHOOTING.md con problemas comunes

### Security 🔒

- JWT Bearer authentication
- Gateway Secret validation
- HTTPS enforcement
- CORS policy configurada
- Security headers automáticos
- Rate limiting por endpoint
- Input validation con FluentValidation

---

## [0.1.0] - 2025-09-01

### Added ✨

#### Initial Setup

- Proyecto .NET 9 creado
- Estructura básica de carpetas
- Git repository inicializado
- .gitignore configurado
- LICENSE file

#### Basic Features

- ASP.NET Core Web API básico
- Swagger/OpenAPI documentación
- Logging básico con console
- Health check simple

---

## Tipos de Cambios

### Símbolos

- ✨ `Added` - Nueva funcionalidad
- 🔄 `Changed` - Cambios en funcionalidad existente
- 🗑️ `Deprecated` - Funcionalidad que será removida
- 🚫 `Removed` - Funcionalidad removida
- 🐛 `Fixed` - Corrección de bug
- 🔒 `Security` - Seguridad
- ⚡ `Performance` - Mejoras de performance
- 📚 `Documentation` - Cambios en documentación

### Categorías

- **Breaking Changes** - Cambios que rompen compatibilidad
- **Features** - Nuevas funcionalidades
- **Bug Fixes** - Corrección de bugs
- **Performance** - Mejoras de performance
- **Documentation** - Actualizaciones de docs
- **Tests** - Cambios en tests
- **CI/CD** - Cambios en pipeline
- **Dependencies** - Actualización de dependencias

---

## Versionado

Este proyecto usa [Semantic Versioning](https://semver.org/lang/es/):

```
MAJOR.MINOR.PATCH

MAJOR: Breaking changes
MINOR: Nuevas features (backward compatible)
PATCH: Bug fixes (backward compatible)
```

**Ejemplos:**

- `1.0.0` → `1.0.1` - Bug fix
- `1.0.1` → `1.1.0` - Nueva feature
- `1.1.0` → `2.0.0` - Breaking change

---

## Enlaces

- [Repository](https://github.com/magodeveloper/accessibility-gw)
- [Issues](https://github.com/magodeveloper/accessibility-gw/issues)
- [Pull Requests](https://github.com/magodeveloper/accessibility-gw/pulls)
- [Releases](https://github.com/magodeveloper/accessibility-gw/releases)

---

## Contributors

Agradecimientos a todos los que han contribuido a este proyecto:

- **Geovanny Camacho** ([@magodeveloper](https://github.com/magodeveloper)) - Creator & Maintainer

---

**Formato del Changelog basado en [Keep a Changelog](https://keepachangelog.com/es-ES/1.0.0/)**

**Última actualización:** 6 de noviembre de 2025
