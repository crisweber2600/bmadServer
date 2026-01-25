using bmadServer.ApiService.Data;
using bmadServer.ApiService.Data.Entities;
using bmadServer.ApiService.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace bmadServer.Tests.Integration;

/// <summary>
/// Integration tests for Session persistence.
/// Note: JSONB-specific features (GIN index, complex queries) require PostgreSQL.
/// These tests validate the entity model and basic persistence patterns.
/// </summary>
public class SessionPersistenceIntegrationTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public SessionPersistenceIntegrationTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Session_Should_Persist_WorkflowState()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var user = new User
        {
            Email = $"test-session-{Guid.NewGuid()}@example.com",
            PasswordHash = "hash",
            DisplayName = "Test User"
        };
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var workflowState = new WorkflowState
        {
            WorkflowName = "create-prd",
            CurrentStep = 2,
            ConversationHistory = new List<ChatMessage>
            {
                new() { Id = "msg1", Role = "user", Content = "Start workflow", Timestamp = DateTime.UtcNow }
            },
            DecisionLocks = new Dictionary<string, bool>
            {
                { "features-locked", true }
            },
            PendingInput = "Awaiting confirmation",
            _version = 1,
            _lastModifiedBy = user.Id,
            _lastModifiedAt = DateTime.UtcNow
        };

        var session = new Session
        {
            UserId = user.Id,
            ConnectionId = "conn-123",
            WorkflowState = workflowState
        };

        // Act
        dbContext.Sessions.Add(session);
        await dbContext.SaveChangesAsync();

        // Assert - Reload from database to verify persistence
        var savedSession = await dbContext.Sessions
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == session.Id);

        Assert.NotNull(savedSession);
        Assert.NotNull(savedSession.WorkflowState);
        Assert.Equal("create-prd", savedSession.WorkflowState.WorkflowName);
        Assert.Equal(2, savedSession.WorkflowState.CurrentStep);
        Assert.Single(savedSession.WorkflowState.ConversationHistory);
        Assert.Equal("user", savedSession.WorkflowState.ConversationHistory[0].Role);
        Assert.True(savedSession.WorkflowState.DecisionLocks["features-locked"]);
        Assert.Equal("Awaiting confirmation", savedSession.WorkflowState.PendingInput);
    }

    [Fact]
    public async Task Session_Should_Allow_Null_ConnectionId()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var user = new User
        {
            Email = $"test-null-conn-{Guid.NewGuid()}@example.com",
            PasswordHash = "hash",
            DisplayName = "Test User"
        };
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var session = new Session
        {
            UserId = user.Id,
            ConnectionId = null // Expired sessions have no connection
        };

        // Act
        dbContext.Sessions.Add(session);
        await dbContext.SaveChangesAsync();

        // Assert
        var savedSession = await dbContext.Sessions.FindAsync(session.Id);
        Assert.NotNull(savedSession);
        Assert.Null(savedSession.ConnectionId);
    }

    [Fact]
    public async Task Session_Should_Track_LastActivityAt()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var user = new User
        {
            Email = $"test-activity-{Guid.NewGuid()}@example.com",
            PasswordHash = "hash",
            DisplayName = "Test User"
        };
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var session = new Session
        {
            UserId = user.Id,
            LastActivityAt = DateTime.UtcNow
        };

        dbContext.Sessions.Add(session);
        await dbContext.SaveChangesAsync();

        // Act - Update activity
        var activityTime = DateTime.UtcNow.AddMinutes(5);
        session.LastActivityAt = activityTime;
        await dbContext.SaveChangesAsync();

        // Assert
        var savedSession = await dbContext.Sessions.FindAsync(session.Id);
        Assert.NotNull(savedSession);
        Assert.Equal(activityTime, savedSession.LastActivityAt, TimeSpan.FromMilliseconds(100));
    }

    [Fact]
    public async Task Session_Should_Support_IsActive_Flag()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var user = new User
        {
            Email = $"test-active-{Guid.NewGuid()}@example.com",
            PasswordHash = "hash",
            DisplayName = "Test User"
        };
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var session = new Session
        {
            UserId = user.Id,
            IsActive = true
        };

        dbContext.Sessions.Add(session);
        await dbContext.SaveChangesAsync();

        // Act - Mark as inactive
        session.IsActive = false;
        await dbContext.SaveChangesAsync();

        // Assert
        var savedSession = await dbContext.Sessions.FindAsync(session.Id);
        Assert.NotNull(savedSession);
        Assert.False(savedSession.IsActive);
    }
}
