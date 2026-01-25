using bmadServer.ApiService.Data;
using bmadServer.ApiService.Data.Entities;
using bmadServer.ApiService.DTOs;
using bmadServer.ApiService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace bmadServer.ApiService.Controllers;

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
