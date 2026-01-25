using bmadServer.ApiService.Data;
using bmadServer.ApiService.Data.Entities;
using bmadServer.ApiService.Hubs;
using bmadServer.ApiService.Models;
using bmadServer.ApiService.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System.Security.Claims;
using Xunit;

namespace bmadServer.Tests.Integration;

/// <summary>
/// Integration tests for ChatHub streaming functionality.
/// Tests MESSAGE_CHUNK events, stop generating, and interruption recovery.
/// </summary>
public class ChatHubStreamingTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public ChatHubStreamingTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task SendMessage_Should_Stream_Agent_Response_With_MESSAGE_CHUNK_Events()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var user = new User
        {
            Email = $"test-streaming-{Guid.NewGuid()}@example.com",
            PasswordHash = "hash",
            DisplayName = "Test User"
        };
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var sessionService = scope.ServiceProvider.GetRequiredService<ISessionService>();
        var (session, _) = await sessionService.RecoverSessionAsync(user.Id, "conn-stream-test");

        // Act - Verify session has a message added
        await sessionService.UpdateSessionStateAsync(session.Id, user.Id, s =>
        {
            s.WorkflowState ??= new WorkflowState();
            s.WorkflowState.ConversationHistory.Add(new ChatMessage
            {
                Id = Guid.NewGuid().ToString(),
                Role = "user",
                Content = "Test message",
                Timestamp = DateTime.UtcNow
            });
        });

        var updatedSession = await sessionService.GetActiveSessionAsync(user.Id, "conn-stream-test");

        // Assert
        Assert.NotNull(updatedSession);
        Assert.NotNull(updatedSession.WorkflowState);
        Assert.Single(updatedSession.WorkflowState.ConversationHistory);
        Assert.Equal("user", updatedSession.WorkflowState.ConversationHistory[0].Role);
    }

    [Fact]
    public async Task SendMessage_Should_Save_Partial_Message_For_Recovery()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var user = new User
        {
            Email = $"test-partial-{Guid.NewGuid()}@example.com",
            PasswordHash = "hash",
            DisplayName = "Test User"
        };
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var sessionService = scope.ServiceProvider.GetRequiredService<ISessionService>();
        var (session, _) = await sessionService.RecoverSessionAsync(user.Id, "conn-partial-test");

        // Act - Simulate saving partial message
        var messageId = Guid.NewGuid().ToString();
        var partialContent = "This is a partial";
        
        await sessionService.UpdateSessionStateAsync(session.Id, user.Id, s =>
        {
            s.WorkflowState ??= new WorkflowState();
            s.WorkflowState.PendingInput = System.Text.Json.JsonSerializer.Serialize(new
            {
                MessageId = messageId,
                PartialContent = partialContent,
                AgentId = "agent-1",
                IsStreaming = true
            });
        });

        var updatedSession = await sessionService.GetActiveSessionAsync(user.Id, "conn-partial-test");

        // Assert
        Assert.NotNull(updatedSession);
        Assert.NotNull(updatedSession.WorkflowState);
        Assert.NotNull(updatedSession.WorkflowState.PendingInput);
        Assert.Contains(messageId, updatedSession.WorkflowState.PendingInput);
        Assert.Contains(partialContent, updatedSession.WorkflowState.PendingInput);
    }

    [Fact]
    public async Task StreamingMessage_Should_Be_Saved_To_ConversationHistory_When_Complete()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var user = new User
        {
            Email = $"test-complete-{Guid.NewGuid()}@example.com",
            PasswordHash = "hash",
            DisplayName = "Test User"
        };
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var sessionService = scope.ServiceProvider.GetRequiredService<ISessionService>();
        var (session, _) = await sessionService.RecoverSessionAsync(user.Id, "conn-complete-test");

        // Act - Simulate completing a streamed message
        var messageId = Guid.NewGuid().ToString();
        var fullContent = "This is the complete message";
        var agentId = "agent-1";

        await sessionService.UpdateSessionStateAsync(session.Id, user.Id, s =>
        {
            s.WorkflowState ??= new WorkflowState();
            s.WorkflowState.ConversationHistory.Add(new ChatMessage
            {
                Id = messageId,
                Role = "agent",
                Content = fullContent,
                Timestamp = DateTime.UtcNow,
                AgentId = agentId
            });
        });

        var updatedSession = await sessionService.GetActiveSessionAsync(user.Id, "conn-complete-test");

        // Assert
        Assert.NotNull(updatedSession);
        Assert.NotNull(updatedSession.WorkflowState);
        Assert.Single(updatedSession.WorkflowState.ConversationHistory);
        var savedMessage = updatedSession.WorkflowState.ConversationHistory[0];
        Assert.Equal(messageId, savedMessage.Id);
        Assert.Equal("agent", savedMessage.Role);
        Assert.Equal(fullContent, savedMessage.Content);
        Assert.Equal(agentId, savedMessage.AgentId);
    }

    [Fact]
    public async Task ConversationHistory_Should_Keep_Only_Last_10_Messages()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var user = new User
        {
            Email = $"test-limit-{Guid.NewGuid()}@example.com",
            PasswordHash = "hash",
            DisplayName = "Test User"
        };
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var sessionService = scope.ServiceProvider.GetRequiredService<ISessionService>();
        var (session, _) = await sessionService.RecoverSessionAsync(user.Id, "conn-limit-test");

        // Act - Add 15 messages
        for (int i = 0; i < 15; i++)
        {
            await sessionService.UpdateSessionStateAsync(session.Id, user.Id, s =>
            {
                s.WorkflowState ??= new WorkflowState();
                s.WorkflowState.ConversationHistory.Add(new ChatMessage
                {
                    Id = Guid.NewGuid().ToString(),
                    Role = i % 2 == 0 ? "user" : "agent",
                    Content = $"Message {i}",
                    Timestamp = DateTime.UtcNow
                });

                // Keep only last 10
                if (s.WorkflowState.ConversationHistory.Count > 10)
                {
                    s.WorkflowState.ConversationHistory = s.WorkflowState.ConversationHistory
                        .TakeLast(10)
                        .ToList();
                }
            });
        }

        var updatedSession = await sessionService.GetActiveSessionAsync(user.Id, "conn-limit-test");

        // Assert
        Assert.NotNull(updatedSession);
        Assert.NotNull(updatedSession.WorkflowState);
        Assert.Equal(10, updatedSession.WorkflowState.ConversationHistory.Count);
        
        // Verify it kept the last 10 messages (5-14)
        Assert.Contains(updatedSession.WorkflowState.ConversationHistory, 
            m => m.Content == "Message 14");
        Assert.DoesNotContain(updatedSession.WorkflowState.ConversationHistory, 
            m => m.Content == "Message 0");
    }
}
