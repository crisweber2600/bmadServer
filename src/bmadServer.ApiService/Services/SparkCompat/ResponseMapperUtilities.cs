using bmadServer.ApiService.DTOs.SparkCompat;

namespace bmadServer.ApiService.Services.SparkCompat;

/// <summary>
/// Mapper utility class for converting domain models to SparkCompat response envelopes.
/// 
/// Architecture:
/// - Follows adapter pattern to wrap native models into SparkCompat format
/// - Used by SparkCompat controllers to serialize responses consistently
/// - Handles JSON serialization with proper envelope metadata
/// 
/// Design:
/// - Stateless utility class (no dependencies)
/// - Generic over payload type T
/// - Includes factory methods for common response types
/// </summary>
public static class ResponseMapperUtilities
{
    /// <summary>
    /// Maps a generic payload into a SparkCompat ResponseEnvelope.
    /// </summary>
    /// <typeparam name="T">Type of payload</typeparam>
    /// <param name="data">The payload to wrap</param>
    /// <param name="traceId">Optional request trace ID</param>
    /// <param name="message">Optional custom message</param>
    /// <returns>Wrapped response envelope</returns>
    public static ResponseEnvelope<T> MapToEnvelope<T>(T data, string? traceId = null, string message = "Success")
    {
        return ResponseEnvelope<T>.Success(data, traceId, message);
    }

    /// <summary>
    /// Maps a generic payload into a SparkCompat ResponseEnvelope with custom status code.
    /// </summary>
    /// <typeparam name="T">Type of payload</typeparam>
    /// <param name="data">The payload to wrap</param>
    /// <param name="statusCode">HTTP status code</param>
    /// <param name="traceId">Optional request trace ID</param>
    /// <param name="message">Optional custom message</param>
    /// <returns>Wrapped response envelope with custom status</returns>
    public static ResponseEnvelope<T> MapToEnvelope<T>(T data, int statusCode, string? traceId = null, string message = "Success")
    {
        return ResponseEnvelope<T>.Success(data, traceId, message, statusCode);
    }

    /// <summary>
    /// Maps an error into a SparkCompat ResponseEnvelope.
    /// </summary>
    /// <typeparam name="T">Type of payload (will be null in error response)</typeparam>
    /// <param name="statusCode">HTTP status code</param>
    /// <param name="message">Error message</param>
    /// <param name="traceId">Optional request trace ID</param>
    /// <returns>Error response envelope</returns>
    public static ResponseEnvelope<T> MapError<T>(int statusCode, string message, string? traceId = null)
    {
        return ResponseEnvelope<T>.Error(statusCode, message, traceId);
    }

    /// <summary>
    /// Maps a health response into an envelope.
    /// </summary>
    /// <param name="healthResponse">The health response data</param>
    /// <param name="traceId">Optional request trace ID</param>
    /// <returns>Health response envelope</returns>
    public static ResponseEnvelope<HealthResponseDto> MapHealthResponse(HealthResponseDto healthResponse, string? traceId = null)
    {
        return ResponseEnvelope<HealthResponseDto>.Success(healthResponse, traceId, "Health check completed", 200);
    }

    /// <summary>
    /// Maps an exception into an error response envelope.
    /// </summary>
    /// <typeparam name="T">Type of original payload</typeparam>
    /// <param name="ex">The exception that occurred</param>
    /// <param name="traceId">Optional request trace ID</param>
    /// <returns>Error response envelope with exception details</returns>
    public static ResponseEnvelope<T> MapException<T>(Exception ex, string? traceId = null)
    {
        var statusCode = 500;
        var message = ex switch
        {
            ArgumentException => (400, "Invalid argument: " + ex.Message),
            KeyNotFoundException => (404, "Not found: " + ex.Message),
            UnauthorizedAccessException => (401, "Unauthorized"),
            _ => (500, "Internal server error")
        };

        return ResponseEnvelope<T>.Error(message.Item1, message.Item2, traceId);
    }
}
