using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Gateway.Models;

/// <summary>
/// DTO validado para TranslateRequest con validaciones de seguridad completas
/// </summary>
public class ValidatedTranslateRequest
{
    /// <summary>
    /// Servicio de destino - debe ser uno de los servicios configurados
    /// </summary>
    [Required(ErrorMessage = "Service is required")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "Service name must be between 2 and 50 characters")]
    [RegularExpression(@"^[a-zA-Z][a-zA-Z0-9\-_]*$",
        ErrorMessage = "Service name must start with a letter and contain only alphanumeric characters, hyphens, and underscores")]
    public string Service { get; set; } = string.Empty;

    /// <summary>
    /// Método HTTP - solo métodos permitidos
    /// </summary>
    [Required(ErrorMessage = "Method is required")]
    [AllowedMethods]
    public string Method { get; set; } = string.Empty;

    /// <summary>
    /// Path de la API - debe comenzar con /api/
    /// </summary>
    [Required(ErrorMessage = "Path is required")]
    [StringLength(500, MinimumLength = 1, ErrorMessage = "Path must be between 1 and 500 characters")]
    [RegularExpression(@"^/api/[a-zA-Z0-9\-_/]*$",
        ErrorMessage = "Path must start with /api/ and contain only valid URL characters")]
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// Query parameters - validación de seguridad
    /// </summary>
    [MaxLength(20, ErrorMessage = "Maximum 20 query parameters allowed")]
    public Dictionary<string, string> Query { get; set; } = new();

    /// <summary>
    /// Headers HTTP - filtrado de headers seguros
    /// </summary>
    [MaxLength(30, ErrorMessage = "Maximum 30 headers allowed")]
    public Dictionary<string, string> Headers { get; set; } = new();

    /// <summary>
    /// Body de la request - puede ser un objeto JSON o string
    /// </summary>
    public JsonElement? Body { get; set; }
}

/// <summary>
/// DTO para invalidar caché con validaciones
/// </summary>
public class InvalidateCacheRequest
{
    /// <summary>
    /// Servicio para invalidar caché
    /// </summary>
    [Required(ErrorMessage = "Service is required")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "Service name must be between 2 and 50 characters")]
    [RegularExpression(@"^[a-zA-Z][a-zA-Z0-9\-_]*$",
        ErrorMessage = "Service name must start with a letter and contain only alphanumeric characters, hyphens, and underscores")]
    public string Service { get; set; } = string.Empty;

    /// <summary>
    /// Opcional: Pattern específico para invalidar
    /// </summary>
    [StringLength(200, ErrorMessage = "Pattern cannot exceed 200 characters")]
    [RegularExpression(@"^[a-zA-Z0-9\-_/*]*$",
        ErrorMessage = "Pattern must contain only alphanumeric characters, hyphens, underscores, slashes, and wildcards")]
    public string? Pattern { get; set; }
}

/// <summary>
/// Validador personalizado para métodos HTTP permitidos
/// </summary>
public class AllowedMethodsAttribute : ValidationAttribute
{
    private static readonly string[] AllowedMethods = { "GET", "POST", "PUT", "PATCH", "DELETE", "HEAD", "OPTIONS" };

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is string method)
        {
            if (AllowedMethods.Contains(method.ToUpperInvariant()))
            {
                return ValidationResult.Success;
            }
        }

        return new ValidationResult($"Method must be one of: {string.Join(", ", AllowedMethods)}");
    }
}

/// <summary>
/// Validador para rangos de IDs seguros
/// </summary>
public class SafeIdRangeAttribute : ValidationAttribute
{
    public int Minimum { get; set; } = 1;
    public int Maximum { get; set; } = int.MaxValue;

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is int id)
        {
            if (id >= Minimum && id <= Maximum)
            {
                return ValidationResult.Success;
            }
        }

        return new ValidationResult($"ID must be between {Minimum} and {Maximum}");
    }
}

/// <summary>
/// Atributo para validar longitud máxima de colecciones
/// </summary>
public class MaxLengthAttribute : ValidationAttribute
{
    public int MaxLength { get; }

    public MaxLengthAttribute(int maxLength)
    {
        MaxLength = maxLength;
    }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is IDictionary<string, string> dictionary)
        {
            if (dictionary.Count <= MaxLength)
            {
                return ValidationResult.Success;
            }
            return new ValidationResult($"Collection cannot have more than {MaxLength} items");
        }

        return ValidationResult.Success;
    }
}