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

    [Fact]
    public async Task CancelWorkflow_FromRunningState_ShouldTransitionToCancelled()
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
        var (success, message) = await _service.CancelWorkflowAsync(instance.Id, userId);

        // Assert
        success.Should().BeTrue();
        message.Should().BeNull();
        instance.Status.Should().Be(WorkflowStatus.Cancelled);
        instance.CancelledAt.Should().NotBeNull();
        instance.CancelledAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        // Verify event was logged
        var events = await _context.WorkflowEvents
            .Where(e => e.WorkflowInstanceId == instance.Id && e.EventType == "WorkflowCancelled")
            .ToListAsync();
        events.Should().HaveCount(1);
        events[0].OldStatus.Should().Be(WorkflowStatus.Running);
        events[0].NewStatus.Should().Be(WorkflowStatus.Cancelled);
        events[0].UserId.Should().Be(userId);
    }

    [Fact]
    public async Task CancelWorkflow_FromPausedState_ShouldTransitionToCancelled()
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
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.WorkflowInstances.Add(instance);
        await _context.SaveChangesAsync();

        // Act
        var (success, message) = await _service.CancelWorkflowAsync(instance.Id, userId);

        // Assert
        success.Should().BeTrue();
        message.Should().BeNull();
        instance.Status.Should().Be(WorkflowStatus.Cancelled);
        instance.CancelledAt.Should().NotBeNull();
    }

    [Fact]
    public async Task CancelWorkflow_FromWaitingForInputState_ShouldTransitionToCancelled()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var instance = new WorkflowInstance
        {
            Id = Guid.NewGuid(),
            WorkflowDefinitionId = "create-prd",
            UserId = userId,
            Status = WorkflowStatus.WaitingForInput,
            CurrentStep = 2,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.WorkflowInstances.Add(instance);
        await _context.SaveChangesAsync();

        // Act
        var (success, message) = await _service.CancelWorkflowAsync(instance.Id, userId);

        // Assert
        success.Should().BeTrue();
        message.Should().BeNull();
        instance.Status.Should().Be(WorkflowStatus.Cancelled);
        instance.CancelledAt.Should().NotBeNull();
    }

    [Fact]
    public async Task CancelWorkflow_WhenCompleted_ShouldReturnError()
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
        var (success, message) = await _service.CancelWorkflowAsync(instance.Id, userId);

        // Assert
        success.Should().BeFalse();
        message.Should().Be("Cannot cancel a completed workflow");
        instance.Status.Should().Be(WorkflowStatus.Completed);
        instance.CancelledAt.Should().BeNull();
    }

    [Fact]
    public async Task CancelWorkflow_WhenFailed_ShouldReturnError()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var instance = new WorkflowInstance
        {
            Id = Guid.NewGuid(),
            WorkflowDefinitionId = "create-prd",
            UserId = userId,
            Status = WorkflowStatus.Failed,
            CurrentStep = 3,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.WorkflowInstances.Add(instance);
        await _context.SaveChangesAsync();

        // Act
        var (success, message) = await _service.CancelWorkflowAsync(instance.Id, userId);

        // Assert
        success.Should().BeFalse();
        message.Should().Be("Cannot cancel a failed workflow");
        instance.Status.Should().Be(WorkflowStatus.Failed);
        instance.CancelledAt.Should().BeNull();
    }

    [Fact]
    public async Task CancelWorkflow_WhenAlreadyCancelled_ShouldReturnError()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var instance = new WorkflowInstance
        {
            Id = Guid.NewGuid(),
            WorkflowDefinitionId = "create-prd",
            UserId = userId,
            Status = WorkflowStatus.Cancelled,
            CurrentStep = 2,
            CancelledAt = DateTime.UtcNow.AddHours(-1),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.WorkflowInstances.Add(instance);
        await _context.SaveChangesAsync();

        // Act
        var (success, message) = await _service.CancelWorkflowAsync(instance.Id, userId);

        // Assert
        success.Should().BeFalse();
        message.Should().Be("Workflow is already cancelled");
        instance.Status.Should().Be(WorkflowStatus.Cancelled);
    }

    [Fact]
    public async Task CancelWorkflow_WithNonExistentWorkflow_ShouldReturnFalse()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var nonExistentId = Guid.NewGuid();

        // Act
        var (success, message) = await _service.CancelWorkflowAsync(nonExistentId, userId);

        // Assert
        success.Should().BeFalse();
        message.Should().Be("Workflow instance not found");
    }

    [Fact]
    public async Task ResumeWorkflow_WhenCancelled_ShouldReturnError()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var instance = new WorkflowInstance
        {
            Id = Guid.NewGuid(),
            WorkflowDefinitionId = "create-prd",
            UserId = userId,
            Status = WorkflowStatus.Cancelled,
            CurrentStep = 2,
            CancelledAt = DateTime.UtcNow.AddHours(-1),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.WorkflowInstances.Add(instance);
        await _context.SaveChangesAsync();

        // Act
        var (success, message) = await _service.ResumeWorkflowAsync(instance.Id, userId);

        // Assert
        success.Should().BeFalse();
        message.Should().Be("Cannot resume a cancelled workflow");
        instance.Status.Should().Be(WorkflowStatus.Cancelled);
    }

    [Fact]
    public async Task SkipCurrentStep_WithOptionalSkippableStep_ShouldSkipAndAdvance()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var workflowId = "create-architecture"; // Has optional step at index 2 (arch-3)
        _registryMock.Setup(r => r.GetWorkflow(workflowId))
            .Returns(new WorkflowRegistry().GetWorkflow(workflowId));
        
        var instance = new WorkflowInstance
        {
            Id = Guid.NewGuid(),
            WorkflowDefinitionId = workflowId,
            UserId = userId,
            Status = WorkflowStatus.Running,
            CurrentStep = 3, // On arch-3 which is optional and can skip
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.WorkflowInstances.Add(instance);
        await _context.SaveChangesAsync();

        // Act
        var (success, message) = await _service.SkipCurrentStepAsync(instance.Id, userId, "Not needed for this project");

        // Assert
        success.Should().BeTrue();
        message.Should().BeNull();
        instance.CurrentStep.Should().Be(4); // Advanced to next step
        instance.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        // Verify step history was created with Skipped status
        var stepHistory = await _context.WorkflowStepHistories
            .Where(h => h.WorkflowInstanceId == instance.Id && h.StepId == "arch-3")
            .FirstOrDefaultAsync();
        stepHistory.Should().NotBeNull();
        stepHistory!.Status.Should().Be(StepExecutionStatus.Skipped);
        stepHistory.ErrorMessage.Should().Be("Not needed for this project");
        stepHistory.CompletedAt.Should().NotBeNull();

        // Verify event was logged
        var events = await _context.WorkflowEvents
            .Where(e => e.WorkflowInstanceId == instance.Id && e.EventType == "StepSkipped")
            .ToListAsync();
        events.Should().HaveCount(1);
        events[0].UserId.Should().Be(userId);
    }

    [Fact]
    public async Task SkipCurrentStep_WithRequiredStep_ShouldReturnError()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var workflowId = "create-prd";
        _registryMock.Setup(r => r.GetWorkflow(workflowId))
            .Returns(new WorkflowRegistry().GetWorkflow(workflowId));
        
        var instance = new WorkflowInstance
        {
            Id = Guid.NewGuid(),
            WorkflowDefinitionId = workflowId,
            UserId = userId,
            Status = WorkflowStatus.Running,
            CurrentStep = 1, // prd-1 is required
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.WorkflowInstances.Add(instance);
        await _context.SaveChangesAsync();

        // Act
        var (success, message) = await _service.SkipCurrentStepAsync(instance.Id, userId);

        // Assert
        success.Should().BeFalse();
        message.Should().Be("This step is required and cannot be skipped");
        instance.CurrentStep.Should().Be(1); // Should not advance
    }

    [Fact]
    public async Task SkipCurrentStep_WithOptionalButNonSkippableStep_ShouldReturnError()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var workflowId = "dev-story"; // dev-4 is optional but CanSkip=false
        _registryMock.Setup(r => r.GetWorkflow(workflowId))
            .Returns(new WorkflowRegistry().GetWorkflow(workflowId));
        
        var instance = new WorkflowInstance
        {
            Id = Guid.NewGuid(),
            WorkflowDefinitionId = workflowId,
            UserId = userId,
            Status = WorkflowStatus.Running,
            CurrentStep = 4, // dev-4: IsOptional=true but CanSkip=false
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.WorkflowInstances.Add(instance);
        await _context.SaveChangesAsync();

        // Act
        var (success, message) = await _service.SkipCurrentStepAsync(instance.Id, userId);

        // Assert
        success.Should().BeFalse();
        message.Should().Be("This step cannot be skipped despite being optional");
        instance.CurrentStep.Should().Be(4);
    }

    [Fact]
    public async Task SkipCurrentStep_WhenNotRunning_ShouldReturnError()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var workflowId = "create-architecture";
        _registryMock.Setup(r => r.GetWorkflow(workflowId))
            .Returns(new WorkflowRegistry().GetWorkflow(workflowId));
        
        var instance = new WorkflowInstance
        {
            Id = Guid.NewGuid(),
            WorkflowDefinitionId = workflowId,
            UserId = userId,
            Status = WorkflowStatus.Paused,
            CurrentStep = 3,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.WorkflowInstances.Add(instance);
        await _context.SaveChangesAsync();

        // Act
        var (success, message) = await _service.SkipCurrentStepAsync(instance.Id, userId);

        // Assert
        success.Should().BeFalse();
        message.Should().Contain("Cannot skip step when workflow is in Paused state");
    }

    [Fact]
    public async Task SkipCurrentStep_WithNonExistentWorkflow_ShouldReturnError()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var nonExistentId = Guid.NewGuid();

        // Act
        var (success, message) = await _service.SkipCurrentStepAsync(nonExistentId, userId);

        // Assert
        success.Should().BeFalse();
        message.Should().Be("Workflow instance not found");
    }

    [Fact]
    public async Task GoToStep_WithPreviouslyVisitedStep_ShouldNavigateToStep()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var workflowId = "create-prd";
        _registryMock.Setup(r => r.GetWorkflow(workflowId))
            .Returns(new WorkflowRegistry().GetWorkflow(workflowId));
        
        var instance = new WorkflowInstance
        {
            Id = Guid.NewGuid(),
            WorkflowDefinitionId = workflowId,
            UserId = userId,
            Status = WorkflowStatus.Running,
            CurrentStep = 3,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.WorkflowInstances.Add(instance);

        // Create step history showing prd-1 was previously completed
        var stepHistory = new WorkflowStepHistory
        {
            Id = Guid.NewGuid(),
            WorkflowInstanceId = instance.Id,
            StepId = "prd-1",
            StepName = "Define Project Vision",
            StartedAt = DateTime.UtcNow.AddHours(-1),
            CompletedAt = DateTime.UtcNow.AddMinutes(-50),
            Status = StepExecutionStatus.Completed
        };
        _context.WorkflowStepHistories.Add(stepHistory);
        await _context.SaveChangesAsync();

        // Act
        var (success, message) = await _service.GoToStepAsync(instance.Id, "prd-1", userId);

        // Assert
        success.Should().BeTrue();
        message.Should().BeNull();
        instance.CurrentStep.Should().Be(1); // Should navigate to step 1 (prd-1)
        instance.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        // Verify event was logged
        var events = await _context.WorkflowEvents
            .Where(e => e.WorkflowInstanceId == instance.Id && e.EventType == "StepRevisit")
            .ToListAsync();
        events.Should().HaveCount(1);
        events[0].UserId.Should().Be(userId);
    }

    [Fact]
    public async Task GoToStep_WithNonVisitedStep_ShouldReturnError()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var workflowId = "create-prd";
        _registryMock.Setup(r => r.GetWorkflow(workflowId))
            .Returns(new WorkflowRegistry().GetWorkflow(workflowId));
        
        var instance = new WorkflowInstance
        {
            Id = Guid.NewGuid(),
            WorkflowDefinitionId = workflowId,
            UserId = userId,
            Status = WorkflowStatus.Running,
            CurrentStep = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.WorkflowInstances.Add(instance);
        await _context.SaveChangesAsync();

        // Act - try to go to prd-3 which hasn't been visited yet
        var (success, message) = await _service.GoToStepAsync(instance.Id, "prd-3", userId);

        // Assert
        success.Should().BeFalse();
        message.Should().Be("Can only navigate to previously visited steps");
        instance.CurrentStep.Should().Be(1);
    }

    [Fact]
    public async Task GoToStep_WithInvalidStepId_ShouldReturnError()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var workflowId = "create-prd";
        _registryMock.Setup(r => r.GetWorkflow(workflowId))
            .Returns(new WorkflowRegistry().GetWorkflow(workflowId));
        
        var instance = new WorkflowInstance
        {
            Id = Guid.NewGuid(),
            WorkflowDefinitionId = workflowId,
            UserId = userId,
            Status = WorkflowStatus.Running,
            CurrentStep = 2,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.WorkflowInstances.Add(instance);
        await _context.SaveChangesAsync();

        // Act
        var (success, message) = await _service.GoToStepAsync(instance.Id, "invalid-step", userId);

        // Assert
        success.Should().BeFalse();
        message.Should().Contain("not found in workflow definition");
        instance.CurrentStep.Should().Be(2);
    }

    [Fact]
    public async Task GoToStep_WhenNotRunning_ShouldReturnError()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var workflowId = "create-prd";
        _registryMock.Setup(r => r.GetWorkflow(workflowId))
            .Returns(new WorkflowRegistry().GetWorkflow(workflowId));
        
        var instance = new WorkflowInstance
        {
            Id = Guid.NewGuid(),
            WorkflowDefinitionId = workflowId,
            UserId = userId,
            Status = WorkflowStatus.Completed,
            CurrentStep = 3,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.WorkflowInstances.Add(instance);
        await _context.SaveChangesAsync();

        // Act
        var (success, message) = await _service.GoToStepAsync(instance.Id, "prd-1", userId);

        // Assert
        success.Should().BeFalse();
        message.Should().Contain("Cannot navigate to step when workflow is in Completed state");
    }

    [Fact]
    public async Task GoToStep_WithNonExistentWorkflow_ShouldReturnError()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var nonExistentId = Guid.NewGuid();

        // Act
        var (success, message) = await _service.GoToStepAsync(nonExistentId, "prd-1", userId);

        // Assert
        success.Should().BeFalse();
        message.Should().Be("Workflow instance not found");
    }

    [Fact]
    public async Task GoToStep_PreservesPreviousStepOutput_WhenRevisiting()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var workflowId = "create-prd";
        _registryMock.Setup(r => r.GetWorkflow(workflowId))
            .Returns(new WorkflowRegistry().GetWorkflow(workflowId));
        
        var instance = new WorkflowInstance
        {
            Id = Guid.NewGuid(),
            WorkflowDefinitionId = workflowId,
            UserId = userId,
            Status = WorkflowStatus.Running,
            CurrentStep = 3,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.WorkflowInstances.Add(instance);

        // Create step history with output data
        var stepHistory = new WorkflowStepHistory
        {
            Id = Guid.NewGuid(),
            WorkflowInstanceId = instance.Id,
            StepId = "prd-2",
            StepName = "Identify User Stories",
            StartedAt = DateTime.UtcNow.AddHours(-1),
            CompletedAt = DateTime.UtcNow.AddMinutes(-50),
            Status = StepExecutionStatus.Completed,
            Output = System.Text.Json.JsonDocument.Parse("{\"userStories\": [\"story1\", \"story2\"]}")
        };
        _context.WorkflowStepHistories.Add(stepHistory);
        await _context.SaveChangesAsync();

        // Act
        var (success, _) = await _service.GoToStepAsync(instance.Id, "prd-2", userId);

        // Assert
        success.Should().BeTrue();
        
        // Verify step history output is still preserved
        var preservedHistory = await _context.WorkflowStepHistories
            .Where(h => h.WorkflowInstanceId == instance.Id && h.StepId == "prd-2")
            .FirstOrDefaultAsync();
        preservedHistory.Should().NotBeNull();
        preservedHistory!.Output.Should().NotBeNull();
        preservedHistory.Output!.RootElement.GetProperty("userStories").GetArrayLength().Should().Be(2);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
