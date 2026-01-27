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

public class InputQueueServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly SqliteConnection _connection;
    private readonly InputQueueService _service;
    private readonly Mock<ILogger<InputQueueService>> _loggerMock;

    public InputQueueServiceTests()
    {
        var options = TestDatabaseHelper.CreateSqliteOptions(out _connection);
        _context = new ApplicationDbContext(options);
        _context.Database.EnsureCreated();
        _loggerMock = new Mock<ILogger<InputQueueService>>();
        _service = new InputQueueService(_context, _loggerMock.Object);
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public async Task EnqueueInputAsync_Should_Create_Queued_Input()
    {
        // Arrange
        var workflowId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var inputType = "message";
        var content = JsonDocument.Parse("{\"text\": \"hello\"}");

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
        var queuedInput = await _service.EnqueueInputAsync(workflowId, userId, inputType, content);

        // Assert
        Assert.NotNull(queuedInput);
        Assert.Equal(workflowId, queuedInput.WorkflowId);
        Assert.Equal(userId, queuedInput.UserId);
        Assert.Equal(inputType, queuedInput.InputType);
        Assert.Equal(InputStatus.Queued, queuedInput.Status);
        Assert.Null(queuedInput.ProcessedAt);
    }

    [Fact]
    public async Task EnqueueInputAsync_Should_Throw_When_Workflow_Not_Found()
    {
        // Arrange
        var workflowId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var content = JsonDocument.Parse("{}");

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.EnqueueInputAsync(workflowId, userId, "message", content));
    }

    [Fact]
    public async Task GetQueuedInputsAsync_Should_Return_Inputs_In_FIFO_Order()
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

        // Enqueue multiple inputs
        var input1 = await _service.EnqueueInputAsync(workflowId, userId, "msg1", JsonDocument.Parse("{\"seq\": 1}"));
        await Task.Delay(10); // Small delay to ensure different timestamps
        var input2 = await _service.EnqueueInputAsync(workflowId, userId, "msg2", JsonDocument.Parse("{\"seq\": 2}"));
        await Task.Delay(10);
        var input3 = await _service.EnqueueInputAsync(workflowId, userId, "msg3", JsonDocument.Parse("{\"seq\": 3}"));

        // Act
        var queuedInputs = await _service.GetQueuedInputsAsync(workflowId);

        // Assert
        Assert.Equal(3, queuedInputs.Count);
        Assert.Equal(input1.Id, queuedInputs[0].Id);
        Assert.Equal(input2.Id, queuedInputs[1].Id);
        Assert.Equal(input3.Id, queuedInputs[2].Id);
    }

    [Fact]
    public async Task ProcessQueuedInputsAsync_Should_Process_All_Queued_Inputs()
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

        await _service.EnqueueInputAsync(workflowId, userId, "msg1", JsonDocument.Parse("{\"text\": \"hello\"}"));
        await _service.EnqueueInputAsync(workflowId, userId, "msg2", JsonDocument.Parse("{\"text\": \"world\"}"));

        // Act
        var result = await _service.ProcessQueuedInputsAsync(workflowId);

        // Assert
        Assert.Equal(2, result.ProcessedCount);
        Assert.Equal(0, result.RejectedCount);
        Assert.Empty(result.Errors);

        // Verify inputs are marked as processed
        var processedInputs = await _context.QueuedInputs
            .Where(qi => qi.WorkflowId == workflowId && qi.Status == InputStatus.Processed)
            .ToListAsync();
        Assert.Equal(2, processedInputs.Count);
    }

    [Fact]
    public async Task ProcessQueuedInputsAsync_Should_Handle_Invalid_Inputs()
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

        // Add a valid input
        await _service.EnqueueInputAsync(workflowId, userId, "msg1", JsonDocument.Parse("{\"text\": \"valid\"}"));

        // Act
        var result = await _service.ProcessQueuedInputsAsync(workflowId);

        // Assert
        Assert.Equal(1, result.ProcessedCount);
        Assert.Equal(0, result.RejectedCount);
    }
}
