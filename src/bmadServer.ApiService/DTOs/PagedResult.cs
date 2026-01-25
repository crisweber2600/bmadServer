using System.Text.Json.Serialization;

namespace bmadServer.ApiService.DTOs;

/// <summary>
/// Generic wrapper for paginated results
/// </summary>
/// <typeparam name="T">The type of items in the result</typeparam>
public class PagedResult<T>
{
    /// <summary>
    /// The items for the current page
    /// </summary>
    [JsonPropertyName("items")]
    public List<T> Items { get; set; } = new();

    /// <summary>
    /// Current page number (1-based)
    /// </summary>
    [JsonPropertyName("page")]
    public int Page { get; set; }

    /// <summary>
    /// Number of items per page
    /// </summary>
    [JsonPropertyName("pageSize")]
    public int PageSize { get; set; }

    /// <summary>
    /// Total number of items across all pages
    /// </summary>
    [JsonPropertyName("totalItems")]
    public int TotalItems { get; set; }

    /// <summary>
    /// Total number of pages
    /// </summary>
    [JsonPropertyName("totalPages")]
    public int TotalPages { get; set; }

    /// <summary>
    /// Whether there is a previous page
    /// </summary>
    [JsonPropertyName("hasPrevious")]
    public bool HasPrevious { get; set; }

    /// <summary>
    /// Whether there is a next page
    /// </summary>
    [JsonPropertyName("hasNext")]
    public bool HasNext { get; set; }
}
