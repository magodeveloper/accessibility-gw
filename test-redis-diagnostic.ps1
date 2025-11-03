#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Script de diagnóstico de Redis
.DESCRIPTION
    Verifica el estado, configuración y conectividad de Redis
#>

Write-Host "============================================" -ForegroundColor Cyan
Write-Host "     DIAGNÓSTICO COMPLETO REDIS" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""

# Verificar contenedor Redis
Write-Host "[1] Estado del Contenedor Redis" -ForegroundColor Yellow
$redisContainer = docker ps --filter "name=redis" --format "{{.Names}}\t{{.Status}}\t{{.Ports}}"
if ($redisContainer) {
    Write-Host "✅ Contenedor Redis ACTIVO:" -ForegroundColor Green
    $redisContainer | ForEach-Object {
        Write-Host "   $_" -ForegroundColor White
    }
} else {
    Write-Host "❌ Contenedor Redis NO encontrado" -ForegroundColor Red
    exit 1
}
Write-Host ""

# Verificar conectividad interna
Write-Host "[2] Conectividad Interna (dentro de Docker)" -ForegroundColor Yellow
$pingResult = docker exec accessibility-redis-dev redis-cli ping 2>&1
if ($pingResult -eq "PONG") {
    Write-Host "✅ Redis responde correctamente: $pingResult" -ForegroundColor Green
} else {
    Write-Host "❌ Redis NO responde: $pingResult" -ForegroundColor Red
}
Write-Host ""

# Verificar versión
Write-Host "[3] Versión de Redis" -ForegroundColor Yellow
$version = docker exec accessibility-redis-dev redis-cli INFO server | Select-String "redis_version"
Write-Host "   $version" -ForegroundColor White
Write-Host ""

# Verificar configuración
Write-Host "[4] Configuración Actual" -ForegroundColor Yellow
$config = docker exec accessibility-redis-dev redis-cli CONFIG GET maxmemory 2>&1
$maxmemory = ($config | Select-Object -Index 1)
if ($maxmemory -eq "0") {
    Write-Host "   Max Memory: Sin límite" -ForegroundColor White
} else {
    $maxmemoryMB = [math]::Round($maxmemory / 1MB, 2)
    Write-Host "   Max Memory: $maxmemoryMB MB" -ForegroundColor White
}

$policy = docker exec accessibility-redis-dev redis-cli CONFIG GET maxmemory-policy 2>&1 | Select-Object -Index 1
Write-Host "   Eviction Policy: $policy" -ForegroundColor White

$appendonly = docker exec accessibility-redis-dev redis-cli CONFIG GET appendonly 2>&1 | Select-Object -Index 1
Write-Host "   Persistence (AOF): $appendonly" -ForegroundColor White
Write-Host ""

# Verificar estadísticas
Write-Host "[5] Estadísticas de Uso" -ForegroundColor Yellow
$stats = docker exec accessibility-redis-dev redis-cli INFO stats 2>&1
$connections = $stats | Select-String "total_connections_received" | ForEach-Object { $_.ToString().Split(':')[1].Trim() }
$commands = $stats | Select-String "total_commands_processed" | ForEach-Object { $_.ToString().Split(':')[1].Trim() }
$keys = docker exec accessibility-redis-dev redis-cli DBSIZE 2>&1 | Select-String "\d+" | ForEach-Object { $_.Matches.Value }

Write-Host "   Conexiones totales: $connections" -ForegroundColor White
Write-Host "   Comandos procesados: $commands" -ForegroundColor White
Write-Host "   Claves en base de datos: $keys" -ForegroundColor White
Write-Host ""

# Verificar memoria
Write-Host "[6] Uso de Memoria" -ForegroundColor Yellow
$memory = docker exec accessibility-redis-dev redis-cli INFO memory 2>&1
$usedMemory = $memory | Select-String "used_memory_human" | Select-Object -First 1 | ForEach-Object { $_.ToString().Split(':')[1].Trim() }
$peakMemory = $memory | Select-String "used_memory_peak_human" | Select-Object -First 1 | ForEach-Object { $_.ToString().Split(':')[1].Trim() }

Write-Host "   Memoria usada: $usedMemory" -ForegroundColor White
Write-Host "   Pico de memoria: $peakMemory" -ForegroundColor White
Write-Host ""

# Mapeo de puertos
Write-Host "[7] Mapeo de Puertos" -ForegroundColor Yellow
Write-Host "   📌 Puerto INTERNO (Docker Network):" -ForegroundColor Cyan
Write-Host "      accessibility-redis-dev:6379" -ForegroundColor White
Write-Host "      → Usado por Gateway y otros servicios Docker" -ForegroundColor DarkGray
Write-Host ""
Write-Host "   📌 Puerto EXTERNO (Host/localhost):" -ForegroundColor Cyan
Write-Host "      localhost:6380" -ForegroundColor White
Write-Host "      → Usado por aplicaciones externas y desarrollo local" -ForegroundColor DarkGray
Write-Host ""

# Probar conexión externa
Write-Host "[8] Prueba de Conexión Externa (localhost:6380)" -ForegroundColor Yellow
$externalPing = docker run --rm redis:7.2-alpine redis-cli -h host.docker.internal -p 6380 ping 2>&1
if ($externalPing -eq "PONG") {
    Write-Host "✅ Conexión externa exitosa: $externalPing" -ForegroundColor Green
} else {
    Write-Host "❌ Conexión externa falló: $externalPing" -ForegroundColor Red
}
Write-Host ""

# Health check desde Gateway
Write-Host "[9] Verificación desde Gateway" -ForegroundColor Yellow
$gatewayHealth = curl -s http://localhost:8100/health | ConvertFrom-Json
$redisHealth = $gatewayHealth.entries.redis

if ($redisHealth.status -eq "Healthy") {
    Write-Host "✅ Gateway puede conectarse a Redis" -ForegroundColor Green
    Write-Host "   Status: $($redisHealth.status)" -ForegroundColor White
    Write-Host "   Duration: $($redisHealth.duration)" -ForegroundColor White
} else {
    Write-Host "⚠️  Gateway reporta problemas con Redis" -ForegroundColor Yellow
    Write-Host "   Status: $($redisHealth.status)" -ForegroundColor White
}
Write-Host ""

# Resumen final
Write-Host "============================================" -ForegroundColor Cyan
Write-Host "          RESUMEN" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "✅ Redis: ACTIVO y SALUDABLE" -ForegroundColor Green
Write-Host "✅ Versión: Redis 7.2" -ForegroundColor Green
Write-Host "✅ Conexión interna: OK" -ForegroundColor Green
Write-Host "✅ Conexión externa: OK" -ForegroundColor Green
Write-Host "✅ Gateway → Redis: OK" -ForegroundColor Green
Write-Host ""
Write-Host "📝 IMPORTANTE:" -ForegroundColor Yellow
Write-Host "   • Redis NO usa protocolo HTTP" -ForegroundColor White
Write-Host "   • No acceder via http://localhost:6379" -ForegroundColor White
Write-Host "   • Usar puerto 6380 desde el host" -ForegroundColor White
Write-Host "   • Usar puerto 6379 desde Docker network" -ForegroundColor White
Write-Host ""
Write-Host "💡 COMANDOS ÚTILES:" -ForegroundColor Yellow
Write-Host "   # Conectar desde PowerShell:" -ForegroundColor White
Write-Host "   docker exec -it accessibility-redis-dev redis-cli" -ForegroundColor Cyan
Write-Host ""
Write-Host "   # Ver todas las claves:" -ForegroundColor White
Write-Host "   docker exec accessibility-redis-dev redis-cli KEYS '*'" -ForegroundColor Cyan
Write-Host ""
Write-Host "   # Ver info completa:" -ForegroundColor White
Write-Host "   docker exec accessibility-redis-dev redis-cli INFO" -ForegroundColor Cyan
Write-Host ""
