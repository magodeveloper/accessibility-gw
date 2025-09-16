using Xunit;
using FluentAssertions;
using Gateway.Services;
using Gateway.UnitTests.Helpers;
using Microsoft.Extensions.Logging;
using Moq;

namespace Gateway.UnitTests.Services;

public class SignatureValidator_ValidateRequestSignatureTests : UnitTestBase
{
    private readonly Mock<ILogger<SignatureValidator>> _mockLogger;
    private readonly SignatureValidator _signatureValidator;
    private readonly string _testSecret = "test-secret-key-for-validation-testing";

    public SignatureValidator_ValidateRequestSignatureTests()
    {
        _mockLogger = CreateMockLogger<SignatureValidator>();
        _signatureValidator = new SignatureValidator(_mockLogger.Object);
    }

    [Fact]
    public void ValidateRequestSignature_WithValidSignature_ShouldReturnTrue()
    {
        // Arrange
        var method = "POST";
        var path = "/api/users";
        var body = "{\"name\":\"test\"}";
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var signature = _signatureValidator.GenerateRequestSignature(method, path, body, timestamp, _testSecret);

        // Act
        var result = _signatureValidator.ValidateRequestSignature(method, path, body, timestamp, signature, _testSecret);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ValidateRequestSignature_WithInvalidSignature_ShouldReturnFalse()
    {
        // Arrange
        var method = "POST";
        var path = "/api/users";
        var body = "{\"name\":\"test\"}";
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var invalidSignature = "invalid-signature-that-wont-match";

        // Act
        var result = _signatureValidator.ValidateRequestSignature(method, path, body, timestamp, invalidSignature, _testSecret);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateRequestSignature_WithTamperedMethod_ShouldReturnFalse()
    {
        // Arrange
        var originalMethod = "POST";
        var tamperedMethod = "PUT";
        var path = "/api/users";
        var body = "{\"name\":\"test\"}";
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var signature = _signatureValidator.GenerateRequestSignature(originalMethod, path, body, timestamp, _testSecret);

        // Act
        var result = _signatureValidator.ValidateRequestSignature(tamperedMethod, path, body, timestamp, signature, _testSecret);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateRequestSignature_WithTamperedPath_ShouldReturnFalse()
    {
        // Arrange
        var method = "POST";
        var originalPath = "/api/users";
        var tamperedPath = "/api/users/123";
        var body = "{\"name\":\"test\"}";
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var signature = _signatureValidator.GenerateRequestSignature(method, originalPath, body, timestamp, _testSecret);

        // Act
        var result = _signatureValidator.ValidateRequestSignature(method, tamperedPath, body, timestamp, signature, _testSecret);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateRequestSignature_WithTamperedBody_ShouldReturnFalse()
    {
        // Arrange
        var method = "POST";
        var path = "/api/users";
        var originalBody = "{\"name\":\"test\"}";
        var tamperedBody = "{\"name\":\"hacker\"}";
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var signature = _signatureValidator.GenerateRequestSignature(method, path, originalBody, timestamp, _testSecret);

        // Act
        var result = _signatureValidator.ValidateRequestSignature(method, path, tamperedBody, timestamp, signature, _testSecret);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateRequestSignature_WithTamperedTimestamp_ShouldReturnFalse()
    {
        // Arrange
        var method = "POST";
        var path = "/api/users";
        var body = "{\"name\":\"test\"}";
        var originalTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var tamperedTimestamp = originalTimestamp + 3600; // 1 hour later
        var signature = _signatureValidator.GenerateRequestSignature(method, path, body, originalTimestamp, _testSecret);

        // Act
        var result = _signatureValidator.ValidateRequestSignature(method, path, body, tamperedTimestamp, signature, _testSecret);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateRequestSignature_WithTimeTolerance_ValidTimestamp_ShouldReturnTrue()
    {
        // Arrange
        var method = "GET";
        var path = "/api/health";
        var body = "";
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - 100; // 100 seconds ago
        var signature = _signatureValidator.GenerateRequestSignature(method, path, body, timestamp, _testSecret);
        var toleranceSeconds = 300; // 5 minutes tolerance

        // Act
        var result = _signatureValidator.ValidateRequestSignature(method, path, body, timestamp, signature, _testSecret, toleranceSeconds);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ValidateRequestSignature_WithTimeTolerance_ExpiredTimestamp_ShouldReturnFalse()
    {
        // Arrange
        var method = "GET";
        var path = "/api/health";
        var body = "";
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - 400; // 400 seconds ago
        var signature = _signatureValidator.GenerateRequestSignature(method, path, body, timestamp, _testSecret);
        var toleranceSeconds = 300; // 5 minutes tolerance

        // Act
        var result = _signatureValidator.ValidateRequestSignature(method, path, body, timestamp, signature, _testSecret, toleranceSeconds);

        // Assert
        result.Should().BeFalse();

        // Verify warning was logged
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Request timestamp is outside tolerance window")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void ValidateRequestSignature_WithTimeTolerance_FutureTimestamp_ShouldReturnFalse()
    {
        // Arrange
        var method = "GET";
        var path = "/api/health";
        var body = "";
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + 400; // 400 seconds in the future
        var signature = _signatureValidator.GenerateRequestSignature(method, path, body, timestamp, _testSecret);
        var toleranceSeconds = 300; // 5 minutes tolerance

        // Act
        var result = _signatureValidator.ValidateRequestSignature(method, path, body, timestamp, signature, _testSecret, toleranceSeconds);

        // Assert
        result.Should().BeFalse();

        // Verify warning was logged
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Request timestamp is outside tolerance window")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void ValidateRequestSignature_WithCustomTolerance_ShouldRespectCustomValue()
    {
        // Arrange
        var method = "GET";
        var path = "/api/health";
        var body = "";
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - 50; // 50 seconds ago
        var signature = _signatureValidator.GenerateRequestSignature(method, path, body, timestamp, _testSecret);
        var customTolerance = 60; // 1 minute tolerance

        // Act
        var result = _signatureValidator.ValidateRequestSignature(method, path, body, timestamp, signature, _testSecret, customTolerance);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ValidateRequestSignature_MethodCaseShouldNotMatter_ShouldReturnTrue()
    {
        // Arrange
        var originalMethod = "POST";
        var differentCaseMethod = "post";
        var path = "/api/users";
        var body = "{\"name\":\"test\"}";
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var signature = _signatureValidator.GenerateRequestSignature(originalMethod, path, body, timestamp, _testSecret);

        // Act
        var result = _signatureValidator.ValidateRequestSignature(differentCaseMethod, path, body, timestamp, signature, _testSecret);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ValidateRequestSignature_WithZeroTolerance_ShouldBeVeryStrict()
    {
        // Arrange
        var method = "GET";
        var path = "/api/health";
        var body = "";
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - 1; // 1 second ago
        var signature = _signatureValidator.GenerateRequestSignature(method, path, body, timestamp, _testSecret);
        var zeroTolerance = 0; // No tolerance

        // Act
        var result = _signatureValidator.ValidateRequestSignature(method, path, body, timestamp, signature, _testSecret, zeroTolerance);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateRequestSignature_WithLargeTolerance_ShouldBePermissive()
    {
        // Arrange
        var method = "GET";
        var path = "/api/health";
        var body = "";
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - 3000; // 50 minutes ago
        var signature = _signatureValidator.GenerateRequestSignature(method, path, body, timestamp, _testSecret);
        var largeTolerance = 3600; // 1 hour tolerance

        // Act
        var result = _signatureValidator.ValidateRequestSignature(method, path, body, timestamp, signature, _testSecret, largeTolerance);

        // Assert
        result.Should().BeTrue();
    }
}
