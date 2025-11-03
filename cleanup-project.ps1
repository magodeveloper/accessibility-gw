# ═══════════════════════════════════════════════════════════════
# SCRIPT DE LIMPIEZA DEL PROYECTO ACCESSIBILITY-GW
# ═══════════════════════════════════════════════════════════════
# Fecha: 2025-10-25
# Propósito: Eliminar archivos temporales, logs y reportes generados
# Uso: .\cleanup-project.ps1 [-DryRun] [-IncludeCoverage] [-IncludeTestScripts]

param(
    [switch]$DryRun,
    [switch]$IncludeCoverage,
    [switch]$IncludeTestScripts,
    [switch]$All
)

$ErrorActionPreference = "Continue"
$filesDeleted = 0
$spaceFreed = 0

# ═══════════════════════════════════════════════════════════════
# FUNCIONES AUXILIARES
# ═══════════════════════════════════════════════════════════════

function Write-CleanupHeader {
    Write-Host "`n╔══════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
    Write-Host "║          🧹 LIMPIEZA DEL PROYECTO ACCESSIBILITY-GW          ║" -ForegroundColor Cyan
    Write-Host "╚══════════════════════════════════════════════════════════════╝`n" -ForegroundColor Cyan
    Write-Host "📅 Fecha: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')" -ForegroundColor Gray
    
    if ($DryRun) {
        Write-Host "⚠️  MODO DRY-RUN: No se eliminarán archivos (solo simulación)`n" -ForegroundColor Yellow
    }
    else {
        Write-Host "⚠️  MODO REAL: Los archivos serán eliminados permanentemente`n" -ForegroundColor Red
    }
}

function Remove-FileIfExists {
    param(
        [string]$Path,
        [string]$Description
    )
    
    $items = Get-ChildItem -Path $Path -ErrorAction SilentlyContinue
    
    foreach ($item in $items) {
        $size = if ($item.PSIsContainer) {
            (Get-ChildItem -Path $item.FullName -Recurse -File | Measure-Object -Property Length -Sum).Sum
        }
        else {
            $item.Length
        }
        
        $sizeKB = [math]::Round($size / 1KB, 2)
        
        if ($DryRun) {
            Write-Host "  [DRY RUN] Eliminaría: $($item.Name) ($sizeKB KB)" -ForegroundColor Yellow
            $script:spaceFreed += $size
        }
        else {
            try {
                if ($item.PSIsContainer) {
                    Remove-Item $item.FullName -Recurse -Force -ErrorAction Stop
                }
                else {
                    Remove-Item $item.FullName -Force -ErrorAction Stop
                }
                Write-Host "  ✅ Eliminado: $($item.Name) ($sizeKB KB)" -ForegroundColor Green
                $script:filesDeleted++
                $script:spaceFreed += $size
            }
            catch {
                Write-Host "  ❌ Error al eliminar: $($item.Name) - $($_.Exception.Message)" -ForegroundColor Red
            }
        }
    }
}

function Clear-FolderContents {
    param(
        [string]$FolderPath,
        [string]$Description
    )
    
    if (-not (Test-Path $FolderPath)) {
        Write-Host "  ⏭️  Carpeta no existe: $FolderPath" -ForegroundColor Gray
        return
    }
    
    $items = Get-ChildItem -Path $FolderPath -Recurse -File
    $totalSize = ($items | Measure-Object -Property Length -Sum).Sum
    $sizeKB = [math]::Round($totalSize / 1KB, 2)
    
    if ($items.Count -eq 0) {
        Write-Host "  ℹ️  Carpeta ya está vacía: $FolderPath" -ForegroundColor Gray
        return
    }
    
    if ($DryRun) {
        Write-Host "  [DRY RUN] Limpiaría: $FolderPath\ ($($items.Count) archivos, $sizeKB KB)" -ForegroundColor Yellow
        $script:spaceFreed += $totalSize
    }
    else {
        try {
            Remove-Item "$FolderPath\*" -Recurse -Force -ErrorAction Stop
            Write-Host "  ✅ Limpiado: $FolderPath\ ($($items.Count) archivos, $sizeKB KB)" -ForegroundColor Green
            $script:filesDeleted += $items.Count
            $script:spaceFreed += $totalSize
        }
        catch {
            Write-Host "  ❌ Error al limpiar: $FolderPath - $($_.Exception.Message)" -ForegroundColor Red
        }
    }
}

# ═══════════════════════════════════════════════════════════════
# INICIO DEL SCRIPT
# ═══════════════════════════════════════════════════════════════

Write-CleanupHeader

# Confirmar si no es DryRun
if (-not $DryRun -and -not $All) {
    Write-Host "⚠️  Esta operación eliminará archivos permanentemente." -ForegroundColor Yellow
    $confirmation = Read-Host "¿Desea continuar? (S/N)"
    if ($confirmation -ne "S" -and $confirmation -ne "s") {
        Write-Host "`n❌ Operación cancelada por el usuario" -ForegroundColor Red
        exit 0
    }
}

# ═══════════════════════════════════════════════════════════════
# FASE 1: ARCHIVOS TEMPORALES DE TEST (Siempre seguro eliminar)
# ═══════════════════════════════════════════════════════════════

Write-Host "`n📋 FASE 1: Limpieza de archivos temporales de test" -ForegroundColor Cyan
Write-Host "$('─' * 70)" -ForegroundColor Gray

Remove-FileIfExists -Path "integration-test-results-*.json" -Description "Resultados de integración"
Remove-FileIfExists -Path "test-results-*.json" -Description "Resultados de test"
Remove-FileIfExists -Path "swagger-*-test.json" -Description "Tests de Swagger"
Remove-FileIfExists -Path "prometheus-rules-check.json" -Description "Check de reglas Prometheus"

# ═══════════════════════════════════════════════════════════════
# FASE 2: LOGS (Siempre seguro eliminar)
# ═══════════════════════════════════════════════════════════════

Write-Host "`n📝 FASE 2: Limpieza de logs" -ForegroundColor Cyan
Write-Host "$('─' * 70)" -ForegroundColor Gray

Remove-FileIfExists -Path "gateway-output.log" -Description "Log de output del Gateway"
Remove-FileIfExists -Path "*.log" -Description "Otros archivos log"

# Limpiar carpeta logs/
if (Test-Path "logs") {
    Clear-FolderContents -FolderPath "logs" -Description "Logs del Gateway"
}

# ═══════════════════════════════════════════════════════════════
# FASE 3: REPORTES DE COBERTURA (Opcional)
# ═══════════════════════════════════════════════════════════════

if ($IncludeCoverage -or $All) {
    Write-Host "`n📊 FASE 3: Limpieza de reportes de cobertura" -ForegroundColor Cyan
    Write-Host "$('─' * 70)" -ForegroundColor Gray
    
    if (Test-Path "coverage-report") {
        Clear-FolderContents -FolderPath "coverage-report" -Description "Reportes de cobertura HTML"
        
        if (-not $DryRun) {
            # Eliminar la carpeta completa
            try {
                Remove-Item "coverage-report" -Recurse -Force -ErrorAction Stop
                Write-Host "  ✅ Carpeta coverage-report eliminada" -ForegroundColor Green
            }
            catch {
                Write-Host "  ⚠️  No se pudo eliminar la carpeta coverage-report" -ForegroundColor Yellow
            }
        }
    }
    else {
        Write-Host "  ℹ️  Carpeta coverage-report no existe" -ForegroundColor Gray
    }
    
    # Limpiar otros directorios de cobertura
    $coverageDirs = @("coverage", "CoverageReport", "coverage-dashboard")
    foreach ($dir in $coverageDirs) {
        if (Test-Path $dir) {
            Clear-FolderContents -FolderPath $dir -Description "Cobertura: $dir"
            if (-not $DryRun) {
                Remove-Item $dir -Recurse -Force -ErrorAction SilentlyContinue
            }
        }
    }
}
else {
    Write-Host "`n📊 FASE 3: Reportes de cobertura (OMITIDO)" -ForegroundColor Gray
    Write-Host "   Use -IncludeCoverage para limpiar reportes de cobertura" -ForegroundColor Gray
}

# ═══════════════════════════════════════════════════════════════
# FASE 4: SCRIPTS DE TEST LEGACY (Revisión manual recomendada)
# ═══════════════════════════════════════════════════════════════

if ($IncludeTestScripts -or $All) {
    Write-Host "`n🔧 FASE 4: Análisis de scripts de test legacy" -ForegroundColor Cyan
    Write-Host "$('─' * 70)" -ForegroundColor Gray
    
    $testScripts = Get-ChildItem -Path "test-step*.ps1" -ErrorAction SilentlyContinue
    
    if ($testScripts.Count -gt 0) {
        Write-Host "`n  ⚠️  Se encontraron $($testScripts.Count) scripts de test legacy:" -ForegroundColor Yellow
        
        foreach ($script in $testScripts) {
            $size = [math]::Round($script.Length / 1KB, 2)
            $lines = (Get-Content $script.FullName).Count
            Write-Host "     📄 $($script.Name) - $size KB ($lines líneas)" -ForegroundColor Cyan
        }
        
        if (-not $DryRun) {
            Write-Host "`n  ⚠️  Estos scripts requieren revisión manual." -ForegroundColor Yellow
            Write-Host "     Recomendación:" -ForegroundColor Gray
            Write-Host "     1. Verificar si están integrados en manage-tests.ps1" -ForegroundColor Gray
            Write-Host "     2. Si son obsoletos, eliminarlos manualmente" -ForegroundColor Gray
            Write-Host "     3. Si tienen lógica única, consolidar en script principal`n" -ForegroundColor Gray
        }
    }
    else {
        Write-Host "  ℹ️  No se encontraron scripts de test legacy" -ForegroundColor Gray
    }
}
else {
    Write-Host "`n🔧 FASE 4: Scripts de test legacy (OMITIDO)" -ForegroundColor Gray
    Write-Host "   Use -IncludeTestScripts para analizar scripts de test" -ForegroundColor Gray
}

# ═══════════════════════════════════════════════════════════════
# FASE 5: ARCHIVOS ADICIONALES OPCIONALES
# ═══════════════════════════════════════════════════════════════

Write-Host "`n📦 FASE 5: Archivos adicionales" -ForegroundColor Cyan
Write-Host "$('─' * 70)" -ForegroundColor Gray

# Verificar si packages.json es reciente
if (Test-Path "packages.json") {
    $packagesJson = Get-Item "packages.json"
    $daysOld = ((Get-Date) - $packagesJson.LastWriteTime).Days
    $sizeKB = [math]::Round($packagesJson.Length / 1KB, 2)
    
    Write-Host "  📄 packages.json encontrado ($sizeKB KB, $daysOld días de antigüedad)" -ForegroundColor Cyan
    
    if ($daysOld -gt 30) {
        Write-Host "     ⚠️  Archivo tiene más de 30 días. Considere regenerar con:" -ForegroundColor Yellow
        Write-Host "     dotnet list package --include-transitive --format json > packages.json" -ForegroundColor Gray
    }
    else {
        Write-Host "     ✅ Archivo está relativamente actualizado" -ForegroundColor Green
    }
}

# ═══════════════════════════════════════════════════════════════
# RESUMEN FINAL
# ═══════════════════════════════════════════════════════════════

Write-Host "`n╔══════════════════════════════════════════════════════════════╗" -ForegroundColor Green
Write-Host "║                     📊 RESUMEN DE LIMPIEZA                   ║" -ForegroundColor Green
Write-Host "╚══════════════════════════════════════════════════════════════╝" -ForegroundColor Green

$spaceMB = [math]::Round($spaceFreed / 1MB, 2)
$spaceKB = [math]::Round($spaceFreed / 1KB, 2)

if ($DryRun) {
    Write-Host "`n  Modo: DRY-RUN (simulación)" -ForegroundColor Yellow
    Write-Host "  Archivos que se eliminarían: $filesDeleted" -ForegroundColor Cyan
    Write-Host "  Espacio que se liberaría: $spaceKB KB ($spaceMB MB)" -ForegroundColor Cyan
    Write-Host "`n  💡 Ejecute sin -DryRun para realizar la limpieza real" -ForegroundColor Gray
}
else {
    Write-Host "`n  ✅ Limpieza completada exitosamente" -ForegroundColor Green
    Write-Host "  📁 Archivos eliminados: $filesDeleted" -ForegroundColor Cyan
    Write-Host "  💾 Espacio liberado: $spaceKB KB ($spaceMB MB)" -ForegroundColor Cyan
}

Write-Host "`n  📅 Fecha: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')" -ForegroundColor Gray

# ═══════════════════════════════════════════════════════════════
# VERIFICACIÓN POST-LIMPIEZA
# ═══════════════════════════════════════════════════════════════

if (-not $DryRun) {
    Write-Host "`n🔍 Verificación post-limpieza:" -ForegroundColor Cyan
    Write-Host "   Ejecute para verificar que el proyecto funciona correctamente:" -ForegroundColor Gray
    Write-Host "   dotnet build Gateway.sln" -ForegroundColor Yellow
    Write-Host "   .\manage-tests.ps1 -Action unit" -ForegroundColor Yellow
}

Write-Host "`n✨ Proceso completado`n" -ForegroundColor Green

# ═══════════════════════════════════════════════════════════════
# AYUDA
# ═══════════════════════════════════════════════════════════════

if ($args -contains "-h" -or $args -contains "--help") {
    Write-Host @"
    
CLEANUP-PROJECT.PS1 - Script de limpieza del proyecto accessibility-gw

USO:
    .\cleanup-project.ps1 [OPCIONES]

OPCIONES:
    -DryRun               Simular limpieza sin eliminar archivos
    -IncludeCoverage      Incluir reportes de cobertura en la limpieza
    -IncludeTestScripts   Analizar scripts de test legacy
    -All                  Limpiar todo (coverage + test scripts)

EJEMPLOS:
    # Simulación (ver qué se eliminaría)
    .\cleanup-project.ps1 -DryRun
    
    # Limpieza básica (solo temporales y logs)
    .\cleanup-project.ps1
    
    # Limpieza completa incluyendo cobertura
    .\cleanup-project.ps1 -IncludeCoverage
    
    # Limpieza total
    .\cleanup-project.ps1 -All

ARCHIVOS QUE SE LIMPIAN:
    - Resultados de test (*.json)
    - Logs (*.log)
    - Reportes de cobertura (opcional)
    - Análisis de scripts legacy (opcional)

MÁS INFORMACIÓN:
    Ver CLEANUP-REPORT.md para detalles completos
    
"@
    exit 0
}
