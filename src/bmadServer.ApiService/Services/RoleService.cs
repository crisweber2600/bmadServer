using bmadServer.ApiService.Data;
using bmadServer.ApiService.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace bmadServer.ApiService.Services;

public class RoleService : IRoleService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<RoleService> _logger;

    public RoleService(ApplicationDbContext dbContext, ILogger<RoleService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<IEnumerable<Role>> GetUserRolesAsync(Guid userId)
    {
        return await _dbContext.UserRoles
            .Where(ur => ur.UserId == userId)
            .Select(ur => ur.Role)
            .ToListAsync();
    }

    public async Task AssignRoleAsync(Guid userId, Role role, Guid? assignedBy = null)
    {
        var existingRole = await _dbContext.UserRoles
            .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.Role == role);

        if (existingRole != null)
        {
            _logger.LogDebug("Role {Role} already assigned to user {UserId}", role, userId);
            return;
        }

        var userRole = new UserRole
        {
            UserId = userId,
            Role = role,
            AssignedAt = DateTime.UtcNow,
            AssignedBy = assignedBy
        };

        _dbContext.UserRoles.Add(userRole);
        await _dbContext.SaveChangesAsync();
        
        _logger.LogInformation("Role {Role} assigned to user {UserId} by {AssignedBy}", 
            role, userId, assignedBy?.ToString() ?? "system");
    }

    public async Task RemoveRoleAsync(Guid userId, Role role)
    {
        // Use transaction to prevent race condition between checking count and removing
        using var transaction = await _dbContext.Database.BeginTransactionAsync();
        
        try
        {
            var userRole = await _dbContext.UserRoles
                .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.Role == role);

            if (userRole == null)
            {
                await transaction.CommitAsync();
                _logger.LogDebug("Role {Role} not assigned to user {UserId}, nothing to remove", role, userId);
                return;
            }

            var remainingRoles = await _dbContext.UserRoles
                .CountAsync(ur => ur.UserId == userId);

            if (remainingRoles <= 1)
            {
                _logger.LogWarning("Attempt to remove last role {Role} from user {UserId} blocked", role, userId);
                throw new InvalidOperationException("Cannot remove the last role from a user");
            }

            _dbContext.UserRoles.Remove(userRole);
            await _dbContext.SaveChangesAsync();
            await transaction.CommitAsync();
            
            _logger.LogInformation("Role {Role} removed from user {UserId}", role, userId);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task AssignDefaultRoleAsync(Guid userId)
    {
        await AssignRoleAsync(userId, Role.Participant);
    }

    public async Task<bool> UserHasRoleAsync(Guid userId, Role role)
    {
        return await _dbContext.UserRoles
            .AnyAsync(ur => ur.UserId == userId && ur.Role == role);
    }
}
