# Script para levantar todos los microservicios reales para load testing
# Ejecutar desde: C:\Git\accessibility-gw

$ErrorActionPreference = "Stop"

Write-Host "🚀 Iniciando microservicios reales..." -ForegroundColor Cyan
Write-Host ""

# Verificar que Docker esté disponible
try {
    $dockerVersion = docker --version 2>&1
    Write-Host "✅ Docker detectado: $dockerVersion" -ForegroundColor Green
}
catch {
    Write-Host "❌ ERROR: Docker no está disponible en el PATH del sistema" -ForegroundColor Red
    Write-Host "   Por favor, asegúrate de que Docker Desktop esté instalado y en ejecución" -ForegroundColor Yellow
    exit 1
}
Write-Host ""

$microservices = @(
    @{
        Name = "Users"
        Path = "C:\Git\accessibility-ms-users"
        Port = 8081
    },
    @{
        Name = "Analysis"
        Path = "C:\Git\accessibility-ms-analysis"
        Port = 8082
    },
    @{
        Name = "Reports"
        Path = "C:\Git\accessibility-ms-reports"
        Port = 8083
    }
)

foreach ($ms in $microservices) {
    Write-Host "📦 Iniciando $($ms.Name) (puerto $($ms.Port))..." -ForegroundColor Yellow
    
    if (-not (Test-Path $ms.Path)) {
        Write-Host "   ❌ Directorio no encontrado: $($ms.Path)" -ForegroundColor Red
        continue
    }
    
    Push-Location $ms.Path
    
    # Verificar si existe docker-compose.yml
    if (Test-Path "docker-compose.yml") {
        Write-Host "   🐳 Levantando con Docker Compose..." -ForegroundColor Gray
        
        try {
            docker compose up -d 2>&1 | Out-Null
            
            if ($LASTEXITCODE -eq 0) {
                Write-Host "   ✅ $($ms.Name) iniciado correctamente" -ForegroundColor Green
            }
            else {
                Write-Host "   ⚠️  $($ms.Name) tuvo problemas al iniciar" -ForegroundColor Yellow
            }
        }
        catch {
            Write-Host "   ❌ Error: $_" -ForegroundColor Red
        }
    }
    else {
        Write-Host "   ⚠️  No se encontró docker-compose.yml" -ForegroundColor Yellow
    }
    
    Pop-Location
    Write-Host ""
}

Write-Host ""
Write-Host "⏳ Esperando 10 segundos para que los servicios se inicialicen..." -ForegroundColor Cyan
Start-Sleep -Seconds 10

Write-Host ""
Write-Host "🔍 Verificando estado de los servicios..." -ForegroundColor Cyan
Write-Host ""

foreach ($ms in $microservices) {
    try {
        $response = Invoke-WebRequest -Uri "http://localhost:$($ms.Port)/health" -TimeoutSec 3 -UseBasicParsing -ErrorAction Stop
        
        if ($response.StatusCode -eq 200) {
            Write-Host "   ✅ $($ms.Name) - Healthy (puerto $($ms.Port))" -ForegroundColor Green
        }
        else {
            Write-Host "   ⚠️  $($ms.Name) - Status: $($response.StatusCode)" -ForegroundColor Yellow
        }
    }
    catch {
        Write-Host "   ❌ $($ms.Name) - No disponible (puerto $($ms.Port))" -ForegroundColor Red
    }
}

Write-Host ""
Write-Host "✨ Proceso completado!" -ForegroundColor Cyan
Write-Host ""
Write-Host "📝 URLs de los microservicios:" -ForegroundColor White
Write-Host "   Users:    http://localhost:8081/health" -ForegroundColor Gray
Write-Host "   Analysis: http://localhost:8082/health" -ForegroundColor Gray
Write-Host "   Reports:  http://localhost:8083/health" -ForegroundColor Gray
Write-Host ""
Write-Host "🧪 Ahora puedes ejecutar:" -ForegroundColor White
Write-Host "   .\manage-tests.ps1 load" -ForegroundColor Cyan
