using Moq;
using Xunit;
using FluentAssertions;
using System.Reflection;
using Gateway.Configuration;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace Gateway.UnitTests.Configuration
{
    /// <summary>
    /// Tests para clases de configuración Swagger
    /// Target: 70%+ coverage para configuration classes
    /// </summary>
    public class SwaggerConfigurationTests
    {
        #region CommonResponsesOperationFilter Tests

        [Fact]
        public void CommonResponsesOperationFilter_Apply_ShouldAdd401Response()
        {
            // Arrange
            var filter = new CommonResponsesOperationFilter();
            var operation = new OpenApiOperation { Responses = new OpenApiResponses() };
            var context = CreateOperationFilterContext();

            // Act
            filter.Apply(operation, context);

            // Assert
            operation.Responses.Should().ContainKey("401");
            operation.Responses["401"].Description.Should().Contain("No autorizado");
        }

        [Fact]
        public void CommonResponsesOperationFilter_Apply_ShouldAdd403Response()
        {
            // Arrange
            var filter = new CommonResponsesOperationFilter();
            var operation = new OpenApiOperation { Responses = new OpenApiResponses() };
            var context = CreateOperationFilterContext();

            // Act
            filter.Apply(operation, context);

            // Assert
            operation.Responses.Should().ContainKey("403");
            operation.Responses["403"].Description.Should().Contain("Prohibido");
        }

        [Fact]
        public void CommonResponsesOperationFilter_Apply_ShouldAdd500Response()
        {
            // Arrange
            var filter = new CommonResponsesOperationFilter();
            var operation = new OpenApiOperation { Responses = new OpenApiResponses() };
            var context = CreateOperationFilterContext();

            // Act
            filter.Apply(operation, context);

            // Assert
            operation.Responses.Should().ContainKey("500");
            operation.Responses["500"].Description.Should().Contain("Error interno");
        }

        [Fact]
        public void CommonResponsesOperationFilter_Apply_ShouldNotOverrideExisting401()
        {
            // Arrange
            var filter = new CommonResponsesOperationFilter();
            var existingDescription = "Custom 401 description";
            var operation = new OpenApiOperation
            {
                Responses = new OpenApiResponses
                {
                    { "401", new OpenApiResponse { Description = existingDescription } }
                }
            };
            var context = CreateOperationFilterContext();

            // Act
            filter.Apply(operation, context);

            // Assert
            operation.Responses["401"].Description.Should().Be(existingDescription);
        }

        [Fact]
        public void CommonResponsesOperationFilter_Apply_ShouldNotOverrideExisting403()
        {
            // Arrange
            var filter = new CommonResponsesOperationFilter();
            var existingDescription = "Custom 403 description";
            var operation = new OpenApiOperation
            {
                Responses = new OpenApiResponses
                {
                    { "403", new OpenApiResponse { Description = existingDescription } }
                }
            };
            var context = CreateOperationFilterContext();

            // Act
            filter.Apply(operation, context);

            // Assert
            operation.Responses["403"].Description.Should().Be(existingDescription);
        }

        [Fact]
        public void CommonResponsesOperationFilter_Apply_ShouldNotOverrideExisting500()
        {
            // Arrange
            var filter = new CommonResponsesOperationFilter();
            var existingDescription = "Custom 500 description";
            var operation = new OpenApiOperation
            {
                Responses = new OpenApiResponses
                {
                    { "500", new OpenApiResponse { Description = existingDescription } }
                }
            };
            var context = CreateOperationFilterContext();

            // Act
            filter.Apply(operation, context);

            // Assert
            operation.Responses["500"].Description.Should().Be(existingDescription);
        }

        [Fact]
        public void CommonResponsesOperationFilter_Apply_ShouldAddAllThreeResponses()
        {
            // Arrange
            var filter = new CommonResponsesOperationFilter();
            var operation = new OpenApiOperation { Responses = new OpenApiResponses() };
            var context = CreateOperationFilterContext();

            // Act
            filter.Apply(operation, context);

            // Assert
            operation.Responses.Should().HaveCount(3);
            operation.Responses.Keys.Should().Contain(new[] { "401", "403", "500" });
        }

        #endregion

        #region OpenApiVersionDocumentFilter Tests

        [Fact]
        public void OpenApiVersionDocumentFilter_Apply_ShouldNotThrowException()
        {
            // Arrange
            var filter = new OpenApiVersionDocumentFilter();
            var swaggerDoc = new OpenApiDocument
            {
                Info = new OpenApiInfo { Title = "Test API", Version = "v1" },
                Paths = new OpenApiPaths()
            };
            var context = CreateDocumentFilterContext();

            // Act
            Action act = () => filter.Apply(swaggerDoc, context);

            // Assert
            act.Should().NotThrow();
        }

        [Fact]
        public void OpenApiVersionDocumentFilter_Apply_WithValidDocument_ShouldNotModifyInfo()
        {
            // Arrange
            var filter = new OpenApiVersionDocumentFilter();
            var originalTitle = "Test API";
            var originalVersion = "v1";
            var swaggerDoc = new OpenApiDocument
            {
                Info = new OpenApiInfo { Title = originalTitle, Version = originalVersion },
                Paths = new OpenApiPaths()
            };
            var context = CreateDocumentFilterContext();

            // Act
            filter.Apply(swaggerDoc, context);

            // Assert
            swaggerDoc.Info.Title.Should().Be(originalTitle);
            swaggerDoc.Info.Version.Should().Be(originalVersion);
        }

        #endregion

        #region RemoveSwaggerPrefixOperationFilter Tests

        [Fact]
        public void RemoveSwaggerPrefixOperationFilter_Apply_WithNormalPath_ShouldNotThrow()
        {
            // Arrange
            var filter = new RemoveSwaggerPrefixOperationFilter();
            var operation = new OpenApiOperation { Responses = new OpenApiResponses() };
            var context = CreateOperationFilterContext("/api/users");

            // Act
            Action act = () => filter.Apply(operation, context);

            // Assert
            act.Should().NotThrow();
        }

        [Fact]
        public void RemoveSwaggerPrefixOperationFilter_Apply_WithSwaggerPrefix_ShouldNotThrow()
        {
            // Arrange
            var filter = new RemoveSwaggerPrefixOperationFilter();
            var operation = new OpenApiOperation { Responses = new OpenApiResponses() };
            var context = CreateOperationFilterContext("_swagger/api/users");

            // Act
            Action act = () => filter.Apply(operation, context);

            // Assert
            act.Should().NotThrow();
        }

        [Fact]
        public void RemoveSwaggerPrefixOperationFilter_Apply_WithNullPath_ShouldNotThrow()
        {
            // Arrange
            var filter = new RemoveSwaggerPrefixOperationFilter();
            var operation = new OpenApiOperation { Responses = new OpenApiResponses() };
            var context = CreateOperationFilterContext(null);

            // Act
            Action act = () => filter.Apply(operation, context);

            // Assert
            act.Should().NotThrow();
        }

        [Fact]
        public void RemoveSwaggerPrefixOperationFilter_Apply_WithEmptyPath_ShouldNotThrow()
        {
            // Arrange
            var filter = new RemoveSwaggerPrefixOperationFilter();
            var operation = new OpenApiOperation { Responses = new OpenApiResponses() };
            var context = CreateOperationFilterContext("");

            // Act
            Action act = () => filter.Apply(operation, context);

            // Assert
            act.Should().NotThrow();
        }

        #endregion

        #region RemoveSwaggerPrefixDocumentFilter Tests

        [Fact]
        public void RemoveSwaggerPrefixDocumentFilter_Apply_ShouldRemovePrefixFromPaths()
        {
            // Arrange
            var filter = new RemoveSwaggerPrefixDocumentFilter();
            var swaggerDoc = new OpenApiDocument
            {
                Info = new OpenApiInfo { Title = "Test API", Version = "v1" },
                Paths = new OpenApiPaths
                {
                    { "/_swagger/api/users", new OpenApiPathItem() },
                    { "/_swagger/api/reports", new OpenApiPathItem() }
                }
            };
            var context = CreateDocumentFilterContext();

            // Act
            filter.Apply(swaggerDoc, context);

            // Assert
            swaggerDoc.Paths.Should().ContainKey("/api/users");
            swaggerDoc.Paths.Should().ContainKey("/api/reports");
            swaggerDoc.Paths.Should().NotContainKey("/_swagger/api/users");
            swaggerDoc.Paths.Should().NotContainKey("/_swagger/api/reports");
        }

        [Fact]
        public void RemoveSwaggerPrefixDocumentFilter_Apply_WithNormalPaths_ShouldNotModify()
        {
            // Arrange
            var filter = new RemoveSwaggerPrefixDocumentFilter();
            var swaggerDoc = new OpenApiDocument
            {
                Info = new OpenApiInfo { Title = "Test API", Version = "v1" },
                Paths = new OpenApiPaths
                {
                    { "/api/users", new OpenApiPathItem() },
                    { "/api/reports", new OpenApiPathItem() }
                }
            };
            var context = CreateDocumentFilterContext();

            // Act
            filter.Apply(swaggerDoc, context);

            // Assert
            swaggerDoc.Paths.Should().HaveCount(2);
            swaggerDoc.Paths.Should().ContainKey("/api/users");
            swaggerDoc.Paths.Should().ContainKey("/api/reports");
        }

        [Fact]
        public void RemoveSwaggerPrefixDocumentFilter_Apply_WithMixedPaths_ShouldOnlyRemovePrefix()
        {
            // Arrange
            var filter = new RemoveSwaggerPrefixDocumentFilter();
            var swaggerDoc = new OpenApiDocument
            {
                Info = new OpenApiInfo { Title = "Test API", Version = "v1" },
                Paths = new OpenApiPaths
                {
                    { "/_swagger/api/users", new OpenApiPathItem() },
                    { "/api/analysis", new OpenApiPathItem() },
                    { "/_swagger/api/reports", new OpenApiPathItem() }
                }
            };
            var context = CreateDocumentFilterContext();

            // Act
            filter.Apply(swaggerDoc, context);

            // Assert
            swaggerDoc.Paths.Should().HaveCount(3);
            swaggerDoc.Paths.Should().ContainKey("/api/users");
            swaggerDoc.Paths.Should().ContainKey("/api/analysis");
            swaggerDoc.Paths.Should().ContainKey("/api/reports");
            swaggerDoc.Paths.Should().NotContainKey("/_swagger/api/users");
            swaggerDoc.Paths.Should().NotContainKey("/_swagger/api/reports");
        }

        [Fact]
        public void RemoveSwaggerPrefixDocumentFilter_Apply_WithEmptyPaths_ShouldNotThrow()
        {
            // Arrange
            var filter = new RemoveSwaggerPrefixDocumentFilter();
            var swaggerDoc = new OpenApiDocument
            {
                Info = new OpenApiInfo { Title = "Test API", Version = "v1" },
                Paths = new OpenApiPaths()
            };
            var context = CreateDocumentFilterContext();

            // Act
            Action act = () => filter.Apply(swaggerDoc, context);

            // Assert
            act.Should().NotThrow();
            swaggerDoc.Paths.Should().BeEmpty();
        }

        #endregion

        #region DisableProxyControllersConvention Tests

        [Fact]
        public void DisableProxyControllersConvention_Apply_WithProxyController_ShouldDisableRouting()
        {
            // Arrange
            var convention = new DisableProxyControllersConvention();
            var application = new ApplicationModel();
            var proxyController = new ControllerModel(
                typeof(object).GetTypeInfo(),
                Array.Empty<object>())
            {
                ControllerName = "UsersProxy"
            };
            proxyController.Selectors.Add(new SelectorModel());
            proxyController.Actions.Add(new ActionModel(
                typeof(object).GetMethod("ToString")!,
                Array.Empty<object>()));

            application.Controllers.Add(proxyController);

            // Act
            convention.Apply(application);

            // Assert
            proxyController.Selectors[0].AttributeRouteModel.Should().BeNull();
            proxyController.Actions[0].ApiExplorer.IsVisible.Should().BeFalse();
        }

        [Fact]
        public void DisableProxyControllersConvention_Apply_WithNormalController_ShouldNotDisable()
        {
            // Arrange
            var convention = new DisableProxyControllersConvention();
            var application = new ApplicationModel();
            var normalController = new ControllerModel(
                typeof(object).GetTypeInfo(),
                Array.Empty<object>())
            {
                ControllerName = "Users"
            };
            var selector = new SelectorModel
            {
                AttributeRouteModel = new AttributeRouteModel()
            };
            normalController.Selectors.Add(selector);

            var action = new ActionModel(
                typeof(object).GetMethod("ToString")!,
                Array.Empty<object>())
            {
                ApiExplorer = new ApiExplorerModel { IsVisible = true }
            };
            normalController.Actions.Add(action);

            application.Controllers.Add(normalController);

            // Act
            convention.Apply(application);

            // Assert
            normalController.Selectors[0].AttributeRouteModel.Should().NotBeNull();
            normalController.Actions[0].ApiExplorer.IsVisible.Should().BeTrue();
        }

        [Fact]
        public void DisableProxyControllersConvention_Apply_WithMixedControllers_ShouldOnlyDisableProxy()
        {
            // Arrange
            var convention = new DisableProxyControllersConvention();
            var application = new ApplicationModel();

            // Proxy controller
            var proxyController = new ControllerModel(
                typeof(object).GetTypeInfo(),
                Array.Empty<object>())
            {
                ControllerName = "AnalysisProxy"
            };
            proxyController.Selectors.Add(new SelectorModel());
            proxyController.Actions.Add(new ActionModel(
                typeof(object).GetMethod("ToString")!,
                Array.Empty<object>()));

            // Normal controller
            var normalController = new ControllerModel(
                typeof(object).GetTypeInfo(),
                Array.Empty<object>())
            {
                ControllerName = "Analysis"
            };
            var selector = new SelectorModel { AttributeRouteModel = new AttributeRouteModel() };
            normalController.Selectors.Add(selector);
            var action = new ActionModel(
                typeof(object).GetMethod("ToString")!,
                Array.Empty<object>())
            {
                ApiExplorer = new ApiExplorerModel { IsVisible = true }
            };
            normalController.Actions.Add(action);

            application.Controllers.Add(proxyController);
            application.Controllers.Add(normalController);

            // Act
            convention.Apply(application);

            // Assert
            proxyController.Selectors[0].AttributeRouteModel.Should().BeNull();
            proxyController.Actions[0].ApiExplorer.IsVisible.Should().BeFalse();
            normalController.Selectors[0].AttributeRouteModel.Should().NotBeNull();
            normalController.Actions[0].ApiExplorer.IsVisible.Should().BeTrue();
        }

        [Fact]
        public void DisableProxyControllersConvention_Apply_WithEmptyApplication_ShouldNotThrow()
        {
            // Arrange
            var convention = new DisableProxyControllersConvention();
            var application = new ApplicationModel();

            // Act
            Action act = () => convention.Apply(application);

            // Assert
            act.Should().NotThrow();
        }

        #endregion

        #region Helper Methods

        private OperationFilterContext CreateOperationFilterContext(string? relativePath = null)
        {
            var apiDescription = new ApiDescription
            {
                RelativePath = relativePath ?? "api/test",
                HttpMethod = "GET"
            };

            var schemaRepository = new SchemaRepository();
            var schemaGenerator = new Mock<ISchemaGenerator>();

            return new OperationFilterContext(
                apiDescription,
                schemaGenerator.Object,
                schemaRepository,
                typeof(object).GetMethod("ToString")!);
        }

        private DocumentFilterContext CreateDocumentFilterContext()
        {
            var apiDescriptions = new List<ApiDescription>();
            var schemaRepository = new SchemaRepository();
            var schemaGenerator = new Mock<ISchemaGenerator>();

            return new DocumentFilterContext(
                apiDescriptions,
                schemaGenerator.Object,
                schemaRepository);
        }

        #endregion
    }
}
