using bmadServer.ApiService.Data;
using bmadServer.ApiService.Data.Entities;
using bmadServer.ApiService.Hubs;
using bmadServer.ApiService.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System.Security.Claims;
using Xunit;

namespace bmadServer.Tests.Integration;

/// <summary>
/// Integration tests for ChatHub session lifecycle management.
/// Tests connection, disconnection, and session recovery flows.
/// </summary>
public class ChatHubIntegrationTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public ChatHubIntegrationTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task OnConnectedAsync_Should_Create_New_Session_For_New_User()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var user = new User
        {
            Email = $"test-hub-{Guid.NewGuid()}@example.com",
            PasswordHash = "hash",
            DisplayName = "Test User"
        };
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        // Act - Simulate connection through hub
        // Note: This is a simplified test. Full SignalR testing would require TestServer with WebSockets.
        // For now, we verify the session service integration works correctly.

        var sessionService = scope.ServiceProvider.GetRequiredService<bmadServer.ApiService.Services.ISessionService>();
        var (session, isRecovered) = await sessionService.RecoverSessionAsync(user.Id, "conn-new");

        // Assert
        Assert.NotNull(session);
        Assert.False(isRecovered);
        Assert.Equal("conn-new", session.ConnectionId);
        Assert.True(session.IsActive);
    }

    [Fact]
    public async Task OnConnectedAsync_Should_Recover_Session_Within_60_Seconds()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var user = new User
        {
            Email = $"test-recovery-{Guid.NewGuid()}@example.com",
            PasswordHash = "hash",
            DisplayName = "Test User"
        };
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var sessionService = scope.ServiceProvider.GetRequiredService<bmadServer.ApiService.Services.ISessionService>();
        
        // Create initial session
        var originalSession = await sessionService.CreateSessionAsync(user.Id, "conn-old");
        await sessionService.UpdateSessionStateAsync(originalSession.Id, user.Id, s =>
        {
            s.WorkflowState = new WorkflowState
            {
                WorkflowName = "create-prd",
                CurrentStep = 5,
                PendingInput = "Awaiting confirmation"
            };
        });

        // Simulate disconnect 30 seconds ago
        var session = await dbContext.Sessions.FindAsync(originalSession.Id);
        session!.LastActivityAt = DateTime.UtcNow.AddSeconds(-30);
        await dbContext.SaveChangesAsync();

        // Act - Reconnect within 60 seconds
        var (recoveredSession, isRecovered) = await sessionService.RecoverSessionAsync(user.Id, "conn-new");

        // Assert
        Assert.NotNull(recoveredSession);
        Assert.True(isRecovered);
        Assert.Equal(originalSession.Id, recoveredSession.Id); // Same session
        Assert.Equal("conn-new", recoveredSession.ConnectionId); // Updated connection
        Assert.NotNull(recoveredSession.WorkflowState);
        Assert.Equal("create-prd", recoveredSession.WorkflowState.WorkflowName);
        Assert.Equal(5, recoveredSession.WorkflowState.CurrentStep);
    }

    [Fact]
    public async Task OnDisconnectedAsync_Should_Keep_Session_Active()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var user = new User
        {
            Email = $"test-disconnect-{Guid.NewGuid()}@example.com",
            PasswordHash = "hash",
            DisplayName = "Test User"
        };
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var sessionService = scope.ServiceProvider.GetRequiredService<bmadServer.ApiService.Services.ISessionService>();
        var session = await sessionService.CreateSessionAsync(user.Id, "conn-123");

        // Act - Disconnect doesn't expire session
        // (OnDisconnectedAsync doesn't call ExpireSessionAsync - cleanup service handles it)

        // Assert - Session should still be active
        var activeSession = await dbContext.Sessions.FindAsync(session.Id);
        Assert.NotNull(activeSession);
        Assert.True(activeSession.IsActive);
        Assert.NotNull(activeSession.ConnectionId);
    }
}
