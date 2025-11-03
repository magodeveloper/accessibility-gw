using WireMock.Server;
using WireMock.Settings;
using WireMock.Matchers;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;

namespace Gateway.IntegrationTests.Fixtures;

/// <summary>
/// Factory que gestiona servidores WireMock para simular servicios backend.
/// Proporciona mocks de Users, Analysis, Reports y Middleware.
/// </summary>
public class MockServiceFactory : IAsyncDisposable
{
    public WireMockServer? UsersService { get; private set; }
    public WireMockServer? AnalysisService { get; private set; }
    public WireMockServer? ReportsService { get; private set; }
    public WireMockServer? MiddlewareService { get; private set; }

    /// <summary>
    /// Inicializa todos los servicios mock.
    /// Cada servicio se inicia en un puerto dinámico aleatorio.
    /// </summary>
    public async Task InitializeAsync()
    {
        // Iniciar servidores en puertos dinámicos
        UsersService = WireMockServer.Start(new WireMockServerSettings
        {
            Port = 0, // Puerto dinámico
            StartAdminInterface = true
        });

        AnalysisService = WireMockServer.Start(new WireMockServerSettings
        {
            Port = 0,
            StartAdminInterface = true
        });

        ReportsService = WireMockServer.Start(new WireMockServerSettings
        {
            Port = 0,
            StartAdminInterface = true
        });

        MiddlewareService = WireMockServer.Start(new WireMockServerSettings
        {
            Port = 0,
            StartAdminInterface = true
        });

        // Configurar respuestas por defecto
        SetupDefaultResponses();

        await Task.CompletedTask;
    }

    /// <summary>
    /// Configura respuestas por defecto para todos los servicios.
    /// Estos mocks básicos permiten que el Gateway funcione sin errores.
    /// </summary>
    private void SetupDefaultResponses()
    {
        // ============================================
        // USERS SERVICE - Health Check
        // ============================================
        UsersService!.Given(Request.Create()
            .WithPath("/health")
            .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{
                    ""status"": ""Healthy"",
                    ""service"": ""users"",
                    ""timestamp"": """ + DateTime.UtcNow.ToString("o") + @"""
                }"));

        // Users Service - GET /api/v1/services/users (ruta completa que envía el Gateway)
        UsersService.Given(Request.Create()
            .WithPath("/api/v1/services/users")
            .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"[
                    {
                        ""id"": ""1"",
                        ""name"": ""Test User 1"",
                        ""email"": ""test1@example.com""
                    },
                    {
                        ""id"": ""2"",
                        ""name"": ""Test User 2"",
                        ""email"": ""test2@example.com""
                    }
                ]"));

        // Users Service - POST /api/v1/services/users (crear usuario)
        UsersService.Given(Request.Create()
            .WithPath("/api/v1/services/users")
            .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(201)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{
                    ""id"": ""new-user-id"",
                    ""name"": ""New User"",
                    ""email"": ""newuser@example.com"",
                    ""createdAt"": """ + DateTime.UtcNow.ToString("o") + @"""
                }"));

        // Users Service - GET /api/users (ruta corta para compatibilidad)
        UsersService.Given(Request.Create()
            .WithPath("/api/users")
            .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"[
                    {
                        ""id"": ""1"",
                        ""name"": ""Test User 1"",
                        ""email"": ""test1@example.com""
                    },
                    {
                        ""id"": ""2"",
                        ""name"": ""Test User 2"",
                        ""email"": ""test2@example.com""
                    }
                ]"));

        // Users Service - POST /api/users (crear usuario)
        UsersService.Given(Request.Create()
            .WithPath("/api/users")
            .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(201)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{
                    ""id"": ""new-user-id"",
                    ""name"": ""New User"",
                    ""email"": ""newuser@example.com"",
                    ""createdAt"": """ + DateTime.UtcNow.ToString("o") + @"""
                }"));

        // Users Service - Catch-all para rutas que empiezan con /api/v1/services/users
        UsersService.Given(Request.Create()
            .WithPath("/api/v1/services/users*")
            .UsingAnyMethod())
            .AtPriority(100) // Prioridad media - después de rutas específicas
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{
                    ""message"": ""Mock response from users service"",
                    ""service"": ""users"",
                    ""timestamp"": """ + DateTime.UtcNow.ToString("o") + @"""
                }"));

        // Users Service - Default 404 para cualquier otra ruta
        UsersService.Given(Request.Create()
            .WithPath("/*")
            .UsingAnyMethod())
            .AtPriority(999) // Baja prioridad - solo si no hay match específico
            .RespondWith(Response.Create()
                .WithStatusCode(404)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{
                    ""error"": ""Endpoint not found in mock"",
                    ""service"": ""users""
                }"));

        // ============================================
        // ANALYSIS SERVICE - Health Check
        // ============================================
        AnalysisService!.Given(Request.Create()
            .WithPath("/health")
            .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{
                    ""status"": ""Healthy"",
                    ""service"": ""analysis"",
                    ""timestamp"": """ + DateTime.UtcNow.ToString("o") + @"""
                }"));

        // Analysis Service - GET /api/Analysis
        AnalysisService.Given(Request.Create()
            .WithPath("/api/Analysis")
            .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"[
                    {
                        ""id"": ""analysis-1"",
                        ""url"": ""https://example.com"",
                        ""status"": ""completed"",
                        ""score"": 95
                    }
                ]"));

        // Analysis Service - POST /api/Analysis
        AnalysisService.Given(Request.Create()
            .WithPath("/api/Analysis")
            .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(202)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{
                    ""id"": ""new-analysis-id"",
                    ""status"": ""processing"",
                    ""message"": ""Analysis started""
                }"));

        // Analysis Service - Catch-all para rutas que empiezan con /api/v1/services/analysis
        AnalysisService.Given(Request.Create()
            .WithPath("/api/v1/services/analysis*")
            .UsingAnyMethod())
            .AtPriority(100)
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{
                    ""message"": ""Mock response from analysis service"",
                    ""service"": ""analysis"",
                    ""timestamp"": """ + DateTime.UtcNow.ToString("o") + @"""
                }"));

        // Analysis Service - Default 404
        AnalysisService.Given(Request.Create()
            .WithPath("/*")
            .UsingAnyMethod())
            .AtPriority(999)
            .RespondWith(Response.Create()
                .WithStatusCode(404)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{
                    ""error"": ""Endpoint not found in mock"",
                    ""service"": ""analysis""
                }"));

        // ============================================
        // REPORTS SERVICE - Health Check
        // ============================================
        ReportsService!.Given(Request.Create()
            .WithPath("/health")
            .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{
                    ""status"": ""Healthy"",
                    ""service"": ""reports"",
                    ""timestamp"": """ + DateTime.UtcNow.ToString("o") + @"""
                }"));

        // Reports Service - GET /api/Report
        ReportsService.Given(Request.Create()
            .WithPath("/api/Report")
            .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"[
                    {
                        ""id"": ""report-1"",
                        ""title"": ""Accessibility Report"",
                        ""generatedAt"": """ + DateTime.UtcNow.ToString("o") + @"""
                    }
                ]"));

        // Reports Service - POST /api/Report
        ReportsService.Given(Request.Create()
            .WithPath("/api/Report")
            .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(201)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{
                    ""id"": ""new-report-id"",
                    ""status"": ""generated"",
                    ""url"": ""/api/Report/new-report-id""
                }"));

        // Reports Service - Catch-all para rutas que empiezan con /api/v1/services/reports
        ReportsService.Given(Request.Create()
            .WithPath("/api/v1/services/reports*")
            .UsingAnyMethod())
            .AtPriority(100)
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{
                    ""message"": ""Mock response from reports service"",
                    ""service"": ""reports"",
                    ""timestamp"": """ + DateTime.UtcNow.ToString("o") + @"""
                }"));

        // Reports Service - Default 404
        ReportsService.Given(Request.Create()
            .WithPath("/*")
            .UsingAnyMethod())
            .AtPriority(999)
            .RespondWith(Response.Create()
                .WithStatusCode(404)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{
                    ""error"": ""Endpoint not found in mock"",
                    ""service"": ""reports""
                }"));

        // ============================================
        // MIDDLEWARE SERVICE - Health Check
        // ============================================
        MiddlewareService!.Given(Request.Create()
            .WithPath("/health")
            .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{
                    ""status"": ""Healthy"",
                    ""service"": ""middleware"",
                    ""timestamp"": """ + DateTime.UtcNow.ToString("o") + @"""
                }"));

        // Middleware Service - /metrics endpoint
        MiddlewareService.Given(Request.Create()
            .WithPath("/metrics")
            .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "text/plain")
                .WithBody("# HELP middleware_requests_total Total requests\n# TYPE middleware_requests_total counter\nmiddleware_requests_total 42"));

        // Middleware Service - Catch-all para rutas que empiezan con /api/v1/services/middleware
        MiddlewareService.Given(Request.Create()
            .WithPath("/api/v1/services/middleware*")
            .UsingAnyMethod())
            .AtPriority(100)
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{
                    ""message"": ""Mock response from middleware service"",
                    ""service"": ""middleware"",
                    ""timestamp"": """ + DateTime.UtcNow.ToString("o") + @"""
                }"));

        // Middleware Service - Default response (cualquier otra ruta)
        MiddlewareService.Given(Request.Create()
            .WithPath("/*")
            .UsingAnyMethod())
            .AtPriority(999)
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{
                    ""message"": ""Middleware mock response"",
                    ""service"": ""middleware""
                }"));
    }

    /// <summary>
    /// Resetea todos los mappings a los defaults.
    /// Útil entre tests para limpiar configuraciones específicas.
    /// </summary>
    public void ResetToDefaults()
    {
        UsersService?.Reset();
        AnalysisService?.Reset();
        ReportsService?.Reset();
        MiddlewareService?.Reset();

        SetupDefaultResponses();
    }

    /// <summary>
    /// Obtiene las URLs de los servicios mock.
    /// Útil para configurar el Gateway con las URLs correctas.
    /// </summary>
    public Dictionary<string, string> GetServiceUrls()
    {
        return new Dictionary<string, string>
        {
            ["Gate:Services:users"] = UsersService!.Urls[0],
            ["Gate:Services:analysis"] = AnalysisService!.Urls[0],
            ["Gate:Services:reports"] = ReportsService!.Urls[0],
            ["Gate:Services:middleware"] = MiddlewareService!.Urls[0]
        };
    }

    public async ValueTask DisposeAsync()
    {
        UsersService?.Stop();
        UsersService?.Dispose();

        AnalysisService?.Stop();
        AnalysisService?.Dispose();

        ReportsService?.Stop();
        ReportsService?.Dispose();

        MiddlewareService?.Stop();
        MiddlewareService?.Dispose();

        await Task.CompletedTask;
    }
}
