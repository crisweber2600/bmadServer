using System.Security.Claims;
using bmadServer.ApiService.Data;
using bmadServer.ApiService.DTOs;
using bmadServer.ApiService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace bmadServer.ApiService.Controllers;

/// <summary>
/// User management controller
/// </summary>
[ApiController]
[Route("api/v1/users")]
public class UsersController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IRoleService _roleService;
    private readonly ILogger<UsersController> _logger;

    public UsersController(
        ApplicationDbContext dbContext,
        IRoleService roleService,
        ILogger<UsersController> logger)
    {
        _dbContext = dbContext;
        _roleService = roleService;
        _logger = logger;
    }

    /// <summary>
    /// Get current authenticated user's profile
    /// </summary>
    /// <returns>Current user's profile information</returns>
    /// <response code="200">Returns the current user's profile</response>
    /// <response code="401">User is not authenticated</response>
    /// <response code="404">User not found (should not happen with valid token)</response>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCurrentUser()
    {
        // Extract user ID from JWT claims
        // JWT middleware maps 'sub' claim to both ClaimTypes.NameIdentifier and 'sub'
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new ProblemDetails
            {
                Type = "https://bmadserver.dev/errors/invalid-token",
                Title = "Invalid Token",
                Status = StatusCodes.Status401Unauthorized,
                Detail = "Unable to extract user information from token"
            });
        }

        // Lookup user
        var user = await _dbContext.Users.FindAsync(userId);

        if (user == null)
        {
            _logger.LogWarning("User {UserId} from token not found in database", userId);
            return NotFound(new ProblemDetails
            {
                Type = "https://bmadserver.dev/errors/user-not-found",
                Title = "User Not Found",
                Status = StatusCodes.Status404NotFound,
                Detail = "User account no longer exists"
            });
        }

        var roles = await _roleService.GetUserRolesAsync(userId);

        var response = new UserResponse
        {
            Id = user.Id,
            Email = user.Email,
            DisplayName = user.DisplayName,
            CreatedAt = user.CreatedAt,
            Roles = roles.Select(r => r.ToString()).ToList()
        };

        return Ok(response);
    }
}
