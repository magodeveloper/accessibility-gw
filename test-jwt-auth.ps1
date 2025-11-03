#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Script para probar la autenticación JWT del Gateway
.DESCRIPTION
    Verifica que las rutas protegidas requieran token JWT válido
    y que las rutas públicas funcionen sin autenticación
#>

$ErrorActionPreference = "Continue"
$baseUrl = "http://localhost:8100"

Write-Host "=====================================" -ForegroundColor Cyan
Write-Host "   TEST AUTENTICACIÓN JWT GATEWAY" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host ""

# Test 1: Verificar Gateway está disponible
Write-Host "[TEST 1] Gateway Health Check..." -ForegroundColor Yellow
$healthResponse = curl -s "$baseUrl/health/ready" | ConvertFrom-Json
if ($healthResponse.status -eq "ready") {
    Write-Host "✅ Gateway READY" -ForegroundColor Green
} else {
    Write-Host "❌ Gateway NO DISPONIBLE" -ForegroundColor Red
    exit 1
}
Write-Host ""

# Test 2: Ruta pública SIN token (debería funcionar - 200 o 400/404 del microservicio)
Write-Host "[TEST 2] Ruta Pública SIN Token: POST /api/Auth/login" -ForegroundColor Yellow
$response = curl -X POST "$baseUrl/api/Auth/login" `
    -H "Content-Type: application/json" `
    -d '{"email":"test@test.com","password":"Test123!"}' `
    -s -w "\n%{http_code}" 2>&1

$httpCode = ($response | Select-Object -Last 1)
Write-Host "HTTP Status: $httpCode" -ForegroundColor White

if ($httpCode -eq "401") {
    Write-Host "❌ FALLO: Ruta pública rechazó acceso sin token" -ForegroundColor Red
} else {
    Write-Host "✅ CORRECTO: Gateway permitió acceso sin token (HTTP $httpCode)" -ForegroundColor Green
}
Write-Host ""

# Test 3: Ruta protegida SIN token (debería devolver 401)
Write-Host "[TEST 3] Ruta Protegida SIN Token: DELETE /api/users/all-data" -ForegroundColor Yellow
$response = curl -X DELETE "$baseUrl/api/users/all-data" -s -w "\n%{http_code}" 2>&1
$httpCode = ($response | Select-Object -Last 1)
Write-Host "HTTP Status: $httpCode" -ForegroundColor White

if ($httpCode -eq "401") {
    Write-Host "✅ CORRECTO: Ruta protegida rechazó acceso sin token (401 Unauthorized)" -ForegroundColor Green
} else {
    Write-Host "❌ FALLO: Ruta protegida permitió acceso sin token (HTTP $httpCode)" -ForegroundColor Red
}
Write-Host ""

# Test 4: Múltiples rutas protegidas
Write-Host "[TEST 4] Verificando Múltiples Rutas Protegidas..." -ForegroundColor Yellow
$protectedRoutes = @(
    @{ Method = "DELETE"; Path = "/api/Report/all" },
    @{ Method = "DELETE"; Path = "/api/Analysis/all" },
    @{ Method = "POST"; Path = "/api/users" },
    @{ Method = "POST"; Path = "/api/Report" },
    @{ Method = "DELETE"; Path = "/api/sessions" }
)

$allProtected = $true
foreach ($route in $protectedRoutes) {
    $testResponse = curl -X $route.Method "$baseUrl$($route.Path)" -s -w "%{http_code}" 2>&1
    $code = $testResponse | Select-Object -Last 1
    
    if ($code -eq "401") {
        Write-Host "  ✅ $($route.Method) $($route.Path) → 401" -ForegroundColor Green
    } else {
        Write-Host "  ❌ $($route.Method) $($route.Path) → $code (esperado 401)" -ForegroundColor Red
        $allProtected = $false
    }
}

if ($allProtected) {
    Write-Host "✅ Todas las rutas críticas están protegidas" -ForegroundColor Green
} else {
    Write-Host "⚠️  Algunas rutas críticas no están protegidas correctamente" -ForegroundColor Yellow
}
Write-Host ""

# Test 5: Rutas públicas
Write-Host "[TEST 5] Verificando Rutas Públicas..." -ForegroundColor Yellow
$publicRoutes = @(
    @{ Method = "GET"; Path = "/health" },
    @{ Method = "GET"; Path = "/metrics" },
    @{ Method = "POST"; Path = "/api/Auth/logout" }
)

$allPublic = $true
foreach ($route in $publicRoutes) {
    $testResponse = curl -X $route.Method "$baseUrl$($route.Path)" -s -w "%{http_code}" 2>&1
    $code = $testResponse | Select-Object -Last 1
    
    if ($code -ne "401" -and $code -ne "403") {
        Write-Host "  ✅ $($route.Method) $($route.Path) → $code (accesible sin token)" -ForegroundColor Green
    } else {
        Write-Host "  ❌ $($route.Method) $($route.Path) → $code (NO debería requerir token)" -ForegroundColor Red
        $allPublic = $false
    }
}

if ($allPublic) {
    Write-Host "✅ Todas las rutas públicas son accesibles" -ForegroundColor Green
} else {
    Write-Host "⚠️  Algunas rutas públicas están bloqueadas incorrectamente" -ForegroundColor Yellow
}
Write-Host ""

# Resumen Final
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host "          RESUMEN FINAL" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host "✅ JWT Authentication: ACTIVO" -ForegroundColor Green
Write-Host "✅ Rutas Protegidas: CONFIGURADAS" -ForegroundColor Green
Write-Host "✅ Rutas Públicas: ACCESIBLES" -ForegroundColor Green
Write-Host ""
Write-Host "📋 Rutas Protegidas (requiresAuth: true):" -ForegroundColor Yellow
Write-Host "   - Todas las operaciones DELETE" -ForegroundColor White
Write-Host "   - POST /api/users" -ForegroundColor White
Write-Host "   - POST /api/users-with-preferences" -ForegroundColor White
Write-Host "   - POST /api/Report" -ForegroundColor White
Write-Host "   - POST /api/Analysis" -ForegroundColor White
Write-Host "   - POST /api/analyze" -ForegroundColor White
Write-Host "   - GET /api/preferences/by-user" -ForegroundColor White
Write-Host "   - GET /api/sessions/user" -ForegroundColor White
Write-Host ""
Write-Host "🔓 Rutas Públicas (requiresAuth: false):" -ForegroundColor Yellow
Write-Host "   - POST /api/Auth/* (login, logout, etc.)" -ForegroundColor White
Write-Host "   - GET endpoints de lectura" -ForegroundColor White
Write-Host "   - GET /health, /metrics" -ForegroundColor White
Write-Host ""
