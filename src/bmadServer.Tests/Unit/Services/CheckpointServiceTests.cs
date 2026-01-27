using bmadServer.ApiService.Data;
using bmadServer.ApiService.Models.Workflows;
using bmadServer.ApiService.Services.Checkpoints;
using bmadServer.Tests.Helpers;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text.Json;
using Xunit;

namespace bmadServer.Tests.Unit.Services;

public class CheckpointServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly SqliteConnection _connection;
    private readonly CheckpointService _service;
    private readonly Mock<ILogger<CheckpointService>> _loggerMock;

    public CheckpointServiceTests()
    {
        var options = TestDatabaseHelper.CreateSqliteOptions(out _connection);
        _context = new ApplicationDbContext(options);
        _context.Database.EnsureCreated();
        _loggerMock = new Mock<ILogger<CheckpointService>>();
        _service = new CheckpointService(_context, _loggerMock.Object);
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public async Task CreateCheckpointAsync_Should_Create_Checkpoint_With_Version_1()
    {
        // Arrange
        var workflowId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var stepId = "step-1";

        var workflow = new WorkflowInstance
        {
            Id = workflowId,
            WorkflowDefinitionId = "test-workflow",
            UserId = userId,
            CurrentStep = 1,
            Status = WorkflowStatus.Running,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.WorkflowInstances.Add(workflow);
        await _context.SaveChangesAsync();

        // Act
        var checkpoint = await _service.CreateCheckpointAsync(
            workflowId, stepId, CheckpointType.StepCompletion, userId);

        // Assert
        Assert.NotNull(checkpoint);
        Assert.Equal(workflowId, checkpoint.WorkflowId);
        Assert.Equal(stepId, checkpoint.StepId);
        Assert.Equal(CheckpointType.StepCompletion, checkpoint.CheckpointType);
        Assert.Equal(1, checkpoint.Version);
        Assert.Equal(userId, checkpoint.TriggeredBy);
        Assert.NotNull(checkpoint.StateSnapshot);
    }

    [Fact]
    public async Task CreateCheckpointAsync_Should_Increment_Version()
    {
        // Arrange
        var workflowId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        
        var workflow = new WorkflowInstance
        {
            Id = workflowId,
            WorkflowDefinitionId = "test-workflow",
            UserId = userId,
            CurrentStep = 1,
            Status = WorkflowStatus.Running,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.WorkflowInstances.Add(workflow);
        await _context.SaveChangesAsync();

        // Create first checkpoint
        await _service.CreateCheckpointAsync(workflowId, "step-1", CheckpointType.StepCompletion, userId);

        // Act - Create second checkpoint
        var checkpoint2 = await _service.CreateCheckpointAsync(
            workflowId, "step-2", CheckpointType.StepCompletion, userId);

        // Assert
        Assert.Equal(2, checkpoint2.Version);
    }

    [Fact]
    public async Task CreateCheckpointAsync_Should_Throw_When_Workflow_Not_Found()
    {
        // Arrange
        var workflowId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CreateCheckpointAsync(workflowId, "step-1", CheckpointType.StepCompletion, userId));
    }

    [Fact]
    public async Task GetLatestCheckpointAsync_Should_Return_Latest_Checkpoint()
    {
        // Arrange
        var workflowId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        
        var workflow = new WorkflowInstance
        {
            Id = workflowId,
            WorkflowDefinitionId = "test-workflow",
            UserId = userId,
            CurrentStep = 1,
            Status = WorkflowStatus.Running,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.WorkflowInstances.Add(workflow);
        await _context.SaveChangesAsync();

        await _service.CreateCheckpointAsync(workflowId, "step-1", CheckpointType.StepCompletion, userId);
        await _service.CreateCheckpointAsync(workflowId, "step-2", CheckpointType.StepCompletion, userId);

        // Act
        var latest = await _service.GetLatestCheckpointAsync(workflowId);

        // Assert
        Assert.NotNull(latest);
        Assert.Equal(2, latest.Version);
        Assert.Equal("step-2", latest.StepId);
    }

    [Fact]
    public async Task RestoreCheckpointAsync_Should_Restore_Workflow_State()
    {
        // Arrange
        var workflowId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        
        var workflow = new WorkflowInstance
        {
            Id = workflowId,
            WorkflowDefinitionId = "test-workflow",
            UserId = userId,
            CurrentStep = 1,
            Status = WorkflowStatus.Running,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.WorkflowInstances.Add(workflow);
        await _context.SaveChangesAsync();

        var checkpoint = await _service.CreateCheckpointAsync(
            workflowId, "step-1", CheckpointType.StepCompletion, userId);

        // Change workflow state
        workflow.CurrentStep = 2;
        workflow.Status = WorkflowStatus.Paused;
        await _context.SaveChangesAsync();

        // Act
        await _service.RestoreCheckpointAsync(workflowId, checkpoint.Id);

        // Assert
        var restoredWorkflow = await _context.WorkflowInstances.FindAsync(workflowId);
        Assert.NotNull(restoredWorkflow);
        Assert.Equal(1, restoredWorkflow.CurrentStep);
        Assert.Equal(WorkflowStatus.Running, restoredWorkflow.Status);
    }

    [Fact]
    public async Task GetCheckpointsAsync_Should_Return_Paged_Results()
    {
        // Arrange
        var workflowId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        
        var workflow = new WorkflowInstance
        {
            Id = workflowId,
            WorkflowDefinitionId = "test-workflow",
            UserId = userId,
            CurrentStep = 1,
            Status = WorkflowStatus.Running,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.WorkflowInstances.Add(workflow);
        await _context.SaveChangesAsync();

        // Create 5 checkpoints
        for (int i = 1; i <= 5; i++)
        {
            await _service.CreateCheckpointAsync(workflowId, $"step-{i}", CheckpointType.StepCompletion, userId);
        }

        // Act
        var result = await _service.GetCheckpointsAsync(workflowId, page: 1, pageSize: 3);

        // Assert
        Assert.Equal(3, result.Items.Count);
        Assert.Equal(5, result.TotalCount);
        Assert.Equal(2, result.TotalPages);
    }
}
