# 🔄 Configuración de URLs para Microservicios

## ⚠️ **Cambio Crítico**: Usar Gateway como Proxy

### ANTES (comunicación directa):

```json
{
  "ExternalServices": {
    "UsersApi": {
      "BaseUrl": "http://localhost:5001"
    },
    "ReportsApi": {
      "BaseUrl": "http://localhost:5002"
    },
    "AnalysisApi": {
      "BaseUrl": "http://localhost:5003"
    }
  }
}
```

### DESPUÉS (a través del gateway):

```json
{
  "ExternalServices": {
    "UsersApi": {
      "BaseUrl": "http://localhost:8000/api/v1/services/users"
    },
    "ReportsApi": {
      "BaseUrl": "http://localhost:8000/api/v1/services/reports"
    },
    "AnalysisApi": {
      "BaseUrl": "http://localhost:8000/api/v1/services/analysis"
    }
  }
}
```

## 📂 **Archivos a Actualizar**

### accessibility-ms-analysis

```bash
# Archivo: src/Analysis.Infrastructure/appsettings.json
# Cambiar URLs para usar el gateway
```

### accessibility-ms-reports

```bash
# Archivo: src/Reports.Infrastructure/appsettings.json
# Cambiar URLs para usar el gateway
```

### accessibility-mw

```bash
# Archivo: .env
ANALYSIS_API_URL=http://localhost:8000/api/v1/services/analysis
```

## 🎯 **Beneficios del Cambio**

1. **Unified Logging**: Todas las peticiones pasan por el gateway
2. **Caching**: Beneficio automático del caché distribuido
3. **Rate Limiting**: Protección unificada
4. **Circuit Breaker**: Resiliencia automática
5. **Metrics**: Telemetría centralizada
6. **Security**: Headers de seguridad uniformes

## ⚡ **URLs de Enrutamiento del Gateway**

| Destino      | URL Original                         | URL a través del Gateway                                       |
| ------------ | ------------------------------------ | -------------------------------------------------------------- |
| Users API    | `http://localhost:5001/api/v1/users` | `http://localhost:8000/api/v1/services/users/api/v1/users`     |
| Reports API  | `http://localhost:5002/api/report`   | `http://localhost:8000/api/v1/services/reports/api/report`     |
| Analysis API | `http://localhost:5003/api/analysis` | `http://localhost:8000/api/v1/services/analysis/api/analysis`  |
| Middleware   | `http://localhost:3000/api/analyze`  | `http://localhost:8000/api/v1/services/middleware/api/analyze` |

## 🔐 **Autenticación Transparente**

El gateway propaga automáticamente el header `Authorization`, por lo que los microservicios reciben el JWT sin cambios.
