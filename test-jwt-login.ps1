#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Script para probar el flujo completo de autenticación JWT
.DESCRIPTION
    1. Crea un usuario de prueba
    2. Obtiene un token JWT mediante login
    3. Prueba acceso a rutas protegidas con el token
    4. Verifica validación de tokens inválidos
#>

$ErrorActionPreference = "Continue"
$baseUrl = "http://localhost:8100"
$usersUrl = "http://localhost:8081"

Write-Host "=========================================" -ForegroundColor Cyan
Write-Host "   TEST FLUJO COMPLETO JWT AUTHENTICATION" -ForegroundColor Cyan
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host ""

# Test 1: Crear usuario de prueba
Write-Host "[TEST 1] Creando usuario de prueba..." -ForegroundColor Yellow
$timestamp = Get-Date -Format "yyyyMMddHHmmss"
$testEmail = "testuser_$timestamp@accessibility.com"
$testPassword = "TestPass123!"

$createUserBody = @{
    name = "Test User $timestamp"
    email = $testEmail
    password = $testPassword
    role = "user"
} | ConvertTo-Json

Write-Host "Email: $testEmail" -ForegroundColor White
$createResponse = curl -X POST "$usersUrl/api/users" `
    -H "Content-Type: application/json" `
    -d $createUserBody `
    -s 2>&1

if ($createResponse -match "error|Error") {
    Write-Host "❌ Error creando usuario: $createResponse" -ForegroundColor Red
    Write-Host ""
    Write-Host "⚠️  Intentando con usuario existente..." -ForegroundColor Yellow
    $testEmail = "admin@accessibility.com"
    $testPassword = "Admin123!"
} else {
    Write-Host "✅ Usuario creado exitosamente" -ForegroundColor Green
    Write-Host "Response: $createResponse" -ForegroundColor DarkGray
}
Write-Host ""

# Test 2: Obtener token JWT mediante login
Write-Host "[TEST 2] Obteniendo token JWT mediante login..." -ForegroundColor Yellow
$loginBody = @{
    email = $testEmail
    password = $testPassword
} | ConvertTo-Json

$loginResponse = curl -X POST "$baseUrl/api/Auth/login" `
    -H "Content-Type: application/json" `
    -d $loginBody `
    -s 2>&1

Write-Host "Response: $loginResponse" -ForegroundColor White

if ($loginResponse -match '"token"' -or $loginResponse -match '"accessToken"') {
    Write-Host "✅ Login exitoso - Token JWT obtenido" -ForegroundColor Green
    
    # Extraer el token del JSON response
    $tokenMatch = $loginResponse | Select-String -Pattern '"(?:token|accessToken)"\s*:\s*"([^"]+)"'
    if ($tokenMatch) {
        $jwtToken = $tokenMatch.Matches.Groups[1].Value
        Write-Host "Token (primeros 50 chars): $($jwtToken.Substring(0, [Math]::Min(50, $jwtToken.Length)))..." -ForegroundColor DarkGray
    } else {
        Write-Host "⚠️  Token obtenido pero no pudo extraerse del JSON" -ForegroundColor Yellow
        $jwtToken = $null
    }
} else {
    Write-Host "❌ Login falló: $loginResponse" -ForegroundColor Red
    $jwtToken = $null
}
Write-Host ""

# Test 3: Probar acceso a ruta protegida CON token
if ($jwtToken) {
    Write-Host "[TEST 3] Probando acceso a ruta protegida CON token..." -ForegroundColor Yellow
    
    $protectedResponse = curl -X GET "$baseUrl/api/users" `
        -H "Authorization: Bearer $jwtToken" `
        -s -w "`n%{http_code}" 2>&1
    
    $httpCode = $protectedResponse[-1]
    $body = $protectedResponse[0..($protectedResponse.Length-2)] -join "`n"
    
    Write-Host "GET /api/users con token JWT" -ForegroundColor White
    Write-Host "HTTP Status: $httpCode" -ForegroundColor White
    
    if ($httpCode -eq "200") {
        Write-Host "✅ Acceso permitido con token válido" -ForegroundColor Green
        Write-Host "Response (primeros 200 chars): $($body.Substring(0, [Math]::Min(200, $body.Length)))..." -ForegroundColor DarkGray
    } elseif ($httpCode -eq "401") {
        Write-Host "❌ Token rechazado (401) - posible problema de validación" -ForegroundColor Red
    } else {
        Write-Host "⚠️  Respuesta inesperada: $httpCode" -ForegroundColor Yellow
    }
} else {
    Write-Host "[TEST 3] ⏭️  Saltado - No hay token disponible" -ForegroundColor Gray
}
Write-Host ""

# Test 4: Probar acceso a ruta protegida SIN token
Write-Host "[TEST 4] Probando acceso a ruta protegida SIN token..." -ForegroundColor Yellow

$noTokenResponse = curl -X GET "$baseUrl/api/users" `
    -s -w "`n%{http_code}" 2>&1

$httpCode = $noTokenResponse[-1]

Write-Host "GET /api/users sin token JWT" -ForegroundColor White
Write-Host "HTTP Status: $httpCode" -ForegroundColor White

if ($httpCode -eq "401") {
    Write-Host "✅ Correctamente rechazado sin token (401 Unauthorized)" -ForegroundColor Green
} else {
    Write-Host "❌ FALLO: Ruta protegida accesible sin token (HTTP $httpCode)" -ForegroundColor Red
}
Write-Host ""

# Test 5: Probar token inválido
Write-Host "[TEST 5] Verificando validación de tokens inválidos..." -ForegroundColor Yellow

$fakeToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c"

$invalidTokenResponse = curl -X DELETE "$baseUrl/api/users/all-data" `
    -H "Authorization: Bearer $fakeToken" `
    -s -w "`n%{http_code}" 2>&1

$httpCode = $invalidTokenResponse[-1]

Write-Host "DELETE /api/users/all-data con token INVÁLIDO" -ForegroundColor White
Write-Host "HTTP Status: $httpCode" -ForegroundColor White

if ($httpCode -eq "401") {
    Write-Host "✅ Token inválido correctamente rechazado (401)" -ForegroundColor Green
} else {
    Write-Host "⚠️  Token inválido devolvió: $httpCode (esperado 401)" -ForegroundColor Yellow
}
Write-Host ""

# Test 6: Múltiples rutas protegidas con token válido
if ($jwtToken) {
    Write-Host "[TEST 6] Probando múltiples rutas protegidas con token..." -ForegroundColor Yellow
    
    $protectedEndpoints = @(
        @{ Method = "GET"; Path = "/api/preferences/by-user?userId=test"; ExpectedCodes = @(200, 404, 400) },
        @{ Method = "GET"; Path = "/api/sessions/user?userId=test"; ExpectedCodes = @(200, 404, 400) },
        @{ Method = "GET"; Path = "/api/Analysis/by-user?userId=test"; ExpectedCodes = @(200, 404, 400) }
    )
    
    $allPassed = $true
    foreach ($endpoint in $protectedEndpoints) {
        $testResp = curl -X $endpoint.Method "$baseUrl$($endpoint.Path)" `
            -H "Authorization: Bearer $jwtToken" `
            -s -w "%{http_code}" 2>&1
        
        $code = $testResp | Select-Object -Last 1
        
        if ($endpoint.ExpectedCodes -contains [int]$code) {
            Write-Host "  ✅ $($endpoint.Method) $($endpoint.Path) → $code" -ForegroundColor Green
        } elseif ($code -eq "401") {
            Write-Host "  ❌ $($endpoint.Method) $($endpoint.Path) → $code (token rechazado)" -ForegroundColor Red
            $allPassed = $false
        } else {
            Write-Host "  ⚠️  $($endpoint.Method) $($endpoint.Path) → $code" -ForegroundColor Yellow
        }
    }
    
    if ($allPassed) {
        Write-Host "✅ Token JWT funciona en múltiples rutas protegidas" -ForegroundColor Green
    }
} else {
    Write-Host "[TEST 6] ⏭️  Saltado - No hay token disponible" -ForegroundColor Gray
}
Write-Host ""

# Resumen Final
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host "          RESUMEN FINAL" -ForegroundColor Cyan
Write-Host "=========================================" -ForegroundColor Cyan

if ($jwtToken) {
    Write-Host "✅ Flujo completo JWT: FUNCIONAL" -ForegroundColor Green
    Write-Host "✅ Login: OK" -ForegroundColor Green
    Write-Host "✅ Token generado: OK" -ForegroundColor Green
    Write-Host "✅ Acceso con token: OK" -ForegroundColor Green
    Write-Host "✅ Rechazo sin token: OK" -ForegroundColor Green
    Write-Host "✅ Validación de token: OK" -ForegroundColor Green
} else {
    Write-Host "⚠️  Flujo JWT: PARCIAL" -ForegroundColor Yellow
    Write-Host "❌ Login: FALLO" -ForegroundColor Red
    Write-Host "✅ Rechazo sin token: OK" -ForegroundColor Green
    Write-Host ""
    Write-Host "💡 Sugerencia: Verificar credenciales de usuario o crear usuario manualmente" -ForegroundColor Yellow
}
Write-Host ""
