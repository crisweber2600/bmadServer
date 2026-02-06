using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using bmadServer.ApiService.DTOs.SparkCompat;
using bmadServer.ApiService.Services.SparkCompat;

namespace bmadServer.ApiService.Controllers.SparkCompat;

/// <summary>
/// SparkCompat base endpoint controller for health checks.
/// 
/// Route: /v1/*
/// 
/// Purpose:
/// - Provides health check endpoint for load balancers and monitoring
/// - Serves as foundational endpoint for SparkCompat compatibility API
/// - Reports system status and version information
/// 
/// Architecture:
/// - Uses ResponseEnvelope{T} for consistent response format
/// - Accessible without authentication for monitoring systems
/// - Wraps responses using ResponseMapperUtilities.MapHealthResponse
/// </summary>
[ApiController]
[Route("v1")]
[Produces("application/json")]
public class HealthController : ControllerBase
{
    private static readonly DateTime ApplicationStartTime = DateTime.UtcNow;

    /// <summary>
    /// Health check endpoint for SparkCompat API.
    /// 
    /// GET /v1/health
    /// 
    /// Returns:
    /// - 200 OK with health status details
    /// - Status: "healthy", "degraded", or "unhealthy"
    /// - Includes version, environment, uptime
    /// 
    /// Authentication: Not required (public endpoint)
    /// Rate Limiting: Standard IP-based rate limiting applies
    /// </summary>
    /// <returns>Health status wrapped in ResponseEnvelope</returns>
    [HttpGet("health")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ResponseEnvelope<HealthResponseDto>), StatusCodes.Status200OK)]
    public ActionResult<ResponseEnvelope<HealthResponseDto>> GetHealth()
    {
        try
        {
            var uptime = (long)(DateTime.UtcNow - ApplicationStartTime).TotalSeconds;
            var healthResponse = HealthResponseDto.Healthy(
                environment: HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>().EnvironmentName,
                uptime: uptime
            );

            var envelope = ResponseMapperUtilities.MapHealthResponse(healthResponse, HttpContext.TraceIdentifier);
            return Ok(envelope);
        }
        catch (Exception ex)
        {
            var errorEnvelope = ResponseMapperUtilities.MapException<HealthResponseDto>(ex, HttpContext.TraceIdentifier);
            return StatusCode(errorEnvelope.StatusCode, errorEnvelope);
        }
    }

    /// <summary>
    /// Liveness probe endpoint for Kubernetes/Docker health checks.
    /// 
    /// GET /v1/live
    /// 
    /// Returns:
    /// - 200 OK if service is running
    /// - Used by orchestrators to determine if container should stay alive
    /// 
    /// Authentication: Not required (public endpoint)
    /// </summary>
    /// <returns>Liveness status</returns>
    [HttpGet("live")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetLive()
    {
        return Ok(new { status = "alive" });
    }

    /// <summary>
    /// Readiness probe endpoint for Kubernetes/Docker readiness checks.
    /// 
    /// GET /v1/ready
    /// 
    /// Returns:
    /// - 200 OK if service is ready to accept traffic
    /// - Currently returns ready immediately; can be extended to check dependencies
    /// 
    /// Authentication: Not required (public endpoint)
    /// </summary>
    /// <returns>Readiness status</returns>
    [HttpGet("ready")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetReady()
    {
        // TODO: Add checks for database, cache, and other dependencies
        return Ok(new { status = "ready" });
    }

    /// <summary>
    /// Alias for /v1/health for backwards compatibility.
    /// 
    /// GET /v1
    /// 
    /// Returns the same health information as /v1/health.
    /// </summary>
    /// <returns>Health status wrapped in ResponseEnvelope</returns>
    [HttpGet("")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ResponseEnvelope<HealthResponseDto>), StatusCodes.Status200OK)]
    public ActionResult<ResponseEnvelope<HealthResponseDto>> GetV1Root()
    {
        return GetHealth();
    }
}
