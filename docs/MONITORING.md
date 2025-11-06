# 📊 Monitoreo y Observabilidad

> Guía de health checks, métricas, logging y monitoreo del Gateway.

## 📋 Tabla de Contenidos

- [Health Checks](#health-checks)
- [Métricas](#métricas)
- [Logging](#logging)
- [Dashboards](#dashboards)

---

## 🏥 Health Checks

### Endpoints Disponibles

#### GET /health/live

**Liveness probe** - Verifica que el Gateway esté ejecutándose.

```bash
curl http://localhost:8100/health/live
```

**Response 200:**

```json
{
  "status": "Healthy"
}
```

#### GET /health/ready

**Readiness probe** - Verifica disponibilidad de todos los servicios.

```bash
curl http://localhost:8100/health/ready
```

**Response 200:**

```json
{
  "status": "Healthy",
  "totalDuration": "00:00:00.0562741",
  "entries": {
    "gateway": { "status": "Healthy" },
    "redis": {
      "status": "Healthy",
      "data": { "connection": "Connected" }
    },
    "users-api": { "status": "Healthy" },
    "analysis-api": { "status": "Healthy" },
    "reports-api": { "status": "Healthy" }
  }
}
```

### Configuración en Kubernetes

```yaml
livenessProbe:
  httpGet:
    path: /health/live
    port: 8100
  initialDelaySeconds: 10
  periodSeconds: 30

readinessProbe:
  httpGet:
    path: /health/ready
    port: 8100
  initialDelaySeconds: 20
  periodSeconds: 10
```

---

## 📈 Métricas

### Endpoint de Métricas

```bash
curl http://localhost:8100/metrics
```

### Métricas Disponibles

```prometheus
# Request totals
gateway_requests_total{endpoint="/api/users",method="GET",status="200"} 1523

# Request duration
gateway_request_duration_seconds{endpoint="/api/users",quantile="0.95"} 0.245

# Cache performance
gateway_cache_hits_total{endpoint="/api/users"} 892
gateway_cache_misses_total{endpoint="/api/users"} 631
gateway_cache_hit_ratio{endpoint="/api/users"} 0.586

# Backend health
gateway_backend_health{service="users-api",status="healthy"} 1
gateway_backend_health{service="redis",status="healthy"} 1

# Rate limiting
gateway_rate_limit_hits_total{endpoint="/api/auth/login"} 23

# Errors
gateway_errors_total{endpoint="/api/users",type="timeout"} 5
```

### Configuración Prometheus

```yaml
# prometheus.yml
scrape_configs:
  - job_name: "accessibility-gateway"
    static_configs:
      - targets: ["localhost:8100"]
    metrics_path: "/metrics"
    scrape_interval: 15s
```

### Queries Útiles

```promql
# Request rate (últimos 5 minutos)
rate(gateway_requests_total[5m])

# P95 latency
histogram_quantile(0.95, gateway_request_duration_seconds)

# Error rate
sum(rate(gateway_errors_total[5m])) / sum(rate(gateway_requests_total[5m]))

# Cache hit ratio
gateway_cache_hits_total / (gateway_cache_hits_total + gateway_cache_misses_total)
```

---

## 📝 Logging

### Configuración Serilog

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
          "outputTemplate": "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}"
        }
      },
      {
        "Name": "File",
        "Args": {
          "path": "logs/gateway-.log",
          "rollingInterval": "Day",
          "retainedFileCountLimit": 7,
          "outputTemplate": "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}"
        }
      }
    ],
    "Enrich": ["FromLogContext", "WithMachineName", "WithThreadId"]
  }
}
```

### Estructura de Logs

```json
{
  "@timestamp": "2025-11-06T10:30:15.123Z",
  "@level": "Information",
  "@message": "Request processed successfully",
  "RequestId": "abc123-def456",
  "CorrelationId": "xyz789",
  "UserId": "user123",
  "Endpoint": "/api/users/profile",
  "Method": "GET",
  "StatusCode": 200,
  "Duration": 245,
  "CacheHit": true,
  "BackendService": "users-api"
}
```

### Niveles de Log

| Nivel           | Cuándo usar                       | Ejemplo              |
| --------------- | --------------------------------- | -------------------- |
| **Trace**       | Debugging detallado               | Valores de variables |
| **Debug**       | Información de desarrollo         | Flujo de ejecución   |
| **Information** | Eventos normales                  | Request procesado    |
| **Warning**     | Situaciones anormales no críticas | Cache miss           |
| **Error**       | Errores manejados                 | Timeout de backend   |
| **Critical**    | Fallos que requieren atención     | Redis no disponible  |

### Ver Logs

```bash
# Logs en tiempo real
tail -f logs/gateway-20251106.log

# Últimas 100 líneas
tail -n 100 logs/gateway-20251106.log

# Filtrar por nivel
grep "ERROR" logs/gateway-*.log

# Filtrar por usuario
grep "user123" logs/gateway-*.log

# Docker logs
docker logs -f accessibility-gw
```

---

## 📊 Dashboards

### Grafana Dashboard

```json
{
  "dashboard": {
    "title": "Accessibility Gateway",
    "panels": [
      {
        "title": "Request Rate",
        "targets": [
          {
            "expr": "rate(gateway_requests_total[5m])"
          }
        ]
      },
      {
        "title": "P95 Latency",
        "targets": [
          {
            "expr": "histogram_quantile(0.95, gateway_request_duration_seconds)"
          }
        ]
      },
      {
        "title": "Error Rate",
        "targets": [
          {
            "expr": "sum(rate(gateway_errors_total[5m])) / sum(rate(gateway_requests_total[5m]))"
          }
        ]
      },
      {
        "title": "Cache Hit Ratio",
        "targets": [
          {
            "expr": "gateway_cache_hit_ratio"
          }
        ]
      }
    ]
  }
}
```

### Métricas Clave

| Métrica                 | Objetivo     | Alerta        |
| ----------------------- | ------------ | ------------- |
| **Uptime**              | > 99.9%      | < 99%         |
| **Response Time (P95)** | < 200ms      | > 500ms       |
| **Error Rate**          | < 1%         | > 5%          |
| **Cache Hit Ratio**     | > 80%        | < 50%         |
| **Backend Health**      | 100% healthy | Any unhealthy |

### Alertas

```yaml
# alerts.yml
groups:
  - name: gateway
    rules:
      - alert: HighErrorRate
        expr: rate(gateway_errors_total[5m]) > 0.05
        for: 5m
        labels:
          severity: critical
        annotations:
          summary: "High error rate detected"

      - alert: HighLatency
        expr: histogram_quantile(0.95, gateway_request_duration_seconds) > 0.5
        for: 5m
        labels:
          severity: warning
        annotations:
          summary: "High latency detected"

      - alert: LowCacheHitRatio
        expr: gateway_cache_hit_ratio < 0.5
        for: 10m
        labels:
          severity: warning
        annotations:
          summary: "Cache hit ratio is low"
```

---

## 🔍 Troubleshooting

### Verificar Health

```bash
# Health check rápido
curl http://localhost:8100/health/live

# Health detallado
curl http://localhost:8100/health/ready | jq

# Con timeout
curl --max-time 5 http://localhost:8100/health/ready
```

### Analizar Métricas

```bash
# Métricas actuales
curl http://localhost:8100/metrics

# Filtrar métricas específicas
curl http://localhost:8100/metrics | grep gateway_requests_total

# Exportar métricas
curl http://localhost:8100/metrics > metrics.txt
```

### Revisar Logs

```bash
# Errores recientes
grep "ERROR" logs/gateway-$(date +%Y%m%d).log | tail -20

# Requests lentos (>1s)
grep "Duration.*[0-9]{4,}" logs/gateway-*.log

# Por correlation ID
grep "CorrelationId.*xyz789" logs/gateway-*.log
```

---

## 📚 Referencias

- [ASP.NET Core Health Checks](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/health-checks)
- [Prometheus Documentation](https://prometheus.io/docs/)
- [Grafana Documentation](https://grafana.com/docs/)
- [Serilog Documentation](https://serilog.net/)

---

[⬅️ Volver al README](../README.new.md)
