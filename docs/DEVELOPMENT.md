# 🛠️ Guía de Desarrollo - Accessibility Gateway

Esta guía proporciona toda la información necesaria para desarrollar, debuggear y contribuir al proyecto Accessibility Gateway.

---

## 📋 Tabla de Contenidos

- [Requisitos Previos](#requisitos-previos)
- [Setup Inicial](#setup-inicial)
- [Estructura del Proyecto](#estructura-del-proyecto)
- [Workflow de Desarrollo](#workflow-de-desarrollo)
- [Convenciones de Código](#convenciones-de-código)
- [Testing](#testing)
- [Debugging](#debugging)
- [Performance](#performance)
- [Best Practices](#best-practices)
- [Tools y Extensions](#tools-y-extensions)

---

## Requisitos Previos

### Software Requerido

| Herramienta       | Versión Mínima | Recomendada | Link                                                 |
| ----------------- | -------------- | ----------- | ---------------------------------------------------- |
| **.NET SDK**      | 9.0.0          | 9.0.100     | [Download](https://dotnet.microsoft.com/download)    |
| **Visual Studio** | 2022 17.8      | 2022 17.12  | [Download](https://visualstudio.microsoft.com/)      |
| **Docker**        | 24.0           | 27.4.0      | [Download](https://www.docker.com/get-started)       |
| **Git**           | 2.40           | 2.47.1      | [Download](https://git-scm.com/downloads)            |
| **PowerShell**    | 7.4            | 7.4.6       | [Download](https://github.com/PowerShell/PowerShell) |

### Software Opcional pero Recomendado

- **JetBrains Rider 2024.3** - IDE alternativo
- **Visual Studio Code** - Editor ligero para scripts
- **Postman** - Testing de APIs
- **Azure Data Studio** - Cliente MySQL
- **k6** - Load testing
- **Redis Insight** - Cliente Redis GUI

---

## Setup Inicial

### 1. Clonar el Repositorio

```bash
git clone https://github.com/magodeveloper/accessibility-gw.git
cd accessibility-gw
```

### 2. Configurar Entorno de Desarrollo

```bash
# Copiar archivo de configuración de desarrollo
cp .env.example .env.development

# Editar variables de entorno
code .env.development
```

**Variables críticas a configurar:**

```bash
# JWT Configuration
JWT_SECRET_KEY=<generar-con-Generate-JwtSecretKey.ps1>
JWT_ISSUER=https://api.accessibility.company.com
JWT_AUDIENCE=accessibility-app
JWT_EXPIRATION_MINUTES=60

# Gateway Secret
GATEWAY_SECRET=<generar-con-Generate-JwtSecretKey.ps1>
GATEWAY_VALIDATION_ENABLED=true

# Redis Configuration
REDIS_CONNECTION=localhost:6379
REDIS_PASSWORD=
REDIS_DATABASE=0

# Microservices URLs (Docker)
MS_USERS_URL=http://msusers-api:8081
MS_ANALYSIS_URL=http://msanalysis-api:8082
MS_REPORTS_URL=http://msreports-api:8083
MIDDLEWARE_URL=http://accessibility-mw:3001

# Logging
LOG_LEVEL=Information
LOG_TO_FILE=true
```

### 3. Generar Secretos

```powershell
# Generar JWT Secret
.\Generate-JwtSecretKey.ps1

# O manualmente
dotnet run --project src/Tools/SecretGenerator

# Copiar el secret generado a .env.development
```

### 4. Crear Red Docker

```bash
# Crear red compartida para microservicios
docker network create accessibility-shared

# Verificar
docker network ls | grep accessibility
```

### 5. Instalar Dependencias

```bash
# Restaurar paquetes NuGet
dotnet restore

# Verificar
dotnet list package
```

### 6. Build del Proyecto

```bash
# Build en modo Debug
dotnet build

# Build en modo Release
dotnet build -c Release

# Verificar errores
dotnet build --no-incremental
```

### 7. Iniciar Servicios Docker

```bash
# Iniciar Redis y microservicios
docker compose -f docker-compose.dev.yml up -d

# Verificar que estén corriendo
docker ps

# Ver logs
docker compose logs -f
```

### 8. Ejecutar el Gateway

```bash
# Modo desarrollo con hot-reload
dotnet watch run --project src/Gateway

# O sin hot-reload
dotnet run --project src/Gateway

# Verificar
curl http://localhost:8100/health
```

---

## Estructura del Proyecto

```
accessibility-gw/
├── .github/
│   └── workflows/              # GitHub Actions CI/CD
│       ├── ci.yml              # Build & Test workflow
│       └── docker.yml          # Docker Build & Push
├── .githooks/                  # Git hooks
│   └── pre-commit              # Pre-commit validations
├── docs/                       # Documentación extendida
│   ├── API.md                  # Referencia completa de API
│   ├── ARCHITECTURE.md         # Arquitectura técnica
│   ├── CACHE.md                # Sistema de caché
│   ├── CONFIGURATION.md        # Configuración detallada
│   ├── DEVELOPMENT.md          # Esta guía
│   ├── DOCKER.md               # Docker & containers
│   ├── MONITORING.md           # Monitoreo y métricas
│   ├── SCRIPTS.md              # Scripts de gestión
│   ├── SECURITY.md             # Seguridad y autenticación
│   ├── TESTING.md              # Testing completo
│   └── TROUBLESHOOTING.md      # Solución de problemas
├── monitoring/                 # Stack de monitoreo
│   ├── grafana/                # Grafana config & dashboards
│   ├── prometheus/             # Prometheus config & alertas
│   └── docker-compose.monitoring.yml
├── scripts/                    # Scripts de utilidad
│   ├── PowerShell/             # Scripts PowerShell
│   └── bash/                   # Scripts Bash
├── src/
│   ├── Gateway/                # Proyecto principal
│   │   ├── Controllers/        # API Controllers
│   │   ├── Middleware/         # Custom middleware
│   │   ├── Services/           # Business services
│   │   ├── Models/             # DTOs y modelos
│   │   ├── Configuration/      # Config classes
│   │   ├── Extensions/         # Extension methods
│   │   ├── Validators/         # FluentValidation
│   │   ├── appsettings.json    # Config base
│   │   ├── appsettings.Development.json
│   │   ├── appsettings.Production.json
│   │   ├── Program.cs          # Entry point
│   │   └── Gateway.csproj
│   └── tests/
│       ├── Gateway.UnitTests/  # Unit tests
│       ├── Gateway.IntegrationTests/
│       └── Gateway.Load/       # k6 load tests
├── .editorconfig               # Editor config
├── .gitignore
├── Directory.Packages.props    # Central package management
├── docker-compose.yml          # Producción
├── docker-compose.dev.yml      # Desarrollo
├── Dockerfile
├── Gateway.sln                 # Solution file
├── global.json                 # .NET SDK version
├── manage-tests.ps1            # Script de tests
├── manage-monitoring.ps1       # Script de monitoreo
├── manage-network.ps1          # Script de red Docker
└── README.md                   # Documentación principal
```

### Proyectos Principales

#### **Gateway** (src/Gateway)

- API Gateway principal
- YARP Reverse Proxy
- Autenticación JWT
- Rate Limiting
- Cache con Redis
- Health Checks
- Métricas Prometheus

#### **Gateway.UnitTests** (src/tests/Gateway.UnitTests)

- Tests unitarios con xUnit
- Mocking con NSubstitute
- Coverage con Coverlet
- 96+ tests

#### **Gateway.IntegrationTests** (src/tests/Gateway.IntegrationTests)

- Tests de integración
- WebApplicationFactory
- InMemory database
- 12+ tests

#### **Gateway.Load** (src/tests/Gateway.Load)

- Load testing con k6
- 6 escenarios (smoke, load, stress, spike, soak)
- Métricas de performance

---

## Workflow de Desarrollo

### 1. Crear Nueva Feature

```bash
# Crear rama desde master
git checkout master
git pull origin master
git checkout -b feature/nombre-feature

# Convención de nombres:
# feature/  - Nueva funcionalidad
# fix/      - Corrección de bug
# docs/     - Cambios en documentación
# refactor/ - Refactoring de código
# test/     - Agregar o mejorar tests
```

### 2. Desarrollo Local

```bash
# Iniciar servicios necesarios
docker compose -f docker-compose.dev.yml up -d

# Hot-reload mode (recomendado)
dotnet watch run --project src/Gateway

# El Gateway se reinicia automáticamente al guardar cambios
```

### 3. Escribir Tests

```bash
# Ejecutar tests mientras desarrollas
dotnet watch test --project src/tests/Gateway.UnitTests

# O usar el script
.\manage-tests.ps1 test
```

### 4. Verificar Calidad

```bash
# Formatear código
dotnet format

# Análisis estático
dotnet build /p:TreatWarningsAsErrors=true

# Tests + coverage
.\manage-tests.ps1 full

# Verificar que coverage sea >90%
```

### 5. Commit

```bash
# Agregar cambios
git add .

# Commit con mensaje descriptivo (Conventional Commits)
git commit -m "feat: agregar endpoint de estadísticas de cache"

# Formato de commits:
# feat:     Nueva funcionalidad
# fix:      Corrección de bug
# docs:     Cambios en documentación
# style:    Formateo, missing semicolons, etc
# refactor: Refactoring de código
# test:     Agregar tests
# chore:    Actualizar dependencias, etc
```

### 6. Push y Pull Request

```bash
# Push a tu rama
git push origin feature/nombre-feature

# Crear Pull Request en GitHub
# - Título descriptivo
# - Descripción con contexto
# - Screenshots si aplica
# - Tests que agregaste
# - Cambios de breaking changes
```

### 7. Code Review

- Esperar aprobación de al menos 1 reviewer
- Resolver comentarios y sugerencias
- Actualizar PR según feedback
- CI/CD debe pasar (build + tests)

### 8. Merge

```bash
# Después de aprobación, merge a master
# GitHub Actions ejecutará:
# 1. Build
# 2. Tests
# 3. Coverage report
# 4. Docker build & push
```

---

## Convenciones de Código

### C# Style Guide

#### Naming Conventions

```csharp
// PascalCase para clases, métodos, propiedades
public class CacheService { }
public void GetCachedValue() { }
public string UserName { get; set; }

// camelCase para variables locales y parámetros
var cacheKey = "user:123";
public void ProcessRequest(string requestId) { }

// PascalCase con 'I' prefix para interfaces
public interface ICacheService { }

// PascalCase con 'T' prefix para type parameters
public class Repository<TEntity> { }

// UPPER_CASE para constantes
public const int MAX_RETRY_ATTEMPTS = 3;

// _camelCase para campos privados
private readonly ICacheService _cacheService;
```

#### Code Organization

```csharp
// Orden de miembros de clase:
public class ExampleService
{
    // 1. Campos privados
    private readonly ILogger<ExampleService> _logger;
    private readonly ICacheService _cacheService;

    // 2. Constructor
    public ExampleService(
        ILogger<ExampleService> logger,
        ICacheService cacheService)
    {
        _logger = logger;
        _cacheService = cacheService;
    }

    // 3. Propiedades públicas
    public string Name { get; set; }

    // 4. Métodos públicos
    public async Task<Result> ProcessAsync(Request request)
    {
        // Implementation
    }

    // 5. Métodos privados
    private void ValidateRequest(Request request)
    {
        // Implementation
    }
}
```

#### Async/Await

```csharp
// ✅ Correcto: Usar async/await consistentemente
public async Task<User> GetUserAsync(int userId)
{
    var user = await _repository.FindAsync(userId);
    return user;
}

// ❌ Incorrecto: Mezclar sync/async
public User GetUser(int userId)
{
    return _repository.FindAsync(userId).Result; // Deadlock risk!
}

// ✅ Correcto: Suffix 'Async' en métodos async
public async Task<bool> SaveAsync()

// ✅ Correcto: ConfigureAwait(false) en libraries
var data = await _service.GetDataAsync().ConfigureAwait(false);
```

#### Dependency Injection

```csharp
// ✅ Correcto: Constructor injection
public class UserController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ILogger<UserController> _logger;

    public UserController(
        IUserService userService,
        ILogger<UserController> logger)
    {
        _userService = userService;
        _logger = logger;
    }
}

// ❌ Incorrecto: Service locator pattern
var service = serviceProvider.GetService<IUserService>();
```

#### Error Handling

```csharp
// ✅ Correcto: Try-catch específico con logging
try
{
    await _service.ProcessAsync(request);
}
catch (ValidationException ex)
{
    _logger.LogWarning(ex, "Validation failed for request {RequestId}", request.Id);
    return BadRequest(ex.Message);
}
catch (NotFoundException ex)
{
    _logger.LogInformation("Resource not found: {ResourceId}", ex.ResourceId);
    return NotFound();
}
catch (Exception ex)
{
    _logger.LogError(ex, "Unexpected error processing request {RequestId}", request.Id);
    return StatusCode(500, "Internal server error");
}

// ❌ Incorrecto: Catch genérico sin logging
try
{
    await _service.ProcessAsync(request);
}
catch
{
    return StatusCode(500);
}
```

### EditorConfig

El proyecto usa `.editorconfig` para mantener consistencia:

```ini
# .editorconfig
root = true

[*.cs]
indent_style = space
indent_size = 4
charset = utf-8
trim_trailing_whitespace = true
insert_final_newline = true

# C# formatting
csharp_new_line_before_open_brace = all
csharp_prefer_braces = true:warning
csharp_prefer_simple_using_statement = true:suggestion
csharp_style_namespace_declarations = file_scoped:warning

[*.{json,yml,yaml}]
indent_size = 2
```

---

## Testing

### Unit Tests

```csharp
// Estructura: Arrange-Act-Assert
[Fact]
public async Task GetUser_WithValidId_ReturnsUser()
{
    // Arrange
    var userId = 1;
    var expectedUser = new User { Id = userId, Name = "Test" };
    _mockRepository.FindAsync(userId).Returns(expectedUser);

    // Act
    var result = await _service.GetUserAsync(userId);

    // Assert
    result.Should().NotBeNull();
    result.Id.Should().Be(userId);
    result.Name.Should().Be("Test");
}

// Naming: MethodName_Scenario_ExpectedResult
[Theory]
[InlineData(0)]
[InlineData(-1)]
public async Task GetUser_WithInvalidId_ThrowsException(int invalidId)
{
    // Act & Assert
    await Assert.ThrowsAsync<ArgumentException>(
        () => _service.GetUserAsync(invalidId)
    );
}
```

### Integration Tests

```csharp
public class GatewayIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public GatewayIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Health_ReturnsHealthy()
    {
        // Act
        var response = await _client.GetAsync("/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Healthy");
    }
}
```

### Ejecutar Tests

```bash
# Todos los tests
dotnet test

# Solo unit tests
dotnet test --filter "FullyQualifiedName~UnitTests"

# Tests con coverage
.\manage-tests.ps1 full

# Un test específico
dotnet test --filter "FullyQualifiedName~GetUser_WithValidId_ReturnsUser"

# Con verbosidad
dotnet test --logger "console;verbosity=detailed"
```

---

## Debugging

### Visual Studio

#### Launch Settings

```json
// Properties/launchSettings.json
{
  "profiles": {
    "Gateway": {
      "commandName": "Project",
      "launchBrowser": true,
      "launchUrl": "swagger",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      },
      "applicationUrl": "http://localhost:8100"
    },
    "Docker": {
      "commandName": "Docker",
      "launchBrowser": true,
      "launchUrl": "{Scheme}://{ServiceHost}:{ServicePort}/swagger",
      "environmentVariables": {
        "ASPNETCORE_URLS": "http://+:8080"
      },
      "publishAllPorts": true
    }
  }
}
```

#### Breakpoints

```csharp
// Conditional breakpoint
// Click derecho en breakpoint → Conditions
// Expression: userId == 123
// Hit Count: >= 5

// Logpoint (no detiene ejecución)
// Click derecho → Actions → Log message
// Message: User ID: {userId}, Name: {user.Name}
```

#### Watch Window

```csharp
// Expresiones útiles en Watch
request.Headers["Authorization"]
_cache.Count
_logger.IsEnabled(LogLevel.Debug)
Environment.GetEnvironmentVariable("JWT_SECRET_KEY")
```

### Logs de Debugging

```csharp
// Structured logging
_logger.LogDebug(
    "Processing request {RequestId} for user {UserId}",
    requestId,
    userId
);

// Ver logs en consola
docker compose logs -f accessibility-gateway

// Filtrar por nivel
docker compose logs | grep "ERROR"

// Buscar por RequestId
docker compose logs | grep "REQ-123456"
```

### Debugging con Postman

```json
// Collection con variables de entorno
{
  "id": "gateway-dev",
  "name": "Gateway Development",
  "values": [
    {
      "key": "baseUrl",
      "value": "http://localhost:8100",
      "enabled": true
    },
    {
      "key": "token",
      "value": "{{auth_token}}",
      "enabled": true
    }
  ]
}
```

---

## Performance

### Profiling

```bash
# dotnet-trace (incluido en .NET SDK)
dotnet-trace collect --process-id <PID>

# dotnet-counters (real-time metrics)
dotnet-counters monitor --process-id <PID>

# BenchmarkDotNet (para microbenchmarks)
dotnet run -c Release --project benchmarks/Gateway.Benchmarks
```

### Optimización

#### Cache Efectivo

```csharp
// ✅ Correcto: Cache key con namespace
var cacheKey = $"user:{userId}:profile";

// ✅ Correcto: TTL apropiado
var options = new DistributedCacheEntryOptions
{
    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15)
};

// ✅ Correcto: Cache aside pattern
var cachedValue = await _cache.GetAsync(key);
if (cachedValue == null)
{
    var value = await _repository.GetAsync(id);
    await _cache.SetAsync(key, value, options);
    return value;
}
return cachedValue;
```

#### Async Best Practices

```csharp
// ✅ Correcto: Parallel async operations
var userTask = _userService.GetUserAsync(userId);
var preferencesTask = _preferencesService.GetAsync(userId);
await Task.WhenAll(userTask, preferencesTask);
var user = await userTask;
var preferences = await preferencesTask;

// ❌ Incorrecto: Sequential cuando puede ser parallel
var user = await _userService.GetUserAsync(userId);
var preferences = await _preferencesService.GetAsync(userId);
```

---

## Best Practices

### 1. Separation of Concerns

- **Controllers**: Solo routing y validación básica
- **Services**: Lógica de negocio
- **Repositories**: Acceso a datos
- **Middleware**: Cross-cutting concerns (auth, logging)

### 2. Configuration

```csharp
// ✅ Correcto: Strongly-typed configuration
services.Configure<JwtSettings>(
    configuration.GetSection("Jwt")
);

// Usar en servicio
public class AuthService
{
    private readonly JwtSettings _jwtSettings;

    public AuthService(IOptions<JwtSettings> jwtSettings)
    {
        _jwtSettings = jwtSettings.Value;
    }
}
```

### 3. Logging

```csharp
// ✅ Correcto: Structured logging
_logger.LogInformation(
    "User {UserId} logged in from {IpAddress}",
    userId,
    ipAddress
);

// ❌ Incorrecto: String interpolation
_logger.LogInformation($"User {userId} logged in from {ipAddress}");
```

### 4. Secrets Management

```csharp
// ✅ Correcto: User Secrets en desarrollo
dotnet user-secrets set "Jwt:SecretKey" "my-secret-key"

// ✅ Correcto: Environment variables en producción
Environment.GetEnvironmentVariable("JWT_SECRET_KEY")

// ❌ Incorrecto: Hardcoded secrets
var secret = "my-secret-key";
```

---

## Tools y Extensions

### Visual Studio Extensions

- **ReSharper** - Code analysis y refactoring
- **CodeMaid** - Code cleanup
- **Roslynator** - Additional analyzers
- **GitLens** - Git supercharged
- **Docker** - Docker integration

### Visual Studio Code Extensions

- **C# Dev Kit** - C# development
- **Docker** - Docker support
- **.NET Core Test Explorer** - Test runner
- **REST Client** - API testing
- **GitLens** - Git integration

### CLI Tools

```bash
# Instalar global tools
dotnet tool install -g dotnet-format
dotnet tool install -g dotnet-outdated
dotnet tool install -g dotnet-trace
dotnet tool install -g dotnet-counters
dotnet tool install -g reportgenerator

# Usar
dotnet format
dotnet outdated
dotnet trace collect
```

---

## Recursos Adicionales

- [ASP.NET Core Docs](https://docs.microsoft.com/aspnet/core)
- [YARP Documentation](https://microsoft.github.io/reverse-proxy/)
- [Polly Documentation](https://www.thepollyproject.org/)
- [xUnit Documentation](https://xunit.net/)
- [Docker Documentation](https://docs.docker.com/)

---

**Autor:** Geovanny Camacho (fgiocl@outlook.com)  
**Última actualización:** 6 de noviembre de 2025  
**Versión:** 1.0.0
