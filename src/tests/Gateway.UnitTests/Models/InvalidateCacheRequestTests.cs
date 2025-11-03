using Xunit;
using Gateway.Models;
using FluentAssertions;
using System.ComponentModel.DataAnnotations;

namespace Gateway.UnitTests.Models
{
    /// <summary>
    /// Tests para InvalidateCacheRequest - DTO para invalidar caché
    /// Target: 100% coverage
    /// </summary>
    public class InvalidateCacheRequestTests
    {
        #region Valid Request Tests

        [Theory]
        [InlineData("users", null)]
        [InlineData("reports", "api/*")]
        [InlineData("analysis", "api/analysis/*")]
        [InlineData("middleware", "sessions/*")]
        public void InvalidateCacheRequest_WithValidData_ShouldPassValidation(string service, string? pattern)
        {
            // Arrange
            var request = new InvalidateCacheRequest
            {
                Service = service,
                Pattern = pattern
            };

            // Act
            var validationResults = ValidateModel(request);

            // Assert
            validationResults.Should().BeEmpty();
        }

        [Fact]
        public void InvalidateCacheRequest_WithMinimumLengthService_ShouldPassValidation()
        {
            // Arrange
            var request = new InvalidateCacheRequest
            {
                Service = "ab" // Minimum 2 characters
            };

            // Act
            var validationResults = ValidateModel(request);

            // Assert
            validationResults.Should().BeEmpty();
        }

        [Fact]
        public void InvalidateCacheRequest_WithMaximumLengthService_ShouldPassValidation()
        {
            // Arrange
            var request = new InvalidateCacheRequest
            {
                Service = new string('a', 50) // Maximum 50 characters
            };

            // Act
            var validationResults = ValidateModel(request);

            // Assert
            validationResults.Should().BeEmpty();
        }

        #endregion

        #region Service Validation Tests

        [Fact]
        public void Service_WhenNull_ShouldFailValidation()
        {
            // Arrange
            var request = new InvalidateCacheRequest
            {
                Service = null!
            };

            // Act
            var validationResults = ValidateModel(request);

            // Assert
            validationResults.Should().NotBeEmpty();
            validationResults.Should().Contain(r => r.ErrorMessage!.Contains("Service is required"));
        }

        [Fact]
        public void Service_WhenEmpty_ShouldFailValidation()
        {
            // Arrange
            var request = new InvalidateCacheRequest
            {
                Service = ""
            };

            // Act
            var validationResults = ValidateModel(request);

            // Assert
            validationResults.Should().NotBeEmpty();
            validationResults.Should().Contain(r => r.ErrorMessage!.Contains("Service is required"));
        }

        [Fact]
        public void Service_WhenTooShort_ShouldFailValidation()
        {
            // Arrange
            var request = new InvalidateCacheRequest
            {
                Service = "a" // Only 1 character, minimum is 2
            };

            // Act
            var validationResults = ValidateModel(request);

            // Assert
            validationResults.Should().NotBeEmpty();
            validationResults.Should().Contain(r => r.ErrorMessage!.Contains("between 2 and 50"));
        }

        [Fact]
        public void Service_WhenTooLong_ShouldFailValidation()
        {
            // Arrange
            var request = new InvalidateCacheRequest
            {
                Service = new string('a', 51) // 51 characters, maximum is 50
            };

            // Act
            var validationResults = ValidateModel(request);

            // Assert
            validationResults.Should().NotBeEmpty();
            validationResults.Should().Contain(r => r.ErrorMessage!.Contains("between 2 and 50"));
        }

        [Theory]
        [InlineData("1users")] // Starts with number
        [InlineData("-users")] // Starts with hyphen
        [InlineData("_users")] // Starts with underscore
        [InlineData(" users")] // Starts with space
        public void Service_WhenNotStartingWithLetter_ShouldFailValidation(string service)
        {
            // Arrange
            var request = new InvalidateCacheRequest
            {
                Service = service
            };

            // Act
            var validationResults = ValidateModel(request);

            // Assert
            validationResults.Should().NotBeEmpty();
            validationResults.Should().Contain(r => r.ErrorMessage!.Contains("must start with a letter"));
        }

        [Theory]
        [InlineData("users@service")]
        [InlineData("users.service")]
        [InlineData("users service")]
        [InlineData("users/service")]
        [InlineData("users*service")]
        [InlineData("users#service")]
        public void Service_WithInvalidCharacters_ShouldFailValidation(string service)
        {
            // Arrange
            var request = new InvalidateCacheRequest
            {
                Service = service
            };

            // Act
            var validationResults = ValidateModel(request);

            // Assert
            validationResults.Should().NotBeEmpty();
            validationResults.Should().Contain(r => r.ErrorMessage!.Contains("alphanumeric characters"));
        }

        [Theory]
        [InlineData("users")]
        [InlineData("Users")]
        [InlineData("USERS")]
        [InlineData("users123")]
        [InlineData("users-service")]
        [InlineData("users_service")]
        [InlineData("users-service_123")]
        [InlineData("myServiceName")]
        [InlineData("my-service-name")]
        [InlineData("my_service_name")]
        public void Service_WithValidFormat_ShouldPassValidation(string service)
        {
            // Arrange
            var request = new InvalidateCacheRequest
            {
                Service = service
            };

            // Act
            var validationResults = ValidateModel(request);

            // Assert
            validationResults.Should().BeEmpty();
        }

        #endregion

        #region Pattern Validation Tests

        [Fact]
        public void Pattern_WhenNull_ShouldPassValidation()
        {
            // Arrange
            var request = new InvalidateCacheRequest
            {
                Service = "users",
                Pattern = null
            };

            // Act
            var validationResults = ValidateModel(request);

            // Assert
            validationResults.Should().BeEmpty();
        }

        [Fact]
        public void Pattern_WhenEmpty_ShouldPassValidation()
        {
            // Arrange
            var request = new InvalidateCacheRequest
            {
                Service = "users",
                Pattern = ""
            };

            // Act
            var validationResults = ValidateModel(request);

            // Assert
            validationResults.Should().BeEmpty();
        }

        [Fact]
        public void Pattern_WhenExceedsMaxLength_ShouldFailValidation()
        {
            // Arrange
            var request = new InvalidateCacheRequest
            {
                Service = "users",
                Pattern = new string('a', 201) // 201 characters, maximum is 200
            };

            // Act
            var validationResults = ValidateModel(request);

            // Assert
            validationResults.Should().NotBeEmpty();
            validationResults.Should().Contain(r => r.ErrorMessage!.Contains("cannot exceed 200"));
        }

        [Theory]
        [InlineData("api/*")]
        [InlineData("api/users/*")]
        [InlineData("api/*/details")]
        [InlineData("users/123")]
        [InlineData("users-service/api")]
        [InlineData("users_service_123")]
        [InlineData("*")]
        [InlineData("**")]
        [InlineData("***")]
        public void Pattern_WithValidFormat_ShouldPassValidation(string pattern)
        {
            // Arrange
            var request = new InvalidateCacheRequest
            {
                Service = "users",
                Pattern = pattern
            };

            // Act
            var validationResults = ValidateModel(request);

            // Assert
            validationResults.Should().BeEmpty();
        }

        [Theory]
        [InlineData("api @service")]
        [InlineData("api#users")]
        [InlineData("api.users")]
        [InlineData("api users")]
        [InlineData("api!users")]
        [InlineData("api$users")]
        public void Pattern_WithInvalidCharacters_ShouldFailValidation(string pattern)
        {
            // Arrange
            var request = new InvalidateCacheRequest
            {
                Service = "users",
                Pattern = pattern
            };

            // Act
            var validationResults = ValidateModel(request);

            // Assert
            validationResults.Should().NotBeEmpty();
            validationResults.Should().Contain(r => r.ErrorMessage!.Contains("alphanumeric characters"));
        }

        [Fact]
        public void Pattern_AtMaxLength_ShouldPassValidation()
        {
            // Arrange
            var request = new InvalidateCacheRequest
            {
                Service = "users",
                Pattern = new string('a', 200) // Exactly 200 characters
            };

            // Act
            var validationResults = ValidateModel(request);

            // Assert
            validationResults.Should().BeEmpty();
        }

        #endregion

        #region Property Tests

        [Fact]
        public void Service_Property_ShouldBeSettable()
        {
            // Arrange
            var request = new InvalidateCacheRequest();
            var expectedService = "users";

            // Act
            request.Service = expectedService;

            // Assert
            request.Service.Should().Be(expectedService);
        }

        [Fact]
        public void Pattern_Property_ShouldBeSettable()
        {
            // Arrange
            var request = new InvalidateCacheRequest();
            var expectedPattern = "api/*";

            // Act
            request.Pattern = expectedPattern;

            // Assert
            request.Pattern.Should().Be(expectedPattern);
        }

        [Fact]
        public void Service_DefaultValue_ShouldBeEmptyString()
        {
            // Arrange & Act
            var request = new InvalidateCacheRequest();

            // Assert
            request.Service.Should().Be(string.Empty);
        }

        [Fact]
        public void Pattern_DefaultValue_ShouldBeNull()
        {
            // Arrange & Act
            var request = new InvalidateCacheRequest();

            // Assert
            request.Pattern.Should().BeNull();
        }

        #endregion

        #region Combined Validation Tests

        [Fact]
        public void InvalidateCacheRequest_WithBothFieldsValid_ShouldPassValidation()
        {
            // Arrange
            var request = new InvalidateCacheRequest
            {
                Service = "users",
                Pattern = "api/users/*"
            };

            // Act
            var validationResults = ValidateModel(request);

            // Assert
            validationResults.Should().BeEmpty();
        }

        [Fact]
        public void InvalidateCacheRequest_WithBothFieldsInvalid_ShouldFailBothValidations()
        {
            // Arrange
            var request = new InvalidateCacheRequest
            {
                Service = "1", // Too short and starts with number
                Pattern = new string('a', 201) // Too long
            };

            // Act
            var validationResults = ValidateModel(request);

            // Assert
            validationResults.Should().HaveCountGreaterThan(1);
            validationResults.Should().Contain(r => r.MemberNames.Contains(nameof(InvalidateCacheRequest.Service)));
            validationResults.Should().Contain(r => r.MemberNames.Contains(nameof(InvalidateCacheRequest.Pattern)));
        }

        #endregion

        #region Edge Cases

        [Theory]
        [InlineData("ab", "a")] // Minimum service length
        [InlineData("a" + "b", "")] // Service with concatenation
        public void InvalidateCacheRequest_WithEdgeCaseValues_ShouldValidateCorrectly(string service, string pattern)
        {
            // Arrange
            var request = new InvalidateCacheRequest
            {
                Service = service,
                Pattern = string.IsNullOrEmpty(pattern) ? null : pattern
            };

            // Act
            var validationResults = ValidateModel(request);

            // Assert
            validationResults.Should().BeEmpty();
        }

        [Fact]
        public void InvalidateCacheRequest_WithWhitespaceInService_ShouldFailValidation()
        {
            // Arrange
            var request = new InvalidateCacheRequest
            {
                Service = "   " // Only whitespace
            };

            // Act
            var validationResults = ValidateModel(request);

            // Assert
            validationResults.Should().NotBeEmpty();
        }

        #endregion

        #region Real-World Scenarios

        [Theory]
        [InlineData("users", "api/users/*", "Invalidate all user endpoints")]
        [InlineData("reports", "api/reports/monthly/*", "Invalidate monthly reports")]
        [InlineData("analysis", "*", "Invalidate all analysis cache")]
        [InlineData("middleware", null, "Invalidate entire middleware cache")]
        public void InvalidateCacheRequest_WithRealWorldData_ShouldValidateCorrectly(
            string service, string? pattern, string scenario)
        {
            // Arrange
            var request = new InvalidateCacheRequest
            {
                Service = service,
                Pattern = pattern
            };

            // Act
            var validationResults = ValidateModel(request);

            // Assert
            validationResults.Should().BeEmpty($"scenario '{scenario}' should be valid");
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Validates a model and returns validation results
        /// </summary>
        private static List<ValidationResult> ValidateModel(object model)
        {
            var validationResults = new List<ValidationResult>();
            var validationContext = new ValidationContext(model);
            Validator.TryValidateObject(model, validationContext, validationResults, true);
            return validationResults;
        }

        #endregion
    }
}
