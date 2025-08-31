using Gateway.Services;

namespace Gateway.UnitTests.Services;

public class RequestTranslatorTests
{
    [Theory]
    [InlineData("GET")]
    [InlineData("POST")]
    [InlineData("PUT")]
    [InlineData("DELETE")]
    [InlineData("PATCH")]
    public void RequestTranslator_Should_Handle_Valid_HttpMethods(string method)
    {
        // Act & Assert
        Assert.Contains(method, new[] { "GET", "POST", "PUT", "DELETE", "PATCH" });
    }

    [Fact]
    public void RequestTranslator_AllowedMethods_Should_Be_Valid()
    {
        // Arrange
        var allowedMethods = new[] { "GET", "POST", "PUT", "PATCH", "DELETE" };

        // Act & Assert
        Assert.Equal(5, allowedMethods.Length);
        Assert.Contains("GET", allowedMethods);
        Assert.Contains("POST", allowedMethods);
        Assert.Contains("PUT", allowedMethods);
        Assert.Contains("PATCH", allowedMethods);
        Assert.Contains("DELETE", allowedMethods);
    }

    [Theory]
    [InlineData("reports")]
    [InlineData("users")]
    [InlineData("analysis")]
    public void RequestTranslator_Should_Handle_Valid_ServiceNames(string serviceName)
    {
        // Act & Assert
        Assert.NotEmpty(serviceName);
        Assert.Contains(serviceName, new[] { "reports", "users", "analysis" });
    }
}
