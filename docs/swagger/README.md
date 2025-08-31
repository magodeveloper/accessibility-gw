# 📖 Documentación OpenAPI Completa - Accessibility Platform

Este directorio contiene la documentación OpenAPI completa para el **Accessibility Platform API Gateway**, proporcionando una especificación detallada de todos los endpoints, schemas y casos de uso.

## 📁 Estructura de Archivos

```
docs/swagger/
├── README.md                          # Este archivo
├── index.html                         # Interfaz Swagger UI interactiva
├── gateway-complete-api.yaml          # Especificación OpenAPI principal
├── gateway-microservices-extension.yaml # Documentación de microservicios
└── openapi-examples.yaml             # Ejemplos y casos de uso avanzados
```

## 🚀 Acceso Rápido

### Documentación Interactiva

Abre `index.html` en tu navegador para acceder a la documentación interactiva completa con:

- **Interfaz Swagger UI** moderna y responsiva
- **Autenticación JWT** integrada
- **Ejemplos de código** en múltiples lenguajes
- **Pruebas en vivo** de todos los endpoints

### URLs de Acceso

- **Local**: `file:///c:/Git/accessibility-gw/docs/swagger/index.html`
- **Servidor local**: `http://localhost:8000/docs/swagger/`
- **Producción**: `https://api.accessibility.company.com/docs/`

## 🔧 Configuración

### Prerrequisitos

- Navegador web moderno (Chrome, Firefox, Safari, Edge)
- Conexión a internet (para cargar recursos de Swagger UI)
- Token JWT válido para probar endpoints protegidos

### Servidor Local

Para servir la documentación localmente:

```bash
# Navegar al directorio del gateway
cd c:\Git\accessibility-gw

# Servir archivos estáticos (Python)
python -m http.server 8000

# O usando Node.js
npx http-server -p 8000

# O usando PowerShell (Windows)
# Instalar IIS Express o usar VS Code Live Server
```

## 📋 Contenido de la Documentación

### 🌐 Gateway Principal (`gateway-complete-api.yaml`)

- **Endpoint translate**: `/api/v1/translate` - Enrutamiento a microservicios
- **Endpoints directos**: `/api/v1/services/{service}/{path}` - Llamadas directas
- **Health checks**: `/health`, `/health/live`, `/health/ready`
- **Métricas**: `/metrics` - Información de rendimiento
- **Esquemas base**: TranslateRequest, HealthCheck, Metrics

### 🔧 Microservicios (`gateway-microservices-extension.yaml`)

#### Users Service

- **Autenticación**: Login, registro, refresh token, logout
- **Gestión usuarios**: CRUD completo con paginación y filtros
- **Preferencias**: Configuración personalizada por usuario

#### Reports Service

- **Gestión reportes**: Crear, listar, actualizar, eliminar
- **Descarga**: PDF, HTML, JSON formats
- **Estados**: Draft, InProgress, Completed, Failed

#### Analysis Service

- **Análisis estándar**: Crear, consultar, reintentar
- **Filtros avanzados**: Por estado, nivel WCAG, puntuación
- **Rate limiting**: 20 req/min para operaciones intensivas

#### Middleware Service

- **Análisis avanzado**: URL y HTML directo
- **Herramientas múltiples**: Axe, Equal Access
- **Configuración flexible**: WCAG A/AA/AAA, viewport, timeout
- **Monitoreo progreso**: Estados en tiempo real

### 📚 Ejemplos Avanzados (`openapi-examples.yaml`)

- **Flujos completos**: Registro → Login → Análisis → Reporte
- **Análisis batch**: Múltiples URLs simultáneas
- **Gestión administrativa**: Operaciones con permisos elevados
- **Integración CI/CD**: Ejemplos para pipelines automatizados
- **SDKs**: Código ejemplo para JavaScript, Python, C#

## 🔐 Autenticación

### Flujo de Autenticación

1. **Registrar usuario** (opcional): `POST /api/v1/translate` → users → `/api/v1/auth/register`
2. **Iniciar sesión**: `POST /api/v1/translate` → users → `/api/v1/auth/login`
3. **Obtener token JWT** de la respuesta
4. **Configurar autorización** en Swagger UI: `Bearer <token>`
5. **Usar endpoints protegidos** con el token configurado

### Ejemplo de Autenticación

```javascript
// 1. Login
const loginResponse = await fetch('/api/v1/translate', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({
    service: 'users',
    method: 'POST',
    path: '/api/v1/auth/login',
    body: {
      email: 'usuario@example.com',
      password: 'password123',
    },
  }),
});

const { token } = await loginResponse.json();

// 2. Usar token en peticiones subsecuentes
const analysisResponse = await fetch('/api/v1/translate', {
  method: 'POST',
  headers: {
    'Content-Type': 'application/json',
    Authorization: `Bearer ${token}`,
  },
  body: JSON.stringify({
    service: 'middleware',
    method: 'POST',
    path: '/api/v1/analyze/url',
    body: {
      url: 'https://example.com',
      tools: ['axe', 'equalAccess'],
      wcagLevel: 'AA',
    },
  }),
});
```

## ⚡ Rate Limiting

### Límites por Endpoint

- **General**: 100 requests/minuto por IP
- **Análisis**: 20 requests/minuto por IP
- **Health/Metrics**: Sin límite

### Headers de Rate Limit

```
X-RateLimit-Limit: 100
X-RateLimit-Remaining: 95
X-RateLimit-Reset: 1640995200
```

## 🛠️ Casos de Uso Comunes

### 1. Análisis Simple de URL

```bash
curl -X POST "http://localhost:8000/api/v1/translate" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -d '{
    "service": "middleware",
    "method": "POST",
    "path": "/api/v1/analyze/url",
    "body": {
      "url": "https://example.com",
      "tools": ["axe"],
      "wcagLevel": "AA"
    }
  }'
```

### 2. Generar Reporte PDF

```bash
# 1. Crear análisis
ANALYSIS_ID=$(curl -s -X POST "http://localhost:8000/api/v1/translate" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -d '{
    "service": "analysis",
    "method": "POST",
    "path": "/api/v1/analysis",
    "body": {
      "url": "https://example.com",
      "title": "Mi Análisis",
      "wcagLevel": "AA"
    }
  }' | jq -r '.id')

# 2. Crear reporte
curl -X POST "http://localhost:8000/api/v1/translate" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -d "{
    \"service\": \"reports\",
    \"method\": \"POST\",
    \"path\": \"/api/v1/reports\",
    \"body\": {
      \"title\": \"Reporte de Accesibilidad\",
      \"analysisId\": $ANALYSIS_ID
    }
  }"
```

### 3. Monitoreo de Salud

```bash
# Health check básico
curl http://localhost:8000/health

# Health check completo con métricas
curl "http://localhost:8000/health?deep=true&includeMetrics=true"

# Métricas detalladas
curl http://localhost:8000/metrics
```

## 🔍 Esquemas Principales

### TranslateRequest

Modelo principal para el enrutamiento del gateway:

- `service`: users | reports | analysis | middleware
- `method`: GET | POST | PUT | PATCH | DELETE
- `path`: Ruta del endpoint en el microservicio
- `query`: Parámetros de consulta (opcional)
- `headers`: Headers personalizados (opcional)
- `body`: Cuerpo de la petición (opcional)
- `useCache`: Habilitar caché para GET (opcional)

### AnalysisResult

Respuesta completa de análisis con herramientas múltiples:

- `id`: Identificador único del análisis
- `url`: URL analizada
- `status`: completed | failed | pending
- `tools`: Resultados de Axe y Equal Access
- `summary`: Resumen con puntuación y estadísticas
- `screenshot`: URL de captura de pantalla

### HealthCheckResponse

Estado de salud del sistema:

- `status`: Healthy | Degraded | Unhealthy
- `services`: Estado individual de cada servicio
- `metrics`: Métricas de rendimiento (opcional)

## 🚨 Manejo de Errores

### Códigos de Estado HTTP

- **200**: Operación exitosa
- **201**: Recurso creado
- **400**: Error de validación
- **401**: No autorizado
- **403**: Permisos insuficientes
- **404**: Recurso no encontrado
- **408**: Timeout
- **413**: Payload demasiado grande
- **429**: Rate limit excedido
- **500**: Error interno del servidor
- **502**: Error en microservicio de destino
- **503**: Servicio no disponible

### Estructura de Errores

```json
{
  "error": {
    "code": "VALIDATION_ERROR",
    "message": "Los datos proporcionados no son válidos",
    "details": [
      {
        "field": "email",
        "message": "El formato del email no es válido",
        "code": "INVALID_EMAIL_FORMAT"
      }
    ]
  },
  "timestamp": "2025-08-30T19:30:00Z"
}
```

## 📞 Soporte

### Contacto

- **Email**: accessibility@company.com
- **GitHub**: https://github.com/company/accessibility-platform
- **Documentación**: https://docs.accessibility.company.com

### Recursos Adicionales

- **Guía de integración**: `/docs/integration/`
- **SDKs oficiales**: `/docs/sdks/`
- **Ejemplos de código**: `/docs/examples/`
- **Changelog**: `/docs/changelog.md`

---

## 🎯 Próximos Pasos

1. **Abrir** `index.html` en tu navegador
2. **Probar** la autenticación con tus credenciales
3. **Explorar** los endpoints disponibles
4. **Experimentar** con los ejemplos de código
5. **Integrar** en tu aplicación

¡La documentación está lista para usar! 🚀
