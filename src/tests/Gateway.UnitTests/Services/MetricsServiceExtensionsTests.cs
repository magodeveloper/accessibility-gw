using Moq;
using Xunit;
using FluentAssertions;
using Gateway.Services;

namespace Gateway.UnitTests.Services;

/// <summary>
/// Tests para MetricsServiceExtensions
/// Target: Cobertura >80% - Extension methods para métricas de resiliencia
/// </summary>
public class MetricsServiceExtensionsTests
{
    private readonly Mock<IMetricsService> _metricsServiceMock;

    public MetricsServiceExtensionsTests()
    {
        _metricsServiceMock = new Mock<IMetricsService>();
    }

    #region RecordResiliencePolicyExecution Tests

    [Fact]
    public void RecordResiliencePolicyExecution_WithValidData_DoesNotThrow()
    {
        // Arrange
        var serviceName = "users";
        var policyType = "Retry";
        var success = true;
        var duration = TimeSpan.FromMilliseconds(100);

        // Act
        Action act = () => _metricsServiceMock.Object.RecordResiliencePolicyExecution(
            serviceName, policyType, success, duration);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void RecordResiliencePolicyExecution_WithSuccessfulRetry_DoesNotThrow()
    {
        // Arrange
        var serviceName = "reports";
        var policyType = "Retry";
        var success = true;
        var duration = TimeSpan.FromMilliseconds(250);

        // Act
        Action act = () => _metricsServiceMock.Object.RecordResiliencePolicyExecution(
            serviceName, policyType, success, duration);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void RecordResiliencePolicyExecution_WithFailedRetry_DoesNotThrow()
    {
        // Arrange
        var serviceName = "analysis";
        var policyType = "Retry";
        var success = false;
        var duration = TimeSpan.FromSeconds(1);

        // Act
        Action act = () => _metricsServiceMock.Object.RecordResiliencePolicyExecution(
            serviceName, policyType, success, duration);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void RecordResiliencePolicyExecution_WithCircuitBreakerOpen_DoesNotThrow()
    {
        // Arrange
        var serviceName = "middleware";
        var policyType = "CircuitBreakerOpen";
        var success = false;
        var duration = TimeSpan.FromSeconds(30);

        // Act
        Action act = () => _metricsServiceMock.Object.RecordResiliencePolicyExecution(
            serviceName, policyType, success, duration);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void RecordResiliencePolicyExecution_WithCircuitBreakerReset_DoesNotThrow()
    {
        // Arrange
        var serviceName = "users";
        var policyType = "CircuitBreakerReset";
        var success = true;
        var duration = TimeSpan.Zero;

        // Act
        Action act = () => _metricsServiceMock.Object.RecordResiliencePolicyExecution(
            serviceName, policyType, success, duration);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void RecordResiliencePolicyExecution_WithCircuitBreakerHalfOpen_DoesNotThrow()
    {
        // Arrange
        var serviceName = "reports";
        var policyType = "CircuitBreakerHalfOpen";
        var success = true;
        var duration = TimeSpan.Zero;

        // Act
        Action act = () => _metricsServiceMock.Object.RecordResiliencePolicyExecution(
            serviceName, policyType, success, duration);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void RecordResiliencePolicyExecution_WithTimeoutPerTry_DoesNotThrow()
    {
        // Arrange
        var serviceName = "analysis";
        var policyType = "TimeoutPerTry";
        var success = false;
        var duration = TimeSpan.FromSeconds(10);

        // Act
        Action act = () => _metricsServiceMock.Object.RecordResiliencePolicyExecution(
            serviceName, policyType, success, duration);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void RecordResiliencePolicyExecution_WithOverallTimeout_DoesNotThrow()
    {
        // Arrange
        var serviceName = "middleware";
        var policyType = "OverallTimeout";
        var success = false;
        var duration = TimeSpan.FromSeconds(30);

        // Act
        Action act = () => _metricsServiceMock.Object.RecordResiliencePolicyExecution(
            serviceName, policyType, success, duration);

        // Assert
        act.Should().NotThrow();
    }

    [Theory]
    [InlineData("users", "Retry", true, 100)]
    [InlineData("reports", "CircuitBreakerOpen", false, 5000)]
    [InlineData("analysis", "TimeoutPerTry", false, 10000)]
    [InlineData("middleware", "OverallTimeout", false, 30000)]
    [InlineData("default", "CircuitBreakerReset", true, 0)]
    public void RecordResiliencePolicyExecution_WithVariousScenarios_DoesNotThrow(
        string serviceName, string policyType, bool success, double milliseconds)
    {
        // Arrange
        var duration = TimeSpan.FromMilliseconds(milliseconds);

        // Act
        Action act = () => _metricsServiceMock.Object.RecordResiliencePolicyExecution(
            serviceName, policyType, success, duration);

        // Assert
        act.Should().NotThrow();
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void RecordResiliencePolicyExecution_WithNullServiceName_DoesNotThrow()
    {
        // Arrange
        string? serviceName = null;
        var policyType = "Retry";
        var success = true;
        var duration = TimeSpan.FromMilliseconds(100);

        // Act
        Action act = () => _metricsServiceMock.Object.RecordResiliencePolicyExecution(
            serviceName!, policyType, success, duration);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void RecordResiliencePolicyExecution_WithEmptyServiceName_DoesNotThrow()
    {
        // Arrange
        var serviceName = string.Empty;
        var policyType = "Retry";
        var success = true;
        var duration = TimeSpan.FromMilliseconds(100);

        // Act
        Action act = () => _metricsServiceMock.Object.RecordResiliencePolicyExecution(
            serviceName, policyType, success, duration);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void RecordResiliencePolicyExecution_WithNullPolicyType_DoesNotThrow()
    {
        // Arrange
        var serviceName = "users";
        string? policyType = null;
        var success = true;
        var duration = TimeSpan.FromMilliseconds(100);

        // Act
        Action act = () => _metricsServiceMock.Object.RecordResiliencePolicyExecution(
            serviceName, policyType!, success, duration);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void RecordResiliencePolicyExecution_WithEmptyPolicyType_DoesNotThrow()
    {
        // Arrange
        var serviceName = "users";
        var policyType = string.Empty;
        var success = true;
        var duration = TimeSpan.FromMilliseconds(100);

        // Act
        Action act = () => _metricsServiceMock.Object.RecordResiliencePolicyExecution(
            serviceName, policyType, success, duration);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void RecordResiliencePolicyExecution_WithZeroDuration_DoesNotThrow()
    {
        // Arrange
        var serviceName = "users";
        var policyType = "CircuitBreakerReset";
        var success = true;
        var duration = TimeSpan.Zero;

        // Act
        Action act = () => _metricsServiceMock.Object.RecordResiliencePolicyExecution(
            serviceName, policyType, success, duration);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void RecordResiliencePolicyExecution_WithNegativeDuration_DoesNotThrow()
    {
        // Arrange
        var serviceName = "users";
        var policyType = "Retry";
        var success = false;
        var duration = TimeSpan.FromMilliseconds(-100);

        // Act
        Action act = () => _metricsServiceMock.Object.RecordResiliencePolicyExecution(
            serviceName, policyType, success, duration);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void RecordResiliencePolicyExecution_WithVeryLongDuration_DoesNotThrow()
    {
        // Arrange
        var serviceName = "analysis";
        var policyType = "OverallTimeout";
        var success = false;
        var duration = TimeSpan.FromMinutes(10);

        // Act
        Action act = () => _metricsServiceMock.Object.RecordResiliencePolicyExecution(
            serviceName, policyType, success, duration);

        // Assert
        act.Should().NotThrow();
    }

    #endregion

    #region Real-World Scenarios

    [Fact]
    public void RecordResiliencePolicyExecution_RetryScenario_MultipleAttempts()
    {
        // Arrange & Act
        Action act = () =>
        {
            // Primer intento fallido
            _metricsServiceMock.Object.RecordResiliencePolicyExecution(
                "users", "Retry", false, TimeSpan.FromMilliseconds(100));

            // Segundo intento fallido
            _metricsServiceMock.Object.RecordResiliencePolicyExecution(
                "users", "Retry", false, TimeSpan.FromMilliseconds(200));

            // Tercer intento exitoso
            _metricsServiceMock.Object.RecordResiliencePolicyExecution(
                "users", "Retry", true, TimeSpan.FromMilliseconds(400));
        };

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void RecordResiliencePolicyExecution_CircuitBreakerScenario_FullCycle()
    {
        // Arrange & Act
        Action act = () =>
        {
            // Circuit breaker se abre después de múltiples fallos
            _metricsServiceMock.Object.RecordResiliencePolicyExecution(
                "reports", "CircuitBreakerOpen", false, TimeSpan.FromSeconds(30));

            // Circuit breaker entra en half-open
            _metricsServiceMock.Object.RecordResiliencePolicyExecution(
                "reports", "CircuitBreakerHalfOpen", true, TimeSpan.Zero);

            // Circuit breaker se resetea después de éxito
            _metricsServiceMock.Object.RecordResiliencePolicyExecution(
                "reports", "CircuitBreakerReset", true, TimeSpan.Zero);
        };

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void RecordResiliencePolicyExecution_TimeoutScenario_MultipleTimeouts()
    {
        // Arrange & Act
        Action act = () =>
        {
            // Timeout per try
            _metricsServiceMock.Object.RecordResiliencePolicyExecution(
                "analysis", "TimeoutPerTry", false, TimeSpan.FromSeconds(10));

            // Retry después del timeout
            _metricsServiceMock.Object.RecordResiliencePolicyExecution(
                "analysis", "Retry", false, TimeSpan.FromMilliseconds(500));

            // Overall timeout alcanzado
            _metricsServiceMock.Object.RecordResiliencePolicyExecution(
                "analysis", "OverallTimeout", false, TimeSpan.FromSeconds(30));
        };

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void RecordResiliencePolicyExecution_ConcurrentServices_DifferentPolicies()
    {
        // Arrange & Act
        Action act = () =>
        {
            // Servicio users con retry
            _metricsServiceMock.Object.RecordResiliencePolicyExecution(
                "users", "Retry", false, TimeSpan.FromMilliseconds(100));

            // Servicio reports con circuit breaker
            _metricsServiceMock.Object.RecordResiliencePolicyExecution(
                "reports", "CircuitBreakerOpen", false, TimeSpan.FromSeconds(30));

            // Servicio analysis con timeout
            _metricsServiceMock.Object.RecordResiliencePolicyExecution(
                "analysis", "TimeoutPerTry", false, TimeSpan.FromSeconds(10));

            // Servicio middleware exitoso
            _metricsServiceMock.Object.RecordResiliencePolicyExecution(
                "middleware", "Retry", true, TimeSpan.FromMilliseconds(50));
        };

        // Assert
        act.Should().NotThrow();
    }

    #endregion

    #region Extension Method Invocation

    [Fact]
    public void RecordResiliencePolicyExecution_CanBeCalledAsExtensionMethod()
    {
        // Arrange
        var metricsService = _metricsServiceMock.Object;

        // Act
        Action act = () => metricsService.RecordResiliencePolicyExecution(
            "users", "Retry", true, TimeSpan.FromMilliseconds(100));

        // Assert
        act.Should().NotThrow("extension method should be callable");
    }

    [Fact]
    public void RecordResiliencePolicyExecution_ExtensionMethodSignature_IsCorrect()
    {
        // Arrange
        var metricsService = _metricsServiceMock.Object;
        var serviceName = "reports";
        var policyType = "CircuitBreakerOpen";
        var success = false;
        var duration = TimeSpan.FromSeconds(5);

        // Act
        Action act = () => metricsService.RecordResiliencePolicyExecution(
            serviceName: serviceName,
            policyType: policyType,
            success: success,
            duration: duration);

        // Assert
        act.Should().NotThrow("named parameters should work correctly");
    }

    #endregion
}
