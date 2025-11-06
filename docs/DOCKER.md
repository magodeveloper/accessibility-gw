# 🐳 Guía de Docker

> Documentación completa de containerización: Dockerfile, Docker Compose y mejores prácticas.

## 📋 Tabla de Contenidos

- [Dockerfile](#dockerfile)
- [Docker Compose](#docker-compose)
- [Construcción de Imágenes](#construcción-de-imágenes)
- [Configuración de Red](#configuración-de-red)
- [Seguridad](#seguridad)
- [Troubleshooting](#troubleshooting)

---

## 📦 Dockerfile

### Multi-Stage Build Optimizado

```dockerfile
# ==========================================
# Stage 1: Build
# ==========================================
FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build
WORKDIR /src

# Copiar archivos de proyecto para restore
COPY ["src/Gateway/Gateway.csproj", "Gateway/"]
COPY ["Directory.Packages.props", "./"]

# Restore dependencies
RUN dotnet restore "Gateway/Gateway.csproj"

# Copiar todo el código fuente
COPY src/ .

# Build en Release
RUN dotnet build "Gateway/Gateway.csproj" \
    -c Release \
    --no-restore \
    -o /app/build

# ==========================================
# Stage 2: Publish
# ==========================================
FROM build AS publish
RUN dotnet publish "Gateway/Gateway.csproj" \
    -c Release \
    --no-build \
    -o /app/publish \
    --self-contained false

# ==========================================
# Stage 3: Runtime
# ==========================================
FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS final
WORKDIR /app

# Crear usuario y grupo no-root
RUN addgroup -S appgroup && \
    adduser -S appuser -G appgroup

# Instalar dependencias mínimas necesarias
RUN apk add --no-cache \
    curl \
    icu-libs \
    tzdata

# Configurar timezone
ENV TZ=America/Mexico_City

# Copiar archivos publicados
COPY --from=publish --chown=appuser:appgroup /app/publish .

# Crear directorio para logs
RUN mkdir -p /app/logs && \
    chown -R appuser:appgroup /app/logs

# Cambiar a usuario no-root
USER appuser

# Exponer puerto
EXPOSE 8100

# Health check
HEALTHCHECK --interval=30s \
            --timeout=10s \
            --start-period=40s \
            --retries=3 \
    CMD curl --fail http://localhost:8100/health/live || exit 1

# Punto de entrada
ENTRYPOINT ["dotnet", "Gateway.dll"]
```

### Optimizaciones del Dockerfile

| Optimización      | Beneficio                     | Implementación                       |
| ----------------- | ----------------------------- | ------------------------------------ |
| **Multi-stage**   | Imagen más pequeña            | 3 stages: build, publish, runtime    |
| **Alpine Linux**  | -60% tamaño                   | Base `alpine` en lugar de `bullseye` |
| **Layer caching** | Build más rápido              | COPY selectivo por layers            |
| **Non-root user** | Seguridad                     | Usuario `appuser` sin privilegios    |
| **Health check**  | Monitoreo                     | Endpoint `/health/live`              |
| **Minimal deps**  | Superficie de ataque reducida | Solo `curl`, `icu-libs`, `tzdata`    |

---

## 🐳 Docker Compose

### Producción (docker-compose.yml)

```yaml
version: "3.8"

services:
  # Gateway Principal
  accessibility-gateway:
    build:
      context: .
      dockerfile: Dockerfile
      args:
        BUILD_CONFIGURATION: Release

    container_name: accessibility-gw-prod
    hostname: accessibility-gateway

    # Puertos
    ports:
      - "8000:8100"

    # Variables de entorno
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ASPNETCORE_URLS=http://+:8100
      - TZ=America/Mexico_City
      - REDIS_CONNECTION_STRING=redis:6379
      - GATE__SERVICES__USERS=http://msusers-api:8081
      - GATE__SERVICES__REPORTS=http://msreports-api:8083
      - GATE__SERVICES__ANALYSIS=http://msanalysis-api:8082
      - GATE__SERVICES__MIDDLEWARE=http://accessibility-mw:3001

    # Archivos de environment
    env_file:
      - .env

    # Dependencias
    depends_on:
      redis:
        condition: service_healthy

    # Redes
    networks:
      - accessibility-shared

    # Volúmenes
    volumes:
      - gateway-logs:/app/logs:rw

    # Seguridad
    security_opt:
      - no-new-privileges:true
    read_only: true

    # Tmpfs para archivos temporales
    tmpfs:
      - /tmp
      - /app/logs

    # Capabilities mínimas
    cap_drop:
      - ALL
    cap_add:
      - NET_BIND_SERVICE

    # Resource limits
    deploy:
      resources:
        limits:
          cpus: "1.0"
          memory: 512M
        reservations:
          cpus: "0.5"
          memory: 256M

    # Health check
    healthcheck:
      test: ["CMD", "curl", "--fail", "http://localhost:8100/health/live"]
      interval: 30s
      timeout: 10s
      retries: 3
      start_period: 40s

    # Restart policy
    restart: unless-stopped

    # Labels
    labels:
      com.accessibility.service: "gateway"
      com.accessibility.version: "2.0.0"
      com.accessibility.environment: "production"

  # Redis Cache
  redis:
    image: redis:7-alpine
    container_name: accessibility-redis
    hostname: redis

    # Comando con optimizaciones
    command: >
      redis-server 
      --appendonly yes 
      --appendfsync everysec
      --maxmemory 256mb
      --maxmemory-policy allkeys-lru
      --tcp-keepalive 60
      --timeout 0
      --save 900 1 300 10

    # Puertos
    ports:
      - "6379:6379"

    # Volúmenes
    volumes:
      - redis-data:/data

    # Redes
    networks:
      - accessibility-shared

    # Seguridad
    security_opt:
      - no-new-privileges:true

    # Resource limits
    deploy:
      resources:
        limits:
          cpus: "0.5"
          memory: 256M
        reservations:
          cpus: "0.25"
          memory: 128M

    # Health check
    healthcheck:
      test: ["CMD", "redis-cli", "ping"]
      interval: 10s
      timeout: 5s
      retries: 3
      start_period: 10s

    # Restart policy
    restart: unless-stopped

    # Labels
    labels:
      com.accessibility.service: "cache"
      com.accessibility.version: "7.2"

# Volúmenes
volumes:
  gateway-logs:
    driver: local
    labels:
      com.accessibility.volume: "gateway-logs"

  redis-data:
    driver: local
    labels:
      com.accessibility.volume: "redis-data"

# Redes
networks:
  accessibility-shared:
    external: true
    name: accessibility-shared
```

### Desarrollo (docker-compose.dev.yml)

```yaml
version: "3.8"

services:
  accessibility-gateway:
    build:
      context: .
      dockerfile: Dockerfile
      target: build # Solo hasta stage build para hot-reload

    container_name: accessibility-gw-dev

    # Puerto desarrollo
    ports:
      - "8100:8100"

    # Environment desarrollo
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ASPNETCORE_URLS=http://+:8100
      - REDIS_CONNECTION_STRING=redis:6379

    # Volúmenes para hot-reload
    volumes:
      - ./src:/src:ro
      - ./logs:/app/logs:rw

    # Sin restricciones de seguridad para desarrollo
    security_opt: []
    read_only: false

    # Sin límites de recursos en desarrollo
    deploy:
      resources:
        limits:
          cpus: "2.0"
          memory: 1G

    # Health check más frecuente
    healthcheck:
      test: ["CMD", "curl", "--fail", "http://localhost:8100/health/live"]
      interval: 10s
      timeout: 5s
      retries: 2
      start_period: 20s

    networks:
      - accessibility-shared

    depends_on:
      redis:
        condition: service_healthy

  redis:
    image: redis:7-alpine
    container_name: accessibility-redis-dev

    ports:
      - "6379:6379"

    # Sin persistencia en desarrollo
    command: redis-server --appendonly no

    networks:
      - accessibility-shared

    healthcheck:
      test: ["CMD", "redis-cli", "ping"]
      interval: 5s
      timeout: 3s
      retries: 3

  # Redis Commander (herramienta de gestión)
  redis-commander:
    image: rediscommander/redis-commander:latest
    container_name: redis-commander

    environment:
      - REDIS_HOSTS=local:redis:6379

    ports:
      - "8081:8081"

    networks:
      - accessibility-shared

    depends_on:
      - redis

    profiles:
      - tools

networks:
  accessibility-shared:
    external: true
```

---

## 🔨 Construcción de Imágenes

### Comandos Básicos

```bash
# Build imagen de producción
docker build -t accessibility-gw:latest .

# Build con tag específico
docker build -t accessibility-gw:2.0.0 .

# Build con argumentos
docker build \
  --build-arg BUILD_CONFIGURATION=Release \
  -t accessibility-gw:latest .

# Build sin cache
docker build --no-cache -t accessibility-gw:latest .

# Build multi-plataforma
docker buildx build \
  --platform linux/amd64,linux/arm64 \
  -t accessibility-gw:latest .
```

### Usando Docker Compose

```bash
# Build servicios
docker-compose build

# Build sin cache
docker-compose build --no-cache

# Build servicio específico
docker-compose build accessibility-gateway

# Build y ejecutar
docker-compose up --build
```

### Optimización de Build

```bash
# Ver tamaño de capas
docker history accessibility-gw:latest

# Analizar imagen
docker scout cves accessibility-gw:latest

# Limpieza de build cache
docker builder prune

# Limpieza completa
docker system prune -a --volumes
```

---

## 🌐 Configuración de Red

### Crear Red Compartida

```bash
# Crear red externa
docker network create \
  --driver bridge \
  --subnet 172.22.0.0/16 \
  --gateway 172.22.0.1 \
  accessibility-shared

# Ver redes
docker network ls

# Inspeccionar red
docker network inspect accessibility-shared
```

### Configuración de Red en Compose

```yaml
networks:
  accessibility-shared:
    external: true
    ipam:
      driver: default
      config:
        - subnet: 172.22.0.0/16
          gateway: 172.22.0.1
```

### Asignación de IPs Estáticas

```yaml
services:
  accessibility-gateway:
    networks:
      accessibility-shared:
        ipv4_address: 172.22.0.10

  redis:
    networks:
      accessibility-shared:
        ipv4_address: 172.22.0.11
```

---

## 🔒 Seguridad

### Mejores Prácticas Implementadas

#### 1. Usuario No-Root

```dockerfile
# Crear usuario sin privilegios
RUN addgroup -S appgroup && adduser -S appuser -G appgroup

# Cambiar ownership
RUN chown -R appuser:appgroup /app

# Switch a usuario
USER appuser
```

#### 2. Filesystem Read-Only

```yaml
services:
  accessibility-gateway:
    read_only: true
    tmpfs:
      - /tmp
      - /app/logs
```

#### 3. Drop Capabilities

```yaml
services:
  accessibility-gateway:
    cap_drop:
      - ALL
    cap_add:
      - NET_BIND_SERVICE # Solo lo necesario
```

#### 4. Security Options

```yaml
services:
  accessibility-gateway:
    security_opt:
      - no-new-privileges:true
```

#### 5. Resource Limits

```yaml
services:
  accessibility-gateway:
    deploy:
      resources:
        limits:
          cpus: "1.0"
          memory: 512M
        reservations:
          cpus: "0.5"
          memory: 256M
```

### Escaneo de Vulnerabilidades

```bash
# Docker Scout
docker scout cves accessibility-gw:latest

# Trivy
trivy image accessibility-gw:latest

# Snyk
snyk container test accessibility-gw:latest
```

---

## 🚀 Gestión de Contenedores

### Comandos de Compose

```bash
# Iniciar servicios
docker-compose up -d

# Iniciar con herramientas (dev)
docker-compose --profile tools up -d

# Ver logs
docker-compose logs -f

# Ver logs de servicio específico
docker-compose logs -f accessibility-gateway

# Estado de servicios
docker-compose ps

# Detener servicios
docker-compose down

# Detener y remover volúmenes
docker-compose down -v

# Reiniciar servicio
docker-compose restart accessibility-gateway

# Ver uso de recursos
docker stats
```

### Comandos Docker Directos

```bash
# Ejecutar contenedor
docker run -d \
  --name accessibility-gw \
  --env-file .env \
  -p 8100:8100 \
  accessibility-gw:latest

# Ver logs
docker logs -f accessibility-gw

# Ejecutar comando en contenedor
docker exec -it accessibility-gw sh

# Inspeccionar contenedor
docker inspect accessibility-gw

# Ver estadísticas
docker stats accessibility-gw

# Detener contenedor
docker stop accessibility-gw

# Remover contenedor
docker rm accessibility-gw
```

---

## 🔧 Troubleshooting

### Problemas Comunes

#### Gateway no inicia

```bash
# Ver logs detallados
docker-compose logs accessibility-gateway

# Verificar health check
docker inspect accessibility-gw | jq '.[0].State.Health'

# Probar conexión manual
docker exec accessibility-gw curl http://localhost:8100/health/live
```

#### Redis no conecta

```bash
# Verificar Redis
docker-compose logs redis

# Probar conexión
docker exec redis redis-cli ping

# Ver configuración
docker exec redis redis-cli CONFIG GET '*'
```

#### Problemas de Red

```bash
# Ver redes del contenedor
docker inspect accessibility-gw | jq '.[0].NetworkSettings.Networks'

# Probar conectividad entre contenedores
docker exec accessibility-gw ping redis

# Ver DNS
docker exec accessibility-gw cat /etc/resolv.conf
```

#### Puerto en Uso

```bash
# Ver procesos usando puerto
netstat -ano | findstr :8100  # Windows
lsof -i :8100                 # Linux/Mac

# Cambiar puerto en compose
ports:
  - '8101:8100'  # Host:Container
```

#### Imagen Muy Grande

```bash
# Ver tamaño de capas
docker history accessibility-gw:latest

# Optimizar:
# 1. Usar .dockerignore
# 2. Multi-stage builds
# 3. Alpine base images
# 4. Minimizar layers
```

### Limpieza y Mantenimiento

```bash
# Limpiar contenedores parados
docker container prune

# Limpiar imágenes sin usar
docker image prune -a

# Limpiar volúmenes sin usar
docker volume prune

# Limpiar todo
docker system prune -a --volumes

# Ver uso de espacio
docker system df
```

### Logs y Debugging

```bash
# Logs con timestamps
docker-compose logs -f --timestamps

# Últimas 100 líneas
docker-compose logs --tail=100

# Logs desde hace 10 minutos
docker-compose logs --since 10m

# Exportar logs
docker-compose logs > gateway-logs.txt

# Ver eventos Docker
docker events --filter container=accessibility-gw
```

---

## 📊 Monitoreo

### Health Checks

```bash
# Estado de health check
docker inspect accessibility-gw \
  | jq '.[0].State.Health'

# Ejecutar health check manualmente
docker exec accessibility-gw \
  curl --fail http://localhost:8100/health/live
```

### Métricas

```bash
# CPU, memoria, red, I/O
docker stats accessibility-gw

# Stats en formato JSON
docker stats --no-stream --format json
```

### Watchtower (Auto-updates)

```yaml
# Agregar a docker-compose.yml
services:
  watchtower:
    image: containrrr/watchtower
    volumes:
      - /var/run/docker.sock:/var/run/docker.sock
    command: --interval 30
    labels:
      com.accessibility.service: "watchtower"
```

---

## 📚 Referencias

- [Docker Best Practices](https://docs.docker.com/develop/dev-best-practices/)
- [Docker Compose](https://docs.docker.com/compose/)
- [Docker Security](https://docs.docker.com/engine/security/)
- [Multi-stage Builds](https://docs.docker.com/build/building/multi-stage/)

---

[⬅️ Volver al README](../README.new.md)
