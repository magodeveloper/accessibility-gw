#!/usr/bin/env pwsh

<#
.SYNOPSIS
    Script unificado para gestión completa del Accessibility Gateway

.DESCRIPTION
    Script optimizado todo-en-uno para:
    - Testing (Unit, Integration, Performance)
    - Building (Local, Docker, Production)
    - Local Development Server (desarrollo aislado)
    - Verificación y Health Checks
    - Gestión de Docker (up, down, logs, status)  
    - Cleanup y mantenimiento
    - Integración con manage.ps1 del middleware

.PARAMETER Action
    Acción principal: test, build, run, verify, docker, cleanup, help

.NOTES
    Este script complementa al manage.ps1 del middleware principal.
    Mientras manage.ps1 orquesta todo el ecosistema, este script
    se enfoca en el desarrollo granular del Gateway específicamente.

.EXAMPLES
    .\manage-gateway.ps1 help
    .\manage-gateway.ps1 test -TestType Unit -GenerateCoverage
    .\manage-gateway.ps1 build -Configuration Release -BuildType production
    .\manage-gateway.ps1 run -Port 8100
    .\manage-gateway.ps1 docker up -Environment dev
    .\manage-gateway.ps1 verify -Full
    .\manage-gateway.ps1 cleanup -Docker -Volumes
#>

param(
    [Parameter(Position = 0)]
    [ValidateSet('test', 'build', 'verify', 'docker', 'cleanup', 'run', 'consistency', 'status', 'help')]
    [string]$Action = 'help',
    
    [Parameter(Position = 1)]
    [string]$SubAction = '',
    
    # Test parameters
    [ValidateSet('All', 'Unit', 'Integration', 'Performance')]
    [string]$TestType = 'All',
    [switch]$GenerateCoverage,
    [switch]$OpenReport,
    
    # Build parameters
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',
    [ValidateSet('standard', 'production', 'docker')]
    [string]$BuildType = 'standard',
    [switch]$Clean,
    [switch]$Push,
    [string]$Registry = '',
    [string]$Version = 'latest',
    
    # Environment parameters
    [ValidateSet('dev', 'prod', 'test')]
    [string]$Environment = 'prod',
    
    # Docker parameters
    [switch]$Follow,
    [switch]$WithTools,
    [switch]$Rebuild,
    
    # Verification parameters
    [switch]$Full,
    
    # Cleanup parameters
    [switch]$Docker,
    [switch]$Volumes,
    [switch]$All,
    
    # Run parameters
    [string]$Port = '8100',
    [ValidateSet('Development', 'Production', 'Staging')]
    [string]$AspNetCoreEnvironment = 'Development',
    [switch]$NoLaunch
)

# ===========================================
# CONFIGURACIÓN GLOBAL
# ===========================================

$script:ProjectName = "accessibility-gw"
$script:ImageName = "accessibility-gateway"
$script:SrcPath = "src"
$script:TestSrcPath = "src/tests"
$script:ProjectPath = "src/Gateway"
$script:SolutionFile = "Gateway.sln"
$script:TestSolutionFile = "src/tests/Gateway.Tests.sln"
$script:TestBasicPath = "src/tests/Gateway.Tests.Basic"
$script:TestUnitPath = "src/tests/Gateway.UnitTests"
$script:TestIntegrationPath = "src/tests/Gateway.IntegrationTests"

# ===========================================
# FUNCIONES AUXILIARES
# ===========================================

function Write-Header { param($title) Write-Host "`n🚪 $title" -ForegroundColor Green }
function Write-Step { param($msg) Write-Host "   ⚡ $msg" -ForegroundColor Cyan }
function Write-Success { param($msg) Write-Host "   ✅ $msg" -ForegroundColor Green }
function Write-Error { param($msg) Write-Host "   ❌ $msg" -ForegroundColor Red }
function Write-Warning { param($msg) Write-Host "   ⚠️ $msg" -ForegroundColor Yellow }
function Write-Info { param($msg) Write-Host "   ℹ️ $msg" -ForegroundColor Blue }

function Test-Prerequisites {
    Write-Header "PREREQUISITES VALIDATION"
    
    $issues = @()
    
    # Check .NET SDK
    if (-not (Test-CommandExists 'dotnet')) {
        $issues += "❌ .NET SDK not installed"
    }
    else {
        $version = dotnet --version
        if ([version]$version -lt [version]"9.0") {
            $issues += "⚠️ .NET SDK version $version (recommended: 9.0+)"
        }
        else {
            Write-Success ".NET SDK $version - OK"
        }
    }
    
    # Check Docker
    if (-not (Test-DockerRunning)) {
        $issues += "⚠️ Docker not running (required for Docker commands)"
    }
    else {
        Write-Success "Docker - Running"
    }
    
    # Check project structure
    if (-not (Test-Path $script:SolutionFile)) {
        $issues += "❌ Solution file not found: $script:SolutionFile"
    }
    
    if (-not (Test-Path $script:ProjectPath)) {
        $issues += "❌ Project path not found: $script:ProjectPath"
    }
    
    if (-not (Test-Path $script:TestSolutionFile)) {
        $issues += "⚠️ Test solution not found: $script:TestSolutionFile"
    }
    
    if ($issues.Count -gt 0) {
        Write-Warning "Found $($issues.Count) prerequisite issues:"
        $issues | ForEach-Object { Write-Host "  $_" -ForegroundColor Yellow }
        return $false
    }
    
    Write-Success "All prerequisites validated ✨"
    return $true
}

function Test-CommandExists {
    param([string]$Command)
    $null = Get-Command $Command -ErrorAction SilentlyContinue
    return $?
}

function Test-DockerRunning {
    try {
        $result = docker version --format '{{.Server.Version}}' 2>$null
        return $null -ne $result
    }
    catch {
        return $false
    }
}

function Test-RequiredNetwork {
    Write-Step "Checking required Docker network..."
    
    # Verificar si Docker está corriendo
    if (-not (Test-DockerRunning)) {
        Write-Error "Docker is not running. Cannot verify network."
        return $false
    }
    
    # Verificar si la red accessibility-shared existe
    try {
        $network = docker network ls --filter name=accessibility-shared --format "{{.Name}}" 2>$null
        if (-not $network -or $network -ne "accessibility-shared") {
            Write-Warning "Network 'accessibility-shared' not found. Creating..."
            
            # Crear la red con configuración específica
            docker network create `
                --driver bridge `
                --subnet=172.18.0.0/16 `
                --gateway=172.18.0.1 `
                accessibility-shared 2>$null
            
            if ($LASTEXITCODE -eq 0) {
                Write-Success "Network 'accessibility-shared' created successfully"
                Write-Info "  Driver: bridge"
                Write-Info "  Subnet: 172.18.0.0/16"
                Write-Info "  Gateway: 172.18.0.1"
                return $true
            } 
            else {
                Write-Error "Failed to create network 'accessibility-shared'"
                return $false
            }
        } 
        else {
            Write-Success "Network 'accessibility-shared' found and ready"
            
            # Mostrar información básica de la red
            try {
                $networkInfo = docker network inspect accessibility-shared --format "{{.Driver}} - {{range .IPAM.Config}}{{.Subnet}}{{end}}" 2>$null
                if ($networkInfo) {
                    Write-Info "  Network info: $networkInfo"
                }
            }
            catch {
                # Silenciar errores de inspect, no es crítico
            }
            
            return $true
        }
    }
    catch {
        Write-Error "Error checking Docker network: $($_.Exception.Message)"
        return $false
    }
}

function Test-NetworkConnectivity {
    Write-Step "Testing network connectivity..."
    
    if (-not (Test-DockerRunning)) {
        Write-Warning "Docker not running - skipping network connectivity tests"
        return $true
    }
    
    # Verificar contenedores conectados a la red
    try {
        $connectedContainers = docker network inspect accessibility-shared --format "{{range .Containers}}{{.Name}} {{end}}" 2>$null
        if ($connectedContainers -and $connectedContainers.Trim()) {
            $containerList = $connectedContainers.Trim() -split '\s+'
            Write-Success "Found $($containerList.Count) containers connected to accessibility-shared:"
            $containerList | ForEach-Object { Write-Info "  • $_" }
        }
        else {
            Write-Info "No containers currently connected to accessibility-shared network"
        }
        return $true
    }
    catch {
        Write-Warning "Could not inspect network connectivity: $($_.Exception.Message)"
        return $true  # No es un error crítico
    }
}

# ===========================================
# FUNCIONES PRINCIPALES
# ===========================================

function Invoke-TestAction {
    Write-Header "TESTING - $TestType Tests"
    
    if (-not (Test-Path $TestSolutionFile)) {
        Write-Error "Test solution not found: $TestSolutionFile"
        return 1
    }
    
    $testCommand = "dotnet test `"$TestSolutionFile`" --configuration $Configuration"
    
    switch ($TestType) {
        'Unit' { 
            Write-Step "Running Unit Tests..."
            $testCommand += " --filter Category=Unit"
        }
        'Integration' { 
            Write-Step "Running Integration Tests..."
            $testCommand += " --filter Category=Integration"
        }
        'Performance' { 
            Write-Step "Running Performance Tests..."
            $testCommand += " --filter Category=Performance"
        }
        default { 
            Write-Step "Running All Tests..."
        }
    }
    
    if ($GenerateCoverage) {
        Write-Step "Generating coverage report..."
        $testCommand += " --collect:`"XPlat Code Coverage`" --results-directory TestResults"
    }
    
    Write-Info "Executing: $testCommand"
    Invoke-Expression $testCommand
    
    if ($LASTEXITCODE -eq 0) {
        Write-Success "Tests completed successfully!"
        
        if ($GenerateCoverage -and $OpenReport) {
            $coverageFile = Get-ChildItem -Path "TestResults" -Filter "coverage.cobertura.xml" -Recurse | Select-Object -First 1
            if ($coverageFile) {
                Write-Step "Opening coverage report..."
                Start-Process $coverageFile.FullName
            }
        }
    }
    else {
        Write-Error "Tests failed with exit code: $LASTEXITCODE"
        return $LASTEXITCODE
    }
}

function Invoke-BuildAction {
    Write-Header "BUILDING - $BuildType Build"
    
    if ($Clean) {
        Write-Step "Cleaning previous builds..."
        dotnet clean $SolutionFile --configuration $Configuration
    }
    
    switch ($BuildType) {
        'standard' {
            Write-Step "Building standard..."
            dotnet restore $SolutionFile
            dotnet build $SolutionFile --configuration $Configuration --no-restore
        }
        'production' {
            Write-Step "Building for production..."
            dotnet restore $SolutionFile
            dotnet build $SolutionFile --configuration Release --no-restore
            dotnet publish "$ProjectPath/$script:ProjectName.csproj" -c Release -o "publish"
        }
        'docker' {
            Write-Step "Building Docker image..."
            $dockerCommand = "docker build -t $($script:ImageName):$Version"
            if ($Configuration -eq 'Debug') {
                $dockerCommand += " --build-arg BUILD_CONFIGURATION=Debug"
            }
            $dockerCommand += " ."
            
            Write-Info "Executing: $dockerCommand"
            Invoke-Expression $dockerCommand
            
            if ($LASTEXITCODE -eq 0 -and $Push -and $Registry) {
                Write-Step "Pushing to registry..."
                $taggedImage = "$Registry/$($script:ImageName):$Version"
                docker tag "$($script:ImageName):$Version" $taggedImage
                docker push $taggedImage
            }
        }
    }
    
    if ($LASTEXITCODE -eq 0) {
        Write-Success "Build completed successfully!"
    }
    else {
        Write-Error "Build failed with exit code: $LASTEXITCODE"
        return $LASTEXITCODE
    }
}

function Invoke-DockerAction {
    Write-Header "DOCKER MANAGEMENT - $SubAction"
    
    if (-not (Test-DockerRunning)) {
        Write-Error "Docker is not running. Please start Docker Desktop."
        return 1
    }
    
    # Verificar red requerida antes de cualquier acción de Docker Compose
    if ($SubAction -eq 'up' -or $SubAction -eq 'restart') {
        if (-not (Test-RequiredNetwork)) {
            Write-Error "Required Docker network validation failed. Cannot proceed with $SubAction."
            return 1
        }
    }
    
    $composeFile = if ($Environment -eq 'dev') { 'docker-compose.dev.yml' } else { 'docker-compose.yml' }
    
    switch ($SubAction) {
        'up' {
            Write-Step "Starting containers ($Environment environment)..."
            
            # Check for port conflicts before starting
            $port = if ($Environment -eq 'dev') { '8101' } else { '8100' }
            $portInUse = netstat -an | findstr ":$port"
            if ($portInUse) {
                Write-Warning "Port $port is already in use. Attempting to resolve conflicts..."
                # Try to stop existing containers that might be using the port
                docker-compose -f $composeFile down --remove-orphans 2>$null
                docker-compose down --remove-orphans 2>$null
            }
            
            $cmd = "docker-compose -f $composeFile up -d --remove-orphans"
            if ($Rebuild) { $cmd += " --build" }
            Invoke-Expression $cmd
            
            # Verificar conectividad después de levantar contenedores
            if ($LASTEXITCODE -eq 0) {
                Start-Sleep -Seconds 3  # Dar tiempo a que los contenedores se conecten
                Test-NetworkConnectivity
            }
        }
        'down' {
            Write-Step "Stopping containers..."
            Invoke-Expression "docker-compose -f $composeFile down"
        }
        'logs' {
            Write-Step "Showing logs..."
            $cmd = "docker-compose -f $composeFile logs"
            if ($Follow) { $cmd += " -f" }
            Invoke-Expression $cmd
        }
        'status' {
            Write-Step "Container status..."
            Invoke-Expression "docker-compose -f $composeFile ps"
        }
        'restart' {
            Write-Step "Restarting containers..."
            Invoke-Expression "docker-compose -f $composeFile restart"
        }
        default {
            Write-Error "Unknown docker action: $SubAction"
            Write-Info "Available actions: up, down, logs, status, restart"
            return 1
        }
    }
    
    if ($LASTEXITCODE -eq 0) {
        Write-Success "Docker action completed successfully!"
    }
    else {
        Write-Error "Docker action failed with exit code: $LASTEXITCODE"
        return $LASTEXITCODE
    }
}

function Invoke-VerifyAction {
    Write-Header "VERIFICATION - System Health Check"
    
    $checks = @()
    
    # Check .NET SDK
    Write-Step "Checking .NET SDK..."
    if (Test-CommandExists 'dotnet') {
        $dotnetVersion = dotnet --version
        Write-Success ".NET SDK: $dotnetVersion"
        $checks += @{ Name = ".NET SDK"; Status = "✅"; Details = $dotnetVersion }
    }
    else {
        Write-Error ".NET SDK not found"
        $checks += @{ Name = ".NET SDK"; Status = "❌"; Details = "Not installed" }
    }
    
    # Check Docker
    Write-Step "Checking Docker..."
    if (Test-DockerRunning) {
        $dockerVersion = docker version --format '{{.Server.Version}}'
        Write-Success "Docker: $dockerVersion"
        $checks += @{ Name = "Docker"; Status = "✅"; Details = $dockerVersion }
        
        # Check Docker network when Docker is running
        Write-Step "Checking Docker network..."
        if (Test-RequiredNetwork) {
            $checks += @{ Name = "Docker Network"; Status = "✅"; Details = "accessibility-shared ready" }
            
            # Test network connectivity if Full verification is requested
            if ($Full) {
                Test-NetworkConnectivity
                $checks += @{ Name = "Network Connectivity"; Status = "✅"; Details = "Verified" }
            }
        }
        else {
            $checks += @{ Name = "Docker Network"; Status = "❌"; Details = "accessibility-shared missing" }
        }
    }
    else {
        Write-Error "Docker not running"
        $checks += @{ Name = "Docker"; Status = "❌"; Details = "Not running" }
        $checks += @{ Name = "Docker Network"; Status = "⚠️"; Details = "Cannot verify (Docker not running)" }
    }
    
    # Check project files
    Write-Step "Checking project structure..."
    if (Test-Path $SolutionFile) {
        Write-Success "Solution file found"
        $checks += @{ Name = "Solution"; Status = "✅"; Details = $SolutionFile }
    }
    else {
        Write-Error "Solution file not found"
        $checks += @{ Name = "Solution"; Status = "❌"; Details = "Missing" }
    }
    
    if ($Full) {
        Write-Step "Running full verification..."
        
        # Test build
        Write-Step "Testing build..."
        dotnet build $SolutionFile --configuration $Configuration --verbosity quiet
        if ($LASTEXITCODE -eq 0) {
            Write-Success "Build test passed"
            $checks += @{ Name = "Build Test"; Status = "✅"; Details = "Compiled successfully" }
        }
        else {
            Write-Error "Build test failed"
            $checks += @{ Name = "Build Test"; Status = "❌"; Details = "Compilation errors" }
        }
        
        # Test basic tests
        if (Test-Path $script:TestBasicPath) {
            Write-Step "Running basic tests..."
            dotnet test $script:TestBasicPath --verbosity quiet
            if ($LASTEXITCODE -eq 0) {
                Write-Success "Basic tests passed"
                $checks += @{ Name = "Basic Tests"; Status = "✅"; Details = "All tests passed" }
            }
            else {
                Write-Error "Basic tests failed"
                $checks += @{ Name = "Basic Tests"; Status = "❌"; Details = "Some tests failed" }
            }
        }
        else {
            Write-Warning "Basic tests not found at: $script:TestBasicPath"
            $checks += @{ Name = "Basic Tests"; Status = "⚠️"; Details = "Test project not found" }
        }
    }
    
    # Summary
    Write-Header "VERIFICATION SUMMARY"
    $checks | ForEach-Object {
        Write-Host "   $($_.Status) $($_.Name): $($_.Details)"
    }
    
    $failed = $checks | Where-Object { $_.Status -eq "❌" }
    if ($failed.Count -eq 0) {
        Write-Success "All checks passed! ✨"
        return 0
    }
    else {
        Write-Error "$($failed.Count) checks failed"
        return 1
    }
}

function Invoke-CleanupAction {
    Write-Header "CLEANUP - System Maintenance"
    
    if ($Docker) {
        Write-Step "Cleaning Docker resources..."
        
        # Stop containers
        docker-compose -f docker-compose.yml down 2>$null
        docker-compose -f docker-compose.dev.yml down 2>$null
        
        # Remove containers
        $containers = docker ps -a --filter "name=accessibility" --format "{{.ID}}"
        if ($containers) {
            Write-Step "Removing accessibility containers..."
            $containers | ForEach-Object { docker rm $_ -f }
        }
        
        # Remove images
        $images = docker images "$script:ImageName" --format "{{.ID}}"
        if ($images) {
            Write-Step "Removing gateway images..."
            $images | ForEach-Object { docker rmi $_ -f }
        }
        
        # Network cleanup (only if All is specified to avoid breaking other services)
        if ($All) {
            Write-Step "Checking network cleanup..."
            try {
                # Verificar si hay contenedores conectados a la red
                $connectedContainers = docker network inspect accessibility-shared --format "{{range .Containers}}{{.Name}} {{end}}" 2>$null
                if ($connectedContainers -and $connectedContainers.Trim()) {
                    $containerList = $connectedContainers.Trim() -split '\s+'
                    Write-Warning "Network 'accessibility-shared' has $($containerList.Count) connected containers:"
                    $containerList | ForEach-Object { Write-Info "  • $_" }
                    Write-Info "Network will not be removed to avoid disrupting other services."
                    Write-Info "Use 'docker network disconnect' manually if needed."
                }
                else {
                    Write-Step "Removing empty accessibility-shared network..."
                    docker network rm accessibility-shared 2>$null
                    if ($LASTEXITCODE -eq 0) {
                        Write-Success "Network 'accessibility-shared' removed"
                    }
                    else {
                        Write-Info "Network 'accessibility-shared' not found or couldn't be removed"
                    }
                }
            }
            catch {
                Write-Warning "Could not check/cleanup network: $($_.Exception.Message)"
            }
        }
        
        if ($Volumes) {
            Write-Step "Removing volumes..."
            docker volume prune -f
        }
        
        if ($All) {
            Write-Step "Comprehensive Docker cleanup..."
            docker system prune -a -f
        }
    }
    
    # Clean build artifacts
    Write-Step "Cleaning build artifacts..."
    Get-ChildItem -Path . -Recurse -Directory -Name "bin", "obj" | ForEach-Object {
        $path = Join-Path (Get-Location) $_
        if (Test-Path $path) {
            Remove-Item $path -Recurse -Force -ErrorAction SilentlyContinue
        }
    }
    
    # Clean test results
    if (Test-Path "TestResults") {
        Write-Step "Cleaning test results..."
        Remove-Item "TestResults" -Recurse -Force -ErrorAction SilentlyContinue
    }
    
    # Clean logs
    if (Test-Path "logs") {
        Write-Step "Cleaning logs..."
        Remove-Item "logs" -Recurse -Force -ErrorAction SilentlyContinue
    }
    
    Write-Success "Cleanup completed!"
}

function Invoke-RunAction {
    Write-Header "LOCAL GATEWAY - Starting Development Server"
    
    # Verificar que el proyecto existe
    $projectFile = "$ProjectPath/Gateway.csproj"
    if (-not (Test-Path $projectFile)) {
        Write-Error "Project file not found: $projectFile"
        return 1
    }
    
    # Configurar variables de entorno
    Write-Step "Configuring environment..."
    $env:ASPNETCORE_ENVIRONMENT = $AspNetCoreEnvironment
    $env:ASPNETCORE_URLS = "http://localhost:$Port"
    
    Write-Info "Environment: $AspNetCoreEnvironment"
    Write-Info "Port: $Port"
    Write-Info "URLs: $($env:ASPNETCORE_URLS)"
    
    # Limpiar procesos dotnet previos si existen
    Write-Step "Cleaning previous dotnet processes..."
    try {
        $dotnetProcesses = Get-Process -Name "dotnet" -ErrorAction SilentlyContinue
        if ($dotnetProcesses) {
            Write-Warning "Found $($dotnetProcesses.Count) dotnet processes. Stopping them..."
            $dotnetProcesses | Stop-Process -Force -ErrorAction SilentlyContinue
            Start-Sleep -Seconds 2
            Write-Success "Previous dotnet processes cleaned"
        }
        else {
            Write-Info "No previous dotnet processes found"
        }
    }
    catch {
        Write-Warning "Could not clean previous processes: $($_.Exception.Message)"
    }
    
    # Restaurar dependencias si es necesario
    Write-Step "Restoring project dependencies..."
    Set-Location $ProjectPath
    dotnet restore
    
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Failed to restore dependencies"
        Set-Location $PSScriptRoot
        return $LASTEXITCODE
    }
    
    Write-Success "Dependencies restored successfully"
    
    # Ejecutar el proyecto
    Write-Step "Starting Gateway server..."
    Write-Info "Press Ctrl+C to stop the server"
    Write-Info "Gateway will be available at: http://localhost:$Port"
    Write-Info "Swagger UI: http://localhost:$Port/swagger"
    
    if ($NoLaunch) {
        Write-Warning "Auto-launch disabled. Gateway running on http://localhost:$Port"
    }
    else {
        Write-Step "Will auto-launch browser in 5 seconds..."
        Start-Sleep -Seconds 1
        Write-Host "   5..." -ForegroundColor Yellow
        Start-Sleep -Seconds 1
        Write-Host "   4..." -ForegroundColor Yellow
        Start-Sleep -Seconds 1
        Write-Host "   3..." -ForegroundColor Yellow
        Start-Sleep -Seconds 1
        Write-Host "   2..." -ForegroundColor Yellow
        Start-Sleep -Seconds 1
        Write-Host "   1..." -ForegroundColor Yellow
        
        # Abrir navegador en background
        Start-Process "http://localhost:$Port/swagger" -ErrorAction SilentlyContinue
    }
    
    # Ejecutar dotnet run
    Write-Success "🚀 Gateway starting..."
    try {
        dotnet run --project Gateway.csproj --configuration $Configuration
    }
    finally {
        # Regresar al directorio original
        Set-Location $PSScriptRoot
        Write-Info "Returned to script directory"
    }
}

function Show-Help {
    Write-Host @"

🚪 ACCESSIBILITY GATEWAY - MANAGEMENT SCRIPT
============================================

COMPLEMENTO AL SCRIPT PRINCIPAL MANAGE.PS1
------------------------------------------
Este script se enfoca específicamente en el desarrollo y gestión 
del Gateway (.NET), mientras que manage.ps1 orquesta todo el ecosistema.

USAGE:
    .\manage-gateway.ps1 <action> [parameters]

ACTIONS:
    test        Run tests (Unit, Integration, Performance)  
    build       Build project (standard, production, docker)
    verify      Verify system health and dependencies       
    docker      Manage Docker containers (up, down, logs, status)
    cleanup     Clean build artifacts and Docker resources  
    run         Start local development server (.NET specific)
    consistency Check system consistency and port conflicts
    status      Quick project status overview
    help        Show this help message

EJEMPLOS DE DESARROLLO:

📋 TESTING:
    .\manage-gateway.ps1 test
    .\manage-gateway.ps1 test -TestType Unit -GenerateCoverage
    .\manage-gateway.ps1 test -TestType Integration

🔨 BUILDING:
    .\manage-gateway.ps1 build
    .\manage-gateway.ps1 build -Configuration Release -BuildType production
    .\manage-gateway.ps1 build -BuildType docker -Push -Registry myregistry.com

🐳 DOCKER:
    .\manage-gateway.ps1 docker up          # Auto-verifica/crea red accessibility-shared
    .\manage-gateway.ps1 docker up -Environment dev
    .\manage-gateway.ps1 docker logs -Follow
    .\manage-gateway.ps1 docker status
    .\manage-gateway.ps1 docker down

🔍 VERIFICATION:
    .\manage-gateway.ps1 verify              # Incluye verificación de red Docker
    .\manage-gateway.ps1 verify -Full        # + tests de conectividad de red

🧹 CLEANUP:
    .\manage-gateway.ps1 cleanup -Docker
    .\manage-gateway.ps1 cleanup -Docker -Volumes
    .\manage-gateway.ps1 cleanup -All        # Incluye limpieza de red (si está vacía)

🚀 LOCAL RUN (.NET DEVELOPMENT):
    .\manage-gateway.ps1 run
    .\manage-gateway.ps1 run -Port 8085
    .\manage-gateway.ps1 run -AspNetCoreEnvironment Production -NoLaunch

💡 COORDINACIÓN CON MIDDLEWARE:
    # Para despliegue completo del ecosistema:
    ..\accessibility-mw\manage.ps1 deploy-all
    
    # Para desarrollo local del Gateway únicamente:
    .\manage-gateway.ps1 run -Port 8100

PARAMETERS:
    -Configuration    Debug|Release (default: Release)
    -Environment      dev|prod|test (default: prod)
    -TestType         All|Unit|Integration|Performance (default: All)
    -BuildType        standard|production|docker (default: standard)
    -GenerateCoverage Generate test coverage report
    -Follow           Follow logs in real-time
    -Full             Full verification including build and tests
    -Docker           Include Docker cleanup
    -Volumes          Include volume cleanup
    -All              Comprehensive cleanup
    -Port             Port for local server (default: 8100)
    -AspNetCoreEnvironment  Development|Production|Staging (default: Development)
    -NoLaunch         Disable auto browser launch

"@ -ForegroundColor Green
}

function Get-ProjectStatus {
    Write-Header "PROJECT STATUS - Quick Overview"
    
    # 1. Basic Info
    Write-Step "Project Information..."
    Write-Info "📁 Project: $script:ProjectName"
    Write-Info "🏗️ Solution: $script:SolutionFile"
    Write-Info "📂 Source: $script:ProjectPath"
    
    # 2. Build Status  
    Write-Step "Build Status..."
    try {
        dotnet build $script:SolutionFile --verbosity quiet --no-restore 2>$null
        if ($LASTEXITCODE -eq 0) {
            Write-Success "✅ Build: SUCCESSFUL"
        }
        else {
            Write-Warning "⚠️ Build: ISSUES DETECTED"
        }
    }
    catch {
        Write-Error "❌ Build: FAILED"
    }
    
    # 3. Port Status
    Write-Step "Port Status..."
    $prodPort = netstat -an 2>$null | findstr ":8100"
    $devPort = netstat -an 2>$null | findstr ":8101"
    
    if ($prodPort) { Write-Success "✅ Port 8100: IN USE (Production)" }
    else { Write-Info "ℹ️ Port 8100: Available" }
    
    if ($devPort) { Write-Success "✅ Port 8101: IN USE (Development)" }
    else { Write-Info "ℹ️ Port 8101: Available" }
    
    # 4. Docker Status
    Write-Step "Docker Status..."
    if (Test-DockerRunning) {
        $containers = docker ps --filter "name=accessibility-gateway" --format "{{.Names}}: {{.Status}}" 2>$null
        if ($containers) {
            $containers | ForEach-Object { Write-Success "✅ $_" }
        }
        else {
            Write-Info "ℹ️ No Gateway containers running"
        }
    }
    else {
        Write-Warning "⚠️ Docker: NOT RUNNING"
    }
    
    # 5. Dependencies
    Write-Step "Dependencies..."
    if (Test-CommandExists 'dotnet') {
        $dotnetVersion = dotnet --version
        Write-Success "✅ .NET SDK: $dotnetVersion"
    }
    
    Write-Success "Status check completed! 🎯"
}

function Test-SystemConsistency {
    Write-Header "SYSTEM CONSISTENCY CHECK"
    
    Write-Step "Checking port availability..."
    $prodPort = netstat -an | findstr ":8100"
    $devPort = netstat -an | findstr ":8101"
    
    # Check production port (now default)
    if ($prodPort) {
        Write-Success "✅ Production port 8100: IN USE (Production Gateway running)"
    }
    else {
        Write-Info "ℹ️ Production port 8100: Available"
    }
    
    # Check development port (optional)
    if ($devPort) {
        Write-Success "✅ Development port 8101: IN USE (Development Gateway running)"
    }
    else {
        Write-Info "ℹ️ Development port 8101: Available (Development mode not active)"
    }
    
    Write-Step "Checking container status..."
    $containers = docker ps --filter "name=accessibility" --format "{{.Names}},{{.Status}},{{.Ports}}"
    
    foreach ($container in $containers) {
        if ($container -match "accessibility-gateway.*healthy.*8100") {
            Write-Success "✅ Production Gateway: HEALTHY on port 8100"
        }
        elseif ($container -match "accessibility-gw-dev.*healthy.*8101") {
            Write-Success "✅ Development Gateway: HEALTHY on port 8101"
        }
        elseif ($container -match "accessibility-mw-prod.*healthy.*3001") {
            Write-Success "✅ Middleware: HEALTHY on port 3001"
        }
        elseif ($container -match "accessibility-redis.*healthy") {
            Write-Success "✅ Redis: HEALTHY"
        }
    }
    
    Write-Step "Testing Gateway endpoints..."
    try {
        $healthCheck = Invoke-RestMethod -Uri "http://localhost:8100/health" -Method Get -TimeoutSec 5 2>$null
        if ($healthCheck.status -eq "Healthy") {
            Write-Success "✅ Gateway Health Check: PASSED"
        }
    }
    catch {
        try {
            $healthCheck = Invoke-RestMethod -Uri "http://localhost:8101/health" -Method Get -TimeoutSec 5 2>$null
            if ($healthCheck.status -eq "Healthy") {
                Write-Success "✅ Dev Gateway Health Check: PASSED"
            }
        }
        catch {
            Write-Warning "⚠️ No Gateway responding on expected ports"
        }
    }
    
    Write-Step "Integration with middleware verification..."
    try {
        $middlewareTest = Invoke-RestMethod -Uri "http://localhost:3001/health" -Method Get -TimeoutSec 5 2>$null
        Write-Success "✅ Middleware: ACCESSIBLE"
        Write-Info "  Status: $($middlewareTest.status)"
    }
    catch {
        Write-Warning "⚠️ Middleware: NOT ACCESSIBLE - $($_.Exception.Message)"
    }
    
    Write-Success "System consistency check completed!"
    return 0
}

# ===========================================
# MAIN EXECUTION
# ===========================================

try {
    switch ($Action) {
        'test' { exit (Invoke-TestAction) }
        'build' { exit (Invoke-BuildAction) }
        'verify' { exit (Invoke-VerifyAction) }
        'docker' { exit (Invoke-DockerAction) }
        'cleanup' { exit (Invoke-CleanupAction) }
        'run' { exit (Invoke-RunAction) }
        'consistency' { exit (Test-SystemConsistency) }
        'status' { exit (Get-ProjectStatus) }
        'help' { Show-Help; exit 0 }
        default { 
            Write-Error "Unknown action: $Action"
            Show-Help
            exit 1
        }
    }
}
catch {
    Write-Error "Script execution failed: $($_.Exception.Message)"
    exit 1
}