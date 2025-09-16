using Xunit;
using FluentAssertions;
using Gateway.Services;
using Gateway.UnitTests.Helpers;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text;
using System.Text.Json;

namespace Gateway.UnitTests.Services;

public class SignatureValidator_JwtTokenTests : UnitTestBase
{
    private readonly Mock<ILogger<SignatureValidator>> _mockLogger;
    private readonly SignatureValidator _signatureValidator;
    private readonly string _testSecret = "test-secret-key-for-jwt-testing";

    public SignatureValidator_JwtTokenTests()
    {
        _mockLogger = CreateMockLogger<SignatureValidator>();
        _signatureValidator = new SignatureValidator(_mockLogger.Object);
    }

    [Fact]
    public void GenerateSimpleJwtToken_WithValidPayload_ShouldReturnWellFormedToken()
    {
        // Arrange
        var payload = new Dictionary<string, object>
        {
            ["sub"] = "1234567890",
            ["name"] = "John Doe",
            ["iat"] = 1516239022
        };

        // Act
        var token = _signatureValidator.GenerateSimpleJwtToken(payload, _testSecret);

        // Assert
        token.Should().NotBeNullOrEmpty();
        var parts = token.Split('.');
        parts.Should().HaveCount(3, "JWT should have header.payload.signature format");
    }

    [Fact]
    public void GenerateSimpleJwtToken_ShouldHaveValidHeader()
    {
        // Arrange
        var payload = new Dictionary<string, object>
        {
            ["test"] = "value"
        };

        // Act
        var token = _signatureValidator.GenerateSimpleJwtToken(payload, _testSecret);
        var parts = token.Split('.');
        var headerBase64 = parts[0];

        // Decode header
        var headerPadded = headerBase64.Replace('-', '+').Replace('_', '/');
        while (headerPadded.Length % 4 != 0)
            headerPadded += "=";

        var headerBytes = Convert.FromBase64String(headerPadded);
        var headerJson = Encoding.UTF8.GetString(headerBytes);
        var header = JsonSerializer.Deserialize<Dictionary<string, object>>(headerJson);

        // Assert
        header.Should().ContainKey("alg");
        header.Should().ContainKey("typ");
        header!["alg"].ToString().Should().Be("HS256");
        header["typ"].ToString().Should().Be("JWT");
    }

    [Fact]
    public void GenerateSimpleJwtToken_ShouldHaveValidPayload()
    {
        // Arrange
        var originalPayload = new Dictionary<string, object>
        {
            ["sub"] = "1234567890",
            ["name"] = "John Doe",
            ["admin"] = true,
            ["exp"] = 1716239022
        };

        // Act
        var token = _signatureValidator.GenerateSimpleJwtToken(originalPayload, _testSecret);
        var parts = token.Split('.');
        var payloadBase64 = parts[1];

        // Decode payload
        var payloadPadded = payloadBase64.Replace('-', '+').Replace('_', '/');
        while (payloadPadded.Length % 4 != 0)
            payloadPadded += "=";

        var payloadBytes = Convert.FromBase64String(payloadPadded);
        var payloadJson = Encoding.UTF8.GetString(payloadBytes);
        var decodedPayload = JsonSerializer.Deserialize<Dictionary<string, object>>(payloadJson);

        // Assert
        decodedPayload.Should().ContainKey("sub");
        decodedPayload.Should().ContainKey("name");
        decodedPayload.Should().ContainKey("admin");
        decodedPayload.Should().ContainKey("exp");

        decodedPayload!["sub"].ToString().Should().Be("1234567890");
        decodedPayload["name"].ToString().Should().Be("John Doe");
    }

    [Fact]
    public void GenerateSimpleJwtToken_WithEmptyPayload_ShouldReturnValidToken()
    {
        // Arrange
        var payload = new Dictionary<string, object>();

        // Act
        var token = _signatureValidator.GenerateSimpleJwtToken(payload, _testSecret);

        // Assert
        token.Should().NotBeNullOrEmpty();
        var parts = token.Split('.');
        parts.Should().HaveCount(3);
    }

    [Fact]
    public void GenerateSimpleJwtToken_WithDifferentPayloads_ShouldReturnDifferentTokens()
    {
        // Arrange
        var payload1 = new Dictionary<string, object>
        {
            ["user"] = "alice"
        };
        var payload2 = new Dictionary<string, object>
        {
            ["user"] = "bob"
        };

        // Act
        var token1 = _signatureValidator.GenerateSimpleJwtToken(payload1, _testSecret);
        var token2 = _signatureValidator.GenerateSimpleJwtToken(payload2, _testSecret);

        // Assert
        token1.Should().NotBe(token2);
    }

    [Fact]
    public void GenerateSimpleJwtToken_WithDifferentSecrets_ShouldReturnDifferentTokens()
    {
        // Arrange
        var payload = new Dictionary<string, object>
        {
            ["user"] = "alice"
        };
        var secret1 = "secret1";
        var secret2 = "secret2";

        // Act
        var token1 = _signatureValidator.GenerateSimpleJwtToken(payload, secret1);
        var token2 = _signatureValidator.GenerateSimpleJwtToken(payload, secret2);

        // Assert
        token1.Should().NotBe(token2);

        // Headers and payloads should be the same, only signatures differ
        var parts1 = token1.Split('.');
        var parts2 = token2.Split('.');

        parts1[0].Should().Be(parts2[0]); // Same header
        parts1[1].Should().Be(parts2[1]); // Same payload
        parts1[2].Should().NotBe(parts2[2]); // Different signature
    }

    [Fact]
    public void GenerateSimpleJwtToken_WithSameInputs_ShouldReturnSameToken()
    {
        // Arrange
        var payload = new Dictionary<string, object>
        {
            ["user"] = "alice",
            ["role"] = "admin"
        };

        // Act
        var token1 = _signatureValidator.GenerateSimpleJwtToken(payload, _testSecret);
        var token2 = _signatureValidator.GenerateSimpleJwtToken(payload, _testSecret);

        // Assert
        token1.Should().Be(token2);
    }

    [Fact]
    public void GenerateSimpleJwtToken_WithComplexPayload_ShouldHandleCorrectly()
    {
        // Arrange
        var payload = new Dictionary<string, object>
        {
            ["sub"] = "user-123",
            ["name"] = "John Doe",
            ["email"] = "john.doe@example.com",
            ["admin"] = true,
            ["roles"] = new[] { "user", "admin", "moderator" },
            ["permissions"] = new Dictionary<string, object>
            {
                ["read"] = true,
                ["write"] = true,
                ["delete"] = false
            },
            ["metadata"] = new Dictionary<string, object>
            {
                ["created"] = "2023-01-01",
                ["lastLogin"] = "2023-12-01"
            }
        };

        // Act
        var token = _signatureValidator.GenerateSimpleJwtToken(payload, _testSecret);

        // Assert
        token.Should().NotBeNullOrEmpty();
        var parts = token.Split('.');
        parts.Should().HaveCount(3);

        // Verify payload can be decoded
        var payloadBase64 = parts[1];
        var payloadPadded = payloadBase64.Replace('-', '+').Replace('_', '/');
        while (payloadPadded.Length % 4 != 0)
            payloadPadded += "=";

        var payloadBytes = Convert.FromBase64String(payloadPadded);
        var payloadJson = Encoding.UTF8.GetString(payloadBytes);

        // Should not throw exception
        var decodedPayload = JsonSerializer.Deserialize<Dictionary<string, object>>(payloadJson);
        decodedPayload.Should().NotBeNull();
    }

    [Fact]
    public void GenerateSimpleJwtToken_WithSpecialCharacters_ShouldHandleCorrectly()
    {
        // Arrange
        var payload = new Dictionary<string, object>
        {
            ["name"] = "José María",
            ["city"] = "São Paulo",
            ["description"] = "User with émojis 🎯 and ñáéíóú characters",
            ["data"] = "Special chars: @#$%^&*()+={}[]|\\:;\"'<>,.?/"
        };

        // Act
        var token = _signatureValidator.GenerateSimpleJwtToken(payload, _testSecret);

        // Assert
        token.Should().NotBeNullOrEmpty();
        var parts = token.Split('.');
        parts.Should().HaveCount(3);

        // Verify we can decode the payload
        var payloadBase64 = parts[1];
        var payloadPadded = payloadBase64.Replace('-', '+').Replace('_', '/');
        while (payloadPadded.Length % 4 != 0)
            payloadPadded += "=";

        var payloadBytes = Convert.FromBase64String(payloadPadded);
        var payloadJson = Encoding.UTF8.GetString(payloadBytes);
        var decodedPayload = JsonSerializer.Deserialize<Dictionary<string, object>>(payloadJson);

        decodedPayload.Should().ContainKey("name");
        decodedPayload!["name"].ToString().Should().Be("José María");
    }

    [Fact]
    public void GenerateSimpleJwtToken_WithNumericPayload_ShouldHandleCorrectly()
    {
        // Arrange
        var payload = new Dictionary<string, object>
        {
            ["userId"] = 12345,
            ["balance"] = 999.99,
            ["isActive"] = true,
            ["loginCount"] = 0,
            ["lastLogin"] = (object?)null!
        };

        // Act
        var token = _signatureValidator.GenerateSimpleJwtToken(payload, _testSecret);

        // Assert
        token.Should().NotBeNullOrEmpty();
        var parts = token.Split('.');
        parts.Should().HaveCount(3);
    }

    [Fact]
    public void GenerateSimpleJwtToken_ShouldUseBase64UrlEncoding()
    {
        // Arrange
        var payload = new Dictionary<string, object>
        {
            ["data"] = "This is a longer string that should result in base64 with padding characters"
        };

        // Act
        var token = _signatureValidator.GenerateSimpleJwtToken(payload, _testSecret);
        var parts = token.Split('.');

        // Assert
        // Base64URL should not contain =, +, or / characters
        parts[0].Should().NotContain("=");
        parts[0].Should().NotContain("+");
        parts[0].Should().NotContain("/");

        parts[1].Should().NotContain("=");
        parts[1].Should().NotContain("+");
        parts[1].Should().NotContain("/");

        parts[2].Should().NotContain("=");
        parts[2].Should().NotContain("+");
        parts[2].Should().NotContain("/");
    }
}
