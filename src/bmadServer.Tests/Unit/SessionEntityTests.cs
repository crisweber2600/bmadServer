using bmadServer.ApiService.Data.Entities;
using bmadServer.ApiService.Models;
using Xunit;

namespace bmadServer.Tests.Unit;

/// <summary>
/// Unit tests for Session entity model and WorkflowState structure.
/// Tests persistence fields, computed properties, and data integrity.
/// </summary>
public class SessionEntityTests
{
    [Fact]
    public void Session_Should_Initialize_With_Default_Values()
    {
        // Arrange & Act
        var session = new Session
        {
            UserId = Guid.NewGuid()
        };

        // Assert
        Assert.NotEqual(Guid.Empty, session.Id);
        Assert.True(session.IsActive);
        Assert.NotEqual(default, session.CreatedAt);
        Assert.NotEqual(default, session.LastActivityAt);
        Assert.NotEqual(default, session.ExpiresAt);
        Assert.True(session.ExpiresAt > session.CreatedAt);
    }

    [Fact]
    public void Session_Should_Support_Nullable_ConnectionId()
    {
        // Arrange & Act
        var session = new Session
        {
            UserId = Guid.NewGuid(),
            ConnectionId = null // Cleared when session expires
        };

        // Assert
        Assert.Null(session.ConnectionId);
    }

    [Fact]
    public void Session_Should_Store_WorkflowState()
    {
        // Arrange
        var workflowState = new WorkflowState
        {
            WorkflowName = "create-prd",
            CurrentStep = 3,
            PendingInput = "Waiting for user confirmation"
        };

        // Act
        var session = new Session
        {
            UserId = Guid.NewGuid(),
            WorkflowState = workflowState
        };

        // Assert
        Assert.NotNull(session.WorkflowState);
        Assert.Equal("create-prd", session.WorkflowState.WorkflowName);
        Assert.Equal(3, session.WorkflowState.CurrentStep);
        Assert.Equal("Waiting for user confirmation", session.WorkflowState.PendingInput);
    }

    [Fact]
    public void IsWithinRecoveryWindow_Should_Return_True_Within_60_Seconds()
    {
        // Arrange
        var session = new Session
        {
            UserId = Guid.NewGuid(),
            LastActivityAt = DateTime.UtcNow.AddSeconds(-30) // 30 seconds ago
        };

        // Act
        var isWithinWindow = session.IsWithinRecoveryWindow;

        // Assert
        Assert.True(isWithinWindow);
    }

    [Fact]
    public void IsWithinRecoveryWindow_Should_Return_False_After_60_Seconds()
    {
        // Arrange
        var session = new Session
        {
            UserId = Guid.NewGuid(),
            LastActivityAt = DateTime.UtcNow.AddSeconds(-61) // 61 seconds ago
        };

        // Act
        var isWithinWindow = session.IsWithinRecoveryWindow;

        // Assert
        Assert.False(isWithinWindow);
    }

    [Fact]
    public void WorkflowState_Should_Initialize_With_Default_Collections()
    {
        // Arrange & Act
        var workflowState = new WorkflowState();

        // Assert
        Assert.NotNull(workflowState.ConversationHistory);
        Assert.Empty(workflowState.ConversationHistory);
        Assert.NotNull(workflowState.DecisionLocks);
        Assert.Empty(workflowState.DecisionLocks);
        Assert.Equal(1, workflowState._version);
    }

    [Fact]
    public void WorkflowState_Should_Store_ConversationHistory()
    {
        // Arrange
        var messages = new List<ChatMessage>
        {
            new() { Id = "msg1", Role = "user", Content = "Hello", Timestamp = DateTime.UtcNow },
            new() { Id = "msg2", Role = "agent", Content = "Hi there!", Timestamp = DateTime.UtcNow, AgentId = "agent1" }
        };

        // Act
        var workflowState = new WorkflowState
        {
            ConversationHistory = messages
        };

        // Assert
        Assert.Equal(2, workflowState.ConversationHistory.Count);
        Assert.Equal("user", workflowState.ConversationHistory[0].Role);
        Assert.Equal("agent", workflowState.ConversationHistory[1].Role);
        Assert.Equal("agent1", workflowState.ConversationHistory[1].AgentId);
    }

    [Fact]
    public void WorkflowState_Should_Store_DecisionLocks()
    {
        // Arrange & Act
        var workflowState = new WorkflowState
        {
            DecisionLocks = new Dictionary<string, bool>
            {
                { "feature-set-locked", true },
                { "tech-stack-locked", false }
            }
        };

        // Assert
        Assert.Equal(2, workflowState.DecisionLocks.Count);
        Assert.True(workflowState.DecisionLocks["feature-set-locked"]);
        Assert.False(workflowState.DecisionLocks["tech-stack-locked"]);
    }

    [Fact]
    public void WorkflowState_Should_Track_Concurrency_Fields()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var timestamp = DateTime.UtcNow;

        // Act
        var workflowState = new WorkflowState
        {
            _version = 5,
            _lastModifiedBy = userId,
            _lastModifiedAt = timestamp
        };

        // Assert
        Assert.Equal(5, workflowState._version);
        Assert.Equal(userId, workflowState._lastModifiedBy);
        Assert.Equal(timestamp, workflowState._lastModifiedAt);
    }
}
