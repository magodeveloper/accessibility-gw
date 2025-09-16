# manage-load-tests.ps1
# Script unificado para gestionar las pruebas de carga con k6 y limpieza de resultados

<#
.SYNOPSIS
    Script unificado para gestión completa de pruebas de carga y mantenimiento

.DESCRIPTION
    Script todo-en-uno que unifica funcionalidades de:
    - Pruebas de carga con k6 (smoke, load, stress, spike, endurance)
    - Limpieza de resultados de pruebas (local y global)
    - Testing con verbose output
    - Gestión de instalación de k6
    - Generación de reportes

.PARAMETER Action
    Acción a ejecutar:
    - smoke: Prueba básica de funcionalidad
    - load: Prueba de carga normal
       # Verificar instalación de k6 (excepto para help)
    if ($Action -ne "help" -and -not (Test-K6Installation)) {
        if ($Action -eq "install") {
            if (-not (Install-K6)) {
                exit 1
            }
        }
        else {
            Write-ColoredOutput "❌ k6 is required. Run with -Action install to install it." "Error"
            exit 1
        }
    }
    
    # Verificar salud del servicio (excepto para install, clean, test-verbose y help)
    if ($Action -notin @("install", "clean", "clean-all", "report", "test-verbose", "help")) {
        if (-not (Test-ServiceHealth -Url $BaseUrl)) {
            Write-ColoredOutput "❌ Service is not healthy. Please check the Gateway is running." "Error"
            exit 1
        }
    }el servicio (excepto para install, clean, test-verbose y help)
    if ($Action -notin @("install", "clean", "clean-all", "report", "test-verbose", "help")) {
        if (-not (Test-ServiceHealth -Url $BaseUrl)) {
            Write-ColoredOutput "❌ Service is not healthy. Please check the Gateway is running." "Error"
            exit 1
        }
    }
    
    # Ejecutar acción solicitada
    switch ($Action) {
        "help" {
            Show-Help
        }
        
        "install" {
            Write-ColoredOutput "✅ k6 installation completed" "Success"
        } Prueba de estrés
    - spike: Prueba de picos de carga
    - endurance: Prueba de resistencia (~40 min)
    - all: Ejecutar todas las pruebas
    - install: Instalar k6
    - report: Generar reporte de resultados
    - clean: Limpiar resultados antiguos de load tests
    - clean-all: Limpiar todos los TestResults del proyecto
    - test-verbose: Ejecutar script de prueba con verbose

.PARAMETER BaseUrl
    URL base del Gateway a probar (default: http://localhost:5000)

.PARAMETER OutputDir
    Directorio para resultados (default: results)

.PARAMETER Users
    Número de usuarios virtuales (override automático)

.PARAMETER Duration
    Duración del test (override automático)

.PARAMETER VerboseOutput
    Activar salida detallada

.PARAMETER GenerateReport
    Generar reporte después de ejecutar tests

.PARAMETER SkipHealthCheck
    Omitir verificación de health del servicio

.PARAMETER DaysOld
    Días de antigüedad para limpieza (default: 7 para load tests, 2 para clean-all)

.PARAMETER WhatIf
    Mostrar qué archivos se eliminarían sin eliminarlos (solo para clean-all)

.EXAMPLES
    .\manage-load-tests.ps1 smoke
    .\manage-load-tests.ps1 load -VerboseOutput
    .\manage-load-tests.ps1 all -GenerateReport
    .\manage-load-tests.ps1 clean -DaysOld 3
    .\manage-load-tests.ps1 clean-all -DaysOld 1 -WhatIf
    .\manage-load-tests.ps1 test-verbose -VerboseOutput
    .\manage-load-tests.ps1 install

.NOTES
    Versión unificada que reemplaza:
    - clean-test-results.ps1 (funcionalidad incluida en clean-all)
    - test-script.ps1 (funcionalidad incluida en test-verbose)
#>

param(
    [Parameter(Mandatory=$false)]
    [ValidateSet("smoke", "load", "stress", "spike", "endurance", "all", "install", "report", "clean", "clean-all", "test-verbose", "dashboard", "help")]
    [string]$Action = "smoke",
    
    [Parameter(Mandatory=$false)]
    [string]$BaseUrl = "http://localhost:5000",
    
    [Parameter(Mandatory=$false)]
    [string]$OutputDir = "results",
    
    [Parameter(Mandatory=$false)]
    [int]$Users = 0,
    
    [Parameter(Mandatory=$false)]
    [string]$Duration = "",
    
    [Parameter(Mandatory=$false)]
    [switch]$VerboseOutput,
    
    [Parameter(Mandatory=$false)]
    [switch]$GenerateReport,
    
    [Parameter(Mandatory=$false)]
    [switch]$SkipHealthCheck,
    
    [Parameter(Mandatory=$false)]
    [int]$DaysOld = 7,
    
    [Parameter(Mandatory=$false)]
    [switch]$WhatIf
)

# Configuración
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$LoadTestsDir = $ScriptDir
$ResultsDir = Join-Path $LoadTestsDir $OutputDir
$LogFile = Join-Path $ResultsDir "load-tests.log"

# Colores para output
$Colors = @{
    Info = "Cyan"
    Success = "Green"
    Warning = "Yellow"
    Error = "Red"
    Header = "Magenta"
}

function Write-ColoredOutput {
    param(
        [string]$Message,
        [string]$Color = "White"
    )
    Write-Host $Message -ForegroundColor $Colors[$Color]
}

function Write-Log {
    param([string]$Message)
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    $logMessage = "[$timestamp] $Message"
    Add-Content -Path $LogFile -Value $logMessage
    if ($VerboseOutput) {
        Write-ColoredOutput $logMessage "Info"
    }
}

function Test-K6Installation {
    Write-ColoredOutput "🔍 Checking k6 installation..." "Info"
    
    try {
        $k6Version = & k6 version 2>$null
        if ($LASTEXITCODE -eq 0) {
            Write-ColoredOutput "✅ k6 is installed: $($k6Version -split "`n" | Select-Object -First 1)" "Success"
            return $true
        }
    }
    catch {
        Write-ColoredOutput "❌ k6 is not installed or not in PATH" "Error"
        return $false
    }
    
    return $false
}

function Install-K6 {
    Write-ColoredOutput "📦 Installing k6..." "Info"
    
    if (Get-Command "winget" -ErrorAction SilentlyContinue) {
        Write-ColoredOutput "Installing k6 using winget..." "Info"
        winget install k6 --source winget
    }
    elseif (Get-Command "choco" -ErrorAction SilentlyContinue) {
        Write-ColoredOutput "Installing k6 using Chocolatey..." "Info"
        choco install k6 -y
    }
    elseif (Get-Command "scoop" -ErrorAction SilentlyContinue) {
        Write-ColoredOutput "Installing k6 using Scoop..." "Info"
        scoop install k6
    }
    else {
        Write-ColoredOutput "❌ No package manager found. Please install k6 manually from https://k6.io/docs/getting-started/installation/" "Error"
        Write-ColoredOutput "Alternatively, you can use:" "Info"
        Write-ColoredOutput "  - winget install k6" "Info"
        Write-ColoredOutput "  - choco install k6" "Info"
        Write-ColoredOutput "  - scoop install k6" "Info"
        return $false
    }
    
    return Test-K6Installation
}

function Test-ServiceHealth {
    param([string]$Url)
    
    if ($SkipHealthCheck) {
        Write-ColoredOutput "⏭️  Skipping health check" "Warning"
        return $true
    }
    
    Write-ColoredOutput "🏥 Checking service health at $Url..." "Info"
    
    try {
        $healthUrl = "$Url/health"
        $response = Invoke-WebRequest -Uri $healthUrl -Method GET -TimeoutSec 10
        
        if ($response.StatusCode -eq 200) {
            Write-ColoredOutput "✅ Service is healthy" "Success"
            return $true
        }
        else {
            Write-ColoredOutput "⚠️  Service returned status code: $($response.StatusCode)" "Warning"
            return $false
        }
    }
    catch {
        Write-ColoredOutput "❌ Service health check failed: $($_.Exception.Message)" "Error"
        return $false
    }
}

function Initialize-Environment {
    Write-ColoredOutput "🚀 ACCESSIBILITY GATEWAY LOAD TESTS" "Header"
    Write-ColoredOutput "====================================" "Header"
    
    # Crear directorio de resultados
    if (-not (Test-Path $ResultsDir)) {
        New-Item -ItemType Directory -Path $ResultsDir -Force | Out-Null
        Write-ColoredOutput "📁 Created results directory: $ResultsDir" "Info"
    }
    
    # Crear archivo de log
    if (-not (Test-Path $LogFile)) {
        New-Item -ItemType File -Path $LogFile -Force | Out-Null
    }
    
    Write-Log "Starting load tests with action: $Action"
    Write-Log "Base URL: $BaseUrl"
    Write-Log "Results directory: $ResultsDir"
}

function Get-K6Command {
    param(
        [string]$TestType,
        [hashtable]$Options = @{}
    )
    
    $scenarioFile = Join-Path $LoadTestsDir "scenarios" "$TestType-test.js"
    
    if (-not (Test-Path $scenarioFile)) {
        throw "Test scenario file not found: $scenarioFile"
    }
    
    $k6Args = @("run")
    
    # Configurar salida
    $resultFile = Join-Path $ResultsDir "$TestType-results-$(Get-Date -Format 'yyyyMMdd-HHmmss').json"
    $k6Args += "--out", "json=$resultFile"
    
    # Variables de entorno
    $k6Args += "--env", "BASE_URL=$BaseUrl"
    
    if ($Users -gt 0) {
        $k6Args += "--env", "USERS=$Users"
    }
    
    if ($Duration) {
        $k6Args += "--env", "DURATION=$Duration"
    }
    
    if ($VerboseOutput) {
        $k6Args += "--env", "VERBOSE=true"
    }
    
    # Añadir opciones específicas
    foreach ($key in $Options.Keys) {
        $k6Args += "--env", "$key=$($Options[$key])"
    }
    
    # Archivo de escenario
    $k6Args += $scenarioFile
    
    return $k6Args, $resultFile
}

function Invoke-LoadTest {
    param(
        [string]$TestType,
        [hashtable]$Options = @{}
    )
    
    Write-ColoredOutput "🧪 Running $TestType test..." "Info"
    Write-Log "Starting $TestType test"
    
    try {
        $k6Args, $resultFile = Get-K6Command -TestType $TestType -Options $Options
        
        Write-Log "k6 command: k6 $($k6Args -join ' ')"
        
        $startTime = Get-Date
        Write-ColoredOutput "⏰ Test started at: $($startTime.ToString('yyyy-MM-dd HH:mm:ss'))" "Info"
        
        # Ejecutar k6
        & k6 @k6Args
        
        $endTime = Get-Date
        $duration = $endTime - $startTime
        
        if ($LASTEXITCODE -eq 0) {
            Write-ColoredOutput "✅ $TestType test completed successfully!" "Success"
            Write-ColoredOutput "⏰ Duration: $($duration.ToString('hh\:mm\:ss'))" "Info"
            Write-ColoredOutput "📄 Results saved to: $resultFile" "Info"
            
            Write-Log "$TestType test completed successfully in $($duration.TotalSeconds) seconds"
            
            return @{
                Success = $true
                ResultFile = $resultFile
                Duration = $duration
            }
        }
        else {
            Write-ColoredOutput "❌ $TestType test failed with exit code: $LASTEXITCODE" "Error"
            Write-Log "$TestType test failed with exit code: $LASTEXITCODE"
            
            return @{
                Success = $false
                ResultFile = $null
                Duration = $duration
            }
        }
    }
    catch {
        Write-ColoredOutput "❌ Error running $TestType test: $($_.Exception.Message)" "Error"
        Write-Log "Error running $TestType test: $($_.Exception.Message)"
        
        return @{
            Success = $false
            ResultFile = $null
            Duration = $null
            Error = $_.Exception.Message
        }
    }
}

function Invoke-AllTests {
    Write-ColoredOutput "🧪 Running complete test suite..." "Header"
    
    $testTypes = @("smoke", "load", "stress", "spike")
    $results = @{}
    $overallSuccess = $true
    $totalDuration = New-TimeSpan
    
    foreach ($testType in $testTypes) {
        Write-ColoredOutput "`n--- $testType TEST ---" "Header"
        
        $result = Invoke-LoadTest -TestType $testType
        $results[$testType] = $result
        
        if ($result.Duration) {
            $totalDuration = $totalDuration.Add($result.Duration)
        }
        
        if (-not $result.Success) {
            $overallSuccess = $false
        }
        
        # Pequeña pausa entre tests
        if ($testType -ne $testTypes[-1]) {
            Write-ColoredOutput "⏸️  Waiting 30 seconds before next test..." "Info"
            Start-Sleep -Seconds 30
        }
    }
    
    # Resumen final
    Write-ColoredOutput "`n📊 TEST SUITE SUMMARY" "Header"
    Write-ColoredOutput "=====================" "Header"
    Write-ColoredOutput "⏰ Total Duration: $($totalDuration.ToString('hh\:mm\:ss'))" "Info"
    
    foreach ($testType in $testTypes) {
        $result = $results[$testType]
        $status = if ($result.Success) { "✅ PASSED" } else { "❌ FAILED" }
        $color = if ($result.Success) { "Success" } else { "Error" }
        
        Write-ColoredOutput "$testType : $status" $color
    }
    
    if ($overallSuccess) {
        Write-ColoredOutput "`n🎉 All tests completed successfully!" "Success"
    } else {
        Write-ColoredOutput "`n⚠️  Some tests failed. Check the logs for details." "Warning"
    }
    
    return $results
}

function New-TestReport {
    Write-ColoredOutput "📊 Generating test report..." "Info"
    
    $resultFiles = Get-ChildItem -Path $ResultsDir -Filter "*-results-*.json" | Sort-Object LastWriteTime -Descending
    
    if ($resultFiles.Count -eq 0) {
        Write-ColoredOutput "❌ No result files found in $ResultsDir" "Error"
        return
    }
    
    $reportFile = Join-Path $ResultsDir "load-test-report-$(Get-Date -Format 'yyyyMMdd-HHmmss').html"
    
    $htmlContent = @"
<!DOCTYPE html>
<html>
<head>
    <title>Load Test Report - Accessibility Gateway</title>
    <style>
        body { font-family: Arial, sans-serif; margin: 20px; }
        .header { color: #2E86AB; border-bottom: 2px solid #2E86AB; padding-bottom: 10px; }
        .test-section { margin: 20px 0; padding: 15px; border: 1px solid #ddd; border-radius: 5px; }
        .success { background-color: #d4edda; border-color: #c3e6cb; }
        .warning { background-color: #fff3cd; border-color: #ffeaa7; }
        .error { background-color: #f8d7da; border-color: #f5c6cb; }
        .metric { display: inline-block; margin: 10px; padding: 10px; background: #f8f9fa; border-radius: 3px; }
        .timestamp { color: #666; font-size: 0.9em; }
    </style>
</head>
<body>
    <h1 class="header">🚀 Accessibility Gateway Load Test Report</h1>
    <p class="timestamp">Generated: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')</p>
    
    <h2>📋 Test Summary</h2>
    <div class="test-section">
        <p><strong>Base URL:</strong> $BaseUrl</p>
        <p><strong>Test Results:</strong> $($resultFiles.Count) files found</p>
        <p><strong>Latest Test:</strong> $($resultFiles[0].LastWriteTime.ToString('yyyy-MM-dd HH:mm:ss'))</p>
    </div>
    
    <h2>📊 Recent Test Results</h2>
"@

    foreach ($file in $resultFiles | Select-Object -First 5) {
        $testName = $file.Name -replace '-results-.*\.json$', ''
        $htmlContent += @"
    <div class="test-section">
        <h3>$testName Test</h3>
        <p><strong>File:</strong> $($file.Name)</p>
        <p><strong>Time:</strong> $($file.LastWriteTime.ToString('yyyy-MM-dd HH:mm:ss'))</p>
        <p><strong>Size:</strong> $([math]::Round($file.Length / 1KB, 2)) KB</p>
    </div>
"@
    }
    
    $htmlContent += @"
    
    <h2>💡 Recommendations</h2>
    <div class="test-section">
        <ul>
            <li>Run smoke tests before each deployment</li>
            <li>Schedule load tests during low-traffic periods</li>
            <li>Monitor error rates and response times</li>
            <li>Set up alerting for SLA violations</li>
        </ul>
    </div>
    
    <footer style="margin-top: 50px; padding-top: 20px; border-top: 1px solid #ddd; color: #666;">
        <p>Generated by Accessibility Gateway Load Test Suite</p>
    </footer>
</body>
</html>
"@
    
    Set-Content -Path $reportFile -Value $htmlContent -Encoding UTF8
    Write-ColoredOutput "✅ Report generated: $reportFile" "Success"
    
    # Intentar abrir el reporte
    try {
        Start-Process $reportFile
        Write-ColoredOutput "🌐 Report opened in default browser" "Info"
    }
    catch {
        Write-ColoredOutput "📄 Report saved to: $reportFile" "Info"
    }
}

function Remove-OldResults {
    param([int]$DaysOld = 7)
    
    Write-ColoredOutput "🧹 Cleaning old test results (older than $DaysOld days)..." "Info"
    
    $cutoffDate = (Get-Date).AddDays(-$DaysOld)
    $oldFiles = Get-ChildItem -Path $ResultsDir -Filter "*.json" | Where-Object { $_.LastWriteTime -lt $cutoffDate }
    
    if ($oldFiles.Count -eq 0) {
        Write-ColoredOutput "✅ No old files to clean" "Success"
        return
    }
    
    Write-ColoredOutput "🗑️  Found $($oldFiles.Count) old files to remove" "Warning"
    
    foreach ($file in $oldFiles) {
        Remove-Item $file.FullName -Force
        Write-Log "Removed old result file: $($file.Name)"
    }
    
    Write-ColoredOutput "✅ Cleaned $($oldFiles.Count) old files" "Success"
}

function Remove-AllTestResults {
    param(
        [int]$DaysOld = 2,
        [switch]$WhatIf
    )
    
    Write-ColoredOutput "🧹 Cleaning Test Results older than $DaysOld days..." "Info"
    
    $cutoffDate = (Get-Date).AddDays(-$DaysOld)
    $projectRoot = Split-Path -Parent (Split-Path -Parent (Split-Path -Parent $ScriptDir))
    
    # Limpiar TestResults principales
    $testResultsPath = Join-Path $projectRoot "TestResults"
    if (Test-Path $testResultsPath) {
        $oldItems = Get-ChildItem $testResultsPath | Where-Object { $_.LastWriteTime -lt $cutoffDate }
        
        if ($oldItems.Count -gt 0) {
            Write-ColoredOutput "📁 Found $($oldItems.Count) old items in main TestResults" "Warning"
            
            if ($WhatIf) {
                $oldItems | ForEach-Object { Write-ColoredOutput "  Would delete: $($_.Name)" "Info" }
            } else {
                $oldItems | Remove-Item -Recurse -Force
                Write-ColoredOutput "✅ Cleaned main TestResults" "Success"
            }
        } else {
            Write-ColoredOutput "✅ No old files in main TestResults" "Success"
        }
    }
    
    # Limpiar TestResults de proyectos individuales
    $testProjects = @(
        "src\tests\Gateway.UnitTests\TestResults",
        "src\tests\Gateway.IntegrationTests\TestResults",
        "src\tests\Gateway.Load\results"
    )
    
    foreach ($testProject in $testProjects) {
        $projectPath = Join-Path $projectRoot $testProject
        
        if (Test-Path $projectPath) {
            $oldItems = Get-ChildItem $projectPath | Where-Object { $_.LastWriteTime -lt $cutoffDate }
            
            if ($oldItems.Count -gt 0) {
                Write-ColoredOutput "📁 Found $($oldItems.Count) old items in $testProject" "Warning"
                
                if ($WhatIf) {
                    $oldItems | ForEach-Object { Write-ColoredOutput "  Would delete: $($_.Name)" "Info" }
                } else {
                    $oldItems | Remove-Item -Recurse -Force
                    Write-ColoredOutput "✅ Cleaned $testProject" "Success"
                }
            }
        }
    }
    
    Write-ColoredOutput "🎉 Test Results cleanup completed!" "Success"
}

function Show-Help {
    Write-ColoredOutput "`n🚀 ACCESSIBILITY GATEWAY LOAD TESTS - UNIFIED SCRIPT" "Header"
    Write-ColoredOutput "=========================================================" "Header"
    Write-ColoredOutput "`n📋 Available Actions:" "Info"
    
    $actions = @(
        @{Name="smoke"; Description="Basic functionality test (quick)"}
        @{Name="load"; Description="Normal load test"}
        @{Name="stress"; Description="Stress test with high load"}
        @{Name="spike"; Description="Spike test with sudden load bursts"}
        @{Name="endurance"; Description="Long-running endurance test (~40 min)"}
        @{Name="all"; Description="Run all load tests sequentially"}
        @{Name="install"; Description="Install k6 load testing tool"}
        @{Name="report"; Description="Generate consolidated test report"}
        @{Name="clean"; Description="Clean old load test results"}
        @{Name="clean-all"; Description="Clean all TestResults in project"}
        @{Name="test-verbose"; Description="Run verbose test script (replaces test-script.ps1)"}
        @{Name="dashboard"; Description="Generate HTML dashboard with test results"}
        @{Name="help"; Description="Show this help information"}
    )
    
    foreach ($action in $actions) {
        Write-ColoredOutput ("  • {0,-12} : {1}" -f $action.Name, $action.Description) "Info"
    }
    
    Write-ColoredOutput "`n🛠️  Common Usage Examples:" "Warning"
    Write-ColoredOutput "  .\manage-load-tests.ps1 smoke" "Success"
    Write-ColoredOutput "  .\manage-load-tests.ps1 load -VerboseOutput" "Success"
    Write-ColoredOutput "  .\manage-load-tests.ps1 all -GenerateReport" "Success"
    Write-ColoredOutput "  .\manage-load-tests.ps1 clean -DaysOld 3" "Success"
    Write-ColoredOutput "  .\manage-load-tests.ps1 clean-all -WhatIf" "Success"
    Write-ColoredOutput "  .\manage-load-tests.ps1 test-verbose -VerboseOutput" "Success"
    Write-ColoredOutput "  .\manage-load-tests.ps1 dashboard" "Success"
    
    Write-ColoredOutput "`n📁 Replaced Scripts:" "Warning"
    Write-ColoredOutput "  • clean-test-results.ps1 → clean-all action" "Info"
    Write-ColoredOutput "  • test-script.ps1 → test-verbose action" "Info"
    
    Write-ColoredOutput "`n🔧 Additional Parameters:" "Warning"
    Write-ColoredOutput "  -BaseUrl        : Target URL (default: http://localhost:5000)" "Info"
    Write-ColoredOutput "  -DaysOld        : Age threshold for cleanup (default: 7)" "Info"
    Write-ColoredOutput "  -WhatIf         : Preview what would be deleted (clean-all only)" "Info"
    Write-ColoredOutput "  -VerboseOutput  : Enable detailed logging" "Info"
    Write-ColoredOutput "  -GenerateReport : Create report after tests" "Info"
    Write-ColoredOutput "  -SkipHealthCheck: Skip service health verification" "Info"
}

function Invoke-DashboardGeneration {
    Write-ColoredOutput "📊 Generating HTML Test Dashboard..." "Info"
    
    try {
        # Navegar al directorio principal del proyecto
        $projectRoot = Split-Path -Parent (Split-Path -Parent (Split-Path -Parent $ScriptDir))
        Set-Location $projectRoot
        
        # Verificar si existe el script de dashboard
        $dashboardScript = Join-Path $projectRoot "manage-tests.ps1"
        
        if (-not (Test-Path $dashboardScript)) {
            Write-ColoredOutput "❌ Dashboard script not found at: $dashboardScript" "Error"
            return $false
        }
        
        Write-ColoredOutput "🚀 Executing dashboard generator..." "Info"
        
        # Ejecutar el script de dashboard con tests y apertura automática
        & $dashboardScript -RunTests -RunLoadTests -OpenDashboard
        
        if ($LASTEXITCODE -eq 0) {
            Write-ColoredOutput "✅ Dashboard generated successfully!" "Success"
            return $true
        }
        else {
            Write-ColoredOutput "❌ Dashboard generation failed with exit code: $LASTEXITCODE" "Error"
            return $false
        }
    }
    catch {
        Write-ColoredOutput "❌ Error generating dashboard: $($_.Exception.Message)" "Error"
        return $false
    }
    finally {
        # Regresar al directorio original
        Set-Location $ScriptDir
    }
}

function Test-VerboseScript {
    Write-ColoredOutput "🧪 Running test script with verbose output..." "Info"
    Write-ColoredOutput "Test script with Verbose = $VerboseOutput" "Success"
    
    # Información del entorno
    Write-ColoredOutput "`n📊 Environment Information:" "Header"
    Write-ColoredOutput "  • PowerShell Version: $($PSVersionTable.PSVersion)" "Info"
    Write-ColoredOutput "  • OS: $($PSVersionTable.OS)" "Info"
    Write-ColoredOutput "  • Script Directory: $ScriptDir" "Info"
    Write-ColoredOutput "  • Results Directory: $ResultsDir" "Info"
    Write-ColoredOutput "  • Base URL: $BaseUrl" "Info"
    
    # Verificar k6
    if (Test-K6Installation) {
        $k6Version = & k6 version 2>$null
        Write-ColoredOutput "  • k6 Version: $($k6Version -split "`n" | Select-Object -First 1)" "Success"
    }
    
    Write-ColoredOutput "`n✅ Verbose test completed successfully!" "Success"
}

# Script principal
try {
    # Manejo especial para help - no requiere inicialización
    if ($Action -eq "help") {
        Show-Help
        exit 0
    }
    
    Initialize-Environment
    
    # Verificar instalación de k6
    if (-not (Test-K6Installation)) {
        if ($Action -eq "install") {
            if (-not (Install-K6)) {
                exit 1
            }
        }
        else {
            Write-ColoredOutput "❌ k6 is required. Run with -Action install to install it." "Error"
            exit 1
        }
    }
    
    # Verificar salud del servicio (excepto para install, clean, test-verbose, dashboard y help)
    if ($Action -notin @("install", "clean", "clean-all", "report", "test-verbose", "dashboard")) {
        if (-not (Test-ServiceHealth -Url $BaseUrl)) {
            Write-ColoredOutput "❌ Service is not healthy. Please check the Gateway is running." "Error"
            exit 1
        }
    }
    
    # Ejecutar acción solicitada
    switch ($Action) {
        "install" {
            Write-ColoredOutput "✅ k6 installation completed" "Success"
        }
        
        "smoke" {
            $result = Invoke-LoadTest -TestType "smoke"
            if (-not $result.Success) { exit 1 }
        }
        
        "load" {
            $result = Invoke-LoadTest -TestType "load"
            if (-not $result.Success) { exit 1 }
        }
        
        "stress" {
            $result = Invoke-LoadTest -TestType "stress"
            if (-not $result.Success) { exit 1 }
        }
        
        "spike" {
            $result = Invoke-LoadTest -TestType "spike"
            if (-not $result.Success) { exit 1 }
        }
        
        "endurance" {
            Write-ColoredOutput "⚠️  Endurance test will run for ~40 minutes. Continue? (y/N)" "Warning"
            $confirmation = Read-Host
            if ($confirmation -eq 'y' -or $confirmation -eq 'Y') {
                $result = Invoke-LoadTest -TestType "endurance"
                if (-not $result.Success) { exit 1 }
            }
            else {
                Write-ColoredOutput "❌ Endurance test cancelled" "Warning"
            }
        }
        
        "all" {
            $results = Invoke-AllTests
            $failed = $results.Values | Where-Object { -not $_.Success }
            if ($failed.Count -gt 0) { exit 1 }
        }
        
        "report" {
            New-TestReport
        }
        
        "clean" {
            Remove-OldResults -DaysOld $DaysOld
        }
        
        "clean-all" {
            Remove-AllTestResults -DaysOld $DaysOld -WhatIf:$WhatIf
        }
        
        "test-verbose" {
            Test-VerboseScript
        }
        
        "dashboard" {
            Invoke-DashboardGeneration
        }
        
        default {
            Write-ColoredOutput "❌ Unknown action: $Action" "Error"
            exit 1
        }
    }
    
    # Generar reporte si se solicita
    if ($GenerateReport -and $Action -ne "report") {
        New-TestReport
    }
    
    Write-ColoredOutput "`n🎉 Load test operation completed successfully!" "Success"
}
catch {
    Write-ColoredOutput "❌ Fatal error: $($_.Exception.Message)" "Error"
    Write-Log "Fatal error: $($_.Exception.Message)"
    exit 1
}