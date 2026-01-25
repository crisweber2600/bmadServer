using bmadServer.ApiService.Hubs;
using bmadServer.ApiService.Services;
using bmadServer.ApiService.Models;
using bmadServer.ApiService.Data.Entities;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using System.Security.Claims;

namespace bmadServer.Tests.Hubs;

public class ChatHubStreamingTests
{
    private readonly Mock<ISessionService> _mockSessionService;
    private readonly Mock<IMessageStreamingService> _mockStreamingService;
    private readonly Mock<IChatHistoryService> _mockChatHistoryService;
    private readonly Mock<ILogger<ChatHub>> _mockLogger;
    private readonly Mock<IHubCallerClients> _mockClients;
    private readonly Mock<ISingleClientProxy> _mockCaller;
    private readonly ChatHub _hub;

    public ChatHubStreamingTests()
    {
        _mockSessionService = new Mock<ISessionService>();
        _mockStreamingService = new Mock<IMessageStreamingService>();
        _mockChatHistoryService = new Mock<IChatHistoryService>();
        _mockLogger = new Mock<ILogger<ChatHub>>();
        _mockClients = new Mock<IHubCallerClients>();
        _mockCaller = new Mock<ISingleClientProxy>();

        _hub = new ChatHub(_mockSessionService.Object, _mockStreamingService.Object, _mockChatHistoryService.Object, _mockLogger.Object);

        // Setup hub context
        _mockClients.Setup(c => c.Caller).Returns(_mockCaller.Object);
        _hub.Clients = _mockClients.Object;

        var mockContext = new Mock<HubCallerContext>();
        mockContext.Setup(c => c.ConnectionId).Returns("test-connection-id");
        mockContext.Setup(c => c.User).Returns(new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())
        })));
        _hub.Context = mockContext.Object;
    }

    [Fact]
    public async Task SendMessage_ShouldStreamResponse_WithinFiveSeconds()
    {
        // Arrange
        var userId = Guid.Parse(_hub.Context.User!.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var sessionId = Guid.NewGuid();
        var message = "Test message";
        var messageId = "msg-123";

        var session = new Session
        {
            Id = sessionId,
            UserId = userId,
            ConnectionId = "test-connection-id"
        };

        _mockSessionService
            .Setup(s => s.GetActiveSessionAsync(It.IsAny<Guid>(), It.IsAny<string>()))
            .ReturnsAsync(session);

        var startTime = DateTime.UtcNow;

        _mockStreamingService
            .Setup(s => s.StreamResponseAsync(
                It.IsAny<string>(), 
                It.IsAny<string>(), 
                It.IsAny<Func<string, string, bool, string, Task>>(),
                It.IsAny<CancellationToken>()))
            .Returns(async (string msg, string msgId, Func<string, string, bool, string, Task> callback, CancellationToken ct) =>
            {
                // Simulate first token within 5 seconds
                await Task.Delay(100);
                await callback("First token", msgId, false, "agent-1");
                return messageId;
            });

        // Act
        await _hub.SendMessageStreaming(message);

        // Assert
        var elapsed = DateTime.UtcNow - startTime;
        Assert.True(elapsed.TotalSeconds < 5, "First token should arrive within 5 seconds");
    }

    [Fact]
    public async Task StopGenerating_ShouldCancelStreaming()
    {
        // Arrange
        var messageId = "msg-123";
        var streamingCancelled = false;

        _mockStreamingService
            .Setup(s => s.CancelStreamingAsync(messageId))
            .Callback(() => streamingCancelled = true)
            .Returns(Task.CompletedTask);

        // Act
        await _hub.StopGenerating(messageId);

        // Assert
        Assert.True(streamingCancelled);
    }
}
