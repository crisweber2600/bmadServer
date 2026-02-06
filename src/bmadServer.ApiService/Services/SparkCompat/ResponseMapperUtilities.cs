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
    /// Maps an exception into a safe error response envelope.
    /// IMPORTANT: Never exposes raw exception messages to clients (OWASP secure coding).
    /// </summary>
    /// <typeparam name="T">Type of original payload</typeparam>
    /// <param name="ex">The exception that occurred</param>
    /// <param name="traceId">Optional request trace ID for correlating logs</param>
    /// <returns>Error response with safe message (no internal details exposed)</returns>
    public static ResponseEnvelope<T> MapException<T>(Exception ex, string? traceId = null)
    {
        // Determine appropriate HTTP status code and SAFE client message
        // NOTE: Never expose raw exception.Message to clients - always use generic messages
        var (statusCode, safeMessage) = ex switch
        {
            ArgumentException => (400, "Invalid request data provided"),
            KeyNotFoundException => (404, "The requested resource was not found"),
            UnauthorizedAccessException => (401, "Authentication required or invalid credentials"),
            _ => (500, "An internal error occurred. Please contact support with your request trace ID")
        };

        // Log the actual exception internally (TODO: inject ILogger for production logging)
        // This ensures debugging without exposing details to clients
        System.Diagnostics.Debug.WriteLine($"[{typeof(T).Name}] Exception: {ex.GetType().Name}: {ex.Message}");

        return ResponseEnvelope<T>.Error(statusCode, safeMessage, traceId);
    }
}
