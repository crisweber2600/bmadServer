using bmadServer.ApiService.Data;
using bmadServer.ApiService.Data.Entities;
using bmadServer.ApiService.Services;
using bmadServer.Tests.Helpers;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace bmadServer.Tests.Unit;

public class RoleServiceTests : IDisposable
{
    private readonly ApplicationDbContext _dbContext;
    private readonly SqliteConnection _connection;
    private readonly RoleService _roleService;
    private readonly User _testUser;
    private readonly Mock<ILogger<RoleService>> _mockLogger;

    public RoleServiceTests()
    {
        var options = TestDatabaseHelper.CreateSqliteOptions(out _connection);
        _dbContext = new ApplicationDbContext(options);
        _dbContext.Database.EnsureCreated();
        _mockLogger = new Mock<ILogger<RoleService>>();
        _roleService = new RoleService(_dbContext, _mockLogger.Object);

        _testUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            PasswordHash = "hash",
            DisplayName = "Test User"
        };
        _dbContext.Users.Add(_testUser);
        _dbContext.SaveChanges();
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public async Task AssignDefaultRoleAsync_AssignsParticipantRole()
    {
        await _roleService.AssignDefaultRoleAsync(_testUser.Id);

        var roles = await _roleService.GetUserRolesAsync(_testUser.Id);
        Assert.Single(roles);
        Assert.Contains(Role.Participant, roles);
    }

    [Fact]
    public async Task AssignRoleAsync_AddsNewRole()
    {
        await _roleService.AssignRoleAsync(_testUser.Id, Role.Admin);

        var roles = await _roleService.GetUserRolesAsync(_testUser.Id);
        Assert.Single(roles);
        Assert.Contains(Role.Admin, roles);
    }

    [Fact]
    public async Task AssignRoleAsync_DoesNotDuplicateRole()
    {
        await _roleService.AssignRoleAsync(_testUser.Id, Role.Participant);
        await _roleService.AssignRoleAsync(_testUser.Id, Role.Participant);

        var roles = await _roleService.GetUserRolesAsync(_testUser.Id);
        Assert.Single(roles);
    }

    [Fact]
    public async Task AssignRoleAsync_WithAssignedBy_TracksAssigner()
    {
        var adminId = Guid.NewGuid();
        await _roleService.AssignRoleAsync(_testUser.Id, Role.Admin, adminId);

        var userRole = await _dbContext.UserRoles
            .FirstOrDefaultAsync(ur => ur.UserId == _testUser.Id && ur.Role == Role.Admin);

        Assert.NotNull(userRole);
        Assert.Equal(adminId, userRole.AssignedBy);
    }

    [Fact]
    public async Task GetUserRolesAsync_ReturnsAllRoles()
    {
        await _roleService.AssignRoleAsync(_testUser.Id, Role.Admin);
        await _roleService.AssignRoleAsync(_testUser.Id, Role.Participant);
        await _roleService.AssignRoleAsync(_testUser.Id, Role.Viewer);

        var roles = await _roleService.GetUserRolesAsync(_testUser.Id);
        Assert.Equal(3, roles.Count());
        Assert.Contains(Role.Admin, roles);
        Assert.Contains(Role.Participant, roles);
        Assert.Contains(Role.Viewer, roles);
    }

    [Fact]
    public async Task GetUserRolesAsync_ReturnsEmptyForUserWithNoRoles()
    {
        var roles = await _roleService.GetUserRolesAsync(_testUser.Id);
        Assert.Empty(roles);
    }

    [Fact]
    public async Task RemoveRoleAsync_RemovesRole()
    {
        await _roleService.AssignRoleAsync(_testUser.Id, Role.Admin);
        await _roleService.AssignRoleAsync(_testUser.Id, Role.Participant);

        await _roleService.RemoveRoleAsync(_testUser.Id, Role.Admin);

        var roles = await _roleService.GetUserRolesAsync(_testUser.Id);
        Assert.Single(roles);
        Assert.Contains(Role.Participant, roles);
    }

    [Fact]
    public async Task RemoveRoleAsync_ThrowsWhenRemovingLastRole()
    {
        await _roleService.AssignRoleAsync(_testUser.Id, Role.Participant);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _roleService.RemoveRoleAsync(_testUser.Id, Role.Participant));
    }

    [Fact]
    public async Task RemoveRoleAsync_DoesNothingIfRoleNotAssigned()
    {
        await _roleService.AssignRoleAsync(_testUser.Id, Role.Admin);
        await _roleService.AssignRoleAsync(_testUser.Id, Role.Participant);

        await _roleService.RemoveRoleAsync(_testUser.Id, Role.Viewer);

        var roles = await _roleService.GetUserRolesAsync(_testUser.Id);
        Assert.Equal(2, roles.Count());
    }

    [Fact]
    public async Task UserHasRoleAsync_ReturnsTrueWhenUserHasRole()
    {
        await _roleService.AssignRoleAsync(_testUser.Id, Role.Admin);

        var hasRole = await _roleService.UserHasRoleAsync(_testUser.Id, Role.Admin);
        Assert.True(hasRole);
    }

    [Fact]
    public async Task UserHasRoleAsync_ReturnsFalseWhenUserDoesNotHaveRole()
    {
        await _roleService.AssignRoleAsync(_testUser.Id, Role.Participant);

        var hasRole = await _roleService.UserHasRoleAsync(_testUser.Id, Role.Admin);
        Assert.False(hasRole);
    }

    [Fact]
    public async Task AssignDefaultRoleAsync_WhenUserAlreadyHasParticipantRole_DoesNotDuplicate()
    {
        // Assign Participant role directly
        await _roleService.AssignRoleAsync(_testUser.Id, Role.Participant);

        // Try to assign default role (which is also Participant)
        await _roleService.AssignDefaultRoleAsync(_testUser.Id);

        var roles = await _roleService.GetUserRolesAsync(_testUser.Id);
        Assert.Single(roles);
        Assert.Contains(Role.Participant, roles);
    }

    [Fact]
    public async Task AssignRoleAsync_WithInvalidUserId_DoesNotThrow()
    {
        var invalidUserId = Guid.NewGuid();

        // Should not throw, just add the role assignment
        await _roleService.AssignRoleAsync(invalidUserId, Role.Admin);

        var roles = await _roleService.GetUserRolesAsync(invalidUserId);
        Assert.Single(roles);
        Assert.Contains(Role.Admin, roles);
    }

    [Fact]
    public async Task ConcurrentRoleOperations_DoNotCauseRaceCondition()
    {
        // Assign initial roles
        await _roleService.AssignRoleAsync(_testUser.Id, Role.Participant);
        await _roleService.AssignRoleAsync(_testUser.Id, Role.Viewer);
        await _roleService.AssignRoleAsync(_testUser.Id, Role.Admin);

        // Try to remove two roles concurrently (one should fail)
        var task1 = Task.Run(async () =>
        {
            try
            {
                await _roleService.RemoveRoleAsync(_testUser.Id, Role.Participant);
                return true;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
        });

        var task2 = Task.Run(async () =>
        {
            try
            {
                await _roleService.RemoveRoleAsync(_testUser.Id, Role.Viewer);
                return true;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
        });

        var results = await Task.WhenAll(task1, task2);

        // At least one should succeed
        Assert.Contains(true, results);
        
        // Verify user still has at least one role
        var remainingRoles = await _roleService.GetUserRolesAsync(_testUser.Id);
        Assert.NotEmpty(remainingRoles);
    }
}
