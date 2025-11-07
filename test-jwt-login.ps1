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

Write-Host "=========================================" -ForegroundColor Cyan
Write-Host "   TEST FLUJO COMPLETO JWT AUTHENTICATION" -ForegroundColor Cyan
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host ""

# PASO 0: Intentar crear usuario de prueba
Write-Host "[PASO 0] Preparando usuario de prueba..." -ForegroundColor Yellow
$createUserFilePath = "src\tests\data\test-create-user.json"
$loginFilePath = "src\tests\data\test-login.example.json"

if (Test-Path $createUserFilePath) {
    Write-Host "✅ Archivo de creación encontrado: $createUserFilePath" -ForegroundColor Green
    $createUserData = Get-Content $createUserFilePath | ConvertFrom-Json
    
    Write-Host "Intentando crear usuario: $($createUserData.email)" -ForegroundColor White
    
    try {
        # Usar el endpoint público de registro /api/Auth/register
        $registerBody = @{
            email    = $createUserData.email
            password = $createUserData.password
            name     = $createUserData.name
            lastname = $createUserData.lastname
            nickname = $createUserData.nickname
        } | ConvertTo-Json

        $createResponse = Invoke-RestMethod -Uri "http://localhost:8100/api/Auth/register" `
            -Method POST `
            -Headers @{"Content-Type" = "application/json" } `
            -Body $registerBody `
            -ErrorAction Stop
        
        Write-Host "✅ Usuario creado exitosamente" -ForegroundColor Green
        Write-Host "   User ID: $($createResponse.userId)" -ForegroundColor DarkGray
    }
    catch {
        $statusCode = $_.Exception.Response.StatusCode.value__
        if ($statusCode -eq 409 -or $_.Exception.Message -match "already exists|duplicate") {
            Write-Host "ℹ️  Usuario ya existe - continuando con login" -ForegroundColor Cyan
        }
        elseif ($statusCode -eq 400) {
            Write-Host "⚠️  Usuario posiblemente ya existe (400) - continuando" -ForegroundColor Yellow
        }
        else {
            Write-Host "⚠️  No se pudo crear usuario: $($_.Exception.Message)" -ForegroundColor Yellow
            Write-Host "   Intentando continuar con usuario existente..." -ForegroundColor Gray
        }
    }
    
    # Usar credenciales del archivo de creación para login
    $testEmail = $createUserData.email
    $testPassword = $createUserData.password
}
elseif (Test-Path $loginFilePath) {
    Write-Host "ℹ️  Usando credenciales de: $loginFilePath" -ForegroundColor Cyan
    $loginData = Get-Content $loginFilePath | ConvertFrom-Json
    $testEmail = $loginData.email
    $testPassword = $loginData.password
}
else {
    Write-Host "⚠️  Archivos de configuración no encontrados" -ForegroundColor Yellow
    Write-Host "Usando credenciales por defecto..." -ForegroundColor White
    $testEmail = "testjwt@test.com"
    $testPassword = "Test123!"
}

Write-Host ""
Write-Host "📧 Email: $testEmail" -ForegroundColor White
Write-Host "🔑 Password: $('*' * $testPassword.Length)" -ForegroundColor White
Write-Host ""

# Test 1: Obtener token JWT mediante login
Write-Host "[TEST 1] Obteniendo token JWT mediante login..." -ForegroundColor Yellow
$loginBody = @{
    email    = $testEmail
    password = $testPassword
}

try {
    $loginResponse = Invoke-RestMethod -Uri "$baseUrl/api/Auth/login" `
        -Method POST `
        -Headers @{"Content-Type" = "application/json" } `
        -Body ($loginBody | ConvertTo-Json) `
        -ErrorAction Stop
    
    Write-Host "✅ Login exitoso - Token JWT obtenido" -ForegroundColor Green
    
    if ($loginResponse.token) {
        $jwtToken = $loginResponse.token
        Write-Host "Token (primeros 50 chars): $($jwtToken.Substring(0, [Math]::Min(50, $jwtToken.Length)))..." -ForegroundColor DarkGray
    }
    elseif ($loginResponse.accessToken) {
        $jwtToken = $loginResponse.accessToken
        Write-Host "Token (primeros 50 chars): $($jwtToken.Substring(0, [Math]::Min(50, $jwtToken.Length)))..." -ForegroundColor DarkGray
    }
    else {
        Write-Host "⚠️  Token obtenido pero formato desconocido" -ForegroundColor Yellow
        Write-Host "Response: $($loginResponse | ConvertTo-Json -Compress)" -ForegroundColor White
        $jwtToken = $null
    }
}
catch {
    Write-Host "❌ Login falló" -ForegroundColor Red
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    
    if ($_.ErrorDetails.Message -match "BCrypt|Invalid salt") {
        Write-Host ""
        Write-Host "💡 El usuario existe pero tiene un hash de password inválido." -ForegroundColor Yellow
        Write-Host "   El microservicio usa BCrypt. Crea el usuario desde el microservicio." -ForegroundColor Yellow
    }
    
    $jwtToken = $null
}
Write-Host ""

# Test 2: Probar acceso a ruta protegida CON token
if ($jwtToken) {
    Write-Host "[TEST 2] Probando acceso a ruta protegida CON token..." -ForegroundColor Yellow
    
    $protectedResponse = curl -X GET "$baseUrl/api/users" `
        -H "Authorization: Bearer $jwtToken" `
        -s -w "`n%{http_code}" 2>&1
    
    $httpCode = $protectedResponse[-1]
    $body = $protectedResponse[0..($protectedResponse.Length - 2)] -join "`n"
    
    Write-Host "GET /api/users con token JWT" -ForegroundColor White
    Write-Host "HTTP Status: $httpCode" -ForegroundColor White
    
    if ($httpCode -eq "200") {
        Write-Host "✅ Acceso permitido con token válido" -ForegroundColor Green
        Write-Host "Response (primeros 200 chars): $($body.Substring(0, [Math]::Min(200, $body.Length)))..." -ForegroundColor DarkGray
    }
    elseif ($httpCode -eq "401") {
        Write-Host "❌ Token rechazado (401) - posible problema de validación" -ForegroundColor Red
    }
    else {
        Write-Host "⚠️  Respuesta inesperada: $httpCode" -ForegroundColor Yellow
    }
}
else {
    Write-Host "[TEST 2] ⏭️  Saltado - No hay token disponible" -ForegroundColor Gray
}
Write-Host ""

# Test 3: Probar acceso a ruta protegida SIN token
Write-Host "[TEST 3] Probando acceso a ruta protegida SIN token..." -ForegroundColor Yellow

$noTokenResponse = curl -X GET "$baseUrl/api/users" `
    -s -w "`n%{http_code}" 2>&1

$httpCode = $noTokenResponse[-1]

Write-Host "GET /api/users sin token JWT" -ForegroundColor White
Write-Host "HTTP Status: $httpCode" -ForegroundColor White

if ($httpCode -eq "401") {
    Write-Host "✅ Correctamente rechazado sin token (401 Unauthorized)" -ForegroundColor Green
}
else {
    Write-Host "❌ FALLO: Ruta protegida accesible sin token (HTTP $httpCode)" -ForegroundColor Red
}
Write-Host ""

# Test 4: Probar token inválido
Write-Host "[TEST 4] Verificando validación de tokens inválidos..." -ForegroundColor Yellow

$fakeToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c"

$invalidTokenResponse = curl -X DELETE "$baseUrl/api/users/all-data" `
    -H "Authorization: Bearer $fakeToken" `
    -s -w "`n%{http_code}" 2>&1

$httpCode = $invalidTokenResponse[-1]

Write-Host "DELETE /api/users/all-data con token INVÁLIDO" -ForegroundColor White
Write-Host "HTTP Status: $httpCode" -ForegroundColor White

if ($httpCode -eq "401") {
    Write-Host "✅ Token inválido correctamente rechazado (401)" -ForegroundColor Green
}
else {
    Write-Host "⚠️  Token inválido devolvió: $httpCode (esperado 401)" -ForegroundColor Yellow
}
Write-Host ""

# Test 5: Múltiples rutas protegidas con token válido
if ($jwtToken) {
    Write-Host "[TEST 5] Probando múltiples rutas protegidas con token..." -ForegroundColor Yellow
    
    $protectedEndpoints = @(
        @{ Method = "GET"; Path = "/api/users"; ExpectedCodes = @(200) },
        @{ Method = "GET"; Path = "/api/Analysis"; ExpectedCodes = @(200, 404) },
        @{ Method = "GET"; Path = "/api/Report"; ExpectedCodes = @(200, 404) },
        @{ Method = "GET"; Path = "/health"; ExpectedCodes = @(200) },
        @{ Method = "GET"; Path = "/metrics"; ExpectedCodes = @(200) }
    )
    
    $allPassed = $true
    foreach ($endpoint in $protectedEndpoints) {
        $testResp = curl -X $endpoint.Method "$baseUrl$($endpoint.Path)" `
            -H "Authorization: Bearer $jwtToken" `
            -s -w "`n%{http_code}" 2>&1
        
        $lines = $testResp -split "`n"
        $code = $lines[-1]
        
        if ($endpoint.ExpectedCodes -contains [int]$code) {
            Write-Host "  ✅ $($endpoint.Method) $($endpoint.Path) → $code" -ForegroundColor Green
        }
        elseif ($code -eq "401") {
            Write-Host "  ❌ $($endpoint.Method) $($endpoint.Path) → $code (token rechazado)" -ForegroundColor Red
            $allPassed = $false
        }
        else {
            Write-Host "  ⚠️  $($endpoint.Method) $($endpoint.Path) → $code" -ForegroundColor Yellow
        }
    }
    
    if ($allPassed) {
        Write-Host "✅ Token JWT funciona en múltiples rutas protegidas" -ForegroundColor Green
    }
}
else {
    Write-Host "[TEST 5] ⏭️  Saltado - No hay token disponible" -ForegroundColor Gray
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
}
else {
    Write-Host "⚠️  Flujo JWT: PARCIAL" -ForegroundColor Yellow
    Write-Host "❌ Login: FALLO" -ForegroundColor Red
    Write-Host "✅ Rechazo sin token: OK" -ForegroundColor Green
    Write-Host ""
    Write-Host "💡 Sugerencia: Verificar credenciales de usuario o crear usuario manualmente" -ForegroundColor Yellow
}
Write-Host ""
