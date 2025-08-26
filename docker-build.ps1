#!/usr/bin/env pwsh

param(
    [Parameter(Position = 0)]
    [ValidateSet('dev', 'prod', 'test')]
    [string]$Environment = 'dev',
    
    [switch]$NoBuild,
    [switch]$Clean,
    [switch]$Push,
    [string]$Registry = '',
    [string]$Version = 'latest'
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

function Write-Error {
    param($Message)
    Write-Host "❌ $Message" -ForegroundColor Red
}

Write-Info "Building Accessibility Gateway - Environment: $Environment"

$ImageName = "accessibility-gateway"
$FullImageName = if ($Registry) { "$Registry/$ImageName" } else { $ImageName }

# Limpiar imágenes previas si se solicita
if ($Clean) {
    Write-Info "Cleaning previous images..."
    docker rmi "$FullImageName`:$Version" -f 2>$null | Out-Null
    docker system prune -f | Out-Null
}

# Build de la imagen
if (-not $NoBuild) {
    Write-Info "Building Docker image: $FullImageName`:$Version"
    
    $BuildArgs = @()
    
    if ($Environment -eq 'prod') {
        $BuildArgs += @('--build-arg', 'BUILD_CONFIGURATION=Release')
        $Target = 'final'
    } elseif ($Environment -eq 'dev') {
        $BuildArgs += @('--build-arg', 'BUILD_CONFIGURATION=Debug')
        $Target = 'build'
    } else {
        $BuildArgs += @('--build-arg', 'BUILD_CONFIGURATION=Release')
        $Target = 'final'
    }
    
    $BuildCommand = @(
        'docker', 'build',
        '--target', $Target,
        '--tag', "$FullImageName`:$Version",
        '--tag', "$FullImageName`:latest"
    ) + $BuildArgs + @('./src')
    
    Write-Host "Executing: $($BuildCommand -join ' ')" -ForegroundColor Cyan
    
    & $BuildCommand[0] $BuildCommand[1..($BuildCommand.Length-1)]
    
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Docker build failed with exit code $LASTEXITCODE"
        exit $LASTEXITCODE
    }
    
    Write-Info "Docker image built successfully: $FullImageName`:$Version"
}

# Push de la imagen si se solicita
if ($Push -and $Registry) {
    Write-Info "Pushing image to registry: $Registry"
    
    docker push "$FullImageName`:$Version"
    docker push "$FullImageName`:latest"
    
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Docker push failed with exit code $LASTEXITCODE"
        exit $LASTEXITCODE
    }
    
    Write-Info "Image pushed successfully"
}

# Mostrar información de la imagen
Write-Info "Image information:"
docker images | Select-String $ImageName | ForEach-Object { Write-Host $_ -ForegroundColor Blue }

Write-Info "Build completed successfully!"
Write-Info "Use 'docker-compose up' to start the gateway"
