using bmadServer.ApiService.Controllers;
using bmadServer.ApiService.Data;
using bmadServer.ApiService.Hubs;
using bmadServer.ApiService.Models.Workflows;
using bmadServer.ApiService.Services.Workflows;
using bmadServer.ApiService.Services.Workflows.Agents;
using bmadServer.ServiceDefaults.Services.Workflows;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;
using Xunit;

namespace bmadServer.Tests.Integration.Workflows;

public class WorkflowCancellationIntegrationTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly IWorkflowInstanceService _workflowInstanceService;
    private readonly WorkflowsController _controller;
    private readonly Guid _testUserId;
    private readonly Mock<IHubContext<ChatHub>> _hubContextMock;

    public WorkflowCancellationIntegrationTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);

        var registryMock = new Mock<IWorkflowRegistry>();
        registryMock.Setup(r => r.ValidateWorkflow(It.IsAny<string>())).Returns(true);

        var agentRegistryMock = new Mock<IAgentRegistry>();
        var agentHandoffServiceMock = new Mock<IAgentHandoffService>();

        _workflowInstanceService = new WorkflowInstanceService(
            _context,
            registryMock.Object,
            agentRegistryMock.Object,
            agentHandoffServiceMock.Object,
            new Mock<ILogger<WorkflowInstanceService>>().Object);

        // Setup SignalR hub mock properly
        _hubContextMock = new Mock<IHubContext<ChatHub>>();
        var mockClients = new Mock<IHubClients>();
        var mockClientProxy = new Mock<IClientProxy>();
        
        _hubContextMock.Setup(h => h.Clients).Returns(mockClients.Object);
        mockClients.Setup(c => c.All).Returns(mockClientProxy.Object);
        mockClientProxy.Setup(p => p.SendCoreAsync(
            It.IsAny<string>(),
            It.IsAny<object[]>(),
            It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        
        _controller = new WorkflowsController(
            _workflowInstanceService,
            registryMock.Object,
            new Mock<IStepExecutor>().Object,
            _hubContextMock.Object,
            new Mock<ILogger<WorkflowsController>>().Object);

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
    }

    [Fact]
    public async Task CancelWorkflow_EndToEnd_ShouldTransitionToCancelled()
    {
        // Arrange - Create a running workflow
        var instance = new WorkflowInstance
        {
            Id = Guid.NewGuid(),
            WorkflowDefinitionId = "test-workflow",
            UserId = _testUserId,
            Status = WorkflowStatus.Running,
            CurrentStep = 2,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.WorkflowInstances.Add(instance);
        await _context.SaveChangesAsync();

        // Act - Cancel via API endpoint
        var result = await _controller.CancelWorkflow(instance.Id);

        // Assert
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull("Result should be OkObjectResult");
        okResult!.StatusCode.Should().Be(StatusCodes.Status200OK);
        
        var cancelledWorkflow = okResult.Value as WorkflowInstance;
        cancelledWorkflow.Should().NotBeNull();
        cancelledWorkflow!.Status.Should().Be(WorkflowStatus.Cancelled);
        cancelledWorkflow.CancelledAt.Should().NotBeNull();
        cancelledWorkflow.CancelledAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        // Verify event was logged
        var events = await _context.WorkflowEvents
            .Where(e => e.WorkflowInstanceId == instance.Id && e.EventType == "WorkflowCancelled")
            .ToListAsync();
        events.Should().HaveCount(1);
        events[0].OldStatus.Should().Be(WorkflowStatus.Running);
        events[0].NewStatus.Should().Be(WorkflowStatus.Cancelled);
    }

    [Fact]
    public async Task CancelWorkflow_WhenCompleted_ShouldReturn400()
    {
        // Arrange
        var instance = new WorkflowInstance
        {
            Id = Guid.NewGuid(),
            WorkflowDefinitionId = "test-workflow",
            UserId = _testUserId,
            Status = WorkflowStatus.Completed,
            CurrentStep = 5,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.WorkflowInstances.Add(instance);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.CancelWorkflow(instance.Id);

        // Assert
        var problemResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        problemResult.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        var problemDetails = problemResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problemDetails.Detail.Should().Contain("Cannot cancel a completed workflow");
    }

    [Fact]
    public async Task CancelWorkflow_ThenAttemptResume_ShouldReturn400()
    {
        // Arrange - Create and cancel a workflow
        var instance = new WorkflowInstance
        {
            Id = Guid.NewGuid(),
            WorkflowDefinitionId = "test-workflow",
            UserId = _testUserId,
            Status = WorkflowStatus.Paused,
            CurrentStep = 2,
            PausedAt = DateTime.UtcNow.AddHours(-1),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.WorkflowInstances.Add(instance);
        await _context.SaveChangesAsync();

        // Cancel the workflow
        await _controller.CancelWorkflow(instance.Id);

        // Act - Try to resume cancelled workflow
        var result = await _controller.ResumeWorkflow(instance.Id);

        // Assert
        var problemResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        problemResult.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        var problemDetails = problemResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problemDetails.Detail.Should().Contain("Cannot resume a cancelled workflow");
    }

    [Fact]
    public async Task CancelWorkflow_PreservesHistory_ShouldNotDelete()
    {
        // Arrange
        var instance = new WorkflowInstance
        {
            Id = Guid.NewGuid(),
            WorkflowDefinitionId = "test-workflow",
            UserId = _testUserId,
            Status = WorkflowStatus.Running,
            CurrentStep = 2,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.WorkflowInstances.Add(instance);

        // Add some step history
        var stepHistory = new WorkflowStepHistory
        {
            Id = Guid.NewGuid(),
            WorkflowInstanceId = instance.Id,
            StepId = "step-1",
            StepName = "First Step",
            StartedAt = DateTime.UtcNow.AddMinutes(-5),
            CompletedAt = DateTime.UtcNow.AddMinutes(-3),
            Status = StepExecutionStatus.Completed
        };
        _context.WorkflowStepHistories.Add(stepHistory);
        await _context.SaveChangesAsync();

        // Act - Cancel workflow
        await _controller.CancelWorkflow(instance.Id);

        // Assert - Workflow still exists (soft delete)
        var workflow = await _context.WorkflowInstances.FindAsync(instance.Id);
        workflow.Should().NotBeNull();
        workflow!.Status.Should().Be(WorkflowStatus.Cancelled);

        // Step history is preserved
        var history = await _context.WorkflowStepHistories
            .Where(h => h.WorkflowInstanceId == instance.Id)
            .ToListAsync();
        history.Should().HaveCount(1);
        history[0].StepId.Should().Be("step-1");
    }

    [Fact]
    public async Task CancelWorkflow_WithNonExistentWorkflow_ShouldReturn404()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _controller.CancelWorkflow(nonExistentId);

        // Assert
        var problemResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        problemResult.StatusCode.Should().Be(StatusCodes.Status404NotFound);
    }

    [Fact]
    public async Task CancelWorkflow_WithoutAuthentication_ShouldReturn401()
    {
        // Arrange
        var controller = new WorkflowsController(
            _workflowInstanceService,
            new Mock<IWorkflowRegistry>().Object,
            new Mock<IStepExecutor>().Object,
            _hubContextMock.Object,
            new Mock<ILogger<WorkflowsController>>().Object);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        var instance = new WorkflowInstance
        {
            Id = Guid.NewGuid(),
            WorkflowDefinitionId = "test-workflow",
            UserId = _testUserId,
            Status = WorkflowStatus.Running,
            CurrentStep = 2,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.WorkflowInstances.Add(instance);
        await _context.SaveChangesAsync();

        // Act
        var result = await controller.CancelWorkflow(instance.Id);

        // Assert
        var problemResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        problemResult.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
    }

    [Fact]
    public async Task GetWorkflows_DefaultFilter_ShouldExcludeCancelled()
    {
        // Arrange - Create multiple workflows
        var runningWorkflow = new WorkflowInstance
        {
            Id = Guid.NewGuid(),
            WorkflowDefinitionId = "test-workflow",
            UserId = _testUserId,
            Status = WorkflowStatus.Running,
            CurrentStep = 2,
            CreatedAt = DateTime.UtcNow.AddHours(-2),
            UpdatedAt = DateTime.UtcNow.AddHours(-2)
        };

        var cancelledWorkflow = new WorkflowInstance
        {
            Id = Guid.NewGuid(),
            WorkflowDefinitionId = "test-workflow",
            UserId = _testUserId,
            Status = WorkflowStatus.Cancelled,
            CurrentStep = 1,
            CancelledAt = DateTime.UtcNow.AddHours(-1),
            CreatedAt = DateTime.UtcNow.AddHours(-3),
            UpdatedAt = DateTime.UtcNow.AddHours(-1)
        };

        var completedWorkflow = new WorkflowInstance
        {
            Id = Guid.NewGuid(),
            WorkflowDefinitionId = "test-workflow",
            UserId = _testUserId,
            Status = WorkflowStatus.Completed,
            CurrentStep = 5,
            CreatedAt = DateTime.UtcNow.AddHours(-4),
            UpdatedAt = DateTime.UtcNow.AddHours(-1)
        };

        _context.WorkflowInstances.AddRange(runningWorkflow, cancelledWorkflow, completedWorkflow);
        await _context.SaveChangesAsync();

        // Act - Get workflows without showing cancelled
        var result = await _controller.GetWorkflows(showCancelled: false);

        // Assert
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        var workflows = okResult!.Value as List<WorkflowInstanceListItem>;
        workflows.Should().NotBeNull();
        workflows!.Should().HaveCount(2); // Running and Completed, but not Cancelled
        workflows.Should().NotContain(w => w.Status == WorkflowStatus.Cancelled);
    }

    [Fact]
    public async Task GetWorkflows_WithShowCancelled_ShouldIncludeCancelled()
    {
        // Arrange - Create multiple workflows
        var runningWorkflow = new WorkflowInstance
        {
            Id = Guid.NewGuid(),
            WorkflowDefinitionId = "test-workflow",
            UserId = _testUserId,
            Status = WorkflowStatus.Running,
            CurrentStep = 2,
            CreatedAt = DateTime.UtcNow.AddHours(-2),
            UpdatedAt = DateTime.UtcNow.AddHours(-2)
        };

        var cancelledWorkflow = new WorkflowInstance
        {
            Id = Guid.NewGuid(),
            WorkflowDefinitionId = "test-workflow",
            UserId = _testUserId,
            Status = WorkflowStatus.Cancelled,
            CurrentStep = 1,
            CancelledAt = DateTime.UtcNow.AddHours(-1),
            CreatedAt = DateTime.UtcNow.AddHours(-3),
            UpdatedAt = DateTime.UtcNow.AddHours(-1)
        };

        _context.WorkflowInstances.AddRange(runningWorkflow, cancelledWorkflow);
        await _context.SaveChangesAsync();

        // Act - Get workflows with showing cancelled
        var result = await _controller.GetWorkflows(showCancelled: true);

        // Assert
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        var workflows = okResult!.Value as List<WorkflowInstanceListItem>;
        workflows.Should().NotBeNull();
        workflows!.Should().HaveCount(2); // Both Running and Cancelled
        workflows.Should().Contain(w => w.Status == WorkflowStatus.Cancelled);
        
        var cancelled = workflows.First(w => w.Status == WorkflowStatus.Cancelled);
        cancelled.IsCancelled.Should().BeTrue();
        cancelled.IsTerminal.Should().BeTrue();
        cancelled.CancelledAt.Should().NotBeNull();
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
