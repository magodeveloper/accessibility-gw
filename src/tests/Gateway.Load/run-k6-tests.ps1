# run-k6-tests.ps1 - Script optimizado para ejecutar tests K6 y gestionar resultados
# Version: 2.0

param(
    [ValidateSet("run", "clean", "help")]
    [string]$Action = "run",
    
    [ValidateSet("light", "medium", "high", "extreme", "all")]
    [string]$Level = "all",
    
    [ValidateSet("simple", "full")]
    [string]$Mode = "full",
    
    [int]$Days = 7,
    
    [switch]$SkipCleanup,
    
    [string]$BaseUrl = "http://localhost:8100"
)

$ScriptDir = $PSScriptRoot
$ResultsDir = "$ScriptDir\results"
$ScenariosDir = "$ScriptDir\scenarios"

function Show-Help {
    Write-Host "`n╔═══════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
    Write-Host "║       🧪 Gateway K6 Load Tests - Script de Ejecución        ║" -ForegroundColor Cyan
    Write-Host "╚═══════════════════════════════════════════════════════════════╝`n" -ForegroundColor Cyan
    
    Write-Host "DESCRIPCIÓN:" -ForegroundColor Yellow
    Write-Host "  Script optimizado para ejecutar pruebas de carga K6 del Gateway`n"
    
    Write-Host "USO:" -ForegroundColor Yellow
    Write-Host "  .\run-k6-tests.ps1 [-Action <action>] [-Level <level>] [-Mode <mode>] [opciones]`n"
    
    Write-Host "PARÁMETROS:" -ForegroundColor Yellow
    Write-Host "  -Action <run|clean|help>" -ForegroundColor White
    Write-Host "      run     - Ejecuta los tests de carga (default)" -ForegroundColor Gray
    Write-Host "      clean   - Limpia resultados antiguos" -ForegroundColor Gray
    Write-Host "      help    - Muestra esta ayuda`n" -ForegroundColor Gray
    
    Write-Host "  -Level <light|medium|high|extreme|all>" -ForegroundColor White
    Write-Host "      light   - 20 usuarios concurrentes (~5 min)" -ForegroundColor Gray
    Write-Host "      medium  - 50 usuarios concurrentes (~9 min)" -ForegroundColor Gray
    Write-Host "      high    - 100 usuarios concurrentes (~21 min)" -ForegroundColor Gray
    Write-Host "      extreme - 500 usuarios concurrentes (~32 min)" -ForegroundColor Gray
    Write-Host "      all     - Todos los niveles secuencialmente (default)`n" -ForegroundColor Gray
    
    Write-Host "  -Mode <simple|full>" -ForegroundColor White
    Write-Host "      simple  - Solo endpoints básicos (/health, /metrics)" -ForegroundColor Gray
    Write-Host "      full    - Todos los microservicios (default)`n" -ForegroundColor Gray
    
    Write-Host "  -Days <número>" -ForegroundColor White
    Write-Host "      Días de antigüedad para limpieza (default: 7)`n" -ForegroundColor Gray
    
    Write-Host "  -SkipCleanup" -ForegroundColor White
    Write-Host "      No ejecuta limpieza automática antes de los tests`n" -ForegroundColor Gray
    
    Write-Host "  -BaseUrl <url>" -ForegroundColor White
    Write-Host "      URL base del Gateway (default: http://localhost:8100)`n" -ForegroundColor Gray
    
    Write-Host "EJEMPLOS:" -ForegroundColor Yellow
    Write-Host "  # Ejecutar solo test ligero en modo simple" -ForegroundColor Gray
    Write-Host "  .\run-k6-tests.ps1 -Level light -Mode simple`n" -ForegroundColor White
    
    Write-Host "  # Ejecutar test extremo con URL personalizada" -ForegroundColor Gray
    Write-Host "  .\run-k6-tests.ps1 -Level extreme -BaseUrl http://gateway.prod:8100`n" -ForegroundColor White
    
    Write-Host "  # Limpiar resultados de más de 3 días" -ForegroundColor Gray
    Write-Host "  .\run-k6-tests.ps1 -Action clean -Days 3`n" -ForegroundColor White
    
    Write-Host "  # Ejecutar todos los tests sin limpieza previa" -ForegroundColor Gray
    Write-Host "  .\run-k6-tests.ps1 -SkipCleanup`n" -ForegroundColor White
}

function Invoke-CleanResults {
    param([int]$DaysOld)
    Write-Host "`n🧹 Limpiando resultados antiguos (>$DaysOld días)..." -ForegroundColor Cyan
    
    if (-not (Test-Path $ResultsDir)) {
        Write-Host "✅ No hay directorio de resultados" -ForegroundColor Green
        return
    }
    
    $cutoffDate = (Get-Date).AddDays(-$DaysOld)
    $oldFiles = Get-ChildItem -Path $ResultsDir -Filter "*.json" | Where-Object { $_.LastWriteTime -lt $cutoffDate }
    
    if ($oldFiles.Count -eq 0) {
        Write-Host "✅ No hay archivos antiguos" -ForegroundColor Green
        return
    }
    
    Write-Host "🗑️  Encontrados $($oldFiles.Count) archivos antiguos" -ForegroundColor Yellow
    foreach ($file in $oldFiles) {
        $age = [math]::Round(((Get-Date) - $file.LastWriteTime).TotalDays, 1)
        Remove-Item $file.FullName -Force
        Write-Host "  ✅ Eliminado: $($file.Name) ($age días)" -ForegroundColor Green
    }
    
    Write-Host "`n✅ Limpieza completada`n" -ForegroundColor Green
}

function Invoke-K6Tests {
    Write-Host "`n╔═══════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
    Write-Host "║       🧪 Gateway K6 Load Tests - NUEVA ARQUITECTURA         ║" -ForegroundColor Cyan
    Write-Host "╚═══════════════════════════════════════════════════════════════╝`n" -ForegroundColor Cyan
    
    # Verificar que k6 esté instalado
    if (-not (Get-Command k6 -ErrorAction SilentlyContinue)) {
        Write-Host "❌ k6 no está instalado" -ForegroundColor Red
        Write-Host "💡 Instalar con: winget install k6" -ForegroundColor Yellow
        Write-Host "   O visita: https://k6.io/docs/get-started/installation/`n" -ForegroundColor Yellow
        exit 1
    }
    
    # Crear directorio de resultados si no existe
    if (-not (Test-Path $ResultsDir)) { 
        New-Item -ItemType Directory -Path $ResultsDir -Force | Out-Null 
    }
    
    # Limpieza automática antes de los tests (si no se especifica -SkipCleanup)
    if (-not $SkipCleanup) {
        Invoke-CleanResults -DaysOld $Days
    }
    
    # Cargar variables de entorno desde .env
    $envFile = "$ScriptDir\.env"
    if (Test-Path $envFile) {
        Get-Content $envFile | ForEach-Object {
            if ($_ -match '^\s*([^#][^=]+)=(.*)$') {
                $key = $matches[1].Trim()
                $value = $matches[2].Trim()
                Set-Item -Path "env:$key" -Value $value
            }
        }
        Write-Host "✅ Variables .env cargadas" -ForegroundColor Green
    }
    else {
        Write-Host "⚠️  Archivo .env no encontrado - usando valores por defecto" -ForegroundColor Yellow
    }
    
    # Configurar URL base
    $env:BASE_URL = $BaseUrl
    Write-Host "🌐 Base URL: $BaseUrl" -ForegroundColor Cyan
    Write-Host "🔧 Modo: $Mode`n" -ForegroundColor Cyan
    
    # Definir tests según el nivel
    $testsToRun = @()
    
    if ($Level -eq "all") {
        $testsToRun = @(
            @{ Name = "light-load"; Users = 20; Level = "light" }
            @{ Name = "medium-load"; Users = 50; Level = "medium" }
            @{ Name = "high-load"; Users = 100; Level = "high" }
            @{ Name = "extreme-load"; Users = 500; Level = "extreme" }
        )
    }
    else {
        $levelConfig = @{
            light   = @{ Name = "light-load"; Users = 20; Level = "light" }
            medium  = @{ Name = "medium-load"; Users = 50; Level = "medium" }
            high    = @{ Name = "high-load"; Users = 100; Level = "high" }
            extreme = @{ Name = "extreme-load"; Users = 500; Level = "extreme" }
        }
        $testsToRun = @($levelConfig[$Level])
    }
    
    $successCount = 0
    $totalTests = $testsToRun.Count
    
    foreach ($test in $testsToRun) {
        Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Magenta
        Write-Host "🚀 Test: $($test.Name) - $($test.Users) usuarios - Nivel: $($test.Level)" -ForegroundColor Magenta
        Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━`n" -ForegroundColor Magenta
        
        $output = "$ResultsDir\$($test.Name)-$Mode.json"
        $scenario = "$ScenariosDir\concurrent-users.js"
        
        # Ejecutar k6 con el nuevo archivo genérico
        $env:USERS = $test.Users
        $env:TEST_LEVEL = $test.Level
        $env:TEST_MODE = $Mode
        
        k6 run --out json=$output $scenario
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "`n✅ $($test.Name) completado exitosamente`n" -ForegroundColor Green
            $successCount++
        }
        else {
            Write-Host "`n❌ $($test.Name) falló (exit code: $LASTEXITCODE)`n" -ForegroundColor Red
        }
        
        # Esperar entre tests (excepto el último)
        if ($testsToRun.IndexOf($test) -lt ($totalTests - 1)) {
            Write-Host "⏸️  Esperando 5 segundos antes del siguiente test...`n" -ForegroundColor Gray
            Start-Sleep -Seconds 5
        }
    }
    
    # Resumen final
    Write-Host "`n╔═══════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
    Write-Host "║                    📊 RESUMEN DE TESTS                       ║" -ForegroundColor Cyan
    Write-Host "╚═══════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
    Write-Host "  Exitosos: $successCount/$totalTests tests" -ForegroundColor $(if ($successCount -eq $totalTests) { "Green" } else { "Yellow" })
    Write-Host "  Modo: $Mode" -ForegroundColor Cyan
    Write-Host "  Nivel: $Level" -ForegroundColor Cyan
    Write-Host "  Resultados en: $ResultsDir`n" -ForegroundColor Gray
    
    if ($successCount -eq $totalTests) {
        Write-Host "✅ ¡Todos los tests completados exitosamente!" -ForegroundColor Green
        Write-Host "💡 Ver dashboard: cd ..\..\..; .\manage-tests.ps1 dashboard -OpenDashboard`n" -ForegroundColor Yellow
        exit 0
    }
    else {
        Write-Host "⚠️  Algunos tests fallaron - revisar logs arriba" -ForegroundColor Yellow
        Write-Host "💡 Tip: Ejecutar con -Level light -Mode simple para verificar conectividad básica`n" -ForegroundColor Yellow
        exit 1
    }
}

# MAIN
switch ($Action) {
    "run" { Invoke-K6Tests }
    "clean" { Invoke-CleanResults -DaysOld $Days }
    "help" { Show-Help }
}
