using bmadServer.ApiService.Data;
using bmadServer.ApiService.Models.Workflows;
using bmadServer.ApiService.Services.Workflows;
using bmadServer.Tests.Helpers;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace bmadServer.Tests.Unit.Services.Workflows;

public class AgentHandoffServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly SqliteConnection _connection;
    private readonly Mock<ILogger<AgentHandoffService>> _loggerMock;
    private readonly AgentHandoffService _service;

    public AgentHandoffServiceTests()
    {
        // Setup SQLite in-memory database
        var options = TestDatabaseHelper.CreateSqliteOptions(out _connection);
        _context = new ApplicationDbContext(options);
        _context.Database.EnsureCreated();
        _loggerMock = new Mock<ILogger<AgentHandoffService>>();

        _service = new AgentHandoffService(_context, _loggerMock.Object);
    }

    #region RecordHandoffAsync Tests

    [Fact]
    public async Task RecordHandoffAsync_WithValidParameters_CreatesHandoffRecord()
    {
        // Arrange
        var workflowInstanceId = Guid.NewGuid();
        var fromAgentId = "agent-alpha";
        var toAgentId = "agent-beta";
        var stepId = "step-1";
        var reason = "Escalation required";

        // Act
        await _service.RecordHandoffAsync(
            workflowInstanceId,
            fromAgentId,
            toAgentId,
            stepId,
            reason);

        // Assert
        var handoff = await _context.AgentHandoffs
            .FirstOrDefaultAsync(h => h.WorkflowInstanceId == workflowInstanceId);
        
        Assert.NotNull(handoff);
        Assert.Equal(workflowInstanceId, handoff.WorkflowInstanceId);
        Assert.Equal(fromAgentId, handoff.FromAgentId);
        Assert.Equal(toAgentId, handoff.ToAgentId);
        Assert.Equal(stepId, handoff.WorkflowStepId);
        Assert.Equal(reason, handoff.Reason);
        Assert.NotEqual(Guid.Empty, handoff.Id);
        Assert.True(handoff.Timestamp > DateTime.UtcNow.AddSeconds(-5));
    }

    [Fact]
    public async Task RecordHandoffAsync_WithoutReason_CreatesHandoffWithNullReason()
    {
        // Arrange
        var workflowInstanceId = Guid.NewGuid();
        var fromAgentId = "agent-alpha";
        var toAgentId = "agent-beta";
        var stepId = "step-1";

        // Act
        await _service.RecordHandoffAsync(
            workflowInstanceId,
            fromAgentId,
            toAgentId,
            stepId);

        // Assert
        var handoff = await _context.AgentHandoffs
            .FirstOrDefaultAsync(h => h.WorkflowInstanceId == workflowInstanceId);
        
        Assert.NotNull(handoff);
        Assert.Null(handoff.Reason);
    }

    [Fact]
    public async Task RecordHandoffAsync_WithEmptyWorkflowId_ThrowsArgumentException()
    {
        // Arrange
        var emptyId = Guid.Empty;
        var fromAgentId = "agent-alpha";
        var toAgentId = "agent-beta";
        var stepId = "step-1";

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _service.RecordHandoffAsync(
                emptyId,
                fromAgentId,
                toAgentId,
                stepId));
        
        Assert.Contains("Workflow instance ID cannot be empty", exception.Message);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task RecordHandoffAsync_WithInvalidFromAgentId_ThrowsArgumentException(string fromAgentId)
    {
        // Arrange
        var workflowInstanceId = Guid.NewGuid();
        var toAgentId = "agent-beta";
        var stepId = "step-1";

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _service.RecordHandoffAsync(
                workflowInstanceId,
                fromAgentId,
                toAgentId,
                stepId));
        
        Assert.Contains("From agent ID cannot be null or empty", exception.Message);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task RecordHandoffAsync_WithInvalidToAgentId_ThrowsArgumentException(string toAgentId)
    {
        // Arrange
        var workflowInstanceId = Guid.NewGuid();
        var fromAgentId = "agent-alpha";
        var stepId = "step-1";

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _service.RecordHandoffAsync(
                workflowInstanceId,
                fromAgentId,
                toAgentId,
                stepId));
        
        Assert.Contains("To agent ID cannot be null or empty", exception.Message);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task RecordHandoffAsync_WithInvalidStepId_ThrowsArgumentException(string stepId)
    {
        // Arrange
        var workflowInstanceId = Guid.NewGuid();
        var fromAgentId = "agent-alpha";
        var toAgentId = "agent-beta";

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _service.RecordHandoffAsync(
                workflowInstanceId,
                fromAgentId,
                toAgentId,
                stepId));
        
        Assert.Contains("Step ID cannot be null or empty", exception.Message);
    }

    [Fact]
    public async Task RecordHandoffAsync_WithMultipleHandoffs_CreatesAllRecords()
    {
        // Arrange
        var workflowInstanceId = Guid.NewGuid();
        var handoffs = new[]
        {
            ("agent-alpha", "agent-beta", "step-1"),
            ("agent-beta", "agent-gamma", "step-2"),
            ("agent-gamma", "agent-alpha", "step-3")
        };

        // Act
        foreach (var (from, to, step) in handoffs)
        {
            await _service.RecordHandoffAsync(
                workflowInstanceId,
                from,
                to,
                step);
        }

        // Assert
        var createdHandoffs = await _context.AgentHandoffs
            .Where(h => h.WorkflowInstanceId == workflowInstanceId)
            .ToListAsync();
        
        Assert.Equal(3, createdHandoffs.Count);
        Assert.Equal("agent-alpha", createdHandoffs[0].FromAgentId);
        Assert.Equal("agent-beta", createdHandoffs[0].ToAgentId);
    }

    [Fact]
    public async Task RecordHandoffAsync_LogsInformationOnSuccess()
    {
        // Arrange
        var workflowInstanceId = Guid.NewGuid();
        var fromAgentId = "agent-alpha";
        var toAgentId = "agent-beta";
        var stepId = "step-1";

        // Act
        await _service.RecordHandoffAsync(
            workflowInstanceId,
            fromAgentId,
            toAgentId,
            stepId);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => 
                    v.ToString()!.Contains(fromAgentId) && 
                    v.ToString()!.Contains(toAgentId)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    #endregion

    #region GetHandoffsAsync Tests

    [Fact]
    public async Task GetHandoffsAsync_WithValidWorkflowId_ReturnsAllHandoffs()
    {
        // Arrange
        var workflowInstanceId = Guid.NewGuid();
        var handoffsList = new[]
        {
            new AgentHandoff
            {
                Id = Guid.NewGuid(),
                WorkflowInstanceId = workflowInstanceId,
                FromAgentId = "agent-alpha",
                ToAgentId = "agent-beta",
                Timestamp = DateTime.UtcNow.AddSeconds(-10),
                WorkflowStepId = "step-1"
            },
            new AgentHandoff
            {
                Id = Guid.NewGuid(),
                WorkflowInstanceId = workflowInstanceId,
                FromAgentId = "agent-beta",
                ToAgentId = "agent-gamma",
                Timestamp = DateTime.UtcNow,
                WorkflowStepId = "step-2"
            }
        };

        _context.AgentHandoffs.AddRange(handoffsList);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetHandoffsAsync(workflowInstanceId);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("agent-alpha", result[0].FromAgentId);
        Assert.Equal("agent-beta", result[1].FromAgentId);
        // Verify chronological order
        Assert.True(result[0].Timestamp <= result[1].Timestamp);
    }

    [Fact]
    public async Task GetHandoffsAsync_WithNonExistentWorkflowId_ReturnsEmptyList()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _service.GetHandoffsAsync(nonExistentId);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetHandoffsAsync_WithMultipleWorkflows_ReturnsOnlySpecifiedWorkflow()
    {
        // Arrange
        var workflowId1 = Guid.NewGuid();
        var workflowId2 = Guid.NewGuid();

        var handoffs = new[]
        {
            new AgentHandoff
            {
                Id = Guid.NewGuid(),
                WorkflowInstanceId = workflowId1,
                FromAgentId = "agent-alpha",
                ToAgentId = "agent-beta",
                Timestamp = DateTime.UtcNow,
                WorkflowStepId = "step-1"
            },
            new AgentHandoff
            {
                Id = Guid.NewGuid(),
                WorkflowInstanceId = workflowId2,
                FromAgentId = "agent-gamma",
                ToAgentId = "agent-delta",
                Timestamp = DateTime.UtcNow,
                WorkflowStepId = "step-1"
            }
        };

        _context.AgentHandoffs.AddRange(handoffs);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetHandoffsAsync(workflowId1);

        // Assert
        Assert.Single(result);
        Assert.Equal("agent-alpha", result[0].FromAgentId);
        Assert.Equal(workflowId1, result[0].WorkflowInstanceId);
    }

    [Fact]
    public async Task GetHandoffsAsync_WithEmptyWorkflowId_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _service.GetHandoffsAsync(Guid.Empty));
        
        Assert.Contains("Workflow instance ID cannot be empty", exception.Message);
    }

    [Fact]
    public async Task GetHandoffsAsync_ReturnsChronologicalOrder()
    {
        // Arrange
        var workflowInstanceId = Guid.NewGuid();
        var baseTime = DateTime.UtcNow;
        var handoffs = new[]
        {
            new AgentHandoff
            {
                Id = Guid.NewGuid(),
                WorkflowInstanceId = workflowInstanceId,
                FromAgentId = "agent-1",
                ToAgentId = "agent-2",
                Timestamp = baseTime.AddSeconds(10),
                WorkflowStepId = "step-1"
            },
            new AgentHandoff
            {
                Id = Guid.NewGuid(),
                WorkflowInstanceId = workflowInstanceId,
                FromAgentId = "agent-2",
                ToAgentId = "agent-3",
                Timestamp = baseTime.AddSeconds(5),
                WorkflowStepId = "step-2"
            },
            new AgentHandoff
            {
                Id = Guid.NewGuid(),
                WorkflowInstanceId = workflowInstanceId,
                FromAgentId = "agent-3",
                ToAgentId = "agent-1",
                Timestamp = baseTime.AddSeconds(15),
                WorkflowStepId = "step-3"
            }
        };

        _context.AgentHandoffs.AddRange(handoffs);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetHandoffsAsync(workflowInstanceId);

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Equal("agent-2", result[0].FromAgentId);  // +5 seconds
        Assert.Equal("agent-1", result[1].FromAgentId);  // +10 seconds
        Assert.Equal("agent-3", result[2].FromAgentId);  // +15 seconds
    }

    #endregion

    #region GetRecentHandoffsAsync Tests

    [Fact]
    public async Task GetRecentHandoffsAsync_WithDefaultLimit_Returns5MostRecent()
    {
        // Arrange
        var workflowInstanceId = Guid.NewGuid();
        var baseTime = DateTime.UtcNow;
        var handoffs = new List<AgentHandoff>();

        for (int i = 0; i < 10; i++)
        {
            handoffs.Add(new AgentHandoff
            {
                Id = Guid.NewGuid(),
                WorkflowInstanceId = workflowInstanceId,
                FromAgentId = $"agent-{i}",
                ToAgentId = $"agent-{i + 1}",
                Timestamp = baseTime.AddSeconds(i),
                WorkflowStepId = $"step-{i}"
            });
        }

        _context.AgentHandoffs.AddRange(handoffs);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetRecentHandoffsAsync(workflowInstanceId);

        // Assert
        Assert.Equal(5, result.Count);
        // Should return most recent 5 in chronological order
        Assert.Equal("agent-5", result[0].FromAgentId);
        Assert.Equal("agent-9", result[4].FromAgentId);
    }

    [Fact]
    public async Task GetRecentHandoffsAsync_WithCustomLimit_ReturnsSpecifiedNumber()
    {
        // Arrange
        var workflowInstanceId = Guid.NewGuid();
        var baseTime = DateTime.UtcNow;
        var handoffs = new List<AgentHandoff>();

        for (int i = 0; i < 10; i++)
        {
            handoffs.Add(new AgentHandoff
            {
                Id = Guid.NewGuid(),
                WorkflowInstanceId = workflowInstanceId,
                FromAgentId = $"agent-{i}",
                ToAgentId = $"agent-{i + 1}",
                Timestamp = baseTime.AddSeconds(i),
                WorkflowStepId = $"step-{i}"
            });
        }

        _context.AgentHandoffs.AddRange(handoffs);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetRecentHandoffsAsync(workflowInstanceId, limit: 3);

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Equal("agent-7", result[0].FromAgentId);
        Assert.Equal("agent-9", result[2].FromAgentId);
    }

    [Fact]
    public async Task GetRecentHandoffsAsync_WithLimitGreaterThanRecords_ReturnsAllRecords()
    {
        // Arrange
        var workflowInstanceId = Guid.NewGuid();
        var handoffs = new[]
        {
            new AgentHandoff
            {
                Id = Guid.NewGuid(),
                WorkflowInstanceId = workflowInstanceId,
                FromAgentId = "agent-1",
                ToAgentId = "agent-2",
                Timestamp = DateTime.UtcNow,
                WorkflowStepId = "step-1"
            },
            new AgentHandoff
            {
                Id = Guid.NewGuid(),
                WorkflowInstanceId = workflowInstanceId,
                FromAgentId = "agent-2",
                ToAgentId = "agent-3",
                Timestamp = DateTime.UtcNow.AddSeconds(1),
                WorkflowStepId = "step-2"
            }
        };

        _context.AgentHandoffs.AddRange(handoffs);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetRecentHandoffsAsync(workflowInstanceId, limit: 10);

        // Assert
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetRecentHandoffsAsync_WithEmptyWorkflowId_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _service.GetRecentHandoffsAsync(Guid.Empty));
        
        Assert.Contains("Workflow instance ID cannot be empty", exception.Message);
    }

    [Fact]
    public async Task GetRecentHandoffsAsync_WithInvalidLimit_ThrowsArgumentException()
    {
        // Arrange
        var workflowInstanceId = Guid.NewGuid();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _service.GetRecentHandoffsAsync(workflowInstanceId, limit: 0));
        
        Assert.Contains("Limit must be greater than 0", exception.Message);
    }

    [Fact]
    public async Task GetRecentHandoffsAsync_WithNegativeLimit_ThrowsArgumentException()
    {
        // Arrange
        var workflowInstanceId = Guid.NewGuid();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _service.GetRecentHandoffsAsync(workflowInstanceId, limit: -1));
        
        Assert.Contains("Limit must be greater than 0", exception.Message);
    }

    [Fact]
    public async Task GetRecentHandoffsAsync_WithNoRecords_ReturnsEmptyList()
    {
        // Arrange
        var workflowInstanceId = Guid.NewGuid();

        // Act
        var result = await _service.GetRecentHandoffsAsync(workflowInstanceId);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetRecentHandoffsAsync_ReturnsInChronologicalOrder()
    {
        // Arrange
        var workflowInstanceId = Guid.NewGuid();
        var baseTime = DateTime.UtcNow;
        var handoffs = new[]
        {
            new AgentHandoff
            {
                Id = Guid.NewGuid(),
                WorkflowInstanceId = workflowInstanceId,
                FromAgentId = "agent-1",
                ToAgentId = "agent-2",
                Timestamp = baseTime.AddSeconds(30),
                WorkflowStepId = "step-1"
            },
            new AgentHandoff
            {
                Id = Guid.NewGuid(),
                WorkflowInstanceId = workflowInstanceId,
                FromAgentId = "agent-2",
                ToAgentId = "agent-3",
                Timestamp = baseTime.AddSeconds(10),
                WorkflowStepId = "step-2"
            },
            new AgentHandoff
            {
                Id = Guid.NewGuid(),
                WorkflowInstanceId = workflowInstanceId,
                FromAgentId = "agent-3",
                ToAgentId = "agent-1",
                Timestamp = baseTime.AddSeconds(20),
                WorkflowStepId = "step-3"
            }
        };

        _context.AgentHandoffs.AddRange(handoffs);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetRecentHandoffsAsync(workflowInstanceId, limit: 3);

        // Assert
        Assert.Equal(3, result.Count);
        // Should be in ascending order by timestamp even though retrieved in descending
        Assert.True(result[0].Timestamp <= result[1].Timestamp);
        Assert.True(result[1].Timestamp <= result[2].Timestamp);
    }

    #endregion

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }
}
