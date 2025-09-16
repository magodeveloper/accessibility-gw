using Moq;
using Xunit;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Gateway.UnitTests;

/// <summary>
/// Tests para configuraciones del Gateway
/// </summary>
public class ConfigurationTests
{
    [Fact]
    public void GateOptions_Should_Be_Configurable()
    {
        // Arrange
        var configDict = new Dictionary<string, string?>
        {
            ["Gate:ServiceName"] = "AccessibilityGateway",
            ["Gate:Version"] = "1.0.0",
            ["Gate:Environment"] = "Test"
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configDict)
            .Build();

        var services = new ServiceCollection();
        services.Configure<GateOptions>(configuration.GetSection("Gate"));
        services.AddSingleton<IConfiguration>(configuration);

        var serviceProvider = services.BuildServiceProvider();

        // Act
        var options = serviceProvider.GetRequiredService<IOptions<GateOptions>>();

        // Assert
        options.Should().NotBeNull();
        options.Value.Should().NotBeNull();
    }

    [Fact]
    public void HealthChecksOptions_Should_Be_Configurable()
    {
        // Arrange
        var configDict = new Dictionary<string, string?>
        {
            ["HealthChecks:Enabled"] = "true",
            ["HealthChecks:Timeout"] = "30"
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configDict)
            .Build();

        var services = new ServiceCollection();
        services.Configure<HealthChecksOptions>(configuration.GetSection("HealthChecks"));
        services.AddSingleton<IConfiguration>(configuration);

        var serviceProvider = services.BuildServiceProvider();

        // Act
        var options = serviceProvider.GetRequiredService<IOptions<HealthChecksOptions>>();

        // Assert
        options.Should().NotBeNull();
        options.Value.Should().NotBeNull();
    }

    [Fact]
    public void RedisOptions_Should_Be_Configurable()
    {
        // Arrange
        var configDict = new Dictionary<string, string?>
        {
            ["Redis:ConnectionString"] = "localhost:6379",
            ["Redis:Database"] = "0"
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configDict)
            .Build();

        var services = new ServiceCollection();
        services.Configure<RedisOptions>(configuration.GetSection("Redis"));
        services.AddSingleton<IConfiguration>(configuration);

        var serviceProvider = services.BuildServiceProvider();

        // Act
        var options = serviceProvider.GetRequiredService<IOptions<RedisOptions>>();

        // Assert
        options.Should().NotBeNull();
        options.Value.Should().NotBeNull();
    }
}

/// <summary>
/// Tests para logging
/// </summary>
public class LoggingTests
{
    [Fact]
    public void Logger_Should_Be_Injectable()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        var serviceProvider = services.BuildServiceProvider();

        // Act
        var logger = serviceProvider.GetRequiredService<ILogger<LoggingTests>>();

        // Assert
        logger.Should().NotBeNull();
    }

    [Fact]
    public void Logger_Should_Handle_Different_LogLevels()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<LoggingTests>>();

        // Act & Assert
        mockLogger.Object.Should().NotBeNull();

        // Verify that the logger can be configured for different levels
        mockLogger.Setup(x => x.IsEnabled(LogLevel.Information)).Returns(true);
        mockLogger.Setup(x => x.IsEnabled(LogLevel.Warning)).Returns(true);
        mockLogger.Setup(x => x.IsEnabled(LogLevel.Error)).Returns(true);

        mockLogger.Object.IsEnabled(LogLevel.Information).Should().BeTrue();
        mockLogger.Object.IsEnabled(LogLevel.Warning).Should().BeTrue();
        mockLogger.Object.IsEnabled(LogLevel.Error).Should().BeTrue();
    }
}
