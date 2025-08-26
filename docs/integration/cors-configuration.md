# 🌐 Configuración CORS con Gateway

## ✅ **El Gateway Maneja CORS Centralmente**

El gateway ya está configurado con CORS permisivo:

```csharp
// En el gateway
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowedOrigins", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});
```

## 🔧 **Cambios en Microservicios**

### Opción 1: Desactivar CORS (Recomendado)

Los microservicios pueden **desactivar CORS** completamente ya que solo reciben tráfico del gateway:

#### .NET Services:

```csharp
// ❌ REMOVER o comentar:
// builder.Services.AddCors(...);
// app.UseCors(...);

// ✅ Los microservicios NO necesitan CORS
// El gateway maneja todo el CORS
```

#### Node.js Service:

```typescript
// ❌ REMOVER o comentar:
// app.use(cors({...}));

// ✅ El middleware API NO necesita CORS
// El gateway maneja todo el CORS
```

### Opción 2: CORS Restrictivo (Solo Gateway)

Si prefieres mantener CORS como medida de seguridad adicional:

#### .NET Services:

```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("GatewayOnly", policy =>
    {
        policy.WithOrigins("http://localhost:8000") // Solo el gateway
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

app.UseCors("GatewayOnly");
```

#### Node.js Service:

```typescript
app.use(
  cors({
    origin: ['http://localhost:8000'], // Solo el gateway
    credentials: true,
  }),
);
```

## 📋 **Recomendación**

**USAR OPCIÓN 1** - Desactivar CORS en microservicios porque:

1. **Single Point of Entry**: Solo el gateway recibe tráfico externo
2. **Simplicidad**: Menos configuración que mantener
3. **Performance**: Una capa menos de validación
4. **Consistency**: CORS centralizado y uniforme

## 🔒 **Seguridad**

Con esta configuración:

- **Gateway**: Maneja CORS para todos los clientes externos
- **Microservicios**: Solo aceptan tráfico del gateway (sin CORS)
- **Red**: Los microservicios están "protegidos" detrás del gateway

## ⚙️ **Variables de Entorno**

### Gateway:

```bash
CORS_ALLOWED_ORIGINS="http://localhost:3000,https://app.company.com"
```

### Microservicios:

```bash
# No necesitan variables CORS
```
