using bmadServer.ApiService.Data;
using bmadServer.ApiService.Data.Entities;
using bmadServer.ApiService.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace bmadServer.Tests.Unit.Services;

public class ConflictDetectionServiceTests
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ConflictDetectionService _service;
    private readonly Mock<ILogger<ConflictDetectionService>> _mockLogger;

    public ConflictDetectionServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        
        _dbContext = new ApplicationDbContext(options);
        _mockLogger = new Mock<ILogger<ConflictDetectionService>>();
        _service = new ConflictDetectionService(_dbContext, _mockLogger.Object);
    }

    [Fact]
    public async Task DetectConflictAsync_NoExistingInput_ReturnsNull()
    {
        // Arrange
        var workflowId = Guid.NewGuid();
        var newInput = new BufferedInput
        {
            Id = Guid.NewGuid(),
            WorkflowInstanceId = workflowId,
            UserId = Guid.NewGuid(),
            DisplayName = "User A",
            FieldName = "testField",
            Value = "value1",
            Timestamp = DateTime.UtcNow
        };

        // Act
        var result = await _service.DetectConflictAsync(workflowId, "testField", newInput);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task DetectConflictAsync_DifferentValues_CreatesConflict()
    {
        // Arrange
        var workflowId = Guid.NewGuid();
        var existingInput = new BufferedInput
        {
            Id = Guid.NewGuid(),
            WorkflowInstanceId = workflowId,
            UserId = Guid.NewGuid(),
            DisplayName = "User A",
            FieldName = "testField",
            Value = "value1",
            Timestamp = DateTime.UtcNow.AddMinutes(-1),
            IsApplied = false
        };

        _dbContext.BufferedInputs.Add(existingInput);
        await _dbContext.SaveChangesAsync();

        var newInput = new BufferedInput
        {
            Id = Guid.NewGuid(),
            WorkflowInstanceId = workflowId,
            UserId = Guid.NewGuid(),
            DisplayName = "User B",
            FieldName = "testField",
            Value = "value2",
            Timestamp = DateTime.UtcNow
        };

        // Act
        var result = await _service.DetectConflictAsync(workflowId, "testField", newInput);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(ConflictStatus.Pending, result.Status);
        Assert.Equal("testField", result.FieldName);
        Assert.Equal(2, result.GetInputs().Count);
    }

    [Fact]
    public async Task DetectConflictAsync_SameValues_ReturnsNull()
    {
        // Arrange
        var workflowId = Guid.NewGuid();
        var existingInput = new BufferedInput
        {
            Id = Guid.NewGuid(),
            WorkflowInstanceId = workflowId,
            UserId = Guid.NewGuid(),
            DisplayName = "User A",
            FieldName = "testField",
            Value = "sameValue",
            Timestamp = DateTime.UtcNow.AddMinutes(-1),
            IsApplied = false
        };

        _dbContext.BufferedInputs.Add(existingInput);
        await _dbContext.SaveChangesAsync();

        var newInput = new BufferedInput
        {
            Id = Guid.NewGuid(),
            WorkflowInstanceId = workflowId,
            UserId = Guid.NewGuid(),
            DisplayName = "User B",
            FieldName = "testField",
            Value = "sameValue",
            Timestamp = DateTime.UtcNow
        };

        // Act
        var result = await _service.DetectConflictAsync(workflowId, "testField", newInput);

        // Assert
        Assert.Null(result);
    }
}
