using System.Security.Claims;
using bmadServer.ApiService.Data;
using bmadServer.ApiService.Data.Entities;
using bmadServer.ApiService.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace bmadServer.ApiService.Controllers;

/// <summary>
/// User management controller
/// </summary>
[ApiController]
[Route("api/v1/users")]
public class UsersController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<UsersController> _logger;
    private readonly IMemoryCache _memoryCache;

    public UsersController(
        ApplicationDbContext dbContext,
        ILogger<UsersController> logger,
        IMemoryCache memoryCache)
    {
        _dbContext = dbContext;
        _logger = logger;
        _memoryCache = memoryCache;
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

        // Return user profile
        var response = new UserResponse
        {
            Id = user.Id,
            Email = user.Email,
            DisplayName = user.DisplayName,
            CreatedAt = user.CreatedAt,
            PersonaType = user.PersonaType
        };

        return Ok(response);
    }

    /// <summary>
    /// Update current user's persona preferences
    /// </summary>
    /// <param name="request">Persona preference update request</param>
    /// <returns>Updated user profile</returns>
    /// <response code="200">Persona preferences updated successfully</response>
    /// <response code="400">Invalid request data</response>
    /// <response code="401">User is not authenticated</response>
    /// <response code="404">User not found</response>
    [HttpPatch("me/persona")]
    [Authorize]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdatePersona([FromBody] UpdatePersonaRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        // Extract user ID from JWT claims
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

        // Update persona preference
        if (!request.PersonaType.HasValue)
        {
            return BadRequest("PersonaType is required");
        }
        
        user.PersonaType = request.PersonaType.Value;
        user.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("User {UserId} updated persona to {PersonaType}", userId, request.PersonaType);

        // Return updated profile
        var response = new UserResponse
        {
            Id = user.Id,
            Email = user.Email,
            DisplayName = user.DisplayName,
            CreatedAt = user.CreatedAt,
            PersonaType = user.PersonaType
        };

        return Ok(response);
    }

    /// <summary>
    /// Get user profile by ID (Story 7.3 - for attribution display)
    /// </summary>
    /// <remarks>
    /// Users can only view their own profile. Admins can view any profile.
    /// </remarks>
    /// <param name="id">User ID</param>
    /// <returns>User profile information</returns>
    /// <response code="200">Returns the user's profile</response>
    /// <response code="401">User is not authenticated</response>
    /// <response code="403">User not authorized to view this profile</response>
    /// <response code="404">User not found</response>
    [HttpGet("{id}/profile")]
    [Authorize]
    [ProducesResponseType(typeof(UserProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUserProfile(Guid id)
    {
        // Authorization: Users can only view their own profile (or admins can view any)
        var currentUserIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(currentUserIdClaim) || !Guid.TryParse(currentUserIdClaim, out var currentUserId))
        {
            return Unauthorized(new ProblemDetails
            {
                Type = "https://bmadserver.api/errors/unauthorized",
                Title = "Unauthorized",
                Status = StatusCodes.Status401Unauthorized,
                Detail = "User ID not found in claims"
            });
        }

        // Check if requesting own profile or is admin
        if (currentUserId != id)
        {
            var isAdmin = await _dbContext.UserRoles
                .AnyAsync(ur => ur.UserId == currentUserId && ur.Role == Role.Admin);
            
            if (!isAdmin)
            {
                return Problem(
                    statusCode: StatusCodes.Status403Forbidden,
                    title: "Forbidden",
                    detail: "You can only view your own profile");
            }
        }

        // Try to get from cache first (5-minute TTL)
        var cacheKey = $"UserProfile_{id}";
        if (_memoryCache.TryGetValue<UserProfileResponse>(cacheKey, out var cachedProfile))
        {
            return Ok(cachedProfile);
        }

        // Lookup user in database
        var user = await _dbContext.Users.FindAsync(id);

        if (user == null)
        {
            return NotFound(new ProblemDetails
            {
                Type = "https://bmadserver.api/errors/user-not-found",
                Title = "User Not Found",
                Status = StatusCodes.Status404NotFound,
                Detail = $"User {id} does not exist or is not accessible"
            });
        }

        // Get user role (default to "Participant" if no role assigned)
        var userRoleEnum = await _dbContext.UserRoles
            .Where(ur => ur.UserId == id)
            .Select(ur => ur.Role)
            .FirstOrDefaultAsync();
        
        var roleString = userRoleEnum != default 
            ? userRoleEnum.ToString() 
            : "Participant";

        // Build response
        var profile = new UserProfileResponse
        {
            UserId = user.Id,
            DisplayName = user.DisplayName,
            AvatarUrl = null, // MVP: Phase 2 feature
            JoinedAt = user.CreatedAt,
            Role = roleString
        };

        // Cache for 5 minutes
        _memoryCache.Set(cacheKey, profile, TimeSpan.FromMinutes(5));

        return Ok(profile);
    }
}
