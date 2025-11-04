#!/usr/bin/env pwsh

<#
.SYNOPSIS
    Script para gestión de red Docker y validación de puertos del ecosistema Accessibility

.DESCRIPTION
    Script minimalista que proporciona:
    - Creación automática de la red accessibility-shared si no existe
    - Validación de contenedores conectados a la red
    - Verificación de conflictos de puertos antes de levantar contenedores

.PARAMETER Action
    Acción a ejecutar: check, validate, create, status, help

.EXAMPLES
    .\manage-network.ps1 check
    .\manage-network.ps1 validate -Port 8100
    .\manage-network.ps1 create
    .\manage-network.ps1 status
#>

param(
    [Parameter(Position = 0)]
    [ValidateSet('check', 'validate', 'create', 'status', 'help')]
    [string]$Action = 'status',
    
    [Parameter()]
    [int]$Port = 0
)

$ErrorActionPreference = 'Continue'

# ===========================================
# CONFIGURACIÓN
# ===========================================

$NetworkName = "accessibility-shared"
$NetworkSubnet = "172.18.0.0/16"
$NetworkGateway = "172.18.0.1"

# Puertos del ecosistema
$KnownPorts = @{
    8100 = "Gateway"
    8081 = "Users Microservice"
    8082 = "Analysis Microservice"
    8083 = "Reports Microservice"
    3001 = "Middleware"
    6379 = "Redis"
    9090 = "Prometheus"
    9093 = "Alertmanager"
    3010 = "Grafana"
}

# ===========================================
# FUNCIONES AUXILIARES
# ===========================================

function Write-Header { param($msg) Write-Host "`n🌐 $msg" -ForegroundColor Cyan }
function Write-Success { param($msg) Write-Host "   ✅ $msg" -ForegroundColor Green }
function Write-Error { param($msg) Write-Host "   ❌ $msg" -ForegroundColor Red }
function Write-Warning { param($msg) Write-Host "   ⚠️  $msg" -ForegroundColor Yellow }
function Write-Info { param($msg) Write-Host "   ℹ️  $msg" -ForegroundColor Blue }

function Test-DockerRunning {
    try {
        $result = docker version --format '{{.Server.Version}}' 2>$null
        return $null -ne $result
    }
    catch {
        return $false
    }
}

# ===========================================
# FUNCIONES PRINCIPALES
# ===========================================

function Test-RequiredNetwork {
    <#
    .SYNOPSIS
        Verifica y crea la red accessibility-shared si no existe
    #>
    
    Write-Header "NETWORK VALIDATION"
    
    # Verificar Docker
    if (-not (Test-DockerRunning)) {
        Write-Error "Docker is not running. Please start Docker Desktop."
        return $false
    }
    
    Write-Success "Docker is running"
    
    # Verificar si la red existe
    try {
        $network = docker network ls --filter name=^${NetworkName}$ --format "{{.Name}}" 2>$null
        
        if (-not $network -or $network -ne $NetworkName) {
            Write-Warning "Network '$NetworkName' not found. Creating..."
            
            # Crear la red con configuración específica
            $createResult = docker network create `
                --driver bridge `
                --subnet=$NetworkSubnet `
                --gateway=$NetworkGateway `
                $NetworkName 2>&1
            
            if ($LASTEXITCODE -eq 0) {
                Write-Success "Network '$NetworkName' created successfully"
                Write-Info "Driver: bridge"
                Write-Info "Subnet: $NetworkSubnet"
                Write-Info "Gateway: $NetworkGateway"
                return $true
            } 
            else {
                Write-Error "Failed to create network '$NetworkName'"
                Write-Error "$createResult"
                return $false
            }
        } 
        else {
            Write-Success "Network '$NetworkName' exists and is ready"
            
            # Mostrar información de la red
            try {
                $networkInfo = docker network inspect $NetworkName --format "{{.Driver}} | {{range .IPAM.Config}}{{.Subnet}}{{end}}" 2>$null
                if ($networkInfo) {
                    Write-Info "Configuration: $networkInfo"
                }
            }
            catch {
                # Silenciar errores, no es crítico
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
    <#
    .SYNOPSIS
        Valida y muestra los contenedores conectados a la red
    #>
    
    Write-Header "NETWORK CONNECTIVITY CHECK"
    
    if (-not (Test-DockerRunning)) {
        Write-Warning "Docker not running - cannot check connectivity"
        return $false
    }
    
    # Verificar si la red existe
    $networkExists = docker network ls --filter name=^${NetworkName}$ --format "{{.Name}}" 2>$null
    if (-not $networkExists -or $networkExists -ne $NetworkName) {
        Write-Error "Network '$NetworkName' does not exist"
        Write-Info "Run: .\manage-network.ps1 create"
        return $false
    }
    
    # Obtener contenedores conectados
    try {
        $connectedData = docker network inspect $NetworkName --format "{{range .Containers}}{{.Name}}|{{.IPv4Address}}||{{end}}" 2>$null
        
        if ($connectedData -and $connectedData.Trim()) {
            $containers = $connectedData.Trim() -split '\|\|' | Where-Object { $_ }
            
            Write-Success "Found $($containers.Count) container(s) connected to '$NetworkName':"
            Write-Host ""
            
            foreach ($container in $containers) {
                if ($container) {
                    $parts = $container -split '\|'
                    $name = $parts[0]
                    $ip = $parts[1]
                    Write-Host "   📦 $name" -ForegroundColor Green
                    Write-Info "   IP: $ip"
                }
            }
            
            return $true
        }
        else {
            Write-Warning "No containers currently connected to '$NetworkName'"
            Write-Info "Containers will connect when started with docker-compose"
            return $true
        }
    }
    catch {
        Write-Error "Could not inspect network connectivity: $($_.Exception.Message)"
        return $false
    }
}

function Test-PortConflict {
    <#
    .SYNOPSIS
        Verifica conflictos en puertos específicos o todos los puertos conocidos
    #>
    
    param(
        [int]$SpecificPort = 0
    )
    
    Write-Header "PORT CONFLICT CHECK"
    
    $conflicts = @()
    $portsToCheck = if ($SpecificPort -gt 0) { 
        @{ $SpecificPort = "Specified Port" } 
    } 
    else { 
        $KnownPorts 
    }
    
    foreach ($port in $portsToCheck.Keys) {
        $portInUse = netstat -an 2>$null | findstr ":$port "
        
        if ($portInUse) {
            $service = $portsToCheck[$port]
            Write-Warning "Port $port ($service) is IN USE"
            
            # Intentar identificar el proceso
            try {
                $process = Get-NetTCPConnection -LocalPort $port -ErrorAction SilentlyContinue | 
                    Select-Object -First 1 -ExpandProperty OwningProcess
                
                if ($process) {
                    $processInfo = Get-Process -Id $process -ErrorAction SilentlyContinue
                    if ($processInfo) {
                        Write-Info "Process: $($processInfo.ProcessName) (PID: $process)"
                    }
                }
            }
            catch {
                # Silenciar errores
            }
            
            $conflicts += @{
                Port = $port
                Service = $service
                InUse = $true
            }
        }
        else {
            Write-Success "Port $port ($($portsToCheck[$port])) is available"
        }
    }
    
    Write-Host ""
    
    if ($conflicts.Count -gt 0) {
        Write-Warning "$($conflicts.Count) port conflict(s) detected"
        Write-Info "Stop conflicting services before starting containers"
        return $false
    }
    else {
        Write-Success "No port conflicts detected"
        return $true
    }
}

function Show-NetworkStatus {
    <#
    .SYNOPSIS
        Muestra el estado completo de la red y puertos
    #>
    
    Write-Header "ACCESSIBILITY NETWORK STATUS"
    Write-Host ""
    
    # 1. Docker Status
    Write-Host "📊 Docker Status:" -ForegroundColor Cyan
    if (Test-DockerRunning) {
        $dockerVersion = docker version --format '{{.Server.Version}}' 2>$null
        Write-Success "Docker Engine: $dockerVersion"
    }
    else {
        Write-Error "Docker is not running"
        return
    }
    
    Write-Host ""
    
    # 2. Network Status
    Write-Host "🌐 Network Status:" -ForegroundColor Cyan
    $networkExists = docker network ls --filter name=^${NetworkName}$ --format "{{.Name}}" 2>$null
    
    if ($networkExists -eq $NetworkName) {
        Write-Success "Network '$NetworkName' exists"
        
        # Info detallada
        $networkInfo = docker network inspect $NetworkName --format "Driver: {{.Driver}} | Subnet: {{range .IPAM.Config}}{{.Subnet}}{{end}}" 2>$null
        Write-Info "$networkInfo"
        
        # Contenedores conectados
        $containerCount = docker network inspect $NetworkName --format "{{len .Containers}}" 2>$null
        Write-Info "Connected containers: $containerCount"
    }
    else {
        Write-Warning "Network '$NetworkName' does not exist"
        Write-Info "Run: .\manage-network.ps1 create"
    }
    
    Write-Host ""
    
    # 3. Port Status
    Write-Host "🔌 Port Status:" -ForegroundColor Cyan
    $inUseCount = 0
    
    foreach ($port in $KnownPorts.Keys | Sort-Object) {
        $portInUse = netstat -an 2>$null | findstr ":$port "
        
        if ($portInUse) {
            Write-Host "   🟢 Port $port" -NoNewline -ForegroundColor Green
            Write-Host " ($($KnownPorts[$port]))" -ForegroundColor Gray
            $inUseCount++
        }
        else {
            Write-Host "   ⚪ Port $port" -NoNewline -ForegroundColor Gray
            Write-Host " ($($KnownPorts[$port]))" -ForegroundColor DarkGray
        }
    }
    
    Write-Host ""
    Write-Info "$inUseCount of $($KnownPorts.Count) ports are in use"
    
    # 4. Resumen
    Write-Host ""
    Write-Host "📋 Quick Actions:" -ForegroundColor Cyan
    Write-Info "Check network:  .\manage-network.ps1 check"
    Write-Info "Validate ports: .\manage-network.ps1 validate"
    Write-Info "Full status:    .\manage-network.ps1 status"
}

function Show-Help {
    Write-Host @"

🌐 ACCESSIBILITY NETWORK MANAGER
=================================

Script minimalista para gestión de red Docker y validación de puertos.

USAGE:
    .\manage-network.ps1 <action> [parameters]

ACTIONS:
    check       Verify and create network if needed
    validate    Check for port conflicts (specific or all)
    create      Create the accessibility-shared network
    status      Show complete network and port status
    help        Show this help message

EXAMPLES:
    .\manage-network.ps1 check
    Verifica la red y la crea si no existe

    .\manage-network.ps1 validate
    Verifica conflictos en todos los puertos conocidos

    .\manage-network.ps1 validate -Port 8100
    Verifica conflicto en un puerto específico

    .\manage-network.ps1 status
    Muestra estado completo de red y puertos

    .\manage-network.ps1 create
    Crea la red accessibility-shared manualmente

NETWORK CONFIGURATION:
    Name:    $NetworkName
    Driver:  bridge
    Subnet:  $NetworkSubnet
    Gateway: $NetworkGateway

KNOWN PORTS:
"@ -ForegroundColor Green

    foreach ($port in $KnownPorts.Keys | Sort-Object) {
        Write-Host "    $port  - $($KnownPorts[$port])" -ForegroundColor Cyan
    }
    
    Write-Host ""
}

# ===========================================
# MAIN EXECUTION
# ===========================================

try {
    switch ($Action) {
        'check' {
            Test-RequiredNetwork
        }
        'validate' {
            if ($Port -gt 0) {
                Test-PortConflict -SpecificPort $Port
            }
            else {
                Test-PortConflict
            }
        }
        'create' {
            Test-RequiredNetwork
        }
        'status' {
            Show-NetworkStatus
        }
        'help' {
            Show-Help
        }
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
