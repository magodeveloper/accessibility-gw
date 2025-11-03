using Xunit;
using System;
using FluentAssertions;
using Gateway.Services;

#pragma warning disable CS8600, CS8602, CS8604, CS8625

namespace Gateway.UnitTests.Services
{
    /// <summary>
    /// Tests para SecurityException - Excepción personalizada para errores de seguridad
    /// Target: 100% branch coverage
    /// </summary>
    public class SecurityExceptionTests
    {
        #region Constructor Tests

        [Fact]
        public void Constructor_WithMessage_ShouldCreateException()
        {
            // Arrange
            var message = "Test security error message";

            // Act
            var exception = new SecurityException(message);

            // Assert
            exception.Should().NotBeNull();
            exception.Message.Should().Be(message);
        }

        [Fact]
        public void Constructor_WithMessageAndInnerException_ShouldCreateException()
        {
            // Arrange
            var message = "Security violation occurred";
            var innerException = new InvalidOperationException("Inner exception message");

            // Act
            var exception = new SecurityException(message, innerException);

            // Assert
            exception.Should().NotBeNull();
            exception.Message.Should().Be(message);
            exception.InnerException.Should().Be(innerException);
            exception.InnerException.Message.Should().Be("Inner exception message");
        }

        [Fact]
        public void Constructor_WithEmptyMessage_ShouldCreateException()
        {
            // Arrange
            var message = "";

            // Act
            var exception = new SecurityException(message);

            // Assert
            exception.Should().NotBeNull();
            exception.Message.Should().Be(message);
        }

        [Fact]
        public void Constructor_WithNullMessage_ShouldCreateException()
        {
            // Arrange
            string message = null;

            // Act
            var exception = new SecurityException(message);

            // Assert
            exception.Should().NotBeNull();
        }

        [Fact]
        public void Constructor_WithNullInnerException_ShouldCreateException()
        {
            // Arrange
            var message = "Security error";
            Exception innerException = null;

            // Act
            var exception = new SecurityException(message, innerException);

            // Assert
            exception.Should().NotBeNull();
            exception.Message.Should().Be(message);
            exception.InnerException.Should().BeNull();
        }

        #endregion

        #region Inheritance Tests

        [Fact]
        public void SecurityException_ShouldInheritFromException()
        {
            // Arrange & Act
            var exception = new SecurityException("Test message");

            // Assert
            exception.Should().BeAssignableTo<Exception>();
        }

        [Fact]
        public void SecurityException_ShouldBeThrowable()
        {
            // Arrange
            var message = "Security violation";

            // Act
            Action act = () => throw new SecurityException(message);

            // Assert
            act.Should().Throw<SecurityException>()
                .WithMessage(message);
        }

        [Fact]
        public void SecurityException_ShouldBeCatchableAsException()
        {
            // Arrange
            var message = "Security error";

            // Act & Assert
            try
            {
                throw new SecurityException(message);
            }
            catch (Exception ex)
            {
                ex.Should().BeOfType<SecurityException>();
                ex.Message.Should().Be(message);
            }
        }

        #endregion

        #region Message Tests

        [Theory]
        [InlineData("SQL Injection attempt detected")]
        [InlineData("XSS attack prevented")]
        [InlineData("Path traversal detected")]
        [InlineData("Invalid input sanitization")]
        [InlineData("Security validation failed")]
        public void Constructor_WithVariousMessages_ShouldStoreMessage(string message)
        {
            // Act
            var exception = new SecurityException(message);

            // Assert
            exception.Message.Should().Be(message);
        }

        [Fact]
        public void Constructor_WithLongMessage_ShouldStoreCompleteMessage()
        {
            // Arrange
            var message = new string('a', 10000);

            // Act
            var exception = new SecurityException(message);

            // Assert
            exception.Message.Should().Be(message);
            exception.Message.Length.Should().Be(10000);
        }

        [Fact]
        public void Constructor_WithSpecialCharactersInMessage_ShouldStoreMessage()
        {
            // Arrange
            var message = "Security error: <script>alert('xss')</script> detected!";

            // Act
            var exception = new SecurityException(message);

            // Assert
            exception.Message.Should().Be(message);
        }

        [Fact]
        public void Constructor_WithMultilineMessage_ShouldStoreMessage()
        {
            // Arrange
            var message = "Security error:\nLine 1\nLine 2\nLine 3";

            // Act
            var exception = new SecurityException(message);

            // Assert
            exception.Message.Should().Be(message);
        }

        #endregion

        #region Inner Exception Tests

        [Fact]
        public void Constructor_WithInnerException_ShouldPreserveInnerException()
        {
            // Arrange
            var innerMessage = "Original error";
            var innerException = new ArgumentException(innerMessage);
            var message = "Security wrapper error";

            // Act
            var exception = new SecurityException(message, innerException);

            // Assert
            exception.InnerException.Should().BeOfType<ArgumentException>();
            exception.InnerException.Message.Should().Contain(innerMessage);
        }

        [Fact]
        public void Constructor_WithNestedInnerExceptions_ShouldPreserveChain()
        {
            // Arrange
            var deepestException = new InvalidOperationException("Deepest error");
            var middleException = new ArgumentException("Middle error", deepestException);
            var message = "Security error";

            // Act
            var exception = new SecurityException(message, middleException);

            // Assert
            exception.InnerException.Should().BeOfType<ArgumentException>();
            exception.InnerException.InnerException.Should().BeOfType<InvalidOperationException>();
            exception.InnerException.InnerException.Message.Should().Be("Deepest error");
        }

        [Theory]
        [InlineData(typeof(ArgumentNullException))]
        [InlineData(typeof(InvalidOperationException))]
        [InlineData(typeof(FormatException))]
        [InlineData(typeof(NotSupportedException))]
        public void Constructor_WithDifferentInnerExceptionTypes_ShouldPreserveType(Type exceptionType)
        {
            // Arrange
            var innerException = (Exception)Activator.CreateInstance(exceptionType, "Inner message");
            var message = "Security error";

            // Act
            var exception = new SecurityException(message, innerException);

            // Assert
            exception.InnerException.Should().BeOfType(exceptionType);
        }

        #endregion

        #region Exception Properties Tests

        [Fact]
        public void Exception_StackTrace_ShouldBePopulated()
        {
            // Arrange & Act
            SecurityException exception = null;
            try
            {
                throw new SecurityException("Test exception");
            }
            catch (SecurityException ex)
            {
                exception = ex;
            }

            // Assert
            exception.Should().NotBeNull();
            exception.StackTrace.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public void Exception_Source_CanBeSet()
        {
            // Arrange
            var exception = new SecurityException("Test message");

            // Act
            exception.Source = "TestSource";

            // Assert
            exception.Source.Should().Be("TestSource");
        }

        [Fact]
        public void Exception_HelpLink_CanBeSet()
        {
            // Arrange
            var exception = new SecurityException("Test message");
            var helpLink = "https://docs.example.com/security-errors";

            // Act
            exception.HelpLink = helpLink;

            // Assert
            exception.HelpLink.Should().Be(helpLink);
        }

        [Fact]
        public void Exception_Data_CanStoreAdditionalInformation()
        {
            // Arrange
            var exception = new SecurityException("Test message");

            // Act
            exception.Data["ErrorCode"] = "SEC001";
            exception.Data["Severity"] = "High";
            exception.Data["Timestamp"] = DateTime.UtcNow;

            // Assert
            exception.Data["ErrorCode"].Should().Be("SEC001");
            exception.Data["Severity"].Should().Be("High");
            exception.Data["Timestamp"].Should().BeOfType<DateTime>();
        }

        #endregion

        #region Exception Throwing and Catching Tests

        [Fact]
        public void ThrowSecurityException_ShouldBeCatchable()
        {
            // Arrange
            var message = "Security violation";
            var exceptionCaught = false;

            // Act
            try
            {
                throw new SecurityException(message);
            }
            catch (SecurityException ex)
            {
                exceptionCaught = true;
                ex.Message.Should().Be(message);
            }

            // Assert
            exceptionCaught.Should().BeTrue();
        }

        [Fact]
        public void ThrowSecurityException_WithInnerException_ShouldPreserveInnerException()
        {
            // Arrange
            var innerException = new ArgumentException("Inner");
            var message = "Outer";
            SecurityException caughtException = null;

            // Act
            try
            {
                throw new SecurityException(message, innerException);
            }
            catch (SecurityException ex)
            {
                caughtException = ex;
            }

            // Assert
            caughtException.Should().NotBeNull();
            caughtException.InnerException.Should().Be(innerException);
        }

        [Fact]
        public void SecurityException_InTryCatchFinally_ShouldExecuteFinally()
        {
            // Arrange
            var finallyExecuted = false;

            // Act
            try
            {
                throw new SecurityException("Test");
            }
            catch (SecurityException)
            {
                // Caught
            }
            finally
            {
                finallyExecuted = true;
            }

            // Assert
            finallyExecuted.Should().BeTrue();
        }

        #endregion

        #region Real-World Scenario Tests

        [Fact]
        public void SecurityException_FromSanitizationFailure_ShouldContainDetailedMessage()
        {
            // Arrange
            var detectedPattern = "<script>alert('xss')</script>";
            var message = $"XSS attempt detected: {detectedPattern}";

            // Act
            var exception = new SecurityException(message);

            // Assert
            exception.Message.Should().Contain("XSS");
            exception.Message.Should().Contain(detectedPattern);
        }

        [Fact]
        public void SecurityException_FromValidationFailure_ShouldWrapOriginalException()
        {
            // Arrange
            var originalException = new FormatException("Invalid input format");
            var message = "Security validation failed";

            // Act
            var exception = new SecurityException(message, originalException);

            // Assert
            exception.Message.Should().Be(message);
            exception.InnerException.Should().BeOfType<FormatException>();
        }

        [Fact]
        public void SecurityException_InLoggingScenario_ShouldBeSerializable()
        {
            // Arrange
            var message = "Security error for logging";
            var exception = new SecurityException(message);

            // Act - Simulamos serialización para logging
            var serialized = $"{exception.GetType().Name}: {exception.Message}";

            // Assert
            serialized.Should().Contain("SecurityException");
            serialized.Should().Contain(message);
        }

        [Fact]
        public void SecurityException_InNestedTryCatch_ShouldBeCatchableAtDifferentLevels()
        {
            // Arrange
            var innerExceptionCaught = false;
            var outerExceptionCaught = false;

            // Act
            try
            {
                try
                {
                    throw new SecurityException("Inner security error");
                }
                catch (SecurityException)
                {
                    innerExceptionCaught = true;
                    throw new SecurityException("Outer security error");
                }
            }
            catch (SecurityException)
            {
                outerExceptionCaught = true;
            }

            // Assert
            innerExceptionCaught.Should().BeTrue();
            outerExceptionCaught.Should().BeTrue();
        }

        #endregion

        #region Edge Cases

        [Fact]
        public void SecurityException_WithUnicodeMessage_ShouldPreserveUnicode()
        {
            // Arrange
            var message = "Security error: 测试消息 🔒 Тест сообщение";

            // Act
            var exception = new SecurityException(message);

            // Assert
            exception.Message.Should().Be(message);
        }

        [Fact]
        public void SecurityException_ToString_ShouldIncludeTypeAndMessage()
        {
            // Arrange
            var message = "Test security error";
            var exception = new SecurityException(message);

            // Act
            var result = exception.ToString();

            // Assert
            result.Should().Contain("SecurityException");
            result.Should().Contain(message);
        }

        [Fact]
        public void SecurityException_GetBaseException_WithInnerException_ShouldReturnDeepestException()
        {
            // Arrange
            var deepestException = new InvalidOperationException("Deepest");
            var middleException = new ArgumentException("Middle", deepestException);
            var exception = new SecurityException("Outer", middleException);

            // Act
            var baseException = exception.GetBaseException();

            // Assert
            baseException.Should().BeOfType<InvalidOperationException>();
            baseException.Message.Should().Be("Deepest");
        }

        [Fact]
        public void SecurityException_GetBaseException_WithoutInnerException_ShouldReturnSelf()
        {
            // Arrange
            var exception = new SecurityException("Test");

            // Act
            var baseException = exception.GetBaseException();

            // Assert
            baseException.Should().Be(exception);
        }

        #endregion
    }
}
