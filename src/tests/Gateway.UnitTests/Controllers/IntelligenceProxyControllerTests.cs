using Xunit;
using FluentAssertions;
using System.Reflection;
using Gateway.Controllers;
using Microsoft.AspNetCore.Mvc;
using Gateway.Models.Swagger.Intelligence;
using Swashbuckle.AspNetCore.Annotations;

namespace Gateway.UnitTests.Controllers
{
    /// <summary>
    /// Tests para IntelligenceProxyController - Controller de documentación Swagger
    /// Este controller es un PROXY para documentación - NO implementa lógica real.
    /// Target: >70% coverage (4 endpoints de IA)
    /// </summary>
    public class IntelligenceProxyControllerTests
    {
        private readonly IntelligenceProxyController _controller;

        public IntelligenceProxyControllerTests()
        {
            _controller = new IntelligenceProxyController();
        }

        #region Controller Attributes Tests

        [Fact]
        public void Controller_ShouldHaveCorrectAttributes()
        {
            // Arrange
            var type = typeof(IntelligenceProxyController);

            // Act
            var apiControllerAttr = type.GetCustomAttribute<ApiControllerAttribute>();
            var routeAttr = type.GetCustomAttribute<RouteAttribute>();
            var producesAttr = type.GetCustomAttribute<ProducesAttribute>();
            var apiExplorerAttr = type.GetCustomAttribute<ApiExplorerSettingsAttribute>();

            // Assert
            apiControllerAttr.Should().NotBeNull("controller debe tener atributo ApiController");
            routeAttr.Should().NotBeNull("controller debe tener ruta definida");
            routeAttr!.Template.Should().Be("api/v1/AIRecommendations", "ruta debe coincidir con Intelligence API");
            producesAttr.Should().NotBeNull("controller debe producir JSON");
            producesAttr!.ContentTypes.Should().Contain("application/json");
            apiExplorerAttr.Should().NotBeNull("controller debe estar en grupo de Swagger");
            apiExplorerAttr!.GroupName.Should().Be("intelligence", "debe estar en grupo 'intelligence'");
        }

        [Fact]
        public void Controller_ShouldHaveSwaggerTagAttribute()
        {
            // Arrange
            var type = typeof(IntelligenceProxyController);

            // Act
            var swaggerTagAttr = type.GetCustomAttribute<SwaggerTagAttribute>();

            // Assert
            swaggerTagAttr.Should().NotBeNull("controller debe tener descripción Swagger");
            swaggerTagAttr!.Description.Should().Contain("inteligencia artificial",
                "descripción debe mencionar IA");
        }

        #endregion

        #region GenerateRecommendations Tests

        [Fact]
        public void GenerateRecommendations_ShouldHaveCorrectAttributes()
        {
            // Arrange
            var method = typeof(IntelligenceProxyController)
                .GetMethod(nameof(IntelligenceProxyController.GenerateRecommendations));

            // Act
            var httpPostAttr = method!.GetCustomAttribute<HttpPostAttribute>();
            var swaggerOperationAttr = method!.GetCustomAttribute<SwaggerOperationAttribute>();
            var producesResponseTypes = method!.GetCustomAttributes<ProducesResponseTypeAttribute>();

            // Assert
            httpPostAttr.Should().NotBeNull("debe ser endpoint POST");
            httpPostAttr!.Template.Should().Be("generate");

            swaggerOperationAttr.Should().NotBeNull("debe tener documentación Swagger");
            swaggerOperationAttr!.OperationId.Should().Be("GenerateAIRecommendations");
            swaggerOperationAttr.Summary.Should().Contain("recomendaciones");

            producesResponseTypes.Should().HaveCountGreaterOrEqualTo(4,
                "debe documentar respuestas 200, 400, 401, 500");
        }

        [Fact]
        public void GenerateRecommendations_ShouldThrowNotImplementedException()
        {
            // Arrange
            var request = new AIRecommendationRequest
            {
                AnalysisData = new AnalysisDataDto
                {
                    AnalysisId = "test-123",
                    Url = "https://test.com",
                    Results = "{}"
                },
                UserLevel = "intermediate"
            };

            // Act
            Action act = () => _controller.GenerateRecommendations(request);

            // Assert
            act.Should().Throw<NotImplementedException>()
                .WithMessage("*proxy*", "debe indicar que es manejado por proxy");
        }

        #endregion

        #region GetCachedRecommendations Tests

        [Fact]
        public void GetCachedRecommendations_ShouldHaveCorrectAttributes()
        {
            // Arrange
            var method = typeof(IntelligenceProxyController)
                .GetMethod(nameof(IntelligenceProxyController.GetCachedRecommendations));

            // Act
            var httpGetAttr = method!.GetCustomAttribute<HttpGetAttribute>();
            var swaggerOperationAttr = method!.GetCustomAttribute<SwaggerOperationAttribute>();
            var producesResponseTypes = method!.GetCustomAttributes<ProducesResponseTypeAttribute>();

            // Assert
            httpGetAttr.Should().NotBeNull("debe ser endpoint GET");
            httpGetAttr!.Template.Should().Be("analysis/{analysisId}");

            swaggerOperationAttr.Should().NotBeNull("debe tener documentación Swagger");
            swaggerOperationAttr!.OperationId.Should().Be("GetCachedRecommendations");
            swaggerOperationAttr.Summary.Should().Contain("cache", "debe mencionar caché");

            producesResponseTypes.Should().HaveCountGreaterOrEqualTo(3,
                "debe documentar respuestas 200, 404, 401");
        }

        [Theory]
        [InlineData("analysis-123")]
        [InlineData("abc-456")]
        [InlineData("test-id")]
        public void GetCachedRecommendations_ShouldThrowNotImplementedException(string analysisId)
        {
            // Act
            Action act = () => _controller.GetCachedRecommendations(analysisId);

            // Assert
            act.Should().Throw<NotImplementedException>()
                .WithMessage("*proxy*", "debe indicar que es manejado por proxy");
        }

        #endregion

        #region GenerateSingleRecommendation Tests

        [Fact]
        public void GenerateSingleRecommendation_ShouldHaveCorrectAttributes()
        {
            // Arrange
            var method = typeof(IntelligenceProxyController)
                .GetMethod(nameof(IntelligenceProxyController.GenerateSingleRecommendation));

            // Act
            var httpPostAttr = method!.GetCustomAttribute<HttpPostAttribute>();
            var swaggerOperationAttr = method!.GetCustomAttribute<SwaggerOperationAttribute>();
            var producesResponseTypes = method!.GetCustomAttributes<ProducesResponseTypeAttribute>();

            // Assert
            httpPostAttr.Should().NotBeNull("debe ser endpoint POST");
            httpPostAttr!.Template.Should().Be("single");

            swaggerOperationAttr.Should().NotBeNull("debe tener documentación Swagger");
            swaggerOperationAttr!.OperationId.Should().Be("GenerateSingleRecommendation");
            swaggerOperationAttr.Summary.Should().Contain("error específico",
                "debe mencionar análisis de error individual");

            producesResponseTypes.Should().HaveCountGreaterOrEqualTo(4,
                "debe documentar respuestas 200, 400, 401, 500");
        }

        [Fact]
        public void GenerateSingleRecommendation_ShouldThrowNotImplementedException()
        {
            // Arrange
            var request = new SingleRecommendationRequest
            {
                Error = new ErrorInfo
                {
                    Code = "color-contrast",
                    Message = "Test error",
                    Impact = "serious"
                }
            };

            // Act
            Action act = () => _controller.GenerateSingleRecommendation(request);

            // Assert
            act.Should().Throw<NotImplementedException>()
                .WithMessage("*proxy*", "debe indicar que es manejado por proxy");
        }

        #endregion

        #region HealthCheck Tests

        [Fact]
        public void HealthCheck_ShouldHaveCorrectAttributes()
        {
            // Arrange
            var method = typeof(IntelligenceProxyController)
                .GetMethod(nameof(IntelligenceProxyController.HealthCheck));

            // Act
            var httpGetAttr = method!.GetCustomAttribute<HttpGetAttribute>();
            var swaggerOperationAttr = method!.GetCustomAttribute<SwaggerOperationAttribute>();
            var producesResponseType = method!.GetCustomAttribute<ProducesResponseTypeAttribute>();

            // Assert
            httpGetAttr.Should().NotBeNull("debe ser endpoint GET");
            httpGetAttr!.Template.Should().Be("health");

            swaggerOperationAttr.Should().NotBeNull("debe tener documentación Swagger");
            swaggerOperationAttr!.OperationId.Should().Be("IntelligenceHealthCheck");
            swaggerOperationAttr.Summary.Should().MatchRegex("(?i)health|salud",
                "debe mencionar health check o salud");

            producesResponseType.Should().NotBeNull("debe documentar respuesta 200");
            producesResponseType!.StatusCode.Should().Be(200);
        }

        [Fact]
        public void HealthCheck_ShouldThrowNotImplementedException()
        {
            // Act
            Action act = () => _controller.HealthCheck();

            // Assert
            act.Should().Throw<NotImplementedException>()
                .WithMessage("*proxy*", "debe indicar que es manejado por proxy");
        }

        #endregion

        #region All Endpoints Coverage Test

        [Fact]
        public void Controller_ShouldHaveFourPublicMethods()
        {
            // Arrange
            var type = typeof(IntelligenceProxyController);

            // Act
            var publicMethods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(m => !m.IsSpecialName); // Excluir getters/setters

            // Assert
            publicMethods.Should().HaveCount(4, "controller debe tener exactamente 4 endpoints");
            publicMethods.Select(m => m.Name).Should().BeEquivalentTo(new[]
            {
                nameof(IntelligenceProxyController.GenerateRecommendations),
                nameof(IntelligenceProxyController.GetCachedRecommendations),
                nameof(IntelligenceProxyController.GenerateSingleRecommendation),
                nameof(IntelligenceProxyController.HealthCheck)
            });
        }

        [Theory]
        [InlineData(nameof(IntelligenceProxyController.GenerateRecommendations))]
        [InlineData(nameof(IntelligenceProxyController.GetCachedRecommendations))]
        [InlineData(nameof(IntelligenceProxyController.GenerateSingleRecommendation))]
        [InlineData(nameof(IntelligenceProxyController.HealthCheck))]
        public void AllEndpoints_ShouldHaveSwaggerOperation(string methodName)
        {
            // Arrange
            var method = typeof(IntelligenceProxyController).GetMethod(methodName);

            // Act
            var swaggerOperationAttr = method!.GetCustomAttribute<SwaggerOperationAttribute>();

            // Assert
            swaggerOperationAttr.Should().NotBeNull($"{methodName} debe tener SwaggerOperation");
            swaggerOperationAttr!.Tags.Should().Contain("AI RECOMMENDATIONS",
                "todos los endpoints deben estar en tag AI RECOMMENDATIONS");
        }

        #endregion

        #region Method Signature Tests

        [Fact]
        public void GenerateRecommendations_ShouldAcceptCorrectParameterType()
        {
            // Arrange
            var method = typeof(IntelligenceProxyController)
                .GetMethod(nameof(IntelligenceProxyController.GenerateRecommendations));

            // Act
            var parameters = method!.GetParameters();

            // Assert
            parameters.Should().HaveCount(1, "debe tener un parámetro");
            parameters[0].ParameterType.Should().Be(typeof(AIRecommendationRequest));
            parameters[0].GetCustomAttribute<FromBodyAttribute>().Should().NotBeNull(
                "parámetro debe venir del body");
        }

        [Fact]
        public void GetCachedRecommendations_ShouldAcceptCorrectParameterType()
        {
            // Arrange
            var method = typeof(IntelligenceProxyController)
                .GetMethod(nameof(IntelligenceProxyController.GetCachedRecommendations));

            // Act
            var parameters = method!.GetParameters();

            // Assert
            parameters.Should().HaveCount(1, "debe tener un parámetro");
            parameters[0].ParameterType.Should().Be(typeof(string));
            parameters[0].Name.Should().Be("analysisId");
            parameters[0].GetCustomAttribute<FromRouteAttribute>().Should().NotBeNull(
                "parámetro debe venir de la ruta");
        }

        [Fact]
        public void GenerateSingleRecommendation_ShouldAcceptCorrectParameterType()
        {
            // Arrange
            var method = typeof(IntelligenceProxyController)
                .GetMethod(nameof(IntelligenceProxyController.GenerateSingleRecommendation));

            // Act
            var parameters = method!.GetParameters();

            // Assert
            parameters.Should().HaveCount(1, "debe tener un parámetro");
            parameters[0].ParameterType.Should().Be(typeof(SingleRecommendationRequest));
            parameters[0].GetCustomAttribute<FromBodyAttribute>().Should().NotBeNull(
                "parámetro debe venir del body");
        }

        [Fact]
        public void HealthCheck_ShouldHaveNoParameters()
        {
            // Arrange
            var method = typeof(IntelligenceProxyController)
                .GetMethod(nameof(IntelligenceProxyController.HealthCheck));

            // Act
            var parameters = method!.GetParameters();

            // Assert
            parameters.Should().BeEmpty("health check no debe tener parámetros");
        }

        #endregion

        #region Return Type Tests

        [Theory]
        [InlineData(nameof(IntelligenceProxyController.GenerateRecommendations))]
        [InlineData(nameof(IntelligenceProxyController.GetCachedRecommendations))]
        [InlineData(nameof(IntelligenceProxyController.GenerateSingleRecommendation))]
        [InlineData(nameof(IntelligenceProxyController.HealthCheck))]
        public void AllEndpoints_ShouldReturnIActionResult(string methodName)
        {
            // Arrange
            var method = typeof(IntelligenceProxyController).GetMethod(methodName);

            // Act
            var returnType = method!.ReturnType;

            // Assert
            returnType.Should().Be(typeof(IActionResult),
                $"{methodName} debe retornar IActionResult");
        }

        #endregion
    }
}
