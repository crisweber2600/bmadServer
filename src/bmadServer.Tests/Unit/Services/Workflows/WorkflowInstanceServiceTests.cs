using bmadServer.ApiService.Data;
using bmadServer.ApiService.Models.Workflows;
using bmadServer.ApiService.Services.Workflows;
using bmadServer.ServiceDefaults.Services.Workflows;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace bmadServer.Tests.Unit.Services.Workflows;

public class WorkflowInstanceServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<IWorkflowRegistry> _registryMock;
    private readonly Mock<ILogger<WorkflowInstanceService>> _loggerMock;
    private readonly IWorkflowInstanceService _service;

    public WorkflowInstanceServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);

        _registryMock = new Mock<IWorkflowRegistry>();
        _loggerMock = new Mock<ILogger<WorkflowInstanceService>>();
        _service = new WorkflowInstanceService(_context, _registryMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task CreateWorkflowInstance_WithValidWorkflow_ShouldCreateInstance()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var workflowId = "create-prd";
        _registryMock.Setup(r => r.ValidateWorkflow(workflowId)).Returns(true);

        // Act
        var result = await _service.CreateWorkflowInstanceAsync(workflowId, userId, new Dictionary<string, object>());

        // Assert
        result.Should().NotBeNull();
        result.WorkflowDefinitionId.Should().Be(workflowId);
        result.UserId.Should().Be(userId);
        result.Status.Should().Be(WorkflowStatus.Created);
        result.CurrentStep.Should().Be(0);
        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task CreateWorkflowInstance_WithInvalidWorkflow_ShouldThrowException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var workflowId = "invalid-workflow";
        _registryMock.Setup(r => r.ValidateWorkflow(workflowId)).Returns(false);

        // Act
        Func<Task> act = async () => await _service.CreateWorkflowInstanceAsync(workflowId, userId, new Dictionary<string, object>());

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*workflow*not found*");
    }

    [Fact]
    public async Task StartWorkflow_WithCreatedInstance_ShouldTransitionToRunning()
    {
        // Arrange
        var instance = new WorkflowInstance
        {
            Id = Guid.NewGuid(),
            WorkflowDefinitionId = "create-prd",
            UserId = Guid.NewGuid(),
            Status = WorkflowStatus.Created,
            CurrentStep = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.WorkflowInstances.Add(instance);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.StartWorkflowAsync(instance.Id);

        // Assert
        result.Should().BeTrue();
        instance.Status.Should().Be(WorkflowStatus.Running);
        instance.CurrentStep.Should().Be(1);
    }

    [Fact]
    public async Task TransitionState_WithInvalidTransition_ShouldReturnFalse()
    {
        // Arrange
        var instance = new WorkflowInstance
        {
            Id = Guid.NewGuid(),
            WorkflowDefinitionId = "create-prd",
            UserId = Guid.NewGuid(),
            Status = WorkflowStatus.Created,
            CurrentStep = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.WorkflowInstances.Add(instance);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.TransitionStateAsync(instance.Id, WorkflowStatus.Completed);

        // Assert
        result.Should().BeFalse();
        instance.Status.Should().Be(WorkflowStatus.Created);
    }

    [Fact]
    public async Task TransitionState_WithValidTransition_ShouldLogEvent()
    {
        // Arrange
        var instance = new WorkflowInstance
        {
            Id = Guid.NewGuid(),
            WorkflowDefinitionId = "create-prd",
            UserId = Guid.NewGuid(),
            Status = WorkflowStatus.Created,
            CurrentStep = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.WorkflowInstances.Add(instance);
        await _context.SaveChangesAsync();

        // Act
        await _service.TransitionStateAsync(instance.Id, WorkflowStatus.Running);

        // Assert
        var events = await _context.WorkflowEvents.Where(e => e.WorkflowInstanceId == instance.Id).ToListAsync();
        events.Should().HaveCount(1);
        events[0].EventType.Should().Be("StateTransition");
        events[0].OldStatus.Should().Be(WorkflowStatus.Created);
        events[0].NewStatus.Should().Be(WorkflowStatus.Running);
    }

    [Fact]
    public async Task GetWorkflowInstance_WithValidId_ShouldReturnInstance()
    {
        // Arrange
        var instance = new WorkflowInstance
        {
            Id = Guid.NewGuid(),
            WorkflowDefinitionId = "create-prd",
            UserId = Guid.NewGuid(),
            Status = WorkflowStatus.Running,
            CurrentStep = 2,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.WorkflowInstances.Add(instance);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetWorkflowInstanceAsync(instance.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(instance.Id);
        result.CurrentStep.Should().Be(2);
    }

    [Fact]
    public async Task GetWorkflowInstance_WithInvalidId_ShouldReturnNull()
    {
        // Act
        var result = await _service.GetWorkflowInstanceAsync(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task PauseWorkflow_FromRunningState_ShouldTransitionToPaused()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var instance = new WorkflowInstance
        {
            Id = Guid.NewGuid(),
            WorkflowDefinitionId = "create-prd",
            UserId = userId,
            Status = WorkflowStatus.Running,
            CurrentStep = 2,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.WorkflowInstances.Add(instance);
        await _context.SaveChangesAsync();

        // Act
        var (success, message) = await _service.PauseWorkflowAsync(instance.Id, userId);

        // Assert
        success.Should().BeTrue();
        message.Should().BeNull();
        instance.Status.Should().Be(WorkflowStatus.Paused);
        instance.PausedAt.Should().NotBeNull();
        instance.PausedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        // Verify event was logged
        var events = await _context.WorkflowEvents
            .Where(e => e.WorkflowInstanceId == instance.Id && e.EventType == "WorkflowPaused")
            .ToListAsync();
        events.Should().HaveCount(1);
        events[0].OldStatus.Should().Be(WorkflowStatus.Running);
        events[0].NewStatus.Should().Be(WorkflowStatus.Paused);
        events[0].UserId.Should().Be(userId);
    }

    [Fact]
    public async Task PauseWorkflow_WhenAlreadyPaused_ShouldReturn400()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var instance = new WorkflowInstance
        {
            Id = Guid.NewGuid(),
            WorkflowDefinitionId = "create-prd",
            UserId = userId,
            Status = WorkflowStatus.Paused,
            CurrentStep = 2,
            PausedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.WorkflowInstances.Add(instance);
        await _context.SaveChangesAsync();

        // Act
        var (success, message) = await _service.PauseWorkflowAsync(instance.Id, userId);

        // Assert
        success.Should().BeFalse();
        message.Should().Be("Workflow is already paused");
        instance.Status.Should().Be(WorkflowStatus.Paused);
    }

    [Fact]
    public async Task PauseWorkflow_WithInvalidState_ShouldReturnFalse()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var instance = new WorkflowInstance
        {
            Id = Guid.NewGuid(),
            WorkflowDefinitionId = "create-prd",
            UserId = userId,
            Status = WorkflowStatus.Completed,
            CurrentStep = 5,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.WorkflowInstances.Add(instance);
        await _context.SaveChangesAsync();

        // Act
        var (success, message) = await _service.PauseWorkflowAsync(instance.Id, userId);

        // Assert
        success.Should().BeFalse();
        message.Should().Contain("Cannot pause workflow");
        instance.Status.Should().Be(WorkflowStatus.Completed);
    }

    [Fact]
    public async Task PauseWorkflow_WithNonExistentWorkflow_ShouldReturnFalse()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var nonExistentId = Guid.NewGuid();

        // Act
        var (success, message) = await _service.PauseWorkflowAsync(nonExistentId, userId);

        // Assert
        success.Should().BeFalse();
        message.Should().Be("Workflow instance not found");
    }

    [Fact]
    public async Task ResumeWorkflow_FromPausedState_ShouldTransitionToRunning()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var instance = new WorkflowInstance
        {
            Id = Guid.NewGuid(),
            WorkflowDefinitionId = "create-prd",
            UserId = userId,
            Status = WorkflowStatus.Paused,
            CurrentStep = 2,
            PausedAt = DateTime.UtcNow.AddHours(-1),
            CreatedAt = DateTime.UtcNow.AddHours(-2),
            UpdatedAt = DateTime.UtcNow.AddHours(-1)
        };
        _context.WorkflowInstances.Add(instance);
        await _context.SaveChangesAsync();

        // Act
        var (success, message) = await _service.ResumeWorkflowAsync(instance.Id, userId);

        // Assert
        success.Should().BeTrue();
        message.Should().BeNull(); // No context refresh for <24 hours
        instance.Status.Should().Be(WorkflowStatus.Running);

        // Verify event was logged
        var events = await _context.WorkflowEvents
            .Where(e => e.WorkflowInstanceId == instance.Id && e.EventType == "WorkflowResumed")
            .ToListAsync();
        events.Should().HaveCount(1);
        events[0].OldStatus.Should().Be(WorkflowStatus.Paused);
        events[0].NewStatus.Should().Be(WorkflowStatus.Running);
        events[0].UserId.Should().Be(userId);
    }

    [Fact]
    public async Task ResumeWorkflow_After24Hours_ShouldRefreshContext()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var instance = new WorkflowInstance
        {
            Id = Guid.NewGuid(),
            WorkflowDefinitionId = "create-prd",
            UserId = userId,
            Status = WorkflowStatus.Paused,
            CurrentStep = 2,
            PausedAt = DateTime.UtcNow.AddHours(-25), // More than 24 hours
            CreatedAt = DateTime.UtcNow.AddHours(-30),
            UpdatedAt = DateTime.UtcNow.AddHours(-25)
        };
        _context.WorkflowInstances.Add(instance);
        await _context.SaveChangesAsync();

        // Act
        var (success, message) = await _service.ResumeWorkflowAsync(instance.Id, userId);

        // Assert
        success.Should().BeTrue();
        message.Should().Be("Workflow resumed. Context has been refreshed.");
        instance.Status.Should().Be(WorkflowStatus.Running);
    }

    [Fact]
    public async Task ResumeWorkflow_WithInvalidState_ShouldReturnFalse()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var instance = new WorkflowInstance
        {
            Id = Guid.NewGuid(),
            WorkflowDefinitionId = "create-prd",
            UserId = userId,
            Status = WorkflowStatus.Running,
            CurrentStep = 2,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.WorkflowInstances.Add(instance);
        await _context.SaveChangesAsync();

        // Act
        var (success, message) = await _service.ResumeWorkflowAsync(instance.Id, userId);

        // Assert
        success.Should().BeFalse();
        message.Should().Contain("Cannot resume workflow");
        instance.Status.Should().Be(WorkflowStatus.Running);
    }

    [Fact]
    public async Task ResumeWorkflow_WithNonExistentWorkflow_ShouldReturnFalse()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var nonExistentId = Guid.NewGuid();

        // Act
        var (success, message) = await _service.ResumeWorkflowAsync(nonExistentId, userId);

        // Assert
        success.Should().BeFalse();
        message.Should().Be("Workflow instance not found");
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
