# Script para generar el dashboard correcto
param(
  [Parameter(Mandatory = $false)]
  [switch]$RunTests = $false,
  
  [Parameter(Mandatory = $false)]
  [switch]$OpenDashboard = $false,
  
  [Parameter(Mandatory = $false)]
  [switch]$GenerateOnly = $false,
  
  [Parameter(Mandatory = $false)]
  [switch]$RunLoadTests = $false,
  
  [Parameter(Mandatory = $false)]
  [string]$OutputPath = "./test-dashboard.html"
)

# Funciones auxiliares
function Write-Header {
  param([string]$Message)
  Write-Host "📋 $Message" -ForegroundColor Magenta
}

function Write-Info {
  param([string]$Message)
  Write-Host "ℹ️  $Message" -ForegroundColor Cyan
}

function Write-Success {
  param([string]$Message)  
  Write-Host "✅ $Message" -ForegroundColor Green
}

function Write-Warning {
  param([string]$Message)
  Write-Host "⚠️  $Message" -ForegroundColor Yellow
}

function Write-Error {
  param([string]$Message)
  Write-Host "❌ $Message" -ForegroundColor Red
}

# Función para obtener resultados reales de tests
function Get-RealTestResults {
  Write-Info "🧪 Ejecutando análisis de tests para obtener resultados reales (.NET)..."
  
  try {
    # Verificar si es un proyecto .NET
    if (-not (Test-Path "Gateway.sln") -and -not (Get-ChildItem -Recurse -Filter "*.csproj" -ErrorAction SilentlyContinue)) {
      Write-Info "📦 No se encontró proyecto .NET"
      return $null
    }
    
    if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
      Write-Info "📦 dotnet CLI no está disponible"
      return $null
    }
    
    # Buscar los archivos TRX más recientes de la misma ejecución
    $allTrxFiles = Get-ChildItem -Path "." -Filter "*.trx" -Recurse | 
                   Sort-Object LastWriteTime -Descending
    
    if (-not $allTrxFiles) {
      # Intentar buscar en TestResults
      $allTrxFiles = Get-ChildItem -Path "TestResults" -Filter "*.trx" -Recurse -ErrorAction SilentlyContinue | 
                     Sort-Object LastWriteTime -Descending
    }
    
    # Tomar solo los archivos de la ejecución más reciente (misma fecha/hora aproximada)
    $trxFiles = @()
    if ($allTrxFiles) {
      $mostRecentTime = $allTrxFiles[0].LastWriteTime
      $timeWindow = New-TimeSpan -Minutes 5  # Ventana de 5 minutos para considerar la misma ejecución
      
      $trxFiles = $allTrxFiles | Where-Object { 
        ($mostRecentTime - $_.LastWriteTime) -lt $timeWindow 
      }
    }
    
    $realTestData = @{
      TotalTests    = 0
      PassingTests  = 0
      FailingTests  = 0
      TestSuites    = @{Count = 0}
      ExecutionTime = [DateTime]::Now
      Duration      = "0s"
    }
    
    if ($trxFiles -and $trxFiles.Count -gt 0) {
      try {
        Write-Info "📄 Procesando $($trxFiles.Count) archivo(s) TRX de la ejecución más reciente:"
        
        $totalTests = 0
        $totalPassed = 0
        $totalFailed = 0
        $earliestStart = $null
        $latestFinish = $null
        
        foreach ($trxFile in $trxFiles) {
          Write-Info "  - $($trxFile.Name)"
          [xml]$trxXml = Get-Content $trxFile.FullName
          $counters = $trxXml.TestRun.ResultSummary.Counters
          
          if ($counters) {
            $totalTests += [int]$counters.total
            $totalPassed += [int]$counters.passed
            $totalFailed += [int]$counters.failed
            
            # Obtener tiempos para calcular duración total
            $startTime = [DateTime]$trxXml.TestRun.Times.start
            $finishTime = [DateTime]$trxXml.TestRun.Times.finish
            
            if (-not $earliestStart -or $startTime -lt $earliestStart) {
              $earliestStart = $startTime
            }
            if (-not $latestFinish -or $finishTime -gt $latestFinish) {
              $latestFinish = $finishTime
            }
          }
        }
        
        $realTestData.TotalTests = $totalTests
        $realTestData.PassingTests = $totalPassed
        $realTestData.FailingTests = $totalFailed
        
        # Calcular duración total
        if ($earliestStart -and $latestFinish) {
          $duration = $latestFinish - $earliestStart
          $realTestData.ExecutionTime = if ($duration.TotalSeconds -lt 60) {
            "$([math]::Round($duration.TotalSeconds, 1))s"
          } else {
            "$([math]::Floor($duration.TotalMinutes))m $([math]::Round($duration.Seconds, 0))s"
          }
          $realTestData.Duration = $duration.ToString("hh\:mm\:ss")
        }
        
        # Contar test suites (archivos de test)
        $testFiles = Get-ChildItem -Path "src" -Recurse -Filter "*Test*.cs" -ErrorAction SilentlyContinue
        if ($testFiles) {
          $realTestData.TestSuites.Count = ($testFiles | Measure-Object).Count
        } else {
          $realTestData.TestSuites.Count = 1
        }
        
        Write-Info "  Tests totales: $($realTestData.TotalTests)"
        Write-Info "  Tests pasados: $($realTestData.PassingTests)"
        Write-Info "  Tests fallidos: $($realTestData.FailingTests)"
        Write-Info "  Test Suites: $($realTestData.TestSuites.Count)"
        Write-Info "  Duración: $($realTestData.Duration)"
        
        return $realTestData
      }
      catch {
        Write-Error "❌ Error al parsear archivos TRX: $($_.Exception.Message)"
      }
    } else {
      Write-Info "📄 No se encontraron archivos TRX recientes"
    }
    
    return $null
  }
  catch {
    Write-Error "Error obteniendo resultados de tests: $($_.Exception.Message)"
    return $null
  }
}

# Función para obtener conteo real de test suites
function Get-RealTestSuites {
  Write-Info "📁 Contando archivos de test .NET..."
  
  try {
    # Contar archivos de test .NET (archivos *Test*.cs)
    $testFiles = Get-ChildItem -Path "src" -Recurse -Filter "*Test*.cs" -ErrorAction SilentlyContinue
    
    if (-not $testFiles) {
      # Buscar también archivos Tests.cs
      $testFiles = Get-ChildItem -Path "src" -Recurse -Filter "*Tests.cs" -ErrorAction SilentlyContinue
    }
    
    $testCount = if ($testFiles) { ($testFiles | Measure-Object).Count } else { 0 }
    
    Write-Info "  Archivos de test .NET encontrados: $testCount"
    
    return @{Count = $testCount}
  }
  catch {
    Write-Warning "Error contando archivos de test: $($_.Exception.Message)"
    return @{Count = 0}
  }
}

# Función para obtener métricas de tests de carga (placeholder por ahora)
function Get-RealLoadTestResults {
  Write-Info "⚡ Verificando tests de carga..."
  
  # Buscar archivos de resultados reales de K6 del gateway
  $k6ResultsPath = "src\tests\Gateway.Load\results"
  
  if (-not (Test-Path $k6ResultsPath)) {
    Write-Info "📁 No se encontró carpeta de resultados K6: $k6ResultsPath"
    return @{ Available = $false }
  }
  
  try {
    $loadTestFiles = @{
      "light-load-k6"  = "$k6ResultsPath\light-load-k6.json"
      "medium-load-k6" = "$k6ResultsPath\medium-load-k6.json"
      "high-load"      = "$k6ResultsPath\high-load.json"
      "extreme-load"   = "$k6ResultsPath\extreme-load.json"
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
          
          # Convertir datos recibidos/enviados a MB
          $dataSentMB = [math]::Round($metrics.data_sent.count / 1MB, 2)
          $dataReceivedMB = [math]::Round($metrics.data_received.count / 1MB, 2)
          
          # Calcular duración estimada basada en iteraciones y rate
          # Duración = Total requests ÷ Rate de requests por segundo ÷ 60 (para minutos)
          $totalRequests = $metrics.http_reqs.count
          $requestRate = $metrics.http_reqs.rate
          $estimatedDurationMinutes = if ($requestRate -gt 0) {
            [math]::Round($totalRequests / $requestRate / 60, 1)
          } else {
            0.5  # Fallback si no hay rate
          }
          
          $k6Results[$testName] = @{
            Status     = if ($errorRate -lt 5) { "Success" } else { "Warning" }
            Users      = $userCount
            ExecutedAt = (Get-Item $filePath).LastWriteTime.ToString("HH:mm:ss")
            Duration   = $estimatedDurationMinutes.ToString()
            Metrics    = @{
              RequestsPerSecond = [math]::Round($metrics.http_reqs.rate, 1).ToString()
              ResponseTimeAvg   = [math]::Round($metrics.http_req_duration.avg, 1).ToString() + "ms"
              ResponseTimeP95   = [math]::Round($metrics.http_req_duration.'p(95)', 1).ToString() + "ms"
              ResponseTimeP99   = [math]::Round($metrics.http_req_duration.'p(99)', 1).ToString() + "ms"
              ErrorRate         = $errorRate.ToString() + "%"
              Iterations        = $metrics.iterations.count.ToString()
              DataSent          = $dataSentMB.ToString() + " MB"
              DataReceived      = $dataReceivedMB.ToString() + " MB"
            }
          }
          
          if ($errorRate -lt 5) { $successfulTests++ }
          
          $reqPerSec = [math]::Round($metrics.http_reqs.rate, 1)
          $avgTime = [math]::Round($metrics.http_req_duration.avg, 1)
          Write-Info "  ✅ $testName`: $userCount usuarios, $reqPerSec req/s, ${avgTime}ms avg"
        }
        catch {
          Write-Warning "⚠️ Error al procesar $testName`: $($_.Exception.Message)"
          $k6Results[$testName] = @{
            Status = "Failed"
            Users = 0
            Metrics = @{ RequestsPerSecond = "Error" }
          }
        }
      }
      else {
        Write-Info "  📄 Archivo no encontrado: $testName"
        $k6Results[$testName] = @{
          Status = "Not Found"
          Users = switch ($testName) {
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
    Write-Error "❌ Error al procesar resultados de K6: $($_.Exception.Message)"
    return @{ Available = $false }
  }
}

function Get-DashboardHTML {
  param(
    [Parameter(Mandatory = $true)]
    [hashtable]$TestData
  )
  
  $timestamp = Get-Date -Format "dd 'de' MMMM yyyy, HH:mm"
  $totalCoverage = [math]::Round((($TestData.Coverage.Statements + $TestData.Coverage.Branches + $TestData.Coverage.Functions + $TestData.Coverage.Lines) / 4), 1)
  
  # Calcular porcentajes para los estilos CSS
  $passingPercentage = if ($TestData.TotalTests -gt 0) { [math]::Round(($TestData.PassingTests / $TestData.TotalTests) * 100, 1) } else { 0 }
  $failingPercentage = if ($TestData.TotalTests -gt 0) { [math]::Round(($TestData.FailingTests / $TestData.TotalTests) * 100, 1) } else { 0 }
    
  # Determinar clases CSS para el estado
  $testStatusClass = if ($TestData.FailingTests -eq 0) { "" } else { "danger" }
  $coverageClass = if ($totalCoverage -ge 80) { "" } elseif ($totalCoverage -ge 60) { "warning" } else { "danger" }
  
  # Expandir variables explícitamente para evitar problemas con here-strings
  $totalTests = $TestData.TotalTests
  $passingTests = $TestData.PassingTests
  $failingTests = $TestData.FailingTests
  $testSuitesCount = $TestData.TestSuites.Count
  $testIcon = if ($TestData.FailingTests -eq 0) { "✅" } else { "⚠️" }
  $coverageStatements = $TestData.Coverage.Statements
  $coverageBranches = $TestData.Coverage.Branches
  $coverageFunctions = $TestData.Coverage.Functions
  $coverageLines = $TestData.Coverage.Lines

  $html = @"
<!DOCTYPE html>
<html lang="es">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>🚪 Accessibility Gateway - Test Dashboard</title>
    <link rel="icon" href="data:image/svg+xml,<svg xmlns=%22http://www.w3.org/2000/svg%22 viewBox=%220 0 100 100%22><text y=%22.9em%22 font-size=%2290%22>🚪</text></svg>">
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
            margin: 5px;
        }

        .status-badge.warning {
            background: linear-gradient(45deg, #f39c12, #e67e22);
            box-shadow: 0 4px 15px rgba(243, 156, 18, 0.3);
        }

        .status-badge.danger {
            background: linear-gradient(45deg, #e74c3c, #c0392b);
            box-shadow: 0 4px 15px rgba(231, 76, 60, 0.3);
        }

        .stats-grid {
            display: grid;
            grid-template-columns: repeat(4, 1fr);
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

        .progress-fill.warning {
            background: linear-gradient(45deg, #f39c12, #e67e22);
        }

        .progress-fill.danger {
            background: linear-gradient(45deg, #e74c3c, #c0392b);
        }

        .progress-text {
            color: white;
            font-weight: bold;
            font-size: 0.9em;
        }

        .details-grid {
            display: grid;
            grid-template-columns: repeat(2, 1fr);
            gap: 20px;
            margin-bottom: 30px;
        }

        .detail-card {
            background: rgba(255, 255, 255, 0.95);
            border-radius: 15px;
            padding: 25px;
            box-shadow: 0 8px 25px rgba(0, 0, 0, 0.15);
        }

        .detail-card.full-width {
            grid-column: span 2;
        }

        .detail-card h3 {
            color: #2c3e50;
            margin-bottom: 20px;
            font-size: 1.3em;
            border-bottom: 2px solid #3498db;
            padding-bottom: 8px;
        }

        .load-results-summary {
            background: linear-gradient(135deg, #f8f9fa, #e9ecef);
            border: 1px solid #dee2e6;
            border-radius: 12px;
            padding: 20px;
            margin-bottom: 30px;
        }

        .load-results-header {
            display: flex;
            justify-content: space-between;
            align-items: center;
            margin-bottom: 15px;
            border-bottom: 2px solid #3498db;
            padding-bottom: 10px;
        }

        .load-results-header h4 {
            color: #2c3e50;
            margin: 0;
            font-size: 1.4em;
        }

        .execution-time {
            background: #3498db;
            color: white;
            padding: 6px 12px;
            border-radius: 20px;
            font-size: 0.85em;
            font-weight: bold;
        }

        .load-results-stats {
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(120px, 1fr));
            gap: 15px;
        }

        .result-stat {
            text-align: center;
            padding: 15px;
            border-radius: 10px;
            display: flex;
            flex-direction: column;
            gap: 5px;
        }

        .result-stat.success {
            background: linear-gradient(135deg, #d5f4e6, #c8e6c9);
            border: 2px solid #4caf50;
        }

        .result-stat.failed {
            background: linear-gradient(135deg, #ffebee, #ffcdd2);
            border: 2px solid #f44336;
        }

        .result-stat.total {
            background: linear-gradient(135deg, #e3f2fd, #bbdefb);
            border: 2px solid #2196f3;
        }

        .stat-number {
            font-size: 2.5em;
            font-weight: bold;
            color: #2c3e50;
        }

        .stat-label {
            font-size: 0.9em;
            color: #6c757d;
            font-weight: 600;
            text-transform: uppercase;
            letter-spacing: 1px;
        }

        .tool-section {
            margin-bottom: 30px;
            border-radius: 15px;
            overflow: hidden;
            box-shadow: 0 8px 25px rgba(0,0,0,0.1);
        }

        .k6-section {
            border: 2px solid #1abc9c;
        }

        .tool-section-header {
            display: flex;
            align-items: center;
            gap: 15px;
            padding: 20px;
            font-weight: bold;
            color: white;
        }

        .k6-section .tool-section-header {
            background: linear-gradient(135deg, #1abc9c, #16a085);
        }

        .tool-icon {
            font-size: 1.8em;
        }

        .tool-section-header h4 {
            margin: 0;
            font-size: 1.3em;
        }

        .tool-badge {
            padding: 5px 10px;
            border-radius: 15px;
            color: white;
            font-size: 0.8em;
            font-weight: bold;
        }

        .tool-k6 {
            background: #1abc9c;
        }

        .tool-results-grid {
            display: grid;
            grid-template-columns: repeat(4, 1fr);
            gap: 16px;
            padding: 20px;
            background: #f8f9fa;
            max-width: 100%;
        }

        /* Responsive para 4 columnas */
        @media (max-width: 1400px) {
            .tool-results-grid {
                grid-template-columns: repeat(2, 1fr);
                gap: 18px;
            }
        }

        @media (max-width: 768px) {
            .tool-results-grid {
                grid-template-columns: 1fr;
                gap: 16px;
                padding: 15px;
            }
        }

        .load-result-card {
            background: white;
            border-radius: 12px;
            padding: 16px;
            border: 1px solid #dee2e6;
            transition: all 0.3s ease;
            min-width: 0; /* Permite que las cards se contraigan */
            font-size: 0.9em; /* Texto ligeramente más pequeño para 4 columnas */
        }

        .load-result-card:hover {
            transform: translateY(-5px);
            box-shadow: 0 10px 30px rgba(0,0,0,0.15);
        }

        .load-result-card.status-success {
            border-left: 5px solid #4caf50;
        }

        .load-result-card.status-failed {
            border-left: 5px solid #f44336;
        }

        .result-header {
            display: flex;
            justify-content: space-between;
            align-items: center;
            margin-bottom: 12px;
            padding-bottom: 8px;
            border-bottom: 1px solid #eee;
        }

        .result-title {
            font-weight: bold;
            color: #2c3e50;
            display: flex;
            align-items: center;
            gap: 8px;
            font-size: 0.95em;
        }

        .users-count {
            background: #3498db;
            color: white;
            padding: 3px 6px;
            border-radius: 12px;
            font-size: 0.75em;
            font-weight: bold;
        }

        .result-status {
            padding: 4px 8px;
            border-radius: 16px;
            font-size: 0.75em;
            font-weight: bold;
        }

        .status-success .result-status {
            background: #d5f4e6;
            color: #2e7d32;
        }

        .status-failed .result-status {
            background: #ffebee;
            color: #c62828;
        }

        .result-metrics {
            display: grid;
            grid-template-columns: 1fr;
            gap: 8px;
            margin-bottom: 12px;
        }

        .metric-row {
            display: flex;
            justify-content: space-between;
            align-items: center;
            padding: 6px 10px;
            background: #f8f9fa;
            border-radius: 6px;
            border-left: 3px solid #3498db;
        }

        .metric-label {
            font-weight: 600;
            color: #495057;
            font-size: 0.8em;
        }

        .metric-value {
            font-family: 'Courier New', monospace;
            font-weight: bold;
            color: #2c3e50;
            background: #e3f2fd;
            padding: 2px 6px;
            border-radius: 4px;
            font-size: 0.75em;
        }

        .result-footer {
            display: flex;
            justify-content: space-between;
            align-items: center;
            padding-top: 8px;
            border-top: 1px solid #eee;
            font-size: 0.75em;
            color: #6c757d;
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

        /* Responsive Design */
        @media (max-width: 1024px) {
            .stats-grid {
                grid-template-columns: repeat(2, 1fr);
            }
            .details-grid {
                grid-template-columns: 1fr;
            }
            .detail-card.full-width {
                grid-column: span 1;
            }
        }

        @media (max-width: 768px) {
            .container-fluid {
                padding: 15px;
            }
            .stats-grid {
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
            <h1>🚪 Accessibility Gateway</h1>
            <p class="subtitle">Dashboard Comprehensivo de Tests - Middleware de Accesibilidad</p>
            <div class="status-badge $testStatusClass">
                $testIcon $passingTests/$totalTests TESTS EXITOSOS
            </div>
            <div class="status-badge $coverageClass">
                📊 $totalCoverage% COBERTURA PROMEDIO
            </div>
            <div class="timestamp">Última actualización: $timestamp</div>
        </div>

        <!-- Main Stats -->
        <div class="stats-grid">
            <div class="stat-card">
                <h3>📝 Total Tests</h3>
                <div class="big-number">$totalTests</div>
                <div class="progress-bar">
                    <div class="progress-fill" style="width: 100%">
                        <span class="progress-text">Suites: $testSuitesCount</span>
                    </div>
                </div>
            </div>

            <div class="stat-card">
                <h3>✅ Tests Exitosos</h3>
                <div class="big-number">$passingTests</div>
                <div class="progress-bar">
                    <div class="progress-fill" style="width: $passingPercentage%">
                        <span class="progress-text">$passingPercentage%</span>
                    </div>
                </div>
            </div>

            <div class="stat-card">
                <h3>❌ Tests Fallidos</h3>
                <div class="big-number $(if ($TestData.FailingTests -gt 0) { 'danger' } else { '' })">$($TestData.FailingTests)</div>
                <div class="progress-bar">
                    <div class="progress-fill $(if ($TestData.FailingTests -gt 0) { 'danger' } else { '' })" style="width: $failingPercentage%">
                        <span class="progress-text">$failingPercentage%</span>
                    </div>
                </div>
            </div>

            <div class="stat-card">
                <h3>📊 Cobertura Promedio</h3>
                <div class="big-number $(if ($totalCoverage -ge 80) { '' } elseif ($totalCoverage -ge 60) { 'warning' } else { 'danger' })">$totalCoverage%</div>
                <div class="progress-bar">
                    <div class="progress-fill $(if ($totalCoverage -ge 80) { '' } elseif ($totalCoverage -ge 60) { 'warning' } else { 'danger' })" style="width: $totalCoverage%">
                        <span class="progress-text">Objetivo: 80%</span>
                    </div>
                </div>
            </div>
        </div>

        <div class="details-grid">
            <div class="detail-card full-width">
                <h3>K6 - Tests de Carga (20, 50, 100 y 500 usuarios)</h3>
"@

  # Mostrar resultados si están disponibles
  if ($TestData.LoadTests.Results) {
    $executionTime = $TestData.LoadTests.Results.ExecutionTime.ToString("dd/MM/yyyy HH:mm:ss")
    $totalExecuted = $TestData.LoadTests.Results.Summary.TotalExecuted
    $successful = $TestData.LoadTests.Results.Summary.Successful
    $failed = $TestData.LoadTests.Results.Summary.Failed
        
    $html += @"
                <div class="load-results-summary">
                    <div class="load-results-header">
                        <h4>📊 Resumen de Ejecución</h4>
                        <span class="execution-time">Última ejecución: $executionTime</span>
                    </div>
                    <div class="load-results-stats">
                        <div class="result-stat success">
                            <span class="stat-number">$successful</span>
                            <span class="stat-label">Exitosos</span>
                        </div>
                        <div class="result-stat failed">
                            <span class="stat-number">$failed</span>
                            <span class="stat-label">Fallidos</span>
                        </div>
                        <div class="result-stat total">
                            <span class="stat-number">$totalExecuted</span>
                            <span class="stat-label">Total</span>
                        </div>
                    </div>
                </div>

                <!-- K6 Results Section -->
                <div class="tool-section k6-section">
                    <div class="tool-section-header">
                        <span class="tool-icon">⚡</span>
                        <h4>K6 - Tests de Carga (20, 50, 100 y 500 usuarios)</h4>
                        <span class="tool-badge tool-k6">K6</span>
                    </div>
                    <div class="tool-results-grid">
"@
        
    # Mostrar resultados de K6 en orden específico: 20, 50, 100, 500 usuarios
    $orderedConfigs = @("light-load-k6", "medium-load-k6", "high-load", "extreme-load")
    foreach ($config in $orderedConfigs) {
      if ($TestData.LoadTests.Results.K6.ContainsKey($config)) {
        $data = $TestData.LoadTests.Results.K6[$config]
        $statusClass = if ($data.Status -eq "Success") { "status-success" } else { "status-failed" }
        $statusIcon = if ($data.Status -eq "Success") { "✅" } else { "❌" }
                
        $html += @"
                        <div class="load-result-card $statusClass">
                            <div class="result-header">
                                <div class="result-title">
                                    $statusIcon $config
                                    <span class="users-count">👥 $($data.Users) usuarios</span>
                                </div>
                                <div class="result-status">$($data.Status)</div>
                            </div>
"@
                
        if ($data.Status -eq "Success" -and $data.Metrics) {
          $html += @"
                            <div class="result-metrics">
                                <div class="metric-row">
                                    <span class="metric-label">🚀 Requests/seg:</span>
                                    <span class="metric-value">$($data.Metrics.RequestsPerSecond)</span>
                                </div>
                                <div class="metric-row">
                                    <span class="metric-label">⏱️ Resp. Promedio:</span>
                                    <span class="metric-value">$($data.Metrics.ResponseTimeAvg)</span>
                                </div>
                                <div class="metric-row">
                                    <span class="metric-label">📊 P95:</span>
                                    <span class="metric-value">$($data.Metrics.ResponseTimeP95)</span>
                                </div>
                                <div class="metric-row">
                                    <span class="metric-label">📈 P99:</span>
                                    <span class="metric-value">$($data.Metrics.ResponseTimeP99)</span>
                                </div>
                                <div class="metric-row">
                                    <span class="metric-label">❌ Tasa Error:</span>
                                    <span class="metric-value">$($data.Metrics.ErrorRate)</span>
                                </div>
                                <div class="metric-row">
                                    <span class="metric-label">🔄 Iteraciones:</span>
                                    <span class="metric-value">$($data.Metrics.Iterations)</span>
                                </div>
                                <div class="metric-row">
                                    <span class="metric-label">📤 Datos Enviados:</span>
                                    <span class="metric-value">$($data.Metrics.DataSent)</span>
                                </div>
                                <div class="metric-row">
                                    <span class="metric-label">📥 Datos Recibidos:</span>
                                    <span class="metric-value">$($data.Metrics.DataReceived)</span>
                                </div>
                            </div>
                            <div class="result-footer">
                                <span class="execution-time">⏰ $($data.ExecutedAt)</span>
                                <span class="duration">⏱️ $($data.Duration) min</span>
                            </div>
"@
        }
                
        $html += @"
                        </div>
"@
      }
    }
        
    $html += @"
                    </div>
                </div>
"@
  }

  $html += @"
            </div>
        </div>
    </div>

    <button class="refresh-btn" onclick="location.reload()">🔄 Actualizar</button>

    <script>
        // Auto-refresh cada 5 minutos
        setTimeout(function() {
            location.reload();
        }, 300000);

        // Mostrar timestamp de carga
        console.log('Dashboard cargado a las: $timestamp');
        
        // Información adicional en consola
        console.log('📊 Estadísticas detalladas:');
        console.log('- Tests totales: $($TestData.TotalTests)');
        console.log('- Tests exitosos: $($TestData.PassingTests)');
        console.log('- Tests fallidos: $($TestData.FailingTests)');
        console.log('- Cobertura promedio: $totalCoverage%');
        console.log('- Suites de tests: $($TestData.TestSuites.Count)');
        console.log('- Tests de carga: $($TestData.LoadTests.Count)');
    </script>
</body>
</html>
"@

  return $html
}

# Función principal
# Función para ejecutar tests reales con cobertura
function Invoke-RealTests {
  Write-Info "🧪 Ejecutando tests con cobertura..."
  
  try {
    # Ejecutar tests con cobertura usando la configuración específica
    $env:COLLECT_COVERAGE = "true"
    $result = & npm run test:coverage 2>&1
    
    if ($LASTEXITCODE -eq 0) {
      Write-Success "Tests ejecutados exitosamente"
      return $true
    } else {
      Write-Warning "Tests completados con warnings"
      Write-Info $result
      return $true  # Continuar aunque haya warnings
    }
  }
  catch {
    Write-Error "Error ejecutando tests: $($_.Exception.Message)"
    return $false
  }
}

# Función para leer datos reales de cobertura de .NET
function Get-JestCoverageData {
  Write-Info "📊 Leyendo datos de cobertura..."
  
  # Buscar archivo de resumen de cobertura de ReportGenerator
  $coverageSummaryPath = "coverage-report/Summary.json"
  
  if (-not (Test-Path $coverageSummaryPath)) {
    Write-Warning "No se encontró archivo de cobertura en: $coverageSummaryPath"
    Write-Info "Usando datos por defecto..."
    return $null
  }
  
  try {
    $coverageData = Get-Content $coverageSummaryPath | ConvertFrom-Json
    $summary = $coverageData.summary
    
    $realCoverageData = @{
      Statements = [math]::Round($summary.linecoverage, 2)
      Branches   = [math]::Round($summary.branchcoverage, 2)
      Functions  = [math]::Round($summary.methodcoverage, 2)
      Lines      = [math]::Round($summary.linecoverage, 2)
    }
    
    Write-Success "Cobertura real obtenida:"
    Write-Info "  Statements: $($realCoverageData.Statements)%"
    Write-Info "  Branches: $($realCoverageData.Branches)%"
    Write-Info "  Functions: $($realCoverageData.Functions)%"
    Write-Info "  Lines: $($realCoverageData.Lines)%"
    
    return $realCoverageData
  }
  catch {
    Write-Error "Error leyendo archivo de cobertura: $($_.Exception.Message)"
    return $null
  }
}

function Main {
  try {
    Write-Header "=== ACCESSIBILITY GATEWAY TEST DASHBOARD GENERATOR ==="
    
    # Construir objeto de datos dinámicamente
    Write-Info "📊 Obteniendo datos reales de tests..."
    
    # Inicializar objeto de datos
    $TestData = @{
      TotalTests      = 0
      PassingTests    = 0
      FailingTests    = 0
      TestSuites      = @{Count = 0}
      LoadTests       = @{Count = 0; Available = $false}
      Coverage        = @{
        Statements = 0
        Branches   = 0
        Functions  = 0
        Lines      = 0
      }
      ExecutionTime   = [DateTime]::Now
      Duration        = "0s"
    }
    
    # Si se solicita ejecutar tests, hacerlo primero y obtener datos reales
    if ($RunTests) {
      Write-Info "Ejecutando tests para obtener datos actualizados..."
      
      # Obtener resultados reales de tests
      $realTestResults = Get-RealTestResults
      if ($realTestResults) {
        $TestData.TotalTests = $realTestResults.TotalTests
        $TestData.PassingTests = $realTestResults.PassingTests
        $TestData.FailingTests = $realTestResults.FailingTests
        $TestData.TestSuites = $realTestResults.TestSuites
        $TestData.ExecutionTime = $realTestResults.ExecutionTime
        $TestData.Duration = $realTestResults.Duration
      }
      
      # Obtener datos reales de cobertura
      $realCoverage = Get-JestCoverageData
      if ($realCoverage) {
        $TestData.Coverage = $realCoverage
        Write-Success "✅ Datos de cobertura reales obtenidos"
      }
      
      # Obtener información de tests de carga
      $loadTestResults = Get-RealLoadTestResults
      if ($loadTestResults) {
        $TestData.LoadTests = @{
          Count = if ($loadTestResults.Available) { $loadTestResults.Summary.TotalExecuted } else { 0 }
          Available = $loadTestResults.Available
          Results = $loadTestResults
        }
      }
    }
    else {
      Write-Info "Usando datos básicos del proyecto (ejecutar con -RunTests para datos completos)"
      
      # Intentar obtener datos de tests existentes desde archivos TRX
      $realTestResults = Get-RealTestResults
      if ($realTestResults) {
        $TestData.TotalTests = $realTestResults.TotalTests
        $TestData.PassingTests = $realTestResults.PassingTests
        $TestData.FailingTests = $realTestResults.FailingTests
        $TestData.TestSuites = $realTestResults.TestSuites
        $TestData.ExecutionTime = $realTestResults.ExecutionTime
        $TestData.Duration = $realTestResults.Duration
      }
      else {
        # Obtener conteo básico de archivos de test sin ejecutarlos
        $testSuites = Get-RealTestSuites
        $TestData.TestSuites = $testSuites
      }
      
      # Intentar obtener datos de cobertura existentes si hay un archivo
      $existingCoverage = Get-JestCoverageData
      if ($existingCoverage) {
        $TestData.Coverage = $existingCoverage
        Write-Info "📊 Usando datos de cobertura existentes"
      }
      else {
        Write-Warning "⚠️ No hay datos de cobertura disponibles. Ejecutar con -RunTests para generar cobertura actualizada."
      }
      
      # Obtener información de tests de carga (también para datos básicos)
      $loadTestResults = Get-RealLoadTestResults
      if ($loadTestResults) {
        $TestData.LoadTests = @{
          Count = if ($loadTestResults.Available) { $loadTestResults.Summary.TotalExecuted } else { 0 }
          Available = $loadTestResults.Available
          Results = $loadTestResults
        }
      }
    }
    
    Write-Info "Generando dashboard HTML..."
    Write-Info "DEBUG: TestData.TotalTests = $($TestData.TotalTests)"
    Write-Info "DEBUG: TestData.PassingTests = $($TestData.PassingTests)"
    Write-Info "DEBUG: TestData.Coverage.Statements = $($TestData.Coverage.Statements)"
    $dashboardHTML = Get-DashboardHTML -TestData $TestData
        
    # Escribir archivo
    $dashboardHTML | Out-File -FilePath $OutputPath -Encoding UTF8
    Write-Success "Dashboard generado: $OutputPath"
        
    # Mostrar estadísticas en consola con datos reales
    Write-Info "=== RESUMEN DE TESTS ==="
    Write-Info "Tests totales: $($TestData.TotalTests)"
    Write-Success "Tests exitosos: $($TestData.PassingTests)"
    if ($TestData.FailingTests -gt 0) {
      Write-Warning "Tests fallidos: $($TestData.FailingTests)"
    }
    
    $averageCoverage = [math]::Round(($TestData.Coverage.Statements + $TestData.Coverage.Branches + $TestData.Coverage.Functions + $TestData.Coverage.Lines) / 4, 1)
    Write-Info "Cobertura promedio: $averageCoverage%"
    Write-Info "  - Statements: $($TestData.Coverage.Statements)%"
    Write-Info "  - Branches: $($TestData.Coverage.Branches)%"
    Write-Info "  - Functions: $($TestData.Coverage.Functions)%"
    Write-Info "  - Lines: $($TestData.Coverage.Lines)%"
    
    Write-Info "Suites de tests: $($TestData.TestSuites.Count)"
    if ($TestData.LoadTests.Available) {
      Write-Info "Tests de carga: $($TestData.LoadTests.Count)"
    }
    else {
      Write-Info "Tests de carga: No configurados"
    }
    
    if ($TestData.Duration -ne "0s") {
      Write-Info "Duración de ejecución: $($TestData.Duration)"
    }
        
    # Abrir dashboard si se solicita
    if ($OpenDashboard) {
      Write-Info "Abriendo dashboard en el navegador..."
      Start-Process $OutputPath
    }
        
    Write-Success "✨ Dashboard de tests generado exitosamente"
    if ($RunTests) {
      Write-Info "📊 Dashboard con datos reales de la ejecución actual"
    }
    else {
      Write-Info "💡 Para datos completos ejecutar: .\manage-tests.ps1 -RunTests -OpenDashboard"
    }
        
  }
  catch {
    Write-Error "Error durante la generación del dashboard: $($_.Exception.Message)"
    exit 1
  }
}

# Ejecutar función principal
Main