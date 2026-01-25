using bmadServer.ApiService.Services.Workflows;
using bmadServer.ApiService.Services.Workflows.Agents;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace bmadServer.Tests.Unit.Services.Workflows;

public class AgentRouterTests
{
    private readonly Mock<ILogger<AgentRouter>> _loggerMock;
    private readonly AgentRouter _agentRouter;

    public AgentRouterTests()
    {
        _loggerMock = new Mock<ILogger<AgentRouter>>();
        _agentRouter = new AgentRouter(_loggerMock.Object);
    }

    [Fact]
    public void RegisterHandler_WithValidAgentId_RegistersSuccessfully()
    {
        // Arrange
        var agentId = "test-agent";
        var handler = new MockAgentHandler();

        // Act
        _agentRouter.RegisterHandler(agentId, handler);
        var retrievedHandler = _agentRouter.GetHandler(agentId);

        // Assert
        Assert.NotNull(retrievedHandler);
        Assert.Same(handler, retrievedHandler);
    }

    [Fact]
    public void RegisterHandler_WithNullAgentId_ThrowsArgumentException()
    {
        // Arrange
        var handler = new MockAgentHandler();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => _agentRouter.RegisterHandler(null!, handler));
    }

    [Fact]
    public void RegisterHandler_WithEmptyAgentId_ThrowsArgumentException()
    {
        // Arrange
        var handler = new MockAgentHandler();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => _agentRouter.RegisterHandler("", handler));
    }

    [Fact]
    public void RegisterHandler_WithNullHandler_ThrowsArgumentNullException()
    {
        // Arrange
        var agentId = "test-agent";

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _agentRouter.RegisterHandler(agentId, null!));
    }

    [Fact]
    public void GetHandler_WithUnregisteredAgentId_ReturnsNull()
    {
        // Arrange
        var agentId = "non-existent-agent";

        // Act
        var handler = _agentRouter.GetHandler(agentId);

        // Assert
        Assert.Null(handler);
    }

    [Fact]
    public void GetHandler_WithNullAgentId_ReturnsNull()
    {
        // Act
        var handler = _agentRouter.GetHandler(null!);

        // Assert
        Assert.Null(handler);
    }

    [Fact]
    public void RegisterHandler_OverwritesExistingHandler()
    {
        // Arrange
        var agentId = "test-agent";
        var handler1 = new MockAgentHandler();
        var handler2 = new MockAgentHandler();

        // Act
        _agentRouter.RegisterHandler(agentId, handler1);
        _agentRouter.RegisterHandler(agentId, handler2);
        var retrievedHandler = _agentRouter.GetHandler(agentId);

        // Assert
        Assert.NotNull(retrievedHandler);
        Assert.Same(handler2, retrievedHandler);
    }

    [Fact]
    public void RegisterHandler_WithMultipleAgents_AllAreAccessible()
    {
        // Arrange
        var handler1 = new MockAgentHandler();
        var handler2 = new MockAgentHandler();
        var handler3 = new MockAgentHandler();

        // Act
        _agentRouter.RegisterHandler("agent-1", handler1);
        _agentRouter.RegisterHandler("agent-2", handler2);
        _agentRouter.RegisterHandler("agent-3", handler3);

        // Assert
        Assert.Same(handler1, _agentRouter.GetHandler("agent-1"));
        Assert.Same(handler2, _agentRouter.GetHandler("agent-2"));
        Assert.Same(handler3, _agentRouter.GetHandler("agent-3"));
    }
}
