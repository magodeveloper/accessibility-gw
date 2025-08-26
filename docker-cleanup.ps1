#!/usr/bin/env pwsh

param(
    [Parameter(Position = 0)]
    [ValidateSet('dev', 'prod', 'all')]
    [string]$Environment = 'all',
    
    [switch]$Volumes,
    [switch]$Networks,
    [switch]$Force
)

$ErrorActionPreference = 'Stop'

function Write-Info {
    param($Message)
    Write-Host "✅ $Message" -ForegroundColor Green
}

function Write-Warning {
    param($Message)
    Write-Host "⚠️ $Message" -ForegroundColor Yellow
}

Write-Info "Cleaning up Docker resources for Accessibility Gateway"

# Detener y eliminar contenedores
$ContainerPatterns = @(
    'accessibility-gw*',
    'accessibility-redis*'
)

foreach ($pattern in $ContainerPatterns) {
    $containers = docker ps -a --filter "name=$pattern" --format "{{.Names}}"
    if ($containers) {
        Write-Info "Stopping and removing containers matching: $pattern"
        $containers | ForEach-Object {
            docker stop $_ 2>$null | Out-Null
            docker rm $_ 2>$null | Out-Null
        }
    }
}

# Eliminar imágenes
$ImagePatterns = @(
    'accessibility-gateway*',
    'accessibility-gw*'
)

foreach ($pattern in $ImagePatterns) {
    $images = docker images --filter "reference=$pattern" --format "{{.Repository}}:{{.Tag}}"
    if ($images) {
        Write-Info "Removing images matching: $pattern"
        $images | ForEach-Object {
            docker rmi $_ -f 2>$null | Out-Null
        }
    }
}

# Limpiar volúmenes si se solicita
if ($Volumes) {
    Write-Info "Removing volumes..."
    $VolumePatterns = @(
        'accessibility-gw*',
        '*gateway*',
        '*redis*'
    )
    
    foreach ($pattern in $VolumePatterns) {
        $volumes = docker volume ls --filter "name=$pattern" --format "{{.Name}}"
        if ($volumes) {
            $volumes | ForEach-Object {
                docker volume rm $_ 2>$null | Out-Null
            }
        }
    }
}

# Limpiar redes si se solicita
if ($Networks) {
    Write-Info "Removing networks..."
    $NetworkPatterns = @(
        'accessibility-*network*'
    )
    
    foreach ($pattern in $NetworkPatterns) {
        $networks = docker network ls --filter "name=$pattern" --format "{{.Name}}"
        if ($networks) {
            $networks | ForEach-Object {
                docker network rm $_ 2>$null | Out-Null
            }
        }
    }
}

# Limpiar recursos no utilizados
Write-Info "Cleaning up unused Docker resources..."
docker system prune -f | Out-Null

if ($Force) {
    Write-Warning "Performing aggressive cleanup..."
    docker system prune -a -f | Out-Null
}

Write-Info "Docker cleanup completed!"
Write-Info "Current Docker status:"

Write-Host "`n🐳 Images:" -ForegroundColor Cyan
docker images | Select-String -Pattern "(accessibility|REPOSITORY)" | ForEach-Object { 
    Write-Host $_ -ForegroundColor Blue 
}

Write-Host "`n📦 Containers:" -ForegroundColor Cyan
docker ps -a | Select-String -Pattern "(accessibility|CONTAINER)" | ForEach-Object { 
    Write-Host $_ -ForegroundColor Blue 
}

if ($Volumes) {
    Write-Host "`n💾 Volumes:" -ForegroundColor Cyan
    docker volume ls | Select-String -Pattern "(accessibility|DRIVER)" | ForEach-Object { 
        Write-Host $_ -ForegroundColor Blue 
    }
}

if ($Networks) {
    Write-Host "`n🌐 Networks:" -ForegroundColor Cyan
    docker network ls | Select-String -Pattern "(accessibility|NETWORK)" | ForEach-Object { 
        Write-Host $_ -ForegroundColor Blue 
    }
}
