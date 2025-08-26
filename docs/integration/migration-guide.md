# 📋 Guía de Migración: Microservicios → Gateway

## 🎯 **Resumen Ejecutivo**

El gateway es **85% transparente**, pero requiere algunos ajustes en los microservicios para aprovechar completamente sus beneficios.

## ✅ **Lo que NO requiere cambios:**

- ✅ **Endpoints**: Todas las rutas se mantienen igual
- ✅ **Autenticación JWT**: Se propaga automáticamente
- ✅ **Formato de respuesta**: JSON se preserva sin modificación
- ✅ **Códigos HTTP**: Se mantienen originales
- ✅ **Middleware de negocio**: Lógica de aplicación intacta

## ⚠️ **Cambios Necesarios por Servicio**

### 🔹 **accessibility-ms-users**

```bash
📂 Archivos a modificar:
- src/Users.Api/Program.cs          # Agregar GatewayContextMiddleware + Health checks
- src/appsettings.json              # URLs de otros servicios via gateway
- src/appsettings.Development.json  # Configuración de desarrollo

⏱️ Tiempo estimado: 30 minutos
🔧 Complejidad: Baja
```

### 🔹 **accessibility-ms-reports**

```bash
📂 Archivos a modificar:
- src/Reports.Api/Program.cs        # Agregar GatewayContextMiddleware + Health checks
- src/appsettings.json              # URLs de otros servicios via gateway
- src/appsettings.Development.json  # Configuración de desarrollo

⏱️ Tiempo estimado: 30 minutos
🔧 Complejidad: Baja
```

### 🔹 **accessibility-ms-analysis**

```bash
📂 Archivos a modificar:
- src/Analysis.Api/Program.cs       # Agregar GatewayContextMiddleware + Health checks
- src/appsettings.json              # URLs de otros servicios via gateway
- src/Analysis.Infrastructure/Services/UserValidationService.cs # URL del Users API

⏱️ Tiempo estimado: 45 minutos
🔧 Complejidad: Media (tiene dependencia externa)
```

### 🔹 **accessibility-mw**

```bash
📂 Archivos a modificar:
- .env                              # URL del Analysis API via gateway
- src/server.ts                     # Agregar gateway context middleware

⏱️ Tiempo estimado: 20 minutos
🔧 Complejidad: Baja
```

## 🚀 **Plan de Implementación Recomendado**

### Fase 1: Preparación (1 hora)

1. **Leer documentación** de integración creada
2. **Backup** de configuraciones actuales
3. **Crear rama** para cambios: `git checkout -b gateway-integration`

### Fase 2: Cambios Core (2 horas)

1. **accessibility-mw** (más simple, 20 min)
2. **accessibility-ms-users** (base crítica, 30 min)
3. **accessibility-ms-reports** (30 min)
4. **accessibility-ms-analysis** (45 min)

### Fase 3: Testing (1 hora)

1. **Levantar servicios** individualmente
2. **Levantar gateway**
3. **Pruebas de integración** con Swagger UI
4. **Verificar health checks**

### Fase 4: Validación (30 min)

1. **Testing end-to-end** de flujos principales
2. **Verificar métricas** en `/metrics`
3. **Confirmar logs** centralizados

## 🧪 **Scripts de Validación**

### Test de Conectividad:

```bash
# Health check del gateway
curl http://localhost:8000/health

# Health check profundo
curl http://localhost:8000/health?deep=true

# Métricas
curl http://localhost:8000/metrics
```

### Test de Autenticación:

```bash
# Login a través del gateway
curl -X POST http://localhost:8000/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"test@test.com","password":"password"}'

# Usar token en petición
curl -X GET http://localhost:8000/api/v1/users \
  -H "Authorization: Bearer JWT_TOKEN"
```

## 📊 **Monitoreo Post-Migración**

### Métricas Clave a Observar:

- ✅ **Response Times**: No deben aumentar significativamente
- ✅ **Success Rate**: Debe mantenerse > 99%
- ✅ **Cache Hit Rate**: Debe ser > 30% para GETs
- ✅ **Health Check Status**: Todos los servicios "Healthy"

### Logs Importantes:

```bash
# Gateway logs
tail -f ./logs/gateway-*.log | grep ERROR

# Service-specific issues
docker-compose logs -f gateway-service
```

## 🆘 **Plan de Rollback**

Si hay problemas, rollback es simple:

1. **Revertir URLs** en configuraciones a direcciones directas
2. **Desactivar gateway** temporalmente
3. **Servicios continúan funcionando** de forma directa
4. **Investigar y corregir** problemas específicos

## 📞 **Soporte**

- **Documentación**: `docs/integration/`
- **Issues**: Crear en repositorio del gateway
- **Testing**: Usar Swagger UI en `localhost:8000/swagger`

---

### 🎯 **Tiempo Total Estimado: 4-5 horas**

### 🔧 **Nivel de Riesgo: Bajo** (rollback fácil)

### 💰 **Beneficio**: Alto (caché, métricas, resiliencia, seguridad)
