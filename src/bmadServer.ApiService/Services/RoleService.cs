using bmadServer.ApiService.Data;
using bmadServer.ApiService.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace bmadServer.ApiService.Services;

public class RoleService : IRoleService
{
    private readonly ApplicationDbContext _dbContext;

    public RoleService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
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
            return;

        var userRole = new UserRole
        {
            UserId = userId,
            Role = role,
            AssignedAt = DateTime.UtcNow,
            AssignedBy = assignedBy
        };

        _dbContext.UserRoles.Add(userRole);
        await _dbContext.SaveChangesAsync();
    }

    public async Task RemoveRoleAsync(Guid userId, Role role)
    {
        var userRole = await _dbContext.UserRoles
            .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.Role == role);

        if (userRole == null)
            return;

        var remainingRoles = await _dbContext.UserRoles
            .CountAsync(ur => ur.UserId == userId);

        if (remainingRoles <= 1)
            throw new InvalidOperationException("Cannot remove the last role from a user");

        _dbContext.UserRoles.Remove(userRole);
        await _dbContext.SaveChangesAsync();
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
