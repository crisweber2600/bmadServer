using bmadServer.ApiService.Data;
using bmadServer.ApiService.Data.Entities;
using bmadServer.ApiService.DTOs;
using bmadServer.ApiService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace bmadServer.ApiService.Controllers;

/// <summary>
/// Role management controller for assigning and managing user roles
/// </summary>
[ApiController]
[Route("api/v1/users/{userId}/roles")]
[Authorize]
public class RolesController : ControllerBase
{
    private readonly IRoleService _roleService;
    private readonly ApplicationDbContext _dbContext;

    public RolesController(IRoleService roleService, ApplicationDbContext dbContext)
    {
        _roleService = roleService;
        _dbContext = dbContext;
    }

    /// <summary>
    /// Get all roles assigned to a user
    /// </summary>
    /// <param name="userId">The unique identifier of the user</param>
    /// <returns>User roles information</returns>
    /// <response code="200">Returns the user's roles</response>
    /// <response code="404">User not found</response>
    [HttpGet]
    public async Task<ActionResult<UserRolesResponse>> GetUserRoles(Guid userId)
    {
        var user = await _dbContext.Users.FindAsync(userId);
        if (user == null)
        {
            return NotFound(new ProblemDetails
            {
                Type = "https://bmadserver.dev/errors/user-not-found",
                Title = "User Not Found",
                Status = StatusCodes.Status404NotFound,
                Detail = "The specified user does not exist"
            });
        }

        var roles = await _roleService.GetUserRolesAsync(userId);
        
        return Ok(new UserRolesResponse
        {
            UserId = userId,
            Roles = roles.Select(r => r.ToString()).ToList()
        });
    }

    /// <summary>
    /// Assign a role to a user (Admin only)
    /// </summary>
    /// <param name="userId">The unique identifier of the user</param>
    /// <param name="request">The role assignment request</param>
    /// <returns>Updated user roles information</returns>
    /// <response code="200">Role assigned successfully</response>
    /// <response code="400">Invalid request data</response>
    /// <response code="401">Not authenticated</response>
    /// <response code="403">Not authorized (requires Admin role)</response>
    /// <response code="404">User not found</response>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<UserRolesResponse>> AssignRole(
        Guid userId, 
        [FromBody] AssignRoleRequest request)
    {
        var user = await _dbContext.Users.FindAsync(userId);
        if (user == null)
        {
            return NotFound(new ProblemDetails
            {
                Type = "https://bmadserver.dev/errors/user-not-found",
                Title = "User Not Found",
                Status = StatusCodes.Status404NotFound,
                Detail = "The specified user does not exist"
            });
        }

        var adminIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        Guid? adminId = adminIdClaim != null ? Guid.Parse(adminIdClaim) : null;

        await _roleService.AssignRoleAsync(userId, request.Role, adminId);
        
        var roles = await _roleService.GetUserRolesAsync(userId);
        
        return Ok(new UserRolesResponse
        {
            UserId = userId,
            Roles = roles.Select(r => r.ToString()).ToList()
        });
    }

    /// <summary>
    /// Remove a role from a user (Admin only)
    /// </summary>
    /// <param name="userId">The unique identifier of the user</param>
    /// <param name="role">The role to remove</param>
    /// <returns>No content on success</returns>
    /// <response code="204">Role removed successfully</response>
    /// <response code="400">Cannot remove role (e.g., last role or removing own admin role)</response>
    /// <response code="401">Not authenticated</response>
    /// <response code="403">Not authorized (requires Admin role)</response>
    /// <response code="404">User not found</response>
    [HttpDelete("{role}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> RemoveRole(Guid userId, Role role)
    {
        var user = await _dbContext.Users.FindAsync(userId);
        if (user == null)
        {
            return NotFound(new ProblemDetails
            {
                Type = "https://bmadserver.dev/errors/user-not-found",
                Title = "User Not Found",
                Status = StatusCodes.Status404NotFound,
                Detail = "The specified user does not exist"
            });
        }

        var currentUserIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (currentUserIdClaim != null && Guid.Parse(currentUserIdClaim) == userId && role == Role.Admin)
        {
            return BadRequest(new ProblemDetails
            {
                Type = "https://bmadserver.dev/errors/cannot-remove-own-admin",
                Title = "Cannot Remove Own Admin Role",
                Status = StatusCodes.Status400BadRequest,
                Detail = "You cannot remove the Admin role from yourself"
            });
        }

        try
        {
            await _roleService.RemoveRoleAsync(userId, role);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ProblemDetails
            {
                Type = "https://bmadserver.dev/errors/cannot-remove-last-role",
                Title = "Cannot Remove Last Role",
                Status = StatusCodes.Status400BadRequest,
                Detail = ex.Message
            });
        }
    }
}
