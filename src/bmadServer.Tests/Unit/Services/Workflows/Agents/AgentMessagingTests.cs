using System.Text.Json;
using bmadServer.ApiService.Data;
using bmadServer.ApiService.Services.Workflows;
using bmadServer.ApiService.Services.Workflows.Agents;
using bmadServer.Tests.Helpers;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace bmadServer.Tests.Unit.Services.Workflows.Agents;

public class AgentMessagingTests : IDisposable
{
    private readonly Mock<IAgentRegistry> _mockAgentRegistry;
    private readonly Mock<IAgentRouter> _mockAgentRouter;
    private readonly ApplicationDbContext _dbContext;
    private readonly SqliteConnection _connection;
    private readonly Mock<ILogger<AgentMessaging>> _mockLogger;
    private readonly AgentMessaging _sut;

    public AgentMessagingTests()
    {
        var options = TestDatabaseHelper.CreateSqliteOptions(out _connection);
        _dbContext = new ApplicationDbContext(options);
        _dbContext.Database.EnsureCreated();
        _mockAgentRegistry = new Mock<IAgentRegistry>();
        _mockAgentRouter = new Mock<IAgentRouter>();
        _mockLogger = new Mock<ILogger<AgentMessaging>>();

        _sut = new AgentMessaging(
            _mockAgentRegistry.Object,
            _mockAgentRouter.Object,
            _dbContext,
            _mockLogger.Object);
    }

    public void Dispose()
    {
        _dbContext?.Dispose();
        _connection?.Dispose();
    }

    [Fact]
    public async Task RequestFromAgentAsync_WithValidRequest_ReturnsSuccessResponse()
    {
        var targetAgentId = "architect";
        var requestType = "get-architecture-input";
        var payload = new { requirements = "scalable system" };
        var context = CreateWorkflowContext();

        var targetAgent = new AgentDefinition
        {
            AgentId = targetAgentId,
            Name = "Architect",
            Capabilities = ["create-architecture"],
            SystemPrompt = "You are an architect",
            ModelPreference = "gpt-4",
            Temperature = 0.7m
        };

        var agentResult = new AgentResult
        {
            Success = true,
            Output = JsonDocument.Parse("""{"architecture":"microservices"}"""),
            ErrorMessage = null,
            IsRetryable = false
        };

        _mockAgentRegistry.Setup(r => r.GetAgent(targetAgentId))
            .Returns(targetAgent);

        var mockHandler = new Mock<IAgentHandler>();
        mockHandler.Setup(h => h.ExecuteAsync(It.IsAny<AgentContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(agentResult);

        _mockAgentRouter.Setup(r => r.GetHandler(targetAgentId))
            .Returns(mockHandler.Object);

        var result = await _sut.RequestFromAgentAsync(
            targetAgentId,
            requestType,
            payload,
            context);

        Assert.True(result.Success);
        Assert.NotNull(result.Content);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public async Task RequestFromAgentAsync_WithUnregisteredAgent_ReturnsErrorImmediately()
    {
        var targetAgentId = "unknown-agent";
        var requestType = "some-request";
        var payload = new { };
        var context = CreateWorkflowContext();

        _mockAgentRegistry.Setup(r => r.GetAgent(targetAgentId))
            .Returns((AgentDefinition?)null);

        var result = await _sut.RequestFromAgentAsync(
            targetAgentId,
            requestType,
            payload,
            context);

        Assert.False(result.Success);
        Assert.Contains("not found", result.ErrorMessage);
        Assert.False(result.IsRetryable);

        _mockAgentRouter.Verify(
            r => r.GetHandler(It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task RequestFromAgentAsync_WithTimeoutOnFirstAttempt_RetriesOnce()
    {
        var targetAgentId = "developer";
        var requestType = "write-code";
        var payload = new { };
        var context = CreateWorkflowContext();

        var targetAgent = new AgentDefinition
        {
            AgentId = targetAgentId,
            Name = "Developer",
            Capabilities = ["write-code"],
            SystemPrompt = "You are a developer",
            ModelPreference = "gpt-4",
            Temperature = 0.5m
        };

        var successResult = new AgentResult
        {
            Success = true,
            Output = JsonDocument.Parse("""{"code":"..."}"""),
            ErrorMessage = null,
            IsRetryable = false
        };

        _mockAgentRegistry.Setup(r => r.GetAgent(targetAgentId))
            .Returns(targetAgent);

        var mockHandler = new Mock<IAgentHandler>();
        var callCount = 0;
        mockHandler.Setup(h => h.ExecuteAsync(It.IsAny<AgentContext>(), It.IsAny<CancellationToken>()))
            .Returns(async (AgentContext _, CancellationToken ct) =>
            {
                callCount++;
                if (callCount == 1)
                {
                    ct.ThrowIfCancellationRequested();
                    await Task.Delay(100, ct);
                    ct.ThrowIfCancellationRequested();
                }
                return successResult;
            });

        _mockAgentRouter.Setup(r => r.GetHandler(targetAgentId))
            .Returns(mockHandler.Object);

        var timeout = TimeSpan.FromMilliseconds(50);
        var result = await _sut.RequestFromAgentAsync(
            targetAgentId,
            requestType,
            payload,
            context,
            timeout);

        Assert.True(result.Success);
        Assert.Equal(2, callCount);
    }

    [Fact]
    public async Task RequestFromAgentAsync_WithTimeoutOnBothAttempts_ReturnsTimeout()
    {
        var targetAgentId = "architect";
        var requestType = "design-system";
        var payload = new { };
        var context = CreateWorkflowContext();

        var targetAgent = new AgentDefinition
        {
            AgentId = targetAgentId,
            Name = "Architect",
            Capabilities = ["design-system"],
            SystemPrompt = "You are an architect",
            ModelPreference = "gpt-4",
            Temperature = 0.7m
        };

        _mockAgentRegistry.Setup(r => r.GetAgent(targetAgentId))
            .Returns(targetAgent);

        var mockHandler = new Mock<IAgentHandler>();
        mockHandler.Setup(h => h.ExecuteAsync(It.IsAny<AgentContext>(), It.IsAny<CancellationToken>()))
            .Returns(async (AgentContext _, CancellationToken ct) =>
            {
                await Task.Delay(500, ct);
                return new AgentResult { Success = true };
            });

        _mockAgentRouter.Setup(r => r.GetHandler(targetAgentId))
            .Returns(mockHandler.Object);

        var timeout = TimeSpan.FromMilliseconds(50);
        var result = await _sut.RequestFromAgentAsync(
            targetAgentId,
            requestType,
            payload,
            context,
            timeout);

        Assert.False(result.Success);
        Assert.Contains("timed out", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        Assert.True(result.IsRetryable);
    }

    [Fact]
    public async Task RequestFromAgentAsync_WithAgentException_ReturnsErrorResponse()
    {
        var targetAgentId = "analyst";
        var requestType = "analyze-data";
        var payload = new { };
        var context = CreateWorkflowContext();

        var targetAgent = new AgentDefinition
        {
            AgentId = targetAgentId,
            Name = "Analyst",
            Capabilities = ["analyze-data"],
            SystemPrompt = "You are an analyst",
            ModelPreference = "gpt-4",
            Temperature = 0.6m
        };

        _mockAgentRegistry.Setup(r => r.GetAgent(targetAgentId))
            .Returns(targetAgent);

        var mockHandler = new Mock<IAgentHandler>();
        mockHandler.Setup(h => h.ExecuteAsync(It.IsAny<AgentContext>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Agent execution failed"));

        _mockAgentRouter.Setup(r => r.GetHandler(targetAgentId))
            .Returns(mockHandler.Object);

        var result = await _sut.RequestFromAgentAsync(
            targetAgentId,
            requestType,
            payload,
            context);

        Assert.False(result.Success);
        Assert.Contains("Agent execution failed", result.ErrorMessage);
        Assert.False(result.IsRetryable);
    }

    [Fact]
    public async Task RequestFromAgentAsync_WithNullTargetAgentId_ThrowsArgumentException()
    {
        var context = CreateWorkflowContext();

        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await _sut.RequestFromAgentAsync(
                null!,
                "request-type",
                new { },
                context));
    }

    [Fact]
    public async Task RequestFromAgentAsync_WithNullPayload_ThrowsArgumentNullException()
    {
        var context = CreateWorkflowContext();

        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await _sut.RequestFromAgentAsync(
                "agent-id",
                "request-type",
                null!,
                context));
    }

    [Fact]
    public async Task RequestFromAgentAsync_WithNullContext_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await _sut.RequestFromAgentAsync(
                "agent-id",
                "request-type",
                new { },
                null!));
    }

    [Fact]
    public async Task RequestFromAgentAsync_CreatesCorrelatedMessages()
    {
        var targetAgentId = "designer";
        var requestType = "create-ui-design";
        var payload = new { };
        var context = CreateWorkflowContext();

        var targetAgent = new AgentDefinition
        {
            AgentId = targetAgentId,
            Name = "Designer",
            Capabilities = ["create-ui-design"],
            SystemPrompt = "You are a designer",
            ModelPreference = "gpt-4",
            Temperature = 0.8m
        };

        var successResult = new AgentResult
        {
            Success = true,
            Output = JsonDocument.Parse("""{"design":""}"""),
            ErrorMessage = null,
            IsRetryable = false
        };

        _mockAgentRegistry.Setup(r => r.GetAgent(targetAgentId))
            .Returns(targetAgent);

        var mockHandler = new Mock<IAgentHandler>();
        mockHandler.Setup(h => h.ExecuteAsync(It.IsAny<AgentContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(successResult);

        _mockAgentRouter.Setup(r => r.GetHandler(targetAgentId))
            .Returns(mockHandler.Object);

        var result = await _sut.RequestFromAgentAsync(
            targetAgentId,
            requestType,
            payload,
            context);

        Assert.True(result.Success);
    }

    private WorkflowContext CreateWorkflowContext()
    {
        return new WorkflowContext
        {
            WorkflowInstanceId = Guid.NewGuid(),
            CurrentStepId = Guid.NewGuid(),
            CurrentStepName = "product-manager",
            StepOutputs = new(),
            WorkflowState = new()
        };
    }
}
