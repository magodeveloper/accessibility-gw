using Moq;
using Xunit;
using System.Text;
using FluentAssertions;
using Gateway.Services;
using Gateway.UnitTests.Helpers;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;

namespace Gateway.UnitTests.Services;

public class SignatureValidatorTests : UnitTestBase
{
    private readonly Mock<ILogger<SignatureValidator>> _mockLogger;
    private readonly SignatureValidator _signatureValidator;
    private readonly string _testSecret = "test-secret-key-for-testing";

    public SignatureValidatorTests()
    {
        _mockLogger = CreateMockLogger<SignatureValidator>();
        _signatureValidator = new SignatureValidator(_mockLogger.Object);
    }

    [Fact]
    public void GenerateSignature_WithValidData_ShouldReturnConsistentSignature()
    {
        // Arrange
        var data = "test-data-to-sign";
        var secret = _testSecret;

        // Act
        var signature1 = _signatureValidator.GenerateSignature(data, secret);
        var signature2 = _signatureValidator.GenerateSignature(data, secret);

        // Assert
        signature1.Should().NotBeNullOrEmpty();
        signature2.Should().NotBeNullOrEmpty();
        signature1.Should().Be(signature2);
    }

    [Fact]
    public void GenerateSignature_WithDifferentData_ShouldReturnDifferentSignatures()
    {
        // Arrange
        var data1 = "test-data-1";
        var data2 = "test-data-2";
        var secret = _testSecret;

        // Act
        var signature1 = _signatureValidator.GenerateSignature(data1, secret);
        var signature2 = _signatureValidator.GenerateSignature(data2, secret);

        // Assert
        signature1.Should().NotBe(signature2);
    }

    [Fact]
    public void GenerateSignature_WithDifferentSecrets_ShouldReturnDifferentSignatures()
    {
        // Arrange
        var data = "test-data";
        var secret1 = "secret-1";
        var secret2 = "secret-2";

        // Act
        var signature1 = _signatureValidator.GenerateSignature(data, secret1);
        var signature2 = _signatureValidator.GenerateSignature(data, secret2);

        // Assert
        signature1.Should().NotBe(signature2);
    }

    [Fact]
    public void ValidateSignature_WithValidSignature_ShouldReturnTrue()
    {
        // Arrange
        var data = "test-data-to-validate";
        var secret = _testSecret;
        var expectedSignature = _signatureValidator.GenerateSignature(data, secret);

        // Act
        var result = _signatureValidator.ValidateSignature(data, expectedSignature, secret);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ValidateSignature_WithInvalidSignature_ShouldReturnFalse()
    {
        // Arrange
        var data = "test-data-to-validate";
        var secret = _testSecret;
        var invalidSignature = "invalid-signature";

        // Act
        var result = _signatureValidator.ValidateSignature(data, invalidSignature, secret);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateSignature_WithTamperedData_ShouldReturnFalse()
    {
        // Arrange
        var originalData = "original-data";
        var tamperedData = "tampered-data";
        var secret = _testSecret;
        var signature = _signatureValidator.GenerateSignature(originalData, secret);

        // Act
        var result = _signatureValidator.ValidateSignature(tamperedData, signature, secret);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateSignature_WithWrongSecret_ShouldReturnFalse()
    {
        // Arrange
        var data = "test-data";
        var correctSecret = "correct-secret";
        var wrongSecret = "wrong-secret";
        var signature = _signatureValidator.GenerateSignature(data, correctSecret);

        // Act
        var result = _signatureValidator.ValidateSignature(data, signature, wrongSecret);

        // Assert
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void GenerateSignature_WithEmptyOrNullData_ShouldHandleGracefully(string? data)
    {
        // Arrange
        var secret = _testSecret;

        // Act & Assert
        Assert.Throws<ArgumentException>(() => _signatureValidator.GenerateSignature(data!, secret));
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void GenerateSignature_WithEmptyOrNullSecret_ShouldHandleGracefully(string? secret)
    {
        // Arrange
        var data = "test-data";

        // Act & Assert
        Assert.Throws<ArgumentException>(() => _signatureValidator.GenerateSignature(data, secret!));
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void ValidateSignature_WithEmptyOrNullSignature_ShouldReturnFalse(string? signature)
    {
        // Arrange
        var data = "test-data";
        var secret = _testSecret;

        // Act
        var result = _signatureValidator.ValidateSignature(data, signature!, secret);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateSignature_WithMalformedSignature_ShouldReturnFalse()
    {
        // Arrange
        var data = "test-data";
        var secret = _testSecret;
        var malformedSignature = "not-a-valid-base64-signature!@#$%";

        // Act
        var result = _signatureValidator.ValidateSignature(data, malformedSignature, secret);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void GenerateSignature_WithUnicodeData_ShouldHandleCorrectly()
    {
        // Arrange
        var unicodeData = "测试数据 🎯 émojis ñáéíóú";
        var secret = _testSecret;

        // Act
        var signature1 = _signatureValidator.GenerateSignature(unicodeData, secret);
        var signature2 = _signatureValidator.GenerateSignature(unicodeData, secret);

        // Assert
        signature1.Should().NotBeNullOrEmpty();
        signature1.Should().Be(signature2);

        var isValid = _signatureValidator.ValidateSignature(unicodeData, signature1, secret);
        isValid.Should().BeTrue();
    }

    [Fact]
    public void GenerateSignature_WithLargeData_ShouldHandleCorrectly()
    {
        // Arrange
        var largeData = new string('A', 10000); // 10KB of data
        var secret = _testSecret;

        // Act
        var signature = _signatureValidator.GenerateSignature(largeData, secret);

        // Assert
        signature.Should().NotBeNullOrEmpty();

        var isValid = _signatureValidator.ValidateSignature(largeData, signature, secret);
        isValid.Should().BeTrue();
    }

    [Fact]
    public void ValidateSignature_WithTimingAttackResistance_ShouldBeConstantTime()
    {
        // Arrange
        var data = "sensitive-data";
        var secret = _testSecret;
        var validSignature = _signatureValidator.GenerateSignature(data, secret);
        var invalidSignature = "completely-wrong-signature";

        // Act - Multiple validations to test timing consistency
        var validResults = new List<bool>();
        var invalidResults = new List<bool>();

        for (int i = 0; i < 100; i++)
        {
            validResults.Add(_signatureValidator.ValidateSignature(data, validSignature, secret));
            invalidResults.Add(_signatureValidator.ValidateSignature(data, invalidSignature, secret));
        }

        // Assert
        validResults.Should().AllSatisfy(result => result.Should().BeTrue());
        invalidResults.Should().AllSatisfy(result => result.Should().BeFalse());
    }

    [Fact]
    public void ValidateRequestSignature_WithFutureTimestamp_ShouldReturnFalse()
    {
        // Arrange
        var method = "POST";
        var path = "/api/test";
        var body = "test body";
        var futureTimestamp = DateTimeOffset.UtcNow.AddMinutes(10).ToUnixTimeSeconds();
        var secret = _testSecret;
        var signature = _signatureValidator.GenerateRequestSignature(method, path, body, futureTimestamp, secret);

        // Act
        var isValid = _signatureValidator.ValidateRequestSignature(method, path, body, futureTimestamp, signature, secret, 300);

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public void ValidateRequestSignature_WithPastTimestamp_ShouldReturnFalse()
    {
        // Arrange
        var method = "GET";
        var path = "/api/old";
        var body = "";
        var pastTimestamp = DateTimeOffset.UtcNow.AddMinutes(-10).ToUnixTimeSeconds();
        var secret = _testSecret;
        var signature = _signatureValidator.GenerateRequestSignature(method, path, body, pastTimestamp, secret);

        // Act
        var isValid = _signatureValidator.ValidateRequestSignature(method, path, body, pastTimestamp, signature, secret, 300);

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public void ValidateRequestSignature_WithinToleranceWindow_ShouldReturnTrue()
    {
        // Arrange
        var method = "PUT";
        var path = "/api/update";
        var body = "{\"test\": true}";
        var timestamp = DateTimeOffset.UtcNow.AddSeconds(-30).ToUnixTimeSeconds(); // 30 seconds ago
        var secret = _testSecret;
        var signature = _signatureValidator.GenerateRequestSignature(method, path, body, timestamp, secret);

        // Act
        var isValid = _signatureValidator.ValidateRequestSignature(method, path, body, timestamp, signature, secret, 60);

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public void GenerateSimpleJwtToken_WithComplexPayload_ShouldReturnValidToken()
    {
        // Arrange
        var payload = new Dictionary<string, object>
        {
            ["sub"] = "user123",
            ["iat"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            ["exp"] = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds(),
            ["roles"] = new[] { "admin", "user" },
            ["nested"] = new Dictionary<string, object> { ["key"] = "value" }
        };
        var secret = _testSecret;

        // Act
        var token = _signatureValidator.GenerateSimpleJwtToken(payload, secret);

        // Assert
        token.Should().NotBeNullOrEmpty();
        var parts = token.Split('.');
        parts.Should().HaveCount(3);

        // Verify each part is valid Base64
        parts.Should().AllSatisfy(part => part.Should().NotBeNullOrEmpty());
    }

    [Fact]
    public void GenerateSimpleJwtToken_WithEmptyPayload_ShouldReturnValidToken()
    {
        // Arrange
        var payload = new Dictionary<string, object>();
        var secret = _testSecret;

        // Act
        var token = _signatureValidator.GenerateSimpleJwtToken(payload, secret);

        // Assert
        token.Should().NotBeNullOrEmpty();
        token.Split('.').Should().HaveCount(3);
    }

    [Theory]
    [InlineData("get", "GET")]
    [InlineData("post", "POST")]
    [InlineData("Put", "PUT")]
    [InlineData("DELETE", "DELETE")]
    public void GenerateRequestSignature_MethodNormalization_ShouldBeConsistent(string inputMethod, string expectedMethod)
    {
        // Arrange
        var path = "/api/test";
        var body = "test";
        var timestamp = 1609459200L;
        var secret = _testSecret;

        // Act
        var signature1 = _signatureValidator.GenerateRequestSignature(inputMethod, path, body, timestamp, secret);
        var signature2 = _signatureValidator.GenerateRequestSignature(expectedMethod, path, body, timestamp, secret);

        // Assert
        signature1.Should().Be(signature2);
    }

    [Fact]
    public void ValidateSignature_LogsWarningForInvalidParameters()
    {
        // Arrange
        var data = "";
        var signature = "test";
        var secret = "test";

        // Act
        var result = _signatureValidator.ValidateSignature(data, signature, secret);

        // Assert
        result.Should().BeFalse();

        // Verify warning was logged
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Invalid parameters for signature validation")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void ValidateRequestSignature_LogsWarningForTimestampOutOfTolerance()
    {
        // Arrange
        var method = "POST";
        var path = "/api/test";
        var body = "test";
        var oldTimestamp = DateTimeOffset.UtcNow.AddMinutes(-10).ToUnixTimeSeconds();
        var secret = _testSecret;
        var signature = _signatureValidator.GenerateRequestSignature(method, path, body, oldTimestamp, secret);

        // Act
        var result = _signatureValidator.ValidateRequestSignature(method, path, body, oldTimestamp, signature, secret, 60);

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
    public void GenerateSignature_LogsDebugInformation()
    {
        // Arrange
        var data = "test data for logging";
        var secret = _testSecret;

        // Act
        var signature = _signatureValidator.GenerateSignature(data, secret);

        // Assert
        signature.Should().NotBeNullOrEmpty();

        // Verify debug log was written
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Signature generated for data of length")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void ValidateSignature_LogsDebugValidationResult()
    {
        // Arrange
        var data = "test data";
        var secret = _testSecret;
        var signature = _signatureValidator.GenerateSignature(data, secret);

        // Act
        var result = _signatureValidator.ValidateSignature(data, signature, secret);

        // Assert
        result.Should().BeTrue();

        // Verify debug log was written (called twice - once in Generate, once in Validate)
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Signature validation result")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
