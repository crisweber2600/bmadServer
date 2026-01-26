using bmadServer.ApiService.Controllers;
using bmadServer.ApiService.Data;
using bmadServer.ApiService.Hubs;
using bmadServer.ApiService.Models.Workflows;
using bmadServer.ApiService.Services;
using bmadServer.ApiService.Services.Workflows;
using bmadServer.ApiService.Services.Workflows.Agents;
using bmadServer.ServiceDefaults.Models.Workflows;
using bmadServer.ServiceDefaults.Services.Workflows;
using bmadServer.Tests.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;
using Xunit;

namespace bmadServer.Tests.Integration.Workflows;

public class StepExecutionIntegrationTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly TestWorkflowRegistry _workflowRegistry;
    private readonly IAgentRouter _agentRouter;
    private readonly IAgentRegistry _agentRegistry;
    private readonly IWorkflowInstanceService _workflowInstanceService;
    private readonly IStepExecutor _stepExecutor;
    private readonly WorkflowsController _controller;
    private readonly Guid _testUserId;

    public StepExecutionIntegrationTests()
    {
        // Setup in-memory database
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);

        // Create real services
        _workflowRegistry = new TestWorkflowRegistry();
        var registryMock = new Mock<IAgentRegistry>();
        _agentRegistry = registryMock.Object;
        _agentRouter = new AgentRouter(registryMock.Object, new Mock<ILogger<AgentRouter>>().Object);
        
        var agentHandoffServiceMock = new Mock<IAgentHandoffService>();
        _workflowInstanceService = new WorkflowInstanceService(
            _context,
            _workflowRegistry,
            registryMock.Object,
            agentHandoffServiceMock.Object,
            new Mock<ILogger<WorkflowInstanceService>>().Object);

        var sharedContextServiceMock = new Mock<ISharedContextService>();
        sharedContextServiceMock
            .Setup(x => x.GetContextAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SharedContext?)null);

        var hubContextMock = new Mock<IHubContext<ChatHub>>();
        
        _stepExecutor = new StepExecutor(
            _context,
            _agentRouter,
            _workflowRegistry,
            _workflowInstanceService,
            new Mock<ILogger<StepExecutor>>().Object);

        // Setup hub context mock
        var mockClients = new Mock<IHubClients>();
        var mockClientProxy = new Mock<IClientProxy>();
        mockClients.Setup(clients => clients.All).Returns(mockClientProxy.Object);
        mockClients.Setup(clients => clients.Group(It.IsAny<string>())).Returns(mockClientProxy.Object);
        var mockHubContext = new Mock<IHubContext<ChatHub>>();
        mockHubContext.Setup(hub => hub.Clients).Returns(mockClients.Object);

        _controller = new WorkflowsController(
            _workflowInstanceService,
            _workflowRegistry,
            _stepExecutor,
            mockHubContext.Object,
            new Mock<ILogger<WorkflowsController>>().Object,
            new Mock<IParticipantService>().Object);

        // Setup test user
        _testUserId = Guid.NewGuid();
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, _testUserId.ToString())
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };

        // Register test workflow
        var workflow = new WorkflowDefinition
        {
            WorkflowId = "test-workflow",
            Name = "Test Workflow",
            Description = "Integration test workflow",
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
        _workflowRegistry.RegisterWorkflow(workflow);

        // Register mock agent handler
        var mockHandler = new MockAgentHandler(shouldSucceed: true);
        _agentRouter.RegisterHandler("test-agent", mockHandler);
    }

    [Fact]
    public async Task EndToEnd_CreateWorkflowAndExecuteSteps_CompletesSuccessfully()
    {
        // Arrange - Create workflow instance
        var createRequest = new CreateWorkflowRequest
        {
            WorkflowId = "test-workflow",
            InitialContext = new Dictionary<string, object> { { "key", "value" } }
        };

        // Act - Create workflow
        var createResult = await _controller.CreateWorkflow(createRequest);
        var createdInstance = (createResult.Result as CreatedAtActionResult)?.Value as WorkflowInstance;
        Assert.NotNull(createdInstance);

        // Act - Start workflow
        var startResult = await _controller.StartWorkflow(createdInstance.Id);
        Assert.IsType<NoContentResult>(startResult);

        // Act - Execute first step
        var executeRequest1 = new ExecuteStepRequest { UserInput = "First input" };
        var executeResult1 = await _controller.ExecuteStep(createdInstance.Id, executeRequest1);
        var stepResult1 = (executeResult1.Result as OkObjectResult)?.Value as StepExecutionResult;
        
        // Assert - First step
        Assert.NotNull(stepResult1);
        Assert.True(stepResult1.Success);
        Assert.Equal("step-1", stepResult1.StepId);
        Assert.Equal(StepExecutionStatus.Completed, stepResult1.Status);
        Assert.Equal(2, stepResult1.NextStep);

        // Act - Execute second step
        var executeRequest2 = new ExecuteStepRequest { UserInput = "Second input" };
        var executeResult2 = await _controller.ExecuteStep(createdInstance.Id, executeRequest2);
        var stepResult2 = (executeResult2.Result as OkObjectResult)?.Value as StepExecutionResult;

        // Assert - Second step (last step)
        Assert.NotNull(stepResult2);
        Assert.True(stepResult2.Success);
        Assert.Equal("step-2", stepResult2.StepId);
        Assert.Equal(StepExecutionStatus.Completed, stepResult2.Status);
        Assert.Equal(WorkflowStatus.Completed, stepResult2.NewWorkflowStatus);
        Assert.Null(stepResult2.NextStep);

        // Assert - Workflow status
        var finalInstance = await _workflowInstanceService.GetWorkflowInstanceAsync(createdInstance.Id);
        Assert.NotNull(finalInstance);
        Assert.Equal(WorkflowStatus.Completed, finalInstance.Status);
        Assert.Equal(3, finalInstance.CurrentStep); // Beyond last step

        // Assert - Step history
        var stepHistories = await _context.WorkflowStepHistories
            .Where(h => h.WorkflowInstanceId == createdInstance.Id)
            .OrderBy(h => h.StartedAt)
            .ToListAsync();

        Assert.Equal(2, stepHistories.Count);
        Assert.Equal("step-1", stepHistories[0].StepId);
        Assert.Equal(StepExecutionStatus.Completed, stepHistories[0].Status);
        Assert.Equal("step-2", stepHistories[1].StepId);
        Assert.Equal(StepExecutionStatus.Completed, stepHistories[1].Status);
    }

    [Fact]
    public async Task EndToEnd_StepFailureWithRetry_TransitionsToWaitingForInput()
    {
        // Arrange - Register failing agent handler
        var failingHandler = new MockAgentHandler(
            shouldSucceed: false, 
            isRetryable: true, 
            errorMessage: "Temporary failure");
        _agentRouter.RegisterHandler("failing-agent", failingHandler);

        // Register workflow with failing agent
        var workflow = new WorkflowDefinition
        {
            WorkflowId = "failing-workflow",
            Name = "Failing Workflow",
            Description = "Test workflow with failing step",
            EstimatedDuration = TimeSpan.FromMinutes(10),
            RequiredRoles = new List<string>(),
            Steps = new List<WorkflowStep>
            {
                new WorkflowStep
                {
                    StepId = "failing-step",
                    Name = "Failing Step",
                    AgentId = "failing-agent",
                    OutputSchema = null,
                    IsOptional = false,
                    CanSkip = false
                }
            }
        };
        _workflowRegistry.RegisterWorkflow(workflow);

        // Create and start workflow
        var instance = await _workflowInstanceService.CreateWorkflowInstanceAsync(
            "failing-workflow", _testUserId, new Dictionary<string, object>());
        await _workflowInstanceService.StartWorkflowAsync(instance.Id);

        // Act - Execute failing step
        var result = await _stepExecutor.ExecuteStepAsync(instance.Id);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(StepExecutionStatus.Failed, result.Status);
        Assert.Equal(WorkflowStatus.WaitingForInput, result.NewWorkflowStatus);
        Assert.Contains("Temporary failure", result.ErrorMessage);

        // Verify step history
        var stepHistory = await _context.WorkflowStepHistories
            .FirstOrDefaultAsync(h => h.WorkflowInstanceId == instance.Id);
        Assert.NotNull(stepHistory);
        Assert.Equal(StepExecutionStatus.Failed, stepHistory.Status);
        Assert.Contains("Temporary failure", stepHistory.ErrorMessage);
    }

    [Fact]
    public async Task StepHistory_TracksAllExecutionDetails()
    {
        // Arrange
        var instance = await _workflowInstanceService.CreateWorkflowInstanceAsync(
            "test-workflow", _testUserId, new Dictionary<string, object>());
        await _workflowInstanceService.StartWorkflowAsync(instance.Id);

        var userInput = "Test user input";

        // Act
        var result = await _stepExecutor.ExecuteStepAsync(instance.Id, userInput);

        // Assert
        Assert.True(result.Success);

        var stepHistory = await _context.WorkflowStepHistories
            .FirstOrDefaultAsync(h => h.WorkflowInstanceId == instance.Id);

        Assert.NotNull(stepHistory);
        Assert.Equal("step-1", stepHistory.StepId);
        Assert.Equal("First Step", stepHistory.StepName);
        Assert.NotNull(stepHistory.StartedAt);
        Assert.NotNull(stepHistory.CompletedAt);
        Assert.True(stepHistory.CompletedAt >= stepHistory.StartedAt);
        Assert.Equal(StepExecutionStatus.Completed, stepHistory.Status);
        Assert.NotNull(stepHistory.Input);
        Assert.NotNull(stepHistory.Output);
    }

    [Fact]
    public async Task StepExecution_WithUserInput_PassesToAgent()
    {
        // Arrange
        string? capturedInput = null;
        var customHandler = new MockAgentHandler(
            executeFunc: async (context) =>
            {
                capturedInput = context.UserInput;
                return await Task.FromResult(new AgentResult
                {
                    Success = true,
                    Output = System.Text.Json.JsonDocument.Parse("{\"result\":\"ok\"}")
                });
            });
        _agentRouter.RegisterHandler("custom-agent", customHandler);

        var workflow = new WorkflowDefinition
        {
            WorkflowId = "custom-workflow",
            Name = "Custom Workflow",
            Description = "Test",
            EstimatedDuration = TimeSpan.FromMinutes(10),
            RequiredRoles = new List<string>(),
            Steps = new List<WorkflowStep>
            {
                new WorkflowStep
                {
                    StepId = "custom-step",
                    Name = "Custom Step",
                    AgentId = "custom-agent",
                    OutputSchema = null,
                    IsOptional = false,
                    CanSkip = false
                }
            }
        };
        _workflowRegistry.RegisterWorkflow(workflow);

        var instance = await _workflowInstanceService.CreateWorkflowInstanceAsync(
            "custom-workflow", _testUserId, new Dictionary<string, object>());
        await _workflowInstanceService.StartWorkflowAsync(instance.Id);

        var userInput = "Test input from user";

        // Act
        var result = await _stepExecutor.ExecuteStepAsync(instance.Id, userInput);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(userInput, capturedInput);
    }

    [Fact]
    public async Task StreamingExecution_YieldsProgressUpdates()
    {
        // Arrange
        var instance = await _workflowInstanceService.CreateWorkflowInstanceAsync(
            "test-workflow", _testUserId, new Dictionary<string, object>());
        await _workflowInstanceService.StartWorkflowAsync(instance.Id);

        // Act
        var progressUpdates = new List<StepProgress>();
        await foreach (var progress in _stepExecutor.ExecuteStepWithStreamingAsync(instance.Id))
        {
            progressUpdates.Add(progress);
        }

        // Assert
        Assert.NotEmpty(progressUpdates);
        Assert.All(progressUpdates, p => Assert.NotNull(p.Message));
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
