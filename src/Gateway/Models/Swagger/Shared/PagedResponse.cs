using System.ComponentModel.DataAnnotations;
using Swashbuckle.AspNetCore.Annotations;

namespace Gateway.Models.Swagger.Shared;

/// <summary>
/// Respuesta paginada genérica para listas de entidades
/// </summary>
/// <typeparam name="T">Tipo de entidad en la lista</typeparam>
[SwaggerSchema(Description = "Respuesta paginada estándar para colecciones de datos")]
public class PagedResponse<T>
{
    /// <summary>
    /// Lista de elementos en la página actual
    /// </summary>
    [SwaggerSchema(Description = "Elementos de la página solicitada")]
    public List<T> Items { get; set; } = new();

    /// <summary>
    /// Número total de elementos que cumplen el filtro (todas las páginas)
    /// </summary>
    [SwaggerSchema(Description = "Total de elementos en todas las páginas")]
    public int TotalCount { get; set; }

    /// <summary>
    /// Número de página actual (base 1)
    /// </summary>
    [SwaggerSchema(Description = "Página actual (primera página = 1)")]
    public int PageNumber { get; set; }

    /// <summary>
    /// Tamaño de página configurado
    /// </summary>
    [SwaggerSchema(Description = "Cantidad de elementos por página")]
    public int PageSize { get; set; }

    /// <summary>
    /// Número total de páginas disponibles
    /// </summary>
    [SwaggerSchema(Description = "Total de páginas calculado (TotalCount / PageSize)")]
    public int TotalPages { get; set; }

    /// <summary>
    /// Indica si hay página anterior
    /// </summary>
    [SwaggerSchema(Description = "True si PageNumber > 1")]
    public bool HasPreviousPage { get; set; }

    /// <summary>
    /// Indica si hay página siguiente
    /// </summary>
    [SwaggerSchema(Description = "True si PageNumber < TotalPages")]
    public bool HasNextPage { get; set; }

    /// <summary>
    /// Número de la página anterior (null si no existe)
    /// </summary>
    [SwaggerSchema(Description = "Número de página anterior o null si es la primera")]
    public int? PreviousPageNumber { get; set; }

    /// <summary>
    /// Número de la página siguiente (null si no existe)
    /// </summary>
    [SwaggerSchema(Description = "Número de página siguiente o null si es la última")]
    public int? NextPageNumber { get; set; }

    /// <summary>
    /// Índice del primer elemento de la página (base 0)
    /// </summary>
    [SwaggerSchema(Description = "Índice global del primer elemento de esta página")]
    public int FirstItemIndex { get; set; }

    /// <summary>
    /// Índice del último elemento de la página (base 0)
    /// </summary>
    [SwaggerSchema(Description = "Índice global del último elemento de esta página")]
    public int LastItemIndex { get; set; }

    /// <summary>
    /// Constructor por defecto
    /// </summary>
    public PagedResponse()
    {
    }

    /// <summary>
    /// Constructor con parámetros
    /// </summary>
    public PagedResponse(List<T> items, int totalCount, int pageNumber, int pageSize)
    {
        Items = items;
        TotalCount = totalCount;
        PageNumber = pageNumber;
        PageSize = pageSize;
        TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        HasPreviousPage = pageNumber > 1;
        HasNextPage = pageNumber < TotalPages;
        PreviousPageNumber = HasPreviousPage ? pageNumber - 1 : null;
        NextPageNumber = HasNextPage ? pageNumber + 1 : null;
        FirstItemIndex = (pageNumber - 1) * pageSize;
        LastItemIndex = Math.Min(FirstItemIndex + pageSize - 1, totalCount - 1);
    }
}

/// <summary>
/// Parámetros de paginación estándar
/// </summary>
[SwaggerSchema(Description = "Parámetros de paginación para queries de lista")]
public class PaginationParams
{
    /// <summary>
    /// Número de página a recuperar (base 1)
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "El número de página debe ser mayor a 0")]
    [SwaggerSchema(Description = "Página a recuperar (default: 1, primera página)")]
    public int PageNumber { get; set; } = 1;

    /// <summary>
    /// Cantidad de elementos por página
    /// </summary>
    [Range(1, 100, ErrorMessage = "El tamaño de página debe estar entre 1 y 100")]
    [SwaggerSchema(Description = "Elementos por página (default: 10, min: 1, max: 100)")]
    public int PageSize { get; set; } = 10;

    /// <summary>
    /// Campo para ordenar resultados
    /// </summary>
    [SwaggerSchema(Description = "Nombre del campo para ordenamiento (ej: 'CreatedAt', 'Email')")]
    public string? SortBy { get; set; }

    /// <summary>
    /// Dirección de ordenamiento
    /// </summary>
    [RegularExpression("^(asc|desc|ASC|DESC)$", ErrorMessage = "SortDirection debe ser 'asc' o 'desc'")]
    [SwaggerSchema(Description = "Dirección: 'asc' (ascendente) o 'desc' (descendente)")]
    public string? SortDirection { get; set; } = "desc";
}

/// <summary>
/// Metadata de paginación (sin datos, solo información de páginas)
/// </summary>
[SwaggerSchema(Description = "Información de paginación sin los datos (útil para headers)")]
public class PaginationMetadata
{
    /// <summary>
    /// Número total de elementos
    /// </summary>
    [SwaggerSchema(Description = "Total de elementos en todas las páginas")]
    public int TotalCount { get; set; }

    /// <summary>
    /// Tamaño de página
    /// </summary>
    [SwaggerSchema(Description = "Elementos por página")]
    public int PageSize { get; set; }

    /// <summary>
    /// Página actual
    /// </summary>
    [SwaggerSchema(Description = "Número de página actual")]
    public int CurrentPage { get; set; }

    /// <summary>
    /// Total de páginas
    /// </summary>
    [SwaggerSchema(Description = "Total de páginas disponibles")]
    public int TotalPages { get; set; }

    /// <summary>
    /// URL de página anterior (opcional)
    /// </summary>
    [SwaggerSchema(Description = "URL completa de la página anterior", Format = "uri")]
    public string? PreviousPageUrl { get; set; }

    /// <summary>
    /// URL de página siguiente (opcional)
    /// </summary>
    [SwaggerSchema(Description = "URL completa de la página siguiente", Format = "uri")]
    public string? NextPageUrl { get; set; }

    /// <summary>
    /// URL de primera página (opcional)
    /// </summary>
    [SwaggerSchema(Description = "URL completa de la primera página", Format = "uri")]
    public string? FirstPageUrl { get; set; }

    /// <summary>
    /// URL de última página (opcional)
    /// </summary>
    [SwaggerSchema(Description = "URL completa de la última página", Format = "uri")]
    public string? LastPageUrl { get; set; }
}

/// <summary>
/// Respuesta paginada con metadata extendida
/// </summary>
/// <typeparam name="T">Tipo de entidad en la lista</typeparam>
[SwaggerSchema(Description = "Respuesta paginada con metadata adicional y links de navegación")]
public class PagedResponseWithLinks<T> : PagedResponse<T>
{
    /// <summary>
    /// Metadata adicional de paginación
    /// </summary>
    [SwaggerSchema(Description = "Información extendida de paginación")]
    public PaginationMetadata Metadata { get; set; } = new();

    /// <summary>
    /// Links de navegación HATEOAS
    /// </summary>
    [SwaggerSchema(Description = "Enlaces de navegación entre páginas")]
    public Dictionary<string, string> Links { get; set; } = new();

    /// <summary>
    /// Constructor por defecto
    /// </summary>
    public PagedResponseWithLinks() : base()
    {
    }

    /// <summary>
    /// Constructor con parámetros
    /// </summary>
    public PagedResponseWithLinks(
        List<T> items,
        int totalCount,
        int pageNumber,
        int pageSize,
        string baseUrl) : base(items, totalCount, pageNumber, pageSize)
    {
        Metadata = new PaginationMetadata
        {
            TotalCount = totalCount,
            PageSize = pageSize,
            CurrentPage = pageNumber,
            TotalPages = TotalPages
        };

        // Construir URLs de navegación
        if (HasPreviousPage)
        {
            Links["previous"] = $"{baseUrl}?pageNumber={PreviousPageNumber}&pageSize={pageSize}";
            Links["first"] = $"{baseUrl}?pageNumber=1&pageSize={pageSize}";
        }

        Links["self"] = $"{baseUrl}?pageNumber={pageNumber}&pageSize={pageSize}";

        if (HasNextPage)
        {
            Links["next"] = $"{baseUrl}?pageNumber={NextPageNumber}&pageSize={pageSize}";
            Links["last"] = $"{baseUrl}?pageNumber={TotalPages}&pageSize={pageSize}";
        }
    }
}

/// <summary>
/// Helper para crear respuestas paginadas
/// </summary>
public static class PagedResponseHelper
{
    /// <summary>
    /// Crea una respuesta paginada desde una lista completa
    /// </summary>
    public static PagedResponse<T> Create<T>(
        List<T> allItems,
        int pageNumber,
        int pageSize)
    {
        var totalCount = allItems.Count;
        var items = allItems
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PagedResponse<T>(items, totalCount, pageNumber, pageSize);
    }

    /// <summary>
    /// Crea una respuesta paginada con links desde una lista completa
    /// </summary>
    public static PagedResponseWithLinks<T> CreateWithLinks<T>(
        List<T> allItems,
        int pageNumber,
        int pageSize,
        string baseUrl)
    {
        var totalCount = allItems.Count;
        var items = allItems
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PagedResponseWithLinks<T>(items, totalCount, pageNumber, pageSize, baseUrl);
    }

    /// <summary>
    /// Crea una respuesta paginada vacía
    /// </summary>
    public static PagedResponse<T> CreateEmpty<T>(int pageNumber, int pageSize)
    {
        return new PagedResponse<T>(new List<T>(), 0, pageNumber, pageSize);
    }
}
