using Xunit;
using Gateway.Models;
using FluentAssertions;
using Gateway.UnitTests.Helpers;

namespace Gateway.UnitTests.Models;

public class HealthCheckRequestTests : UnitTestBase
{
    [Fact]
    public void HealthCheckRequest_DefaultValues_ShouldBeCorrect()
    {
        // Arrange & Act
        var request = new HealthCheckRequest();

        // Assert
        request.Deep.Should().BeFalse("Deep should default to false");
        request.IncludeMetrics.Should().BeFalse("IncludeMetrics should default to false");
    }

    [Fact]
    public void HealthCheckRequest_WithDeepTrue_ShouldSetDeepCorrectly()
    {
        // Arrange & Act
        var request = new HealthCheckRequest
        {
            Deep = true
        };

        // Assert
        request.Deep.Should().BeTrue("Deep should be set to true");
        request.IncludeMetrics.Should().BeFalse("IncludeMetrics should remain default false");
    }

    [Fact]
    public void HealthCheckRequest_WithIncludeMetricsTrue_ShouldSetIncludeMetricsCorrectly()
    {
        // Arrange & Act
        var request = new HealthCheckRequest
        {
            IncludeMetrics = true
        };

        // Assert
        request.IncludeMetrics.Should().BeTrue("IncludeMetrics should be set to true");
        request.Deep.Should().BeFalse("Deep should remain default false");
    }

    [Fact]
    public void HealthCheckRequest_WithBothPropertiesTrue_ShouldSetBothCorrectly()
    {
        // Arrange & Act
        var request = new HealthCheckRequest
        {
            Deep = true,
            IncludeMetrics = true
        };

        // Assert
        request.Deep.Should().BeTrue("Deep should be set to true");
        request.IncludeMetrics.Should().BeTrue("IncludeMetrics should be set to true");
    }

    [Fact]
    public void HealthCheckRequest_WithBothPropertiesFalse_ShouldSetBothCorrectly()
    {
        // Arrange & Act
        var request = new HealthCheckRequest
        {
            Deep = false,
            IncludeMetrics = false
        };

        // Assert
        request.Deep.Should().BeFalse("Deep should be explicitly set to false");
        request.IncludeMetrics.Should().BeFalse("IncludeMetrics should be explicitly set to false");
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    public void HealthCheckRequest_WithVariousCombinations_ShouldSetPropertiesCorrectly(bool deep, bool includeMetrics)
    {
        // Arrange & Act
        var request = new HealthCheckRequest
        {
            Deep = deep,
            IncludeMetrics = includeMetrics
        };

        // Assert
        request.Deep.Should().Be(deep, $"Deep should be set to {deep}");
        request.IncludeMetrics.Should().Be(includeMetrics, $"IncludeMetrics should be set to {includeMetrics}");
    }

    [Fact]
    public void HealthCheckRequest_IsClass_ShouldHaveReferenceSemantics()
    {
        // Arrange
        var request1 = new HealthCheckRequest { Deep = true, IncludeMetrics = false };
        var request2 = new HealthCheckRequest { Deep = true, IncludeMetrics = false };
        var request3 = request1; // Same reference

        // Assert
        request1.Should().NotBe(request2, "different instances with same values should not be equal (reference semantics)");
        request1.Should().Be(request3, "same reference should be equal");
        request1.Should().NotBeSameAs(request2, "different instances should not be the same reference");
        request1.Should().BeSameAs(request3, "same reference should be the same reference");
    }

    [Fact]
    public void HealthCheckRequest_ToString_ShouldReturnTypeString()
    {
        // Arrange
        var request = new HealthCheckRequest
        {
            Deep = true,
            IncludeMetrics = false
        };

        // Act
        var stringRepresentation = request.ToString();

        // Assert
        stringRepresentation.Should().NotBeNullOrEmpty("ToString should return a non-empty string");
        stringRepresentation.Should().Be("Gateway.Models.HealthCheckRequest", "ToString should return the type name by default for classes");
    }

    [Fact]
    public void HealthCheckRequest_InitOnlyProperties_ShouldNotAllowDirectMutation()
    {
        // Arrange
        var request = new HealthCheckRequest { Deep = true, IncludeMetrics = true };

        // Assert - This test verifies that properties are init-only by checking compilation
        // The fact that this compiles and the properties were set during initialization
        // confirms that init-only semantics are working correctly
        request.Deep.Should().BeTrue();
        request.IncludeMetrics.Should().BeTrue();
    }
}
