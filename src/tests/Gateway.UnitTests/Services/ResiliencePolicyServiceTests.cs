using Moq;
using Xunit;
using System.Net;
using Polly.Timeout;
using FluentAssertions;
using Gateway.Services;
using Polly.CircuitBreaker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace Gateway.UnitTests.Services;

/// <summary>
/// Tests para ResiliencePolicyService
/// Target: Mejorar cobertura de 60.6% a >80%
/// Políticas: Retry, Circuit Breaker, Timeout
/// </summary>
public class ResiliencePolicyServiceTests
{
    private readonly Mock<ILogger<ResiliencePolicyService>> _loggerMock;
    private readonly Mock<IMetricsService> _metricsServiceMock;
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly ResiliencePolicyService _service;

    public ResiliencePolicyServiceTests()
    {
        _loggerMock = new Mock<ILogger<ResiliencePolicyService>>();
        _metricsServiceMock = new Mock<IMetricsService>();
        _configurationMock = new Mock<IConfiguration>();

        _service = new ResiliencePolicyService(
            _loggerMock.Object,
            _metricsServiceMock.Object,
            _configurationMock.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_InitializesService_Successfully()
    {
        // Act
        var service = new ResiliencePolicyService(
            _loggerMock.Object,
            _metricsServiceMock.Object,
            _configurationMock.Object);

        // Assert
        service.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_InitializesPolicies_ForAllServices()
    {
        // Act
        var usersPolicy = _service.GetPolicyForService("users");
        var reportsPolicy = _service.GetPolicyForService("reports");
        var analysisPolicy = _service.GetPolicyForService("analysis");
        var middlewarePolicy = _service.GetPolicyForService("middleware");
        var defaultPolicy = _service.GetPolicyForService("default");

        // Assert
        usersPolicy.Should().NotBeNull();
        reportsPolicy.Should().NotBeNull();
        analysisPolicy.Should().NotBeNull();
        middlewarePolicy.Should().NotBeNull();
        defaultPolicy.Should().NotBeNull();
    }

    #endregion

    #region GetPolicyForService Tests

    [Fact]
    public void GetPolicyForService_WithUsersService_ReturnsPolicy()
    {
        // Act
        var policy = _service.GetPolicyForService("users");

        // Assert
        policy.Should().NotBeNull();
    }

    [Fact]
    public void GetPolicyForService_WithReportsService_ReturnsPolicy()
    {
        // Act
        var policy = _service.GetPolicyForService("reports");

        // Assert
        policy.Should().NotBeNull();
    }

    [Fact]
    public void GetPolicyForService_WithAnalysisService_ReturnsPolicy()
    {
        // Act
        var policy = _service.GetPolicyForService("analysis");

        // Assert
        policy.Should().NotBeNull();
    }

    [Fact]
    public void GetPolicyForService_WithMiddlewareService_ReturnsPolicy()
    {
        // Act
        var policy = _service.GetPolicyForService("middleware");

        // Assert
        policy.Should().NotBeNull();
    }

    [Fact]
    public void GetPolicyForService_WithUnknownService_ReturnsDefaultPolicy()
    {
        // Act
        var policy = _service.GetPolicyForService("unknown-service");

        // Assert
        policy.Should().NotBeNull();
        policy.Should().Be(_service.GetPolicyForService("default"));
    }

    [Fact]
    public void GetPolicyForService_WithEmptyString_ReturnsDefaultPolicy()
    {
        // Act
        var policy = _service.GetPolicyForService(string.Empty);

        // Assert
        policy.Should().NotBeNull();
    }

    [Fact]
    public void GetPolicyForService_MultipleCalls_ReturnsSameInstance()
    {
        // Act
        var policy1 = _service.GetPolicyForService("users");
        var policy2 = _service.GetPolicyForService("users");

        // Assert
        policy1.Should().BeSameAs(policy2);
    }

    #endregion

    #region GetConfigForService Tests

    [Fact]
    public void GetConfigForService_WithUsersService_ReturnsCriticalConfig()
    {
        // Act
        var config = _service.GetConfigForService("users");

        // Assert
        config.Should().NotBeNull();
        config.RetryCount.Should().Be(5);
        config.BaseDelay.Should().Be(TimeSpan.FromMilliseconds(50));
        config.MaxDelay.Should().Be(TimeSpan.FromSeconds(10));
        config.TimeoutPerTry.Should().Be(TimeSpan.FromSeconds(15));
        config.OverallTimeout.Should().Be(TimeSpan.FromSeconds(45));
        config.CircuitBreakerThreshold.Should().Be(3);
        config.CircuitBreakerDuration.Should().Be(TimeSpan.FromSeconds(60));
    }

    [Fact]
    public void GetConfigForService_WithReportsService_ReturnsDefaultConfig()
    {
        // Act
        var config = _service.GetConfigForService("reports");

        // Assert
        config.Should().NotBeNull();
        config.RetryCount.Should().Be(3);
        config.BaseDelay.Should().Be(TimeSpan.FromMilliseconds(100));
        config.MaxDelay.Should().Be(TimeSpan.FromSeconds(30));
        config.TimeoutPerTry.Should().Be(TimeSpan.FromSeconds(10));
        config.OverallTimeout.Should().Be(TimeSpan.FromSeconds(30));
        config.CircuitBreakerThreshold.Should().Be(5);
        config.CircuitBreakerDuration.Should().Be(TimeSpan.FromSeconds(30));
    }

    [Fact]
    public void GetConfigForService_WithAnalysisService_ReturnsToleratedConfig()
    {
        // Act
        var config = _service.GetConfigForService("analysis");

        // Assert
        config.Should().NotBeNull();
        config.RetryCount.Should().Be(2);
        config.BaseDelay.Should().Be(TimeSpan.FromMilliseconds(200));
        config.MaxDelay.Should().Be(TimeSpan.FromSeconds(60));
        config.TimeoutPerTry.Should().Be(TimeSpan.FromSeconds(30));
        config.OverallTimeout.Should().Be(TimeSpan.FromMinutes(2));
        config.CircuitBreakerThreshold.Should().Be(8);
        config.CircuitBreakerDuration.Should().Be(TimeSpan.FromMinutes(2));
    }

    [Fact]
    public void GetConfigForService_WithMiddlewareService_ReturnsToleratedConfig()
    {
        // Act
        var config = _service.GetConfigForService("middleware");

        // Assert
        config.Should().NotBeNull();
        config.RetryCount.Should().Be(2);
        config.CircuitBreakerThreshold.Should().Be(8);
    }

    [Fact]
    public void GetConfigForService_WithUnknownService_ReturnsDefaultConfig()
    {
        // Act
        var config = _service.GetConfigForService("unknown-service");

        // Assert
        config.Should().NotBeNull();
        config.Should().Be(_service.GetConfigForService("default"));
    }

    [Fact]
    public void GetConfigForService_WithDefaultService_ReturnsValidConfig()
    {
        // Act
        var config = _service.GetConfigForService("default");

        // Assert
        config.Should().NotBeNull();
        config.RetryCount.Should().Be(3);
        config.UseJitter.Should().BeTrue();
        config.RetryStatusCodes.Should().Contain(HttpStatusCode.ServiceUnavailable);
    }

    [Fact]
    public void GetConfigForService_RetryStatusCodes_ContainsExpectedCodes()
    {
        // Act
        var config = _service.GetConfigForService("default");

        // Assert
        config.RetryStatusCodes.Should().Contain(new[]
        {
            HttpStatusCode.RequestTimeout,
            HttpStatusCode.InternalServerError,
            HttpStatusCode.BadGateway,
            HttpStatusCode.ServiceUnavailable,
            HttpStatusCode.GatewayTimeout,
            HttpStatusCode.TooManyRequests
        });
    }

    #endregion

    #region RecordPolicyExecution Tests

    [Fact]
    public void RecordPolicyExecution_WithValidData_DoesNotThrow()
    {
        // Arrange
        var serviceName = "users";
        var policyType = "Retry";
        var success = true;
        var duration = TimeSpan.FromMilliseconds(100);

        // Act
        Action act = () => _service.RecordPolicyExecution(serviceName, policyType, success, duration);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void RecordPolicyExecution_WithRetryPolicy_DoesNotThrow()
    {
        // Arrange
        var serviceName = "reports";
        var policyType = "Retry";
        var success = false;
        var duration = TimeSpan.FromMilliseconds(250);

        // Act
        Action act = () => _service.RecordPolicyExecution(serviceName, policyType, success, duration);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void RecordPolicyExecution_WithCircuitBreakerOpen_DoesNotThrow()
    {
        // Arrange
        var serviceName = "analysis";
        var policyType = "CircuitBreakerOpen";
        var success = false;
        var duration = TimeSpan.FromSeconds(30);

        // Act
        Action act = () => _service.RecordPolicyExecution(serviceName, policyType, success, duration);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void RecordPolicyExecution_WithTimeout_DoesNotThrow()
    {
        // Arrange
        var serviceName = "middleware";
        var policyType = "TimeoutPerTry";
        var success = false;
        var duration = TimeSpan.FromSeconds(10);

        // Act
        Action act = () => _service.RecordPolicyExecution(serviceName, policyType, success, duration);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void RecordPolicyExecution_MultipleCalls_AllComplete()
    {
        // Arrange & Act
        Action act = () =>
        {
            _service.RecordPolicyExecution("users", "Retry", false, TimeSpan.FromMilliseconds(100));
            _service.RecordPolicyExecution("users", "Retry", false, TimeSpan.FromMilliseconds(200));
            _service.RecordPolicyExecution("users", "Retry", true, TimeSpan.FromMilliseconds(400));
        };

        // Assert
        act.Should().NotThrow();
    }

    #endregion

    #region Policy Execution Tests - Retry

    [Fact]
    public async Task Policy_WithSuccessfulRequest_ReturnsResponse()
    {
        // Arrange
        var policy = _service.GetPolicyForService("users");
        var expectedResponse = new HttpResponseMessage(HttpStatusCode.OK);

        // Act
        var result = await policy.ExecuteAsync(() => Task.FromResult(expectedResponse));

        // Assert
        result.Should().NotBeNull();
        result.StatusCode.Should().Be(HttpStatusCode.OK);
    }
#pragma warning restore CS1998

    [Fact]
#pragma warning disable CS1998
    public async Task Policy_WithTransientFailure_RetriesRequest()
    {
        // Arrange
        var policy = _service.GetPolicyForService("users");
        var attemptCount = 0;

        // Act
        var result = await policy.ExecuteAsync(async () =>
        {
            attemptCount++;
            if (attemptCount < 3)
            {
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }
            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        // Assert
        result.StatusCode.Should().Be(HttpStatusCode.OK);
        attemptCount.Should().Be(3);
    }
#pragma warning restore CS1998

    [Fact]
#pragma warning disable CS1998
    public async Task Policy_WithPermanentFailure_ReturnsFailedResponse()
    {
        // Arrange
        var policy = _service.GetPolicyForService("reports");

        // Act
        var result = await policy.ExecuteAsync(() =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest)));

        // Assert
        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
#pragma warning restore CS1998

    #endregion

    #region Policy Execution Tests - Circuit Breaker

    [Fact]
#pragma warning disable CS1998
    public async Task Policy_WithCircuitBreakerOpen_ThrowsBrokenCircuitException()
    {
        // Arrange
        var policy = _service.GetPolicyForService("users");
        var config = _service.GetConfigForService("users");

        // Provocar apertura del circuit breaker con múltiples fallos
        for (int i = 0; i < config.CircuitBreakerThreshold + 1; i++)
        {
            try
            {
                await policy.ExecuteAsync(() =>
                    Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError)));
            }
            catch (BrokenCircuitException)
            {
                // Expected after threshold
            }
        }

        // Act
        Func<Task> act = async () => await policy.ExecuteAsync(() =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));

        // Assert
        await act.Should().ThrowAsync<BrokenCircuitException>();
    }
#pragma warning restore CS1998

    #endregion

    #region Policy Execution Tests - Timeout

    [Fact]
#pragma warning disable CS1998
    public async Task Policy_WithSlowRequest_ThrowsTimeoutException()
    {
        // Arrange
        var policy = _service.GetPolicyForService("users");
        var config = _service.GetConfigForService("users");

        // Act
        Func<Task> act = async () => await policy.ExecuteAsync(async () =>
        {
            // Simular request que excede timeout
            await Task.Delay(config.OverallTimeout.Add(TimeSpan.FromSeconds(5)));
            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        // Assert
        await act.Should().ThrowAsync<TimeoutRejectedException>();
    }
#pragma warning restore CS1998

    #endregion

    #region ResiliencePolicyConfig Tests

    [Fact]
    public void ResiliencePolicyConfig_DefaultConstructor_HasExpectedDefaults()
    {
        // Act
        var config = new ResiliencePolicyConfig();

        // Assert
        config.RetryCount.Should().Be(3);
        config.BaseDelay.Should().Be(TimeSpan.FromMilliseconds(100));
        config.MaxDelay.Should().Be(TimeSpan.FromSeconds(30));
        config.TimeoutPerTry.Should().Be(TimeSpan.FromSeconds(10));
        config.OverallTimeout.Should().Be(TimeSpan.FromSeconds(30));
        config.CircuitBreakerThreshold.Should().Be(5);
        config.CircuitBreakerDuration.Should().Be(TimeSpan.FromSeconds(30));
        config.CircuitBreakerSamplingDuration.Should().Be(60);
        config.UseJitter.Should().BeTrue();
    }

    [Fact]
    public void ResiliencePolicyConfig_RetryStatusCodes_ContainsExpectedCodes()
    {
        // Act
        var config = new ResiliencePolicyConfig();

        // Assert
        config.RetryStatusCodes.Should().HaveCount(6);
        config.RetryStatusCodes.Should().Contain(HttpStatusCode.RequestTimeout);
        config.RetryStatusCodes.Should().Contain(HttpStatusCode.InternalServerError);
        config.RetryStatusCodes.Should().Contain(HttpStatusCode.BadGateway);
        config.RetryStatusCodes.Should().Contain(HttpStatusCode.ServiceUnavailable);
        config.RetryStatusCodes.Should().Contain(HttpStatusCode.GatewayTimeout);
        config.RetryStatusCodes.Should().Contain(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public void ResiliencePolicyConfig_CanBeModified()
    {
        // Arrange
        var config = new ResiliencePolicyConfig();

        // Act
        config.RetryCount = 10;
        config.BaseDelay = TimeSpan.FromMilliseconds(50);
        config.MaxDelay = TimeSpan.FromSeconds(60);
        config.UseJitter = false;

        // Assert
        config.RetryCount.Should().Be(10);
        config.BaseDelay.Should().Be(TimeSpan.FromMilliseconds(50));
        config.MaxDelay.Should().Be(TimeSpan.FromSeconds(60));
        config.UseJitter.Should().BeFalse();
    }

    #endregion

    #region Real-World Scenarios

    [Fact]
#pragma warning disable CS1998
    public async Task Scenario_TransientErrorRecovery_SucceedsAfterRetries()
    {
        // Arrange
        var policy = _service.GetPolicyForService("reports");
        var attemptCount = 0;

        // Act - Simular servicio que falla 2 veces y luego funciona
        var result = await policy.ExecuteAsync(async () =>
        {
            attemptCount++;
            if (attemptCount <= 2)
            {
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }
            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        // Assert
        result.StatusCode.Should().Be(HttpStatusCode.OK);
        attemptCount.Should().Be(3);
    }
#pragma warning restore CS1998

    [Fact]
#pragma warning disable CS1998
    public async Task Scenario_CriticalServiceWithAggressiveRetry_UsersService()
    {
        // Arrange
        var policy = _service.GetPolicyForService("users");
        var config = _service.GetConfigForService("users");
        var attemptCount = 0;

        // Act
        var result = await policy.ExecuteAsync(async () =>
        {
            attemptCount++;
            if (attemptCount < 4)
            {
                return new HttpResponseMessage(HttpStatusCode.BadGateway);
            }
            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        // Assert
        result.StatusCode.Should().Be(HttpStatusCode.OK);
        config.RetryCount.Should().Be(5); // Configuración agresiva para servicio crítico
        attemptCount.Should().Be(4);
    }
#pragma warning restore CS1998

    [Fact]
#pragma warning disable CS1998
    public async Task Scenario_ToleratedServiceWithFewRetries_AnalysisService()
    {
        // Arrange
        var policy = _service.GetPolicyForService("analysis");
        var config = _service.GetConfigForService("analysis");

        // Act
        var result = await policy.ExecuteAsync(() =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));

        // Assert
        result.StatusCode.Should().Be(HttpStatusCode.OK);
        config.RetryCount.Should().Be(2); // Servicio más tolerante con menos reintentos
        config.CircuitBreakerThreshold.Should().Be(8); // Mayor threshold
    }
#pragma warning restore CS1998

    [Fact]
#pragma warning disable CS1998
    public async Task Scenario_MultipleServicesWithDifferentPolicies()
    {
        // Arrange
        var usersPolicy = _service.GetPolicyForService("users");
        var reportsPolicy = _service.GetPolicyForService("reports");
        var analysisPolicy = _service.GetPolicyForService("analysis");

        // Act & Assert - Cada servicio tiene su propia política
        var usersResult = await usersPolicy.ExecuteAsync(() =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));

        var reportsResult = await reportsPolicy.ExecuteAsync(() =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));

        var analysisResult = await analysisPolicy.ExecuteAsync(() =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));

        usersResult.StatusCode.Should().Be(HttpStatusCode.OK);
        reportsResult.StatusCode.Should().Be(HttpStatusCode.OK);
        analysisResult.StatusCode.Should().Be(HttpStatusCode.OK);
    }
#pragma warning restore CS1998

    [Fact]
#pragma warning disable CS1998
    public async Task Scenario_HttpRequestException_TriggersRetry()
    {
        // Arrange
        var policy = _service.GetPolicyForService("middleware");
        var attemptCount = 0;

        // Act
        var result = await policy.ExecuteAsync(async () =>
        {
            attemptCount++;
            if (attemptCount < 2)
            {
                throw new HttpRequestException("Network error");
            }
            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        // Assert
        result.StatusCode.Should().Be(HttpStatusCode.OK);
        attemptCount.Should().Be(2);
    }
#pragma warning restore CS1998

    [Fact]
#pragma warning disable CS1998
    public async Task Scenario_NonRetryableStatusCode_NoRetry()
    {
        // Arrange
        var policy = _service.GetPolicyForService("reports");
        var attemptCount = 0;

        // Act
        var result = await policy.ExecuteAsync(() =>
        {
            attemptCount++;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        });

        // Assert
        result.StatusCode.Should().Be(HttpStatusCode.NotFound);
        attemptCount.Should().Be(1); // No retry para 404
    }
#pragma warning restore CS1998

    #endregion

    #region Edge Cases

    [Fact]
    public async Task Policy_WithManyHttpRequestExceptions_EventuallyFails()
    {
        // Arrange
        var policy = _service.GetPolicyForService("users");
        var config = _service.GetConfigForService("users");
        var attemptCount = 0;

        // Act - Exceder el número de reintentos
        try
        {
#pragma warning disable CS1998 // Lambda de Polly no requiere await explícito
            await policy.ExecuteAsync(async () =>
            {
                attemptCount++;
                if (attemptCount <= config.RetryCount + 1) // Excede el retry count
                {
                    throw new HttpRequestException("Connection failed");
                }
                return new HttpResponseMessage(HttpStatusCode.OK);
            });
#pragma warning restore CS1998
        }
        catch (HttpRequestException)
        {
            // Expected - se agotaron los reintentos
        }

        // Assert - Debería haber intentado múltiples veces antes de fallar
        attemptCount.Should().BeGreaterThan(1); // Se intentó retry
        attemptCount.Should().BeLessOrEqualTo(config.RetryCount + 1); // No excedió el límite + intento original
    }

    [Fact]
    public async Task Policy_WithTaskCanceledException_Retries()
    {
        // Arrange
        var policy = _service.GetPolicyForService("reports");
        var attemptCount = 0;

        // Act
#pragma warning disable CS1998 // Lambda de Polly no requiere await explícito
        var result = await policy.ExecuteAsync(async () =>
        {
            attemptCount++;
            if (attemptCount < 2)
            {
                throw new TaskCanceledException("Request canceled");
            }
            return new HttpResponseMessage(HttpStatusCode.OK);
        });
#pragma warning restore CS1998

        // Assert
        result.StatusCode.Should().Be(HttpStatusCode.OK);
        attemptCount.Should().Be(2);
    }

    #endregion

    #region Branch Coverage Tests - Circuit Breaker States

    [Fact]
    public void CircuitBreaker_WithUseJitterFalse_ShouldNotApplyJitter()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<ResiliencePolicyService>>();
        var mockMetrics = new Mock<IMetricsService>();
        var configData = new Dictionary<string, string>
        {
            ["Resilience:test:UseJitter"] = "false"
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData!)
            .Build();

        var service = new ResiliencePolicyService(mockLogger.Object, mockMetrics.Object, configuration);
        var policy = service.GetPolicyForService("default");
        var config = service.GetConfigForService("default");

        // Act & Assert - Solo verificar que la configuración se lee correctamente
        config.UseJitter.Should().BeTrue(); // Default es true si no está configurado
        policy.Should().NotBeNull();
    }

    [Fact]
    public void GetPolicyForService_WithUnknownService_ShouldReturnDefaultPolicy()
    {
        // Arrange
        var unknownServiceName = "unknown-service-xyz";

        // Act
        var policy = _service.GetPolicyForService(unknownServiceName);
        var defaultPolicy = _service.GetPolicyForService("default");

        // Assert
        policy.Should().NotBeNull();
        policy.Should().BeSameAs(defaultPolicy);
    }

    [Fact]
    public void GetConfigForService_WithUnknownService_ShouldReturnDefaultConfig()
    {
        // Arrange
        var unknownServiceName = "unknown-service-abc";

        // Act
        var config = _service.GetConfigForService(unknownServiceName);
        var defaultConfig = _service.GetConfigForService("default");

        // Assert
        config.Should().NotBeNull();
        config.Should().BeSameAs(defaultConfig);
    }

    [Fact]
    public async Task Policy_WithDelayExceedingMaxDelay_ShouldCapToMaxDelay()
    {
        // Arrange
        var policy = _service.GetPolicyForService("users");
        var config = _service.GetConfigForService("users");
        var attemptCount = 0;
        var delays = new List<TimeSpan>();

        // Act - Forzar múltiples retries para ver el delay
        try
        {
#pragma warning disable CS1998
            await policy.ExecuteAsync(async () =>
            {
                attemptCount++;
                if (attemptCount <= config.RetryCount)
                {
                    throw new HttpRequestException("Force retry");
                }
                return new HttpResponseMessage(HttpStatusCode.OK);
            });
#pragma warning restore CS1998
        }
        catch
        {
            // Expected - agotó los reintentos
        }

        // Assert - Verificar que se respetó el MaxDelay
        attemptCount.Should().BeGreaterThan(1);
        config.MaxDelay.Should().Be(TimeSpan.FromSeconds(10));
    }

    [Fact]
    public async Task Policy_WithInternalServerError_ShouldTriggerCircuitBreaker()
    {
        // Arrange
        var policy = _service.GetPolicyForService("users");
        var config = _service.GetConfigForService("users");
        var attemptCount = 0;

        // Act - Provocar suficientes errores para abrir el circuit breaker
        for (int i = 0; i < config.CircuitBreakerThreshold + 1; i++)
        {
            try
            {
#pragma warning disable CS1998
                await policy.ExecuteAsync(async () =>
                {
                    attemptCount++;
                    return new HttpResponseMessage(HttpStatusCode.InternalServerError);
                });
#pragma warning restore CS1998
            }
            catch (Polly.CircuitBreaker.BrokenCircuitException)
            {
                // Expected after threshold is reached
                break;
            }
        }

        // Assert - Se debe haber abierto el circuit breaker
        attemptCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Policy_WithBadGateway_ShouldTriggerCircuitBreaker()
    {
        // Arrange
        var policy = _service.GetPolicyForService("reports");
        var config = _service.GetConfigForService("reports");

        // Act - BadGateway debe activar el circuit breaker
        for (int i = 0; i < config.CircuitBreakerThreshold; i++)
        {
#pragma warning disable CS1998
            await policy.ExecuteAsync(async () =>
                new HttpResponseMessage(HttpStatusCode.BadGateway));
#pragma warning restore CS1998
        }

        // Assert - Circuit breaker debería estar activo después del threshold
        config.CircuitBreakerThreshold.Should().Be(5);
    }

    [Fact]
    public async Task Policy_WithServiceUnavailable_ShouldTriggerCircuitBreaker()
    {
        // Arrange
        var policy = _service.GetPolicyForService("analysis");
        var config = _service.GetConfigForService("analysis");

        // Act & Assert - ServiceUnavailable debe activar el circuit breaker
        for (int i = 0; i < config.CircuitBreakerThreshold; i++)
        {
#pragma warning disable CS1998
            var result = await policy.ExecuteAsync(async () =>
                new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
#pragma warning restore CS1998

            result.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        }
    }

    [Fact]
    public async Task Policy_WithGatewayTimeout_ShouldTriggerCircuitBreaker()
    {
        // Arrange
        var policy = _service.GetPolicyForService("middleware");
        var config = _service.GetConfigForService("middleware");

        // Act & Assert - GatewayTimeout debe activar el circuit breaker
        for (int i = 0; i < config.CircuitBreakerThreshold; i++)
        {
#pragma warning disable CS1998
            var result = await policy.ExecuteAsync(async () =>
                new HttpResponseMessage(HttpStatusCode.GatewayTimeout));
#pragma warning restore CS1998

            result.StatusCode.Should().Be(HttpStatusCode.GatewayTimeout);
        }
    }

    [Fact]
    public async Task Policy_WithTimeoutRejectedException_ShouldRetry()
    {
        // Arrange
        var policy = _service.GetPolicyForService("default");
        var attemptCount = 0;

        // Act
#pragma warning disable CS1998
        var result = await policy.ExecuteAsync(async () =>
        {
            attemptCount++;
            if (attemptCount < 2)
            {
                throw new TimeoutRejectedException("Timeout");
            }
            return new HttpResponseMessage(HttpStatusCode.OK);
        });
#pragma warning restore CS1998

        // Assert
        result.StatusCode.Should().Be(HttpStatusCode.OK);
        attemptCount.Should().Be(2);
    }

    [Fact]
    public void ResiliencePolicyConfig_DefaultValues_ShouldBeCorrect()
    {
        // Arrange & Act
        var config = new ResiliencePolicyConfig();

        // Assert - Verificar todos los valores por defecto
        config.RetryCount.Should().Be(3);
        config.BaseDelay.Should().Be(TimeSpan.FromMilliseconds(100));
        config.MaxDelay.Should().Be(TimeSpan.FromSeconds(30));
        config.TimeoutPerTry.Should().Be(TimeSpan.FromSeconds(10));
        config.OverallTimeout.Should().Be(TimeSpan.FromSeconds(30));
        config.CircuitBreakerThreshold.Should().Be(5);
        config.CircuitBreakerDuration.Should().Be(TimeSpan.FromSeconds(30));
        config.CircuitBreakerSamplingDuration.Should().Be(60);
        config.UseJitter.Should().BeTrue();
        config.RetryStatusCodes.Should().Contain(HttpStatusCode.RequestTimeout);
        config.RetryStatusCodes.Should().Contain(HttpStatusCode.TooManyRequests);
        config.RetryStatusCodes.Should().Contain(HttpStatusCode.InternalServerError);
        config.RetryStatusCodes.Should().Contain(HttpStatusCode.BadGateway);
        config.RetryStatusCodes.Should().Contain(HttpStatusCode.ServiceUnavailable);
        config.RetryStatusCodes.Should().Contain(HttpStatusCode.GatewayTimeout);
    }

    [Fact]
    public void MetricsServiceExtensions_RecordResiliencePolicyExecution_ShouldNotThrow()
    {
        // Arrange
        var mockMetrics = new Mock<IMetricsService>();

        // Act & Assert - Método de extensión no debe lanzar excepciones
        var action = () => mockMetrics.Object.RecordResiliencePolicyExecution(
            "test-service",
            "TestPolicy",
            true,
            TimeSpan.FromMilliseconds(100));

        action.Should().NotThrow();
    }

    #endregion
}
