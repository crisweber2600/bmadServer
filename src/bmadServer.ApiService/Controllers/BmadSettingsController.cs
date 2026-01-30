using bmadServer.ApiService.DTOs;
using bmadServer.ApiService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace bmadServer.ApiService.Controllers;

/// <summary>
/// API endpoints for BMAD configuration settings.
/// </summary>
[Authorize]
[ApiController]
[Route("api/bmad")]
public class BmadSettingsController : ControllerBase
{
    private readonly IBmadSettingsService _settingsService;
    private readonly ILogger<BmadSettingsController> _logger;

    public BmadSettingsController(IBmadSettingsService settingsService, ILogger<BmadSettingsController> logger)
    {
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets current BMAD settings and available modules/IDEs from manifest.
    /// </summary>
    [HttpGet("settings")]
    public async Task<ActionResult<BmadSettingsResponse>> GetSettings(CancellationToken cancellationToken)
    {
        var response = await _settingsService.GetSettingsAsync(cancellationToken);
        return Ok(response);
    }

    /// <summary>
    /// Updates BMAD settings (base path, manifest paths, modules).
    /// </summary>
    [HttpPut("settings")]
    public async Task<ActionResult<BmadSettingsResponse>> UpdateSettings(
        [FromBody] BmadSettingsRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await _settingsService.UpdateSettingsAsync(request, cancellationToken);
            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid BMAD settings update request");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update BMAD settings");
            return Problem(
                detail: "An error occurred while updating BMAD settings",
                statusCode: 500,
                title: "BMAD Settings Error");
        }
    }
}
