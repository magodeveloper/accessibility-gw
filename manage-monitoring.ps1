# ============================================
# Manage Monitoring Stack
# Prometheus + Alertmanager + Grafana + Redis
# ============================================

param(
    [Parameter(Mandatory = $false)]
    [ValidateSet('start', 'stop', 'restart', 'status', 'logs', 'test-alerts', 'validate', 'clean')]
    [string]$Action = 'status',
    
    [Parameter(Mandatory = $false)]
    [string]$Service = 'all'
)

$ErrorActionPreference = 'Stop'

# ============================================
# Configuración y Constantes
# ============================================

$ComposeFile = "docker-compose.monitoring.yml"

# URLs de servicios
$ServiceUrls = @{
    Prometheus   = "http://localhost:9090"
    Alertmanager = "http://localhost:9093"
    Grafana      = "http://localhost:3010"
}

# Health endpoints
$HealthEndpoints = @{
    Prometheus   = "$($ServiceUrls.Prometheus)/-/healthy"
    Alertmanager = "$($ServiceUrls.Alertmanager)/-/healthy"
    Grafana      = "$($ServiceUrls.Grafana)/api/health"
}

# Configuración Redis
$RedisConfig = @{
    ContainerName = "redis-test"
    Image         = "redis:latest"
    Port          = 6379
}

# Archivos de configuración requeridos
$RequiredConfigFiles = @(
    @{ Path = "prometheus.yml"; Name = "Prometheus" }
    @{ Path = "prometheus/alerts.yml"; Name = "Alertas" }
    @{ Path = "prometheus/alertmanager.yml"; Name = "Alertmanager" }
)

# ============================================
# Funciones de Utilidad
# ============================================

function Write-ColorOutput {
    param(
        [string]$Message,
        [string]$Color = 'White'
    )
    Write-Host $Message -ForegroundColor $Color
}

function Write-Section {
    param([string]$Title)
    Write-Host ""
    Write-ColorOutput "============================================" "Cyan"
    Write-ColorOutput $Title "Cyan"
    Write-ColorOutput "============================================" "Cyan"
}

function Test-DockerRunning {
    try {
        docker ps | Out-Null
        return $true
    }
    catch {
        Write-ColorOutput "❌ Docker no está corriendo. Por favor, inicia Docker Desktop" "Red"
        return $false
    }
}

function Test-ContainerExists {
    param([string]$ContainerName)
    
    $container = docker ps -a --filter "name=$ContainerName" --format "{{.Names}}" 2>$null
    return ($container -eq $ContainerName)
}

function Test-ContainerRunning {
    param([string]$ContainerName)
    
    $container = docker ps --filter "name=$ContainerName" --format "{{.Names}}" 2>$null
    return ($container -eq $ContainerName)
}

function Stop-DockerContainer {
    param(
        [string]$ContainerName,
        [bool]$Remove = $true
    )
    
    if (-not (Test-ContainerExists $ContainerName)) {
        return $false
    }
    
    Write-ColorOutput "🛑 Deteniendo contenedor $ContainerName..." "Yellow"
    docker stop $ContainerName 2>&1 | Out-Null
    
    if ($Remove) {
        Write-ColorOutput "🗑️  Eliminando contenedor $ContainerName..." "Yellow"
        docker rm $ContainerName 2>&1 | Out-Null
    }
    
    return $true
}

function Test-ConfigFiles {
    Write-ColorOutput "Validando configuraciones..." "Yellow"
    $allValid = $true
    
    foreach ($config in $RequiredConfigFiles) {
        if (-not (Test-Path $config.Path)) {
            Write-ColorOutput "❌ No se encuentra $($config.Path)" "Red"
            $allValid = $false
        }
    }
    
    if ($allValid) {
        Write-ColorOutput "✅ Configuraciones válidas" "Green"
    }
    
    return $allValid
}

function Test-ServiceHealth {
    param(
        [string]$ServiceName,
        [string]$Url,
        [int]$MaxRetries = 5,
        [bool]$Silent = $false
    )
    
    if (-not $Silent) {
        Write-ColorOutput "Verificando $ServiceName..." "Yellow"
    }
    
    for ($i = 1; $i -le $MaxRetries; $i++) {
        try {
            $response = Invoke-WebRequest -Uri $Url -Method Get -TimeoutSec 5 -ErrorAction Stop
            if ($response.StatusCode -eq 200) {
                if (-not $Silent) {
                    Write-ColorOutput "✅ $ServiceName está saludable" "Green"
                }
                return $true
            }
        }
        catch {
            if ($i -eq $MaxRetries) {
                if (-not $Silent) {
                    Write-ColorOutput "❌ $ServiceName no responde después de $MaxRetries intentos" "Red"
                }
                return $false
            }
            if (-not $Silent) {
                Write-ColorOutput "Intento $i/$MaxRetries fallido, reintentando..." "Yellow"
            }
            Start-Sleep -Seconds 3
        }
    }
    return $false
}

function Test-AllServicesHealth {
    param([int]$MaxRetries = 5)
    
    Write-Section "🏥 Health Checks"
    
    $results = @{}
    foreach ($service in $HealthEndpoints.Keys) {
        $results[$service] = Test-ServiceHealth $service $HealthEndpoints[$service] -MaxRetries $MaxRetries
    }
    
    return $results
}

function Show-ServicesSummary {
    param([hashtable]$HealthResults)
    
    Write-Section "📊 Resumen de Servicios"
    
    Write-Host "Prometheus:    $($ServiceUrls.Prometheus)  " -NoNewline
    if ($HealthResults.Prometheus) { Write-ColorOutput "✅" "Green" } else { Write-ColorOutput "❌" "Red" }
    
    Write-Host "Alertmanager:  $($ServiceUrls.Alertmanager)  " -NoNewline
    if ($HealthResults.Alertmanager) { Write-ColorOutput "✅" "Green" } else { Write-ColorOutput "❌" "Red" }
    
    Write-Host "Grafana:       $($ServiceUrls.Grafana)  " -NoNewline
    if ($HealthResults.Grafana) { Write-ColorOutput "✅" "Green" } else { Write-ColorOutput "❌" "Red" }
    
    Write-Host "`nCredenciales de Grafana:"
    Write-ColorOutput "  Usuario: admin" "Cyan"
    Write-ColorOutput "  Password: admin" "Cyan"
}

# ============================================
# Acciones
# ============================================

switch ($Action) {
    'start' {
        Write-Section "🚀 Iniciando Monitoring Stack"
        
        # Validar configuraciones
        if (-not (Test-ConfigFiles)) {
            exit 1
        }
        
        # Iniciar servicios
        Write-ColorOutput "`nIniciando servicios..." "Yellow"
        docker compose -f $ComposeFile up -d
        
        if ($LASTEXITCODE -eq 0) {
            Write-ColorOutput "✅ Servicios iniciados correctamente" "Green"
            
            # Health checks
            Start-Sleep -Seconds 5
            $healthResults = Test-AllServicesHealth
            
            # Resumen
            Show-ServicesSummary $healthResults
            
            # Nota: Redis ya está corriendo en el stack de desarrollo (accessibility-redis-dev)
            # No es necesario iniciar otro contenedor Redis
        }
        else {
            Write-ColorOutput "❌ Error al iniciar servicios" "Red"
            exit 1
        }
    }
    
    'stop' {
        Write-Section "🛑 Deteniendo Monitoring Stack"
        docker compose -f $ComposeFile down
        
        if ($LASTEXITCODE -eq 0) {
            Write-ColorOutput "✅ Servicios detenidos correctamente" "Green"
        }
        
        # Nota: No detener Redis porque es parte del stack de desarrollo
        # Redis (accessibility-redis-dev) debe permanecer corriendo
    }
    
    'restart' {
        Write-Section "🔄 Reiniciando Monitoring Stack"
        
        if ($Service -eq 'all') {
            # Reiniciar servicios de monitoreo
            docker compose -f $ComposeFile restart
            Write-ColorOutput "✅ Todos los servicios de monitoreo reiniciados" "Green"
            Write-ColorOutput "⏳ Esperando a que los servicios estén listos..." "Yellow"
            Start-Sleep -Seconds 15
            
            # Nota: Redis ya está corriendo en el stack de desarrollo
            # No es necesario reiniciarlo aquí
            
            # Health check de todos los servicios
            Test-AllServicesHealth -MaxRetries 10 | Out-Null
        }
        else {
            docker compose -f $ComposeFile restart $Service
            Write-ColorOutput "✅ $Service reiniciado" "Green"
            Write-ColorOutput "⏳ Esperando a que el servicio esté listo..." "Yellow"
            Start-Sleep -Seconds 10
            
            # Health check del servicio específico
            if ($HealthEndpoints.ContainsKey($Service)) {
                Test-ServiceHealth $Service $HealthEndpoints[$Service] -MaxRetries 10 | Out-Null
            }
        }
    }
    
    'status' {
        Write-Section "📊 Estado de Monitoring Stack"
        docker compose -f $ComposeFile ps
        
        Write-Host ""
        Write-ColorOutput "Health Checks:" "Cyan"
        Test-AllServicesHealth | Out-Null
        
        # Alertas activas
        Write-Host ""
        Write-ColorOutput "Alertas Activas:" "Cyan"
        try {
            $alerts = Invoke-RestMethod -Uri "$($ServiceUrls.Prometheus)/api/v1/alerts" -Method Get
            $firingAlerts = $alerts.data.alerts | Where-Object { $_.state -eq 'firing' }
            
            if ($firingAlerts) {
                Write-ColorOutput "🔴 $($firingAlerts.Count) alerta(s) activa(s):" "Red"
                foreach ($alert in $firingAlerts) {
                    Write-Host "  - $($alert.labels.alertname) " -NoNewline
                    Write-ColorOutput "[$($alert.labels.severity)]" "Yellow"
                }
            }
            else {
                Write-ColorOutput "✅ No hay alertas activas" "Green"
            }
        }
        catch {
            Write-ColorOutput "⚠️ No se pueden obtener alertas (Prometheus no está corriendo?)" "Yellow"
        }
        
        # Nota: Redis es parte del stack de desarrollo y se muestra en el docker compose ps arriba
    }
    
    'logs' {
        Write-Section "📜 Logs de Servicios"
        
        if ($Service -eq 'redis') {
            # Redis es parte del stack de desarrollo
            Write-ColorOutput "⚠️ Redis es parte del stack de desarrollo" "Yellow"
            Write-ColorOutput "Para ver logs de Redis:" "Cyan"
            Write-ColorOutput "  docker logs accessibility-redis" "White"
            Write-ColorOutput "  docker logs -f accessibility-redis  # Modo follow" "White"
        }
        elseif ($Service -eq 'all') {
            Write-ColorOutput "Mostrando logs de todos los servicios de monitoreo (últimas 50 líneas)..." "Yellow"
            docker compose -f $ComposeFile logs --tail=50 -f
        }
        else {
            Write-ColorOutput "Mostrando logs de $Service (últimas 50 líneas)..." "Yellow"
            docker compose -f $ComposeFile logs --tail=50 -f $Service
        }
    }
    
    'test-alerts' {
        Write-Section "🧪 Probando Alertas"
        
        Write-ColorOutput "1. Verificando reglas de alertas..." "Yellow"
        try {
            $rules = Invoke-RestMethod -Uri "$($ServiceUrls.Prometheus)/api/v1/rules" -Method Get
            $alertGroups = $rules.data.groups | Where-Object { $_.name -like "*alert*" }
            
            Write-ColorOutput "✅ Se encontraron $($alertGroups.Count) grupos de alertas" "Green"
            
            foreach ($group in $alertGroups) {
                Write-Host "`n  📁 $($group.name):"
                $alerts = $group.rules | Where-Object { $_.type -eq 'alerting' }
                Write-ColorOutput "     $($alerts.Count) alertas definidas" "Cyan"
            }
        }
        catch {
            Write-ColorOutput "❌ Error al obtener reglas: $($_.Exception.Message)" "Red"
        }
        
        Write-Host ""
        Write-ColorOutput "2. Verificando conexión Prometheus -> Alertmanager..." "Yellow"
        try {
            $config = Invoke-RestMethod -Uri "$($ServiceUrls.Prometheus)/api/v1/status/config" -Method Get
            if ($config.data.yaml -match 'alertmanager') {
                Write-ColorOutput "✅ Alertmanager configurado en Prometheus" "Green"
            }
            else {
                Write-ColorOutput "⚠️ No se encuentra configuración de Alertmanager" "Yellow"
            }
        }
        catch {
            Write-ColorOutput "❌ Error al verificar configuración" "Red"
        }
        
        Write-Host ""
        Write-ColorOutput "3. Estado de Alertmanager..." "Yellow"
        try {
            $amStatus = Invoke-RestMethod -Uri "$($ServiceUrls.Alertmanager)/api/v2/status" -Method Get
            Write-ColorOutput "✅ Alertmanager operativo" "Green"
            Write-Host "   Versión: $($amStatus.versionInfo.version)"
            Write-Host "   Cluster: $($amStatus.cluster.status)"
        }
        catch {
            Write-ColorOutput "❌ Alertmanager no responde" "Red"
        }
        
        Write-Host ""
        Write-ColorOutput "4. Silences activos..." "Yellow"
        try {
            $silences = Invoke-RestMethod -Uri "$($ServiceUrls.Alertmanager)/api/v2/silences" -Method Get
            $activeSilences = $silences | Where-Object { $_.status.state -eq 'active' }
            
            if ($activeSilences) {
                Write-ColorOutput "⚠️ $($activeSilences.Count) silence(s) activo(s)" "Yellow"
            }
            else {
                Write-ColorOutput "✅ No hay silences activos" "Green"
            }
        }
        catch {
            Write-ColorOutput "⚠️ No se pueden obtener silences" "Yellow"
        }
        
        Write-Host ""
        Write-ColorOutput "Para probar una alerta manualmente:" "Cyan"
        Write-ColorOutput "  docker stop accessibility-mw-prod" "White"
        Write-ColorOutput "  # Esperar 1-2 minutos" "Gray"
        Write-ColorOutput "  # Ver en: $($ServiceUrls.Alertmanager)/#/alerts" "Gray"
    }
    
    'validate' {
        Write-Section "✅ Validando Configuraciones"
        
        # Validar prometheus.yml
        Write-ColorOutput "1. Validando prometheus.yml..." "Yellow"
        if (Test-Path "prometheus.yml") {
            try {
                $promContent = Get-Content "prometheus.yml" -Raw
                if ($promContent -match "global:" -and $promContent -match "scrape_configs:") {
                    Write-ColorOutput "   ✅ prometheus.yml tiene sintaxis válida" "Green"
                    
                    # Contar jobs
                    $jobCount = ([regex]::Matches($promContent, "- job_name:")).Count
                    Write-ColorOutput "   📊 $jobCount jobs de scraping configurados" "Cyan"
                }
                else {
                    Write-ColorOutput "   ⚠️ prometheus.yml podría tener problemas de estructura" "Yellow"
                }
            }
            catch {
                Write-ColorOutput "   ❌ Error leyendo prometheus.yml" "Red"
            }
        }
        else {
            Write-ColorOutput "   ❌ prometheus.yml no encontrado" "Red"
        }
        
        # Validar alerts.yml
        Write-ColorOutput "`n2. Validando alerts.yml..." "Yellow"
        if (Test-Path "prometheus/alerts.yml") {
            # Verificar sintaxis básica
            try {
                $alertContent = Get-Content "prometheus/alerts.yml" -Raw
                if ($alertContent -match "groups:" -and $alertContent -match "- alert:") {
                    $alertCount = ([regex]::Matches($alertContent, "- alert:")).Count
                    Write-ColorOutput "   ✅ alerts.yml tiene sintaxis válida" "Green"
                    Write-ColorOutput "   📊 $alertCount alertas definidas" "Cyan"
                }
                else {
                    Write-ColorOutput "   ⚠️ alerts.yml podría tener problemas de estructura" "Yellow"
                }
            }
            catch {
                Write-ColorOutput "   ❌ Error leyendo alerts.yml" "Red"
            }
        }
        else {
            Write-ColorOutput "   ❌ alerts.yml no encontrado" "Red"
        }
        
        # Validar alertmanager.yml
        Write-ColorOutput "`n3. Validando alertmanager.yml..." "Yellow"
        if (Test-Path "prometheus/alertmanager.yml") {
            try {
                $amContent = Get-Content "prometheus/alertmanager.yml" -Raw
                if ($amContent -match "route:" -and $amContent -match "receivers:") {
                    Write-ColorOutput "   ✅ alertmanager.yml tiene sintaxis válida" "Green"
                }
                else {
                    Write-ColorOutput "   ⚠️ alertmanager.yml podría tener problemas de estructura" "Yellow"
                }
            }
            catch {
                Write-ColorOutput "   ❌ Error leyendo alertmanager.yml" "Red"
            }
        }
        else {
            Write-ColorOutput "   ❌ alertmanager.yml no encontrado" "Red"
        }
        
        # Verificar templates
        Write-ColorOutput "`n4. Verificando templates..." "Yellow"
        if (Test-Path "prometheus/templates/email.tmpl") {
            Write-ColorOutput "   ✅ email.tmpl encontrado" "Green"
        }
        else {
            Write-ColorOutput "   ⚠️ email.tmpl no encontrado" "Yellow"
        }
    }
    
    'clean' {
        Write-Section "🧹 Limpiando Monitoring Stack"
        
        Write-ColorOutput "⚠️ ADVERTENCIA: Esto eliminará todos los volúmenes de datos!" "Yellow"
        Write-Host "Presiona Enter para continuar o Ctrl+C para cancelar..."
        Read-Host
        
        docker compose -f $ComposeFile down -v
        
        Write-ColorOutput "✅ Stack detenido y volúmenes eliminados" "Green"
        Write-ColorOutput "Datos eliminados:" "Yellow"
        Write-ColorOutput "  - Métricas históricas de Prometheus" "Gray"
        Write-ColorOutput "  - Alertas y silences de Alertmanager" "Gray"
        Write-ColorOutput "  - Dashboards y configuración de Grafana" "Gray"
    }
    
    default {
        Write-ColorOutput "Acción no reconocida: $Action" "Red"
        Write-Host "`nUso: .\manage-monitoring.ps1 -Action <accion> [-Service <servicio>]"
        Write-Host "`nAcciones disponibles:"
        Write-Host "  start         - Iniciar el stack de monitoreo"
        Write-Host "  stop          - Detener el stack"
        Write-Host "  restart       - Reiniciar servicios (especificar -Service o 'all')"
        Write-Host "  status        - Ver estado y alertas activas"
        Write-Host "  logs          - Ver logs (especificar -Service: all, prometheus, alertmanager, grafana)"
        Write-Host "  test-alerts   - Probar configuración de alertas"
        Write-Host "  validate      - Validar archivos de configuración"
        Write-Host "  clean         - Detener y eliminar todos los volúmenes"
        Write-Host "`nServicios disponibles para logs/restart: prometheus, alertmanager, grafana, all"
        Write-Host "`nEjemplos:"
        Write-Host "  .\manage-monitoring.ps1 -Action start"
        Write-Host "  .\manage-monitoring.ps1 -Action restart -Service alertmanager"
        Write-Host "  .\manage-monitoring.ps1 -Action logs -Service prometheus"
        Write-Host ""
        Write-Host "📝 NOTA: Redis es parte del stack de desarrollo (docker-compose.dev.yml)"
        Write-Host "   Para gestionar Redis: docker-compose -f docker-compose.dev.yml up -d redis"
        exit 1
    }
}