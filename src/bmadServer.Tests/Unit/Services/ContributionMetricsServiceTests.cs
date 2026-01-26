using System.Text.Json;
using bmadServer.ApiService.Data;
using bmadServer.ApiService.Data.Entities;
using bmadServer.ApiService.Models;
using bmadServer.ApiService.Models.Workflows;
using bmadServer.ApiService.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Moq;
using Xunit;

namespace bmadServer.Tests.Unit.Services;

public class ContributionMetricsServiceTests
{
    private readonly Mock<IDistributedCache> _mockCache;
    private readonly ApplicationDbContext _dbContext;
    private readonly IContributionMetricsService _service;

    public ContributionMetricsServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"TestDb_Contributions_{Guid.NewGuid()}")
            .Options;
        _dbContext = new ApplicationDbContext(options);
        _mockCache = new Mock<IDistributedCache>();
        _service = new ContributionMetricsService(_dbContext, _mockCache.Object);
    }

    [Fact]
    public async Task GetContributionMetricsAsync_WithMultipleUsers_ReturnsAggregatedMetrics()
    {
        // Arrange
        var workflowId = Guid.NewGuid();
        var user1Id = Guid.NewGuid();
        var user2Id = Guid.NewGuid();

        // Create workflow instance
        var workflowInstance = new WorkflowInstance
        {
            Id = workflowId,
            WorkflowDefinitionId = "test-workflow",
            UserId = user1Id,
            Status = WorkflowStatus.Running,
            CurrentStep = 1,
            CreatedAt = DateTime.UtcNow.AddHours(-2),
            UpdatedAt = DateTime.UtcNow
        };
        _dbContext.WorkflowInstances.Add(workflowInstance);

        // Create sessions with conversation history
        var session1 = new Session
        {
            UserId = user1Id,
            IsActive = true,
            ConnectionId = "conn1",
            CreatedAt = DateTime.UtcNow.AddHours(-2),
            LastActivityAt = DateTime.UtcNow.AddHours(-1),
            ExpiresAt = DateTime.UtcNow.AddHours(2),
            WorkflowState = new WorkflowState
            {
                ActiveWorkflowInstanceId = workflowId,
                ConversationHistory = new List<ChatMessage>
                {
                    new ChatMessage 
                    { 
                        Id = "msg1", 
                        Role = "user", 
                        Content = "Message 1", 
                        Timestamp = DateTime.UtcNow.AddHours(-2),
                        UserId = user1Id,
                        DisplayName = "User One",
                        InputType = "Message"
                    },
                    new ChatMessage 
                    { 
                        Id = "msg2", 
                        Role = "user", 
                        Content = "Message 2", 
                        Timestamp = DateTime.UtcNow.AddHours(-1),
                        UserId = user1Id,
                        DisplayName = "User One",
                        InputType = "Message"
                    }
                }
            }
        };
        
        var session2 = new Session
        {
            UserId = user2Id,
            IsActive = true,
            ConnectionId = "conn2",
            CreatedAt = DateTime.UtcNow.AddHours(-1),
            LastActivityAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddHours(2),
            WorkflowState = new WorkflowState
            {
                ActiveWorkflowInstanceId = workflowId,
                ConversationHistory = new List<ChatMessage>
                {
                    new ChatMessage 
                    { 
                        Id = "msg3", 
                        Role = "user", 
                        Content = "Message 3", 
                        Timestamp = DateTime.UtcNow.AddMinutes(-30),
                        UserId = user2Id,
                        DisplayName = "User Two",
                        InputType = "Message"
                    }
                }
            }
        };

        _dbContext.Sessions.AddRange(session1, session2);

        // Create decision events
        var decisionEvent = new WorkflowEvent
        {
            Id = Guid.NewGuid(),
            WorkflowInstanceId = workflowId,
            EventType = "DecisionMade",
            UserId = user1Id,
            DisplayName = "User One",
            Timestamp = DateTime.UtcNow.AddMinutes(-30),
            InputType = "Decision"
        };
        _dbContext.WorkflowEvents.Add(decisionEvent);

        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _service.GetContributionMetricsAsync(workflowId);

        // Assert
        result.Should().NotBeNull();
        result.WorkflowId.Should().Be(workflowId);
        result.Contributors.Should().HaveCount(2);
        
        var user1Contrib = result.Contributors.First(c => c.UserId == user1Id);
        user1Contrib.MessagesSent.Should().Be(2);
        user1Contrib.DecisionsMade.Should().Be(1);
        user1Contrib.DisplayName.Should().Be("User One");
        
        var user2Contrib = result.Contributors.First(c => c.UserId == user2Id);
        user2Contrib.MessagesSent.Should().Be(1);
        user2Contrib.DecisionsMade.Should().Be(0);
        
        result.Summary.TotalMessages.Should().Be(3);
        result.Summary.TotalDecisions.Should().Be(1);
        result.Summary.TotalContributors.Should().Be(2);
    }

    [Fact]
    public async Task GetContributionMetricsAsync_WithNoContributors_ReturnsEmptyMetrics()
    {
        // Arrange
        var workflowId = Guid.NewGuid();
        var workflowInstance = new WorkflowInstance
        {
            Id = workflowId,
            WorkflowDefinitionId = "test-workflow",
            UserId = Guid.NewGuid(),
            Status = WorkflowStatus.Running,
            CurrentStep = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _dbContext.WorkflowInstances.Add(workflowInstance);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _service.GetContributionMetricsAsync(workflowId);

        // Assert
        result.Should().NotBeNull();
        result.Contributors.Should().BeEmpty();
        result.Summary.TotalMessages.Should().Be(0);
        result.Summary.TotalDecisions.Should().Be(0);
    }

    [Fact]
    public async Task GetUserContributionAsync_WithValidUser_ReturnsUserMetrics()
    {
        // Arrange
        var workflowId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var workflowInstance = new WorkflowInstance
        {
            Id = workflowId,
            WorkflowDefinitionId = "test-workflow",
            UserId = userId,
            Status = WorkflowStatus.Running,
            CurrentStep = 1,
            CreatedAt = DateTime.UtcNow.AddHours(-1),
            UpdatedAt = DateTime.UtcNow
        };
        _dbContext.WorkflowInstances.Add(workflowInstance);

        var session = new Session
        {
            UserId = userId,
            IsActive = true,
            ConnectionId = "conn1",
            CreatedAt = DateTime.UtcNow.AddHours(-1),
            LastActivityAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddHours(2),
            WorkflowState = new WorkflowState
            {
                ActiveWorkflowInstanceId = workflowId,
                ConversationHistory = new List<ChatMessage>
                {
                    new ChatMessage 
                    { 
                        Id = "msg1", 
                        Role = "user", 
                        Content = "Test", 
                        Timestamp = DateTime.UtcNow,
                        UserId = userId,
                        DisplayName = "Test User",
                        InputType = "Message"
                    }
                }
            }
        };
        _dbContext.Sessions.Add(session);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _service.GetUserContributionAsync(workflowId, userId);

        // Assert
        result.Should().NotBeNull();
        result.UserId.Should().Be(userId);
        result.MessagesSent.Should().Be(1);
        result.DisplayName.Should().Be("Test User");
    }

    [Fact]
    public async Task GetContributionMetricsAsync_CalculatesTimeSpentCorrectly()
    {
        // Arrange
        var workflowId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var startTime = DateTime.UtcNow.AddHours(-2);
        var endTime = DateTime.UtcNow;

        var workflowInstance = new WorkflowInstance
        {
            Id = workflowId,
            WorkflowDefinitionId = "test-workflow",
            UserId = userId,
            Status = WorkflowStatus.Running,
            CurrentStep = 1,
            CreatedAt = startTime,
            UpdatedAt = endTime
        };
        _dbContext.WorkflowInstances.Add(workflowInstance);

        var session = new Session
        {
            UserId = userId,
            IsActive = true,
            ConnectionId = "conn1",
            CreatedAt = startTime,
            LastActivityAt = endTime,
            ExpiresAt = endTime.AddHours(2),
            WorkflowState = new WorkflowState
            {
                ActiveWorkflowInstanceId = workflowId,
                ConversationHistory = new List<ChatMessage>
                {
                    new ChatMessage 
                    { 
                        Id = "msg1", 
                        Role = "user", 
                        Content = "Test", 
                        Timestamp = startTime,
                        UserId = userId,
                        DisplayName = "Test User",
                        InputType = "Message"
                    }
                }
            }
        };
        _dbContext.Sessions.Add(session);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _service.GetContributionMetricsAsync(workflowId);

        // Assert
        var userContrib = result.Contributors.First();
        userContrib.TimeSpent.Should().BeCloseTo(TimeSpan.FromHours(2), TimeSpan.FromMinutes(1));
    }
}
