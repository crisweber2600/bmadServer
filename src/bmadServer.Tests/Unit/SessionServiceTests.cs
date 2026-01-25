using bmadServer.ApiService.Data;
using bmadServer.ApiService.Data.Entities;
using bmadServer.ApiService.Models;
using bmadServer.ApiService.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace bmadServer.Tests.Unit;

/// <summary>
/// Unit tests for SessionService.
/// Tests session lifecycle: create, recover, update, expire.
/// </summary>
public class SessionServiceTests
{
    private readonly Mock<ILogger<SessionService>> _loggerMock;

    public SessionServiceTests()
    {
        _loggerMock = new Mock<ILogger<SessionService>>();
    }

    private ApplicationDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task CreateSessionAsync_Should_Create_New_Session()
    {
        // Arrange
        await using var dbContext = CreateInMemoryDbContext();
        var service = new SessionService(dbContext, _loggerMock.Object);

        var user = new User
        {
            Email = "test@example.com",
            PasswordHash = "hash",
            DisplayName = "Test User"
        };
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        // Act
        var session = await service.CreateSessionAsync(user.Id, "conn-123");

        // Assert
        Assert.NotEqual(Guid.Empty, session.Id);
        Assert.Equal(user.Id, session.UserId);
        Assert.Equal("conn-123", session.ConnectionId);
        Assert.True(session.IsActive);
        Assert.NotEqual(default, session.CreatedAt);
        Assert.NotEqual(default, session.LastActivityAt);
        Assert.True(session.ExpiresAt > session.CreatedAt);
    }

    [Fact]
    public async Task GetActiveSessionAsync_Should_Return_Session_By_ConnectionId()
    {
        // Arrange
        await using var dbContext = CreateInMemoryDbContext();
        var service = new SessionService(dbContext, _loggerMock.Object);

        var user = new User
        {
            Email = "test@example.com",
            PasswordHash = "hash",
            DisplayName = "Test User"
        };
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var session = await service.CreateSessionAsync(user.Id, "conn-123");

        // Act
        var foundSession = await service.GetActiveSessionAsync(user.Id, "conn-123");

        // Assert
        Assert.NotNull(foundSession);
        Assert.Equal(session.Id, foundSession.Id);
    }

    [Fact]
    public async Task GetActiveSessionAsync_Should_Return_Null_For_Inactive_Session()
    {
        // Arrange
        await using var dbContext = CreateInMemoryDbContext();
        var service = new SessionService(dbContext, _loggerMock.Object);

        var user = new User
        {
            Email = "test@example.com",
            PasswordHash = "hash",
            DisplayName = "Test User"
        };
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var session = await service.CreateSessionAsync(user.Id, "conn-123");
        await service.ExpireSessionAsync(session.Id);

        // Act
        var foundSession = await service.GetActiveSessionAsync(user.Id, "conn-123");

        // Assert
        Assert.Null(foundSession);
    }

    [Fact]
    public async Task RecoverSessionAsync_Should_Return_New_Session_When_No_Existing_Session()
    {
        // Arrange
        await using var dbContext = CreateInMemoryDbContext();
        var service = new SessionService(dbContext, _loggerMock.Object);

        var user = new User
        {
            Email = "test@example.com",
            PasswordHash = "hash",
            DisplayName = "Test User"
        };
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        // Act
        var (session, isRecovered) = await service.RecoverSessionAsync(user.Id, "conn-new");

        // Assert
        Assert.NotNull(session);
        Assert.False(isRecovered);
        Assert.Equal("conn-new", session.ConnectionId);
    }

    [Fact]
    public async Task RecoverSessionAsync_Should_Recover_Same_Session_Within_60_Seconds()
    {
        // Arrange
        await using var dbContext = CreateInMemoryDbContext();
        var service = new SessionService(dbContext, _loggerMock.Object);

        var user = new User
        {
            Email = "test@example.com",
            PasswordHash = "hash",
            DisplayName = "Test User"
        };
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var originalSession = await service.CreateSessionAsync(user.Id, "conn-old");
        var originalId = originalSession.Id;

        // Simulate activity within 60 seconds
        originalSession.LastActivityAt = DateTime.UtcNow.AddSeconds(-30);
        await dbContext.SaveChangesAsync();

        // Act - Reconnect within 60 seconds
        var (recoveredSession, isRecovered) = await service.RecoverSessionAsync(user.Id, "conn-new");

        // Assert
        Assert.NotNull(recoveredSession);
        Assert.True(isRecovered);
        Assert.Equal(originalId, recoveredSession.Id); // Same session
        Assert.Equal("conn-new", recoveredSession.ConnectionId); // Updated connection
        Assert.True(recoveredSession.IsActive);
    }

    [Fact]
    public async Task RecoverSessionAsync_Should_Create_New_Session_After_60_Seconds_But_Restore_State()
    {
        // Arrange
        await using var dbContext = CreateInMemoryDbContext();
        var service = new SessionService(dbContext, _loggerMock.Object);

        var user = new User
        {
            Email = "test@example.com",
            PasswordHash = "hash",
            DisplayName = "Test User"
        };
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var originalSession = await service.CreateSessionAsync(user.Id, "conn-old");
        var workflowState = new WorkflowState
        {
            WorkflowName = "create-prd",
            CurrentStep = 5,
            PendingInput = "User confirmation needed"
        };
        
        await service.UpdateSessionStateAsync(originalSession.Id, user.Id, s =>
        {
            s.WorkflowState = workflowState;
        });

        var originalId = originalSession.Id;

        // Simulate disconnect for 61 seconds (outside recovery window, within idle timeout)
        var session = await dbContext.Sessions.FindAsync(originalId);
        session!.LastActivityAt = DateTime.UtcNow.AddSeconds(-61);
        await dbContext.SaveChangesAsync();

        // Act - Reconnect after 61 seconds
        var (recoveredSession, isRecovered) = await service.RecoverSessionAsync(user.Id, "conn-new");

        // Assert
        Assert.NotNull(recoveredSession);
        Assert.True(isRecovered);
        Assert.NotEqual(originalId, recoveredSession.Id); // New session
        Assert.Equal("conn-new", recoveredSession.ConnectionId);
        
        // Workflow state should be restored
        Assert.NotNull(recoveredSession.WorkflowState);
        Assert.Equal("create-prd", recoveredSession.WorkflowState.WorkflowName);
        Assert.Equal(5, recoveredSession.WorkflowState.CurrentStep);
        Assert.Equal("User confirmation needed", recoveredSession.WorkflowState.PendingInput);

        // Old session should be inactive
        var oldSession = await dbContext.Sessions.FindAsync(originalId);
        Assert.NotNull(oldSession);
        Assert.False(oldSession.IsActive);
        Assert.Null(oldSession.ConnectionId);
    }

    [Fact]
    public async Task RecoverSessionAsync_Should_Create_Fresh_Session_After_30_Minutes()
    {
        // Arrange
        await using var dbContext = CreateInMemoryDbContext();
        var service = new SessionService(dbContext, _loggerMock.Object);

        var user = new User
        {
            Email = "test@example.com",
            PasswordHash = "hash",
            DisplayName = "Test User"
        };
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var originalSession = await service.CreateSessionAsync(user.Id, "conn-old");
        var workflowState = new WorkflowState
        {
            WorkflowName = "create-prd",
            CurrentStep = 5
        };
        
        await service.UpdateSessionStateAsync(originalSession.Id, user.Id, s =>
        {
            s.WorkflowState = workflowState;
        });

        // Simulate idle for 31 minutes (past idle timeout)
        var session = await dbContext.Sessions.FindAsync(originalSession.Id);
        session!.LastActivityAt = DateTime.UtcNow.AddMinutes(-31);
        await dbContext.SaveChangesAsync();

        // Act - Reconnect after 31 minutes
        var (recoveredSession, isRecovered) = await service.RecoverSessionAsync(user.Id, "conn-new");

        // Assert
        Assert.NotNull(recoveredSession);
        Assert.False(isRecovered); // Session expired - no recovery
        Assert.NotEqual(originalSession.Id, recoveredSession.Id); // New session
        Assert.Null(recoveredSession.WorkflowState); // No state restoration
    }

    [Fact]
    public async Task UpdateSessionStateAsync_Should_Update_WorkflowState_And_Activity()
    {
        // Arrange
        await using var dbContext = CreateInMemoryDbContext();
        var service = new SessionService(dbContext, _loggerMock.Object);

        var user = new User
        {
            Email = "test@example.com",
            PasswordHash = "hash",
            DisplayName = "Test User"
        };
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var session = await service.CreateSessionAsync(user.Id, "conn-123");

        // Act
        var result = await service.UpdateSessionStateAsync(session.Id, user.Id, s =>
        {
            s.WorkflowState = new WorkflowState
            {
                WorkflowName = "create-epic",
                CurrentStep = 3
            };
        });

        // Assert
        Assert.True(result);

        var updatedSession = await dbContext.Sessions.FindAsync(session.Id);
        Assert.NotNull(updatedSession);
        Assert.NotNull(updatedSession.WorkflowState);
        Assert.Equal("create-epic", updatedSession.WorkflowState.WorkflowName);
        Assert.Equal(3, updatedSession.WorkflowState.CurrentStep);
        Assert.Equal(2, updatedSession.WorkflowState._version); // Incremented
        Assert.Equal(user.Id, updatedSession.WorkflowState._lastModifiedBy);
    }

    [Fact]
    public async Task UpdateSessionStateAsync_Should_Return_False_For_Nonexistent_Session()
    {
        // Arrange
        await using var dbContext = CreateInMemoryDbContext();
        var service = new SessionService(dbContext, _loggerMock.Object);

        // Act
        var result = await service.UpdateSessionStateAsync(Guid.NewGuid(), Guid.NewGuid(), s =>
        {
            s.WorkflowState = new WorkflowState();
        });

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ExpireSessionAsync_Should_Mark_Session_Inactive()
    {
        // Arrange
        await using var dbContext = CreateInMemoryDbContext();
        var service = new SessionService(dbContext, _loggerMock.Object);

        var user = new User
        {
            Email = "test@example.com",
            PasswordHash = "hash",
            DisplayName = "Test User"
        };
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var session = await service.CreateSessionAsync(user.Id, "conn-123");

        // Act
        await service.ExpireSessionAsync(session.Id);

        // Assert
        var expiredSession = await dbContext.Sessions.FindAsync(session.Id);
        Assert.NotNull(expiredSession);
        Assert.False(expiredSession.IsActive);
        Assert.Null(expiredSession.ConnectionId);
    }

    [Fact]
    public async Task UpdateActivityAsync_Should_Update_Timestamps()
    {
        // Arrange
        await using var dbContext = CreateInMemoryDbContext();
        var service = new SessionService(dbContext, _loggerMock.Object);

        var user = new User
        {
            Email = "test@example.com",
            PasswordHash = "hash",
            DisplayName = "Test User"
        };
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var session = await service.CreateSessionAsync(user.Id, "conn-123");
        var originalActivity = session.LastActivityAt;
        var originalExpiry = session.ExpiresAt;

        // Wait a bit to ensure timestamps differ
        await Task.Delay(10);

        // Act
        await service.UpdateActivityAsync(session.Id);

        // Assert
        var updatedSession = await dbContext.Sessions.FindAsync(session.Id);
        Assert.NotNull(updatedSession);
        Assert.True(updatedSession.LastActivityAt > originalActivity);
        Assert.True(updatedSession.ExpiresAt > originalExpiry);
    }

    [Fact]
    public async Task GetMostRecentActiveSessionAsync_Should_Return_Latest_Session()
    {
        // Arrange
        await using var dbContext = CreateInMemoryDbContext();
        var service = new SessionService(dbContext, _loggerMock.Object);

        var user = new User
        {
            Email = "test@example.com",
            PasswordHash = "hash",
            DisplayName = "Test User"
        };
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var session1 = await service.CreateSessionAsync(user.Id, "conn-1");
        await Task.Delay(10); // Ensure different timestamps
        var session2 = await service.CreateSessionAsync(user.Id, "conn-2");

        // Act
        var mostRecent = await service.GetMostRecentActiveSessionAsync(user.Id);

        // Assert
        Assert.NotNull(mostRecent);
        Assert.Equal(session2.Id, mostRecent.Id);
    }

    [Fact]
    public async Task RecoverSessionAsync_Should_Support_Multi_Device_Scenarios()
    {
        // Arrange
        await using var dbContext = CreateInMemoryDbContext();
        var service = new SessionService(dbContext, _loggerMock.Object);

        var user = new User
        {
            Email = "test@example.com",
            PasswordHash = "hash",
            DisplayName = "Test User"
        };
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        // Create session on device 1
        var session1 = await service.CreateSessionAsync(user.Id, "device1-conn");
        
        // Add workflow state
        await service.UpdateSessionStateAsync(session1.Id, user.Id, s =>
        {
            s.WorkflowState = new WorkflowState
            {
                WorkflowName = "create-prd",
                CurrentStep = 3
            };
        });

        // Act - Connect on device 2 (creates separate session)
        var (session2, isRecovered) = await service.RecoverSessionAsync(user.Id, "device2-conn");

        // Assert - Should get the most recent session's state
        Assert.NotNull(session2);
        Assert.True(isRecovered); // Within recovery window
        Assert.Equal(session1.Id, session2.Id); // Same session (within 60s)
        Assert.Equal("device2-conn", session2.ConnectionId); // New connection ID
    }
}
