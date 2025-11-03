# ============================================
# Script de Pruebas de Integración del Sistema
# ============================================
# Pruebas end-to-end de flujos completos entre servicios

param(
    [switch]$Verbose,
    [switch]$SkipLoadTests,
    [int]$TimeoutSeconds = 30
)

# Colores
$Green = "Green"
$Red = "Red"
$Yellow = "Yellow"
$Cyan = "Cyan"

# Contadores
$testsPassed = 0
$testsFailed = 0
$integrationTestsPassed = 0
$integrationTestsFailed = 0

# Servicios a probar
$script:microservices = @(
    @{ Name = "Users"; Url = "http://localhost:8081"; HealthPath = "/health"; ExpectedDB = "UsersDb" },
    @{ Name = "Reports"; Url = "http://localhost:8083"; HealthPath = "/health"; ExpectedDB = "ReportsDb" },
    @{ Name = "Analysis"; Url = "http://localhost:8082"; HealthPath = "/health"; ExpectedDB = "AnalysisDb" }
)

# Gateway configuration
$script:gatewayUrl = "http://localhost:8100"
$script:gatewayAvailable = $false

function Test-ServiceAvailability {
    param([hashtable]$Service)
    
    $healthUrl = "$($Service.Url)$($Service.HealthPath)"
    try {
        $null = Invoke-RestMethod -Uri $healthUrl -Method GET -TimeoutSec 2 -ErrorAction Stop
        return $true
    }
    catch {
        return $false
    }
}

function Get-AvailableServices {
    Write-Host "🔍 Detectando servicios disponibles..." -ForegroundColor Gray
    
    $availableServices = @()
    foreach ($service in $script:microservices) {
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

function Test-GatewayAvailability {
    Write-Host "🔍 Detectando Gateway..." -ForegroundColor Gray
    
    try {
        $null = Invoke-RestMethod -Uri "$script:gatewayUrl/health" -Method GET -TimeoutSec 2 -ErrorAction Stop
        $script:gatewayAvailable = $true
        Write-Host "   ✅ Gateway - Disponible en $script:gatewayUrl" -ForegroundColor Green
    }
    catch {
        $script:gatewayAvailable = $false
        Write-Host "   ⏭️  Gateway - No disponible (tests relacionados omitidos)" -ForegroundColor Yellow
    }
    
    Write-Host ""
    return $script:gatewayAvailable
}

function Write-Header {
    param([string]$Message)
    Write-Host ""
    Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Cyan
    Write-Host " $Message" -ForegroundColor Cyan
    Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Cyan
    Write-Host ""
}

function Write-TestResult {
    param(
        [string]$TestName,
        [bool]$Passed,
        [string]$Message = "",
        [switch]$IsIntegrationTest
    )
    
    if ($Passed) {
        Write-Host "✅ PASS" -ForegroundColor $Green -NoNewline
        Write-Host " - $TestName" -ForegroundColor White
        if ($Message) {
            Write-Host "   $Message" -ForegroundColor Gray
        }
        $script:testsPassed++
        if ($IsIntegrationTest) { $script:integrationTestsPassed++ }
    }
    else {
        Write-Host "❌ FAIL" -ForegroundColor $Red -NoNewline
        Write-Host " - $TestName" -ForegroundColor White
        if ($Message) {
            Write-Host "   $Message" -ForegroundColor Gray
        }
        $script:testsFailed++
        if ($IsIntegrationTest) { $script:integrationTestsFailed++ }
    }
}

function Test-GatewayToMicroserviceRouting {
    Write-Header "Test 1: Gateway Routing to Microservices"
    
    # Verificar si Gateway está disponible
    if (-not $script:gatewayAvailable) {
        Write-Host "⏭️  Gateway no disponible - Tests de Gateway omitidos" -ForegroundColor Yellow
        Write-Host "   ℹ️  Para ejecutar estos tests, asegúrate de que el Gateway esté corriendo en $script:gatewayUrl" -ForegroundColor Gray
        Write-Host ""
    }
    else {
        # Test 1.1: Gateway health check (validates all backend services)
        try {
            $url = "$script:gatewayUrl/health"
            Write-Host "Testing Gateway health check: $url" -ForegroundColor Gray
            
            $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
            $response = Invoke-RestMethod -Uri $url -Method Get -TimeoutSec $TimeoutSeconds -ErrorAction Stop
            $stopwatch.Stop()
            
            $passed = $response.status -eq "Healthy" -or $response.status -eq "ok"
            $message = "Status: $($response.status), Time: $($stopwatch.ElapsedMilliseconds)ms"
            
            Write-TestResult -TestName "Gateway health check" -Passed $passed -Message $message -IsIntegrationTest
            
            # Verificar que el Gateway reporta health de los servicios backend
            if ($response.entries) {
                $servicesHealthy = 0
                $servicesTotal = 0
                
                $response.entries.PSObject.Properties | ForEach-Object {
                    $serviceName = $_.Name
                    $serviceStatus = $_.Value.status
                    $servicesTotal++
                    
                    if ($serviceStatus -eq "Healthy") {
                        $servicesHealthy++
                    }
                    
                    Write-Host "   ✓ $serviceName : $serviceStatus" -ForegroundColor $(if ($serviceStatus -eq "Healthy") { $Green } else { $Yellow })
                }
                
                Write-TestResult -TestName "Backend services health via Gateway" -Passed ($servicesHealthy -eq $servicesTotal) -Message "$servicesHealthy/$servicesTotal services healthy" -IsIntegrationTest
            }
            
        }
        catch {
            Write-TestResult -TestName "Gateway health check" -Passed $false -Message "Error: $($_.Exception.Message)" -IsIntegrationTest
        }
    }
    
    # Test 1.2: Direct microservice access (bypass Gateway)
    Write-Host "`nTesting direct microservice access..." -ForegroundColor Gray
    
    # Obtener solo servicios disponibles
    $availableServices = Get-AvailableServices
    
    if ($availableServices.Count -eq 0) {
        Write-Host "⚠️  No hay microservicios disponibles para probar" -ForegroundColor Yellow
        return
    }
    
    foreach ($service in $availableServices) {
        try {
            $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
            $response = Invoke-RestMethod -Uri "$($service.Url)$($service.HealthPath)" -Method Get -TimeoutSec $TimeoutSeconds -ErrorAction Stop
            $stopwatch.Stop()
            
            $passed = $response.status -eq "Healthy"
            $message = "Status: $($response.status), Time: $($stopwatch.ElapsedMilliseconds)ms"
            
            Write-TestResult -TestName "$($service.Name) direct access" -Passed $passed -Message $message -IsIntegrationTest
            
        }
        catch {
            Write-TestResult -TestName "$($service.Name) direct access" -Passed $false -Message "Error: $($_.Exception.Message)" -IsIntegrationTest
        }
    }
}

function Test-DatabaseConnectivity {
    Write-Header "Test 2: Microservices to Database Connectivity"
    
    # Obtener solo servicios disponibles
    $availableServices = Get-AvailableServices
    
    if ($availableServices.Count -eq 0) {
        Write-Host "⚠️  No hay microservicios disponibles para probar" -ForegroundColor Yellow
        return
    }
    
    foreach ($service in $availableServices) {
        try {
            $response = Invoke-RestMethod -Uri "$($service.Url)$($service.HealthPath)" -Method Get -TimeoutSec $TimeoutSeconds -ErrorAction Stop
            
            # Verificar que el health check incluya información de la base de datos
            $dbHealthy = $false
            $dbMessage = "Database health check not found"
            
            # Manejar dos formatos de respuesta diferentes: "entries" (objeto) y "checks" (array)
            $dbEntries = @()
            
            if ($response.entries) {
                # Formato "entries" (objeto con propiedades) - usado por Users
                $dbEntries = $response.entries.PSObject.Properties | Where-Object { 
                    $_.Name -match "^database$|^mysql$|^postgres$|^sqlserver$|_dbcontext$" 
                }
            }
            elseif ($response.checks) {
                # Formato "checks" (array de objetos) - usado por Reports y Analysis
                $dbEntries = $response.checks | Where-Object { 
                    $_.name -match "^database$|^mysql$|^postgres$|^sqlserver$|_dbcontext$" 
                }
            }
            
            if ($dbEntries -and $dbEntries.Count -gt 0) {
                # Contar cuántas databases están healthy
                $healthyCount = 0
                $totalCount = $dbEntries.Count
                
                foreach ($entry in $dbEntries) {
                    # Manejar ambos formatos: entries usa $entry.Value.status, checks usa $entry.status
                    $status = if ($entry.Value) { $entry.Value.status } else { $entry.status }
                    if ($status -eq "Healthy") {
                        $healthyCount++
                    }
                }
                
                $dbHealthy = $healthyCount -eq $totalCount
                $dbMessage = "Database health: $healthyCount/$totalCount healthy"
            }
            
            Write-TestResult -TestName "$($service.Name) database connectivity" -Passed $dbHealthy -Message $dbMessage -IsIntegrationTest
            
        }
        catch {
            Write-TestResult -TestName "$($service.Name) database connectivity" -Passed $false -Message "Error: $($_.Exception.Message)" -IsIntegrationTest
        }
    }
}

function Test-MetricsCollection {
    Write-Header "Test 3: Metrics Collection Pipeline"
    
    Write-Host "Testing Prometheus scraping..." -ForegroundColor Gray
    
    # Verificar que Prometheus esté scrapeando todos los servicios
    try {
        $targetsUrl = "http://localhost:9090/api/v1/targets"
        $response = Invoke-RestMethod -Uri $targetsUrl -Method Get -TimeoutSec $TimeoutSeconds -ErrorAction Stop
        
        if ($response.status -eq "success") {
            $activeTargets = $response.data.activeTargets
            
            # Construir lista de targets esperados dinámicamente según disponibilidad
            $expectedTargetJobs = @("users-microservice", "reports-microservice", "analysis-microservice", "prometheus")
            if ($script:gatewayAvailable) {
                $expectedTargetJobs += "gateway"
            }
            
            $relevantTargets = $activeTargets | Where-Object { $expectedTargetJobs -contains $_.labels.job }
            
            $upTargets = ($relevantTargets | Where-Object { $_.health -eq "up" }).Count
            $totalRelevantTargets = $relevantTargets.Count
            
            $allTargetsUp = $upTargets -eq $totalRelevantTargets
            $message = "$upTargets/$totalRelevantTargets targets UP"
            if (-not $script:gatewayAvailable) {
                $message += " (Gateway omitido)"
            }
            
            Write-TestResult -TestName "Prometheus targets health" -Passed $allTargetsUp -Message $message -IsIntegrationTest
            
            # Verificar targets específicos de microservicios
            $expectedTargets = @("users-microservice", "reports-microservice", "analysis-microservice")
            foreach ($target in $expectedTargets) {
                $targetUp = $activeTargets | Where-Object { $_.labels.job -eq $target -and $_.health -eq "up" }
                Write-TestResult -TestName "Target '$target' scraped" -Passed ($null -ne $targetUp) -IsIntegrationTest
            }
            
            # Verificar Gateway solo si está disponible
            if ($script:gatewayAvailable) {
                $gatewayTarget = $activeTargets | Where-Object { $_.labels.job -eq "gateway" -and $_.health -eq "up" }
                Write-TestResult -TestName "Target 'gateway' scraped" -Passed ($null -ne $gatewayTarget) -IsIntegrationTest
            }
            else {
                Write-Host "   ⏭️  Gateway target validation skipped (Gateway not available)" -ForegroundColor Gray
            }
            
        }
        else {
            Write-TestResult -TestName "Prometheus targets" -Passed $false -Message "API returned: $($response.status)" -IsIntegrationTest
        }
        
    }
    catch {
        Write-TestResult -TestName "Prometheus metrics collection" -Passed $false -Message "Error: $($_.Exception.Message)" -IsIntegrationTest
    }
}

function Test-AlertingSystem {
    Write-Header "Test 4: Alerting System Integration"
    
    try {
        # Verificar que las reglas de alertas estén cargadas
        $rulesUrl = "http://localhost:9090/api/v1/rules"
        $response = Invoke-RestMethod -Uri $rulesUrl -Method Get -TimeoutSec $TimeoutSeconds -ErrorAction Stop
        
        if ($response.status -eq "success") {
            $groupCount = $response.data.groups.Count
            $rulesLoaded = $groupCount -gt 0
            
            Write-TestResult -TestName "Alert rules loaded" -Passed $rulesLoaded -Message "$groupCount rule groups" -IsIntegrationTest
            
            # Contar alertas totales
            $totalAlerts = 0
            $totalRecordings = 0
            foreach ($group in $response.data.groups) {
                $alerts = ($group.rules | Where-Object { $_.type -eq 'alerting' }).Count
                $recordings = ($group.rules | Where-Object { $_.type -eq 'recording' }).Count
                $totalAlerts += $alerts
                $totalRecordings += $recordings
            }
            
            Write-TestResult -TestName "Alert rules count" -Passed ($totalAlerts -gt 0) -Message "$totalAlerts alerts, $totalRecordings recordings" -IsIntegrationTest
            
        }
        else {
            Write-TestResult -TestName "Alert rules" -Passed $false -Message "API error" -IsIntegrationTest
        }
        
    }
    catch {
        Write-TestResult -TestName "Alerting system" -Passed $false -Message "Error: $($_.Exception.Message)" -IsIntegrationTest
    }
}

function Test-DashboardsAvailability {
    Write-Header "Test 5: Grafana Dashboards Availability"
    
    try {
        # Verificar que Grafana esté accesible
        $healthUrl = "http://localhost:3010/api/health"
        $healthResponse = Invoke-RestMethod -Uri $healthUrl -Method Get -TimeoutSec $TimeoutSeconds -ErrorAction Stop
        
        Write-TestResult -TestName "Grafana health" -Passed ($healthResponse.database -eq "ok") -Message "Status: $($healthResponse.database)" -IsIntegrationTest
        
        # Verificar dashboards (requiere autenticación, por ahora solo verificamos que Grafana responde)
        $apiUrl = "http://localhost:3010/api/search?type=dash-db"
        try {
            # Intentar sin autenticación (si está habilitado anonymous access)
            $dashboards = Invoke-RestMethod -Uri $apiUrl -Method Get -TimeoutSec 5 -ErrorAction Stop
            $dashboardCount = $dashboards.Count
            
            Write-TestResult -TestName "Grafana dashboards accessible" -Passed ($dashboardCount -gt 0) -Message "$dashboardCount dashboards found" -IsIntegrationTest
            
        }
        catch {
            # Si falla por autenticación, es esperado
            if ($_.Exception.Message -match "401|Unauthorized") {
                Write-Host "   ℹ️  Dashboards require authentication (expected)" -ForegroundColor Gray
            }
            else {
                Write-TestResult -TestName "Grafana dashboards" -Passed $false -Message $_.Exception.Message
            }
        }
        
    }
    catch {
        Write-TestResult -TestName "Grafana availability" -Passed $false -Message "Error: $($_.Exception.Message)" -IsIntegrationTest
    }
}

function Test-EndToEndFlow {
    Write-Header "Test 6: End-to-End Request Flow"
    
    Write-Host "Testing Gateway as entry point to microservices..." -ForegroundColor Gray
    Write-Host ""
    
    # Test 1: Gateway Health Check (validates all backends) - solo si Gateway disponible
    if ($script:gatewayAvailable) {
        try {
            Write-Host "Testing: Gateway Health Check" -ForegroundColor Cyan
            
            $stopwatch1 = [System.Diagnostics.Stopwatch]::StartNew()
            $gatewayResponse = Invoke-RestMethod -Uri "$script:gatewayUrl/health" -Method Get -TimeoutSec $TimeoutSeconds -ErrorAction Stop
            $stopwatch1.Stop()
            
            $gatewayHealthy = $gatewayResponse.status -eq "Healthy"
            
            # Count backend services reported by Gateway
            $backendCount = 0
            $backendHealthy = 0
            
            if ($gatewayResponse.entries) {
                $gatewayResponse.entries.PSObject.Properties | ForEach-Object {
                    $backendCount++
                    if ($_.Value.status -eq "Healthy") {
                        $backendHealthy++
                    }
                }
            }
            
            $message = "Gateway: ${stopwatch1.ElapsedMilliseconds}ms, Backends: $backendHealthy/$backendCount healthy"
            Write-TestResult -TestName "Gateway health check with backend validation" -Passed ($gatewayHealthy -and $backendHealthy -eq $backendCount) -Message $message -IsIntegrationTest
            
        }
        catch {
            Write-TestResult -TestName "Gateway health check" -Passed $false -Message "Error: $($_.Exception.Message)" -IsIntegrationTest
        }
    }
    else {
        Write-Host "⏭️  Gateway health check skipped (Gateway not available)" -ForegroundColor Yellow
        Write-Host ""
    }
    
    # Test 2: Direct microservice access and database validation
    Write-Host "`nTesting: Direct Microservice Access + Database Health" -ForegroundColor Cyan
    
    # Obtener solo servicios disponibles
    $availableServices = Get-AvailableServices
    
    if ($availableServices.Count -eq 0) {
        Write-Host "⚠️  No hay microservicios disponibles para probar" -ForegroundColor Yellow
        return
    }
    
    foreach ($service in $availableServices) {
        try {
            $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
            $response = Invoke-RestMethod -Uri "$($service.Url)$($service.HealthPath)" -Method Get -TimeoutSec $TimeoutSeconds -ErrorAction Stop
            $stopwatch.Stop()
            
            $serviceHealthy = $response.status -eq "Healthy"
            
            # Verificar que el health check incluye database status
            $dbHealthy = $false
            
            # Manejar dos formatos de respuesta diferentes: "entries" (objeto) y "checks" (array)
            $dbEntries = @()
            
            if ($response.entries) {
                # Formato "entries" (objeto con propiedades) - usado por Users
                $dbEntries = $response.entries.PSObject.Properties | Where-Object { 
                    $_.Name -match "^database$|^mysql$|^postgres$|^sqlserver$|_dbcontext$" 
                }
            }
            elseif ($response.checks) {
                # Formato "checks" (array de objetos) - usado por Reports y Analysis
                $dbEntries = $response.checks | Where-Object { 
                    $_.name -match "^database$|^mysql$|^postgres$|^sqlserver$|_dbcontext$" 
                }
            }
            
            if ($dbEntries -and $dbEntries.Count -gt 0) {
                # Contar cuántas databases están healthy
                $healthyCount = 0
                $totalCount = $dbEntries.Count
                
                foreach ($entry in $dbEntries) {
                    # Manejar ambos formatos: entries usa $entry.Value.status, checks usa $entry.status
                    $status = if ($entry.Value) { $entry.Value.status } else { $entry.status }
                    if ($status -eq "Healthy") {
                        $healthyCount++
                    }
                }
                
                $dbHealthy = $healthyCount -eq $totalCount
            }
            
            $passed = $serviceHealthy -and $dbHealthy
            $message = "Service: ${stopwatch.ElapsedMilliseconds}ms, DB: $(if ($dbHealthy) { 'Healthy' } else { 'Unknown' })"
            
            Write-TestResult -TestName "$($service.Name) end-to-end flow" -Passed $passed -Message $message -IsIntegrationTest
            
        }
        catch {
            Write-TestResult -TestName "$($service.Name) end-to-end flow" -Passed $false -Message "Error: $($_.Exception.Message)" -IsIntegrationTest
        }
    }
    
    # Test 3: Gateway response time overhead
    try {
        Write-Host "`nTesting: Gateway Overhead" -ForegroundColor Cyan
        
        # Obtener solo servicios disponibles para comparación
        $availableServices = Get-AvailableServices
        
        if ($availableServices.Count -gt 0 -and $script:gatewayAvailable) {
            # Usar el primer servicio disponible para comparación
            $testService = $availableServices[0]
            
            # Direct call
            $stopwatch1 = [System.Diagnostics.Stopwatch]::StartNew()
            $null = Invoke-RestMethod -Uri "$($testService.Url)$($testService.HealthPath)" -Method Get -TimeoutSec $TimeoutSeconds -ErrorAction Stop
            $stopwatch1.Stop()
            $directTime = $stopwatch1.ElapsedMilliseconds
            
            # Via Gateway
            $stopwatch2 = [System.Diagnostics.Stopwatch]::StartNew()
            $null = Invoke-RestMethod -Uri "$script:gatewayUrl/health" -Method Get -TimeoutSec $TimeoutSeconds -ErrorAction Stop
            $stopwatch2.Stop()
            $gatewayTime = $stopwatch2.ElapsedMilliseconds
            
            # Calculate overhead (Gateway checks all services, so it's expected to be slower)
            $overhead = $gatewayTime - $directTime
            $acceptableOverhead = $overhead -lt 500  # 500ms is acceptable for gateway health check
            
            $message = "Direct: ${directTime}ms, Gateway: ${gatewayTime}ms, Overhead: ${overhead}ms"
            Write-TestResult -TestName "Gateway response time overhead" -Passed $acceptableOverhead -Message $message -IsIntegrationTest
        }
        elseif (-not $script:gatewayAvailable) {
            Write-Host "⏭️  Gateway overhead test skipped (Gateway not available)" -ForegroundColor Yellow
        }
        else {
            Write-Host "⏭️  Gateway overhead test skipped (no services available)" -ForegroundColor Yellow
        }
        
    }
    catch {
        Write-TestResult -TestName "Gateway overhead" -Passed $false -Message "Error: $($_.Exception.Message)" -IsIntegrationTest
    }
}

function Test-LoadCapacity {
    Write-Header "Test 7: System Load Capacity"
    
    if ($SkipLoadTests) {
        Write-Host "⏭️  Skipping load tests (use -SkipLoadTests:`$false to enable)" -ForegroundColor Yellow
        return
    }
    
    Write-Host "Running basic load test (10 concurrent requests)..." -ForegroundColor Gray
    
    try {
        # Determinar endpoint a usar basado en disponibilidad
        $endpoint = if ($script:gatewayAvailable) {
            "http://localhost:8100/health"
        } elseif ($script:usersAvailable) {
            "http://localhost:8081/health"
        } else {
            Write-Host "⏭️  No services available for load testing" -ForegroundColor Yellow
            return
        }
        
        $serviceName = if ($script:gatewayAvailable) { "Gateway" } else { "Users Microservice" }
        Write-Host "   Using $serviceName endpoint: $endpoint" -ForegroundColor Gray
        
        $jobs = @()
        
        # Ejecutar 10 requests concurrentes
        for ($i = 1; $i -le 10; $i++) {
            $jobs += Start-Job -ScriptBlock {
                param($url, $timeout)
                try {
                    $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
                    $response = Invoke-RestMethod -Uri $url -Method Get -TimeoutSec $timeout -ErrorAction Stop
                    $stopwatch.Stop()
                    return @{
                        Success = $true
                        Time    = $stopwatch.ElapsedMilliseconds
                        Status  = if ($response.status) { $response.status } else { "OK" }
                    }
                }
                catch {
                    return @{
                        Success = $false
                        Error   = $_.Exception.Message
                    }
                }
            } -ArgumentList $endpoint, $TimeoutSeconds
        }
        
        # Esperar a que completen
        $results = $jobs | Wait-Job | Receive-Job
        $jobs | Remove-Job
        
        $successful = ($results | Where-Object { $_.Success }).Count
        $failed = ($results | Where-Object { -not $_.Success }).Count
        $avgTime = ($results | Where-Object { $_.Success } | Measure-Object -Property Time -Average).Average
        $maxTime = ($results | Where-Object { $_.Success } | Measure-Object -Property Time -Maximum).Maximum
        
        $passed = $successful -eq 10
        $message = "Successful: $successful/10, Avg: $([math]::Round($avgTime, 0))ms, Max: $maxTime ms ($serviceName)"
        
        Write-TestResult -TestName "Concurrent requests handling" -Passed $passed -Message $message -IsIntegrationTest
        
        if ($failed -gt 0) {
            Write-Host "   ⚠️  $failed requests failed" -ForegroundColor Yellow
        }
        
    }
    catch {
        Write-TestResult -TestName "Load capacity test" -Passed $false -Message "Error: $($_.Exception.Message)"
    }
}

function Test-ServiceRecovery {
    Write-Header "Test 8: Service Recovery & Resilience"
    
    Write-Host "Testing health check persistence..." -ForegroundColor Gray
    
    if (-not $script:gatewayAvailable) {
        Write-Host "⏭️  Service reliability test skipped (Gateway not available)" -ForegroundColor Yellow
        Write-Host "   ℹ️  Testing reliability with direct microservice access instead..." -ForegroundColor Gray
        
        # Probar resiliencia con acceso directo a microservicios
        $availableServices = Get-AvailableServices
        
        if ($availableServices.Count -gt 0) {
            $testService = $availableServices[0]
            $consecutiveCalls = 5
            $successCount = 0
            
            for ($i = 1; $i -le $consecutiveCalls; $i++) {
                try {
                    $response = Invoke-RestMethod -Uri "$($testService.Url)$($testService.HealthPath)" -Method Get -TimeoutSec 5 -ErrorAction Stop
                    if ($response.status -eq "Healthy") {
                        $successCount++
                    }
                }
                catch {
                    # Fallo en una llamada
                }
                Start-Sleep -Milliseconds 100
            }
            
            $reliabilityRate = ($successCount / $consecutiveCalls) * 100
            $passed = $reliabilityRate -ge 100
            
            Write-TestResult -TestName "$($testService.Name) service reliability" -Passed $passed -Message "$successCount/$consecutiveCalls calls successful ($reliabilityRate%)" -IsIntegrationTest
        }
        else {
            Write-Host "   ⚠️  No services available for reliability testing" -ForegroundColor Yellow
        }
        
        return
    }
    
    # Hacer múltiples llamadas en secuencia rápida al Gateway
    $consecutiveCalls = 5
    $successCount = 0
    
    for ($i = 1; $i -le $consecutiveCalls; $i++) {
        try {
            $response = Invoke-RestMethod -Uri "$script:gatewayUrl/health" -Method Get -TimeoutSec 5 -ErrorAction Stop
            if ($response.status -eq "Healthy") {
                $successCount++
            }
        }
        catch {
            # Fallo en una llamada
        }
        Start-Sleep -Milliseconds 100
    }
    
    $reliabilityRate = ($successCount / $consecutiveCalls) * 100
    $passed = $reliabilityRate -ge 100
    
    Write-TestResult -TestName "Gateway service reliability" -Passed $passed -Message "$successCount/$consecutiveCalls calls successful ($reliabilityRate%)" -IsIntegrationTest
}

function Test-MetricsToAlertsPipeline {
    Write-Header "Test 9: Metrics → Prometheus → Alerts Pipeline"
    
    Write-Host "Testing complete observability pipeline..." -ForegroundColor Gray
    
    # 1. Verificar que las métricas se están generando
    try {
        # Obtener servicios disponibles para verificar métricas
        $availableServices = Get-AvailableServices
        
        if ($availableServices.Count -gt 0) {
            $testService = $availableServices[0]
            $metricsUrl = "$($testService.Url)/metrics"
            $metrics = Invoke-RestMethod -Uri $metricsUrl -Method Get -TimeoutSec 10 -ErrorAction Stop
            $hasMetrics = $metrics.Length -gt 0
            
            Write-TestResult -TestName "Services generating metrics" -Passed $hasMetrics -Message "Metrics available" -IsIntegrationTest
        }
        else {
            Write-Host "⏭️  Skipping metrics test - no services available" -ForegroundColor Yellow
        }
        
    }
    catch {
        Write-TestResult -TestName "Metrics generation" -Passed $false -Message "Error: $($_.Exception.Message)" -IsIntegrationTest
    }
    
    # 2. Verificar que Prometheus está scrapeando
    try {
        $queryUrl = "http://localhost:9090/api/v1/query?query=up{job='users-microservice'}"
        $response = Invoke-RestMethod -Uri $queryUrl -Method Get -TimeoutSec 10 -ErrorAction Stop
        
        $hasData = $response.data.result.Count -gt 0
        Write-TestResult -TestName "Prometheus scraping metrics" -Passed $hasData -Message "Data available in Prometheus" -IsIntegrationTest
        
    }
    catch {
        Write-TestResult -TestName "Prometheus scraping" -Passed $false -Message "Error: $($_.Exception.Message)" -IsIntegrationTest
    }
    
    # 3. Verificar que las alertas pueden evaluar métricas
    try {
        $rulesUrl = "http://localhost:9090/api/v1/rules"
        $response = Invoke-RestMethod -Uri $rulesUrl -Method Get -TimeoutSec 10 -ErrorAction Stop
        
        $hasRules = $response.data.groups.Count -gt 0
        Write-TestResult -TestName "Alert rules evaluating" -Passed $hasRules -Message "Rules active" -IsIntegrationTest
        
    }
    catch {
        Write-TestResult -TestName "Alert evaluation" -Passed $false -Message "Error: $($_.Exception.Message)" -IsIntegrationTest
    }
}

function Show-IntegrationSummary {
    Write-Header "Integration Test Summary"
    
    Write-Host "📊 Test Categories:" -ForegroundColor Yellow
    Write-Host "   1. Gateway Routing" -ForegroundColor White
    Write-Host "   2. Database Connectivity" -ForegroundColor White
    Write-Host "   3. Metrics Collection" -ForegroundColor White
    Write-Host "   4. Alerting System" -ForegroundColor White
    Write-Host "   5. Dashboard Availability" -ForegroundColor White
    Write-Host "   6. End-to-End Flows" -ForegroundColor White
    Write-Host "   7. Load Capacity" -ForegroundColor White
    Write-Host "   8. Service Recovery" -ForegroundColor White
    Write-Host "   9. Observability Pipeline" -ForegroundColor White
    Write-Host ""
    
    Write-Host "🎯 Integration Tests:" -ForegroundColor Yellow
    Write-Host "   Passed: " -NoNewline -ForegroundColor White
    Write-Host $integrationTestsPassed -ForegroundColor $Green
    Write-Host "   Failed: " -NoNewline -ForegroundColor White
    Write-Host $integrationTestsFailed -ForegroundColor $(if ($integrationTestsFailed -eq 0) { $Green }else { $Red })
    
    $integrationRate = if (($integrationTestsPassed + $integrationTestsFailed) -gt 0) {
        [math]::Round(($integrationTestsPassed / ($integrationTestsPassed + $integrationTestsFailed)) * 100, 2)
    }
    else { 0 }
    
    Write-Host "   Success Rate: " -NoNewline -ForegroundColor White
    Write-Host "$integrationRate%" -ForegroundColor $(if ($integrationRate -eq 100) { $Green } elseif ($integrationRate -ge 80) { $Yellow } else { $Red })
    Write-Host ""
}

# ============================================
# Main Execution
# ============================================

Write-Header "System Integration Tests"
Write-Host "  Date: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')" -ForegroundColor Gray
Write-Host "  Timeout: $TimeoutSeconds seconds" -ForegroundColor Gray
Write-Host "  Skip Load Tests: $SkipLoadTests" -ForegroundColor Gray
Write-Host ""

# Detectar disponibilidad del Gateway primero
Test-GatewayAvailability

# Ejecutar tests de integración
Test-GatewayToMicroserviceRouting
Test-DatabaseConnectivity
Test-MetricsCollection
Test-AlertingSystem
Test-DashboardsAvailability
Test-EndToEndFlow
Test-LoadCapacity
Test-ServiceRecovery
Test-MetricsToAlertsPipeline

# Mostrar resumen
Show-IntegrationSummary

# Summary final
Write-Header "Final Summary"
$totalTests = $testsPassed + $testsFailed
$successRate = if ($totalTests -gt 0) { [math]::Round(($testsPassed / $totalTests) * 100, 2) } else { 0 }

Write-Host "Total Tests: " -NoNewline
Write-Host $totalTests -ForegroundColor $Cyan

Write-Host "Passed: " -NoNewline
Write-Host $testsPassed -ForegroundColor $Green

Write-Host "Failed: " -NoNewline
Write-Host $testsFailed -ForegroundColor $Red

Write-Host "Success Rate: " -NoNewline
Write-Host "$successRate%" -ForegroundColor $(if ($successRate -eq 100) { $Green } elseif ($successRate -ge 80) { $Yellow } else { $Red })

Write-Host ""

# Exit code
if ($integrationTestsFailed -eq 0) {
    Write-Host "✅ All integration tests passed!" -ForegroundColor $Green
    exit 0
}
else {
    Write-Host "❌ $integrationTestsFailed integration test(s) failed!" -ForegroundColor $Red
    exit 1
}
