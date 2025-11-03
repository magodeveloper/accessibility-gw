using Xunit;
using Gateway;
using WireMock.Server;
using WireMock.Settings;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Gateway.IntegrationTests.Fixtures
{

    /// <summary>
    /// Factory para crear y configurar la aplicación Gateway para tests de integración
    /// </summary>
    public class GatewayTestFactory : WebApplicationFactory<Program>, IAsyncLifetime
    {
        // Mock servers para servicios externos
        public WireMockServer UsersServiceMock { get; private set; } = null!;
        public WireMockServer ReportsServiceMock { get; private set; } = null!;
        public WireMockServer AnalysisServiceMock { get; private set; } = null!;
        public WireMockServer MiddlewareServiceMock { get; private set; } = null!;

        // Puertos para los mock servers
        private const int UsersServicePort = 15001;
        private const int ReportsServicePort = 15002;
        private const int AnalysisServicePort = 15003;
        private const int MiddlewareServicePort = 15000;

        public async Task InitializeAsync()
        {
            // Configurar mock servers
            await StartMockServers();
        }

        public new async Task DisposeAsync()
        {
            // Limpiar mock servers
            await StopMockServers();

            // Llamar al método base
            await base.DisposeAsync();
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureAppConfiguration((context, config) =>
            {
                // Configuración específica para tests
                var testConfig = new Dictionary<string, string?>
                {
                    // Gateway configuration
                    ["Gate:ServiceName"] = "TestGateway",
                    ["Gate:Version"] = "1.0.0-test",
                    ["Gate:Environment"] = "Test",
                    ["Gate:DefaultTimeoutSeconds"] = "10",
                    ["Gate:EnableCaching"] = "true",
                    ["Gate:EnableMetrics"] = "true",
                    ["Gate:EnableTracing"] = "false",

                    // Services endpoints (pointing to mock servers)
                    ["Gate:Services:users"] = $"http://localhost:{UsersServicePort}",
                    ["Gate:Services:reports"] = $"http://localhost:{ReportsServicePort}",
                    ["Gate:Services:analysis"] = $"http://localhost:{AnalysisServicePort}",
                    ["Gate:Services:middleware"] = $"http://localhost:{MiddlewareServicePort}",

                    // Health checks configuration
                    ["HealthChecks:CheckIntervalSeconds"] = "5",
                    ["HealthChecks:UnhealthyTimeoutSeconds"] = "3",

                    // Redis configuration (usar memoria para tests)
                    ["Redis:ConnectionString"] = "",
                    ["Redis:Database"] = "0",

                    // Logging configuration
                    ["Logging:LogLevel:Default"] = "Warning",
                    ["Logging:LogLevel:Microsoft"] = "Error",
                    ["Logging:LogLevel:Gateway"] = "Information"
                };

                config.AddInMemoryCollection(testConfig);
            });

            builder.ConfigureServices(services =>
            {
                // Remover registraciones de servicios que puedan interferir con tests
                RemoveService<IHostedService>(services);

                // Usar cache en memoria para tests
                services.AddMemoryCache();
                services.AddSingleton<Microsoft.Extensions.Caching.Distributed.IDistributedCache,
                    Microsoft.Extensions.Caching.Distributed.MemoryDistributedCache>();
            });

            builder.UseEnvironment("Test");
        }

        private async Task StartMockServers()
        {
            // Configurar y iniciar mock servers
            var settings = new WireMockServerSettings
            {
                StartAdminInterface = false,
                ReadStaticMappings = false
            };

            try
            {
                UsersServiceMock = WireMockServer.Start(new WireMockServerSettings { Port = UsersServicePort });
                ReportsServiceMock = WireMockServer.Start(new WireMockServerSettings { Port = ReportsServicePort });
                AnalysisServiceMock = WireMockServer.Start(new WireMockServerSettings { Port = AnalysisServicePort });
                MiddlewareServiceMock = WireMockServer.Start(new WireMockServerSettings { Port = MiddlewareServicePort });

                // Configurar respuestas básicas de health check
                SetupBasicHealthCheckResponses();

                await Task.Delay(100); // Pequeña pausa para asegurar que los servidores estén listos
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to start mock servers for integration tests", ex);
            }
        }

        private void SetupBasicHealthCheckResponses()
        {
            var healthResponse = new { status = "healthy", timestamp = DateTimeOffset.UtcNow };

            // Health checks para todos los servicios mock
            foreach (var server in new[] { UsersServiceMock, ReportsServiceMock, AnalysisServiceMock, MiddlewareServiceMock })
            {
                server
                    .Given(WireMock.RequestBuilders.Request.Create()
                        .WithPath("/health")
                        .UsingGet())
                    .RespondWith(WireMock.ResponseBuilders.Response.Create()
                        .WithStatusCode(200)
                        .WithHeader("Content-Type", "application/json")
                        .WithBodyAsJson(healthResponse));

                server
                    .Given(WireMock.RequestBuilders.Request.Create()
                        .WithPath("/health/live")
                        .UsingGet())
                    .RespondWith(WireMock.ResponseBuilders.Response.Create()
                        .WithStatusCode(200)
                        .WithHeader("Content-Type", "application/json")
                        .WithBodyAsJson(healthResponse));

                server
                    .Given(WireMock.RequestBuilders.Request.Create()
                        .WithPath("/health/ready")
                        .UsingGet())
                    .RespondWith(WireMock.ResponseBuilders.Response.Create()
                        .WithStatusCode(200)
                        .WithHeader("Content-Type", "application/json")
                        .WithBodyAsJson(healthResponse));
            }
        }

        private async Task StopMockServers()
        {
            try
            {
                UsersServiceMock?.Stop();
                ReportsServiceMock?.Stop();
                AnalysisServiceMock?.Stop();
                MiddlewareServiceMock?.Stop();
            }
            catch (Exception ex)
            {
                // Log pero no fallar - los servers pueden ya estar detenidos
                Console.WriteLine($"Warning: Error stopping mock servers: {ex.Message}");
            }

            await Task.CompletedTask;
        }

        /// <summary>
        /// Remueve un servicio del contenedor de DI
        /// </summary>
        private static void RemoveService<T>(IServiceCollection services)
        {
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(T));
            if (descriptor != null)
            {
                services.Remove(descriptor);
            }
        }

        /// <summary>
        /// Resetea todos los mock servers a su estado inicial
        /// </summary>
        public void ResetMockServers()
        {
            UsersServiceMock?.Reset();
            ReportsServiceMock?.Reset();
            AnalysisServiceMock?.Reset();
            MiddlewareServiceMock?.Reset();

            // Reconfigurar respuestas básicas
            SetupBasicHealthCheckResponses();
        }

        /// <summary>
        /// Configura un mock server específico con respuestas personalizadas
        /// </summary>
        public void ConfigureMockServer(string serviceName, Action<WireMockServer> configure)
        {
            var server = serviceName.ToLowerInvariant() switch
            {
                "users" => UsersServiceMock,
                "reports" => ReportsServiceMock,
                "analysis" => AnalysisServiceMock,
                "middleware" => MiddlewareServiceMock,
                _ => throw new ArgumentException($"Unknown service: {serviceName}", nameof(serviceName))
            };

            configure(server);
        }

        /// <summary>
        /// Verifica que todos los mock servers estén funcionando
        /// </summary>
        public async Task<bool> VerifyMockServersAsync()
        {
            var httpClient = new HttpClient();
            var servers = new[]
            {
            ($"Users", $"http://localhost:{UsersServicePort}/health"),
            ($"Reports", $"http://localhost:{ReportsServicePort}/health"),
            ($"Analysis", $"http://localhost:{AnalysisServicePort}/health"),
            ($"Middleware", $"http://localhost:{MiddlewareServicePort}/health")
        };

            foreach (var (name, url) in servers)
            {
                try
                {
                    var response = await httpClient.GetAsync(url);
                    if (!response.IsSuccessStatusCode)
                    {
                        Console.WriteLine($"Mock server {name} is not responding correctly: {response.StatusCode}");
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to connect to mock server {name}: {ex.Message}");
                    return false;
                }
            }

            return true;
        }
    }
}
