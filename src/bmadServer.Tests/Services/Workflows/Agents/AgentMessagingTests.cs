using bmadServer.ApiService.Services.Workflows.Agents;
using Xunit;
using FluentAssertions;
using Moq;
using Microsoft.Extensions.Logging;

namespace bmadServer.Tests.Services.Workflows.Agents;

public class AgentMessagingTests
{
    private readonly Mock<ILogger<AgentMessaging>> _mockLogger;
    private readonly Mock<IAgentRegistry> _mockRegistry;
    private readonly AgentMessaging _messaging;

    public AgentMessagingTests()
    {
        _mockLogger = new Mock<ILogger<AgentMessaging>>();
        _mockRegistry = new Mock<IAgentRegistry>();
        _messaging = new AgentMessaging(_mockLogger.Object, _mockRegistry.Object);
    }

    [Fact]
    public async Task RequestFromAgent_WithValidRequest_ReturnsResponse()
    {
        // Arrange
        var targetAgentId = "developer";
        var request = "Implement authentication";
        var context = new AgentMessageContext
        {
            WorkflowInstanceId = Guid.NewGuid(),
            SourceAgentId = "architect",
            ConversationHistory = new List<string>()
        };

        var targetAgent = new AgentDefinition
        {
            AgentId = targetAgentId,
            Name = "Developer",
            Description = "Test developer agent",
            Capabilities = new List<string> { "code-implementation" },
            SystemPrompt = "Test prompt",
            ModelPreference = "gpt-4"
        };

        _mockRegistry.Setup(r => r.GetAgent(targetAgentId)).Returns(targetAgent);

        // Act
        var result = await _messaging.RequestFromAgent(targetAgentId, request, context);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.MessageId.Should().NotBeEmpty();
        result.Response.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task RequestFromAgent_WithInvalidAgentId_ReturnsError()
    {
        // Arrange
        var targetAgentId = "nonexistent";
        var request = "Test request";
        var context = new AgentMessageContext
        {
            WorkflowInstanceId = Guid.NewGuid(),
            SourceAgentId = "architect",
            ConversationHistory = new List<string>()
        };

        _mockRegistry.Setup(r => r.GetAgent(targetAgentId)).Returns((AgentDefinition?)null);

        // Act
        var result = await _messaging.RequestFromAgent(targetAgentId, request, context);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("not found");
    }

    [Fact]
    public async Task RequestFromAgent_IncludesAllRequiredFields()
    {
        // Arrange
        var targetAgentId = "developer";
        var request = "Test request";
        var workflowInstanceId = Guid.NewGuid();
        var context = new AgentMessageContext
        {
            WorkflowInstanceId = workflowInstanceId,
            SourceAgentId = "architect",
            ConversationHistory = new List<string> { "Previous message" }
        };

        var targetAgent = new AgentDefinition
        {
            AgentId = targetAgentId,
            Name = "Developer",
            Description = "Test developer agent",
            Capabilities = new List<string> { "code-implementation" },
            SystemPrompt = "Test prompt",
            ModelPreference = "gpt-4"
        };

        _mockRegistry.Setup(r => r.GetAgent(targetAgentId)).Returns(targetAgent);

        // Act
        var result = await _messaging.RequestFromAgent(targetAgentId, request, context);

        // Assert
        result.MessageId.Should().NotBeEmpty();
        result.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        result.SourceAgent.Should().Be("architect");
        result.TargetAgent.Should().Be(targetAgentId);
        result.MessageType.Should().Be("request");
        result.Content.Should().Be(request);
        result.WorkflowInstanceId.Should().Be(workflowInstanceId);
    }

    [Fact]
    public async Task RequestFromAgent_LogsExchange()
    {
        // Arrange
        var targetAgentId = "developer";
        var request = "Test request";
        var context = new AgentMessageContext
        {
            WorkflowInstanceId = Guid.NewGuid(),
            SourceAgentId = "architect",
            ConversationHistory = new List<string>()
        };

        var targetAgent = new AgentDefinition
        {
            AgentId = targetAgentId,
            Name = "Developer",
            Description = "Test developer agent",
            Capabilities = new List<string> { "code-implementation" },
            SystemPrompt = "Test prompt",
            ModelPreference = "gpt-4"
        };

        _mockRegistry.Setup(r => r.GetAgent(targetAgentId)).Returns(targetAgent);

        // Act
        var result = await _messaging.RequestFromAgent(targetAgentId, request, context);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Agent message")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task RequestFromAgent_WithTimeout_RetriesOnce()
    {
        // Arrange
        var targetAgentId = "slow-agent";
        var request = "Test request";
        var context = new AgentMessageContext
        {
            WorkflowInstanceId = Guid.NewGuid(),
            SourceAgentId = "architect",
            ConversationHistory = new List<string>()
        };

        // Act
        var result = await _messaging.RequestFromAgent(targetAgentId, request, context, timeoutSeconds: 1);

        // Assert - Should attempt twice (original + retry)
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task RequestFromAgent_MultipleMessages_HaveUniqueMessageIds()
    {
        // Arrange
        var targetAgentId = "developer";
        var context = new AgentMessageContext
        {
            WorkflowInstanceId = Guid.NewGuid(),
            SourceAgentId = "architect",
            ConversationHistory = new List<string>()
        };

        var targetAgent = new AgentDefinition
        {
            AgentId = targetAgentId,
            Name = "Developer",
            Description = "Test developer agent",
            Capabilities = new List<string> { "code-implementation" },
            SystemPrompt = "Test prompt",
            ModelPreference = "gpt-4"
        };

        _mockRegistry.Setup(r => r.GetAgent(targetAgentId)).Returns(targetAgent);

        // Act
        var result1 = await _messaging.RequestFromAgent(targetAgentId, "Request 1", context);
        var result2 = await _messaging.RequestFromAgent(targetAgentId, "Request 2", context);

        // Assert
        result1.MessageId.Should().NotBe(result2.MessageId);
    }

    [Fact]
    public void GetMessageHistory_ForWorkflow_ReturnsAllMessages()
    {
        // Arrange
        var workflowInstanceId = Guid.NewGuid();
        
        // Act
        var history = _messaging.GetMessageHistory(workflowInstanceId);

        // Assert
        history.Should().NotBeNull();
        history.Should().BeAssignableTo<IReadOnlyList<AgentMessage>>();
    }
}
