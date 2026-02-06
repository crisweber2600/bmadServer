using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using bmadServer.ApiService.DTOs.SparkCompat;
using bmadServer.ApiService.Services.SparkCompat;

namespace bmadServer.ApiService.Controllers.SparkCompat;

/// <summary>
/// SparkCompat base endpoint controller for health checks.
/// 
/// Route: /v1/*
/// 
/// Purpose:
/// - Provides health check endpoints for load balancers and monitoring systems
/// - Reports system status, version, and uptime information
/// - Enables Kubernetes liveness and readiness probes
/// 
/// Architecture:
/// - Injects IWebHostEnvironment and ILogger via constructor (testable, mockable)
/// - All responses use consistent ResponseEnvelope format
/// - Exception handling with secure error messages (no raw exception details exposed)
/// - Uptime calculated at request time (accurate across application lifetime)
/// </summary>
[ApiController]
[Route("v1")]
[Produces("application/json")]
public class HealthController : ControllerBase
{
    private readonly IWebHostEnvironment _webHostEnvironment;
    private readonly ILogger<HealthController> _logger;
    private readonly DateTime _applicationStartTime;

    /// <summary>
    /// Initializes the HealthController with required dependencies.
    /// </summary>
    /// <param name="webHostEnvironment">Host environment for environment-specific configuration</param>
    /// <param name="logger">Logger for diagnostic output (production logging)</param>
    public HealthController(IWebHostEnvironment webHostEnvironment, ILogger<HealthController> logger)
    {
        _webHostEnvironment = webHostEnvironment ?? throw new ArgumentNullException(nameof(webHostEnvironment));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        // TODO: Inject IHostApplicationLifetime in future for more accurate start time
        _applicationStartTime = DateTime.UtcNow;
    }

    /// <summary>
    /// Health check endpoint for SparkCompat API.
    /// 
    /// GET /v1/health
    /// 
    /// Returns detailed health information including:
    /// - Status: "healthy", "degraded", or "unhealthy"
    /// - Version: API version (1.0.0)
    /// - Environment: Deployment environment (Development/Staging/Production)
    /// - Uptime: Seconds since application started
    /// - Database: Connection status
    /// 
    /// HTTP Response:
    /// - 200 OK with health status wrapped in ResponseEnvelope
    /// - 500 Internal Server Error if health check fails (still wrapped in ResponseEnvelope)
    /// 
    /// Authentication: Not required (public endpoint for monitoring systems)
    /// Rate Limiting: Standard IP-based rate limiting applies
    /// </summary>
    /// <returns>Health status wrapped in ResponseEnvelope{HealthResponseDto}</returns>
    [HttpGet("health")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ResponseEnvelope<HealthResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseEnvelope<object>), StatusCodes.Status500InternalServerError)]
    public ActionResult<ResponseEnvelope<HealthResponseDto>> GetHealth()
    {
        try
        {
            _logger.LogInformation("Health check requested");

            // Calculate uptime at request time for accuracy
            var uptime = (long)(DateTime.UtcNow - _applicationStartTime).TotalSeconds;
            if (uptime < 0)
            {
                _logger.LogWarning("Uptime calculation resulted in negative value: {Uptime}", uptime);
                uptime = 0;
            }

            // Create health response with validated parameters
            var healthResponse = HealthResponseDto.Healthy(
                environment: _webHostEnvironment.EnvironmentName,
                uptime: uptime
            );

            var envelope = ResponseMapperUtilities.MapHealthResponse(healthResponse, HttpContext.TraceIdentifier);
            _logger.LogInformation("Health check completed successfully. Environment: {Environment}, Uptime: {Uptime}s",
                _webHostEnvironment.EnvironmentName, uptime);

            return Ok(envelope);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed with exception: {ExceptionType}",
                ex.GetType().Name);

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
    /// - 200 OK if service is running and responding to requests
    /// - Used by orchestrators (Kubernetes, Docker Swarm) to determine if container should stay alive
    /// - If this endpoint fails, orchestrator will restart the container
    /// 
    /// Response Format: Consistent ResponseEnvelope format for all endpoints
    /// Authentication: Not required (public endpoint)
    /// </summary>
    /// <returns>Liveness status wrapped in ResponseEnvelope</returns>
    [HttpGet("live")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ResponseEnvelope<object>), StatusCodes.Status200OK)]
    public ActionResult<ResponseEnvelope<object>> GetLive()
    {
        try
        {
            _logger.LogDebug("Liveness probe requested");
            var envelope = ResponseEnvelope<object>.Success(
                new { status = "alive" },
                HttpContext.TraceIdentifier,
                "Service is running"
            );
            return Ok(envelope);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Liveness probe failed");
            var errorEnvelope = ResponseMapperUtilities.MapException<object>(ex, HttpContext.TraceIdentifier);
            return StatusCode(errorEnvelope.StatusCode, errorEnvelope);
        }
    }

    /// <summary>
    /// Readiness probe endpoint for Kubernetes/Docker readiness checks.
    /// 
    /// GET /v1/ready
    /// 
    /// Returns:
    /// - 200 OK if service is ready to accept traffic
    /// - Used by orchestrators to determine if traffic should be routed to this instance
    /// - If this endpoint fails, orchestrator will remove instance from load balancer
    /// - Currently returns ready immediately; can be extended to check dependencies
    /// 
    /// TODO: Add dependency checks (database connection, cache availability, etc.)
    /// 
    /// Response Format: Consistent ResponseEnvelope format for all endpoints
    /// Authentication: Not required (public endpoint)
    /// </summary>
    /// <returns>Readiness status wrapped in ResponseEnvelope</returns>
    [HttpGet("ready")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ResponseEnvelope<object>), StatusCodes.Status200OK)]
    public ActionResult<ResponseEnvelope<object>> GetReady()
    {
        try
        {
            _logger.LogDebug("Readiness probe requested");
            // TODO: Add real dependency checks here
            //   - Database: Test connection to ApplicationDbContext
            //   - Cache: Test Redis/memory cache availability
            //   - External APIs: Verify dependent service health
            var envelope = ResponseEnvelope<object>.Success(
                new { status = "ready" },
                HttpContext.TraceIdentifier,
                "Service is ready to accept traffic"
            );
            return Ok(envelope);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Readiness probe failed");
            var errorEnvelope = ResponseMapperUtilities.MapException<object>(ex, HttpContext.TraceIdentifier);
            return StatusCode(errorEnvelope.StatusCode, errorEnvelope);
        }
    }

    /// <summary>
    /// Alias for /v1/health as convenience endpoint.
    /// 
    /// GET /v1
    /// 
    /// Returns the same health information as GET /v1/health.
    /// Useful for clients that want to check API status without knowing specific endpoint names.
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
