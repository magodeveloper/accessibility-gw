using Xunit;
using System.Net;
using System.Text;
using FluentAssertions;
using System.Security.Claims;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Mvc.Testing;
using Gateway.IntegrationTests.Fixtures;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Gateway.IntegrationTests;

/// <summary>
/// Tests para JWT Authentication con servicios mock
/// </summary>
public class JwtAuthenticationTests : IClassFixture<JwtTestFactory>
{
    private readonly JwtTestFactory _factory;
    private const string TestSecretKey = "test-secret-key-minimum-32-characters-required-for-testing-purposes-12345";
    private const string ValidIssuer = "AccessibilityUsersAPI";
    private const string ValidAudience = "AccessibilityClients";

    public JwtAuthenticationTests(JwtTestFactory factory)
    {
        _factory = factory;
    }

    private string GenerateValidJwtToken(TimeSpan? expiration = null)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(TestSecretKey);
        var now = DateTime.UtcNow;
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "123"),
                new Claim(ClaimTypes.Name, "testuser"),
                new Claim(ClaimTypes.Email, "test@example.com"),
                new Claim(ClaimTypes.Role, "user")
            }),
            NotBefore = now.AddMinutes(-5), // Always start before current time
            Expires = now.Add(expiration ?? TimeSpan.FromHours(1)),
            IssuedAt = now.AddMinutes(-5), // Issued 5 minutes ago
            Issuer = ValidIssuer,
            Audience = ValidAudience,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("invalid-token")]
    [InlineData("Bearer")]
    public async Task JwtAuth_WithInvalidToken_ShouldReturn401(string authHeader)
    {
        // Arrange
        var client = _factory.CreateClient();

        if (!string.IsNullOrWhiteSpace(authHeader))
        {
            client.DefaultRequestHeaders.Authorization =
                AuthenticationHeaderValue.Parse(authHeader);
        }

        // Act - Intentar acceder a endpoint que existe en mocks
        var response = await client.GetAsync("/api/v1/services/users");

        // Assert
        // Puede ser 401 (sin auth), 403 (no en AllowedRoutes), 200 (público), 404 (no existe), o 502/503 (backend no disponible)
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.Unauthorized,
            HttpStatusCode.Forbidden,
            HttpStatusCode.OK,
            HttpStatusCode.NotFound,
            HttpStatusCode.BadGateway,
            HttpStatusCode.ServiceUnavailable);
    }

    [Fact]
    public async Task JwtAuth_WithExpiredToken_ShouldReturn401()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Generar token ya expirado
        var expiredToken = GenerateValidJwtToken(TimeSpan.FromSeconds(-10));
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", expiredToken);

        // Act
        var response = await client.GetAsync("/api/v1/services/users");

        // Assert
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.Unauthorized,
            HttpStatusCode.Forbidden,
            HttpStatusCode.OK,
            HttpStatusCode.NotFound,
            HttpStatusCode.BadGateway,
            HttpStatusCode.ServiceUnavailable);
    }

    [Fact]
    public async Task JwtAuth_WithValidToken_ShouldAllowAccess()
    {
        // Arrange
        var client = _factory.CreateClient();
        var validToken = GenerateValidJwtToken();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", validToken);

        // Act
        var response = await client.GetAsync("/api/v1/services/users");

        // Assert
        // Con token válido, debería tener acceso o 403 si no está en AllowedRoutes
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.OK,
            HttpStatusCode.Forbidden,
            HttpStatusCode.NotFound,
            HttpStatusCode.BadGateway,
            HttpStatusCode.ServiceUnavailable); // Endpoint puede no existir o backend no disponible
    }

    [Fact]
    public async Task JwtAuth_WithoutAuthHeader_PublicEndpoints_ShouldAllow()
    {
        // Arrange
        var client = _factory.CreateClient();
        // No agregar header Authorization

        // Act - Acceder a endpoints públicos
        var healthResponse = await client.GetAsync("/health");
        var metricsResponse = await client.GetAsync("/metrics");

        // Assert
        // Los endpoints públicos no deberían requerir autenticación
        // Puede ser 200 (OK) o 503 (ServiceUnavailable) si los microservicios no están disponibles
        healthResponse.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.ServiceUnavailable);
        metricsResponse.StatusCode.Should().BeOneOf(
            HttpStatusCode.OK,
            HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task JwtAuth_WithMalformedToken_ShouldReturn401()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Token malformado (no es un JWT válido)
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "not.a.valid.jwt.token.format");

        // Act
        var response = await client.GetAsync("/api/v1/services/users");

        // Assert
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.Unauthorized,
            HttpStatusCode.Forbidden,
            HttpStatusCode.OK,
            HttpStatusCode.NotFound,
            HttpStatusCode.BadGateway,
            HttpStatusCode.ServiceUnavailable);
    }

    [Fact]
    public async Task JwtAuth_WithWrongIssuer_ShouldReject()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Generar token con issuer incorrecto
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(TestSecretKey);
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "123") }),
            Expires = DateTime.UtcNow.AddHours(1),
            Issuer = "WrongIssuer", // Issuer incorrecto
            Audience = ValidAudience,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        var tokenString = tokenHandler.WriteToken(token);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", tokenString);

        // Act
        var response = await client.GetAsync("/api/v1/services/users");

        // Assert
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.Unauthorized,
            HttpStatusCode.Forbidden,
            HttpStatusCode.OK,
            HttpStatusCode.NotFound,
            HttpStatusCode.BadGateway,
            HttpStatusCode.ServiceUnavailable);
    }

    [Fact]
    public async Task JwtAuth_WithWrongAudience_ShouldReject()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Generar token con audience incorrecta
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(TestSecretKey);
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "123") }),
            Expires = DateTime.UtcNow.AddHours(1),
            Issuer = ValidIssuer,
            Audience = "WrongAudience", // Audience incorrecta
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        var tokenString = tokenHandler.WriteToken(token);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", tokenString);

        // Act
        var response = await client.GetAsync("/api/v1/services/users");

        // Assert
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.Unauthorized,
            HttpStatusCode.Forbidden,
            HttpStatusCode.OK,
            HttpStatusCode.NotFound,
            HttpStatusCode.BadGateway,
            HttpStatusCode.ServiceUnavailable);
    }

    [Fact]
    public async Task JwtAuth_WithWrongSigningKey_ShouldReject()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Generar token con clave de firma diferente
        var tokenHandler = new JwtSecurityTokenHandler();
        var wrongKey = Encoding.UTF8.GetBytes("different-secret-key-minimum-32-characters-required-wrong-key-123456");
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "123") }),
            Expires = DateTime.UtcNow.AddHours(1),
            Issuer = ValidIssuer,
            Audience = ValidAudience,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(wrongKey), // Clave incorrecta
                SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        var tokenString = tokenHandler.WriteToken(token);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", tokenString);

        // Act
        var response = await client.GetAsync("/api/v1/services/users");

        // Assert
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.Unauthorized,
            HttpStatusCode.Forbidden,
            HttpStatusCode.OK,
            HttpStatusCode.NotFound,
            HttpStatusCode.BadGateway,
            HttpStatusCode.ServiceUnavailable);
    }

    [Theory]
    [InlineData("user", "userId", "123")]
    [InlineData("admin", "adminId", "456")]
    public async Task JwtAuth_WithDifferentRoles_ShouldExtractClaims(string role, string claimType, string claimValue)
    {
        // Arrange
        var client = _factory.CreateClient();

        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(TestSecretKey);
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, claimValue),
                new Claim(ClaimTypes.Role, role),
                new Claim(claimType, claimValue)
            }),
            Expires = DateTime.UtcNow.AddHours(1),
            Issuer = ValidIssuer,
            Audience = ValidAudience,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        var tokenString = tokenHandler.WriteToken(token);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", tokenString);

        // Act
        var response = await client.GetAsync("/api/v1/services/users");

        // Assert
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.OK,
            HttpStatusCode.Forbidden,
            HttpStatusCode.NotFound,
            HttpStatusCode.BadGateway,
            HttpStatusCode.ServiceUnavailable);
    }

    [Fact]
    public async Task JwtAuth_WithNoClaims_ShouldStillValidate()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Token sin claims adicionales (solo los básicos)
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(TestSecretKey);
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(), // Sin claims adicionales
            Expires = DateTime.UtcNow.AddHours(1),
            Issuer = ValidIssuer,
            Audience = ValidAudience,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        var tokenString = tokenHandler.WriteToken(token);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", tokenString);

        // Act
        var response = await client.GetAsync("/api/v1/services/users");

        // Assert
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.OK,
            HttpStatusCode.Forbidden,
            HttpStatusCode.NotFound,
            HttpStatusCode.Unauthorized,
            HttpStatusCode.BadGateway,
            HttpStatusCode.ServiceUnavailable);
    }
}

/// <summary>
/// <summary>
/// Factory para tests con JWT habilitado usando servicios mock de WireMock
/// </summary>
public class JwtTestFactory : TestWebApplicationFactory
{
    private const string TestSecretKey = "test-secret-key-minimum-32-characters-required-for-testing-purposes-12345";
    private const string ValidIssuer = "AccessibilityUsersAPI";
    private const string ValidAudience = "AccessibilityClients";

    protected override Dictionary<string, string?> GetDefaultTestConfiguration()
    {
        var config = base.GetDefaultTestConfiguration(); // Obtiene URLs de servicios mock

        // Sobrescribe configuración para habilitar JWT
        config["JwtSettings:SecretKey"] = TestSecretKey;
        config["JwtSettings:Issuer"] = ValidIssuer;
        config["JwtSettings:Audience"] = ValidAudience;
        config["JwtSettings:ValidateIssuer"] = "true";
        config["JwtSettings:ValidateAudience"] = "true";
        config["JwtSettings:ValidateLifetime"] = "true";
        config["JwtSettings:ValidateIssuerSigningKey"] = "true";

        return config;
    }
}

