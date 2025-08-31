using System.Text;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;

namespace Gateway.Services;

/// <summary>
/// Servicio para validación y generación de firmas digitales
/// </summary>
public sealed class SignatureValidator
{
    private readonly ILogger<SignatureValidator> _logger;

    public SignatureValidator(ILogger<SignatureValidator> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Genera una firma HMAC-SHA256 para los datos proporcionados
    /// </summary>
    /// <param name="data">Datos a firmar</param>
    /// <param name="secret">Clave secreta para la firma</param>
    /// <returns>Firma en formato hexadecimal</returns>
    public string GenerateSignature(string data, string secret)
    {
        if (string.IsNullOrEmpty(data))
            throw new ArgumentException("Data cannot be null or empty", nameof(data));

        if (string.IsNullOrEmpty(secret))
            throw new ArgumentException("Secret cannot be null or empty", nameof(secret));

        try
        {
            var keyBytes = Encoding.UTF8.GetBytes(secret);
            var dataBytes = Encoding.UTF8.GetBytes(data);

            using var hmac = new HMACSHA256(keyBytes);
            var hashBytes = hmac.ComputeHash(dataBytes);

            var signature = Convert.ToHexString(hashBytes).ToLowerInvariant();

            _logger.LogDebug("Signature generated for data of length {DataLength}", data.Length);

            return signature;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate signature for data");
            throw;
        }
    }

    /// <summary>
    /// Valida una firma contra los datos proporcionados
    /// </summary>
    /// <param name="data">Datos originales</param>
    /// <param name="signature">Firma a validar</param>
    /// <param name="secret">Clave secreta</param>
    /// <returns>True si la firma es válida</returns>
    public bool ValidateSignature(string data, string signature, string secret)
    {
        if (string.IsNullOrEmpty(data) || string.IsNullOrEmpty(signature) || string.IsNullOrEmpty(secret))
        {
            _logger.LogWarning("Invalid parameters for signature validation");
            return false;
        }

        try
        {
            var expectedSignature = GenerateSignature(data, secret);
            var isValid = string.Equals(signature, expectedSignature, StringComparison.OrdinalIgnoreCase);

            _logger.LogDebug("Signature validation result: {IsValid}", isValid);

            return isValid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate signature");
            return false;
        }
    }

    /// <summary>
    /// Genera una firma para una request HTTP usando headers y body
    /// </summary>
    /// <param name="method">Método HTTP</param>
    /// <param name="path">Path de la request</param>
    /// <param name="body">Body de la request</param>
    /// <param name="timestamp">Timestamp de la request</param>
    /// <param name="secret">Clave secreta</param>
    /// <returns>Firma de la request</returns>
    public string GenerateRequestSignature(string method, string path, string body, long timestamp, string secret)
    {
        var dataToSign = $"{method.ToUpperInvariant()}|{path}|{body}|{timestamp}";
        return GenerateSignature(dataToSign, secret);
    }

    /// <summary>
    /// Valida la firma de una request HTTP
    /// </summary>
    /// <param name="method">Método HTTP</param>
    /// <param name="path">Path de la request</param>
    /// <param name="body">Body de la request</param>
    /// <param name="timestamp">Timestamp de la request</param>
    /// <param name="signature">Firma a validar</param>
    /// <param name="secret">Clave secreta</param>
    /// <param name="toleranceSeconds">Tolerancia en segundos para el timestamp</param>
    /// <returns>True si la request es válida</returns>
    public bool ValidateRequestSignature(string method, string path, string body, long timestamp, string signature, string secret, int toleranceSeconds = 300)
    {
        // Validar timestamp (no más de toleranceSeconds de diferencia)
        var currentTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        if (Math.Abs(currentTimestamp - timestamp) > toleranceSeconds)
        {
            _logger.LogWarning("Request timestamp is outside tolerance window. Current: {Current}, Request: {Request}, Tolerance: {Tolerance}s",
                currentTimestamp, timestamp, toleranceSeconds);
            return false;
        }

        return ValidateRequestSignature(method, path, body, timestamp, signature, secret);
    }

    /// <summary>
    /// Valida la firma de una request HTTP sin validación de timestamp
    /// </summary>
    private bool ValidateRequestSignature(string method, string path, string body, long timestamp, string signature, string secret)
    {
        var expectedSignature = GenerateRequestSignature(method, path, body, timestamp, secret);
        return string.Equals(signature, expectedSignature, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Genera un token JWT simple (para testing - no usar en producción)
    /// </summary>
    /// <param name="payload">Payload del token</param>
    /// <param name="secret">Clave secreta</param>
    /// <returns>Token JWT simple</returns>
    public string GenerateSimpleJwtToken(Dictionary<string, object> payload, string secret)
    {
        var header = new Dictionary<string, object>
        {
            ["alg"] = "HS256",
            ["typ"] = "JWT"
        };

        var headerJson = System.Text.Json.JsonSerializer.Serialize(header);
        var payloadJson = System.Text.Json.JsonSerializer.Serialize(payload);

        var headerBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(headerJson))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');
        var payloadBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(payloadJson))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');

        var data = $"{headerBase64}.{payloadBase64}";
        var signature = GenerateSignature(data, secret);
        var signatureBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(signature))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');

        return $"{data}.{signatureBase64}";
    }
}