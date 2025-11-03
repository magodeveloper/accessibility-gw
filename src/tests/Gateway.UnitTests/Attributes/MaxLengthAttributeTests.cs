using Xunit;
using System;
using Gateway.Models;
using FluentAssertions;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Gateway.UnitTests.Attributes
{
    /// <summary>
    /// Tests para MaxLengthAttribute - Validación de longitud máxima de colecciones
    /// Target: 100% branch coverage
    /// </summary>
    public class MaxLengthAttributeTests
    {
        #region Valid Dictionary Tests

        [Theory]
        [InlineData(1, 0)]
        [InlineData(1, 1)]
        [InlineData(5, 3)]
        [InlineData(5, 5)]
        [InlineData(10, 1)]
        [InlineData(10, 10)]
        [InlineData(100, 50)]
        public void IsValid_WithDictionaryBelowOrEqualMaxLength_ShouldReturnSuccess(int maxLength, int itemCount)
        {
            // Arrange
            var attribute = new Gateway.Models.MaxLengthAttribute(maxLength);
            var validationContext = new ValidationContext(new object());
            var dictionary = new Dictionary<string, string>();

            for (int i = 0; i < itemCount; i++)
            {
                dictionary[$"key{i}"] = $"value{i}";
            }

            // Act
            var result = attribute.GetValidationResult(dictionary, validationContext);

            // Assert
            result.Should().Be(ValidationResult.Success);
        }

        [Theory]
        [InlineData(1, 2)]
        [InlineData(5, 6)]
        [InlineData(10, 11)]
        [InlineData(10, 20)]
        [InlineData(100, 101)]
        public void IsValid_WithDictionaryAboveMaxLength_ShouldReturnError(int maxLength, int itemCount)
        {
            // Arrange
            var attribute = new Gateway.Models.MaxLengthAttribute(maxLength);
            var validationContext = new ValidationContext(new object());
            var dictionary = new Dictionary<string, string>();

            for (int i = 0; i < itemCount; i++)
            {
                dictionary[$"key{i}"] = $"value{i}";
            }

            // Act
            var result = attribute.GetValidationResult(dictionary, validationContext);

            // Assert
            result.Should().NotBe(ValidationResult.Success);
            result?.ErrorMessage.Should().Contain("Collection cannot have more than");
            result?.ErrorMessage.Should().Contain(maxLength.ToString());
        }

        #endregion

        #region Empty Dictionary Tests

        [Fact]
        public void IsValid_WithEmptyDictionary_ShouldReturnSuccess()
        {
            // Arrange
            var attribute = new Gateway.Models.MaxLengthAttribute(10);
            var validationContext = new ValidationContext(new object());
            var dictionary = new Dictionary<string, string>();

            // Act
            var result = attribute.GetValidationResult(dictionary, validationContext);

            // Assert
            result.Should().Be(ValidationResult.Success);
        }

        [Fact]
        public void IsValid_WithEmptyDictionaryAndZeroMaxLength_ShouldReturnSuccess()
        {
            // Arrange
            var attribute = new Gateway.Models.MaxLengthAttribute(0);
            var validationContext = new ValidationContext(new object());
            var dictionary = new Dictionary<string, string>();

            // Act
            var result = attribute.GetValidationResult(dictionary, validationContext);

            // Assert
            result.Should().Be(ValidationResult.Success);
        }

        #endregion

        #region Boundary Tests

        [Fact]
        public void IsValid_WithExactlyMaxLengthItems_ShouldReturnSuccess()
        {
            // Arrange
            var maxLength = 5;
            var attribute = new Gateway.Models.MaxLengthAttribute(maxLength);
            var validationContext = new ValidationContext(new object());
            var dictionary = new Dictionary<string, string>
            {
                { "key1", "value1" },
                { "key2", "value2" },
                { "key3", "value3" },
                { "key4", "value4" },
                { "key5", "value5" }
            };

            // Act
            var result = attribute.GetValidationResult(dictionary, validationContext);

            // Assert
            dictionary.Count.Should().Be(maxLength);
            result.Should().Be(ValidationResult.Success);
        }

        [Fact]
        public void IsValid_WithOneMoreThanMaxLength_ShouldReturnError()
        {
            // Arrange
            var maxLength = 5;
            var attribute = new Gateway.Models.MaxLengthAttribute(maxLength);
            var validationContext = new ValidationContext(new object());
            var dictionary = new Dictionary<string, string>
            {
                { "key1", "value1" },
                { "key2", "value2" },
                { "key3", "value3" },
                { "key4", "value4" },
                { "key5", "value5" },
                { "key6", "value6" }
            };

            // Act
            var result = attribute.GetValidationResult(dictionary, validationContext);

            // Assert
            dictionary.Count.Should().Be(maxLength + 1);
            result.Should().NotBe(ValidationResult.Success);
        }

        #endregion

        #region Non-Dictionary Values Tests

        [Fact]
        public void IsValid_WithNullValue_ShouldReturnSuccess()
        {
            // Arrange
            var attribute = new Gateway.Models.MaxLengthAttribute(10);
            var validationContext = new ValidationContext(new object());

            // Act
            var result = attribute.GetValidationResult(null, validationContext);

            // Assert
            result.Should().Be(ValidationResult.Success);
        }

        [Fact]
        public void IsValid_WithStringValue_ShouldReturnSuccess()
        {
            // Arrange
            var attribute = new Gateway.Models.MaxLengthAttribute(10);
            var validationContext = new ValidationContext(new object());

            // Act
            var result = attribute.GetValidationResult("some string", validationContext);

            // Assert
            result.Should().Be(ValidationResult.Success);
        }

        [Fact]
        public void IsValid_WithIntValue_ShouldReturnSuccess()
        {
            // Arrange
            var attribute = new Gateway.Models.MaxLengthAttribute(10);
            var validationContext = new ValidationContext(new object());

            // Act
            var result = attribute.GetValidationResult(123, validationContext);

            // Assert
            result.Should().Be(ValidationResult.Success);
        }

        [Fact]
        public void IsValid_WithListValue_ShouldReturnSuccess()
        {
            // Arrange
            var attribute = new Gateway.Models.MaxLengthAttribute(10);
            var validationContext = new ValidationContext(new object());
            var list = new List<string> { "item1", "item2", "item3", "item4", "item5", "item6" };

            // Act
            var result = attribute.GetValidationResult(list, validationContext);

            // Assert
            result.Should().Be(ValidationResult.Success);
        }

        [Fact]
        public void IsValid_WithArrayValue_ShouldReturnSuccess()
        {
            // Arrange
            var attribute = new Gateway.Models.MaxLengthAttribute(10);
            var validationContext = new ValidationContext(new object());
            var array = new[] { "item1", "item2", "item3", "item4", "item5", "item6" };

            // Act
            var result = attribute.GetValidationResult(array, validationContext);

            // Assert
            result.Should().Be(ValidationResult.Success);
        }

        #endregion

        #region Edge Cases with Dictionary

        [Fact]
        public void IsValid_WithDictionaryContainingNullValues_ShouldValidateCorrectly()
        {
            // Arrange
            var attribute = new Gateway.Models.MaxLengthAttribute(3);
            var validationContext = new ValidationContext(new object());
#pragma warning disable CS8625
            var dictionary = new Dictionary<string, string>
            {
                { "key1", null },
                { "key2", "value2" },
                { "key3", null }
            };
#pragma warning restore CS8625

            // Act
            var result = attribute.GetValidationResult(dictionary, validationContext);

            // Assert
            result.Should().Be(ValidationResult.Success);
        }

        [Fact]
        public void IsValid_WithDictionaryContainingEmptyStringValues_ShouldValidateCorrectly()
        {
            // Arrange
            var attribute = new Gateway.Models.MaxLengthAttribute(3);
            var validationContext = new ValidationContext(new object());
            var dictionary = new Dictionary<string, string>
            {
                { "key1", "" },
                { "key2", "" },
                { "key3", "" }
            };

            // Act
            var result = attribute.GetValidationResult(dictionary, validationContext);

            // Assert
            result.Should().Be(ValidationResult.Success);
        }

        [Fact]
        public void IsValid_WithDictionaryContainingLongKeys_ShouldValidateCorrectly()
        {
            // Arrange
            var attribute = new Gateway.Models.MaxLengthAttribute(2);
            var validationContext = new ValidationContext(new object());
            var dictionary = new Dictionary<string, string>
            {
                { new string('a', 1000), "value1" },
                { new string('b', 1000), "value2" }
            };

            // Act
            var result = attribute.GetValidationResult(dictionary, validationContext);

            // Assert
            result.Should().Be(ValidationResult.Success);
        }

        [Fact]
        public void IsValid_WithDictionaryContainingLongValues_ShouldValidateCorrectly()
        {
            // Arrange
            var attribute = new Gateway.Models.MaxLengthAttribute(2);
            var validationContext = new ValidationContext(new object());
            var dictionary = new Dictionary<string, string>
            {
                { "key1", new string('x', 10000) },
                { "key2", new string('y', 10000) }
            };

            // Act
            var result = attribute.GetValidationResult(dictionary, validationContext);

            // Assert
            result.Should().Be(ValidationResult.Success);
        }

        #endregion

        #region Constructor and Property Tests

        [Theory]
        [InlineData(1)]
        [InlineData(5)]
        [InlineData(10)]
        [InlineData(100)]
        [InlineData(1000)]
        public void MaxLengthAttribute_Constructor_ShouldSetMaxLength(int maxLength)
        {
            // Act
            var attribute = new Gateway.Models.MaxLengthAttribute(maxLength);

            // Assert
            attribute.MaxLength.Should().Be(maxLength);
        }

        [Fact]
        public void MaxLengthAttribute_ShouldInheritFromValidationAttribute()
        {
            // Arrange & Act
            var attribute = new Gateway.Models.MaxLengthAttribute(10);

            // Assert
            attribute.Should().BeAssignableTo<ValidationAttribute>();
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(-100)]
        public void MaxLengthAttribute_WithZeroOrNegativeMaxLength_ShouldAllowConstruction(int maxLength)
        {
            // Act
            var attribute = new Gateway.Models.MaxLengthAttribute(maxLength);

            // Assert
            attribute.MaxLength.Should().Be(maxLength);
        }

        #endregion

        #region Error Message Tests

        [Fact]
        public void ErrorMessage_ShouldContainMaxLength()
        {
            // Arrange
            var maxLength = 5;
            var attribute = new Gateway.Models.MaxLengthAttribute(maxLength);
            var validationContext = new ValidationContext(new object());
            var dictionary = new Dictionary<string, string>
            {
                { "key1", "value1" },
                { "key2", "value2" },
                { "key3", "value3" },
                { "key4", "value4" },
                { "key5", "value5" },
                { "key6", "value6" }
            };

            // Act
            var result = attribute.GetValidationResult(dictionary, validationContext);

            // Assert
            result?.ErrorMessage.Should().Contain("5");
        }

        [Fact]
        public void ErrorMessage_ShouldExplainValidationFailure()
        {
            // Arrange
            var attribute = new Gateway.Models.MaxLengthAttribute(3);
            var validationContext = new ValidationContext(new object());
            var dictionary = new Dictionary<string, string>
            {
                { "key1", "value1" },
                { "key2", "value2" },
                { "key3", "value3" },
                { "key4", "value4" }
            };

            // Act
            var result = attribute.GetValidationResult(dictionary, validationContext);

            // Assert
            result?.ErrorMessage.Should().Contain("Collection");
            result?.ErrorMessage.Should().Contain("cannot have more than");
            result?.ErrorMessage.Should().Contain("items");
        }

        #endregion

        #region Extreme Values Tests

        [Fact]
        public void IsValid_WithMaxLengthOfIntMaxValue_AndSmallDictionary_ShouldReturnSuccess()
        {
            // Arrange
            var attribute = new Gateway.Models.MaxLengthAttribute(int.MaxValue);
            var validationContext = new ValidationContext(new object());
            var dictionary = new Dictionary<string, string>
            {
                { "key1", "value1" }
            };

            // Act
            var result = attribute.GetValidationResult(dictionary, validationContext);

            // Assert
            result.Should().Be(ValidationResult.Success);
        }

        [Fact]
        public void IsValid_WithMaxLengthOfZero_AndNonEmptyDictionary_ShouldReturnError()
        {
            // Arrange
            var attribute = new Gateway.Models.MaxLengthAttribute(0);
            var validationContext = new ValidationContext(new object());
            var dictionary = new Dictionary<string, string>
            {
                { "key1", "value1" }
            };

            // Act
            var result = attribute.GetValidationResult(dictionary, validationContext);

            // Assert
            result.Should().NotBe(ValidationResult.Success);
        }

        [Fact]
        public void IsValid_WithVeryLargeDictionary_ShouldValidateCorrectly()
        {
            // Arrange
            var maxLength = 1000;
            var attribute = new Gateway.Models.MaxLengthAttribute(maxLength);
            var validationContext = new ValidationContext(new object());
            var dictionary = new Dictionary<string, string>();

            for (int i = 0; i < maxLength; i++)
            {
                dictionary[$"key{i}"] = $"value{i}";
            }

            // Act
            var result = attribute.GetValidationResult(dictionary, validationContext);

            // Assert
            dictionary.Count.Should().Be(maxLength);
            result.Should().Be(ValidationResult.Success);
        }

        #endregion

        #region Integration Tests

        [Fact]
        public void IsValid_WithComplexValidationContext_ShouldValidateCorrectly()
        {
            // Arrange
            var testObject = new { Headers = new Dictionary<string, string>() };
            var attribute = new Gateway.Models.MaxLengthAttribute(10);
            var validationContext = new ValidationContext(testObject)
            {
                MemberName = "Headers"
            };
            var dictionary = new Dictionary<string, string>
            {
                { "header1", "value1" },
                { "header2", "value2" }
            };

            // Act
            var result = attribute.GetValidationResult(dictionary, validationContext);

            // Assert
            result.Should().Be(ValidationResult.Success);
        }

        [Fact]
        public void IsValid_WithComplexValidationContextAndTooManyItems_ShouldReturnError()
        {
            // Arrange
            var testObject = new { Headers = new Dictionary<string, string>() };
            var attribute = new Gateway.Models.MaxLengthAttribute(2);
            var validationContext = new ValidationContext(testObject)
            {
                MemberName = "Headers"
            };
            var dictionary = new Dictionary<string, string>
            {
                { "header1", "value1" },
                { "header2", "value2" },
                { "header3", "value3" }
            };

            // Act
            var result = attribute.GetValidationResult(dictionary, validationContext);

            // Assert
            result.Should().NotBe(ValidationResult.Success);
            result?.ErrorMessage.Should().NotBeNullOrEmpty();
        }

        #endregion

        #region Additional Non-IDictionary Values Tests (Branch Coverage)

        [Fact]
        public void IsValid_WithBooleanValue_ShouldReturnSuccess()
        {
            // Arrange
            var attribute = new Gateway.Models.MaxLengthAttribute(10);
            var validationContext = new ValidationContext(new object());
            var boolValue = true;

            // Act
            var result = attribute.GetValidationResult(boolValue, validationContext);

            // Assert
            // Boolean values are not IDictionary<string, string>, so they pass validation
            result.Should().Be(ValidationResult.Success);
        }

        [Fact]
        public void IsValid_WithDateTimeValue_ShouldReturnSuccess()
        {
            // Arrange
            var attribute = new Gateway.Models.MaxLengthAttribute(10);
            var validationContext = new ValidationContext(new object());
            var dateValue = DateTime.Now;

            // Act
            var result = attribute.GetValidationResult(dateValue, validationContext);

            // Assert
            // DateTime values are not IDictionary<string, string>, so they pass validation
            result.Should().Be(ValidationResult.Success);
        }

        [Fact]
        public void IsValid_WithComplexObjectValue_ShouldReturnSuccess()
        {
            // Arrange
            var attribute = new Gateway.Models.MaxLengthAttribute(10);
            var validationContext = new ValidationContext(new object());
            var objectValue = new { Property1 = "value1", Property2 = "value2", Property3 = 123 };

            // Act
            var result = attribute.GetValidationResult(objectValue, validationContext);

            // Assert
            // Anonymous objects are not IDictionary<string, string>, so they pass validation
            result.Should().Be(ValidationResult.Success);
        }

        #endregion

        #region Constructor and Property Tests

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(100)]
        [InlineData(int.MaxValue)]
        public void Constructor_WithVariousMaxLengths_ShouldSetProperty(int maxLength)
        {
            // Act
            var attribute = new Gateway.Models.MaxLengthAttribute(maxLength);

            // Assert
            attribute.MaxLength.Should().Be(maxLength);
        }

        #endregion
    }
}
