using Xunit;
using Gateway.Models;
using FluentAssertions;
using System.ComponentModel.DataAnnotations;

namespace Gateway.UnitTests.Attributes
{
    /// <summary>
    /// Tests para SafeIdRangeAttribute - Validación de rangos de IDs seguros
    /// Target: 100% branch coverage
    /// </summary>
    public class SafeIdRangeAttributeTests
    {
        #region Default Range Tests (1 to int.MaxValue)

        [Theory]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(100)]
        [InlineData(1000)]
        [InlineData(int.MaxValue)]
        public void IsValid_WithDefaultRangeAndValidId_ShouldReturnSuccess(int id)
        {
            // Arrange
            var attribute = new SafeIdRangeAttribute();
            var validationContext = new ValidationContext(new object());

            // Act
            var result = attribute.GetValidationResult(id, validationContext);

            // Assert
            result.Should().Be(ValidationResult.Success);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(-100)]
        [InlineData(-1000)]
        [InlineData(int.MinValue)]
        public void IsValid_WithDefaultRangeAndInvalidId_ShouldReturnError(int id)
        {
            // Arrange
            var attribute = new SafeIdRangeAttribute();
            var validationContext = new ValidationContext(new object());

            // Act
            var result = attribute.GetValidationResult(id, validationContext);

            // Assert
            result.Should().NotBe(ValidationResult.Success);
            result?.ErrorMessage.Should().Contain("ID must be between");
            result?.ErrorMessage.Should().Contain("1");
            result?.ErrorMessage.Should().Contain(int.MaxValue.ToString());
        }

        #endregion

        #region Custom Range Tests

        [Theory]
        [InlineData(1, 100, 1)]
        [InlineData(1, 100, 50)]
        [InlineData(1, 100, 100)]
        [InlineData(10, 20, 10)]
        [InlineData(10, 20, 15)]
        [InlineData(10, 20, 20)]
        public void IsValid_WithCustomRangeAndValidId_ShouldReturnSuccess(int min, int max, int id)
        {
            // Arrange
            var attribute = new SafeIdRangeAttribute { Minimum = min, Maximum = max };
            var validationContext = new ValidationContext(new object());

            // Act
            var result = attribute.GetValidationResult(id, validationContext);

            // Assert
            result.Should().Be(ValidationResult.Success);
        }

        [Theory]
        [InlineData(1, 100, 0)]
        [InlineData(1, 100, 101)]
        [InlineData(10, 20, 9)]
        [InlineData(10, 20, 21)]
        [InlineData(100, 200, 50)]
        [InlineData(100, 200, 250)]
        public void IsValid_WithCustomRangeAndInvalidId_ShouldReturnError(int min, int max, int id)
        {
            // Arrange
            var attribute = new SafeIdRangeAttribute { Minimum = min, Maximum = max };
            var validationContext = new ValidationContext(new object());

            // Act
            var result = attribute.GetValidationResult(id, validationContext);

            // Assert
            result.Should().NotBe(ValidationResult.Success);
            result?.ErrorMessage.Should().Contain("ID must be between");
            result?.ErrorMessage.Should().Contain(min.ToString());
            result?.ErrorMessage.Should().Contain(max.ToString());
        }

        #endregion

        #region Boundary Tests

        [Fact]
        public void IsValid_WithMinimumBoundaryValue_ShouldReturnSuccess()
        {
            // Arrange
            var attribute = new SafeIdRangeAttribute { Minimum = 100, Maximum = 200 };
            var validationContext = new ValidationContext(new object());

            // Act
            var result = attribute.GetValidationResult(100, validationContext);

            // Assert
            result.Should().Be(ValidationResult.Success);
        }

        [Fact]
        public void IsValid_WithMaximumBoundaryValue_ShouldReturnSuccess()
        {
            // Arrange
            var attribute = new SafeIdRangeAttribute { Minimum = 100, Maximum = 200 };
            var validationContext = new ValidationContext(new object());

            // Act
            var result = attribute.GetValidationResult(200, validationContext);

            // Assert
            result.Should().Be(ValidationResult.Success);
        }

        [Fact]
        public void IsValid_WithValueBelowMinimum_ShouldReturnError()
        {
            // Arrange
            var attribute = new SafeIdRangeAttribute { Minimum = 100, Maximum = 200 };
            var validationContext = new ValidationContext(new object());

            // Act
            var result = attribute.GetValidationResult(99, validationContext);

            // Assert
            result.Should().NotBe(ValidationResult.Success);
        }

        [Fact]
        public void IsValid_WithValueAboveMaximum_ShouldReturnError()
        {
            // Arrange
            var attribute = new SafeIdRangeAttribute { Minimum = 100, Maximum = 200 };
            var validationContext = new ValidationContext(new object());

            // Act
            var result = attribute.GetValidationResult(201, validationContext);

            // Assert
            result.Should().NotBe(ValidationResult.Success);
        }

        #endregion

        #region Edge Cases Tests

        [Fact]
        public void IsValid_WithNullValue_ShouldReturnError()
        {
            // Arrange
            var attribute = new SafeIdRangeAttribute();
            var validationContext = new ValidationContext(new object());

            // Act
            var result = attribute.GetValidationResult(null, validationContext);

            // Assert
            result.Should().NotBe(ValidationResult.Success);
        }

        [Fact]
        public void IsValid_WithNonIntegerValue_ShouldReturnError()
        {
            // Arrange
            var attribute = new SafeIdRangeAttribute();
            var validationContext = new ValidationContext(new object());

            // Act
            var result = attribute.GetValidationResult("not-an-integer", validationContext);

            // Assert
            result.Should().NotBe(ValidationResult.Success);
        }

        [Fact]
        public void IsValid_WithLongValue_ShouldReturnError()
        {
            // Arrange
            var attribute = new SafeIdRangeAttribute();
            var validationContext = new ValidationContext(new object());
            long longValue = 123L;

            // Act
            var result = attribute.GetValidationResult(longValue, validationContext);

            // Assert
            result.Should().NotBe(ValidationResult.Success);
        }

        [Fact]
        public void IsValid_WithDoubleValue_ShouldReturnError()
        {
            // Arrange
            var attribute = new SafeIdRangeAttribute();
            var validationContext = new ValidationContext(new object());
            double doubleValue = 123.45;

            // Act
            var result = attribute.GetValidationResult(doubleValue, validationContext);

            // Assert
            result.Should().NotBe(ValidationResult.Success);
        }

        #endregion

        #region Extreme Range Tests

        [Fact]
        public void IsValid_WithZeroMinimum_ShouldValidateCorrectly()
        {
            // Arrange
            var attribute = new SafeIdRangeAttribute { Minimum = 0, Maximum = 100 };
            var validationContext = new ValidationContext(new object());

            // Act
            var result0 = attribute.GetValidationResult(0, validationContext);
            var resultNeg = attribute.GetValidationResult(-1, validationContext);

            // Assert
            result0.Should().Be(ValidationResult.Success);
            resultNeg.Should().NotBe(ValidationResult.Success);
        }

        [Fact]
        public void IsValid_WithNegativeRange_ShouldValidateCorrectly()
        {
            // Arrange
            var attribute = new SafeIdRangeAttribute { Minimum = -100, Maximum = -1 };
            var validationContext = new ValidationContext(new object());

            // Act
            var resultValid = attribute.GetValidationResult(-50, validationContext);
            var resultInvalid = attribute.GetValidationResult(0, validationContext);

            // Assert
            resultValid.Should().Be(ValidationResult.Success);
            resultInvalid.Should().NotBe(ValidationResult.Success);
        }

        [Fact]
        public void IsValid_WithSingleValueRange_ShouldValidateCorrectly()
        {
            // Arrange
            var attribute = new SafeIdRangeAttribute { Minimum = 42, Maximum = 42 };
            var validationContext = new ValidationContext(new object());

            // Act
            var resultValid = attribute.GetValidationResult(42, validationContext);
            var resultLow = attribute.GetValidationResult(41, validationContext);
            var resultHigh = attribute.GetValidationResult(43, validationContext);

            // Assert
            resultValid.Should().Be(ValidationResult.Success);
            resultLow.Should().NotBe(ValidationResult.Success);
            resultHigh.Should().NotBe(ValidationResult.Success);
        }

        [Fact]
        public void IsValid_WithIntMaxValue_ShouldValidateCorrectly()
        {
            // Arrange
            var attribute = new SafeIdRangeAttribute { Minimum = 1, Maximum = int.MaxValue };
            var validationContext = new ValidationContext(new object());

            // Act
            var result = attribute.GetValidationResult(int.MaxValue, validationContext);

            // Assert
            result.Should().Be(ValidationResult.Success);
        }

        [Fact]
        public void IsValid_WithIntMinValue_ShouldReturnError()
        {
            // Arrange
            var attribute = new SafeIdRangeAttribute { Minimum = 1, Maximum = int.MaxValue };
            var validationContext = new ValidationContext(new object());

            // Act
            var result = attribute.GetValidationResult(int.MinValue, validationContext);

            // Assert
            result.Should().NotBe(ValidationResult.Success);
        }

        #endregion

        #region Attribute Properties Tests

        [Fact]
        public void SafeIdRangeAttribute_ShouldInheritFromValidationAttribute()
        {
            // Arrange & Act
            var attribute = new SafeIdRangeAttribute();

            // Assert
            attribute.Should().BeAssignableTo<ValidationAttribute>();
        }

        [Fact]
        public void SafeIdRangeAttribute_DefaultMinimum_ShouldBe1()
        {
            // Arrange & Act
            var attribute = new SafeIdRangeAttribute();

            // Assert
            attribute.Minimum.Should().Be(1);
        }

        [Fact]
        public void SafeIdRangeAttribute_DefaultMaximum_ShouldBeIntMaxValue()
        {
            // Arrange & Act
            var attribute = new SafeIdRangeAttribute();

            // Assert
            attribute.Maximum.Should().Be(int.MaxValue);
        }

        [Fact]
        public void SafeIdRangeAttribute_CanSetMinimum()
        {
            // Arrange & Act
            var attribute = new SafeIdRangeAttribute { Minimum = 100 };

            // Assert
            attribute.Minimum.Should().Be(100);
        }

        [Fact]
        public void SafeIdRangeAttribute_CanSetMaximum()
        {
            // Arrange & Act
            var attribute = new SafeIdRangeAttribute { Maximum = 1000 };

            // Assert
            attribute.Maximum.Should().Be(1000);
        }

        #endregion

        #region Error Message Tests

        [Fact]
        public void ErrorMessage_ShouldIncludeMinimumAndMaximumValues()
        {
            // Arrange
            var attribute = new SafeIdRangeAttribute { Minimum = 10, Maximum = 100 };
            var validationContext = new ValidationContext(new object());

            // Act
            var result = attribute.GetValidationResult(5, validationContext);

            // Assert
            result?.ErrorMessage.Should().Contain("10");
            result?.ErrorMessage.Should().Contain("100");
        }

        [Fact]
        public void ErrorMessage_WithDefaultRange_ShouldIncludeDefaultValues()
        {
            // Arrange
            var attribute = new SafeIdRangeAttribute();
            var validationContext = new ValidationContext(new object());

            // Act
            var result = attribute.GetValidationResult(0, validationContext);

            // Assert
            result?.ErrorMessage.Should().Contain("1");
            result?.ErrorMessage.Should().Contain(int.MaxValue.ToString());
        }

        #endregion

        #region Integration Tests

        [Fact]
        public void IsValid_WithComplexValidationContext_ShouldValidateCorrectly()
        {
            // Arrange
            var testObject = new { UserId = 123 };
            var attribute = new SafeIdRangeAttribute { Minimum = 1, Maximum = 1000 };
            var validationContext = new ValidationContext(testObject)
            {
                MemberName = "UserId"
            };

            // Act
            var result = attribute.GetValidationResult(123, validationContext);

            // Assert
            result.Should().Be(ValidationResult.Success);
        }

        [Fact]
        public void IsValid_WithComplexValidationContextAndInvalidId_ShouldReturnError()
        {
            // Arrange
            var testObject = new { UserId = 5000 };
            var attribute = new SafeIdRangeAttribute { Minimum = 1, Maximum = 1000 };
            var validationContext = new ValidationContext(testObject)
            {
                MemberName = "UserId"
            };

            // Act
            var result = attribute.GetValidationResult(5000, validationContext);

            // Assert
            result.Should().NotBe(ValidationResult.Success);
            result?.ErrorMessage.Should().NotBeNullOrEmpty();
        }

        #endregion

        #region Property Tests

        [Fact]
        public void DefaultConstructor_ShouldSetDefaultMinimumAndMaximum()
        {
            // Act
            var attribute = new SafeIdRangeAttribute();

            // Assert
            attribute.Minimum.Should().Be(1, "default minimum should be 1");
            attribute.Maximum.Should().Be(int.MaxValue, "default maximum should be int.MaxValue");
        }

        [Theory]
        [InlineData(0, 100)]
        [InlineData(1, 1000)]
        [InlineData(-100, 100)]
        [InlineData(int.MinValue, int.MaxValue)]
        public void Properties_ShouldBeSettableAndGettable(int minimum, int maximum)
        {
            // Act
            var attribute = new SafeIdRangeAttribute { Minimum = minimum, Maximum = maximum };

            // Assert
            attribute.Minimum.Should().Be(minimum);
            attribute.Maximum.Should().Be(maximum);
        }

        #endregion
    }
}
