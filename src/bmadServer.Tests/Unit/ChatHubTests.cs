using bmadServer.ApiService.Data.Entities;
using bmadServer.ApiService.Hubs;
using bmadServer.ApiService.Models;
using bmadServer.ApiService.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;
using Xunit;

namespace bmadServer.Tests.Unit;

/// <summary>
/// Unit tests for ChatHub methods.
/// Tests workflow management and message handling.
/// </summary>
public class ChatHubTests
{
    private readonly Mock<ISessionService> _sessionServiceMock;
    private readonly Mock<IMessageStreamingService> _streamingServiceMock;
    private readonly Mock<ILogger<ChatHub>> _loggerMock;
    private readonly Mock<HubCallerContext> _contextMock;
    private readonly Mock<IHubCallerClients> _clientsMock;
    private readonly Mock<ISingleClientProxy> _callerMock;
    private readonly Mock<IGroupManager> _groupsMock;
    private readonly ChatHub _chatHub;

    public ChatHubTests()
    {
        _sessionServiceMock = new Mock<ISessionService>();
        _streamingServiceMock = new Mock<IMessageStreamingService>();
        _loggerMock = new Mock<ILogger<ChatHub>>();
        _contextMock = new Mock<HubCallerContext>();
        _clientsMock = new Mock<IHubCallerClients>();
        _callerMock = new Mock<ISingleClientProxy>();
        _groupsMock = new Mock<IGroupManager>();

        _chatHub = new ChatHub(
            _sessionServiceMock.Object, 
            _streamingServiceMock.Object,
            _loggerMock.Object)
        {
            Context = _contextMock.Object,
            Clients = _clientsMock.Object,
            Groups = _groupsMock.Object
        };

        // Setup default user claims
        var userId = Guid.NewGuid();
        var claims = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        }));
        _contextMock.Setup(c => c.User).Returns(claims);
        _contextMock.Setup(c => c.ConnectionId).Returns("conn-123");
        _clientsMock.Setup(c => c.Caller).Returns(_callerMock.Object);
    }

    [Fact]
    public async Task JoinWorkflow_Should_Add_User_To_Workflow_Group()
    {
        // Arrange
        var workflowName = "create-prd";

        // Act
        await _chatHub.JoinWorkflow(workflowName);

        // Assert
        _groupsMock.Verify(g => g.AddToGroupAsync(
            "conn-123",
            "workflow-create-prd",
            default), Times.Once);

        _callerMock.Verify(c => c.SendCoreAsync(
            "JoinedWorkflow",
            It.Is<object[]>(args => 
                args.Length == 1 && 
                args[0].GetType().GetProperty("WorkflowName")!.GetValue(args[0])!.ToString() == workflowName),
            default), Times.Once);
    }

    [Fact]
    public async Task LeaveWorkflow_Should_Remove_User_From_Workflow_Group()
    {
        // Arrange
        var workflowName = "create-prd";

        // Act
        await _chatHub.LeaveWorkflow(workflowName);

        // Assert
        _groupsMock.Verify(g => g.RemoveFromGroupAsync(
            "conn-123",
            "workflow-create-prd",
            default), Times.Once);

        _callerMock.Verify(c => c.SendCoreAsync(
            "LeftWorkflow",
            It.Is<object[]>(args => 
                args.Length == 1 && 
                args[0].GetType().GetProperty("WorkflowName")!.GetValue(args[0])!.ToString() == workflowName),
            default), Times.Once);
    }

    [Fact]
    public async Task SendMessage_Should_Update_Session_And_Echo_Message()
    {
        // Arrange
        var userId = Guid.Parse(_contextMock.Object.User!.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var sessionId = Guid.NewGuid();
        var message = "Hello, assistant!";

        var session = new Session
        {
            Id = sessionId,
            UserId = userId,
            ConnectionId = "conn-123",
            IsActive = true,
            WorkflowState = new WorkflowState
            {
                ConversationHistory = new List<ChatMessage>()
            }
        };

        _sessionServiceMock
            .Setup(s => s.GetActiveSessionAsync(userId, "conn-123"))
            .ReturnsAsync(session);

        _sessionServiceMock
            .Setup(s => s.UpdateSessionStateAsync(
                sessionId,
                userId,
                It.IsAny<Action<Session>>()))
            .ReturnsAsync(true);

        // Act
        await _chatHub.SendMessage(message);

        // Assert
        _sessionServiceMock.Verify(s => s.GetActiveSessionAsync(userId, "conn-123"), Times.Once);
        _sessionServiceMock.Verify(s => s.UpdateSessionStateAsync(
            sessionId,
            userId,
            It.IsAny<Action<Session>>()), Times.Once);

        _callerMock.Verify(c => c.SendCoreAsync(
            "ReceiveMessage",
            It.Is<object[]>(args => 
                args.Length == 1 && 
                args[0].GetType().GetProperty("Content")!.GetValue(args[0])!.ToString() == message),
            default), Times.Once);
    }

    [Fact]
    public async Task SendMessage_Should_Throw_When_No_Active_Session()
    {
        // Arrange
        var userId = Guid.Parse(_contextMock.Object.User!.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        _sessionServiceMock
            .Setup(s => s.GetActiveSessionAsync(userId, "conn-123"))
            .ReturnsAsync((Session?)null);

        // Act & Assert
        await Assert.ThrowsAsync<HubException>(() => _chatHub.SendMessage("test"));
    }

    [Fact]
    public void GetUserIdFromClaims_Should_Throw_When_No_User_Id_Claim()
    {
        // Arrange
        var contextWithoutClaims = new Mock<HubCallerContext>();
        contextWithoutClaims.Setup(c => c.User).Returns((ClaimsPrincipal?)null);
        
        var chatHub = new ChatHub(
            _sessionServiceMock.Object, 
            _streamingServiceMock.Object,
            _loggerMock.Object)
        {
            Context = contextWithoutClaims.Object
        };

        // Act & Assert - Reflection wraps the exception in TargetInvocationException
        var exception = Assert.Throws<System.Reflection.TargetInvocationException>(() => 
        {
            var method = typeof(ChatHub).GetMethod("GetUserIdFromClaims", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            method!.Invoke(chatHub, null);
        });

        // Verify the inner exception is HubException
        Assert.IsType<HubException>(exception.InnerException);
    }
}
