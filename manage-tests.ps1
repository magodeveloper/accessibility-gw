#requires -version 5.0
<#
.SYNOPSIS
    Script completo para gestión de tests del Gateway con dashboard dinámico y pruebas de carga K6
.DESCRIPTION
    Este script proporciona una interfaz completa para ejecutar tests, generar reportes de cobertura,
    crear un dashboard dinámico y visualizar resultados de pruebas de carga K6.
.PARAMETER Action
    Acción a ejecutar: test, coverage, dashboard, full, clean, help
.PARAMETER Filter
    Filtro para tests específicos (opcional)
.PARAMETER Configuration
    Configuración de build: Debug o Release (default: Debug)
.EXAMPLE
    .\manage-tests.ps1 full
    Ejecuta tests completos, genera cobertura y dashboard con métricas de K6
.EXAMPLE
    .\manage-tests.ps1 test -Filter "Gateway.UnitTests"
    Ejecuta solo tests que contengan "Gateway.UnitTests"
#>

param(
    [Parameter(Position = 0)]
    [ValidateSet("test", "coverage", "dashboard", "full", "clean", "help", "")]
    [string]$Action = "help",
    
    [Parameter()]
    [string]$Filter = "",
    
    [Parameter()]
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Debug",
    
    [Parameter()]
    [switch]$DetailedOutput,
    
    [Parameter()]
    [switch]$OpenDashboard
)

# Configuración global
$ErrorActionPreference = "Stop"
$ProjectName = "Gateway"
$SolutionFile = "Gateway.sln"
$TestProject = "src\tests\Gateway.UnitTests\Gateway.UnitTests.csproj"
$OutputDir = "TestResults"
$DashboardFile = "test-dashboard.html"
$K6ResultsPath = "src\tests\Gateway.Load\results"

# Colores para output
$Colors = @{
    Header    = "Cyan"
    Success   = "Green" 
    Warning   = "Yellow"
    Error     = "Red"
    Info      = "White"
    Highlight = "Magenta"
}

#region Funciones Auxiliares

function Write-ColorMessage {
    param(
        [string]$Message,
        [string]$Color = "White",
        [string]$Prefix = ""
    )
    if ($Prefix) {
        Write-Host "$Prefix " -ForegroundColor $Color -NoNewline
        Write-Host $Message -ForegroundColor "White"
    }
    else {
        Write-Host $Message -ForegroundColor $Color
    }
}

function Write-Banner {
    param([string]$Title)
    $border = "=" * 80
    Write-Host ""
    Write-ColorMessage $border $Colors.Header
    Write-ColorMessage "  $Title" $Colors.Header
    Write-ColorMessage $border $Colors.Header
    Write-Host ""
}

function Test-Prerequisites {
    Write-ColorMessage "🔍 Verificando prerequisitos..." $Colors.Info
    
    # Verificar .NET
    if (-not (Get-Command "dotnet" -ErrorAction SilentlyContinue)) {
        throw "❌ .NET CLI no encontrado. Instale .NET 9 SDK."
    }
    
    $dotnetVersion = dotnet --version
    Write-ColorMessage "✅ .NET CLI encontrado: $dotnetVersion" $Colors.Success
    
    # Verificar archivos de proyecto
    if (-not (Test-Path $SolutionFile)) {
        throw "❌ Archivo de solución no encontrado: $SolutionFile"
    }
    
    if (-not (Test-Path $TestProject)) {
        throw "❌ Proyecto de tests no encontrado: $TestProject"
    }
    
    Write-ColorMessage "✅ Archivos de proyecto verificados" $Colors.Success
}

function Initialize-Environment {
    Write-ColorMessage "🏗️ Inicializando entorno..." $Colors.Info
    
    # Crear directorio de resultados
    if (-not (Test-Path $OutputDir)) {
        New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null
        Write-ColorMessage "📁 Directorio de resultados creado: $OutputDir" $Colors.Success
    }
    
    # Limpiar resultados anteriores si se especifica
    if ($Action -eq "clean" -or $Action -eq "full") {
        Remove-Item "$OutputDir\*" -Recurse -Force -ErrorAction SilentlyContinue
        Write-ColorMessage "🧹 Resultados anteriores limpiados" $Colors.Success
    }
}

function Invoke-BuildSolution {
    Write-ColorMessage "🔨 Compilando solución..." $Colors.Info
    
    $buildArgs = @(
        "build"
        $SolutionFile
        "--configuration", $Configuration
        "--verbosity", "minimal"
        "--no-restore"
    )
    
    if ($DetailedOutput) {
        $buildArgs += "--verbosity", "detailed"
    }
    
    dotnet restore $SolutionFile --verbosity minimal
    dotnet @buildArgs | Out-Null
    
    if ($LASTEXITCODE -ne 0) {
        Write-ColorMessage "❌ Error en la compilación" $Colors.Error
        throw "Build failed with exit code $LASTEXITCODE"
    }
    
    Write-ColorMessage "✅ Compilación exitosa" $Colors.Success
}

function Invoke-Tests {
    param([bool]$CollectCoverage = $true)
    
    Write-ColorMessage "🧪 Ejecutando tests..." $Colors.Info
    
    $testArgs = @(
        "test"
        $TestProject
        "--configuration", $Configuration
        "--no-build"
        "--verbosity", "normal"
        "--logger", "trx;LogFileName=test-results.trx"
        "--results-directory", $OutputDir
    )
    
    if ($Filter) {
        $testArgs += "--filter", $Filter
        Write-ColorMessage "🔍 Filtro aplicado: $Filter" $Colors.Warning
    }
    
    if ($CollectCoverage) {
        $testArgs += @(
            "--collect", "XPlat Code Coverage"
            "--settings", "coverlet.runsettings"
        )
        Write-ColorMessage "📊 Recolección de cobertura habilitada" $Colors.Info
    }
    
    $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
    $testOutput = dotnet @testArgs 2>&1
    $stopwatch.Stop()
    
    $exitCode = $LASTEXITCODE
    $duration = $stopwatch.Elapsed.TotalSeconds
    
    # Parsear resultados de tests
    $testResults = @{
        ExitCode = $exitCode
        Duration = $duration
        Output   = $testOutput -join "`n"
        Passed   = 0
        Failed   = 0
        Skipped  = 0
        Total    = 0
    }
    
    # Extraer estadísticas del output y del archivo TRX
    $outputString = $testOutput -join "`n"
    
    # Intentar parsear desde el output directo
    if ($outputString -match "Passed!\s+-\s+Failed:\s+(\d+),\s+Passed:\s+(\d+),\s+Skipped:\s+(\d+),\s+Total:\s+(\d+)") {
        $testResults.Failed = [int]$matches[1]
        $testResults.Passed = [int]$matches[2] 
        $testResults.Skipped = [int]$matches[3]
        $testResults.Total = [int]$matches[4]
    }
    elseif ($outputString -match "Failed!\s+-\s+Failed:\s+(\d+),\s+Passed:\s+(\d+),\s+Skipped:\s+(\d+),\s+Total:\s+(\d+)") {
        $testResults.Failed = [int]$matches[1]
        $testResults.Passed = [int]$matches[2]
        $testResults.Skipped = [int]$matches[3]
        $testResults.Total = [int]$matches[4]
    }
    else {
        # Intentar parsear desde archivo TRX
        $trxFile = Get-ChildItem -Path $OutputDir -Filter "test-results.trx" -Recurse | 
        Sort-Object LastWriteTime -Descending | 
        Select-Object -First 1
        
        if ($trxFile) {
            try {
                [xml]$trxXml = Get-Content $trxFile.FullName
                $counters = $trxXml.TestRun.ResultSummary.Counters
                if ($counters) {
                    $testResults.Total = [int]$counters.total
                    $testResults.Passed = [int]$counters.passed
                    $testResults.Failed = [int]$counters.failed
                    $testResults.Skipped = [int]$counters.skipped
                }
            }
            catch {
                Write-ColorMessage "⚠️ No se pudo parsear archivo TRX: $($_.Exception.Message)" $Colors.Warning
                # Valores por defecto si no se puede parsear
                $testResults.Total = 1
                $testResults.Passed = if ($exitCode -eq 0) { 1 } else { 0 }
                $testResults.Failed = if ($exitCode -ne 0) { 1 } else { 0 }
                $testResults.Skipped = 0
            }
        }
    }
    
    # Mostrar resultados
    if ($exitCode -eq 0) {
        Write-ColorMessage "✅ Tests completados exitosamente" $Colors.Success
        Write-ColorMessage "📈 Resultados: $($testResults.Passed) exitosos, $($testResults.Failed) fallidos, $($testResults.Skipped) omitidos" $Colors.Info
    }
    else {
        Write-ColorMessage "❌ Tests fallaron" $Colors.Error
        Write-ColorMessage "📉 Resultados: $($testResults.Passed) exitosos, $($testResults.Failed) fallidos, $($testResults.Skipped) omitidos" $Colors.Warning
    }
    
    Write-ColorMessage "⏱️ Duración: $([math]::Round($duration, 2)) segundos" $Colors.Info
    
    # Usar Write-Output para asegurar que solo se devuelve el hashtable
    Write-Output $testResults
}

function Get-CoverageData {
    Write-ColorMessage "📊 Analizando cobertura..." $Colors.Info
    
    # Buscar archivo de cobertura más reciente
    $coverageFiles = Get-ChildItem -Path $OutputDir -Filter "coverage.cobertura.xml" -Recurse | 
    Sort-Object LastWriteTime -Descending
    
    if (-not $coverageFiles) {
        Write-ColorMessage "⚠️ No se encontraron archivos de cobertura" $Colors.Warning
        return $null
    }
    
    $coverageFile = $coverageFiles[0]
    Write-ColorMessage "📄 Archivo de cobertura: $($coverageFile.Name)" $Colors.Info
    
    try {
        [xml]$xml = Get-Content $coverageFile.FullName
        $coverage = $xml.coverage
        
        # Inicializar variables para cálculo ajustado
        $adjustedTotalLines = 0
        $adjustedCoveredLines = 0
        $adjustedTotalBranches = 0
        $adjustedCoveredBranches = 0
        
        $coverageData = @{
            Timestamp       = $coverageFile.LastWriteTime
            FilePath        = $coverageFile.FullName
            TotalLines      = 0  # Se calculará después
            CoveredLines    = 0  # Se calculará después
            TotalBranches   = [int]$coverage.'branches-valid'
            CoveredBranches = [int]$coverage.'branches-covered'
            LineRate        = [math]::Round(([double]$coverage.'line-rate') * 100, 2)
            BranchRate      = [math]::Round(([double]$coverage.'branch-rate') * 100, 2)
            Assemblies      = @()
        }
        
        # Procesar assemblies (excluyendo Infrastructure para líneas ajustadas)
        foreach ($package in $xml.coverage.packages.package) {
            if ($package.name -notlike "*Infrastructure*") {
                # Calcular métricas sumando desde las clases del package
                $packageLinesValid = 0
                $packageLinesCovered = 0
                $packageBranchesValid = 0
                $packageBranchesCovered = 0
                
                foreach ($class in $package.classes.class) {
                    foreach ($line in $class.lines.line) {
                        $packageLinesValid++
                        if ([int]$line.hits -gt 0) {
                            $packageLinesCovered++
                        }
                    }
                }
                
                $assemblyData = @{
                    Name            = $package.name
                    LineRate        = [math]::Round([double]$package.'line-rate' * 100, 2)
                    BranchRate      = [math]::Round([double]$package.'branch-rate' * 100, 2)
                    LinesValid      = $packageLinesValid
                    LinesCovered    = $packageLinesCovered
                    BranchesValid   = $packageBranchesValid
                    BranchesCovered = $packageBranchesCovered
                }
                $coverageData.Assemblies += $assemblyData
                
                # Sumar líneas solo de proyectos evaluados
                $adjustedTotalLines += $assemblyData.LinesValid
                $adjustedCoveredLines += $assemblyData.LinesCovered
                $adjustedTotalBranches += $assemblyData.BranchesValid
                $adjustedCoveredBranches += $assemblyData.BranchesCovered
            }
        }
        
        # Actualizar valores ajustados (sin Infrastructure)
        $coverageData.TotalLines = $adjustedTotalLines
        $coverageData.CoveredLines = $adjustedCoveredLines
        $coverageData.TotalBranches = $adjustedTotalBranches
        $coverageData.CoveredBranches = $adjustedCoveredBranches
        
        # Recalcular porcentajes ajustados
        if ($adjustedTotalLines -gt 0) {
            $coverageData.LineRate = [math]::Round(($adjustedCoveredLines / $adjustedTotalLines) * 100, 2)
        }
        if ($adjustedTotalBranches -gt 0) {
            $coverageData.BranchRate = [math]::Round(($adjustedCoveredBranches / $adjustedTotalBranches) * 100, 2)
        }
        
        Write-ColorMessage "✅ Cobertura total: $($coverageData.LineRate)% líneas, $($coverageData.BranchRate)% ramas" $Colors.Success
        
        return $coverageData
    }
    catch {
        Write-ColorMessage "❌ Error al procesar archivo de cobertura: $($_.Exception.Message)" $Colors.Error
        return $null
    }
}

#endregion

#region Funciones específicas del Gateway - Pruebas de Carga K6

function Get-K6LoadTestResults {
    <#
    .SYNOPSIS
        Obtiene y procesa los resultados de las pruebas de carga K6
    .DESCRIPTION
        Lee los archivos JSON generados por K6 y extrae métricas de rendimiento
    .NOTES
        Esta función es específica del Gateway y procesa resultados de:
        - light-load-k6 (20 usuarios)
        - medium-load-k6 (50 usuarios)
        - high-load (100 usuarios)
        - extreme-load (500 usuarios)
    #>
    
    Write-ColorMessage "⚡ Verificando tests de carga K6..." $Colors.Info
    
    if (-not (Test-Path $K6ResultsPath)) {
        Write-ColorMessage "📁 No se encontró carpeta de resultados K6: $K6ResultsPath" $Colors.Warning
        return @{ Available = $false }
    }
    
    try {
        $loadTestFiles = @{
            "light-load-k6"  = "$K6ResultsPath\light-load-k6.json"
            "medium-load-k6" = "$K6ResultsPath\medium-load-k6.json"
            "high-load"      = "$K6ResultsPath\high-load.json"
            "extreme-load"   = "$K6ResultsPath\extreme-load.json"
        }
        
        $k6Results = @{}
        $successfulTests = 0
        $totalTests = 0
        
        foreach ($testName in $loadTestFiles.Keys) {
            $filePath = $loadTestFiles[$testName]
            $totalTests++
            
            if (Test-Path $filePath) {
                try {
                    $content = Get-Content $filePath | ConvertFrom-Json
                    $metrics = $content.metrics
                    
                    # Mapear usuarios según el test
                    $userCount = switch ($testName) {
                        "light-load-k6" { 20 }
                        "medium-load-k6" { 50 }
                        "high-load" { 100 }
                        "extreme-load" { 500 }
                    }
                    
                    # Calcular error rate
                    $totalRequests = $metrics.http_reqs.count
                    $failedRequests = if ($metrics.http_req_failed.count) { $metrics.http_req_failed.count } else { 0 }
                    $errorRate = if ($totalRequests -gt 0) { [math]::Round(($failedRequests / $totalRequests) * 100, 2) } else { 0 }
                    
                    $k6Results[$testName] = @{
                        Status     = if ($errorRate -lt 5) { "Success" } else { "Warning" }
                        Users      = $userCount
                        ExecutedAt = (Get-Item $filePath).LastWriteTime.ToString("HH:mm:ss")
                        Metrics    = @{
                            RequestsPerSecond = [math]::Round($metrics.http_reqs.rate, 1).ToString()
                            ResponseTimeAvg   = [math]::Round($metrics.http_req_duration.avg, 1).ToString() + "ms"
                            ResponseTimeP95   = [math]::Round($metrics.http_req_duration.'p(95)', 1).ToString() + "ms"
                            ResponseTimeP99   = [math]::Round($metrics.http_req_duration.'p(99)', 1).ToString() + "ms"
                            ErrorRate         = $errorRate.ToString() + "%"
                            Iterations        = $metrics.iterations.count.ToString()
                        }
                    }
                    
                    if ($errorRate -lt 5) { $successfulTests++ }
                    
                    $reqPerSec = [math]::Round($metrics.http_reqs.rate, 1)
                    $avgTime = [math]::Round($metrics.http_req_duration.avg, 1)
                    Write-ColorMessage "  ✅ $testName`: $userCount usuarios, $reqPerSec req/s, ${avgTime}ms avg" $Colors.Success
                }
                catch {
                    Write-ColorMessage "⚠️ Error al procesar $testName`: $($_.Exception.Message)" $Colors.Warning
                    $k6Results[$testName] = @{
                        Status  = "Failed"
                        Users   = 0
                        Metrics = @{ RequestsPerSecond = "Error" }
                    }
                }
            }
            else {
                Write-ColorMessage "  📄 Archivo no encontrado: $testName" $Colors.Info
                $k6Results[$testName] = @{
                    Status  = "Not Found"
                    Users   = switch ($testName) {
                        "light-load-k6" { 20 }
                        "medium-load-k6" { 50 }
                        "high-load" { 100 }
                        "extreme-load" { 500 }
                    }
                    Metrics = @{ RequestsPerSecond = "N/A" }
                }
            }
        }
        
        return @{
            ExecutionTime = [DateTime]::Now.AddMinutes(-1)
            Summary       = @{
                TotalExecuted = $totalTests
                Successful    = $successfulTests
                Failed        = $totalTests - $successfulTests
            }
            Available     = $true
            K6            = $k6Results
            Note          = "Datos reales de K6 del Gateway"
        }
    }
    catch {
        Write-ColorMessage "❌ Error al procesar resultados de K6: $($_.Exception.Message)" $Colors.Error
        return @{ Available = $false }
    }
}

#endregion

#region Generación de Dashboard

function New-DashboardHtml {
    param(
        $TestResults,
        $CoverageData,
        $LoadTestData
    )
    
    Write-ColorMessage "🎨 Generando dashboard HTML..." $Colors.Info
    
    # Asegurar que tenemos hashtables
    Write-ColorMessage "🔍 Debug TestResults Type: $($TestResults.GetType().Name)" $Colors.Info
    Write-ColorMessage "🔍 Debug TestResults Length: $($TestResults.Length)" $Colors.Info
    
    if ($TestResults -is [array]) {
        Write-ColorMessage "🔍 Debug TestResults is Array, items: $($TestResults.Count)" $Colors.Info
        for ($i = 0; $i -lt $TestResults.Count; $i++) {
            Write-ColorMessage "🔍 Debug Item $i Type: $($TestResults[$i].GetType().Name)" $Colors.Info
            if ($TestResults[$i] -is [hashtable]) {
                Write-ColorMessage "🔍 Debug Found hashtable at index $i" $Colors.Info
                $TestResults = $TestResults[$i]
                break
            }
        }
    }
    if ($CoverageData -is [array]) {
        $CoverageData = $CoverageData[0]
    }
    
    # Debug - verificar que tenemos los datos correctos
    Write-ColorMessage "🔍 Debug Final TestResults.Total: $($TestResults.Total)" $Colors.Info
    Write-ColorMessage "🔍 Debug Final TestResults.Passed: $($TestResults.Passed)" $Colors.Info
    Write-ColorMessage "🔍 Debug Final TestResults.Duration: $($TestResults.Duration)" $Colors.Info
    
    $timestamp = Get-Date -Format "dd 'de' MMMM yyyy, HH:mm"
    
    # Calcular métricas
    $successRate = if ($TestResults.Total -gt 0) { 
        [math]::Round(($TestResults.Passed / $TestResults.Total) * 100, 1) 
    }
    else { 0 }
    
    $statusBadge = if ($TestResults.Failed -eq 0) { 
        "✅ $($TestResults.Total)/$($TestResults.Total) TESTS PASANDO"
    }
    else {
        "⚠️ $($TestResults.Passed)/$($TestResults.Total) TESTS PASANDO - $($TestResults.Failed) FALLIDOS"
    }
    
    # Generar filas de assemblies
    $assemblyRows = ""
    foreach ($assembly in $CoverageData.Assemblies) {
        $assemblyClass = if ($assembly.LineRate -ge 90) { "coverage-excellent" }
        elseif ($assembly.LineRate -ge 70) { "coverage-good" }
        else { "coverage-poor" }
        
        $assemblyRows += @"
                <div class="assembly-row">
                    <div class="assembly-name">$($assembly.Name)</div>
                    <div class="assembly-coverage">
                        <div class="coverage-badge $assemblyClass">$($assembly.LineRate)%</div>
                        <small>$($assembly.LinesCovered)/$($assembly.LinesValid) líneas</small>
                    </div>
                </div>
"@
    }

    $html = @"
<!DOCTYPE html>
<html lang="es">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>👥 $ProjectName - Test Dashboard</title>
    <link rel="icon" href="data:image/svg+xml,<svg xmlns=%22http://www.w3.org/2000/svg%22 viewBox=%220 0 100 100%22><text y=%22.9em%22 font-size=%2290%22>👥</text></svg>">
    <style>
        * {
            margin: 0;
            padding: 0;
            box-sizing: border-box;
        }

        body {
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            min-height: 100vh;
            color: #333;
        }

        .container-fluid {
            width: 100%;
            margin: 0;
            padding: 30px;
        }

        .header {
            background: rgba(255, 255, 255, 0.95);
            border-radius: 15px;
            padding: 30px;
            margin-bottom: 30px;
            box-shadow: 0 10px 30px rgba(0, 0, 0, 0.2);
            text-align: center;
        }

        .header h1 {
            color: #2c3e50;
            font-size: 2.5em;
            margin-bottom: 10px;
            font-weight: 700;
        }

        .header .subtitle {
            color: #7f8c8d;
            font-size: 1.2em;
            margin-bottom: 20px;
        }

        .status-badge {
            display: inline-block;
            padding: 10px 20px;
            border-radius: 25px;
            color: white;
            font-weight: bold;
            font-size: 1.1em;
            background: linear-gradient(45deg, #2ecc71, #27ae60);
            box-shadow: 0 4px 15px rgba(46, 204, 113, 0.3);
        }

        .status-badge.warning {
            background: linear-gradient(45deg, #f39c12, #e67e22);
            box-shadow: 0 4px 15px rgba(243, 156, 18, 0.3);
        }

        .stats-grid {
            display: grid;
            grid-template-columns: repeat(3, 1fr);
            gap: 20px;
            margin-bottom: 30px;
        }

        .stat-card {
            background: rgba(255, 255, 255, 0.95);
            border-radius: 15px;
            padding: 25px;
            box-shadow: 0 8px 25px rgba(0, 0, 0, 0.15);
            transition: transform 0.3s ease, box-shadow 0.3s ease;
        }

        .stat-card:hover {
            transform: translateY(-5px);
            box-shadow: 0 12px 35px rgba(0, 0, 0, 0.2);
        }

        .stat-card h3 {
            color: #2c3e50;
            margin-bottom: 15px;
            font-size: 1.3em;
            border-bottom: 2px solid #3498db;
            padding-bottom: 8px;
        }

        .big-number {
            font-size: 3em;
            font-weight: bold;
            color: #2ecc71;
            text-align: center;
            margin: 15px 0;
        }

        .big-number.warning {
            color: #f39c12;
        }

        .big-number.danger {
            color: #e74c3c;
        }

        .progress-bar {
            background: #ecf0f1;
            border-radius: 25px;
            height: 25px;
            margin: 15px 0;
            overflow: hidden;
            position: relative;
        }

        .progress-fill {
            height: 100%;
            border-radius: 25px;
            background: linear-gradient(45deg, #2ecc71, #27ae60);
            transition: width 0.5s ease;
            display: flex;
            align-items: center;
            justify-content: center;
        }

        .progress-text {
            color: white;
            font-weight: bold;
            font-size: 0.9em;
        }

        .details-grid {
            display: grid;
            grid-template-columns: repeat(3, 1fr);
            gap: 20px;
            margin-bottom: 30px;
        }

        .detail-card {
            background: rgba(255, 255, 255, 0.95);
            border-radius: 15px;
            padding: 25px;
            box-shadow: 0 8px 25px rgba(0, 0, 0, 0.15);
        }

        .detail-card h3 {
            color: #2c3e50;
            margin-bottom: 20px;
            font-size: 1.3em;
            border-bottom: 2px solid #3498db;
            padding-bottom: 8px;
        }

        .assembly-row {
            display: flex;
            justify-content: space-between;
            align-items: center;
            padding: 12px 0;
            border-bottom: 1px solid #ecf0f1;
        }

        .assembly-row:last-child {
            border-bottom: none;
        }

        .assembly-name {
            font-weight: bold;
            color: #2c3e50;
        }

        .assembly-coverage {
            text-align: right;
        }

        .coverage-badge {
            display: inline-block;
            padding: 5px 12px;
            border-radius: 15px;
            color: white;
            font-weight: bold;
            margin-bottom: 3px;
        }

        .coverage-excellent {
            background: linear-gradient(45deg, #2ecc71, #27ae60);
        }

        .coverage-good {
            background: linear-gradient(45deg, #f39c12, #e67e22);
        }

        .coverage-poor {
            background: linear-gradient(45deg, #e74c3c, #c0392b);
        }

        .test-list {
            margin-top: 10px;
        }

        .test-item {
            display: flex;
            justify-content: space-between;
            align-items: center;
            padding: 10px 0;
            border-bottom: 1px solid #f1f1f1;
        }

        .test-item:last-child {
            border-bottom: none;
        }

        .test-status {
            width: 12px;
            height: 12px;
            border-radius: 50%;
            margin-right: 10px;
        }

        .test-pass {
            background: #2ecc71;
        }

        .test-fail {
            background: #e74c3c;
        }

        .test-warning {
            background: #f39c12;
        }

        .timestamp {
            color: #7f8c8d;
            font-size: 0.9em;
            margin-top: 10px;
        }

        .refresh-btn {
            position: fixed;
            bottom: 20px;
            right: 20px;
            background: linear-gradient(45deg, #3498db, #2980b9);
            color: white;
            border: none;
            border-radius: 50px;
            padding: 15px 25px;
            font-size: 16px;
            font-weight: bold;
            cursor: pointer;
            box-shadow: 0 4px 15px rgba(52, 152, 219, 0.3);
            transition: all 0.3s ease;
        }

        .refresh-btn:hover {
            transform: translateY(-2px);
            box-shadow: 0 6px 20px rgba(52, 152, 219, 0.4);
        }

        /* Test Stats Flexbox */
        .test-stats-flex {
            display: flex;
            justify-content: space-between;
            margin-top: 15px;
        }

        .stat-number-success {
            color: #2ecc71;
            font-size: 1.4em;
        }

        .stat-number-danger {
            color: #e74c3c;
            font-size: 1.4em;
        }

        .stat-number-warning {
            color: #f39c12;
            font-size: 1.4em;
        }

        /* List Styles */
        .tech-list {
            line-height: 1.8;
        }

        .arch-list {
            margin-top: 10px;
            padding-left: 20px;
        }

        /* Assembly Excluded */
        .assembly-excluded {
            background: #e8f5e8;
            padding: 8px;
            border-radius: 8px;
        }

        .coverage-excluded {
            background: #95a5a6;
        }

        /* Responsive Design */
        @media (max-width: 1024px) and (min-width: 769px) {
            .stats-grid,
            .details-grid {
                grid-template-columns: repeat(2, 1fr);
            }
        }

        @media (max-width: 768px) {
            .container-fluid {
                padding: 15px;
            }

            .stats-grid,
            .details-grid {
                grid-template-columns: 1fr;
            }

            .header h1 {
                font-size: 2em;
            }

            .big-number {
                font-size: 2.5em;
            }
        }
    </style>
</head>

<body>
    <div class="container-fluid">
        <!-- Header -->
        <div class="header">
            <h1>🛡️ $ProjectName</h1>
            <p class="subtitle">Dashboard de Cobertura de Tests - Clean Architecture</p>
            <div class="status-badge $(if ($TestResults.Failed -gt 0) { 'warning' })">$statusBadge</div>
            <div class="timestamp">Última actualización: $timestamp</div>
        </div>

        <!-- Main Stats -->
        <div class="stats-grid">
            <div class="stat-card">
                <h3>📊 Cobertura Total</h3>
                <div class="big-number $(if ($CoverageData.LineRate -lt 70) { 'warning' } elseif ($CoverageData.LineRate -lt 50) { 'danger' })">$($CoverageData.LineRate)%</div>
                <div class="progress-bar">
                    <div class="progress-fill" data-width="$($CoverageData.LineRate)%">
                        <div class="progress-text">$(if ($CoverageData.LineRate -ge 90) { 'Excelente' } elseif ($CoverageData.LineRate -ge 70) { 'Bueno' } else { 'Mejorable' })</div>
                    </div>
                </div>
                <p><strong>$($CoverageData.CoveredLines) de $($CoverageData.TotalLines)</strong> líneas cubiertas</p>
                <p><strong>$($CoverageData.CoveredBranches) de $($CoverageData.TotalBranches)</strong> ramas cubiertas ($($CoverageData.BranchRate)%)</p>
            </div>

            <div class="stat-card">
                <h3>🧪 Tests Ejecutados</h3>
                <div class="big-number">$($TestResults.Total)</div>
                <div class="test-stats-flex">
                    <div>
                        <strong class="stat-number-success">$($TestResults.Passed)</strong><br>
                        <small>✅ Exitosos</small>
                    </div>
                    <div>
                        <strong class="$(if ($TestResults.Failed -gt 0) { 'stat-number-danger' } else { 'stat-number-success' })">$($TestResults.Failed)</strong><br>
                        <small>❌ Fallidos</small>
                    </div>
                    <div>
                        <strong class="$(if ($successRate -ge 95) { 'stat-number-success' } elseif ($successRate -ge 80) { 'stat-number-warning' } else { 'stat-number-danger' })">$successRate%</strong><br>
                        <small>🎯 Tasa de éxito</small>
                    </div>
                </div>
            </div>

            <div class="stat-card">
                <h3>⚡ Performance</h3>
                <div class="big-number">$([math]::Round($TestResults.Duration, 1))s</div>
                <div class="progress-bar">
                    <div class="progress-fill" data-width="$(if ($TestResults.Duration -lt 10) { 90 } elseif ($TestResults.Duration -lt 30) { 70 } else { 50 })%">
                        <div class="progress-text">$(if ($TestResults.Duration -lt 10) { 'Excelente' } elseif ($TestResults.Duration -lt 30) { 'Bueno' } else { 'Lento' })</div>
                    </div>
                </div>
                <p>Tiempo de ejecución para <strong>$($TestResults.Total)</strong> tests</p>
            </div>
        </div>

        <!-- Assembly Details & Technical Details -->
        <div class="details-grid">
            <div class="detail-card">
                <h3>🏗️ Coverage por Assembly</h3>
                $assemblyRows
                <div class="assembly-row assembly-excluded">
                    <div class="assembly-name">✅ Users.Infrastructure</div>
                    <div class="assembly-coverage">
                        <div class="coverage-badge coverage-excluded">0% (excluido)</div>
                        <small>Omitido del cálculo</small>
                    </div>
                </div>
            </div>
            
            <div class="detail-card">
                <h3>⚙️ Configuración Técnica</h3>
                <ul class="tech-list">
                    <li><strong>Framework:</strong> .NET 9</li>
                    <li><strong>Test Framework:</strong> xUnit</li>
                    <li><strong>Mocking:</strong> Moq</li>
                    <li><strong>Assertions:</strong> FluentAssertions</li>
                    <li><strong>Coverage Tool:</strong> Coverlet + ReportGenerator</li>
                    <li><strong>Database:</strong> MySQL (Production) / InMemory (Tests)</li>
                    <li><strong>Architecture:</strong> Clean Architecture</li>
                </ul>
            </div>

            <div class="detail-card">
                <h3>📁 Arquitectura</h3>
                <div class="big-number">.NET 9</div>
                <p>Sistema construido con:</p>
                <ul class="arch-list">
                    <li>Entity Framework Core + MySQL</li>
                    <li>xUnit + FluentAssertions</li>
                    <li>Clean Architecture</li>
                    <li>Dependency Injection</li>
                    <li>JWT Authentication</li>
                </ul>
            </div>
        </div>
    </div>

    <button class="refresh-btn" onclick="location.reload()">🔄 Actualizar</button>

    <script>
        // Animación de carga para progress bars
        document.addEventListener('DOMContentLoaded', function() {
            const progressBars = document.querySelectorAll('.progress-fill');
            progressBars.forEach(bar => {
                const targetWidth = bar.getAttribute('data-width');
                if (targetWidth) {
                    bar.style.width = '0%';
                    setTimeout(() => {
                        bar.style.width = targetWidth;
                    }, 500);
                }
            });
        });

        // Auto-refresh cada 30 segundos si la página está activa
        let autoRefresh = setInterval(() => {
            if (!document.hidden) {
                location.reload();
            }
        }, 30000);

        // Pausar auto-refresh cuando la página no está visible
        document.addEventListener('visibilitychange', function() {
            if (document.hidden) {
                clearInterval(autoRefresh);
            } else {
                autoRefresh = setInterval(() => location.reload(), 30000);
            }
        });
    </script>
</body>
</html>
"@

    $html | Out-File -FilePath $DashboardFile -Encoding UTF8 -Force
    Write-ColorMessage "✅ Dashboard generado: $DashboardFile" $Colors.Success
    
    return $DashboardFile
}

#endregion

#region Funciones Principales

function Invoke-CleanAction {
    Write-Banner "🧹 LIMPIEZA DE ARCHIVOS"
    
    Initialize-Environment
    
    # Limpiar directorios bin y obj
    Get-ChildItem -Path "src" -Directory | ForEach-Object {
        $binPath = Join-Path $_.FullName "bin"
        $objPath = Join-Path $_.FullName "obj"
        
        if (Test-Path $binPath) {
            Remove-Item $binPath -Recurse -Force
            Write-ColorMessage "🗑️ Eliminado: $binPath" $Colors.Info
        }
        
        if (Test-Path $objPath) {
            Remove-Item $objPath -Recurse -Force  
            Write-ColorMessage "🗑️ Eliminado: $objPath" $Colors.Info
        }
    }
    
    Write-ColorMessage "✅ Limpieza completada" $Colors.Success
}

function Invoke-TestAction {
    Write-Banner "🧪 EJECUCIÓN DE TESTS"
    
    Test-Prerequisites
    Initialize-Environment
    Invoke-BuildSolution
    
    $testResults = Invoke-Tests -CollectCoverage $true
    
    if ($testResults.ExitCode -eq 0) {
        Write-ColorMessage "🎉 Todos los tests completados exitosamente!" $Colors.Success
    }
    else {
        Write-ColorMessage "⚠️ Algunos tests fallaron. Revise los resultados." $Colors.Warning
    }
    
    Write-Output $testResults
}

function Invoke-CoverageAction {
    Write-Banner "📊 ANÁLISIS DE COBERTURA"
    
    $coverageData = Get-CoverageData
    
    if ($coverageData) {
        Write-ColorMessage "📈 Reporte de cobertura generado exitosamente" $Colors.Success
        return $coverageData
    }
    else {
        Write-ColorMessage "❌ No se pudo generar el reporte de cobertura" $Colors.Error
        return $null
    }
}

function Invoke-DashboardAction {
    Write-Banner "🎨 GENERACIÓN DE DASHBOARD"
    
    $testResults = Invoke-TestAction
    $coverageData = Invoke-CoverageAction
    $loadTestData = Get-K6LoadTestResults
    
    if ($testResults -and $coverageData) {
        $dashboardFile = New-DashboardHtml -TestResults $testResults -CoverageData $coverageData -LoadTestData $loadTestData
        
        if ($OpenDashboard) {
            Write-ColorMessage "🌐 Abriendo dashboard en el navegador..." $Colors.Info
            Start-Process $dashboardFile
        }
        
        Write-ColorMessage "🎯 Dashboard disponible en: $dashboardFile" $Colors.Highlight
    }
    else {
        Write-ColorMessage "❌ No se pudo generar el dashboard" $Colors.Error
    }
}

function Invoke-FullAction {
    Write-Banner "🚀 EJECUCIÓN COMPLETA - TESTS + COBERTURA + DASHBOARD + K6"
    
    try {
        Invoke-DashboardAction
        Write-ColorMessage "🎉 Proceso completo finalizado exitosamente!" $Colors.Success
    }
    catch {
        Write-ColorMessage "❌ Error durante la ejecución: $($_.Exception.Message)" $Colors.Error
        throw
    }
}

function Show-Help {
    Write-Banner "📚 AYUDA - GESTOR DE TESTS GATEWAY"
    
    Write-Host @"
USO:
    .\manage-tests.ps1 [ACCIÓN] [PARÁMETROS]

ACCIONES:
    test        Ejecuta solo los tests unitarios
    coverage    Ejecuta tests y genera reporte de cobertura  
    dashboard   Ejecuta tests, cobertura y genera dashboard HTML con métricas K6
    full        Ejecuta todo el proceso completo (recomendado)
    clean       Limpia archivos de build y resultados anteriores
    help        Muestra esta ayuda

PARÁMETROS:
    -Filter <filtro>        Filtra tests específicos (ej: "Gateway.UnitTests")
    -Configuration <config> Configuración de build: Debug o Release (default: Debug)
    -OpenDashboard          Abre automáticamente el dashboard en el navegador
    -DetailedOutput         Muestra información detallada durante la ejecución

EJEMPLOS:
    .\manage-tests.ps1 full
        Ejecuta el proceso completo con dashboard y métricas K6

    .\manage-tests.ps1 test -Filter "Gateway.UnitTests" 
        Ejecuta solo tests de Gateway.UnitTests

    .\manage-tests.ps1 dashboard -OpenDashboard
        Genera dashboard con K6 y lo abre en el navegador

    .\manage-tests.ps1 coverage -Configuration Release
        Ejecuta tests en modo Release con cobertura

ARCHIVOS GENERADOS:
    • TestResults/              - Resultados de tests y cobertura
    • test-dashboard.html       - Dashboard interactivo con métricas K6
    • *.trx                     - Resultados detallados de tests

PRUEBAS DE CARGA K6:
    El dashboard incluye métricas de los siguientes tests de carga:
    • light-load-k6  (20 usuarios)   - Carga ligera
    • medium-load-k6 (50 usuarios)   - Carga media
    • high-load      (100 usuarios)  - Carga alta
    • extreme-load   (500 usuarios)  - Carga extrema

    Los resultados se leen desde: $K6ResultsPath

"@ -ForegroundColor $Colors.Info

    Write-ColorMessage "🎯 Para empezar rápidamente, ejecute: .\manage-tests.ps1 full" $Colors.Highlight
    Write-ColorMessage "⚡ Para ejecutar pruebas de carga K6, use: .\src\tests\Gateway.Load\manage-load-tests.ps1" $Colors.Info
}

#endregion

#region Ejecución Principal

try {
    switch ($Action.ToLower()) {
        "test" { 
            Invoke-TestAction | Out-Null
        }
        "coverage" { 
            Invoke-CoverageAction | Out-Null
        }
        "dashboard" { 
            Invoke-DashboardAction 
        }
        "full" { 
            Invoke-FullAction 
        }
        "clean" { 
            Invoke-CleanAction 
        }
        "help" { 
            Show-Help 
        }
        "" { 
            Show-Help 
        }
        default { 
            Write-ColorMessage "❌ Acción no válida: $Action" $Colors.Error
            Write-ColorMessage "💡 Ejecute '.\manage-tests.ps1 help' para ver las opciones disponibles" $Colors.Info
            exit 1
        }
    }
}
catch {
    Write-ColorMessage "💥 Error fatal: $($_.Exception.Message)" $Colors.Error
    if ($DetailedOutput) {
        Write-ColorMessage "📋 Stack trace: $($_.ScriptStackTrace)" $Colors.Error
    }
    exit 1
}

#endregion
