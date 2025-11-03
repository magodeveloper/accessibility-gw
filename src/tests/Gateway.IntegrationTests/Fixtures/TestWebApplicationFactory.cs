using Gateway;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Gateway.IntegrationTests.Fixtures
{
    /// <summary>
    /// Clase base para crear WebApplicationFactory con configuración común de tests.
    /// Proporciona configuración estándar para evitar duplicación en archivos de tests.
    /// Incluye servicios mock con WireMock para simular backend.
    /// </summary>
    /// <remarks>
    /// Esta clase base establece:
    /// - Environment = "Test" para evitar carga de User Secrets
    /// - Servicios backend mock con WireMock
    /// - JWT deshabilitado por defecto (se puede habilitar)
    /// - Cache en memoria (Redis deshabilitado por defecto)
    /// - Logging reducido para tests
    /// 
    /// Uso:
    /// <code>
    /// public class MyTests : IClassFixture&lt;TestWebApplicationFactory&gt;
    /// {
    ///     private readonly HttpClient _client;
    ///     
    ///     public MyTests(TestWebApplicationFactory factory)
    ///     {
    ///         _client = factory.CreateClient();
    ///     }
    /// }
    /// </code>
    /// </remarks>
    public class TestWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
    {
        private MockServiceFactory? _mockServices;

        /// <summary>
        /// Proporciona acceso a los servicios mock para configuración específica en tests.
        /// </summary>
        public MockServiceFactory MockServices => _mockServices ?? throw new InvalidOperationException("Mock services not initialized. Call InitializeAsync first.");

        /// <summary>
        /// Inicializa los servicios mock antes de crear la aplicación.
        /// </summary>
        public async Task InitializeAsync()
        {
            _mockServices = new MockServiceFactory();
            await _mockServices.InitializeAsync();
        }

        /// <summary>
        /// Limpia los servicios mock después de los tests.
        /// </summary>
        public new async Task DisposeAsync()
        {
            if (_mockServices != null)
            {
                await _mockServices.DisposeAsync();
            }
            await base.DisposeAsync();
        }
        /// <summary>
        /// Configuración por defecto para todos los tests.
        /// Sobrescribe este método para personalizar configuración en clases derivadas.
        /// </summary>
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            // CRÍTICO: Establecer environment a "Test" PRIMERO para evitar carga de User Secrets
            builder.UseEnvironment("Test");

            builder.ConfigureAppConfiguration((context, config) =>
            {
                // CRÍTICO: Limpiar todas las fuentes de configuración existentes
                // para evitar que User Secrets y otros providers sobrescriban configuración de test
                config.Sources.Clear();

                // Agregar configuración base de tests
                config.AddInMemoryCollection(GetDefaultTestConfiguration());
            });

            builder.ConfigureServices(services =>
            {
                // Configuración adicional de servicios si es necesario
                ConfigureTestServices(services);
            });
        }

        /// <summary>
        /// Obtiene la configuración por defecto para tests.
        /// Puede ser sobrescrito en clases derivadas para agregar configuración adicional.
        /// </summary>
        protected virtual Dictionary<string, string?> GetDefaultTestConfiguration()
        {
            // CRÍTICO: Usar URLs de servicios mock si están disponibles
            var config = new Dictionary<string, string?>
            {
                // ============================================
                // Gateway Configuration
                // ============================================
                ["Gate:ServiceName"] = "TestGateway",
                ["Gate:Version"] = "1.0.0-test",
                ["Gate:Environment"] = "Test",
                ["Gate:DefaultTimeoutSeconds"] = "10",
                ["Gate:EnableCaching"] = "true",
                ["Gate:EnableMetrics"] = "true",
                ["Gate:EnableTracing"] = "false",

                // ============================================
                // Allowed Routes Configuration (CRÍTICO para evitar 403)
                // ============================================
                // Rutas de usuarios
                ["Gate:AllowedRoutes:0:service"] = "users",
                ["Gate:AllowedRoutes:0:methods:0"] = "GET",
                ["Gate:AllowedRoutes:0:methods:1"] = "POST",
                ["Gate:AllowedRoutes:0:methods:2"] = "PUT",
                ["Gate:AllowedRoutes:0:methods:3"] = "PATCH",
                ["Gate:AllowedRoutes:0:methods:4"] = "DELETE",
                ["Gate:AllowedRoutes:0:pathPrefix"] = "/api/v1/services/users",
                ["Gate:AllowedRoutes:0:requiresAuth"] = "false",

                // Rutas de análisis
                ["Gate:AllowedRoutes:1:service"] = "analysis",
                ["Gate:AllowedRoutes:1:methods:0"] = "GET",
                ["Gate:AllowedRoutes:1:methods:1"] = "POST",
                ["Gate:AllowedRoutes:1:methods:2"] = "PUT",
                ["Gate:AllowedRoutes:1:methods:3"] = "PATCH",
                ["Gate:AllowedRoutes:1:methods:4"] = "DELETE",
                ["Gate:AllowedRoutes:1:pathPrefix"] = "/api/v1/services/analysis",
                ["Gate:AllowedRoutes:1:requiresAuth"] = "false",

                // Rutas de reportes
                ["Gate:AllowedRoutes:2:service"] = "reports",
                ["Gate:AllowedRoutes:2:methods:0"] = "GET",
                ["Gate:AllowedRoutes:2:methods:1"] = "POST",
                ["Gate:AllowedRoutes:2:methods:2"] = "PUT",
                ["Gate:AllowedRoutes:2:methods:3"] = "PATCH",
                ["Gate:AllowedRoutes:2:methods:4"] = "DELETE",
                ["Gate:AllowedRoutes:2:pathPrefix"] = "/api/v1/services/reports",
                ["Gate:AllowedRoutes:2:requiresAuth"] = "false",

                // Rutas de middleware
                ["Gate:AllowedRoutes:3:service"] = "middleware",
                ["Gate:AllowedRoutes:3:methods:0"] = "GET",
                ["Gate:AllowedRoutes:3:methods:1"] = "POST",
                ["Gate:AllowedRoutes:3:methods:2"] = "PUT",
                ["Gate:AllowedRoutes:3:methods:3"] = "PATCH",
                ["Gate:AllowedRoutes:3:methods:4"] = "DELETE",
                ["Gate:AllowedRoutes:3:pathPrefix"] = "/api/v1/services/middleware",
                ["Gate:AllowedRoutes:3:requiresAuth"] = "false",

                // ============================================
                // Health Checks Configuration
                // ============================================
                ["HealthChecks:CheckIntervalSeconds"] = "5",
                ["HealthChecks:UnhealthyTimeoutSeconds"] = "2",
                ["HealthChecks:Timeout"] = "00:00:03",

                // ============================================
                // Redis Configuration (Disabled for Tests)
                // ============================================
                ["Redis:ConnectionString"] = "",
                ["Redis:Database"] = "0",

                // ============================================
                // JWT Configuration (Disabled for Tests)
                // ============================================
                ["Jwt:SecretKey"] = "",
                ["Jwt:ValidIssuer"] = "TestIssuer",
                ["Jwt:ValidAudience"] = "TestAudience",
                ["Jwt:TokenExpirationMinutes"] = "60",

                // ============================================
                // Rate Limiting Configuration
                // ============================================
                ["RateLimiting:Enabled"] = "false",
                ["RateLimiting:PermitLimit"] = "100",
                ["RateLimiting:WindowSeconds"] = "60",

                // ============================================
                // CORS Configuration
                // ============================================
                ["Cors:Enabled"] = "true",
                ["Cors:AllowedOrigins"] = "*",

                // ============================================
                // Logging Configuration (Minimal for Tests)
                // ============================================
                ["Logging:LogLevel:Default"] = "Warning",
                ["Logging:LogLevel:Microsoft"] = "Error",
                ["Logging:LogLevel:Microsoft.AspNetCore"] = "Error",
                ["Logging:LogLevel:Gateway"] = "Information",

                // ============================================
                // Cache Configuration
                // ============================================
                ["Cache:DefaultExpirationMinutes"] = "5",
                ["Cache:Enabled"] = "true"
            };

            // Agregar URLs de servicios mock si están disponibles
            if (_mockServices != null)
            {
                foreach (var kvp in _mockServices.GetServiceUrls())
                {
                    config[kvp.Key] = kvp.Value;
                }
            }
            else
            {
                // Fallback a URLs localhost (no se usarán en tests reales)
                config["Gate:Services:users"] = "http://localhost:5001";
                config["Gate:Services:reports"] = "http://localhost:5003";
                config["Gate:Services:analysis"] = "http://localhost:5002";
                config["Gate:Services:middleware"] = "http://localhost:3001";
            }

            return config;
        }

        /// <summary>
        /// Configura servicios adicionales para tests.
        /// Puede ser sobrescrito en clases derivadas para agregar servicios mock o test doubles.
        /// </summary>
        protected virtual void ConfigureTestServices(IServiceCollection services)
        {
            // Remover servicios hosted que puedan interferir con tests
            RemoveHostedServices(services);

            // Asegurar que usamos cache en memoria para tests
            EnsureMemoryCache(services);
        }

        /// <summary>
        /// Remueve todos los IHostedService registrados para evitar background tasks en tests.
        /// </summary>
        private void RemoveHostedServices(IServiceCollection services)
        {
            var hostedServices = services
                .Where(descriptor => descriptor.ServiceType == typeof(IHostedService))
                .ToList();

            foreach (var service in hostedServices)
            {
                services.Remove(service);
            }
        }

        /// <summary>
        /// Asegura que el cache en memoria está configurado para tests.
        /// Reemplaza cualquier implementación de Redis con MemoryDistributedCache.
        /// </summary>
        private void EnsureMemoryCache(IServiceCollection services)
        {
            // CRÍTICO: Remover cualquier IDistributedCache existente (RedisCache)
            var distributedCacheDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(Microsoft.Extensions.Caching.Distributed.IDistributedCache));
            if (distributedCacheDescriptor != null)
            {
                services.Remove(distributedCacheDescriptor);
            }

            // Agregar IMemoryCache si no existe
            if (!services.Any(s => s.ServiceType == typeof(Microsoft.Extensions.Caching.Memory.IMemoryCache)))
            {
                services.AddMemoryCache();
            }

            // Agregar MemoryDistributedCache como implementación de IDistributedCache
            services.AddSingleton<Microsoft.Extensions.Caching.Distributed.IDistributedCache,
                Microsoft.Extensions.Caching.Distributed.MemoryDistributedCache>();
        }
    }

    /// <summary>
    /// Extension methods para simplificar configuración de tests.
    /// </summary>
    public static class TestWebApplicationFactoryExtensions
    {
        /// <summary>
        /// Crea una nueva factory con configuración personalizada manteniendo la configuración base.
        /// </summary>
        /// <param name="factory">Factory base</param>
        /// <param name="additionalConfig">Configuración adicional a agregar</param>
        /// <returns>Nueva factory con configuración combinada</returns>
        public static WebApplicationFactory<Program> WithAdditionalConfiguration(
            this TestWebApplicationFactory factory,
            Dictionary<string, string?> additionalConfig)
        {
            return factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((context, config) =>
                {
                    config.AddInMemoryCollection(additionalConfig);
                });
            });
        }

        /// <summary>
        /// Crea una nueva factory con JWT habilitado para tests de autenticación.
        /// </summary>
        /// <param name="factory">Factory base</param>
        /// <param name="secretKey">Secret key para JWT (64+ caracteres recomendados)</param>
        /// <returns>Nueva factory con JWT habilitado</returns>
        public static WebApplicationFactory<Program> WithJwtEnabled(
            this TestWebApplicationFactory factory,
            string secretKey = "Test-Secret-Key-For-JWT-Authentication-Min-64-Characters-Long-12345")
        {
            return factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((context, config) =>
                {
                    config.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["Jwt:SecretKey"] = secretKey,
                        ["Jwt:ValidIssuer"] = "TestIssuer",
                        ["Jwt:ValidAudience"] = "TestAudience"
                    });
                });
            });
        }

        /// <summary>
        /// Crea una nueva factory con Redis habilitado para tests de cache distribuido.
        /// </summary>
        /// <param name="factory">Factory base</param>
        /// <param name="connectionString">Connection string de Redis</param>
        /// <returns>Nueva factory con Redis habilitado</returns>
        public static WebApplicationFactory<Program> WithRedisEnabled(
            this TestWebApplicationFactory factory,
            string connectionString = "localhost:6379")
        {
            return factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((context, config) =>
                {
                    config.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["Redis:ConnectionString"] = connectionString
                    });
                });
            });
        }
    }
}
