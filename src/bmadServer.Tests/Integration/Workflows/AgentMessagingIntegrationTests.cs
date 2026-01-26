using System.Text.Json;
using bmadServer.ApiService.Data;
using bmadServer.ApiService.Data.Entities;
using bmadServer.ApiService.Models.Workflows;
using bmadServer.ApiService.Services.Workflows;
using bmadServer.ApiService.Services.Workflows.Agents;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace bmadServer.Tests.Integration.Workflows;

public class AgentMessagingIntegrationTests
{
    private readonly ApplicationDbContext _dbContext;
    private readonly Mock<IAgentRouter> _mockAgentRouter;
    private readonly Mock<ILogger<AgentMessaging>> _mockLogger;
    private readonly AgentRegistry _agentRegistry;
    private readonly AgentMessaging _agentMessaging;

    public AgentMessagingIntegrationTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
            .Options;

        _dbContext = new ApplicationDbContext(options);
        _mockAgentRouter = new Mock<IAgentRouter>();
        _mockLogger = new Mock<ILogger<AgentMessaging>>();
        var registryLogger = new Mock<ILogger<AgentRegistry>>();
        _agentRegistry = new AgentRegistry(registryLogger.Object);
        
        _agentMessaging = new AgentMessaging(
            _agentRegistry,
            _mockAgentRouter.Object,
            _dbContext,
            _mockLogger.Object);
    }

    [Fact]
    public async Task RequestFromAgentAsync_WithSuccessfulResponse_PersistsMessagesToDatabase()
    {
        var workflowId = Guid.NewGuid();
        CreateWorkflowInstance(workflowId);

        var targetAgentId = "architect";
        var requestType = "get-architecture";
        var payload = new { };
        var context = new WorkflowContext
        {
            WorkflowInstanceId = workflowId,
            CurrentStepId = Guid.NewGuid(),
            CurrentStepName = "product-manager"
        };

        var successResult = new AgentResult
        {
            Success = true,
            Output = JsonDocument.Parse("""{"architecture":"microservices"}"""),
            ErrorMessage = null,
            IsRetryable = false
        };

        var mockHandler = new Mock<IAgentHandler>();
        mockHandler.Setup(h => h.ExecuteAsync(It.IsAny<AgentContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(successResult);

        _mockAgentRouter.Setup(r => r.GetHandler(targetAgentId))
            .Returns(mockHandler.Object);

        await _agentMessaging.RequestFromAgentAsync(
            targetAgentId,
            requestType,
            payload,
            context);

        await Task.Delay(100);

        var logs = _dbContext.AgentMessageLogs
            .Where(m => m.WorkflowInstanceId == workflowId)
            .ToList();

        Assert.NotEmpty(logs);
    }

    [Fact]
    public async Task GetConversationHistoryAsync_ReturnsMessagesInTimestampOrder()
    {
        var workflowId = Guid.NewGuid();
        CreateWorkflowInstance(workflowId);

        var msg1 = new AgentMessageLog
        {
            Id = Guid.NewGuid(),
            MessageId = Guid.NewGuid(),
            Timestamp = DateTime.UtcNow.AddSeconds(-10),
            SourceAgent = "product-manager",
            TargetAgent = "architect",
            MessageType = 0,
            Content = JsonDocument.Parse("{}"),
            WorkflowInstanceId = workflowId,
            CorrelationId = "corr-1"
        };

        var msg2 = new AgentMessageLog
        {
            Id = Guid.NewGuid(),
            MessageId = Guid.NewGuid(),
            Timestamp = DateTime.UtcNow,
            SourceAgent = "architect",
            TargetAgent = "product-manager",
            MessageType = 1,
            Content = JsonDocument.Parse("{}"),
            WorkflowInstanceId = workflowId,
            CorrelationId = "corr-1"
        };

        _dbContext.AgentMessageLogs.Add(msg1);
        _dbContext.AgentMessageLogs.Add(msg2);
        await _dbContext.SaveChangesAsync();

        var history = await _agentMessaging.GetConversationHistoryAsync(workflowId);

        Assert.Equal(2, history.Count);
        Assert.Equal("product-manager", history[0].SourceAgent);
        Assert.Equal("architect", history[1].SourceAgent);
    }

    [Fact]
    public async Task GetConversationHistoryAsync_OnlyReturnsMessagesForSpecificWorkflow()
    {
        var workflowId1 = Guid.NewGuid();
        var workflowId2 = Guid.NewGuid();
        CreateWorkflowInstance(workflowId1);
        CreateWorkflowInstance(workflowId2);

        var msg1 = new AgentMessageLog
        {
            Id = Guid.NewGuid(),
            MessageId = Guid.NewGuid(),
            Timestamp = DateTime.UtcNow,
            SourceAgent = "agent-a",
            TargetAgent = "agent-b",
            MessageType = 0,
            Content = JsonDocument.Parse("{}"),
            WorkflowInstanceId = workflowId1,
            CorrelationId = "corr-1"
        };

        var msg2 = new AgentMessageLog
        {
            Id = Guid.NewGuid(),
            MessageId = Guid.NewGuid(),
            Timestamp = DateTime.UtcNow,
            SourceAgent = "agent-c",
            TargetAgent = "agent-d",
            MessageType = 0,
            Content = JsonDocument.Parse("{}"),
            WorkflowInstanceId = workflowId2,
            CorrelationId = "corr-2"
        };

        _dbContext.AgentMessageLogs.Add(msg1);
        _dbContext.AgentMessageLogs.Add(msg2);
        await _dbContext.SaveChangesAsync();

        var history = await _agentMessaging.GetConversationHistoryAsync(workflowId1);

        Assert.Single(history);
        Assert.Equal("agent-a", history[0].SourceAgent);
    }

    [Fact]
    public async Task RequestFromAgentAsync_WithConcurrentRequests_AllSucceed()
    {
        var workflowId = Guid.NewGuid();
        CreateWorkflowInstance(workflowId);

        var successResult = new AgentResult
        {
            Success = true,
            Output = JsonDocument.Parse("""{"result":"success"}"""),
            ErrorMessage = null,
            IsRetryable = false
        };

        var mockHandler = new Mock<IAgentHandler>();
        mockHandler.Setup(h => h.ExecuteAsync(It.IsAny<AgentContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(successResult);

        _mockAgentRouter.Setup(r => r.GetHandler(It.IsAny<string>()))
            .Returns(mockHandler.Object);

        var tasks = new List<Task<AgentResponse>>();

        for (int i = 0; i < 5; i++)
        {
            var context = new WorkflowContext
            {
                WorkflowInstanceId = workflowId,
                CurrentStepId = Guid.NewGuid(),
                CurrentStepName = $"step-{i}"
            };

            var task = _agentMessaging.RequestFromAgentAsync(
                "developer",
                $"request-{i}",
                new { },
                context);

            tasks.Add(task);
        }

        var results = await Task.WhenAll(tasks);

        Assert.All(results, r => Assert.True(r.Success));
    }

    [Fact]
    public async Task RequestFromAgentAsync_WithErrorResponse_LogsErrorMessage()
    {
        var workflowId = Guid.NewGuid();
        CreateWorkflowInstance(workflowId);

        var context = new WorkflowContext
        {
            WorkflowInstanceId = workflowId,
            CurrentStepId = Guid.NewGuid(),
            CurrentStepName = "product-manager"
        };

        var errorResult = new AgentResult
        {
            Success = false,
            Output = null,
            ErrorMessage = "Agent processing failed",
            IsRetryable = false
        };

        var mockHandler = new Mock<IAgentHandler>();
        mockHandler.Setup(h => h.ExecuteAsync(It.IsAny<AgentContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(errorResult);

        _mockAgentRouter.Setup(r => r.GetHandler("analyst"))
            .Returns(mockHandler.Object);

        var result = await _agentMessaging.RequestFromAgentAsync(
            "analyst",
            "analyze-data",
            new { },
            context);

        Assert.False(result.Success);
        Assert.Equal("Agent processing failed", result.ErrorMessage);
    }

    private void CreateWorkflowInstance(Guid workflowId)
    {
        var instance = new WorkflowInstance
        {
            Id = workflowId,
            WorkflowDefinitionId = "test-workflow",
            UserId = Guid.NewGuid(),
            Status = WorkflowStatus.Running,
            CreatedAt = DateTime.UtcNow,
            Context = JsonDocument.Parse("{}")
        };

        _dbContext.WorkflowInstances.Add(instance);
        _dbContext.SaveChanges();
    }
}
