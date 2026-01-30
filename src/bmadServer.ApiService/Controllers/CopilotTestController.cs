using bmadServer.ApiService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace bmadServer.ApiService.Controllers;

/// <summary>
/// API endpoints for testing GitHub Copilot SDK functionality.
/// Provides raw testing of Copilot integration with debug output.
/// </summary>
[Authorize]
[ApiController]
[Route("api/[controller]")]
public class CopilotTestController : ControllerBase
{
    private readonly ICopilotTestService _testService;
    private readonly ILogger<CopilotTestController> _logger;

    public CopilotTestController(ICopilotTestService testService, ILogger<CopilotTestController> logger)
    {
        _testService = testService ?? throw new ArgumentNullException(nameof(testService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Tests Copilot SDK connection with a prompt and returns debug output.
    /// </summary>
    /// <param name="request">Test request containing prompt and optional parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Test response with content, debug log, and event log</returns>
    [HttpPost("test")]
    public async Task<ActionResult<CopilotTestResponse>> TestCopilot(
        [FromBody] CopilotTestRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request?.Prompt))
        {
            return BadRequest(new { error = "Prompt is required" });
        }

        try
        {
            _logger.LogInformation("Starting Copilot test with prompt: {PromptPreview}",
                request.Prompt.Length > 100 ? request.Prompt.Substring(0, 100) + "..." : request.Prompt);

            var result = await _testService.TestCopilotConnectionAsync(request, cancellationToken);

            return Ok(result);
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogWarning(ex, "Copilot test cancelled");
            return StatusCode(408, new
            {
                error = "Test request timed out",
                errorType = "TimeoutException"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Copilot test failed with exception: {Message}", ex.Message);
            return StatusCode(500, new
            {
                error = "Test failed",
                errorType = ex.GetType().Name,
                message = ex.Message
            });
        }
    }

    /// <summary>
    /// Tests Copilot SDK with a health check probe.
    /// Used to verify basic connectivity without sending a user message.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Health check response</returns>
    [HttpGet("health")]
    public async Task<ActionResult<object>> HealthCheck(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Running Copilot health check");

            var request = new CopilotTestRequest
            {
                Prompt = "Respond with 'OK' to confirm connection.",
                SystemMessage = "You are a helpful assistant. Respond concisely."
            };

            var result = await _testService.TestCopilotConnectionAsync(request, cancellationToken);

            return Ok(new
            {
                status = result.Success ? "healthy" : "unhealthy",
                success = result.Success,
                error = result.Error,
                responseTime = (DateTime.UtcNow - result.Timestamp).TotalMilliseconds,
                timedOut = result.TimedOut
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed: {Message}", ex.Message);
            return StatusCode(503, new
            {
                status = "unavailable",
                error = ex.Message,
                errorType = ex.GetType().Name
            });
        }
    }
}
