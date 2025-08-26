# 📝 Headers de Contexto del Gateway

El gateway agrega automáticamente estos headers a todas las peticiones:

## Headers Agregados por el Gateway

```http
X-Gateway-Request-Id: 12345678-1234-1234-1234-123456789abc
X-Gateway-Service: users|reports|analysis|middleware
X-Gateway-Forwarded-For: 192.168.1.1
X-Gateway-Original-Host: api.company.com
X-Gateway-Timestamp: 2025-08-26T10:30:00Z
X-Correlation-ID: correlationId (si existe)
```

## Headers Propagados desde Cliente

```http
Authorization: Bearer jwt-token
Content-Type: application/json
Accept-Language: es-ES,en;q=0.9
User-Agent: Mozilla/5.0...
```

## ⚠️ **Acción Requerida en Microservicios**

### Para Users, Reports, Analysis:

Agregar middleware para leer headers del gateway:

```csharp
public class GatewayContextMiddleware
{
    private readonly RequestDelegate _next;

    public GatewayContextMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Leer headers del gateway
        var requestId = context.Request.Headers["X-Gateway-Request-Id"].FirstOrDefault();
        var service = context.Request.Headers["X-Gateway-Service"].FirstOrDefault();
        var forwardedFor = context.Request.Headers["X-Gateway-Forwarded-For"].FirstOrDefault();

        if (!string.IsNullOrEmpty(requestId))
        {
            // Agregar al contexto para logging y tracking
            context.Items["GatewayRequestId"] = requestId;
        }

        if (!string.IsNullOrEmpty(forwardedFor))
        {
            // IP real del cliente (no la del gateway)
            context.Items["ClientIP"] = forwardedFor;
        }

        await _next(context);
    }
}
```

### Para Middleware API (Node.js):

```typescript
// middleware/gateway-context.ts
export const gatewayContext = (req: Request, res: Response, next: NextFunction) => {
  // Leer headers del gateway
  req.gatewayRequestId = req.get('X-Gateway-Request-Id');
  req.gatewayService = req.get('X-Gateway-Service');
  req.clientIP = req.get('X-Gateway-Forwarded-For') || req.ip;

  next();
};
```
