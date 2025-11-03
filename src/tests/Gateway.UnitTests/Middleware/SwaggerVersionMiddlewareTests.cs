using Xunit;
using System.Text;
using FluentAssertions;
using Gateway.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Gateway.UnitTests.Middleware;

/// <summary>
/// Tests para SwaggerVersionMiddleware - Modificación de versión OpenAPI en documentos Swagger
/// </summary>
public class SwaggerVersionMiddlewareTests
{
    #region Test Helper Methods

    private static SwaggerVersionMiddleware CreateMiddleware(RequestDelegate next)
    {
        return new SwaggerVersionMiddleware(next);
    }

    private static HttpContext CreateHttpContext(string path, string originalBody)
    {
        var context = new DefaultHttpContext();
        context.Request.Path = path;
        context.Response.Body = new MemoryStream();

        // Simular respuesta original del siguiente middleware
        if (!string.IsNullOrEmpty(originalBody))
        {
            var bytes = Encoding.UTF8.GetBytes(originalBody);
            context.Response.Body.Write(bytes, 0, bytes.Length);
            context.Response.Body.Position = 0;
        }

        return context;
    }

    private static async Task<string> GetResponseBody(HttpContext context)
    {
        context.Response.Body.Position = 0;
        using var reader = new StreamReader(context.Response.Body);
        return await reader.ReadToEndAsync();
    }

    #endregion

    #region Constructor Tests

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Component", "Middleware")]
    public void Constructor_WithValidRequestDelegate_CreatesInstance()
    {
        // Arrange
        RequestDelegate next = context => Task.CompletedTask;

        // Act
        var middleware = CreateMiddleware(next);

        // Assert
        middleware.Should().NotBeNull();
    }

    #endregion

    #region InvokeAsync - Swagger Path Tests

    [Theory]
    [InlineData("/swagger/v1/swagger.json")]
    [InlineData("/swagger/middleware/swagger.json")]
    [InlineData("/swagger/analysis/swagger.json")]
    [InlineData("/swagger/reports/swagger.json")]
    [InlineData("/swagger/users/swagger.json")]
    [Trait("Category", "Unit")]
    [Trait("Component", "Middleware")]
    public async Task InvokeAsync_SwaggerJsonPath_ProcessesResponse(string path)
    {
        // Arrange
        var originalJson = "{\"openapi\": \"3.0.4\", \"info\": {\"title\": \"Test API\"}}";
        var context = CreateHttpContext(path, "");

        var nextCalled = false;
        RequestDelegate next = async ctx =>
        {
            nextCalled = true;
            var bytes = Encoding.UTF8.GetBytes(originalJson);
            await ctx.Response.Body.WriteAsync(bytes);
        };

        var middleware = CreateMiddleware(next);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        nextCalled.Should().BeTrue("el siguiente middleware debe ser llamado");
        var responseBody = await GetResponseBody(context);
        responseBody.Should().Contain("\"openapi\": \"3.0.1\"");
        responseBody.Should().NotContain("\"openapi\": \"3.0.4\"");
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Component", "Middleware")]
    public async Task InvokeAsync_SwaggerJsonPath_WithSpaces_ReplacesVersion()
    {
        // Arrange
        var originalJson = "{\"openapi\": \"3.0.4\", \"info\": {\"title\": \"Test API\"}}";
        var context = CreateHttpContext("/swagger/v1/swagger.json", "");

        RequestDelegate next = async ctx =>
        {
            var bytes = Encoding.UTF8.GetBytes(originalJson);
            await ctx.Response.Body.WriteAsync(bytes);
        };

        var middleware = CreateMiddleware(next);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        var responseBody = await GetResponseBody(context);
        responseBody.Should().Be("{\"openapi\": \"3.0.1\", \"info\": {\"title\": \"Test API\"}}");
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Component", "Middleware")]
    public async Task InvokeAsync_SwaggerJsonPath_WithoutSpaces_ReplacesVersion()
    {
        // Arrange
        var originalJson = "{\"openapi\":\"3.0.4\",\"info\":{\"title\":\"Test API\"}}";
        var context = CreateHttpContext("/swagger/v1/swagger.json", "");

        RequestDelegate next = async ctx =>
        {
            var bytes = Encoding.UTF8.GetBytes(originalJson);
            await ctx.Response.Body.WriteAsync(bytes);
        };

        var middleware = CreateMiddleware(next);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        var responseBody = await GetResponseBody(context);
        responseBody.Should().Be("{\"openapi\":\"3.0.1\",\"info\":{\"title\":\"Test API\"}}");
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Component", "Middleware")]
    public async Task InvokeAsync_SwaggerJsonPath_WithMixedSpacing_ReplacesAllVersions()
    {
        // Arrange
        var originalJson = "{\"openapi\": \"3.0.4\", \"definitions\": {\"openapi\":\"3.0.4\"}}";
        var context = CreateHttpContext("/swagger/v1/swagger.json", "");

        RequestDelegate next = async ctx =>
        {
            var bytes = Encoding.UTF8.GetBytes(originalJson);
            await ctx.Response.Body.WriteAsync(bytes);
        };

        var middleware = CreateMiddleware(next);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        var responseBody = await GetResponseBody(context);
        responseBody.Should().Be("{\"openapi\": \"3.0.1\", \"definitions\": {\"openapi\":\"3.0.1\"}}");
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Component", "Middleware")]
    public async Task InvokeAsync_SwaggerJsonPath_EmptyResponse_HandlesGracefully()
    {
        // Arrange
        var context = CreateHttpContext("/swagger/v1/swagger.json", "");

        RequestDelegate next = ctx => Task.CompletedTask; // No escribe nada

        var middleware = CreateMiddleware(next);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        var responseBody = await GetResponseBody(context);
        responseBody.Should().BeEmpty();
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Component", "Middleware")]
    public async Task InvokeAsync_SwaggerJsonPath_WithoutOpenApiField_PassesThrough()
    {
        // Arrange
        var originalJson = "{\"info\": {\"title\": \"Test API\", \"version\": \"1.0.0\"}}";
        var context = CreateHttpContext("/swagger/v1/swagger.json", "");

        RequestDelegate next = async ctx =>
        {
            var bytes = Encoding.UTF8.GetBytes(originalJson);
            await ctx.Response.Body.WriteAsync(bytes);
        };

        var middleware = CreateMiddleware(next);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        var responseBody = await GetResponseBody(context);
        responseBody.Should().Be(originalJson, "JSON sin campo 'openapi' debe pasar sin modificar");
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Component", "Middleware")]
    public async Task InvokeAsync_SwaggerJsonPath_CaseInsensitive_ProcessesResponse()
    {
        // Arrange
        var originalJson = "{\"openapi\": \"3.0.4\", \"info\": {\"title\": \"Test API\"}}";
        var context = CreateHttpContext("/swagger/v1/SWAGGER.JSON", ""); // Uppercase extension

        RequestDelegate next = async ctx =>
        {
            var bytes = Encoding.UTF8.GetBytes(originalJson);
            await ctx.Response.Body.WriteAsync(bytes);
        };

        var middleware = CreateMiddleware(next);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        var responseBody = await GetResponseBody(context);
        responseBody.Should().Contain("\"openapi\": \"3.0.1\"");
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Component", "Middleware")]
    public async Task InvokeAsync_SwaggerJsonPath_SetsCorrectContentLength()
    {
        // Arrange
        var originalJson = "{\"openapi\": \"3.0.4\", \"info\": {\"title\": \"Test API\"}}";
        var context = CreateHttpContext("/swagger/v1/swagger.json", "");

        RequestDelegate next = async ctx =>
        {
            var bytes = Encoding.UTF8.GetBytes(originalJson);
            await ctx.Response.Body.WriteAsync(bytes);
        };

        var middleware = CreateMiddleware(next);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        var responseBody = await GetResponseBody(context);
        var expectedLength = Encoding.UTF8.GetBytes(responseBody).Length;
        context.Response.ContentLength.Should().Be(expectedLength, "ContentLength debe ser actualizado");
    }

    #endregion

    #region InvokeAsync - Non-Swagger Path Tests

    [Theory]
    [InlineData("/api/health")]
    [InlineData("/api/middleware/analyze")]
    [InlineData("/swagger")]
    [InlineData("/swagger/index.html")]
    [InlineData("/docs/swagger.json")]
    [InlineData("/api/swagger.json")]
    [Trait("Category", "Unit")]
    [Trait("Component", "Middleware")]
    public async Task InvokeAsync_NonSwaggerPath_PassesThrough(string path)
    {
        // Arrange
        var context = CreateHttpContext(path, "");
        var nextCalled = false;

        RequestDelegate next = ctx =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };

        var middleware = CreateMiddleware(next);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        nextCalled.Should().BeTrue("el siguiente middleware debe ser llamado");
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Component", "Middleware")]
    public async Task InvokeAsync_NonSwaggerPath_DoesNotModifyResponse()
    {
        // Arrange
        var originalBody = "{\"openapi\": \"3.0.4\", \"data\": \"test\"}";
        var context = CreateHttpContext("/api/data", "");

        RequestDelegate next = async ctx =>
        {
            var bytes = Encoding.UTF8.GetBytes(originalBody);
            await ctx.Response.Body.WriteAsync(bytes);
        };

        var middleware = CreateMiddleware(next);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        var responseBody = await GetResponseBody(context);
        responseBody.Should().Contain("\"openapi\": \"3.0.4\"", "respuestas no-swagger no deben ser modificadas");
    }

    #endregion

    #region Edge Cases

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Component", "Middleware")]
    public async Task InvokeAsync_LargeSwaggerDocument_ProcessesCorrectly()
    {
        // Arrange
        var largeJson = new StringBuilder();
        largeJson.Append("{\"openapi\": \"3.0.4\", \"paths\": {");

        for (int i = 0; i < 100; i++)
        {
            largeJson.Append($"\"path{i}\": {{\"get\": {{\"summary\": \"Endpoint {i}\"}}}},");
        }

        largeJson.Append("\"lastPath\": {\"get\": {\"summary\": \"Last\"}}}}");

        var context = CreateHttpContext("/swagger/v1/swagger.json", "");

        RequestDelegate next = async ctx =>
        {
            var bytes = Encoding.UTF8.GetBytes(largeJson.ToString());
            await ctx.Response.Body.WriteAsync(bytes);
        };

        var middleware = CreateMiddleware(next);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        var responseBody = await GetResponseBody(context);
        responseBody.Should().Contain("\"openapi\": \"3.0.1\"");
        responseBody.Should().NotContain("\"openapi\": \"3.0.4\"");
        responseBody.Should().Contain("\"lastPath\"", "el documento completo debe estar presente");
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Component", "Middleware")]
    public async Task InvokeAsync_NextMiddlewareThrows_PropagatesException()
    {
        // Arrange
        var context = CreateHttpContext("/swagger/v1/swagger.json", "");
        var expectedException = new InvalidOperationException("Next middleware failed");

        RequestDelegate next = ctx => throw expectedException;

        var middleware = CreateMiddleware(next);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            middleware.InvokeAsync(context));

        exception.Should().Be(expectedException);
    }

    #endregion
}

/// <summary>
/// Tests para SwaggerVersionMiddlewareExtensions
/// </summary>
public class SwaggerVersionMiddlewareExtensionsTests
{
    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Component", "Middleware")]
    public void UseSwaggerVersionMiddleware_WithValidBuilder_AddsMiddleware()
    {
        // Arrange
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();
        var appBuilder = new ApplicationBuilder(serviceProvider);

        // Act
        var result = appBuilder.UseSwaggerVersionMiddleware();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeSameAs(appBuilder, "debe devolver el mismo builder para chaining");
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Component", "Middleware")]
    public void UseSwaggerVersionMiddleware_AllowsMethodChaining()
    {
        // Arrange
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();
        var appBuilder = new ApplicationBuilder(serviceProvider);

        // Act
        var result = appBuilder
            .UseSwaggerVersionMiddleware()
            .UseSwaggerVersionMiddleware(); // Llamar dos veces para verificar chaining

        // Assert
        result.Should().NotBeNull();
    }
}
