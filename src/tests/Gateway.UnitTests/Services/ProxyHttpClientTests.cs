using Moq;
using Xunit;
using System.Net;
using Moq.Protected;
using System.Text.Json;
using Gateway.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace Gateway.UnitTests.Services
{
    /// <summary>
    /// Tests para ProxyHttpClient - Cliente HTTP crítico para proxy de microservicios
    /// Target: 80%+ coverage
    /// </summary>
    public class ProxyHttpClientTests
    {
        private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
        private readonly Mock<IConfiguration> _configurationMock;
        private readonly Mock<ILogger<ProxyHttpClient>> _loggerMock;
        private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
        private readonly ProxyHttpClient _proxyHttpClient;

        public ProxyHttpClientTests()
        {
            _httpClientFactoryMock = new Mock<IHttpClientFactory>();
            _configurationMock = new Mock<IConfiguration>();
            _loggerMock = new Mock<ILogger<ProxyHttpClient>>();
            _httpMessageHandlerMock = new Mock<HttpMessageHandler>();

            _proxyHttpClient = new ProxyHttpClient(
                _httpClientFactoryMock.Object,
                _configurationMock.Object,
                _loggerMock.Object);
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_WithValidParameters_ShouldCreateInstance()
        {
            // Arrange & Act
            var client = new ProxyHttpClient(
                _httpClientFactoryMock.Object,
                _configurationMock.Object,
                _loggerMock.Object);

            // Assert
            client.Should().NotBeNull();
        }

        #endregion

        #region ProxyRequestAsync - Success Cases

        [Fact]
        public async Task ProxyRequestAsync_WithValidRequest_ShouldReturnSuccessResult()
        {
            // Arrange
            var serviceName = "AnalysisService";
            var path = "/api/analysis";
            var serviceUrl = "http://localhost:5001";
            var responseContent = "{\"id\":\"123\",\"status\":\"success\"}";

            SetupConfiguration(serviceName, serviceUrl);
            SetupHttpClient(HttpStatusCode.OK, responseContent, "application/json");

            // Act
            var result = await _proxyHttpClient.ProxyRequestAsync(
                serviceName, path, HttpMethod.Get);

            // Assert
            result.Should().NotBeNull();
            VerifyHttpRequest(HttpMethod.Get, $"{serviceUrl}{path}");
        }

        [Fact]
        public async Task ProxyRequestAsync_WithQueryParameters_ShouldIncludeInUrl()
        {
            // Arrange
            var serviceName = "UsersService";
            var path = "/api/users";
            var serviceUrl = "http://localhost:5002";
            var queryParams = new Dictionary<string, string>
            {
                { "page", "1" },
                { "size", "10" },
                { "email", "test@example.com" }
            };

            SetupConfiguration(serviceName, serviceUrl);
            SetupHttpClient(HttpStatusCode.OK, "[]", "application/json");

            // Act
            var result = await _proxyHttpClient.ProxyRequestAsync(
                serviceName, path, HttpMethod.Get, null, queryParams);

            // Assert
            result.Should().NotBeNull();
            _httpMessageHandlerMock
                .Protected()
                .Verify(
                    "SendAsync",
                    Times.Once(),
                    ItExpr.Is<HttpRequestMessage>(req =>
                        req.Method == HttpMethod.Get &&
                        req.RequestUri!.ToString().Contains("page=1") &&
                        req.RequestUri.ToString().Contains("size=10") &&
                        req.RequestUri.ToString().Contains("email=test%40example.com")),
                    ItExpr.IsAny<CancellationToken>());
        }

        [Fact]
        public async Task ProxyRequestAsync_WithPostBody_ShouldSerializeAsJson()
        {
            // Arrange
            var serviceName = "ReportsService";
            var path = "/api/reports";
            var serviceUrl = "http://localhost:5003";
            var requestBody = new { title = "Test Report", format = "PDF" };

            SetupConfiguration(serviceName, serviceUrl);
            SetupHttpClient(HttpStatusCode.Created, "{\"id\":\"456\"}", "application/json");

            // Act
            var result = await _proxyHttpClient.ProxyRequestAsync(
                serviceName, path, HttpMethod.Post, requestBody);

            // Assert
            result.Should().NotBeNull();
            VerifyHttpRequest(HttpMethod.Post, $"{serviceUrl}{path}");
        }

        [Theory]
        [InlineData(HttpStatusCode.OK)]
        [InlineData(HttpStatusCode.Created)]
        [InlineData(HttpStatusCode.Accepted)]
        [InlineData(HttpStatusCode.NoContent)]
        public async Task ProxyRequestAsync_WithSuccessStatusCodes_ShouldReturnSuccess(HttpStatusCode statusCode)
        {
            // Arrange
            var serviceName = "TestService";
            var path = "/api/test";
            var serviceUrl = "http://localhost:5000";

            SetupConfiguration(serviceName, serviceUrl);
            SetupHttpClient(statusCode, "{\"success\":true}", "application/json");

            // Act
            var result = await _proxyHttpClient.ProxyRequestAsync(
                serviceName, path, HttpMethod.Get);

            // Assert
            result.Should().NotBeNull();
        }

        [Fact]
        public async Task ProxyRequestAsync_WithNonJsonResponse_ShouldReturnContent()
        {
            // Arrange
            var serviceName = "FileService";
            var path = "/api/download";
            var serviceUrl = "http://localhost:5004";
            var textContent = "Plain text response";

            SetupConfiguration(serviceName, serviceUrl);
            SetupHttpClient(HttpStatusCode.OK, textContent, "text/plain");

            // Act
            var result = await _proxyHttpClient.ProxyRequestAsync(
                serviceName, path, HttpMethod.Get);

            // Assert
            result.Should().NotBeNull();
        }

        [Fact]
        public async Task ProxyRequestAsync_WithInvalidJson_ShouldReturnAsText()
        {
            // Arrange
            var serviceName = "TestService";
            var path = "/api/test";
            var serviceUrl = "http://localhost:5000";
            var invalidJson = "{invalid json}";

            SetupConfiguration(serviceName, serviceUrl);
            SetupHttpClient(HttpStatusCode.OK, invalidJson, "application/json");

            // Act
            var result = await _proxyHttpClient.ProxyRequestAsync(
                serviceName, path, HttpMethod.Get);

            // Assert
            result.Should().NotBeNull();
        }

        #endregion

        #region ProxyRequestAsync - Error Cases

        [Fact]
        public async Task ProxyRequestAsync_WithMissingServiceConfiguration_ShouldReturnServiceUnavailable()
        {
            // Arrange
            var serviceName = "NonExistentService";
            var path = "/api/test";

            _configurationMock.Setup(c => c[$"Gate:Services:{serviceName}"])
                .Returns((string?)null);

            // Act
            var result = await _proxyHttpClient.ProxyRequestAsync(
                serviceName, path, HttpMethod.Get);

            // Assert
            result.Should().NotBeNull();
            VerifyLogError("Service URL not found");
        }

        [Theory]
        [InlineData(HttpStatusCode.BadRequest)]
        [InlineData(HttpStatusCode.Unauthorized)]
        [InlineData(HttpStatusCode.Forbidden)]
        [InlineData(HttpStatusCode.NotFound)]
        [InlineData(HttpStatusCode.InternalServerError)]
        public async Task ProxyRequestAsync_WithErrorStatusCode_ShouldReturnProblem(HttpStatusCode statusCode)
        {
            // Arrange
            var serviceName = "TestService";
            var path = "/api/test";
            var serviceUrl = "http://localhost:5000";
            var errorContent = "{\"error\":\"Something went wrong\"}";

            SetupConfiguration(serviceName, serviceUrl);
            SetupHttpClient(statusCode, errorContent, "application/json");

            // Act
            var result = await _proxyHttpClient.ProxyRequestAsync(
                serviceName, path, HttpMethod.Get);

            // Assert
            result.Should().NotBeNull();
            VerifyLogWarning("Proxy request failed");
        }

        [Fact]
        public async Task ProxyRequestAsync_WithHttpRequestException_ShouldReturnServiceUnavailable()
        {
            // Arrange
            var serviceName = "TestService";
            var path = "/api/test";
            var serviceUrl = "http://localhost:5000";

            SetupConfiguration(serviceName, serviceUrl);
            SetupHttpClientException(new HttpRequestException("Connection refused"));

            // Act
            var result = await _proxyHttpClient.ProxyRequestAsync(
                serviceName, path, HttpMethod.Get);

            // Assert
            result.Should().NotBeNull();
            VerifyLogError("HTTP error during proxy request");
        }

        [Fact]
        public async Task ProxyRequestAsync_WithTimeout_ShouldReturnRequestTimeout()
        {
            // Arrange
            var serviceName = "SlowService";
            var path = "/api/slow";
            var serviceUrl = "http://localhost:5000";

            SetupConfiguration(serviceName, serviceUrl);
            SetupHttpClientException(new TaskCanceledException("Request timeout"));

            // Act
            var result = await _proxyHttpClient.ProxyRequestAsync(
                serviceName, path, HttpMethod.Get);

            // Assert
            result.Should().NotBeNull();
            VerifyLogError("Timeout during proxy request");
        }

        [Fact]
        public async Task ProxyRequestAsync_WithUnexpectedException_ShouldReturnInternalServerError()
        {
            // Arrange
            var serviceName = "TestService";
            var path = "/api/test";
            var serviceUrl = "http://localhost:5000";

            SetupConfiguration(serviceName, serviceUrl);
            SetupHttpClientException(new InvalidOperationException("Unexpected error"));

            // Act
            var result = await _proxyHttpClient.ProxyRequestAsync(
                serviceName, path, HttpMethod.Get);

            // Assert
            result.Should().NotBeNull();
            VerifyLogError("Unexpected error during proxy request");
        }

        #endregion

        #region HTTP Method Shortcuts Tests

        [Fact]
        public async Task GetAsync_ShouldCallProxyRequestWithGetMethod()
        {
            // Arrange
            var serviceName = "TestService";
            var path = "/api/test";
            var serviceUrl = "http://localhost:5000";

            SetupConfiguration(serviceName, serviceUrl);
            SetupHttpClient(HttpStatusCode.OK, "{}", "application/json");

            // Act
            var result = await _proxyHttpClient.GetAsync(serviceName, path);

            // Assert
            result.Should().NotBeNull();
            VerifyHttpRequest(HttpMethod.Get, $"{serviceUrl}{path}");
        }

        [Fact]
        public async Task GetAsync_WithQueryParams_ShouldPassToProxyRequest()
        {
            // Arrange
            var serviceName = "TestService";
            var path = "/api/test";
            var serviceUrl = "http://localhost:5000";
            var queryParams = new Dictionary<string, string> { { "id", "123" } };

            SetupConfiguration(serviceName, serviceUrl);
            SetupHttpClient(HttpStatusCode.OK, "{}", "application/json");

            // Act
            var result = await _proxyHttpClient.GetAsync(serviceName, path, queryParams);

            // Assert
            result.Should().NotBeNull();
            _httpMessageHandlerMock
                .Protected()
                .Verify(
                    "SendAsync",
                    Times.Once(),
                    ItExpr.Is<HttpRequestMessage>(req =>
                        req.Method == HttpMethod.Get &&
                        req.RequestUri!.ToString().Contains("id=123")),
                    ItExpr.IsAny<CancellationToken>());
        }

        [Fact]
        public async Task PostAsync_ShouldCallProxyRequestWithPostMethod()
        {
            // Arrange
            var serviceName = "TestService";
            var path = "/api/test";
            var serviceUrl = "http://localhost:5000";
            var body = new { data = "test" };

            SetupConfiguration(serviceName, serviceUrl);
            SetupHttpClient(HttpStatusCode.Created, "{}", "application/json");

            // Act
            var result = await _proxyHttpClient.PostAsync(serviceName, path, body);

            // Assert
            result.Should().NotBeNull();
            VerifyHttpRequest(HttpMethod.Post, $"{serviceUrl}{path}");
        }

        [Fact]
        public async Task PutAsync_ShouldCallProxyRequestWithPutMethod()
        {
            // Arrange
            var serviceName = "TestService";
            var path = "/api/test/123";
            var serviceUrl = "http://localhost:5000";
            var body = new { data = "updated" };

            SetupConfiguration(serviceName, serviceUrl);
            SetupHttpClient(HttpStatusCode.OK, "{}", "application/json");

            // Act
            var result = await _proxyHttpClient.PutAsync(serviceName, path, body);

            // Assert
            result.Should().NotBeNull();
            VerifyHttpRequest(HttpMethod.Put, $"{serviceUrl}{path}");
        }

        [Fact]
        public async Task PatchAsync_ShouldCallProxyRequestWithPatchMethod()
        {
            // Arrange
            var serviceName = "TestService";
            var path = "/api/test/123";
            var serviceUrl = "http://localhost:5000";
            var body = new { field = "value" };

            SetupConfiguration(serviceName, serviceUrl);
            SetupHttpClient(HttpStatusCode.OK, "{}", "application/json");

            // Act
            var result = await _proxyHttpClient.PatchAsync(serviceName, path, body);

            // Assert
            result.Should().NotBeNull();
            VerifyHttpRequest(new HttpMethod("PATCH"), $"{serviceUrl}{path}");
        }

        [Fact]
        public async Task DeleteAsync_ShouldCallProxyRequestWithDeleteMethod()
        {
            // Arrange
            var serviceName = "TestService";
            var path = "/api/test/123";
            var serviceUrl = "http://localhost:5000";

            SetupConfiguration(serviceName, serviceUrl);
            SetupHttpClient(HttpStatusCode.NoContent, "", "application/json");

            // Act
            var result = await _proxyHttpClient.DeleteAsync(serviceName, path);

            // Assert
            result.Should().NotBeNull();
            VerifyHttpRequest(HttpMethod.Delete, $"{serviceUrl}{path}");
        }

        #endregion

        #region Helper Methods

        private void SetupConfiguration(string serviceName, string serviceUrl)
        {
            _configurationMock.Setup(c => c[$"Gate:Services:{serviceName}"])
                .Returns(serviceUrl);
        }

        private void SetupHttpClient(HttpStatusCode statusCode, string content, string contentType)
        {
            var response = new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(content, System.Text.Encoding.UTF8, contentType)
            };

            _httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(response);

            var httpClient = new HttpClient(_httpMessageHandlerMock.Object);
            _httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>()))
                .Returns(httpClient);
        }

        private void SetupHttpClientException(Exception exception)
        {
            _httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ThrowsAsync(exception);

            var httpClient = new HttpClient(_httpMessageHandlerMock.Object);
            _httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>()))
                .Returns(httpClient);
        }

        private void VerifyHttpRequest(HttpMethod method, string url)
        {
            _httpMessageHandlerMock
                .Protected()
                .Verify(
                    "SendAsync",
                    Times.Once(),
                    ItExpr.Is<HttpRequestMessage>(req =>
                        req.Method == method &&
                        req.RequestUri!.ToString() == url),
                    ItExpr.IsAny<CancellationToken>());
        }

        private void VerifyLogError(string messageContains)
        {
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(messageContains)),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.AtLeastOnce);
        }

        private void VerifyLogWarning(string messageContains)
        {
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(messageContains)),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.AtLeastOnce);
        }

        #endregion
    }
}
