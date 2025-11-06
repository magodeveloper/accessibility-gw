# 🔧 Guía de Troubleshooting

> Soluciones a problemas comunes del Gateway.

## 📋 Tabla de Contenidos

- [Problemas de Inicio](#problemas-de-inicio)
- [Problemas de Conectividad](#problemas-de-conectividad)
- [Problemas de Caché](#problemas-de-caché)
- [Problemas de Rendimiento](#problemas-de-rendimiento)
- [Problemas de Docker](#problemas-de-docker)

---

## 🚫 Problemas de Inicio

### Gateway no inicia

**Síntomas:**

- Error al ejecutar `dotnet run`
- Container de Docker se detiene inmediatamente
- Health checks fallan

**Diagnóstico:**

```bash
# Ver logs
docker logs accessibility-gw

# Verificar configuración
.\manage-gateway.ps1 verify -Full

# Ver puertos en uso
netstat -ano | findstr :8100
```

**Soluciones:**

1. **Puerto en uso**

```bash
# Cambiar puerto
$env:ASPNETCORE_URLS = "http://+:8101"
dotnet run
```

2. **Variables faltantes**

```bash
# Copiar template
cp .env.example .env

# Verificar variables
.\manage-gateway.ps1 verify -Full
```

3. **Dependencias faltantes**

```bash
# Restaurar packages
dotnet restore

# Limpiar y rebuild
.\manage-gateway.ps1 build -Clean
```

---

## 🌐 Problemas de Conectividad

### Redis no conecta

**Síntomas:**

- Errores: `StackExchange.Redis.RedisConnectionException`
- Cache no funciona
- Timeouts frecuentes

**Diagnóstico:**

```bash
# Verificar Redis
docker ps | grep redis

# Test conexión
redis-cli -h localhost -p 6379 ping

# Ver logs Redis
docker logs redis
```

**Soluciones:**

1. **Redis no está ejecutándose**

```bash
# Iniciar Redis
docker-compose up -d redis

# O con script
.\manage-gateway.ps1 docker up
```

2. **ConnectionString incorrecta**

```env
# .env - Verificar
REDIS_CONNECTION_STRING=localhost:6379  # Local
REDIS_CONNECTION_STRING=redis:6379      # Docker
```

3. **Usar fallback a memoria**

```bash
# Gateway detecta automáticamente y usa memoria
# Ver logs para confirmación
docker logs accessibility-gw | grep -i "cache"
```

### Microservicios no responden

**Síntomas:**

- 502 Bad Gateway
- 504 Gateway Timeout
- Health checks fallan

**Diagnóstico:**

```bash
# Verificar servicios
curl http://localhost:8081/health  # Users
curl http://localhost:8082/health  # Analysis
curl http://localhost:8083/health  # Reports

# Health detallado del gateway
curl http://localhost:8100/health/ready | jq
```

**Soluciones:**

1. **Verificar URLs en configuración**

```env
# .env
GATE__SERVICES__USERS=http://msusers-api:8081
GATE__SERVICES__ANALYSIS=http://msanalysis-api:8082
GATE__SERVICES__REPORTS=http://msreports-api:8083
```

2. **Verificar red Docker**

```bash
# Ver redes
docker network inspect accessibility-shared

# Recrear red si es necesario
docker network rm accessibility-shared
docker network create accessibility-shared
```

3. **Aumentar timeouts**

```env
GATEWAY_REQUEST_TIMEOUT_SECONDS=60
CIRCUIT_BREAKER_TIMEOUT_SECONDS=45
```

---

## 💾 Problemas de Caché

### Hit Ratio Bajo (< 50%)

**Diagnóstico:**

```bash
# Ver estadísticas
curl http://localhost:8100/api/cache/stats

# Ver métricas
curl http://localhost:8100/metrics | grep cache
```

**Soluciones:**

1. **Aumentar TTL**

```env
CACHE_DEFAULT_TTL_MINUTES=60  # Aumentar de 30 a 60
```

2. **Verificar invalidación**

```csharp
// Revisar que no se invalide demasiado frecuentemente
// Ajustar patrones de invalidación
```

3. **Cache warming**

```bash
# Precachear datos comunes al inicio
# Ver docs/CACHE.md para implementación
```

### Memoria Cache Crece Demasiado

**Síntomas:**

- Alto uso de memoria
- OutOfMemoryException
- Performance degradada

**Soluciones:**

1. **Configurar límite de memoria**

```csharp
// Program.cs
builder.Services.AddMemoryCache(options =>
{
    options.SizeLimit = 100 * 1024 * 1024; // 100MB
    options.CompactionPercentage = 0.20;
});
```

2. **Reducir TTL**

```env
CACHE_DEFAULT_TTL_MINUTES=15  # Reducir de 30 a 15
```

3. **Usar Redis obligatoriamente**

```env
CACHE_FALLBACK_TO_MEMORY=false
```

---

## ⚡ Problemas de Rendimiento

### Response Time Alto (> 500ms)

**Diagnóstico:**

```bash
# Ver métricas de latencia
curl http://localhost:8100/metrics | grep duration

# Logs de requests lentos
grep "Duration.*[0-9]{3,}" logs/gateway-*.log
```

**Soluciones:**

1. **Habilitar/optimizar caché**

```bash
# Verificar cache stats
curl http://localhost:8100/api/cache/stats

# Objetivo: hit ratio > 80%
```

2. **Aumentar recursos Docker**

```yaml
# docker-compose.yml
deploy:
  resources:
    limits:
      cpus: "2.0"
      memory: 1G
```

3. **Revisar backends lentos**

```bash
# Identificar servicio lento
curl http://localhost:8100/health/ready | jq '.entries'
```

### Rate Limiting Demasiado Restrictivo

**Síntomas:**

- Muchas respuestas 429
- Usuarios reportan bloqueos

**Soluciones:**

1. **Aumentar límites**

```env
# .env
RATE_LIMIT_REQUESTS_PER_MINUTE=200  # Aumentar de 100
RATE_LIMIT_BURST_SIZE=40             # Aumentar de 20
```

2. **Límites diferentes por tier**

```csharp
// Configurar políticas por tipo de usuario
// Ver docs/SECURITY.md para implementación
```

---

## 🐳 Problemas de Docker

### Container Se Detiene Inmediatamente

**Diagnóstico:**

```bash
# Ver logs
docker logs accessibility-gw

# Ver exit code
docker inspect accessibility-gw | jq '.[0].State'
```

**Soluciones:**

1. **Error en ENTRYPOINT**

```bash
# Probar manualmente
docker run -it accessibility-gw sh
dotnet Gateway.dll
```

2. **Dependencias faltantes**

```dockerfile
# Verificar que estén instaladas:
RUN apk add --no-cache curl icu-libs tzdata
```

3. **Variables de entorno**

```bash
# Verificar env vars
docker exec accessibility-gw env
```

### Health Check Falla

**Síntomas:**

- Docker marca container como unhealthy
- Container se reinicia frecuentemente

**Diagnóstico:**

```bash
# Ver health check status
docker inspect accessibility-gw | jq '.[0].State.Health'

# Probar health check manualmente
docker exec accessibility-gw curl http://localhost:8100/health/live
```

**Soluciones:**

1. **Aumentar start_period**

```yaml
# docker-compose.yml
healthcheck:
  start_period: 60s # Aumentar de 40s
```

2. **Verificar endpoint**

```bash
# Debe responder 200
curl -v http://localhost:8100/health/live
```

### Volúmenes de Logs No Funcionan

**Síntomas:**

- No se generan archivos de logs
- Error de permisos

**Soluciones:**

1. **Verificar permisos**

```bash
# Crear directorio con permisos
mkdir -p logs
chmod 777 logs
```

2. **Usar tmpfs en dev**

```yaml
# docker-compose.dev.yml
volumes:
  - ./logs:/app/logs:rw
```

3. **Verificar tmpfs en prod**

```yaml
# docker-compose.yml
tmpfs:
  - /app/logs
```

---

## 📋 Checklist de Diagnóstico

### Cuando algo falla, verificar:

- [ ] ✅ **Puerto 8100 disponible**: `netstat -ano | findstr :8100`
- [ ] ✅ **Redis ejecutándose**: `docker ps | grep redis`
- [ ] ✅ **Variables de entorno**: `.\manage-gateway.ps1 verify -Full`
- [ ] ✅ **Red Docker**: `docker network inspect accessibility-shared`
- [ ] ✅ **Logs del Gateway**: `docker logs accessibility-gw`
- [ ] ✅ **Health checks**: `curl http://localhost:8100/health/ready`
- [ ] ✅ **Microservicios disponibles**: Verificar cada health endpoint
- [ ] ✅ **Espacio en disco**: `docker system df`
- [ ] ✅ **Memoria disponible**: `docker stats`

---

## 🆘 Comandos de Emergencia

### Reset Completo

```bash
# 1. Detener todo
docker-compose down -v

# 2. Limpiar Docker
.\manage-gateway.ps1 cleanup -Docker -Volumes -Force

# 3. Limpiar builds
.\manage-gateway.ps1 cleanup -Builds

# 4. Rebuild desde cero
.\manage-gateway.ps1 build -Clean

# 5. Recrear red
docker network rm accessibility-shared
docker network create accessibility-shared

# 6. Iniciar de nuevo
.\manage-gateway.ps1 docker up -Rebuild
```

### Logs de Emergencia

```bash
# Exportar todos los logs
docker-compose logs > all-logs.txt

# Logs con timestamps
docker-compose logs --timestamps > logs-timestamped.txt

# Últimas 500 líneas de cada servicio
docker-compose logs --tail=500 > logs-recent.txt
```

---

## 📞 Obtener Ayuda

### Información para Reportar Issues

```bash
# 1. Versión del Gateway
docker inspect accessibility-gw | jq '.[0].Config.Labels'

# 2. Estado del sistema
.\manage-gateway.ps1 verify -Full > system-info.txt

# 3. Logs recientes
docker-compose logs --tail=200 > recent-logs.txt

# 4. Health check
curl http://localhost:8100/health/ready > health.json

# 5. Métricas
curl http://localhost:8100/metrics > metrics.txt
```

---

## 📚 Referencias

- [Docker Troubleshooting](https://docs.docker.com/config/daemon/troubleshoot/)
- [ASP.NET Core Troubleshooting](https://learn.microsoft.com/en-us/aspnet/core/test/troubleshoot)
- [Redis Troubleshooting](https://redis.io/docs/management/optimization/)

---

[⬅️ Volver al README](../README.new.md)
