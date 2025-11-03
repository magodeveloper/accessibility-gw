using Xunit;
using FluentAssertions;
using System.Reflection;
using Gateway.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace Gateway.UnitTests.Controllers
{
    /// <summary>
    /// Tests para MiddlewareProxyController - Controller de documentación Swagger
    /// Este controller es un PROXY para documentación - NO implementa lógica real.
    /// YARP maneja las peticiones reales.
    /// Target: >70% coverage
    /// </summary>
    public class MiddlewareProxyControllerTests
    {
        private readonly MiddlewareProxyController _controller;

        public MiddlewareProxyControllerTests()
        {
            _controller = new MiddlewareProxyController();
        }

        #region Controller Attributes Tests

        [Fact]
        public void Controller_ShouldHave_ApiControllerAttribute()
        {
            // Arrange & Act
            var attribute = typeof(MiddlewareProxyController)
                .GetCustomAttribute<ApiControllerAttribute>();

            // Assert
            attribute.Should().NotBeNull("el controller debe tener [ApiController]");
        }

        [Fact]
        public void Controller_ShouldHave_RouteAttribute_WithCorrectPath()
        {
            // Arrange & Act
            var attribute = typeof(MiddlewareProxyController)
                .GetCustomAttribute<RouteAttribute>();

            // Assert
            attribute.Should().NotBeNull();
            attribute!.Template.Should().Be("api");
        }

        [Fact]
        public void Controller_ShouldHave_ProducesAttribute_WithJsonContentType()
        {
            // Arrange & Act
            var attribute = typeof(MiddlewareProxyController)
                .GetCustomAttribute<ProducesAttribute>();

            // Assert
            attribute.Should().NotBeNull();
            attribute!.ContentTypes.Should().Contain("application/json");
        }

        [Fact]
        public void Controller_ShouldHave_ApiExplorerSettings_WithCorrectGroupName()
        {
            // Arrange & Act
            var attribute = typeof(MiddlewareProxyController)
                .GetCustomAttribute<ApiExplorerSettingsAttribute>();

            // Assert
            attribute.Should().NotBeNull();
            attribute!.GroupName.Should().Be("middleware");
            attribute.IgnoreApi.Should().BeFalse();
        }

        #endregion

        #region Middleware Endpoints Tests

        [Fact]
        public void AnalyzeUrl_ShouldThrow_NotImplementedException()
        {
            // Act
            Action act = () => _controller.AnalyzeUrl(null!);

            // Assert
            act.Should().Throw<NotImplementedException>()
                .WithMessage("*Este endpoint es manejado por el reverse proxy YARP*");
        }

        [Fact]
        public void AnalyzeUrl_ShouldAccept_RequestObject()
        {
            // Arrange
            var method = typeof(MiddlewareProxyController).GetMethod(nameof(MiddlewareProxyController.AnalyzeUrl));

            // Assert
            method.Should().NotBeNull();
            method!.GetParameters().Should().HaveCount(1);
        }

        [Fact]
        public void AnalyzeUrl_ShouldHave_HttpPostAttribute()
        {
            // Arrange
            var method = typeof(MiddlewareProxyController).GetMethod(nameof(MiddlewareProxyController.AnalyzeUrl));

            // Act
            var attribute = method!.GetCustomAttribute<HttpPostAttribute>();

            // Assert
            attribute.Should().NotBeNull();
            attribute!.Template.Should().Be("analyze");
        }

        #endregion

        #region Health and Metrics Tests

        [Fact]
        public void GetHealth_ShouldThrow_NotImplementedException()
        {
            // Act
            Action act = () => _controller.GetHealth();

            // Assert
            act.Should().Throw<NotImplementedException>();
        }

        [Fact]
        public void GetLiveness_ShouldThrow_NotImplementedException()
        {
            // Act
            Action act = () => _controller.GetLiveness();

            // Assert
            act.Should().Throw<NotImplementedException>();
        }

        [Fact]
        public void GetReadiness_ShouldThrow_NotImplementedException()
        {
            // Act
            Action act = () => _controller.GetReadiness();

            // Assert
            act.Should().Throw<NotImplementedException>();
        }

        [Fact]
        public void GetMetrics_ShouldThrow_NotImplementedException()
        {
            // Act
            Action act = () => _controller.GetMetrics();

            // Assert
            act.Should().Throw<NotImplementedException>();
        }

        [Fact]
        public void GetHealth_ShouldHave_HttpGetAttribute()
        {
            // Arrange
            var method = typeof(MiddlewareProxyController).GetMethod(nameof(MiddlewareProxyController.GetHealth));

            // Act
            var attribute = method!.GetCustomAttribute<HttpGetAttribute>();

            // Assert
            attribute.Should().NotBeNull();
            attribute!.Template.Should().Be("/middleware-service/health");
        }

        [Fact]
        public void GetLiveness_ShouldHave_HttpGetAttribute()
        {
            // Arrange
            var method = typeof(MiddlewareProxyController).GetMethod(nameof(MiddlewareProxyController.GetLiveness));

            // Act
            var attribute = method!.GetCustomAttribute<HttpGetAttribute>();

            // Assert
            attribute.Should().NotBeNull();
            attribute!.Template.Should().Be("/middleware-service/health/live");
        }

        [Fact]
        public void GetReadiness_ShouldHave_HttpGetAttribute()
        {
            // Arrange
            var method = typeof(MiddlewareProxyController).GetMethod(nameof(MiddlewareProxyController.GetReadiness));

            // Act
            var attribute = method!.GetCustomAttribute<HttpGetAttribute>();

            // Assert
            attribute.Should().NotBeNull();
            attribute!.Template.Should().Be("/middleware-service/health/ready");
        }

        [Fact]
        public void GetMetrics_ShouldHave_HttpGetAttribute()
        {
            // Arrange
            var method = typeof(MiddlewareProxyController).GetMethod(nameof(MiddlewareProxyController.GetMetrics));

            // Act
            var attribute = method!.GetCustomAttribute<HttpGetAttribute>();

            // Assert
            attribute.Should().NotBeNull();
            attribute!.Template.Should().Be("/middleware-service/metrics");
        }

        #endregion

        #region Method Signature Tests

        [Fact]
        public void AnalyzeUrl_ShouldReturn_IActionResult()
        {
            // Arrange
            var method = typeof(MiddlewareProxyController).GetMethod(nameof(MiddlewareProxyController.AnalyzeUrl));

            // Assert
            method.Should().NotBeNull();
            method!.ReturnType.Should().Be(typeof(IActionResult));
        }

        [Fact]
        public void GetHealth_ShouldReturn_IActionResult()
        {
            // Arrange
            var method = typeof(MiddlewareProxyController).GetMethod(nameof(MiddlewareProxyController.GetHealth));

            // Assert
            method.Should().NotBeNull();
            method!.ReturnType.Should().Be(typeof(IActionResult));
        }

        #endregion

        #region Controller Instance Tests

        [Fact]
        public void Controller_ShouldBeInstantiable()
        {
            // Act
            var controller = new MiddlewareProxyController();

            // Assert
            controller.Should().NotBeNull();
            controller.Should().BeAssignableTo<ControllerBase>();
        }

        [Fact]
        public void Controller_ShouldInheritFrom_ControllerBase()
        {
            // Assert
            typeof(MiddlewareProxyController).Should().BeDerivedFrom<ControllerBase>();
        }

        #endregion
    }
}
