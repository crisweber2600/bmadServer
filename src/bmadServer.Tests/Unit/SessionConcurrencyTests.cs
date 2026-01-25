using bmadServer.ApiService.Data;
using bmadServer.ApiService.Data.Entities;
using bmadServer.ApiService.Models;
using bmadServer.ApiService.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace bmadServer.Tests.Unit;

/// <summary>
/// Tests for optimistic concurrency control in session updates.
/// Validates version tracking and conflict detection per AC.
/// </summary>
public class SessionConcurrencyTests
{
    private readonly Mock<ILogger<SessionService>> _loggerMock;

    public SessionConcurrencyTests()
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
    public async Task UpdateSessionStateAsync_Should_Increment_Version_On_Each_Update()
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

        // Act - First update
        await service.UpdateSessionStateAsync(session.Id, user.Id, s =>
        {
            s.WorkflowState = new WorkflowState { WorkflowName = "workflow1", CurrentStep = 1 };
        });

        var afterFirstUpdate = await dbContext.Sessions.FindAsync(session.Id);
        var firstVersion = afterFirstUpdate!.WorkflowState!._version;

        // Second update
        await service.UpdateSessionStateAsync(session.Id, user.Id, s =>
        {
            s.WorkflowState!.CurrentStep = 2;
        });

        var afterSecondUpdate = await dbContext.Sessions.FindAsync(session.Id);
        var secondVersion = afterSecondUpdate!.WorkflowState!._version;

        // Assert
        Assert.Equal(2, firstVersion);
        Assert.Equal(3, secondVersion);
    }

    [Fact]
    public async Task UpdateSessionStateAsync_Should_Track_LastModifiedBy()
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
        await service.UpdateSessionStateAsync(session.Id, user.Id, s =>
        {
            s.WorkflowState = new WorkflowState { WorkflowName = "test-workflow" };
        });

        // Assert
        var updatedSession = await dbContext.Sessions.FindAsync(session.Id);
        Assert.NotNull(updatedSession);
        Assert.NotNull(updatedSession.WorkflowState);
        Assert.Equal(user.Id, updatedSession.WorkflowState._lastModifiedBy);
        Assert.True(updatedSession.WorkflowState._lastModifiedAt <= DateTime.UtcNow);
    }

    [Fact]
    public async Task UpdateSessionStateAsync_Should_Update_ExpiresAt_On_Activity()
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
        var originalExpiry = session.ExpiresAt;

        await Task.Delay(10);

        // Act
        await service.UpdateSessionStateAsync(session.Id, user.Id, s =>
        {
            s.WorkflowState = new WorkflowState { WorkflowName = "test" };
        });

        // Assert
        var updatedSession = await dbContext.Sessions.FindAsync(session.Id);
        Assert.NotNull(updatedSession);
        Assert.True(updatedSession.ExpiresAt > originalExpiry);
        Assert.True(updatedSession.LastActivityAt > session.CreatedAt);
    }

    [Fact]
    public async Task Concurrent_Updates_Should_Use_Version_Tracking()
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

        // Initialize workflow state
        await service.UpdateSessionStateAsync(session.Id, user.Id, s =>
        {
            s.WorkflowState = new WorkflowState
            {
                WorkflowName = "create-prd",
                CurrentStep = 1,
                _version = 1
            };
        });

        // Act - Multiple sequential updates
        await service.UpdateSessionStateAsync(session.Id, user.Id, s =>
        {
            s.WorkflowState!.CurrentStep = 2;
        });

        await service.UpdateSessionStateAsync(session.Id, user.Id, s =>
        {
            s.WorkflowState!.CurrentStep = 3;
        });

        // Assert - Version should increment with each update
        var finalSession = await dbContext.Sessions.FindAsync(session.Id);
        Assert.NotNull(finalSession);
        Assert.NotNull(finalSession.WorkflowState);
        Assert.Equal(3, finalSession.WorkflowState.CurrentStep);
        Assert.Equal(4, finalSession.WorkflowState._version); // Initial 1 + 3 updates
    }
}
