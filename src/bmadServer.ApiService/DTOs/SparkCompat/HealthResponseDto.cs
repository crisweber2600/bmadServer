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
    /// Creates a healthy response for the current environment.
    /// </summary>
    /// <param name="environment">Current environment name</param>
    /// <param name="uptime">Uptime in seconds</param>
    /// <returns>A HealthResponseDto indicating healthy status</returns>
    public static HealthResponseDto Healthy(string environment, long uptime)
    {
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
