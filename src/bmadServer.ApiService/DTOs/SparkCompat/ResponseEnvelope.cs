using System.Text.Json.Serialization;

namespace bmadServer.ApiService.DTOs.SparkCompat;

/// <summary>
/// Standard response envelope for SparkCompat API endpoints.
/// 
/// Purpose:
/// - Provides consistent response format across all /v1/* endpoints
/// - Wraps actual payload with metadata (status, timestamp, trace ID)
/// - Validates HTTP status codes (100-599)
/// - Never exposes raw exception details to clients
/// </summary>
/// <typeparam name="T">The type of data being wrapped</typeparam>
public sealed class ResponseEnvelope<T>
{
    /// <summary>
    /// Indicates success or failure of the request.
    /// </summary>
    [JsonPropertyName("success")]
    public bool IsSuccess { get; set; }

    /// <summary>
    /// HTTP status code of the response.
    /// </summary>
    [JsonPropertyName("statusCode")]
    public int StatusCode { get; set; }

    /// <summary>
    /// Human-readable message describing the response.
    /// </summary>
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// The wrapped payload data. Null if response is an error.
    /// </summary>
    [JsonPropertyName("data")]
    public T? Data { get; set; }

    /// <summary>
    /// ISO 8601 timestamp when the response was generated.
    /// </summary>
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Request trace ID for end-to-end tracing and debugging.
    /// </summary>
    [JsonPropertyName("traceId")]
    public string? TraceId { get; set; }

    /// <summary>
    /// Creates a successful response envelope.
    /// </summary>
    /// <param name="data">The payload data</param>
    /// <param name="traceId">Optional trace ID for debugging</param>
    /// <param name="message">Optional success message</param>
    /// <param name="statusCode">Optional HTTP status code (defaults to 200)</param>
    /// <returns>A successful ResponseEnvelope</returns>
    public static ResponseEnvelope<T> Success(T data, string? traceId = null, string message = "Success", int statusCode = 200)
    {
        ValidateStatusCode(statusCode);
        ArgumentNullException.ThrowIfNull(message);
        return new ResponseEnvelope<T>
        {
            IsSuccess = true,
            StatusCode = statusCode,
            Message = message,
            Data = data,
            TraceId = traceId,
            Timestamp = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates an error response envelope.
    /// </summary>
    /// <param name="statusCode">The HTTP status code (100-599)</param>
    /// <param name="message">Safe error message (never contains raw exception details)</param>
    /// <param name="traceId">Optional trace ID for debugging</param>
    /// <returns>An error ResponseEnvelope</returns>
    public static ResponseEnvelope<T> Error(int statusCode, string message, string? traceId = null)
    {
        ValidateStatusCode(statusCode);
        ArgumentNullException.ThrowIfNull(message);
        return new ResponseEnvelope<T>
        {
            IsSuccess = false,
            StatusCode = statusCode,
            Message = message,
            Data = default,
            TraceId = traceId,
            Timestamp = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Validates HTTP status code is within valid range (100-599).
    /// </summary>
    private static void ValidateStatusCode(int statusCode)
    {
        if (statusCode < 100 || statusCode > 599)
        {
            throw new ArgumentOutOfRangeException(nameof(statusCode),
                $"HTTP status code must be between 100 and 599. Got: {statusCode}");
        }
    }
}
