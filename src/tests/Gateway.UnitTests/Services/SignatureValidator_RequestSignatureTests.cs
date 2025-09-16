using Xunit;
using FluentAssertions;
using Gateway.Services;
using Gateway.UnitTests.Helpers;
using Microsoft.Extensions.Logging;
using Moq;

namespace Gateway.UnitTests.Services;

public class SignatureValidator_RequestSignatureTests : UnitTestBase
{
    private readonly Mock<ILogger<SignatureValidator>> _mockLogger;
    private readonly SignatureValidator _signatureValidator;
    private readonly string _testSecret = "test-secret-key-for-request-testing";

    public SignatureValidator_RequestSignatureTests()
    {
        _mockLogger = CreateMockLogger<SignatureValidator>();
        _signatureValidator = new SignatureValidator(_mockLogger.Object);
    }

    [Fact]
    public void GenerateRequestSignature_WithValidParameters_ShouldReturnConsistentSignature()
    {
        // Arrange
        var method = "POST";
        var path = "/api/users";
        var body = "{\"name\":\"test\"}";
        var timestamp = 1609459200L; // Fixed timestamp
        var secret = _testSecret;

        // Act
        var signature1 = _signatureValidator.GenerateRequestSignature(method, path, body, timestamp, secret);
        var signature2 = _signatureValidator.GenerateRequestSignature(method, path, body, timestamp, secret);

        // Assert
        signature1.Should().NotBeNullOrEmpty();
        signature1.Should().Be(signature2);
    }

    [Fact]
    public void GenerateRequestSignature_WithDifferentMethods_ShouldReturnDifferentSignatures()
    {
        // Arrange
        var path = "/api/users";
        var body = "{\"name\":\"test\"}";
        var timestamp = 1609459200L;
        var secret = _testSecret;

        // Act
        var getSignature = _signatureValidator.GenerateRequestSignature("GET", path, body, timestamp, secret);
        var postSignature = _signatureValidator.GenerateRequestSignature("POST", path, body, timestamp, secret);

        // Assert
        getSignature.Should().NotBe(postSignature);
    }

    [Fact]
    public void GenerateRequestSignature_WithDifferentPaths_ShouldReturnDifferentSignatures()
    {
        // Arrange
        var method = "GET";
        var body = "";
        var timestamp = 1609459200L;
        var secret = _testSecret;

        // Act
        var usersSignature = _signatureValidator.GenerateRequestSignature(method, "/api/users", body, timestamp, secret);
        var productsSignature = _signatureValidator.GenerateRequestSignature(method, "/api/products", body, timestamp, secret);

        // Assert
        usersSignature.Should().NotBe(productsSignature);
    }

    [Fact]
    public void GenerateRequestSignature_WithDifferentBodies_ShouldReturnDifferentSignatures()
    {
        // Arrange
        var method = "POST";
        var path = "/api/users";
        var timestamp = 1609459200L;
        var secret = _testSecret;

        // Act
        var body1Signature = _signatureValidator.GenerateRequestSignature(method, path, "{\"name\":\"test1\"}", timestamp, secret);
        var body2Signature = _signatureValidator.GenerateRequestSignature(method, path, "{\"name\":\"test2\"}", timestamp, secret);

        // Assert
        body1Signature.Should().NotBe(body2Signature);
    }

    [Fact]
    public void GenerateRequestSignature_WithDifferentTimestamps_ShouldReturnDifferentSignatures()
    {
        // Arrange
        var method = "POST";
        var path = "/api/users";
        var body = "{\"name\":\"test\"}";
        var secret = _testSecret;

        // Act
        var timestamp1Signature = _signatureValidator.GenerateRequestSignature(method, path, body, 1609459200L, secret);
        var timestamp2Signature = _signatureValidator.GenerateRequestSignature(method, path, body, 1609459260L, secret);

        // Assert
        timestamp1Signature.Should().NotBe(timestamp2Signature);
    }

    [Fact]
    public void GenerateRequestSignature_MethodShouldBeUpperCase_ShouldNormalizeMethodCase()
    {
        // Arrange
        var path = "/api/users";
        var body = "{\"name\":\"test\"}";
        var timestamp = 1609459200L;
        var secret = _testSecret;

        // Act
        var lowerCaseSignature = _signatureValidator.GenerateRequestSignature("post", path, body, timestamp, secret);
        var upperCaseSignature = _signatureValidator.GenerateRequestSignature("POST", path, body, timestamp, secret);
        var mixedCaseSignature = _signatureValidator.GenerateRequestSignature("Post", path, body, timestamp, secret);

        // Assert
        lowerCaseSignature.Should().Be(upperCaseSignature);
        lowerCaseSignature.Should().Be(mixedCaseSignature);
    }

    [Fact]
    public void GenerateRequestSignature_WithEmptyBody_ShouldHandleCorrectly()
    {
        // Arrange
        var method = "GET";
        var path = "/api/users";
        var body = "";
        var timestamp = 1609459200L;
        var secret = _testSecret;

        // Act
        var signature = _signatureValidator.GenerateRequestSignature(method, path, body, timestamp, secret);

        // Assert
        signature.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void GenerateRequestSignature_WithNullBody_ShouldHandleCorrectly()
    {
        // Arrange
        var method = "GET";
        var path = "/api/users";
        string? body = null;
        var timestamp = 1609459200L;
        var secret = _testSecret;

        // Act
        var signature = _signatureValidator.GenerateRequestSignature(method, path, body!, timestamp, secret);

        // Assert
        signature.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void GenerateRequestSignature_WithComplexPath_ShouldHandleCorrectly()
    {
        // Arrange
        var method = "GET";
        var path = "/api/users/123/orders?status=active&limit=10";
        var body = "";
        var timestamp = 1609459200L;
        var secret = _testSecret;

        // Act
        var signature = _signatureValidator.GenerateRequestSignature(method, path, body, timestamp, secret);

        // Assert
        signature.Should().NotBeNullOrEmpty();
        signature.Should().HaveLength(64); // HMAC-SHA256 produces 32 bytes = 64 hex chars
    }

    [Fact]
    public void GenerateRequestSignature_WithSpecialCharacters_ShouldHandleCorrectly()
    {
        // Arrange
        var method = "POST";
        var path = "/api/üsers/测试";
        var body = "{\"naïme\":\"tëst\",\"émoji\":\"🎯\"}";
        var timestamp = 1609459200L;
        var secret = _testSecret;

        // Act
        var signature1 = _signatureValidator.GenerateRequestSignature(method, path, body, timestamp, secret);
        var signature2 = _signatureValidator.GenerateRequestSignature(method, path, body, timestamp, secret);

        // Assert
        signature1.Should().NotBeNullOrEmpty();
        signature1.Should().Be(signature2);
    }
}
