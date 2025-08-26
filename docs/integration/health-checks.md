# 🏥 Health Checks para Integración con Gateway

## ⚠️ **Requerimiento**: Endpoints de Health Unificados

El gateway verifica la salud de los microservicios. Cada servicio debe exponer:

### Endpoints Requeridos:

```http
GET /health           # Health check básico
GET /health/live      # Liveness probe (Kubernetes)
GET /health/ready     # Readiness probe (Kubernetes)
```

## 🔧 **Implementación por Servicio**

### .NET Services (Users, Reports, Analysis)

Agregar a `Program.cs`:

```csharp
// Health checks
builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy())
    .AddDbContext<YourDbContext>(); // Si usa base de datos

var app = builder.Build();

// Mapear endpoints de health
app.MapHealthChecks("/health");
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false // Solo check básico
});
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});
```

### Node.js Service (accessibility-mw)

Ya implementado en `src/routes/health.route.ts`:

```typescript
// ✅ YA EXISTE - No requiere cambios
router.get('/', basicHealthCheck);
router.get('/live', livenessCheck);
router.get('/ready', readinessCheck);
router.get('/deep', deepHealthCheck);
```

## 📊 **Formato de Respuesta Esperado**

```json
{
  "status": "Healthy",
  "totalDuration": "00:00:00.0234567",
  "entries": {
    "self": {
      "data": {},
      "description": null,
      "duration": "00:00:00.0123456",
      "status": "Healthy"
    }
  }
}
```

## 🚨 **Estados de Health Check**

| Estado      | Código HTTP | Descripción                              |
| ----------- | ----------- | ---------------------------------------- |
| `Healthy`   | 200         | Servicio funcionando correctamente       |
| `Degraded`  | 200         | Servicio funcional pero con advertencias |
| `Unhealthy` | 503         | Servicio no disponible                   |

## 🎯 **Configuración del Gateway**

El gateway está configurado para verificar cada servicio:

```json
{
  "HealthChecks": {
    "IntervalSeconds": 30,
    "TimeoutSeconds": 10,
    "Services": {
      "users": "http://localhost:5001/health",
      "reports": "http://localhost:5002/health",
      "analysis": "http://localhost:5003/health",
      "middleware": "http://localhost:3000/health"
    }
  }
}
```
