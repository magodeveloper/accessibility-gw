# 📚 Documentación API Gateway - Plataforma de Accesibilidad

Este directorio contiene la documentación completa de la API del Gateway de Accesibilidad, incluyendo todos los endpoints de los microservicios integrados.

## 📋 Archivos Incluidos

### `gateway-complete-api.yaml`

Especificación OpenAPI 3.0.3 completa que incluye:

- ✅ **Gateway Principal**: Endpoints de traducción y enrutamiento
- ✅ **Microservicio Users**: Autenticación, usuarios y preferencias
- ✅ **Microservicio Reports**: Generación y gestión de reportes
- ✅ **Microservicio Analysis**: Motor de análisis de accesibilidad
- ✅ **Middleware API**: Herramientas avanzadas de análisis (axe-core, Equal Access)
- ✅ **Endpoints de Monitoreo**: Health checks, métricas y gestión de caché

### `index.html`

Página de documentación interactiva que incluye:

- 🚀 **Guía de inicio rápido**
- 📊 **Vista general de servicios**
- 🔧 **Interfaz Swagger interactiva**
- 🧪 **Capacidad de testing con JWT tokens**

## 🌐 Servicios Documentados

### 1. Gateway Principal (`/api/v1/`)

- **Traducción de peticiones**: `POST /api/v1/translate`
- **Enrutamiento directo**: `GET|POST|PUT|DELETE /api/v1/services/{service}/{path}`
- **Monitoreo**: Health checks y métricas en tiempo real

### 2. Users & Authentication (`/api/v1/auth/`, `/api/v1/users/`)

- **Autenticación JWT**: Login, logout, registro
- **Gestión de usuarios**: CRUD completo con paginación
- **Preferencias**: Configuración personalizada por usuario

### 3. Reports (`/api/report/`)

- **Gestión de reportes**: Creación, actualización, eliminación
- **Historial**: Tracking de cambios y versiones
- **Filtros**: Por usuario, análisis y fechas

### 4. Analysis (`/api/analysis/`)

- **Motor de análisis**: Múltiples niveles WCAG (A, AA, AAA)
- **Resultados detallados**: Violaciones, advertencias y recomendaciones
- **Gestión de errores**: Tracking y resolución de problemas

### 5. Accessibility Tools (`/api/analyze/`)

- **Análisis de URLs**: Herramientas axe-core y Equal Access
- **Análisis de HTML**: Contenido directo
- **Análisis en lote**: Múltiples URLs en paralelo
- **Configuración avanzada**: Timeouts, viewports, screenshots

### 6. Monitoring (`/health/`, `/metrics/`)

- **Health Checks**: Liveness y readiness probes (Kubernetes)
- **Métricas**: Estadísticas detalladas de uso y rendimiento
- **Gestión de caché**: Invalidación por servicio

## 🚀 Cómo Usar la Documentación

### 1. **Explorar Localmente**

```bash
# Desde el directorio del gateway
cd docs/swagger
python -m http.server 8080

# Abrir en el navegador
open http://localhost:8080
```

### 2. **Integrar con el Gateway**

La documentación se sirve automáticamente desde el gateway en:

- **Swagger UI**: `http://localhost:8000/swagger`
- **OpenAPI JSON**: `http://localhost:8000/swagger/v1/swagger.json`

### 3. **Testing con JWT**

1. Hacer login: `POST /api/v1/auth/login`
2. Copiar el JWT token de la respuesta
3. En la interfaz Swagger, hacer clic en "🔑 Configurar JWT Token"
4. Pegar el token
5. Probar endpoints autenticados

## 🔧 Configuración de Desarrollo

### Prerrequisitos

- ✅ Gateway ejecutándose en `http://localhost:8000`
- ✅ Todos los microservicios disponibles
- ✅ Redis configurado para caché
- ✅ Base de datos de usuarios inicializada

### Variables de Entorno

```bash
# Gateway
GATEWAY_PORT=8000
REDIS_CONNECTION_STRING="localhost:6379"

# Servicios
USERS_SERVICE_URL="http://localhost:5001"
REPORTS_SERVICE_URL="http://localhost:5002"
ANALYSIS_SERVICE_URL="http://localhost:5003"
MIDDLEWARE_SERVICE_URL="http://localhost:3000"

# JWT
JWT_AUTHORITY="https://your-auth-server"
JWT_AUDIENCE="accessibility-api"
```

## 📝 Ejemplos de Uso

### Autenticación

```bash
# Login
curl -X POST "http://localhost:8000/api/v1/auth/login" \
  -H "Content-Type: application/json" \
  -d '{
    "email": "user@example.com",
    "password": "password123"
  }'
```

### Análisis de Accesibilidad

```bash
# Analizar URL
curl -X POST "http://localhost:8000/api/analyze/url" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "url": "https://example.com",
    "tools": ["axe", "equalAccess"],
    "wcagLevel": "AA"
  }'
```

### Crear Reporte

```bash
# Crear reporte
curl -X POST "http://localhost:8000/api/report" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "title": "Reporte de Accesibilidad Web",
    "analysisId": 123
  }'
```

## 🔍 Rate Limiting

La API implementa rate limiting por IP:

- **General**: 100 requests/minuto
- **Análisis**: 20 requests/minuto (endpoints `/api/analyze/*`)

## 🏷️ Versionado

- **Versión actual**: `v1.0.0`
- **Compatibilidad**: OpenAPI 3.0.3
- **Formato de versionado**: Semantic Versioning (SemVer)

## 🤝 Contribuir

Para agregar nuevos endpoints a la documentación:

1. **Actualizar `gateway-complete-api.yaml`**:

   ```yaml
   /api/new-endpoint:
     get:
       tags: [Category]
       summary: Description
       # ... rest of specification
   ```

2. **Agregar ejemplos en `index.html`** si es necesario

3. **Actualizar este README** con la nueva funcionalidad

## 📞 Soporte

- **Documentación técnica**: Ver archivos YAML y HTML
- **Issues**: Crear issue en el repositorio del proyecto
- **Equipo**: accessibility@company.com

---

### 🎯 Estado de Documentación

| Servicio   | Endpoints | Esquemas | Ejemplos | Estado   |
| ---------- | --------- | -------- | -------- | -------- |
| Gateway    | ✅        | ✅       | ✅       | Completo |
| Users      | ✅        | ✅       | ✅       | Completo |
| Reports    | ✅        | ✅       | ✅       | Completo |
| Analysis   | ✅        | ✅       | ✅       | Completo |
| Middleware | ✅        | ✅       | ✅       | Completo |
| Monitoring | ✅        | ✅       | ✅       | Completo |

**📊 Total**: 50+ endpoints documentados | 25+ esquemas definidos | Rate limiting configurado | Autenticación JWT integrada
