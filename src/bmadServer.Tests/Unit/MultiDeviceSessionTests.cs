using bmadServer.ApiService.Configuration;
using bmadServer.ApiService.Data;
using bmadServer.ApiService.Data.Entities;
using bmadServer.ApiService.Models;
using bmadServer.ApiService.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace bmadServer.Tests.Unit;

public class MultiDeviceSessionTests
{
    private readonly Mock<ILogger<SessionService>> _loggerMock;
    private readonly IOptions<SessionSettings> _sessionSettings;

    public MultiDeviceSessionTests()
    {
        _loggerMock = new Mock<ILogger<SessionService>>();
        _sessionSettings = Options.Create(new SessionSettings
        {
            RecoveryWindowSeconds = 60,
            IdleTimeoutMinutes = 30,
            WarningTimeoutMinutes = 28
        });
    }

    private ApplicationDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task User_Should_Have_Separate_Sessions_Per_Device()
    {
        // Arrange
        await using var dbContext = CreateInMemoryDbContext();
        var service = new SessionService(dbContext, _loggerMock.Object, _sessionSettings);

        var user = new User
        {
            Email = "test@example.com",
            PasswordHash = "hash",
            DisplayName = "Test User"
        };
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        // Act - User connects from laptop
        var laptopSession = await service.CreateSessionAsync(user.Id, "laptop-conn");
        
        // Simulate some time passing (enough to be outside recovery window)
        var session = await dbContext.Sessions.FindAsync(laptopSession.Id);
        session!.LastActivityAt = DateTime.UtcNow.AddSeconds(-65);
        await dbContext.SaveChangesAsync();

        // User connects from mobile
        var (mobileSession, _) = await service.RecoverSessionAsync(user.Id, "mobile-conn");

        // Assert
        var allSessions = await dbContext.Sessions
            .Where(s => s.UserId == user.Id)
            .ToListAsync();

        Assert.Equal(2, allSessions.Count); // Two separate sessions
        Assert.NotEqual(laptopSession.Id, mobileSession.Id);
        
        // Old session should be inactive with cleared connection
        var laptopCheck = allSessions.First(s => s.Id == laptopSession.Id);
        Assert.False(laptopCheck.IsActive);
        Assert.Null(laptopCheck.ConnectionId); // Cleared when marked inactive
        
        // New session should be active
        var mobileCheck = allSessions.First(s => s.Id == mobileSession.Id);
        Assert.True(mobileCheck.IsActive);
        Assert.Equal("mobile-conn", mobileCheck.ConnectionId);
    }

    [Fact]
    public async Task Workflow_State_Should_Sync_Across_Devices()
    {
        // Arrange
        await using var dbContext = CreateInMemoryDbContext();
        var service = new SessionService(dbContext, _loggerMock.Object, _sessionSettings);

        var user = new User
        {
            Email = "test@example.com",
            PasswordHash = "hash",
            DisplayName = "Test User"
        };
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        // Device 1 creates session and updates workflow state
        var session1 = await service.CreateSessionAsync(user.Id, "device1-conn");
        await service.UpdateSessionStateAsync(session1.Id, user.Id, s =>
        {
            s.WorkflowState = new WorkflowState
            {
                WorkflowName = "create-prd",
                CurrentStep = 3,
                PendingInput = "Feature list confirmation"
            };
        });

        // Simulate time passing (outside recovery window, within idle timeout)
        var s1 = await dbContext.Sessions.FindAsync(session1.Id);
        s1!.LastActivityAt = DateTime.UtcNow.AddSeconds(-65);
        await dbContext.SaveChangesAsync();

        // Act - Device 2 connects and recovers state
        var (session2, isRecovered) = await service.RecoverSessionAsync(user.Id, "device2-conn");

        // Assert - Workflow state from device 1 is restored on device 2
        Assert.NotNull(session2);
        Assert.True(isRecovered);
        Assert.NotEqual(session1.Id, session2.Id); // New session (outside 60s window)
        Assert.NotNull(session2.WorkflowState);
        Assert.Equal("create-prd", session2.WorkflowState.WorkflowName);
        Assert.Equal(3, session2.WorkflowState.CurrentStep);
        Assert.Equal("Feature list confirmation", session2.WorkflowState.PendingInput);
    }

    [Fact]
    public async Task Last_Write_Wins_For_Concurrent_Updates()
    {
        // Arrange
        await using var dbContext = CreateInMemoryDbContext();
        var service = new SessionService(dbContext, _loggerMock.Object, _sessionSettings);

        var user = new User
        {
            Email = "test@example.com",
            PasswordHash = "hash",
            DisplayName = "Test User"
        };
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var session = await service.CreateSessionAsync(user.Id, "conn-123");

        // Initialize workflow state
        await service.UpdateSessionStateAsync(session.Id, user.Id, s =>
        {
            s.WorkflowState = new WorkflowState
            {
                WorkflowName = "create-prd",
                CurrentStep = 1
            };
        });

        // Act - Simulate concurrent updates (last write wins)
        await service.UpdateSessionStateAsync(session.Id, user.Id, s =>
        {
            s.WorkflowState!.CurrentStep = 2; // First update
        });

        await service.UpdateSessionStateAsync(session.Id, user.Id, s =>
        {
            s.WorkflowState!.CurrentStep = 3; // Second update (wins)
        });

        // Assert
        var finalSession = await dbContext.Sessions.FindAsync(session.Id);
        Assert.NotNull(finalSession);
        Assert.NotNull(finalSession.WorkflowState);
        Assert.Equal(3, finalSession.WorkflowState.CurrentStep); // Last write wins
        Assert.Equal(4, finalSession.WorkflowState._version); // Version incremented
    }

    [Fact]
    public async Task Most_Recent_Session_Should_Be_Recovered()
    {
        // Arrange
        await using var dbContext = CreateInMemoryDbContext();
        var service = new SessionService(dbContext, _loggerMock.Object, _sessionSettings);

        var user = new User
        {
            Email = "test@example.com",
            PasswordHash = "hash",
            DisplayName = "Test User"
        };
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        // Create multiple sessions at different times
        var oldSession = await service.CreateSessionAsync(user.Id, "old-conn");
        await service.UpdateSessionStateAsync(oldSession.Id, user.Id, s =>
        {
            s.WorkflowState = new WorkflowState { WorkflowName = "old-workflow", CurrentStep = 1 };
        });

        await Task.Delay(50);

        var recentSession = await service.CreateSessionAsync(user.Id, "recent-conn");
        await service.UpdateSessionStateAsync(recentSession.Id, user.Id, s =>
        {
            s.WorkflowState = new WorkflowState { WorkflowName = "recent-workflow", CurrentStep = 5 };
        });

        // Both sessions are outside recovery window, but recentSession has later LastActivityAt
        var s1 = await dbContext.Sessions.FindAsync(oldSession.Id);
        s1!.LastActivityAt = DateTime.UtcNow.AddSeconds(-70);
        var s2 = await dbContext.Sessions.FindAsync(recentSession.Id);
        s2!.LastActivityAt = DateTime.UtcNow.AddSeconds(-65);
        await dbContext.SaveChangesAsync();

        // Act - Recover should get most recent session's state
        var (recoveredSession, isRecovered) = await service.RecoverSessionAsync(user.Id, "new-conn");

        // Assert - Should recover the most recent workflow state
        Assert.NotNull(recoveredSession);
        Assert.True(isRecovered);
        Assert.NotNull(recoveredSession.WorkflowState);
        Assert.Equal("recent-workflow", recoveredSession.WorkflowState.WorkflowName);
        Assert.Equal(5, recoveredSession.WorkflowState.CurrentStep);
    }
}
