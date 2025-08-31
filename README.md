# 🚪 Accessibility Gateway - AP```powershell

# 🎮 Ver todas las opciones del script maestro

.\manage-gateway.ps1 help

# 🔍 Verificar estado completo del proyecto

.\manage-gateway.ps1 verify -Full

# 🚀 Iniciar servidor local de desarrollo (puerto 8100) - NUEVA FUNCIONALIDAD UNIFICADA

.\manage-gateway.ps1 run -Port 8100

# 🐳 Iniciar en desarrollo (puerto 8100)

.\manage-gateway.ps1 docker up -Environment dev

# 🚀 Iniciar en producción (puerto 8000)

.\manage-gateway.ps1 docker up -Environment prod

````resarial

[![.NET 9.0](https://img.shields.io/badge/.NET-9.0-512BD4?logo=.net&logoColor=white)](https://dotnet.microsoft.com/en-us/download/dotnet/9.0)
[![Docker](https://img.shields.io/badge/Docker-Ready-2496ED?logo=docker&logoColor=white)](https://www.docker.com/)
[![Redis](https://img.shields.io/badge/Redis-Cache-DC382D?logo=redis&logoColor=white)](https://redis.io/)
[![Tests](https://img.shields.io/badge/Tests-108%20Passing-00D100?logo=github&logoColor=white)](https://github.com/)
[![Build](https://img.shields.io/badge/Build-Passing-00D100?logo=.net&logoColor=white)](https://github.com/)
[![Security](https://img.shields.io/badge/Security-Hardened-00D100?logo=security&logoColor=white)](https://github.com/)

API Gateway empresarial desarrollado en .NET 9 que actúa como punto de entrada único para la plataforma de accesibilidad web. Proporciona enrutamiento inteligente, caché distribuido con Redis, monitoreo avanzado y gestión centralizada de microservicios.

## 📊 Estado del Proyecto

🟢 **Totalmente Operacional y Optimizado**

- ✅ **108 tests** pasando (96 unitarios + 12 integración)
- ✅ **0 errores** de compilación
- ✅ **0 advertencias** críticas
- ✅ **Cobertura completa** de funcionalidades
- ✅ **Docker optimizado** con seguridad reforzada
- ✅ **Redis configurado** con fallback a memoria
- ✅ **Configuración lista** para producción

> 📅 **Última actualización:** 31 de agosto de 2025 - README unificado con toda la documentación del proyecto

## 🚀 Inicio Rápido

### **⚡ UN SOLO COMANDO - Todo Preparado**

El proyecto está **completamente funcional**. Simplemente ejecuta:

```powershell
# 🎮 Ver todas las opciones del script maestro
.\manage-gateway.ps1 help

# 🔍 Verificar estado completo del proyecto
.\manage-gateway.ps1 verify -Full

# � Iniciar en desarrollo (puerto 8100)
.\manage-gateway.ps1 docker up -Environment dev

# 🚀 Iniciar en producción (puerto 8000)
.\manage-gateway.ps1 docker up -Environment prod
````

### **🌐 URLs del Gateway una vez iniciado**

#### **Desarrollo** (puerto 8100):

- **Swagger UI**: http://localhost:8100/swagger
- **Health Check**: http://localhost:8100/health
- **API Base**: http://localhost:8100/api/

#### **Producción** (puerto 8000):

- **Swagger UI**: http://localhost:8000/swagger
- **Health Check**: http://localhost:8000/health
- **API Base**: http://localhost:8000/api/

### **📚 Documentación OpenAPI Completa**

Una vez iniciado el Gateway, accede a la documentación interactiva:

- **Swagger UI**: Interfaz completa con 40+ endpoints documentados
- **Funcionalidad**: Pruebas interactivas de todas las APIs
- **Organización**: Endpoints agrupados por microservicio (Users, Reports, Analysis, Middleware)

## 🏗️ Arquitectura y Características

### **🌟 Características Principales**

- 🔄 **Reverse Proxy** con YARP (Yet Another Reverse Proxy)
- 🗄️ **Caché inteligente** con Redis y fallback a memoria
- 🏥 **Health checks** avanzados para todos los microservicios
- 🔐 **Autenticación JWT** centralizada
- 📊 **Logging estructurado** con Serilog
- ⚡ **Rate limiting** configurable por servicio
- 🌐 **CORS** centralizado (microservicios pueden desactivar CORS)
- 🐳 **Docker optimizado** con seguridad reforzada
- 📈 **Monitoreo** y métricas en tiempo real
- 🔒 **Configuración de seguridad** production-ready

### **🎯 Microservicios Soportados**

| Servicio           | Ruta Gateway      | Puerto Interno                 | Health Check | Descripción                         |
| ------------------ | ----------------- | ------------------------------ | ------------ | ----------------------------------- |
| **Users API**      | `/api/v1/users/*` | `http://msusers-api:8081`      | `/health`    | Gestión de usuarios y autenticación |
| **Users Auth**     | `/api/auth/*`     | `http://msusers-api:8081`      | `/health`    | JWT y autorización                  |
| **Reports API**    | `/api/Report/*`   | `http://msreports-api:8083`    | `/health`    | Informes de accesibilidad           |
| **Analysis API**   | `/api/Analysis/*` | `http://msanalysis-api:8082`   | `/health`    | Análisis de sitios web              |
| **Middleware API** | `/api/analyze/*`  | `http://accessibility-mw:3001` | `/health`    | Servicios auxiliares y herramientas |

### **🗄️ Sistema de Caché Avanzado**

#### **Configuración Automática**

- ✅ **Redis** como caché primario (producción)
- ✅ **Memoria** como fallback (desarrollo/testing)
- ✅ **Detección automática** de disponibilidad de Redis
- ✅ **Serialización JSON** optimizada
- ✅ **Invalidación selectiva** por servicio

#### **Características del Caché**

- 🔑 **Generación automática** de claves basada en request
- 🛡️ **Exclusión de headers sensibles** (authorization, cookies)
- ⏰ **Expiración configurable** por tipo de request
- 🔄 **Invalidación granular** por servicio o endpoint
- 📊 **Output Cache** adicional con políticas base

#### **Configuración Redis Optimizada**

```yaml
# Redis con 7 parámetros de optimización
redis:
  command: |
    redis-server 
    --appendonly yes 
    --appendfsync everysec     # Persistencia cada segundo
    --maxmemory 256mb          # Límite de memoria
    --maxmemory-policy allkeys-lru  # Política de expulsión
    --tcp-keepalive 60         # Conexiones más estables
    --timeout 0                # Sin timeout de conexión
    --save 900 1 300 10        # Snapshots automáticos
```

## 🐳 Docker - Configuración Optimizada

### **✅ Mejoras de Seguridad Implementadas**

- **🔒 Non-root user**: Contenedores ejecutados como usuario no privilegiado
- **🛡️ No new privileges**: `security_opt: no-new-privileges:true`
- **📖 Read-only filesystem**: `read_only: true` con tmpfs para temporales
- **🌡️ Timezone configurado**: `America/Mexico_City`
- **🏷️ Labels completos**: Metadatos del proyecto y versiones

### **⚡ Optimizaciones de Rendimiento**

- **🩺 Health checks mejorados**: 30s start_period para inicialización
- **🔌 Puertos separados**: Desarrollo (8100) vs Producción (8000)
- **🧹 Variables optimizadas**: Eliminadas duplicaciones
- **💾 Caché Redis**: 7 parámetros de optimización para rendimiento

### **🔧 Comandos Docker Actualizados**

```powershell
# Desarrollo con herramientas (puerto 8100)
docker-compose -f docker-compose.dev.yml --profile tools up --build

# Producción optimizada (puerto 8000)
docker-compose up --build

# Validar configuración
docker-compose -f docker-compose.yml config
docker-compose -f docker-compose.dev.yml config

# Logs en tiempo real
docker-compose logs -f accessibility-gateway
```

## 🛠️ Scripts de Gestión

### **⚡ `manage-gateway.ps1` - Script Maestro Unificado**

Un solo script que maneja todo el ciclo de vida del proyecto **(UNIFICA start-local.ps1)**:

```powershell
# 📋 INFORMACIÓN Y AYUDA
.\manage-gateway.ps1 help                    # Mostrar todas las opciones
.\manage-gateway.ps1 verify -Full            # Verificación completa del proyecto

# 🚀 SERVIDOR LOCAL (NUEVA FUNCIONALIDAD - reemplaza start-local.ps1)
.\manage-gateway.ps1 run                     # Servidor local puerto 8100
.\manage-gateway.ps1 run -Port 8085          # Puerto personalizado
.\manage-gateway.ps1 run -NoLaunch           # Sin abrir navegador automáticamente
.\manage-gateway.ps1 run -AspNetCoreEnvironment Production  # Entorno específico

# 🔨 CONSTRUCCIÓN Y TESTING
.\manage-gateway.ps1 build                   # Build estándar
.\manage-gateway.ps1 build -Configuration Release -BuildType production
.\manage-gateway.ps1 test -TestType Unit     # Solo tests unitarios
.\manage-gateway.ps1 test -TestType Integration  # Solo tests de integración

# 🐳 GESTIÓN DE DOCKER
.\manage-gateway.ps1 docker up -Environment dev -WithTools     # Desarrollo + herramientas
.\manage-gateway.ps1 docker up -Environment prod               # Producción
.\manage-gateway.ps1 docker status                             # Estado de contenedores
.\manage-gateway.ps1 docker logs -Follow                       # Logs en tiempo real
.\manage-gateway.ps1 docker down                               # Detener servicios

# 🧹 LIMPIEZA Y MANTENIMIENTO
.\manage-gateway.ps1 cleanup -Docker -Volumes    # Limpiar Docker completamente
.\manage-gateway.ps1 cleanup -Builds             # Limpiar builds locales
```

### **🔍 Configuración Manual**

Para configurar el proyecto sin scripts adicionales:

```powershell
# 1. Crear archivo .env desde template (opcional)
cp .env.example .env

# 2. Editar variables según tu entorno
notepad .env  # Windows

# 3. El proyecto detecta automáticamente las variables necesarias
# ✅ Sin validación previa requerida - el gateway maneja fallbacks automáticamente
```

## ⚙️ Configuración

### **📋 Variables de Entorno - Setup Rápido**

#### **1. Configuración Inicial**

```bash
# Copia el template de variables (56 configuraciones incluidas)
cp .env.example .env

# Edita con tus valores locales
notepad .env  # Windows
```

#### **2. Variables Principales por Categoría**

```bash
# 🚀 APLICACIÓN
ASPNETCORE_ENVIRONMENT=Development
ASPNETCORE_URLS=http://+:8080
ASPNETCORE_HTTP_PORTS=8080

# 🗄️ REDIS CACHÉ
REDIS_CONNECTION_STRING=localhost:6379
REDIS_DATABASE=0
REDIS_INSTANCE_NAME=AccessibilityGateway

# 🌐 SERVICIOS (URLs internas de microservicios)
GATE__SERVICES__USERS=http://msusers-api:8081
GATE__SERVICES__REPORTS=http://msreports-api:8083
GATE__SERVICES__ANALYSIS=http://msanalysis-api:8082
GATE__SERVICES__MIDDLEWARE=http://accessibility-mw:3001

# 🔐 JWT AUTENTICACIÓN
JWT_SECRET=tu-clave-secreta-muy-segura-aqui
JWT_ISSUER=AccessibilityGateway
JWT_AUDIENCE=AccessibilityClients
JWT_EXPIRY_MINUTES=60

# 🚪 GATEWAY CONFIGURACIÓN
GATEWAY_PORT=3000
GATEWAY_ENVIRONMENT=Development
GATEWAY_REQUEST_TIMEOUT_SECONDS=30
GATEWAY_MAX_REQUEST_BODY_SIZE=52428800

# 🏥 HEALTH CHECKS
HEALTH_CHECK_INTERVAL=30
HEALTH_CHECK_TIMEOUT=10
HEALTH_CHECK_FAILURE_THRESHOLD=3

# 📊 LOGGING
LOG_LEVEL=Information
LOG_FILE_PATH=logs/gateway.log
SERILOG_MINIMUM_LEVEL=Information

# ⚡ RATE LIMITING
RATE_LIMIT_REQUESTS_PER_MINUTE=100
RATE_LIMIT_BURST_SIZE=20

# 🌐 CORS
CORS_ALLOWED_ORIGINS=http://localhost:3000,http://localhost:8100
CORS_ALLOWED_METHODS=GET,POST,PUT,DELETE,OPTIONS
CORS_ALLOWED_HEADERS=*

# ⏰ TIMEOUTS Y CIRCUIT BREAKER
CIRCUIT_BREAKER_FAILURE_THRESHOLD=5
CIRCUIT_BREAKER_TIMEOUT_SECONDS=30
CIRCUIT_BREAKER_RETRY_ATTEMPTS=3
```

### **🔄 Configuración por Entorno**

El sistema detecta automáticamente el entorno y aplica la configuración correcta:

| Entorno         | Redis              | Puerto | Logs        | Health Checks |
| --------------- | ------------------ | ------ | ----------- | ------------- |
| **Development** | Memoria (fallback) | 8100   | Verbose     | 30s intervalo |
| **Testing**     | Memoria            | 8080   | Warning     | 15s intervalo |
| **Production**  | Redis obligatorio  | 8000   | Information | 60s intervalo |

## 🏗️ Estructura del Proyecto Completa

- **🎯 Punto de Entrada Único**: Centraliza el acceso a todos los microservicios
- **⚡ Caché Distribuido**: Redis para optimización de rendimiento
- **🔍 Monitoreo Avanzado**: Health checks y métricas en tiempo real
- **🔐 Seguridad Centralizada**: Autenticación y autorización unificada
- **📊 Trazabilidad**: Logging estructurado con correlación de requests
- **🐳 Docker Ready**: Contenedores optimizados para producción

### **🏛️ Stack Tecnológico**

| Componente    | Tecnología    | Versión | Propósito                |
| ------------- | ------------- | ------- | ------------------------ |
| **Gateway**   | .NET 9 + YARP | 9.0     | Enrutamiento y proxy     |
| **Cache**     | Redis         | 7.x     | Cache distribuido        |
| **Logging**   | Serilog       | 8.x     | Logging estructurado     |
| **Monitoreo** | Health Checks | .NET    | Supervisión de servicios |
| **Container** | Docker        | Latest  | Contenedorización        |

## 🔧 Gestión Unificada

### **📋 Comandos Principales**

| Comando   | Descripción               | Ejemplos                                                     |
| --------- | ------------------------- | ------------------------------------------------------------ |
| `test`    | Ejecutar pruebas          | `.\manage-gateway.ps1 test -TestType Unit -GenerateCoverage` |
| `build`   | Construir proyecto        | `.\manage-gateway.ps1 build -Configuration Release`          |
| `run`     | **NUEVO**: Servidor local | `.\manage-gateway.ps1 run -Port 8100`                        |
| `verify`  | Verificar estado          | `.\manage-gateway.ps1 verify -Full`                          |
| `docker`  | Gestión Docker            | `.\manage-gateway.ps1 docker up -Environment prod`           |
| `cleanup` | Limpieza                  | `.\manage-gateway.ps1 cleanup -Docker -Volumes`              |

### **🧪 Testing Completo**

```powershell
# Ejecutar todas las pruebas
.\manage-gateway.ps1 test

# Pruebas específicas con cobertura
.\manage-gateway.ps1 test -TestType Unit -GenerateCoverage -OpenReport

# Pruebas de integración
.\manage-gateway.ps1 test -TestType Integration

# Pruebas de rendimiento
.\manage-gateway.ps1 test -TestType Performance
```

### **🔨 Building Optimizado**

```powershell
# Build estándar para desarrollo
.\manage-gateway.ps1 build

# Build para producción
.\manage-gateway.ps1 build -Configuration Release -BuildType production

# Build Docker con push
.\manage-gateway.ps1 build -BuildType docker -Push -Registry myregistry.com
```

### **🐳 Docker Management**

```powershell
# Iniciar en modo desarrollo
.\manage-gateway.ps1 docker up -Environment dev -WithTools

# Iniciar en modo producción
.\manage-gateway.ps1 docker up -Environment prod

# Ver logs en tiempo real
.\manage-gateway.ps1 docker logs -Follow

# Estado de contenedores
.\manage-gateway.ps1 docker status

# Detener y limpiar
.\manage-gateway.ps1 docker down
.\manage-gateway.ps1 cleanup -Docker -Volumes
```

## 🌐 Configuración de Servicios

### **📡 Endpoints y Rutas**

El gateway maneja el enrutamiento a los siguientes microservicios:

| Servicio           | Ruta Gateway      | Puerto Interno                 | Health Check |
| ------------------ | ----------------- | ------------------------------ | ------------ |
| **Users API**      | `/api/v1/users/*` | `http://msusers-api:8081`      | `/health`    |
| **Users Auth**     | `/api/auth/*`     | `http://msusers-api:8081`      | `/health`    |
| **Reports API**    | `/api/Report/*`   | `http://msreports-api:8083`    | `/health`    |
| **Analysis API**   | `/api/Analysis/*` | `http://msanalysis-api:8082`   | `/health`    |
| **Middleware API** | `/api/analyze/*`  | `http://accessibility-mw:3001` | `/health`    |

### **🔧 Variables de Entorno**

#### **📋 Configuración Inicial**

Para configurar el proyecto localmente:

## 🏗️ Estructura del Proyecto Completa

```
accessibility-gw/
├── 📄 manage-gateway.ps1               # ✨ Script maestro unificado
├── 📄 README.md                        # 📚 Documentación completa (este archivo)
├── 📄 .env.example                     # 🔧 Template de 56 variables de entorno
├── 📄 Gateway.sln                      # 🏗️ Solución principal
├── 📄 Dockerfile                       # 🐳 Multi-stage con seguridad reforzada
├── 📄 docker-compose.yml               # 🐳 Producción (puerto 8000)
├── 📄 docker-compose.dev.yml           # 🐳 Desarrollo (puerto 8100)
├── 📄 Directory.Packages.props         # 📦 Gestión centralizada de dependencias
├── 📄 .dockerignore                    # 🐳 Exclusiones para build de contenedor
├── 📄 .gitignore                       # 🔒 Excluye .env y archivos sensibles
│
├── 📁 src/
│   ├── 📁 Gateway/                     # 🚪 Proyecto principal del gateway
│   │   ├── 📄 Program.cs               # 🚀 Configuración y punto de entrada
│   │   ├── 📄 Gateway.csproj           # 🏗️ Configuración del proyecto
│   │   ├── 📄 appsettings.json         # ⚙️ Configuración base
│   │   ├── 📄 appsettings.Development.json # ⚙️ Configuración desarrollo
│   │   ├── 📄 appsettings.Production.json  # ⚙️ Configuración producción
│   │   ├── 📁 Services/                # 🔧 Servicios del gateway
│   │   │   ├── 📄 CacheService.cs      # 🗄️ Sistema de caché Redis/Memory
│   │   │   ├── 📄 HealthCheckService.cs # 🏥 Health checks automáticos
│   │   │   └── 📄 ProxyService.cs      # 🔄 Lógica de proxy y enrutamiento
│   │   ├── 📁 Models/                  # 📊 Modelos de datos
│   │   ├── 📁 Middleware/              # ⚙️ Middleware personalizado
│   │   └── 📁 Configuration/           # 🔧 Clases de configuración
│   │
│   └── 📁 tests/                       # 🧪 Suite completa de pruebas
│       ├── 📄 Gateway.Tests.sln        # 🧪 Solución de pruebas
│       ├── 📄 run-all-tests.ps1        # 🧪 Script ejecutor de pruebas
│       ├── 📁 Gateway.Tests.Basic/     # ✅ 12 pruebas básicas
│       ├── 📁 Gateway.UnitTests/       # 🔬 96 pruebas unitarias
│       └── 📁 Gateway.IntegrationTests/ # 🔄 12 pruebas de integración
│
├── 📁 docs/                           # 📚 Documentación técnica
│   ├── 📁 integration/                # 🔗 Guías de integración
│   │   ├── 📄 cors-configuration.md   # 🌐 Configuración CORS
│   │   ├── 📄 gateway-headers.md      # 📋 Headers del gateway
│   │   ├── 📄 health-checks.md        # 🏥 Documentación health checks
│   │   ├── 📄 migration-guide.md      # 🔄 Guía de migración
│   │   └── 📄 service-urls.md         # 🌐 URLs de servicios
│   │
│   └── 📁 swagger/                    # 📋 Documentación API
│       ├── 📄 gateway-api.json        # 📋 Especificación OpenAPI
│       └── 📄 microservices-api.json  # 📋 APIs de microservicios
│
└── 📁 logs/                           # 📊 Directorio de logs (creado automáticamente)
    ├── 📄 gateway.log                 # 📝 Logs principales del gateway
    └── 📄 health-checks.log           # 🏥 Logs específicos de health checks
```

## 🧪 Testing - Suite Completa de 108 Tests

### **📊 Distribución de Tests**

| Categoría       | Cantidad      | Descripción             | Estado              |
| --------------- | ------------- | ----------------------- | ------------------- |
| **Básicos**     | 12 tests      | Configuración y startup | ✅ Passing          |
| **Unitarios**   | 96 tests      | Servicios individuales  | ✅ Passing          |
| **Integración** | 12 tests      | End-to-end completos    | ✅ Passing          |
| **Total**       | **108 tests** | Suite completa          | ✅ **100% Passing** |

### **🚀 Ejecutar Tests**

```powershell
# Todos los tests (108 tests)
.\manage-gateway.ps1 test

# Solo tests unitarios (96 tests)
.\manage-gateway.ps1 test -TestType Unit

# Solo tests de integración (12 tests)
.\manage-gateway.ps1 test -TestType Integration

# Tests con cobertura detallada
.\manage-gateway.ps1 test -TestType Unit -Verbose
```

### **📈 Cobertura de Testing**

- ✅ **Servicios de caché** (Redis + Memory fallback)
- ✅ **Health checks** de microservicios
- ✅ **Autenticación JWT** completa
- ✅ **Rate limiting** por endpoint
- ✅ **CORS** y headers personalizados
- ✅ **Proxy** y enrutamiento YARP
- ✅ **Logging** estructurado
- ✅ **Configuración** por entornos

## 🔐 Seguridad y Mejores Prácticas

### **🛡️ Características de Seguridad Implementadas**

#### **Docker Security**

- **🔒 Non-root user**: Contenedores como usuario no privilegiado
- **📖 Read-only filesystem**: `read_only: true` con tmpfs para temporales
- **🛡️ No new privileges**: `security_opt: no-new-privileges:true`
- **🔥 Minimal attack surface**: Solo puertos necesarios expuestos

#### **Application Security**

- **🔐 JWT Authentication**: Tokens seguros con expiración configurable
- **⚡ Rate limiting**: Protección contra ataques de fuerza bruta
- **🔍 Request validation**: Validación centralizada de todas las requests
- **📝 Audit logging**: Registro detallado de todas las operaciones
- **🌐 CORS restrictivo**: Configuración granular de orígenes permitidos

#### **Data Security**

- **🗄️ Redis seguro**: Configuración optimizada sin autenticación externa
- **🔒 Environment variables**: .env excluido de git, template disponible
- **📊 Sensitive data exclusion**: Headers sensibles excluidos del caché
- **🔑 Secret management**: Variables sensibles por entorno

### **📊 Monitoreo y Observabilidad**

#### **Health Checks Avanzados**

- **🏥 Microservices health**: Verificación automática de todos los servicios
- **🔄 Circuit breaker**: Fallos automáticos con recuperación
- **⏰ Configurable timeouts**: Diferentes timeouts por servicio
- **📈 Health metrics**: Métricas detalladas de disponibilidad

#### **Logging Estructurado**

- **📝 Serilog integration**: Logging estructurado y configurable
- **🔗 Request correlation**: Seguimiento de requests cross-service
- **📊 Performance metrics**: Tiempos de respuesta y throughput
- **🚨 Error tracking**: Captura y análisis de errores

## 🚀 Despliegue y Producción

### **🌍 Entornos Soportados**

| Entorno        | Comando                       | Puerto | Redis   | Logs    | Descripción                |
| -------------- | ----------------------------- | ------ | ------- | ------- | -------------------------- |
| **Desarrollo** | `docker up -Environment dev`  | 8100   | Memoria | Verbose | Con herramientas de debug  |
| **Testing**    | `docker up -Environment test` | 8080   | Memoria | Warning | Para pruebas automatizadas |
| **Producción** | `docker up -Environment prod` | 8000   | Redis   | Info    | Configuración optimizada   |

### **📦 Gestión de Dependencias**

**Directory.Packages.props** centraliza todas las versiones:

```xml
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
  </PropertyGroup>
  <ItemGroup>
    <!-- Reverse Proxy -->
    <PackageVersion Include="Yarp.ReverseProxy" Version="2.2.0" />

    <!-- Caching -->
    <PackageVersion Include="StackExchange.Redis" Version="2.8.16" />
    <PackageVersion Include="Microsoft.Extensions.Caching.StackExchangeRedis" Version="9.0.0" />

    <!-- Logging -->
    <PackageVersion Include="Serilog.AspNetCore" Version="8.0.3" />
    <PackageVersion Include="Serilog.Sinks.File" Version="6.0.0" />

    <!-- Authentication -->
    <PackageVersion Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="9.0.0" />

    <!-- Testing -->
    <PackageVersion Include="Microsoft.AspNetCore.Mvc.Testing" Version="9.0.0" />
    <PackageVersion Include="xunit" Version="2.9.0" />
    <PackageVersion Include="Moq" Version="4.20.72" />
  </ItemGroup>
</Project>
```

## 🤝 Desarrollo y Contribución

### **🔄 Flujo de Desarrollo Recomendado**

1. **🔍 Verificar estado**: `.\manage-gateway.ps1 verify -Full`
2. **📝 Hacer cambios** en el código
3. **🧪 Ejecutar pruebas**: `.\manage-gateway.ps1 test -TestType Unit`
4. **🔨 Build del proyecto**: `.\manage-gateway.ps1 build`
5. **✅ Verificación completa**: `.\manage-gateway.ps1 verify -Full`
6. **🐳 Deploy local**: `.\manage-gateway.ps1 docker up -Environment dev`
7. **🌐 Verificar APIs**: Acceder a http://localhost:8100/swagger

### **📋 Checklist para Pull Requests**

- [ ] ✅ Todos los tests pasan (`108/108`)
- [ ] 🔨 Build exitoso sin warnings
- [ ] 📚 Documentación actualizada
- [ ] 🔧 Variables de entorno en `.env.example`
- [ ] 🧪 Tests para nuevas funcionalidades
- [ ] 🐳 Docker compose funcional
- [ ] 🔍 Health checks actualizados

## 🧑‍💻 Guía de Uso Completa

### **🎯 Cómo Probar las APIs**

#### **Método 1: Swagger UI (Recomendado)**

1. Iniciar el gateway:

   ```powershell
   .\manage-gateway.ps1 docker up -Environment dev
   ```

2. Ir a: http://localhost:8100/swagger

3. Explorar endpoints organizados por microservicios:

   - 👥 **Users API** (gestión de usuarios)
   - 🔐 **Users Auth** (autenticación JWT)
   - 📊 **Reports API** (informes de accesibilidad)
   - 🔍 **Analysis API** (análisis de sitios web)
   - ⚙️ **Middleware API** (servicios auxiliares)

4. **Probar un endpoint**:
   - Clic en cualquier endpoint
   - Clic en "Try it out"
   - Completar parámetros
   - Clic en "Execute"

#### **Método 2: cURL/Postman**

```bash
# Health Check del Gateway
curl -X GET "http://localhost:8100/health" -H "accept: application/json"

# Obtener token JWT
curl -X POST "http://localhost:8100/api/auth/login" \
  -H "Content-Type: application/json" \
  -d '{
    "email": "usuario@ejemplo.com",
    "password": "password123"
  }'

# Usar token en requests autenticados
curl -X GET "http://localhost:8100/api/v1/users/profile" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"

# Generar reporte de accesibilidad
curl -X POST "http://localhost:8100/api/Report/generate" \
  -H "accept: application/json" \
  -H "Content-Type: application/json" \
  -d '{
    "url": "https://example.com",
    "guidelines": ["WCAG2.1"]
  }'

# Analizar sitio web
curl -X POST "http://localhost:8100/api/Analysis/analyze" \
  -H "Content-Type: application/json" \
  -d '{
    "url": "https://ejemplo.com",
    "depth": 1,
    "includeImages": true
  }'
```

## 📞 Soporte y Troubleshooting

### **🔍 Diagnóstico Rápido**

```powershell
# ✅ Verificar estado completo del proyecto
.\manage-gateway.ps1 verify -Full

# 📊 Ver logs en tiempo real
.\manage-gateway.ps1 docker logs -Follow

# 🔄 Reiniciar servicios específicos
.\manage-gateway.ps1 docker restart

# 🧹 Limpiar y reiniciar completamente
.\manage-gateway.ps1 cleanup -Docker -Volumes
.\manage-gateway.ps1 docker up -Environment dev

# ✅ Verificar configuración automáticamente (gateway detecta variables faltantes)
.\manage-gateway.ps1 verify -Full
```

### **🚨 Problemas Comunes y Soluciones**

| Problema                     | Síntoma                  | Solución                                 |
| ---------------------------- | ------------------------ | ---------------------------------------- |
| **Puerto en uso**            | Error al iniciar Docker  | `.\manage-gateway.ps1 cleanup -Docker`   |
| **Cache no responde**        | 500 errors en requests   | Verificar Redis: `docker logs redis`     |
| **Servicios no disponibles** | Health checks fallando   | `.\manage-gateway.ps1 verify -Full`      |
| **Build errors**             | Errores de compilación   | `.\manage-gateway.ps1 build -Clean`      |
| **Variables faltantes**      | Configuración incompleta | `.\manage-gateway.ps1 verify -Full`      |
| **Tests fallando**           | Test suite errors        | `.\manage-gateway.ps1 test -Verbose`     |
| **JWT inválido**             | 401 unauthorized         | Regenerar token con `/api/auth/login`    |
| **CORS errors**              | Requests bloqueadas      | Verificar `CORS_ALLOWED_ORIGINS` en .env |

### **📊 Logs y Monitoreo**

#### **Archivos de Log**

- **📝 Gateway principal**: `logs/gateway.log`
- **🏥 Health checks**: `logs/health-checks.log`
- **🐳 Docker logs**: `docker-compose logs -f [servicio]`

#### **Métricas Disponibles**

- **⚡ Performance**: Tiempos de respuesta por endpoint
- **📈 Throughput**: Requests por segundo
- **🔍 Health status**: Estado de microservicios
- **💾 Cache hit ratio**: Efectividad del caché
- **🚨 Error rates**: Tasas de error por servicio

---

## 📚 Documentación Consolidada

> **ℹ️ IMPORTANTE**: Este README.md **reemplaza y unifica** la documentación previamente distribuida en:
>
> - ~~`CACHE-IMPLEMENTATION.md`~~ → **Sección:** Sistema de Caché Avanzado
> - ~~`DOCKER-CHANGES-APPLIED.md`~~ → **Sección:** Docker - Configuración Optimizada
> - ~~`DOCKER-IMPROVEMENTS.md`~~ → **Sección:** Docker - Configuración Optimizada
> - ~~`GUIA-DE-USO.md`~~ → **Sección:** Guía de Uso Completa

**✅ Todos los archivos individuales han sido integrados en este documento unificado.**

<div align="center">

---

**🚪 Accessibility Gateway - API Gateway Empresarial Unificado**

**`.\manage-gateway.ps1 help` - ¡Todo lo que necesitas en un solo comando!**

• ✅ **108 tests verificados** • ✅ **0 errores** • ✅ **Docker optimizado** • ✅ **Redis configurado** • ✅ **Documentación unificada** •

[⭐ Star este proyecto](../../) • [🐛 Reportar Bug](../../issues) • [💡 Solicitar Feature](../../issues)

**📅 Última actualización completa:** 31 de agosto de 2025

</div>
