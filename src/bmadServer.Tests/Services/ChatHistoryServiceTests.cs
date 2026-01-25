using bmadServer.ApiService.Data;
using bmadServer.ApiService.Data.Entities;
using bmadServer.ApiService.Models;
using bmadServer.ApiService.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace bmadServer.Tests.Services;

public class ChatHistoryServiceTests
{
    private ApplicationDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task GetChatHistoryAsync_ShouldReturnLast50Messages_WhenNoOffsetProvided()
    {
        // Arrange
        await using var context = GetInMemoryDbContext();
        var service = new ChatHistoryService(context);
        var userId = Guid.NewGuid();
        var workflowId = Guid.NewGuid();

        // Create a session with 100 messages
        var session = new Session
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            WorkflowState = new WorkflowState
            {
                WorkflowName = "test-workflow",
                ConversationHistory = Enumerable.Range(1, 100)
                    .Select(i => new ChatMessage
                    {
                        Id = Guid.NewGuid().ToString(),
                        Role = i % 2 == 0 ? "user" : "agent",
                        Content = $"Message {i}",
                        Timestamp = DateTime.UtcNow.AddMinutes(i) // Increasing timestamps
                    })
                    .ToList()
            }
        };

        context.Sessions.Add(session);
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetChatHistoryAsync(userId, session.Id, pageSize: 50, offset: 0);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(50, result.Messages.Count);
        Assert.Equal(100, result.TotalCount);
        Assert.True(result.HasMore);
        
        // Should return the most recent messages
        Assert.Contains("Message 100", result.Messages[0].Content);
    }

    [Fact]
    public async Task GetChatHistoryAsync_ShouldSupportPagination_WithOffset()
    {
        // Arrange
        await using var context = GetInMemoryDbContext();
        var service = new ChatHistoryService(context);
        var userId = Guid.NewGuid();

        var session = new Session
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            WorkflowState = new WorkflowState
            {
                WorkflowName = "test-workflow",
                ConversationHistory = Enumerable.Range(1, 100)
                    .Select(i => new ChatMessage
                    {
                        Id = Guid.NewGuid().ToString(),
                        Role = "user",
                        Content = $"Message {i}",
                        Timestamp = DateTime.UtcNow.AddMinutes(i) // Increasing timestamps
                    })
                    .ToList()
            }
        };

        context.Sessions.Add(session);
        await context.SaveChangesAsync();

        // Act - Get second page (offset 50)
        var result = await service.GetChatHistoryAsync(userId, session.Id, pageSize: 50, offset: 50);

        // Assert
        Assert.Equal(50, result.Messages.Count);
        Assert.Equal(100, result.TotalCount);
        Assert.False(result.HasMore); // No more pages after this
    }

    [Fact]
    public async Task GetChatHistoryAsync_ShouldReturnEmpty_ForNewWorkflow()
    {
        // Arrange
        await using var context = GetInMemoryDbContext();
        var service = new ChatHistoryService(context);
        var userId = Guid.NewGuid();

        var session = new Session
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            WorkflowState = new WorkflowState
            {
                WorkflowName = "new-workflow",
                ConversationHistory = new List<ChatMessage>()
            }
        };

        context.Sessions.Add(session);
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetChatHistoryAsync(userId, session.Id, pageSize: 50, offset: 0);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.Messages);
        Assert.Equal(0, result.TotalCount);
        Assert.False(result.HasMore);
    }

    [Fact]
    public async Task GetChatHistoryAsync_ShouldThrow_ForUnauthorizedAccess()
    {
        // Arrange
        await using var context = GetInMemoryDbContext();
        var service = new ChatHistoryService(context);
        var userId = Guid.NewGuid();
        var differentUserId = Guid.NewGuid();

        var session = new Session
        {
            Id = Guid.NewGuid(),
            UserId = userId, // Different user
            WorkflowState = new WorkflowState()
        };

        context.Sessions.Add(session);
        await context.SaveChangesAsync();

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => service.GetChatHistoryAsync(differentUserId, session.Id, 50, 0)
        );
    }
}
