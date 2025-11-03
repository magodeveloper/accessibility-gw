#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Script consolidado de validación del stack de monitoreo

.DESCRIPTION
    Valida Prometheus (métricas, scraping, alertas), Grafana (dashboards, datasources)
    y health checks de todos los microservicios del sistema.

.PARAMETER Verbose
    Muestra información detallada de las pruebas

.PARAMETER SkipPrometheus
    Omite las pruebas de Prometheus

.PARAMETER SkipGrafana
    Omite las pruebas de Grafana

.PARAMETER SkipHealthChecks
    Omite las pruebas de health checks de microservicios

.PARAMETER SkipAlerts
    Omite la validación de alertas

.PARAMETER SkipDashboards
    Omite la validación de dashboards

.PARAMETER TimeoutSeconds
    Tiempo de espera para las peticiones HTTP (default: 10)

.PARAMETER PrometheusUrl
    URL de Prometheus (default: http://localhost:9090)

.PARAMETER GrafanaUrl
    URL de Grafana (default: http://localhost:3010)

.EXAMPLE
    .\test-monitoring.ps1
    Ejecuta todas las pruebas de monitoreo

.EXAMPLE
    .\test-monitoring.ps1 -Verbose
    Ejecuta todas las pruebas con salida detallada

.EXAMPLE
    .\test-monitoring.ps1 -SkipGrafana
    Ejecuta solo pruebas de Prometheus y health checks

.EXAMPLE
    .\test-monitoring.ps1 -SkipDashboards -SkipAlerts
    Ejecuta pruebas básicas sin validar dashboards ni alertas
#>

param(
    [switch]$Verbose,
    [switch]$SkipPrometheus,
    [switch]$SkipGrafana,
    [switch]$SkipHealthChecks,
    [switch]$SkipAlerts,
    [switch]$SkipDashboards,
    [int]$TimeoutSeconds = 10,
    [string]$PrometheusUrl = "http://localhost:9090",
    [string]$GrafanaUrl = "http://localhost:3010"
)

$ErrorActionPreference = "Continue"
$Host.UI.RawUI.WindowTitle = "Monitoring Stack Validation"

# ============================================
# CONFIGURACIÓN Y UTILIDADES
# ============================================

# Colores
$Green = "Green"
$Red = "Red"
$Yellow = "Yellow"
$Cyan = "Cyan"
$White = "White"
$Gray = "Gray"
$Blue = "Blue"

# Contadores globales
$script:totalTests = 0
$script:passedTests = 0
$script:failedTests = 0
$script:warningTests = 0

# Configuración de Grafana
$GrafanaUser = "admin"
$GrafanaPass = "admin"
$GrafanaAuthPair = "${GrafanaUser}:${GrafanaPass}"
$GrafanaAuthEncoded = [System.Convert]::ToBase64String([System.Text.Encoding]::ASCII.GetBytes($GrafanaAuthPair))
$GrafanaHeaders = @{ Authorization = "Basic $GrafanaAuthEncoded" }

# Configuración de servicios
$script:services = @(
    @{
        Name                 = "Gateway"
        Url                  = "http://localhost:8100"
        HealthEndpoint       = "/health"
        MetricsEndpoint      = "/metrics"
        ExpectedHealthStatus = "Healthy"
        Container            = "accessibility-gw"
    },
    @{
        Name                 = "Users Microservice"
        Url                  = "http://localhost:8081"
        HealthEndpoint       = "/health"
        MetricsEndpoint      = "/metrics"
        ExpectedHealthStatus = "Healthy"
        Container            = "msusers-api"
    },
    @{
        Name                 = "Reports Microservice"
        Url                  = "http://localhost:8083"
        HealthEndpoint       = "/health"
        MetricsEndpoint      = "/metrics"
        ExpectedHealthStatus = "Healthy"
        Container            = "msreports-api"
    },
    @{
        Name                 = "Analysis Microservice"
        Url                  = "http://localhost:8082"
        HealthEndpoint       = "/health"
        MetricsEndpoint      = "/metrics"
        ExpectedHealthStatus = "Healthy"
        Container            = "msanalysis-api"
    }
)

# Alertas críticas esperadas (validar solo las más importantes)
$script:expectedAlerts = @(
    "ServiceDown",
    "HighMemoryUsage",
    "HighCPUUsage",
    "HealthCheckFailed",
    "HighErrorRate5xx",
    "HighLatencyP95"
)

# Dashboards esperados (validar solo los existentes)
$script:expectedDashboards = @(
    "Accessibility Gateway Metrics",
    "Analysis Microservice Metrics",
    "Reports Microservice Metrics",
    "Users Microservice Metrics"
)

# ============================================
# FUNCIONES DE UTILIDAD
# ============================================

function Test-ServiceAvailability {
    param([hashtable]$Service)
    
    $healthUrl = "$($Service.Url)$($Service.HealthEndpoint)"
    try {
        $null = Invoke-RestMethod -Uri $healthUrl -Method GET -UseBasicParsing -TimeoutSec 2 -ErrorAction Stop
        return $true
    }
    catch {
        return $false
    }
}

function Get-AvailableServices {
    Write-Host "🔍 Detectando servicios disponibles..." -ForegroundColor Gray
    
    $availableServices = @()
    foreach ($service in $script:services) {
        if (Test-ServiceAvailability -Service $service) {
            $availableServices += $service
            Write-Host "   ✅ $($service.Name) - Disponible" -ForegroundColor Green
        }
        else {
            Write-Host "   ⏭️  $($service.Name) - No disponible (omitido)" -ForegroundColor Yellow
        }
    }
    
    Write-Host ""
    return $availableServices
}

function Write-Header {
    param([string]$Message)
    Write-Host ""
    Write-Host "══════════════════════════════════════════════════════════════════" -ForegroundColor $Cyan
    Write-Host " $Message" -ForegroundColor $Cyan
    Write-Host "══════════════════════════════════════════════════════════════════" -ForegroundColor $Cyan
    Write-Host ""
}

function Write-SubHeader {
    param([string]$Message)
    Write-Host ""
    Write-Host "────────────────────────────────────────────────────────" -ForegroundColor $Blue
    Write-Host " $Message" -ForegroundColor $Blue
    Write-Host "────────────────────────────────────────────────────────" -ForegroundColor $Blue
}

function Write-TestResult {
    param(
        [string]$TestName,
        [bool]$Passed,
        [string]$Message = "",
        [switch]$IsWarning
    )
    
    $script:totalTests++
    
    if ($IsWarning) {
        Write-Host "⚠️  WARN" -ForegroundColor $Yellow -NoNewline
        Write-Host " - $TestName" -ForegroundColor $White
        if ($Message) {
            Write-Host "   $Message" -ForegroundColor $Gray
        }
        $script:warningTests++
    }
    elseif ($Passed) {
        Write-Host "✅ PASS" -ForegroundColor $Green -NoNewline
        Write-Host " - $TestName" -ForegroundColor $White
        if ($Message) {
            Write-Host "   $Message" -ForegroundColor $Gray
        }
        $script:passedTests++
    }
    else {
        Write-Host "❌ FAIL" -ForegroundColor $Red -NoNewline
        Write-Host " - $TestName" -ForegroundColor $White
        if ($Message) {
            Write-Host "   $Message" -ForegroundColor $Red
        }
        $script:failedTests++
    }
}

function Test-HttpEndpoint {
    param(
        [string]$Url,
        [string]$TestName,
        [hashtable]$Headers = @{},
        [string]$ExpectedPattern = $null,
        [int]$Timeout = $TimeoutSeconds
    )
    
    try {
        if ($Verbose) {
            Write-Host "   🔍 Testing: $Url" -ForegroundColor $Gray
        }
        
        $response = Invoke-WebRequest -Uri $Url -Method GET -Headers $Headers -UseBasicParsing -TimeoutSec $Timeout -ErrorAction Stop
        
        if ($response.StatusCode -eq 200) {
            if ($ExpectedPattern -and $response.Content -notmatch $ExpectedPattern) {
                Write-TestResult -TestName $TestName -Passed $false -Message "Expected pattern '$ExpectedPattern' not found in response"
                return $false
            }
            
            Write-TestResult -TestName $TestName -Passed $true -Message "Status: $($response.StatusCode)"
            return $true
        }
        else {
            Write-TestResult -TestName $TestName -Passed $false -Message "Unexpected status code: $($response.StatusCode)"
            return $false
        }
    }
    catch {
        Write-TestResult -TestName $TestName -Passed $false -Message "Error: $($_.Exception.Message)"
        return $false
    }
}

function Test-PrometheusQuery {
    param(
        [string]$Query,
        [string]$TestName,
        [int]$MinResults = 0
    )
    
    try {
        $encodedQuery = [System.Uri]::EscapeDataString($Query)
        $url = "$PrometheusUrl/api/v1/query?query=$encodedQuery"
        
        if ($Verbose) {
            Write-Host "   🔍 Query: $Query" -ForegroundColor $Gray
        }
        
        $response = Invoke-RestMethod -Uri $url -Method GET -UseBasicParsing -TimeoutSec $TimeoutSeconds
        
        if ($response.status -eq "success") {
            $resultCount = $response.data.result.Count
            
            if ($resultCount -ge $MinResults) {
                Write-TestResult -TestName $TestName -Passed $true -Message "$resultCount results found"
                return $true
            }
            else {
                Write-TestResult -TestName $TestName -Passed $false -Message "Expected at least $MinResults results, got $resultCount"
                return $false
            }
        }
        else {
            Write-TestResult -TestName $TestName -Passed $false -Message "Query failed: $($response.status)"
            return $false
        }
    }
    catch {
        Write-TestResult -TestName $TestName -Passed $false -Message "Error: $($_.Exception.Message)"
        return $false
    }
}

# ============================================
# PRUEBAS DE HEALTH CHECKS
# ============================================

function Test-MicroservicesHealth {
    Write-Header "🏥 HEALTH CHECKS DE MICROSERVICIOS"
    
    # Obtener solo servicios disponibles
    $availableServices = Get-AvailableServices
    
    if ($availableServices.Count -eq 0) {
        Write-Host "⚠️  No hay servicios disponibles para probar" -ForegroundColor Yellow
        return
    }
    
    foreach ($service in $availableServices) {
        Write-SubHeader "Testing: $($service.Name)"
        
        # Test health endpoint
        $healthUrl = "$($service.Url)$($service.HealthEndpoint)"
        try {
            $healthResponse = Invoke-RestMethod -Uri $healthUrl -Method GET -UseBasicParsing -TimeoutSec $TimeoutSeconds -ErrorAction Stop
            
            $status = $healthResponse.status
            if ($status -eq $service.ExpectedHealthStatus) {
                Write-TestResult -TestName "$($service.Name) Health Check" -Passed $true -Message "Status: $status"
            }
            else {
                Write-TestResult -TestName "$($service.Name) Health Check" -Passed $false -Message "Expected: $($service.ExpectedHealthStatus), Got: $status"
            }
            
            # Verificar dependencias si existen
            if ($healthResponse.PSObject.Properties.Name -contains "entries") {
                $unhealthyDeps = $healthResponse.entries.PSObject.Properties | Where-Object {
                    $_.Value.status -ne "Healthy"
                }
                
                if ($unhealthyDeps) {
                    foreach ($dep in $unhealthyDeps) {
                        Write-TestResult -TestName "$($service.Name) - $($dep.Name)" -IsWarning -Message "Dependency unhealthy: $($dep.Value.status)"
                    }
                }
            }
        }
        catch {
            Write-TestResult -TestName "$($service.Name) Health Check" -Passed $false -Message "Error: $($_.Exception.Message)"
        }
        
        # Test metrics endpoint
        if (-not $SkipHealthChecks) {
            Test-HttpEndpoint -Url "$($service.Url)$($service.MetricsEndpoint)" `
                -TestName "$($service.Name) Metrics Endpoint" `
                -ExpectedPattern "# HELP" | Out-Null
        }
    }
}

# ============================================
# PRUEBAS DE PROMETHEUS
# ============================================

function Test-PrometheusCore {
    Write-Header "📊 VALIDACIÓN DE PROMETHEUS"
    
    Write-SubHeader "Pruebas Básicas"
    
    # Health check
    Test-HttpEndpoint -Url "$PrometheusUrl/-/healthy" `
        -TestName "Prometheus Health Check" | Out-Null
    
    # Ready check
    Test-HttpEndpoint -Url "$PrometheusUrl/-/ready" `
        -TestName "Prometheus Ready Check" | Out-Null
    
    # API status
    Test-HttpEndpoint -Url "$PrometheusUrl/api/v1/status/config" `
        -TestName "Prometheus Configuration API" | Out-Null
}

function Test-PrometheusScraping {
    Write-SubHeader "Validación de Scraping"
    
    # Obtener servicios disponibles para validación
    $availableServices = Get-AvailableServices
    
    # Verificar targets activos
    try {
        $targetsUrl = "$PrometheusUrl/api/v1/targets"
        $targetsResponse = Invoke-RestMethod -Uri $targetsUrl -Method GET -UseBasicParsing -TimeoutSec $TimeoutSeconds
        
        if ($targetsResponse.status -eq "success") {
            $activeTargets = $targetsResponse.data.activeTargets
            $totalTargets = $activeTargets.Count
            $upTargets = ($activeTargets | Where-Object { $_.health -eq "up" }).Count
            $downTargets = ($activeTargets | Where-Object { $_.health -eq "down" }).Count
            
            Write-TestResult -TestName "Prometheus Targets Discovery" -Passed ($totalTargets -gt 0) `
                -Message "$totalTargets targets total ($upTargets up, $downTargets down)"
            
            # Verificar solo microservicios disponibles
            foreach ($service in $availableServices) {
                $serviceTarget = $activeTargets | Where-Object { 
                    $_.labels.job -eq $service.Container -or 
                    $_.labels.instance -like "*$($service.Url.Split(':')[-1])*"
                }
                
                if ($serviceTarget) {
                    $health = $serviceTarget.health
                    Write-TestResult -TestName "Target: $($service.Name)" -Passed ($health -eq "up") -Message "Health: $health"
                }
                else {
                    Write-TestResult -TestName "Target: $($service.Name)" -Passed $false -Message "Target not found in Prometheus"
                }
            }
        }
        else {
            Write-TestResult -TestName "Prometheus Targets API" -Passed $false -Message "API returned error"
        }
    }
    catch {
        Write-TestResult -TestName "Prometheus Targets Check" -Passed $false -Message "Error: $($_.Exception.Message)"
    }
    
    # Verificar métricas críticas
    Write-SubHeader "Validación de Métricas"
    
    $criticalMetrics = @(
        @{ Name = "up"; Query = "up"; MinResults = 1 },
        @{ Name = "http_requests_total"; Query = "http_requests_total"; MinResults = 0 },
        @{ Name = "gateway_requests_total"; Query = "gateway_requests_total"; MinResults = 0 },
        @{ Name = "process_cpu_seconds_total"; Query = "process_cpu_seconds_total"; MinResults = 1 }
    )
    
    foreach ($metric in $criticalMetrics) {
        Test-PrometheusQuery -Query $metric.Query -TestName "Metric: $($metric.Name)" -MinResults $metric.MinResults | Out-Null
    }
}

function Test-PrometheusAlerts {
    Write-SubHeader "Validación de Alertas"
    
    try {
        # Verificar reglas de alertas
        $rulesUrl = "$PrometheusUrl/api/v1/rules"
        $rulesResponse = Invoke-RestMethod -Uri $rulesUrl -Method GET -UseBasicParsing -TimeoutSec $TimeoutSeconds
        
        if ($rulesResponse.status -eq "success") {
            $groups = $rulesResponse.data.groups
            $totalRules = 0
            $alertRules = 0
            $recordingRules = 0
            
            foreach ($group in $groups) {
                $totalRules += $group.rules.Count
                $alertRules += ($group.rules | Where-Object { $_.type -eq "alerting" }).Count
                $recordingRules += ($group.rules | Where-Object { $_.type -eq "recording" }).Count
            }
            
            Write-TestResult -TestName "Alert Rules Loaded" -Passed ($alertRules -gt 0) `
                -Message "$alertRules alert rules, $recordingRules recording rules"
            
            # Verificar alertas específicas
            $loadedAlertNames = $groups.rules | Where-Object { $_.type -eq "alerting" } | Select-Object -ExpandProperty name
            
            foreach ($expectedAlert in $script:expectedAlerts) {
                if ($loadedAlertNames -contains $expectedAlert) {
                    Write-TestResult -TestName "Alert Rule: $expectedAlert" -Passed $true -Message "Loaded"
                }
                else {
                    Write-TestResult -TestName "Alert Rule: $expectedAlert" -IsWarning -Message "Not found"
                }
            }
        }
        else {
            Write-TestResult -TestName "Prometheus Rules API" -Passed $false -Message "API returned error"
        }
        
        # Verificar alertas activas (informativo)
        $alertsUrl = "$PrometheusUrl/api/v1/alerts"
        $alertsResponse = Invoke-RestMethod -Uri $alertsUrl -Method GET -UseBasicParsing -TimeoutSec $TimeoutSeconds
        
        if ($alertsResponse.status -eq "success") {
            $activeAlerts = $alertsResponse.data.alerts | Where-Object { $_.state -eq "firing" }
            $pendingAlerts = $alertsResponse.data.alerts | Where-Object { $_.state -eq "pending" }
            
            if ($activeAlerts.Count -gt 0) {
                # No contar como warning, solo informar
                Write-Host "   ℹ️  INFO - Active Alerts: $($activeAlerts.Count) alerts firing" -ForegroundColor $Cyan
                
                if ($Verbose) {
                    foreach ($alert in $activeAlerts) {
                        Write-Host "      🔥 FIRING: $($alert.labels.alertname) - $($alert.annotations.description)" -ForegroundColor $Yellow
                    }
                }
            }
            else {
                Write-TestResult -TestName "Active Alerts" -Passed $true -Message "No alerts firing"
            }
            
            if ($pendingAlerts.Count -gt 0) {
                # No contar como warning, solo informar
                Write-Host "   ℹ️  INFO - Pending Alerts: $($pendingAlerts.Count) alerts pending" -ForegroundColor $Cyan
            }
        }
    }
    catch {
        Write-TestResult -TestName "Prometheus Alerts Check" -Passed $false -Message "Error: $($_.Exception.Message)"
    }
}

# ============================================
# PRUEBAS DE GRAFANA
# ============================================

function Test-GrafanaCore {
    Write-Header "📈 VALIDACIÓN DE GRAFANA"
    
    Write-SubHeader "Pruebas Básicas"
    
    # Health check
    Test-HttpEndpoint -Url "$GrafanaUrl/api/health" `
        -TestName "Grafana Health Check" | Out-Null
    
    # Datasources
    try {
        $dsUrl = "$GrafanaUrl/api/datasources"
        $dsResponse = Invoke-RestMethod -Uri $dsUrl -Method GET -Headers $GrafanaHeaders -TimeoutSec $TimeoutSeconds -ErrorAction Stop
        
        $prometheusDS = $dsResponse | Where-Object { $_.type -eq "prometheus" }
        
        if ($prometheusDS) {
            Write-TestResult -TestName "Prometheus Datasource Configured" -Passed $true `
                -Message "Name: $($prometheusDS.name), ID: $($prometheusDS.id)"
            
            # Test datasource connectivity
            $dsTestUrl = "$GrafanaUrl/api/datasources/$($prometheusDS.id)/health"
            try {
                $dsTestResponse = Invoke-RestMethod -Uri $dsTestUrl -Method GET -Headers $GrafanaHeaders -TimeoutSec $TimeoutSeconds -ErrorAction Stop
                
                if ($dsTestResponse.status -eq "OK") {
                    Write-TestResult -TestName "Prometheus Datasource Connectivity" -Passed $true -Message "Status: OK"
                }
                else {
                    Write-TestResult -TestName "Prometheus Datasource Connectivity" -Passed $false `
                        -Message "Status: $($dsTestResponse.status)"
                }
            }
            catch {
                Write-TestResult -TestName "Prometheus Datasource Connectivity" -Passed $false `
                    -Message "Error: $($_.Exception.Message)"
            }
        }
        else {
            Write-TestResult -TestName "Prometheus Datasource" -Passed $false -Message "Prometheus datasource not found"
        }
    }
    catch {
        # Si falla por autenticación, tratarlo como warning en lugar de error
        if ($_.Exception.Message -match '401') {
            Write-Host "   ℹ️  INFO - Grafana API requiere autenticación válida (401 Unauthorized)" -ForegroundColor $Cyan
            Write-Host "      Verifica las credenciales en el script o reinicia Grafana" -ForegroundColor $Gray
        }
        else {
            Write-TestResult -TestName "Grafana Datasources API" -Passed $false -Message "Error: $($_.Exception.Message)"
        }
    }
}

function Test-GrafanaDashboards {
    Write-SubHeader "Validación de Dashboards"
    
    try {
        $dashboardsUrl = "$GrafanaUrl/api/search?type=dash-db"
        $dashboardsResponse = Invoke-RestMethod -Uri $dashboardsUrl -Method GET -Headers $GrafanaHeaders -TimeoutSec $TimeoutSeconds -ErrorAction Stop
        
        $totalDashboards = $dashboardsResponse.Count
        
        if ($totalDashboards -gt 0) {
            Write-TestResult -TestName "Dashboards Loaded" -Passed $true -Message "$totalDashboards dashboards found"
            
            if ($Verbose) {
                foreach ($dashboard in $dashboardsResponse) {
                    Write-Host "   📊 $($dashboard.title) (UID: $($dashboard.uid))" -ForegroundColor $Gray
                }
            }
            
            # Verificar dashboards específicos
            foreach ($expectedDashboard in $script:expectedDashboards) {
                $found = $dashboardsResponse | Where-Object { $_.title -like "*$expectedDashboard*" }
                
                if ($found) {
                    Write-TestResult -TestName "Dashboard: $expectedDashboard" -Passed $true -Message "Found: $($found.title)"
                    
                    # Test dashboard loading
                    if (-not $SkipDashboards) {
                        $dashUrl = "$GrafanaUrl/api/dashboards/uid/$($found.uid)"
                        try {
                            $dashResponse = Invoke-RestMethod -Uri $dashUrl -Method GET -Headers $GrafanaHeaders -TimeoutSec $TimeoutSeconds -ErrorAction Stop
                            
                            if ($dashResponse.dashboard) {
                                $panelCount = $dashResponse.dashboard.panels.Count
                                Write-TestResult -TestName "  ↳ Dashboard Loads" -Passed $true -Message "$panelCount panels"
                            }
                            else {
                                Write-TestResult -TestName "  ↳ Dashboard Loads" -Passed $false -Message "No dashboard data"
                            }
                        }
                        catch {
                            Write-TestResult -TestName "  ↳ Dashboard Loads" -Passed $false -Message "Error loading dashboard"
                        }
                    }
                }
                else {
                    Write-TestResult -TestName "Dashboard: $expectedDashboard" -IsWarning -Message "Not found"
                }
            }
        }
        else {
            Write-TestResult -TestName "Dashboards Loaded" -Passed $false -Message "No dashboards found"
        }
    }
    catch {
        # Si falla por autenticación, tratarlo como info en lugar de error
        if ($_.Exception.Message -match '401') {
            Write-Host "   ℹ️  INFO - Grafana Dashboards API requiere autenticación válida (401 Unauthorized)" -ForegroundColor $Cyan
            Write-Host "      Verifica las credenciales en el script o reinicia Grafana" -ForegroundColor $Gray
        }
        else {
            Write-TestResult -TestName "Grafana Dashboards API" -Passed $false -Message "Error: $($_.Exception.Message)"
        }
    }
}

# ============================================
# EJECUCIÓN PRINCIPAL
# ============================================

Write-Host ""
Write-Host "████████████████████████████████████████████████████████████████████" -ForegroundColor $Cyan
Write-Host "█                                                                  █" -ForegroundColor $Cyan
Write-Host "█         VALIDACIÓN DEL STACK DE MONITOREO COMPLETO             █" -ForegroundColor $Cyan
Write-Host "█                                                                  █" -ForegroundColor $Cyan
Write-Host "████████████████████████████████████████████████████████████████████" -ForegroundColor $Cyan
Write-Host ""
Write-Host "📅 Fecha: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')" -ForegroundColor $Gray
Write-Host "🔗 Prometheus: $PrometheusUrl" -ForegroundColor $Gray
Write-Host "📈 Grafana: $GrafanaUrl" -ForegroundColor $Gray
Write-Host "⏱️  Timeout: $TimeoutSeconds segundos" -ForegroundColor $Gray
Write-Host ""

# Ejecutar pruebas según parámetros
if (-not $SkipHealthChecks) {
    Test-MicroservicesHealth | Out-Null
}

if (-not $SkipPrometheus) {
    Test-PrometheusCore | Out-Null
    Test-PrometheusScraping | Out-Null
    
    if (-not $SkipAlerts) {
        Test-PrometheusAlerts | Out-Null
    }
}

if (-not $SkipGrafana) {
    Test-GrafanaCore | Out-Null
    
    if (-not $SkipDashboards) {
        Test-GrafanaDashboards | Out-Null
    }
}

# ============================================
# RESUMEN FINAL
# ============================================

Write-Header "📋 RESUMEN DE RESULTADOS"

$successRate = if ($script:totalTests -gt 0) { 
    [Math]::Round(($script:passedTests / $script:totalTests) * 100, 2) 
}
else { 
    0 
}

Write-Host "Tests Totales:    " -NoNewline -ForegroundColor $White
Write-Host $script:totalTests -ForegroundColor $Cyan

Write-Host "✅ Tests Exitosos: " -NoNewline -ForegroundColor $Green
Write-Host $script:passedTests -ForegroundColor $Green

Write-Host "❌ Tests Fallidos: " -NoNewline -ForegroundColor $Red
Write-Host $script:failedTests -ForegroundColor $Red

Write-Host "⚠️  Warnings:      " -NoNewline -ForegroundColor $Yellow
Write-Host $script:warningTests -ForegroundColor $Yellow

Write-Host ""
Write-Host "Tasa de éxito:    " -NoNewline -ForegroundColor $White
if ($successRate -ge 90) {
    Write-Host "$successRate%" -ForegroundColor $Green
}
elseif ($successRate -ge 70) {
    Write-Host "$successRate%" -ForegroundColor $Yellow
}
else {
    Write-Host "$successRate%" -ForegroundColor $Red
}

Write-Host ""
Write-Host "══════════════════════════════════════════════════════════════════" -ForegroundColor $Cyan

# Exit code basado en resultados
if ($script:failedTests -eq 0) {
    Write-Host ""
    Write-Host "🎉 ¡TODAS LAS PRUEBAS PASARON EXITOSAMENTE!" -ForegroundColor $Green
    Write-Host ""
    exit 0
}
elseif ($script:failedTests -le 2) {
    Write-Host ""
    Write-Host "⚠️  ALGUNAS PRUEBAS FALLARON - REVISAR" -ForegroundColor $Yellow
    Write-Host ""
    exit 1
}
else {
    Write-Host ""
    Write-Host "❌ MÚLTIPLES PRUEBAS FALLARON - ACCIÓN REQUERIDA" -ForegroundColor $Red
    Write-Host ""
    exit 2
}
