# 🛠️ Guía de Scripts

> Documentación de scripts PowerShell para gestión del Gateway.

## 📋 Tabla de Contenidos

- [manage-gateway.ps1](#manage-gatewayps1)
- [Comandos Disponibles](#comandos-disponibles)
- [Ejemplos de Uso](#ejemplos-de-uso)

---

## 🔧 manage-gateway.ps1

Script maestro unificado para gestión completa del Gateway.

### Comandos Principales

```powershell
# Ver ayuda completa
.\manage-gateway.ps1 help

# Verificación del sistema
.\manage-gateway.ps1 verify -Full

# Testing
.\manage-gateway.ps1 test -TestType All
.\manage-gateway.ps1 test -TestType Unit -GenerateCoverage

# Building
.\manage-gateway.ps1 build -Configuration Release
.\manage-gateway.ps1 build -BuildType docker

# Servidor local
.\manage-gateway.ps1 run -Port 8100

# Docker
.\manage-gateway.ps1 docker up -Environment prod
.\manage-gateway.ps1 docker logs -Follow

# Limpieza
.\manage-gateway.ps1 cleanup -Docker -Volumes
```

---

## 📝 Comandos Disponibles

### Testing

```powershell
# Todos los tests
.\manage-gateway.ps1 test

# Tests unitarios
.\manage-gateway.ps1 test -TestType Unit

# Tests de integración
.\manage-gateway.ps1 test -TestType Integration

# Tests de carga
.\manage-gateway.ps1 test -TestType Load -Scenario smoke

# Con cobertura
.\manage-gateway.ps1 test -TestType Unit -GenerateCoverage

# Abrir reporte
.\manage-gateway.ps1 test -OpenReport

# Verbose
.\manage-gateway.ps1 test -Verbose
```

### Building

```powershell
# Build estándar
.\manage-gateway.ps1 build

# Build Release
.\manage-gateway.ps1 build -Configuration Release

# Build Docker
.\manage-gateway.ps1 build -BuildType docker

# Build con limpieza previa
.\manage-gateway.ps1 build -Clean

# Build producción
.\manage-gateway.ps1 build -BuildType production -Configuration Release
```

### Servidor Local

```powershell
# Iniciar servidor (puerto 8100)
.\manage-gateway.ps1 run

# Puerto personalizado
.\manage-gateway.ps1 run -Port 8085

# Sin abrir navegador
.\manage-gateway.ps1 run -NoLaunch

# Environment específico
.\manage-gateway.ps1 run -AspNetCoreEnvironment Production
```

### Docker Management

```powershell
# Desarrollo
.\manage-gateway.ps1 docker up -Environment dev

# Desarrollo con herramientas
.\manage-gateway.ps1 docker up -Environment dev -WithTools

# Producción
.\manage-gateway.ps1 docker up -Environment prod

# Rebuild
.\manage-gateway.ps1 docker up -Rebuild

# Ver logs
.\manage-gateway.ps1 docker logs
.\manage-gateway.ps1 docker logs -Follow

# Estado
.\manage-gateway.ps1 docker status

# Detener
.\manage-gateway.ps1 docker down
```

### Verificación

```powershell
# Verificación básica
.\manage-gateway.ps1 verify

# Verificación completa
.\manage-gateway.ps1 verify -Full

# Verificar consistencia
.\manage-gateway.ps1 consistency
```

### Limpieza

```powershell
# Limpiar builds
.\manage-gateway.ps1 cleanup -Builds

# Limpiar Docker
.\manage-gateway.ps1 cleanup -Docker

# Limpiar volúmenes Docker
.\manage-gateway.ps1 cleanup -Volumes

# Limpieza completa
.\manage-gateway.ps1 cleanup -All
```

---

## 💡 Ejemplos de Uso

### Setup Inicial

```powershell
# 1. Verificar prerrequisitos
.\manage-gateway.ps1 verify -Full

# 2. Build inicial
.\manage-gateway.ps1 build

# 3. Ejecutar tests
.\manage-gateway.ps1 test -TestType All

# 4. Iniciar en desarrollo
.\manage-gateway.ps1 docker up -Environment dev
```

### Desarrollo Diario

```powershell
# Desarrollo con hot-reload
.\manage-gateway.ps1 run -Port 8100

# Tests mientras desarrollas
.\manage-gateway.ps1 test -TestType Unit -Verbose

# Ver logs Docker
.\manage-gateway.ps1 docker logs -Follow
```

### Pre-Deployment

```powershell
# 1. Tests completos
.\manage-gateway.ps1 test -TestType All -GenerateCoverage

# 2. Build producción
.\manage-gateway.ps1 build -BuildType production -Configuration Release

# 3. Build Docker
.\manage-gateway.ps1 build -BuildType docker

# 4. Verificar todo
.\manage-gateway.ps1 verify -Full

# 5. Deploy
.\manage-gateway.ps1 docker up -Environment prod
```

### Troubleshooting

```powershell
# Ver estado
.\manage-gateway.ps1 docker status

# Logs detallados
.\manage-gateway.ps1 docker logs -Follow

# Reiniciar servicios
.\manage-gateway.ps1 docker down
.\manage-gateway.ps1 cleanup -Docker
.\manage-gateway.ps1 docker up -Rebuild
```

### CI/CD Local

```powershell
# Simular pipeline CI/CD
.\manage-gateway.ps1 verify -Full
.\manage-gateway.ps1 build -Clean -Configuration Release
.\manage-gateway.ps1 test -TestType All -GenerateCoverage
.\manage-gateway.ps1 build -BuildType docker
```

---

## 🔍 Parámetros Comunes

### -Verbose

Muestra información detallada de ejecución.

```powershell
.\manage-gateway.ps1 test -Verbose
```

### -WhatIf

Muestra qué se ejecutaría sin hacerlo (dry-run).

```powershell
.\manage-gateway.ps1 cleanup -All -WhatIf
```

### -Force

Fuerza la ejecución sin confirmaciones.

```powershell
.\manage-gateway.ps1 cleanup -Docker -Force
```

---

## 📚 Scripts Adicionales

### manage-tests.ps1

Script especializado en testing (deprecated, usar manage-gateway.ps1 test).

```powershell
# Equivalencias
.\manage-tests.ps1 run-all        → .\manage-gateway.ps1 test -TestType All
.\manage-tests.ps1 unit           → .\manage-gateway.ps1 test -TestType Unit
.\manage-tests.ps1 coverage       → .\manage-gateway.ps1 test -GenerateCoverage
```

### Generate-JwtSecretKey.ps1

Genera claves JWT seguras.

```powershell
.\Generate-JwtSecretKey.ps1

# Output:
# ✅ JWT Secret Keys Generadas:
# Base64: x1y2z3a4b5c6...
# Hexadecimal: 1A2B3C4D...
```

### Validate-JwtConfig.ps1

Valida configuración JWT.

```powershell
.\Validate-JwtConfig.ps1

# Verifica:
# - JWT_SECRET existe y es segura (≥32 bytes)
# - JWT_ISSUER configurado
# - JWT_AUDIENCE configurado
```

---

## 📚 Referencias

- [PowerShell Documentation](https://learn.microsoft.com/en-us/powershell/)
- [Script Best Practices](https://learn.microsoft.com/en-us/powershell/scripting/dev-cross-plat/performance/script-authoring-considerations)

---

[⬅️ Volver al README](../README.new.md)
