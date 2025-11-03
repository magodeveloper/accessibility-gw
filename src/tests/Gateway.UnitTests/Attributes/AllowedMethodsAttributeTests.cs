using Xunit;
using Gateway.Models;
using FluentAssertions;
using System.ComponentModel.DataAnnotations;

namespace Gateway.UnitTests.Attributes
{
    /// <summary>
    /// Tests para AllowedMethodsAttribute - Validación de métodos HTTP permitidos
    /// Target: 100% branch coverage
    /// </summary>
    public class AllowedMethodsAttributeTests
    {
        private readonly AllowedMethodsAttribute _attribute;

        public AllowedMethodsAttributeTests()
        {
            _attribute = new AllowedMethodsAttribute();
        }

        #region Valid HTTP Methods Tests

        [Theory]
        [InlineData("GET")]
        [InlineData("POST")]
        [InlineData("PUT")]
        [InlineData("PATCH")]
        [InlineData("DELETE")]
        [InlineData("HEAD")]
        [InlineData("OPTIONS")]
        public void IsValid_WithValidHttpMethod_ShouldReturnSuccess(string method)
        {
            // Arrange
            var validationContext = new ValidationContext(new object());

            // Act
            var result = _attribute.GetValidationResult(method, validationContext);

            // Assert
            result.Should().Be(ValidationResult.Success);
        }

        [Theory]
        [InlineData("get")]
        [InlineData("post")]
        [InlineData("put")]
        [InlineData("patch")]
        [InlineData("delete")]
        [InlineData("head")]
        [InlineData("options")]
        public void IsValid_WithLowercaseValidHttpMethod_ShouldReturnSuccess(string method)
        {
            // Arrange
            var validationContext = new ValidationContext(new object());

            // Act
            var result = _attribute.GetValidationResult(method, validationContext);

            // Assert
            result.Should().Be(ValidationResult.Success);
        }

        [Theory]
        [InlineData("GeT")]
        [InlineData("PoSt")]
        [InlineData("pUt")]
        [InlineData("PATCH")]
        [InlineData("DeLeTe")]
        public void IsValid_WithMixedCaseValidHttpMethod_ShouldReturnSuccess(string method)
        {
            // Arrange
            var validationContext = new ValidationContext(new object());

            // Act
            var result = _attribute.GetValidationResult(method, validationContext);

            // Assert
            result.Should().Be(ValidationResult.Success);
        }

        #endregion

        #region Invalid HTTP Methods Tests

        [Theory]
        [InlineData("CONNECT")]
        [InlineData("TRACE")]
        [InlineData("INVALID")]
        [InlineData("CUSTOM")]
        [InlineData("")]
        public void IsValid_WithInvalidHttpMethod_ShouldReturnError(string method)
        {
            // Arrange
            var validationContext = new ValidationContext(new object());

            // Act
            var result = _attribute.GetValidationResult(method, validationContext);

            // Assert
            result.Should().NotBe(ValidationResult.Success);
            result?.ErrorMessage.Should().Contain("Method must be one of:");
            result?.ErrorMessage.Should().Contain("GET");
            result?.ErrorMessage.Should().Contain("POST");
            result?.ErrorMessage.Should().Contain("PUT");
            result?.ErrorMessage.Should().Contain("DELETE");
        }

        [Fact]
        public void IsValid_WithNullValue_ShouldReturnError()
        {
            // Arrange
            var validationContext = new ValidationContext(new object());

            // Act
            var result = _attribute.GetValidationResult(null, validationContext);

            // Assert
            result.Should().NotBe(ValidationResult.Success);
            result?.ErrorMessage.Should().Contain("Method must be one of:");
        }

        [Fact]
        public void IsValid_WithNonStringValue_ShouldReturnError()
        {
            // Arrange
            var validationContext = new ValidationContext(new object());
            var invalidValue = 123; // Number instead of string

            // Act
            var result = _attribute.GetValidationResult(invalidValue, validationContext);

            // Assert
            result.Should().NotBe(ValidationResult.Success);
            result?.ErrorMessage.Should().Contain("Method must be one of:");
        }

        [Theory]
        [InlineData("GET POST")]
        [InlineData("GET,POST")]
        [InlineData("GET\nPOST")]
        [InlineData("GET;POST")]
        public void IsValid_WithMultipleMethodsInString_ShouldReturnError(string method)
        {
            // Arrange
            var validationContext = new ValidationContext(new object());

            // Act
            var result = _attribute.GetValidationResult(method, validationContext);

            // Assert
            result.Should().NotBe(ValidationResult.Success);
        }

        #endregion

        #region Edge Cases Tests

        [Theory]
        [InlineData(" GET ")]
        [InlineData(" POST")]
        [InlineData("PUT ")]
        [InlineData("  DELETE  ")]
        public void IsValid_WithWhitespaceAroundMethod_ShouldReturnError(string method)
        {
            // Arrange
            var validationContext = new ValidationContext(new object());

            // Act
            var result = _attribute.GetValidationResult(method, validationContext);

            // Assert
            // La validación no hace trim, por lo que debería fallar
            result.Should().NotBe(ValidationResult.Success);
        }

        [Fact]
        public void IsValid_WithWhitespaceOnly_ShouldReturnError()
        {
            // Arrange
            var validationContext = new ValidationContext(new object());

            // Act
            var result = _attribute.GetValidationResult("   ", validationContext);

            // Assert
            result.Should().NotBe(ValidationResult.Success);
        }

        [Theory]
        [InlineData("GET123")]
        [InlineData("POST_")]
        [InlineData("PUT-")]
        [InlineData("DELETE!")]
        public void IsValid_WithMethodAndSuffix_ShouldReturnError(string method)
        {
            // Arrange
            var validationContext = new ValidationContext(new object());

            // Act
            var result = _attribute.GetValidationResult(method, validationContext);

            // Assert
            result.Should().NotBe(ValidationResult.Success);
        }

        #endregion

        #region Attribute Usage Tests

        [Fact]
        public void AllowedMethodsAttribute_ShouldInheritFromValidationAttribute()
        {
            // Assert
            _attribute.Should().BeAssignableTo<ValidationAttribute>();
        }

        [Fact]
        public void AllowedMethodsAttribute_CanBeInstantiated()
        {
            // Act
            var attribute = new AllowedMethodsAttribute();

            // Assert
            attribute.Should().NotBeNull();
        }

        [Fact]
        public void ErrorMessage_ShouldContainAllAllowedMethods()
        {
            // Arrange
            var validationContext = new ValidationContext(new object());
            var expectedMethods = new[] { "GET", "POST", "PUT", "PATCH", "DELETE", "HEAD", "OPTIONS" };

            // Act
            var result = _attribute.GetValidationResult("INVALID", validationContext);

            // Assert
            result.Should().NotBeNull();
            foreach (var method in expectedMethods)
            {
                result?.ErrorMessage.Should().Contain(method);
            }
        }

        #endregion

        #region Integration Tests with Validation Context

        [Fact]
        public void IsValid_WithComplexObject_ShouldValidateCorrectly()
        {
            // Arrange
            var testObject = new { Method = "GET" };
            var validationContext = new ValidationContext(testObject);

            // Act
            var result = _attribute.GetValidationResult("GET", validationContext);

            // Assert
            result.Should().Be(ValidationResult.Success);
        }

        [Fact]
        public void IsValid_WithInvalidMethodAndComplexContext_ShouldReturnError()
        {
            // Arrange
            var testObject = new { Method = "INVALID" };
            var validationContext = new ValidationContext(testObject)
            {
                MemberName = "Method"
            };

            // Act
            var result = _attribute.GetValidationResult("INVALID", validationContext);

            // Assert
            result.Should().NotBe(ValidationResult.Success);
            result?.ErrorMessage.Should().NotBeNullOrEmpty();
        }

        #endregion

        #region Boundary Tests

        [Theory]
        [InlineData("G")]
        [InlineData("GE")]
        [InlineData("GETX")]
        [InlineData("GETGET")]
        public void IsValid_WithPartialOrExtendedMethodNames_ShouldReturnError(string method)
        {
            // Arrange
            var validationContext = new ValidationContext(new object());

            // Act
            var result = _attribute.GetValidationResult(method, validationContext);

            // Assert
            result.Should().NotBe(ValidationResult.Success);
        }

        [Theory]
        [InlineData("GET\t")]
        [InlineData("\nPOST")]
        [InlineData("PUT\r\n")]
        public void IsValid_WithMethodAndControlCharacters_ShouldReturnError(string method)
        {
            // Arrange
            var validationContext = new ValidationContext(new object());

            // Act
            var result = _attribute.GetValidationResult(method, validationContext);

            // Assert
            result.Should().NotBe(ValidationResult.Success);
        }

        #endregion
    }
}
