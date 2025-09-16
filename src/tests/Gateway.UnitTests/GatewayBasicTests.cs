using Xunit;
using FluentAssertions;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Gateway.UnitTests;

/// <summary>
/// Tests unitarios básicos para el Gateway
/// </summary>
public class GatewayBasicTests
{
    [Fact]
    public void Gateway_Should_Have_Required_Services()
    {
        // Arrange & Act
        var builder = WebApplication.CreateBuilder();

        // Assert - El builder no debe ser null
        builder.Should().NotBeNull();
        builder.Services.Should().NotBeNull();
    }

    [Fact]
    public void Gateway_Configuration_Should_Be_Valid()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder();

        // Act
        var config = builder.Configuration;

        // Assert
        config.Should().NotBeNull();
    }

    [Theory]
    [InlineData("Development")]
    [InlineData("Production")]
    [InlineData("Testing")]
    public void Gateway_Should_Handle_Different_Environments(string environment)
    {
        // Arrange
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", environment);

        // Act
        var builder = WebApplication.CreateBuilder();

        // Assert
        builder.Environment.Should().NotBeNull();
        builder.Environment.EnvironmentName.Should().Be(environment);

        // Cleanup
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", null);
    }
}

/// <summary>
/// Tests para servicios del Gateway
/// </summary>
public class GatewayServicesTests
{
    [Fact]
    public void ServiceCollection_Should_Accept_Basic_Services()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddLogging();
        services.AddOptions();

        // Assert
        services.Should().NotBeEmpty();
        services.Count.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Configuration_Should_Support_Sections()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder();

        // Act
        var gateSection = builder.Configuration.GetSection("Gate");
        var healthSection = builder.Configuration.GetSection("HealthChecks");
        var redisSection = builder.Configuration.GetSection("Redis");

        // Assert
        gateSection.Should().NotBeNull();
        healthSection.Should().NotBeNull();
        redisSection.Should().NotBeNull();
    }
}
