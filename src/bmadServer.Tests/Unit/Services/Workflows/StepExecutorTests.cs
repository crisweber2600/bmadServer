using bmadServer.ApiService.Data;
using bmadServer.ApiService.Models.Workflows;
using bmadServer.ApiService.Services.Workflows;
using bmadServer.ApiService.Services.Workflows.Agents;
using bmadServer.ServiceDefaults.Models.Workflows;
using bmadServer.ServiceDefaults.Services.Workflows;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text.Json;
using Xunit;

namespace bmadServer.Tests.Unit.Services.Workflows;

public class StepExecutorTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<IAgentRouter> _agentRouterMock;
    private readonly Mock<IWorkflowRegistry> _workflowRegistryMock;
    private readonly Mock<IWorkflowInstanceService> _workflowInstanceServiceMock;
    private readonly Mock<ILogger<StepExecutor>> _loggerMock;
    private readonly StepExecutor _stepExecutor;

    public StepExecutorTests()
    {
        // Setup in-memory database
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);

        _agentRouterMock = new Mock<IAgentRouter>();
        _workflowRegistryMock = new Mock<IWorkflowRegistry>();
        _workflowInstanceServiceMock = new Mock<IWorkflowInstanceService>();
        _loggerMock = new Mock<ILogger<StepExecutor>>();

        var sharedContextServiceMock = new Mock<ISharedContextService>();
        sharedContextServiceMock.Setup(s => s.GetContextAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SharedContext?)null);

        _stepExecutor = new StepExecutor(
            _context,
            _agentRouterMock.Object,
            _workflowRegistryMock.Object,
            _workflowInstanceServiceMock.Object,
            sharedContextServiceMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task ExecuteStepAsync_WithValidWorkflow_ExecutesSuccessfully()
    {
        // Arrange
        var workflowId = "test-workflow";
        var instanceId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var definition = new WorkflowDefinition
        {
            WorkflowId = workflowId,
            Name = "Test Workflow",
            Description = "Test",
            EstimatedDuration = TimeSpan.FromMinutes(10),
            RequiredRoles = new List<string>(),
            Steps = new List<WorkflowStep>
            {
                new WorkflowStep
                {
                    StepId = "step-1",
                    Name = "First Step",
                    AgentId = "test-agent",
                    OutputSchema = null,
                    IsOptional = false,
                    CanSkip = false
                }
            }
        };

        var instance = new WorkflowInstance
        {
            Id = instanceId,
            WorkflowDefinitionId = workflowId,
            UserId = userId,
            CurrentStep = 1,
            Status = WorkflowStatus.Running,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var mockHandler = new MockAgentHandler(shouldSucceed: true);

        _workflowInstanceServiceMock
            .Setup(s => s.GetWorkflowInstanceAsync(instanceId))
            .ReturnsAsync(instance);

        _workflowRegistryMock
            .Setup(r => r.GetWorkflow(workflowId))
            .Returns(definition);

        _agentRouterMock
            .Setup(r => r.GetHandler("test-agent"))
            .Returns(mockHandler);

        _workflowInstanceServiceMock
            .Setup(s => s.TransitionStateAsync(instanceId, WorkflowStatus.Completed))
            .ReturnsAsync(true);

        // Act
        var result = await _stepExecutor.ExecuteStepAsync(instanceId);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("step-1", result.StepId);
        Assert.Equal("First Step", result.StepName);
        Assert.Equal(StepExecutionStatus.Completed, result.Status);
        Assert.Equal(WorkflowStatus.Completed, result.NewWorkflowStatus);

        // Verify step history was created
        var stepHistory = await _context.WorkflowStepHistories
            .FirstOrDefaultAsync(h => h.WorkflowInstanceId == instanceId);
        Assert.NotNull(stepHistory);
        Assert.Equal("step-1", stepHistory.StepId);
        Assert.Equal(StepExecutionStatus.Completed, stepHistory.Status);
        Assert.NotNull(stepHistory.CompletedAt);
    }

    [Fact]
    public async Task ExecuteStepAsync_WithNonExistentWorkflow_ReturnsError()
    {
        // Arrange
        var instanceId = Guid.NewGuid();

        _workflowInstanceServiceMock
            .Setup(s => s.GetWorkflowInstanceAsync(instanceId))
            .ReturnsAsync((WorkflowInstance?)null);

        // Act
        var result = await _stepExecutor.ExecuteStepAsync(instanceId);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(StepExecutionStatus.Failed, result.Status);
        Assert.Contains("not found", result.ErrorMessage);
    }

    [Fact]
    public async Task ExecuteStepAsync_WithNoAgentHandler_ReturnsError()
    {
        // Arrange
        var workflowId = "test-workflow";
        var instanceId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var definition = new WorkflowDefinition
        {
            WorkflowId = workflowId,
            Name = "Test Workflow",
            Description = "Test",
            EstimatedDuration = TimeSpan.FromMinutes(10),
            RequiredRoles = new List<string>(),
            Steps = new List<WorkflowStep>
            {
                new WorkflowStep
                {
                    StepId = "step-1",
                    Name = "First Step",
                    AgentId = "missing-agent",
                    OutputSchema = null,
                    IsOptional = false,
                    CanSkip = false
                }
            }
        };

        var instance = new WorkflowInstance
        {
            Id = instanceId,
            WorkflowDefinitionId = workflowId,
            UserId = userId,
            CurrentStep = 1,
            Status = WorkflowStatus.Running,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _workflowInstanceServiceMock
            .Setup(s => s.GetWorkflowInstanceAsync(instanceId))
            .ReturnsAsync(instance);

        _workflowRegistryMock
            .Setup(r => r.GetWorkflow(workflowId))
            .Returns(definition);

        _agentRouterMock
            .Setup(r => r.GetHandler("missing-agent"))
            .Returns((IAgentHandler?)null);

        // Act
        var result = await _stepExecutor.ExecuteStepAsync(instanceId);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(StepExecutionStatus.Failed, result.Status);
        Assert.Contains("No handler found", result.ErrorMessage);
    }

    [Fact]
    public async Task ExecuteStepAsync_WithAgentFailure_TransitionsToCorrectState()
    {
        // Arrange
        var workflowId = "test-workflow";
        var instanceId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var definition = new WorkflowDefinition
        {
            WorkflowId = workflowId,
            Name = "Test Workflow",
            Description = "Test",
            EstimatedDuration = TimeSpan.FromMinutes(10),
            RequiredRoles = new List<string>(),
            Steps = new List<WorkflowStep>
            {
                new WorkflowStep
                {
                    StepId = "step-1",
                    Name = "First Step",
                    AgentId = "test-agent",
                    OutputSchema = null,
                    IsOptional = false,
                    CanSkip = false
                }
            }
        };

        var instance = new WorkflowInstance
        {
            Id = instanceId,
            WorkflowDefinitionId = workflowId,
            UserId = userId,
            CurrentStep = 1,
            Status = WorkflowStatus.Running,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var mockHandler = new MockAgentHandler(
            shouldSucceed: false, 
            isRetryable: true, 
            errorMessage: "Agent failed");

        _workflowInstanceServiceMock
            .Setup(s => s.GetWorkflowInstanceAsync(instanceId))
            .ReturnsAsync(instance);

        _workflowRegistryMock
            .Setup(r => r.GetWorkflow(workflowId))
            .Returns(definition);

        _agentRouterMock
            .Setup(r => r.GetHandler("test-agent"))
            .Returns(mockHandler);

        _workflowInstanceServiceMock
            .Setup(s => s.TransitionStateAsync(instanceId, WorkflowStatus.WaitingForInput))
            .ReturnsAsync(true);

        // Act
        var result = await _stepExecutor.ExecuteStepAsync(instanceId);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(StepExecutionStatus.Failed, result.Status);
        Assert.Equal(WorkflowStatus.WaitingForInput, result.NewWorkflowStatus);
        Assert.Contains("Agent failed", result.ErrorMessage);
    }

    [Fact]
    public async Task ExecuteStepAsync_WithInvalidOutputSchema_FailsValidation()
    {
        // Arrange
        var workflowId = "test-workflow";
        var instanceId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var outputSchema = @"{
            ""type"": ""object"",
            ""properties"": {
                ""requiredField"": { ""type"": ""string"" }
            },
            ""required"": [""requiredField""]
        }";

        var definition = new WorkflowDefinition
        {
            WorkflowId = workflowId,
            Name = "Test Workflow",
            Description = "Test",
            EstimatedDuration = TimeSpan.FromMinutes(10),
            RequiredRoles = new List<string>(),
            Steps = new List<WorkflowStep>
            {
                new WorkflowStep
                {
                    StepId = "step-1",
                    Name = "First Step",
                    AgentId = "test-agent",
                    OutputSchema = outputSchema,
                    IsOptional = false,
                    CanSkip = false
                }
            }
        };

        var instance = new WorkflowInstance
        {
            Id = instanceId,
            WorkflowDefinitionId = workflowId,
            UserId = userId,
            CurrentStep = 1,
            Status = WorkflowStatus.Running,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Mock handler that returns output without required field
        var mockHandler = new MockAgentHandler(
            shouldSucceed: true,
            executeFunc: async (context) =>
            {
                var output = JsonDocument.Parse(JsonSerializer.Serialize(new { wrongField = "value" }));
                return await Task.FromResult(new AgentResult
                {
                    Success = true,
                    Output = output
                });
            });

        _workflowInstanceServiceMock
            .Setup(s => s.GetWorkflowInstanceAsync(instanceId))
            .ReturnsAsync(instance);

        _workflowRegistryMock
            .Setup(r => r.GetWorkflow(workflowId))
            .Returns(definition);

        _agentRouterMock
            .Setup(r => r.GetHandler("test-agent"))
            .Returns(mockHandler);

        _workflowInstanceServiceMock
            .Setup(s => s.TransitionStateAsync(instanceId, WorkflowStatus.Failed))
            .ReturnsAsync(true);

        // Act
        var result = await _stepExecutor.ExecuteStepAsync(instanceId);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(StepExecutionStatus.Failed, result.Status);
        Assert.Contains("validation failed", result.ErrorMessage);
    }

    [Fact]
    public async Task ExecuteStepAsync_WithMultipleSteps_AdvancesToNextStep()
    {
        // Arrange
        var workflowId = "test-workflow";
        var instanceId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var definition = new WorkflowDefinition
        {
            WorkflowId = workflowId,
            Name = "Test Workflow",
            Description = "Test",
            EstimatedDuration = TimeSpan.FromMinutes(10),
            RequiredRoles = new List<string>(),
            Steps = new List<WorkflowStep>
            {
                new WorkflowStep
                {
                    StepId = "step-1",
                    Name = "First Step",
                    AgentId = "test-agent",
                    OutputSchema = null,
                    IsOptional = false,
                    CanSkip = false
                },
                new WorkflowStep
                {
                    StepId = "step-2",
                    Name = "Second Step",
                    AgentId = "test-agent",
                    OutputSchema = null,
                    IsOptional = false,
                    CanSkip = false
                }
            }
        };

        var instance = new WorkflowInstance
        {
            Id = instanceId,
            WorkflowDefinitionId = workflowId,
            UserId = userId,
            CurrentStep = 1,
            Status = WorkflowStatus.Running,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var mockHandler = new MockAgentHandler(shouldSucceed: true);

        _workflowInstanceServiceMock
            .Setup(s => s.GetWorkflowInstanceAsync(instanceId))
            .ReturnsAsync(instance);

        _workflowRegistryMock
            .Setup(r => r.GetWorkflow(workflowId))
            .Returns(definition);

        _agentRouterMock
            .Setup(r => r.GetHandler("test-agent"))
            .Returns(mockHandler);

        // Act
        var result = await _stepExecutor.ExecuteStepAsync(instanceId);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(2, result.NextStep);
        Assert.Equal(WorkflowStatus.Running, result.NewWorkflowStatus);
    }

    [Fact]
    public async Task ExecuteStepWithStreamingAsync_YieldsProgressUpdates()
    {
        // Arrange
        var workflowId = "test-workflow";
        var instanceId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var definition = new WorkflowDefinition
        {
            WorkflowId = workflowId,
            Name = "Test Workflow",
            Description = "Test",
            EstimatedDuration = TimeSpan.FromMinutes(10),
            RequiredRoles = new List<string>(),
            Steps = new List<WorkflowStep>
            {
                new WorkflowStep
                {
                    StepId = "step-1",
                    Name = "First Step",
                    AgentId = "test-agent",
                    OutputSchema = null,
                    IsOptional = false,
                    CanSkip = false
                }
            }
        };

        var instance = new WorkflowInstance
        {
            Id = instanceId,
            WorkflowDefinitionId = workflowId,
            UserId = userId,
            CurrentStep = 1,
            Status = WorkflowStatus.Running,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var mockHandler = new MockAgentHandler(shouldSucceed: true);

        _workflowInstanceServiceMock
            .Setup(s => s.GetWorkflowInstanceAsync(instanceId))
            .ReturnsAsync(instance);

        _workflowRegistryMock
            .Setup(r => r.GetWorkflow(workflowId))
            .Returns(definition);

        _agentRouterMock
            .Setup(r => r.GetHandler("test-agent"))
            .Returns(mockHandler);

        // Act
        var progressUpdates = new List<StepProgress>();
        await foreach (var progress in _stepExecutor.ExecuteStepWithStreamingAsync(instanceId))
        {
            progressUpdates.Add(progress);
        }

        // Assert
        Assert.NotEmpty(progressUpdates);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
