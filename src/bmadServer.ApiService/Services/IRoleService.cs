using bmadServer.ApiService.Data.Entities;

namespace bmadServer.ApiService.Services;

public interface IRoleService
{
    Task<IEnumerable<Role>> GetUserRolesAsync(Guid userId);
    Task AssignRoleAsync(Guid userId, Role role, Guid? assignedBy = null);
    Task RemoveRoleAsync(Guid userId, Role role);
    Task AssignDefaultRoleAsync(Guid userId);
    Task<bool> UserHasRoleAsync(Guid userId, Role role);
}
