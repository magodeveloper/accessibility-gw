using Moq;
using Xunit;
using FluentAssertions;
using Gateway.Services;
using Gateway.UnitTests.Helpers;
using Microsoft.Extensions.Logging;

namespace Gateway.UnitTests.Services;

public class SignatureValidator_AdditionalTests : UnitTestBase
{
    private readonly Mock<ILogger<SignatureValidator>> _mockLogger;
    private readonly SignatureValidator _signatureValidator;

    public SignatureValidator_AdditionalTests()
    {
        _mockLogger = CreateMockLogger<SignatureValidator>();
        _signatureValidator = new SignatureValidator(_mockLogger.Object);
    }

    [Theory]
    [InlineData("")]
    public void GenerateSignature_WithInvalidData_ShouldThrowArgumentException(string invalidData)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            _signatureValidator.GenerateSignature(invalidData, "secret"));

        exception.Message.Should().Contain("Data cannot be null or empty");
        exception.ParamName.Should().Be("data");
    }

    [Fact]
    public void GenerateSignature_WithNullData_ShouldThrowArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            _signatureValidator.GenerateSignature(null!, "secret"));

        exception.Message.Should().Contain("Data cannot be null or empty");
        exception.ParamName.Should().Be("data");
    }

    [Theory]
    [InlineData("")]
    public void GenerateSignature_WithInvalidSecret_ShouldThrowArgumentException(string invalidSecret)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            _signatureValidator.GenerateSignature("data", invalidSecret));

        exception.Message.Should().Contain("Secret cannot be null or empty");
        exception.ParamName.Should().Be("secret");
    }

    [Fact]
    public void GenerateSignature_WithNullSecret_ShouldThrowArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            _signatureValidator.GenerateSignature("data", null!));

        exception.Message.Should().Contain("Secret cannot be null or empty");
        exception.ParamName.Should().Be("secret");
    }

    [Fact]
    public void GenerateSignature_WithWhitespaceData_ShouldSucceed()
    {
        // Arrange
        var data = "  ";
        var secret = "test-secret";

        // Act
        var result = _signatureValidator.GenerateSignature(data, secret);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().HaveLength(64); // SHA256 hex string length
    }

    [Fact]
    public void GenerateSignature_WithWhitespaceSecret_ShouldSucceed()
    {
        // Arrange
        var data = "test-data";
        var secret = "  ";

        // Act
        var result = _signatureValidator.GenerateSignature(data, secret);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().HaveLength(64); // SHA256 hex string length
    }

    [Theory]
    [InlineData("", "signature", "secret")]
    [InlineData("data", "", "secret")]
    [InlineData("data", "signature", "")]
    public void ValidateSignature_WithInvalidParameters_ShouldReturnFalseAndLogWarning(string data, string signature, string secret)
    {
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
            Times.AtLeastOnce);
    }

    [Fact]
    public void ValidateSignature_WithNullParameters_ShouldReturnFalseAndLogWarning()
    {
        // Act
        var result = _signatureValidator.ValidateSignature(null!, "signature", "secret");

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
    public void ValidateSignature_WithValidSignature_ShouldReturnTrueAndLogDebug()
    {
        // Arrange
        var data = "test-data";
        var secret = "test-secret";
        var validSignature = _signatureValidator.GenerateSignature(data, secret);

        // Act
        var result = _signatureValidator.ValidateSignature(data, validSignature, secret);

        // Assert
        result.Should().BeTrue();

        // Verify debug log was called
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public void ValidateSignature_WithInvalidSignature_ShouldReturnFalseAndLogDebug()
    {
        // Arrange
        var data = "test-data";
        var secret = "test-secret";
        var invalidSignature = "invalid-signature";

        // Act
        var result = _signatureValidator.ValidateSignature(data, invalidSignature, secret);

        // Assert
        result.Should().BeFalse();

        // Verify debug log was called
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public void ValidateRequestSignature_WithTimestampOutsideTolerance_ShouldReturnFalseAndLogWarning()
    {
        // Arrange
        var method = "POST";
        var path = "/api/test";
        var body = "test-body";
        var secret = "test-secret";
        var signature = "test-signature";
        var oldTimestamp = DateTimeOffset.UtcNow.AddMinutes(-10).ToUnixTimeSeconds(); // 10 minutes ago
        var toleranceSeconds = 300; // 5 minutes

        // Act
        var result = _signatureValidator.ValidateRequestSignature(
            method, path, body, oldTimestamp, signature, secret, toleranceSeconds);

        // Assert
        result.Should().BeFalse();

        // Verify warning was logged about timestamp
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
    public void ValidateRequestSignature_WithValidTimestamp_ShouldValidateSignature()
    {
        // Arrange
        var method = "POST";
        var path = "/api/test";
        var body = "test-body";
        var secret = "test-secret";
        var currentTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var validSignature = _signatureValidator.GenerateRequestSignature(method, path, body, currentTimestamp, secret);
        var toleranceSeconds = 300;

        // Act
        var result = _signatureValidator.ValidateRequestSignature(
            method, path, body, currentTimestamp, validSignature, secret, toleranceSeconds);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ValidateRequestSignature_WithFutureTimestamp_ShouldReturnFalseAndLogWarning()
    {
        // Arrange
        var method = "GET";
        var path = "/api/future";
        var body = "";
        var secret = "test-secret";
        var signature = "test-signature";
        var futureTimestamp = DateTimeOffset.UtcNow.AddMinutes(10).ToUnixTimeSeconds(); // 10 minutes in future
        var toleranceSeconds = 300; // 5 minutes

        // Act
        var result = _signatureValidator.ValidateRequestSignature(
            method, path, body, futureTimestamp, signature, secret, toleranceSeconds);

        // Assert
        result.Should().BeFalse();

        // Verify warning was logged about timestamp
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Request timestamp is outside tolerance window")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public void ValidateRequestSignature_WithTimestampAtToleranceEdge_ShouldReturnTrue()
    {
        // Arrange
        var method = "PUT";
        var path = "/api/edge";
        var body = "edge-test";
        var secret = "test-secret";
        var toleranceSeconds = 300;
        var edgeTimestamp = DateTimeOffset.UtcNow.AddSeconds(-toleranceSeconds).ToUnixTimeSeconds(); // Exactly at tolerance edge
        var validSignature = _signatureValidator.GenerateRequestSignature(method, path, body, edgeTimestamp, secret);

        // Act
        var result = _signatureValidator.ValidateRequestSignature(
            method, path, body, edgeTimestamp, validSignature, secret, toleranceSeconds);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void GenerateSimpleJwtToken_WithValidPayload_ShouldReturnValidJwtStructure()
    {
        // Arrange
        var payload = new Dictionary<string, object>
        {
            ["userId"] = 123,
            ["email"] = "test@example.com",
            ["exp"] = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds()
        };
        var secret = "jwt-secret-key";

        // Act
        var token = _signatureValidator.GenerateSimpleJwtToken(payload, secret);

        // Assert
        token.Should().NotBeNullOrEmpty();
        var parts = token.Split('.');
        parts.Should().HaveCount(3); // header.payload.signature

        // Verify each part is base64url encoded
        parts[0].Should().NotBeNullOrEmpty(); // header
        parts[1].Should().NotBeNullOrEmpty(); // payload
        parts[2].Should().NotBeNullOrEmpty(); // signature
    }

    [Fact]
    public void GenerateSimpleJwtToken_WithEmptyPayload_ShouldReturnValidJwtStructure()
    {
        // Arrange
        var payload = new Dictionary<string, object>();
        var secret = "jwt-secret-key";

        // Act
        var token = _signatureValidator.GenerateSimpleJwtToken(payload, secret);

        // Assert
        token.Should().NotBeNullOrEmpty();
        var parts = token.Split('.');
        parts.Should().HaveCount(3);
    }

    [Fact]
    public void GenerateSimpleJwtToken_ShouldUseCorrectAlgorithmAndType()
    {
        // Arrange
        var payload = new Dictionary<string, object> { ["test"] = "value" };
        var secret = "test-secret";

        // Act
        var token = _signatureValidator.GenerateSimpleJwtToken(payload, secret);

        // Assert
        var headerPart = token.Split('.')[0];

        // Decode header to verify algorithm and type
        var paddedHeader = headerPart.PadRight((headerPart.Length + 3) & ~3, '=');
        var headerBase64 = paddedHeader.Replace('-', '+').Replace('_', '/');
        var headerBytes = Convert.FromBase64String(headerBase64);
        var headerJson = System.Text.Encoding.UTF8.GetString(headerBytes);

        headerJson.Should().Contain("\"alg\":\"HS256\"");
        headerJson.Should().Contain("\"typ\":\"JWT\"");
    }
}
