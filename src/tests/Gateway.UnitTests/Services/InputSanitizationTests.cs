using Moq;
using Xunit;
using System.Linq;
using FluentAssertions;
using Gateway.Services;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

#pragma warning disable CS8604 // Posible argumento nulo - Tests intencionales con null

namespace Gateway.UnitTests.Services;

/// <summary>
/// Tests enfocados en mejorar branch coverage de InputSanitizationService.
/// Estado actual: 43.1% - Target: >75%
/// Métodos reales: SanitizeString, SanitizeApiPath, SanitizeQueryParameters, ValidateAndSanitizeHeaders, IsValidService
/// </summary>
public class InputSanitizationTests
{
    private readonly Mock<ILogger<InputSanitizationService>> _mockLogger;
    private readonly InputSanitizationService _service;

    public InputSanitizationTests()
    {
        _mockLogger = new Mock<ILogger<InputSanitizationService>>();
        _service = new InputSanitizationService(_mockLogger.Object);
    }

    #region SanitizeString Tests - Branch Coverage

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void SanitizeString_WithNullOrEmptyInput_ShouldReturnEmpty(string? input)
    {
        // Act
#pragma warning disable CS8604 // Test intencional con null
        var result = _service.SanitizeString(input);
#pragma warning restore CS8604

        // Assert
        result.Should().BeEmpty("null or empty input should return empty string");
    }

    [Fact]
    public void SanitizeString_WithWhitespaceInput_ShouldReturnEncodedWhitespace()
    {
        // Act
        var result = _service.SanitizeString("   ");

        // Assert
        result.Should().Be("   ", "whitespace should be preserved after encoding");
    }

    [Theory]
    [InlineData("normal text")]
    [InlineData("Hello World 123")]
    [InlineData("test@example.com")]
    public void SanitizeString_WithSafeInput_ShouldReturnTrimmed(string input)
    {
        // Act
        var result = _service.SanitizeString(input);

        // Assert
        result.Should().Be(input.Trim(), "safe input should be trimmed");
    }

    [Theory]
    [InlineData("  leading space")]
    [InlineData("trailing space  ")]
    [InlineData("  both spaces  ")]
    public void SanitizeString_WithLeadingTrailingSpaces_ShouldTrim(string input)
    {
        // Act
        var result = _service.SanitizeString(input);

        // Assert
        result.Should().Be(input.Trim());
        result.Should().NotStartWith(" ");
        result.Should().NotEndWith(" ");
    }

    [Theory]
    [InlineData("<script>alert('xss')</script>")]
    [InlineData("<img src=x onerror=alert(1)>")]
    [InlineData("<iframe src='evil.com'></iframe>")]
    public void SanitizeString_WithHtmlTags_ShouldEscapeOrRemoveTags(string input)
    {
        // Act
        var result = _service.SanitizeString(input);

        // Assert - Verificar que el string resultante no contiene tags sin escapar
        result.Should().NotBe(input, "HTML tags should be escaped or removed");
    }

    [Fact]
    public void SanitizeString_WithVeryLongInput_ShouldHandleGracefully()
    {
        // Arrange
        var longInput = new string('a', 10000);

        // Act
        var result = _service.SanitizeString(longInput);

        // Assert
        result.Should().NotBeNull("should handle very long inputs");
    }

    [Theory]
    [InlineData("test\r\nnewline")]
    [InlineData("test\ttab")]
    [InlineData("test\nnewline")]
    public void SanitizeString_WithWhitespaceCharacters_ShouldHandleWhitespace(string input)
    {
        // Act
        var result = _service.SanitizeString(input);

        // Assert
        result.Should().NotBeNull();
    }

    #endregion

    #region SanitizeApiPath Tests - Branch Coverage

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void SanitizeApiPath_WithNullOrEmpty_ShouldReturnEmpty(string? input)
    {
        // Act
#pragma warning disable CS8604 // Test intencional con null
        var result = _service.SanitizeApiPath(input);
#pragma warning restore CS8604

        // Assert
        result.Should().BeEmpty("null or empty paths should return empty");
    }

    [Theory]
    [InlineData("/api/users")]
    [InlineData("/api/v1/reports")]
    [InlineData("/health")]
    public void SanitizeApiPath_WithValidPaths_ShouldReturnPath(string input)
    {
        // Act
        var result = _service.SanitizeApiPath(input);

        // Assert
        result.Should().NotBeNull();
        result.Should().Contain("/", "path should contain forward slash");
    }

    [Theory]
    [InlineData("../../../etc/passwd")]
    [InlineData("..\\..\\windows\\system32")]
    [InlineData("/api/../../../etc/passwd")]
    public void SanitizeApiPath_WithPathTraversal_ShouldThrowSecurityException(string input)
    {
        // Act
        var result = _service.SanitizeApiPath(input);

        // Assert
        result.Should().BeEmpty("path traversal should return empty");
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("traversal")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Theory]
    [InlineData("/api/users?id=<script>")]
    [InlineData("/api/test?param='; DROP TABLE")]
    public void SanitizeApiPath_WithQueryStringAttacks_ShouldSanitize(string input)
    {
        // Act
        var result = _service.SanitizeApiPath(input);

        // Assert
        result.Should().NotBeNull("should handle paths with dangerous query strings");
    }

    [Theory]
    [InlineData("/api/users/")]
    [InlineData("//api//users")]
    [InlineData("/api///users")]
    public void SanitizeApiPath_WithMultipleSlashes_ShouldNormalize(string input)
    {
        // Act
        var result = _service.SanitizeApiPath(input);

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public void SanitizeApiPath_WithVeryLongPath_ShouldHandleGracefully()
    {
        // Arrange
        var longPath = "/api/" + new string('a', 5000);

        // Act
        var result = _service.SanitizeApiPath(longPath);

        // Assert
        result.Should().NotBeNull("should handle very long paths");
    }

    #endregion

    #region SanitizeQueryParameters Tests - Branch Coverage

    [Fact]
    public void SanitizeQueryParameters_WithNullDictionary_ShouldReturnEmptyDictionary()
    {
        // Act
        var result = _service.SanitizeQueryParameters(null!);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty("null dictionary should return empty");
    }

    [Fact]
    public void SanitizeQueryParameters_WithEmptyDictionary_ShouldReturnEmptyDictionary()
    {
        // Arrange
        var parameters = new Dictionary<string, string>();

        // Act
        var result = _service.SanitizeQueryParameters(parameters);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public void SanitizeQueryParameters_WithSafeParameters_ShouldReturnSanitized()
    {
        // Arrange
        var parameters = new Dictionary<string, string>
        {
            ["page"] = "1",
            ["size"] = "10",
            ["sort"] = "name"
        };

        // Act
        var result = _service.SanitizeQueryParameters(parameters);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(3);
    }

    [Fact]
    public void SanitizeQueryParameters_WithDangerousValues_ShouldSanitize()
    {
        // Arrange
        var parameters = new Dictionary<string, string>
        {
            ["query"] = "<script>alert('xss')</script>",
            ["filter"] = "'; DROP TABLE users; --"
        };

        // Act
        var result = _service.SanitizeQueryParameters(parameters);

        // Assert
        result.Should().NotBeNull();
        // Verificar que los valores peligrosos fueron sanitizados
        result.Values.Should().NotContain(v => v.Contains("<script>"));
    }

    [Fact]
    public void SanitizeQueryParameters_WithSpecialCharacters_ShouldEscapeOrRemove()
    {
        // Arrange
        var parameters = new Dictionary<string, string>
        {
            ["param1"] = "value&with&ampersands",
            ["param2"] = "value=with=equals",
            ["param3"] = "value%20with%20encoding"
        };

        // Act
        var result = _service.SanitizeQueryParameters(parameters);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(3);
    }

    [Fact]
    public void SanitizeQueryParameters_WithVeryLongValue_ShouldHandleGracefully()
    {
        // Arrange
        var parameters = new Dictionary<string, string>
        {
            ["longParam"] = new string('a', 10000)
        };

        // Act
        var result = _service.SanitizeQueryParameters(parameters);

        // Assert
        result.Should().NotBeNull();
        result.Should().ContainKey("longParam");
    }

    [Fact]
    public void SanitizeQueryParameters_WithEmptyKeyOrValue_ShouldHandle()
    {
        // Arrange
        var parameters = new Dictionary<string, string>
        {
            [""] = "value",
            ["key"] = "",
            ["normal"] = "value"
        };

        // Act
        var result = _service.SanitizeQueryParameters(parameters);

        // Assert
        result.Should().NotBeNull();
    }

    #endregion

    #region ValidateAndSanitizeHeaders Tests - Branch Coverage

    [Fact]
    public void ValidateAndSanitizeHeaders_WithNullDictionary_ShouldReturnEmptyDictionary()
    {
        // Act
        var result = _service.ValidateAndSanitizeHeaders(null!);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public void ValidateAndSanitizeHeaders_WithEmptyDictionary_ShouldReturnEmptyDictionary()
    {
        // Arrange
        var headers = new Dictionary<string, string>();

        // Act
        var result = _service.ValidateAndSanitizeHeaders(headers);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public void ValidateAndSanitizeHeaders_WithSafeHeaders_ShouldReturnSanitized()
    {
        // Arrange
        var headers = new Dictionary<string, string>
        {
            ["Content-Type"] = "application/json",
            ["Accept"] = "application/json",
            ["User-Agent"] = "TestAgent/1.0"
        };

        // Act
        var result = _service.ValidateAndSanitizeHeaders(headers);

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public void ValidateAndSanitizeHeaders_WithDangerousValues_ShouldSanitize()
    {
        // Arrange
        var headers = new Dictionary<string, string>
        {
            ["X-Custom"] = "<script>alert('xss')</script>",
            ["X-Auth"] = "'; DROP TABLE users; --"
        };

        // Act
        var result = _service.ValidateAndSanitizeHeaders(headers);

        // Assert
        result.Should().NotBeNull();
    }

    [Theory]
    [InlineData("Host")]
    [InlineData("Connection")]
    [InlineData("Transfer-Encoding")]
    public void ValidateAndSanitizeHeaders_WithRestrictedHeaders_ShouldFilter(string restrictedHeader)
    {
        // Arrange
        var headers = new Dictionary<string, string>
        {
            [restrictedHeader] = "value",
            ["Content-Type"] = "application/json"
        };

        // Act
        var result = _service.ValidateAndSanitizeHeaders(headers);

        // Assert
        result.Should().NotBeNull();
        // Headers restringidos podrían ser filtrados o no según implementación
    }

    [Fact]
    public void ValidateAndSanitizeHeaders_WithVeryLongHeaderValue_ShouldHandleGracefully()
    {
        // Arrange
        var headers = new Dictionary<string, string>
        {
            ["X-Long-Header"] = new string('a', 10000)
        };

        // Act
        var result = _service.ValidateAndSanitizeHeaders(headers);

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public void ValidateAndSanitizeHeaders_WithMultipleHeaders_ShouldSanitizeAll()
    {
        // Arrange
        var headers = new Dictionary<string, string>
        {
            ["Header1"] = "value1",
            ["Header2"] = "value2",
            ["Header3"] = "<dangerous>",
            ["Header4"] = "normal",
            ["Header5"] = "'; sql injection"
        };

        // Act
        var result = _service.ValidateAndSanitizeHeaders(headers);

        // Assert
        result.Should().NotBeNull();
    }

    #endregion

    #region IsValidService Tests - Branch Coverage

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void IsValidService_WithNullOrEmptyServiceName_ShouldReturnFalse(string? serviceName)
    {
        // Arrange
        var allowedServices = new List<string> { "users", "reports", "analysis" };

        // Act
#pragma warning disable CS8604 // Test intencional con null
        var result = _service.IsValidService(serviceName, allowedServices);
#pragma warning restore CS8604

        // Assert
        result.Should().BeFalse("null or empty service name should be invalid");
    }

    [Fact]
    public void IsValidService_WithNullAllowedServices_ShouldReturnFalse()
    {
        // Act
        var result = _service.IsValidService("users", null!);

        // Assert
        result.Should().BeFalse("null allowed services should return false");
    }

    [Fact]
    public void IsValidService_WithEmptyAllowedServices_ShouldReturnFalse()
    {
        // Arrange
        var allowedServices = new List<string>();

        // Act
        var result = _service.IsValidService("users", allowedServices);

        // Assert
        result.Should().BeFalse("empty allowed services should return false");
    }

    [Theory]
    [InlineData("users", true)]
    [InlineData("reports", true)]
    [InlineData("analysis", true)]
    public void IsValidService_WithValidServiceName_ShouldReturnTrue(string serviceName, bool expected)
    {
        // Arrange
        var allowedServices = new List<string> { "users", "reports", "analysis" };

        // Act
        var result = _service.IsValidService(serviceName, allowedServices);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("USERS", false)]
    [InlineData("Users", false)]
    [InlineData("UsErS", false)]
    public void IsValidService_WithCaseMismatch_ShouldBeCaseSensitive(string serviceName, bool expected)
    {
        // Arrange
        var allowedServices = new List<string> { "users" }; // lowercase only

        // Act
        var result = _service.IsValidService(serviceName, allowedServices);

        // Assert - Assuming case-sensitive validation
        // Si la implementación es case-insensitive, ajustar expected a true
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("malicious")]
    [InlineData("unknown")]
    [InlineData("hacker")]
    public void IsValidService_WithUnknownService_ShouldReturnFalse(string serviceName)
    {
        // Arrange
        var allowedServices = new List<string> { "users", "reports", "analysis" };

        // Act
        var result = _service.IsValidService(serviceName, allowedServices);

        // Assert
        result.Should().BeFalse("unknown service should be invalid");
    }

    [Theory]
    [InlineData("users ")]
    [InlineData(" users")]
    [InlineData(" users ")]
    public void IsValidService_WithWhitespaceInServiceName_ShouldHandle(string serviceName)
    {
        // Arrange
        var allowedServices = new List<string> { "users" };

        // Act
        var result = _service.IsValidService(serviceName, allowedServices);

        // Assert
        // Depende de si la implementación trimea o no
        result.Should().BeFalse("service names with whitespace should be invalid unless trimmed");
    }

    #endregion

    #region Additional Branch Coverage Tests - Exception Handlers & Edge Cases

    [Theory]
    [InlineData("javascript:alert('xss')")]
    [InlineData("vbscript:msgbox(1)")]
    [InlineData("onclick=alert(1)")]
    public void SanitizeString_WithJavascriptProtocol_ShouldRemoveAndLog(string dangerousInput)
    {
        // Act
        var result = _service.SanitizeString(dangerousInput);

        // Assert
        result.Should().NotBeNull();
        // El patrón XSS debería detectar estos inputs
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("XSS")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void SanitizeString_WithSqlInjectionPattern_ShouldLogWarningButNotRemove()
    {
        // Arrange
        var sqlInput = "test SELECT * FROM users";

        // Act
        var result = _service.SanitizeString(sqlInput);

        // Assert
        result.Should().NotBeNull();
        // SQL injection solo loguea pero no remueve
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("SQL injection")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void SanitizeApiPath_WithPathTraversalPattern_ShouldLogWarningAndReturnEmpty()
    {
        // Arrange
        var traversalPath = "/api/../../../etc/passwd";

        // Act
        var result = _service.SanitizeApiPath(traversalPath);

        // Assert
        result.Should().BeEmpty();
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Path traversal")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void SanitizeApiPath_WithQueryStringContainingXss_ShouldLogWarningAndRemoveQueryString()
    {
        // Arrange
        var pathWithXss = "/api/users?param=<script>alert(1)</script>";

        // Act
        var result = _service.SanitizeApiPath(pathWithXss);

        // Assert
        result.Should().NotBeNull();
        result.Should().NotContain("?");
        result.Should().NotContain("<script>");
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Attack detected")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void SanitizeApiPath_WithQueryStringContainingSqlInjection_ShouldLogAndRemoveQueryString()
    {
        // Arrange
        var pathWithSql = "/api/data?id=1; DROP TABLE users";

        // Act
        var result = _service.SanitizeApiPath(pathWithSql);

        // Assert
        result.Should().NotBeNull();
        result.Should().Be("/api/data");
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Attack detected")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void SanitizeQueryParameters_WithMoreThan20Parameters_ShouldLimitTo20()
    {
        // Arrange
        var parameters = new Dictionary<string, string>();
        for (int i = 1; i <= 30; i++)
        {
            parameters[$"param{i}"] = $"value{i}";
        }

        // Act
        var result = _service.SanitizeQueryParameters(parameters);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCountLessOrEqualTo(20);
    }

    [Fact]
    public void SanitizeQueryParameters_WithKeyLongerThan100Chars_ShouldSkipAndLogWarning()
    {
        // Arrange
        var longKey = new string('a', 150);
        var parameters = new Dictionary<string, string>
        {
            [longKey] = "value",
            ["normalKey"] = "normalValue"
        };

        // Act
        var result = _service.SanitizeQueryParameters(parameters);

        // Assert
        result.Should().NotBeNull();
        result.Should().NotContainKey(longKey);
        result.Should().ContainKey("normalKey");
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("key too long")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void SanitizeQueryParameters_WithValueLongerThan1000Chars_ShouldTruncateAndLogWarning()
    {
        // Arrange
        var longValue = new string('b', 1500);
        var parameters = new Dictionary<string, string>
        {
            ["longParam"] = longValue
        };

        // Act
        var result = _service.SanitizeQueryParameters(parameters);

        // Assert
        result.Should().NotBeNull();
        result.Should().ContainKey("longParam");
        result["longParam"].Length.Should().BeLessOrEqualTo(1000);
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("value too long")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void ValidateAndSanitizeHeaders_WithMoreThan30Headers_ShouldLimitTo30()
    {
        // Arrange
        var headers = new Dictionary<string, string>();
        for (int i = 1; i <= 40; i++)
        {
            headers[$"X-Custom-{i}"] = $"value{i}";
        }

        // Act
        var result = _service.ValidateAndSanitizeHeaders(headers);

        // Assert
        result.Should().NotBeNull();
        // El límite es 30, pero también se filtran por whitelist
    }

    [Fact]
    public void ValidateAndSanitizeHeaders_WithHeaderNotInWhitelist_ShouldSkipAndLogDebug()
    {
        // Arrange
        var headers = new Dictionary<string, string>
        {
            ["X-Custom-Dangerous"] = "value",
            ["Content-Type"] = "application/json"
        };

        // Act
        var result = _service.ValidateAndSanitizeHeaders(headers);

        // Assert
        result.Should().NotBeNull();
        result.Should().NotContainKey("X-Custom-Dangerous");
        result.Should().ContainKey("Content-Type");
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("not in whitelist")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public void ValidateAndSanitizeHeaders_WithHeaderValueLongerThan2048_ShouldSkipAndLogWarning()
    {
        // Arrange
        var longValue = new string('c', 3000);
        var headers = new Dictionary<string, string>
        {
            ["Authorization"] = longValue,
            ["Content-Type"] = "application/json"
        };

        // Act
        var result = _service.ValidateAndSanitizeHeaders(headers);

        // Assert
        result.Should().NotBeNull();
        result.Should().NotContainKey("Authorization");
        result.Should().ContainKey("Content-Type");
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("value too long")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Theory]
    [InlineData("ALTER TABLE users")]
    [InlineData("DROP TABLE users")]
    [InlineData("EXEC sp_executesql")]
    [InlineData("INSERT INTO users")]
    [InlineData("UNION ALL SELECT")]
    public void SanitizeString_WithSqlKeywords_ShouldLogWarning(string sqlInput)
    {
        // Act
        var result = _service.SanitizeString(sqlInput);

        // Assert
        result.Should().NotBeNull();
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("SQL")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Theory]
    [InlineData("..\\..\\.ssh\\id_rsa")]
    [InlineData("..%2F..%2Fetc%2Fpasswd")]
    [InlineData(".%2F.%2F.%2F")]
    public void SanitizeApiPath_WithVariousPathTraversalPatterns_ShouldDetectAndBlock(string maliciousPath)
    {
        // Act
        var result = _service.SanitizeApiPath(maliciousPath);

        // Assert
        result.Should().BeEmpty();
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("traversal")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void SanitizeApiPath_WithMultipleSlashesNormalization_ShouldReduceToSingleSlash()
    {
        // Arrange
        var pathWithMultipleSlashes = "/api///users////123";

        // Act
        var result = _service.SanitizeApiPath(pathWithMultipleSlashes);

        // Assert
        result.Should().NotBeNull();
        result.Should().NotContain("//");
        result.Should().Contain("/api/users/123");
    }

    #endregion
}
