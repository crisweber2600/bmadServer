using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using bmadServer.ApiService.Agents;

namespace bmadServer.Tests.Unit;

public class AgentMessagingTests
{
    private readonly Mock<ILogger<AgentMessaging>> _mockLogger;
    private readonly IAgentRegistry _agentRegistry;
    private readonly AgentMessaging _agentMessaging;

    public AgentMessagingTests()
    {
        _mockLogger = new Mock<ILogger<AgentMessaging>>();
        _agentRegistry = new AgentRegistry();
        _agentMessaging = new AgentMessaging(_agentRegistry, _mockLogger.Object);
    }

    [Fact]
    public async Task RequestFromAgentAsync_WithValidRequest_ReturnsSuccessResponse()
    {
        // Arrange
        var request = new AgentRequest
        {
            SourceAgentId = "architect",
            RequestType = "gather-requirements",
            Payload = new { question = "What are the requirements?" },
            WorkflowContext = new Dictionary<string, object> { { "step", "design" } },
            ConversationHistory = new List<string> { "Starting conversation" }
        };

        var context = new Dictionary<string, object>
        {
            { "workflowInstanceId", "wf-123" }
        };

        // Act
        var response = await _agentMessaging.RequestFromAgentAsync("product-manager", request, context);

        // Assert
        Assert.NotNull(response);
        Assert.True(response.Success);
        Assert.Equal("product-manager", response.RespondingAgentId);
        Assert.NotNull(response.Data);
    }

    [Fact]
    public async Task RequestFromAgentAsync_WithInvalidTargetAgent_ReturnsErrorResponse()
    {
        // Arrange
        var request = new AgentRequest
        {
            SourceAgentId = "architect",
            RequestType = "gather-requirements",
            Payload = new { question = "What are the requirements?" },
            WorkflowContext = new Dictionary<string, object>(),
            ConversationHistory = new List<string>()
        };

        var context = new Dictionary<string, object>
        {
            { "workflowInstanceId", "wf-123" }
        };

        // Act
        var response = await _agentMessaging.RequestFromAgentAsync("non-existent-agent", request, context);

        // Assert
        Assert.NotNull(response);
        Assert.False(response.Success);
        Assert.Contains("not found", response.Error ?? "");
        Assert.Equal("non-existent-agent", response.RespondingAgentId);
    }

    [Fact]
    public async Task RequestFromAgentAsync_LogsRequestInitiation()
    {
        // Arrange
        var request = new AgentRequest
        {
            SourceAgentId = "architect",
            RequestType = "gather-requirements",
            Payload = new { question = "What are the requirements?" },
            WorkflowContext = new Dictionary<string, object>(),
            ConversationHistory = new List<string>()
        };

        var context = new Dictionary<string, object>
        {
            { "workflowInstanceId", "wf-123" }
        };

        // Act
        await _agentMessaging.RequestFromAgentAsync("product-manager", request, context);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Agent request initiated")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task RequestFromAgentAsync_LogsSuccessfulCompletion()
    {
        // Arrange
        var request = new AgentRequest
        {
            SourceAgentId = "architect",
            RequestType = "gather-requirements",
            Payload = new { question = "What are the requirements?" },
            WorkflowContext = new Dictionary<string, object>(),
            ConversationHistory = new List<string>()
        };

        var context = new Dictionary<string, object>
        {
            { "workflowInstanceId", "wf-123" }
        };

        // Act
        await _agentMessaging.RequestFromAgentAsync("product-manager", request, context);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Agent request completed successfully")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task RequestFromAgentAsync_WithCancellation_ReturnsCancelledResponse()
    {
        // Arrange
        var request = new AgentRequest
        {
            SourceAgentId = "architect",
            RequestType = "gather-requirements",
            Payload = new { question = "What are the requirements?" },
            WorkflowContext = new Dictionary<string, object>(),
            ConversationHistory = new List<string>()
        };

        var context = new Dictionary<string, object>
        {
            { "workflowInstanceId", "wf-123" }
        };

        var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel immediately

        // Act
        var response = await _agentMessaging.RequestFromAgentAsync("product-manager", request, context, cts.Token);

        // Assert
        Assert.NotNull(response);
        Assert.False(response.Success);
        Assert.Contains("cancelled", response.Error ?? "", StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RequestFromAgentAsync_LogsErrorForInvalidAgent()
    {
        // Arrange
        var request = new AgentRequest
        {
            SourceAgentId = "architect",
            RequestType = "gather-requirements",
            Payload = new { question = "What are the requirements?" },
            WorkflowContext = new Dictionary<string, object>(),
            ConversationHistory = new List<string>()
        };

        var context = new Dictionary<string, object>
        {
            { "workflowInstanceId", "wf-123" }
        };

        // Act
        await _agentMessaging.RequestFromAgentAsync("non-existent-agent", request, context);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("not found in registry")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task RequestFromAgentAsync_ResponseIncludesTimestamp()
    {
        // Arrange
        var request = new AgentRequest
        {
            SourceAgentId = "architect",
            RequestType = "gather-requirements",
            Payload = new { question = "What are the requirements?" },
            WorkflowContext = new Dictionary<string, object>(),
            ConversationHistory = new List<string>()
        };

        var context = new Dictionary<string, object>
        {
            { "workflowInstanceId", "wf-123" }
        };

        var beforeRequest = DateTime.UtcNow;

        // Act
        var response = await _agentMessaging.RequestFromAgentAsync("product-manager", request, context);

        // Assert
        var afterRequest = DateTime.UtcNow;
        Assert.NotNull(response);
        Assert.True(response.Timestamp >= beforeRequest);
        Assert.True(response.Timestamp <= afterRequest);
    }

    [Fact]
    public async Task RequestFromAgentAsync_ResponseIncludesRespondingAgentId()
    {
        // Arrange
        var request = new AgentRequest
        {
            SourceAgentId = "architect",
            RequestType = "gather-requirements",
            Payload = new { question = "What are the requirements?" },
            WorkflowContext = new Dictionary<string, object>(),
            ConversationHistory = new List<string>()
        };

        var context = new Dictionary<string, object>
        {
            { "workflowInstanceId", "wf-123" }
        };

        // Act
        var response = await _agentMessaging.RequestFromAgentAsync("product-manager", request, context);

        // Assert
        Assert.NotNull(response);
        Assert.Equal("product-manager", response.RespondingAgentId);
    }

    [Fact]
    public async Task RequestFromAgentAsync_WithoutWorkflowInstanceId_UsesUnknown()
    {
        // Arrange
        var request = new AgentRequest
        {
            SourceAgentId = "architect",
            RequestType = "gather-requirements",
            Payload = new { question = "What are the requirements?" },
            WorkflowContext = new Dictionary<string, object>(),
            ConversationHistory = new List<string>()
        };

        var context = new Dictionary<string, object>(); // No workflowInstanceId

        // Act
        var response = await _agentMessaging.RequestFromAgentAsync("product-manager", request, context);

        // Assert - Should not throw and should handle missing workflowInstanceId
        Assert.NotNull(response);
        Assert.True(response.Success);
    }

    [Fact]
    public void AgentRequest_IncludesAllRequiredFields()
    {
        // Arrange & Act
        var request = new AgentRequest
        {
            SourceAgentId = "architect",
            RequestType = "gather-requirements",
            Payload = new { question = "What are the requirements?" },
            WorkflowContext = new Dictionary<string, object> { { "step", "design" } },
            ConversationHistory = new List<string> { "Message 1", "Message 2" }
        };

        // Assert
        Assert.NotNull(request.SourceAgentId);
        Assert.NotNull(request.RequestType);
        Assert.NotNull(request.Payload);
        Assert.NotNull(request.WorkflowContext);
        Assert.NotNull(request.ConversationHistory);
        Assert.NotEmpty(request.WorkflowContext);
        Assert.NotEmpty(request.ConversationHistory);
    }

    [Fact]
    public void AgentMessage_IncludesAllRequiredFields()
    {
        // Arrange & Act
        var message = new AgentMessage
        {
            MessageId = Guid.NewGuid().ToString(),
            Timestamp = DateTime.UtcNow,
            SourceAgent = "architect",
            TargetAgent = "product-manager",
            MessageType = "request",
            Content = new { requestType = "gather-requirements" },
            WorkflowInstanceId = "wf-123"
        };

        // Assert
        Assert.NotNull(message.MessageId);
        Assert.NotEqual(default(DateTime), message.Timestamp);
        Assert.NotNull(message.SourceAgent);
        Assert.NotNull(message.TargetAgent);
        Assert.NotNull(message.MessageType);
        Assert.NotNull(message.Content);
        Assert.NotNull(message.WorkflowInstanceId);
    }

    [Fact]
    public void AgentResponse_IncludesAllRequiredFields()
    {
        // Arrange & Act
        var response = new AgentResponse
        {
            Success = true,
            Data = new { result = "success" },
            RespondingAgentId = "product-manager",
            Timestamp = DateTime.UtcNow
        };

        // Assert
        Assert.True(response.Success);
        Assert.NotNull(response.Data);
        Assert.NotNull(response.RespondingAgentId);
        Assert.NotEqual(default(DateTime), response.Timestamp);
    }

    [Fact]
    public void AgentResponse_CanHaveErrorMessage()
    {
        // Arrange & Act
        var response = new AgentResponse
        {
            Success = false,
            Error = "Something went wrong",
            RespondingAgentId = "product-manager",
            Timestamp = DateTime.UtcNow
        };

        // Assert
        Assert.False(response.Success);
        Assert.NotNull(response.Error);
        Assert.Contains("wrong", response.Error);
    }
}
