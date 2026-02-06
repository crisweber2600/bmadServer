using System.Text.Json.Serialization;

namespace bmadServer.ApiService.DTOs.SparkCompat;

/// <summary>
/// Response model for the /v1/health endpoint.
/// 
/// Purpose:
/// - Reports health status of the SparkCompat compatibility layer
/// - Includes version and environment information
/// - Used by load balancers and monitoring systems
/// </summary>
public class HealthResponseDto
{
    /// <summary>
    /// Health status: "healthy", "degraded", or "unhealthy".
    /// </summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = "healthy";

    /// <summary>
    /// Version of the SparkCompat API module.
    /// </summary>
    [JsonPropertyName("version")]
    public string Version { get; set; } = "1.0.0";

    /// <summary>
    /// Environment where the service is running: Development, Staging, Production.
    /// </summary>
    [JsonPropertyName("environment")]
    public string Environment { get; set; } = "Development";

    /// <summary>
    /// System uptime in seconds since application started.
    /// </summary>
    [JsonPropertyName("uptimeSeconds")]
    public long UptimeSeconds { get; set; }

    /// <summary>
    /// Timestamp when health check was performed (ISO 8601).
    /// </summary>
    [JsonPropertyName("checkedAt")]
    public DateTime CheckedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Health status of database connection.
    /// </summary>
    [JsonPropertyName("database")]
    public string Database { get; set; } = "healthy";

    /// <summary>
    /// Factory method: Creates a healthy response for the current environment.
    /// Validates all input parameters.
    /// </summary>
    /// <param name="environment">Current environment name (required, non-empty)</param>
    /// <param name="uptime">Uptime in seconds since application started (must be >= 0)</param>
    /// <returns>A HealthResponseDto with validated data</returns>
    /// <exception cref="ArgumentNullException">If environment is null</exception>
    /// <exception cref="ArgumentException">If environment is empty</exception>
    /// <exception cref="ArgumentOutOfRangeException">If uptime is negative</exception>
    public static HealthResponseDto Healthy(string environment, long uptime)
    {
        ArgumentNullException.ThrowIfNull(environment);
        if (string.IsNullOrWhiteSpace(environment))
        {
            throw new ArgumentException("Environment name cannot be empty", nameof(environment));
        }
        if (uptime < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(uptime), "Uptime cannot be negative");
        }

        return new HealthResponseDto
        {
            Status = "healthy",
            Version = "1.0.0",
            Environment = environment,
            UptimeSeconds = uptime,
            Database = "healthy",
            CheckedAt = DateTime.UtcNow
        };
    }
}
