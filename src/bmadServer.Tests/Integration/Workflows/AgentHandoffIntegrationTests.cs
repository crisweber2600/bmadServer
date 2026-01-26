using bmadServer.ApiService.Data;
using bmadServer.ApiService.DTOs;
using bmadServer.ApiService.Models.Workflows;
using bmadServer.ApiService.Services.Workflows;
using bmadServer.ApiService.Services.Workflows.Agents;
using bmadServer.ServiceDefaults.Models.Workflows;
using bmadServer.ServiceDefaults.Services.Workflows;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace bmadServer.Tests.Integration.Workflows;

public class AgentHandoffIntegrationTests : IDisposable
{
    private readonly string _databaseName;
    private readonly ApplicationDbContext _context;
    private readonly Mock<ILogger<AgentHandoffService>> _loggerMock;
    private readonly AgentHandoffService _handoffService;
    private readonly Mock<IWorkflowRegistry> _workflowRegistryMock;
    private readonly Mock<ILogger<WorkflowInstanceService>> _workflowLoggerMock;
    private readonly Mock<IAgentRegistry> _agentRegistryMock;
    private readonly WorkflowInstanceService _workflowInstanceService;

    public AgentHandoffIntegrationTests()
    {
        _databaseName = Guid.NewGuid().ToString();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: _databaseName)
            .Options;
        _context = new ApplicationDbContext(options);
        
        _loggerMock = new Mock<ILogger<AgentHandoffService>>();
        _handoffService = new AgentHandoffService(_context, _loggerMock.Object);
        
        _workflowRegistryMock = new Mock<IWorkflowRegistry>();
        _agentRegistryMock = new Mock<IAgentRegistry>();
        _workflowLoggerMock = new Mock<ILogger<WorkflowInstanceService>>();
        
        var contextServiceMock = new Mock<ISharedContextService>();
        contextServiceMock.Setup(s => s.GetContextAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SharedContext?)null);
        
        _workflowInstanceService = new WorkflowInstanceService(
            _context,
            _workflowRegistryMock.Object,
            _agentRegistryMock.Object,
            _handoffService,
            _workflowLoggerMock.Object);
    }

    #region Full Workflow Handoff Tests

    [Fact]
    public async Task FullWorkflow_WithThreeAgents_RecordsAllHandoffs()
    {
        // Arrange
        var workflowInstanceId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var workflowId = "three-agent-workflow";

        var agents = new[] { "agent-alpha", "agent-beta", "agent-gamma" };
        var handoffSequence = new[] 
        {
            ("agent-alpha", "agent-beta", "step-1"),
            ("agent-beta", "agent-gamma", "step-2"),
            ("agent-gamma", "agent-alpha", "step-3")
        };

        // Create workflow instance
        var instance = new WorkflowInstance
        {
            Id = workflowInstanceId,
            WorkflowDefinitionId = workflowId,
            UserId = userId,
            CurrentStep = 1,
            Status = WorkflowStatus.Running,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.WorkflowInstances.Add(instance);
        await _context.SaveChangesAsync();

        // Act: Record all handoffs
        foreach (var (from, to, step) in handoffSequence)
        {
            await _handoffService.RecordHandoffAsync(
                workflowInstanceId,
                from,
                to,
                step,
                $"Transition to {to}");
        }

        // Assert: All handoffs recorded in correct sequence
        var allHandoffs = await _handoffService.GetHandoffsAsync(workflowInstanceId);
        
        Assert.Equal(3, allHandoffs.Count);
        Assert.Equal("agent-alpha", allHandoffs[0].FromAgentId);
        Assert.Equal("agent-beta", allHandoffs[0].ToAgentId);
        Assert.Equal("agent-beta", allHandoffs[1].FromAgentId);
        Assert.Equal("agent-gamma", allHandoffs[1].ToAgentId);
        Assert.Equal("agent-gamma", allHandoffs[2].FromAgentId);
        Assert.Equal("agent-alpha", allHandoffs[2].ToAgentId);
    }

    [Fact]
    public async Task FullWorkflow_MultipleInstances_IsolatesHandoffs()
    {
        // Arrange
        var workflowId1 = Guid.NewGuid();
        var workflowId2 = Guid.NewGuid();

        var instance1 = new WorkflowInstance
        {
            Id = workflowId1,
            WorkflowDefinitionId = "workflow-1",
            UserId = Guid.NewGuid(),
            CurrentStep = 1,
            Status = WorkflowStatus.Running,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var instance2 = new WorkflowInstance
        {
            Id = workflowId2,
            WorkflowDefinitionId = "workflow-2",
            UserId = Guid.NewGuid(),
            CurrentStep = 1,
            Status = WorkflowStatus.Running,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.WorkflowInstances.AddRange(instance1, instance2);
        await _context.SaveChangesAsync();

        // Act: Record handoffs for both workflows
        await _handoffService.RecordHandoffAsync(workflowId1, "agent-a", "agent-b", "step-1");
        await _handoffService.RecordHandoffAsync(workflowId1, "agent-b", "agent-c", "step-2");
        await _handoffService.RecordHandoffAsync(workflowId2, "agent-x", "agent-y", "step-1");
        await _handoffService.RecordHandoffAsync(workflowId2, "agent-y", "agent-z", "step-2");

        // Assert: Each workflow has its own handoff history
        var handoffs1 = await _handoffService.GetHandoffsAsync(workflowId1);
        var handoffs2 = await _handoffService.GetHandoffsAsync(workflowId2);

        Assert.Equal(2, handoffs1.Count);
        Assert.Equal(2, handoffs2.Count);
        Assert.Equal("agent-a", handoffs1[0].FromAgentId);
        Assert.Equal("agent-x", handoffs2[0].FromAgentId);
    }

    #endregion

    #region Pagination and Filtering Tests

    [Fact]
    public async Task GetRecentHandoffs_WithLargeManyHandoffs_ReturnsPagedResults()
    {
        // Arrange
        var workflowInstanceId = Guid.NewGuid();
        var instance = new WorkflowInstance
        {
            Id = workflowInstanceId,
            WorkflowDefinitionId = "workflow",
            UserId = Guid.NewGuid(),
            CurrentStep = 1,
            Status = WorkflowStatus.Running,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.WorkflowInstances.Add(instance);
        await _context.SaveChangesAsync();

        // Create 25 handoffs (5 per page at default limit)
        for (int i = 0; i < 25; i++)
        {
            await _handoffService.RecordHandoffAsync(
                workflowInstanceId,
                $"agent-{i}",
                $"agent-{i + 1}",
                $"step-{i}",
                cancellationToken: default);
            
            // Add small delay to ensure ordering
            await Task.Delay(1);
        }

        // Act
        var recentHandoffs = await _handoffService.GetRecentHandoffsAsync(workflowInstanceId, limit: 5);
        var moreHandoffs = await _handoffService.GetRecentHandoffsAsync(workflowInstanceId, limit: 10);
        var allHandoffs = await _handoffService.GetRecentHandoffsAsync(workflowInstanceId, limit: 100);

        // Assert: Pagination works correctly
        Assert.Equal(5, recentHandoffs.Count);
        Assert.Equal(10, moreHandoffs.Count);
        Assert.Equal(25, allHandoffs.Count);
        
        // Most recent should be last in returned list (chronological order)
        Assert.Equal("agent-20", recentHandoffs[0].FromAgentId);
        Assert.Equal("agent-24", recentHandoffs[4].FromAgentId);
    }

    [Fact]
    public async Task GetAllHandoffs_WithTimeFilteredQuery_ReturnsCorrectResults()
    {
        // Arrange
        var workflowInstanceId = Guid.NewGuid();
        var instance = new WorkflowInstance
        {
            Id = workflowInstanceId,
            WorkflowDefinitionId = "workflow",
            UserId = Guid.NewGuid(),
            CurrentStep = 1,
            Status = WorkflowStatus.Running,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.WorkflowInstances.Add(instance);
        await _context.SaveChangesAsync();

        var baseTime = DateTime.UtcNow;

        // Create handoffs at different times
        for (int i = 0; i < 5; i++)
        {
            var handoff = new AgentHandoff
            {
                Id = Guid.NewGuid(),
                WorkflowInstanceId = workflowInstanceId,
                FromAgentId = $"agent-{i}",
                ToAgentId = $"agent-{i + 1}",
                WorkflowStepId = $"step-{i}",
                Timestamp = baseTime.AddMinutes(i * 5)
            };
            _context.AgentHandoffs.Add(handoff);
        }
        await _context.SaveChangesAsync();

        // Act: Get handoffs in time range
        var allHandoffs = await _handoffService.GetHandoffsAsync(workflowInstanceId);
        var filteredHandoffs = allHandoffs
            .Where(h => h.Timestamp >= baseTime.AddMinutes(5) && h.Timestamp <= baseTime.AddMinutes(15))
            .ToList();

        // Assert
        Assert.Equal(5, allHandoffs.Count);
        Assert.Equal(3, filteredHandoffs.Count);
    }

    #endregion

    #region Concurrency and Persistence Tests

    [Fact]
    public async Task ConcurrentHandoffRecording_AllHandoffsAreSaved()
    {
        // Arrange
        var workflowInstanceId = Guid.NewGuid();
        var instance = new WorkflowInstance
        {
            Id = workflowInstanceId,
            WorkflowDefinitionId = "workflow",
            UserId = Guid.NewGuid(),
            CurrentStep = 1,
            Status = WorkflowStatus.Running,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.WorkflowInstances.Add(instance);
        await _context.SaveChangesAsync();

        // Act: Record handoffs concurrently
        var tasks = Enumerable.Range(0, 10)
            .Select(i => _handoffService.RecordHandoffAsync(
                workflowInstanceId,
                $"agent-prev-{i}",
                $"agent-next-{i}",
                $"step-{i}"))
            .ToList();

        await Task.WhenAll(tasks);

        // Assert
        var handoffs = await _handoffService.GetHandoffsAsync(workflowInstanceId);
        Assert.Equal(10, handoffs.Count);
    }

    [Fact]
    public async Task HandoffRecording_IsPersistedAcrossContextInstances()
    {
        // Arrange
        var workflowInstanceId = Guid.NewGuid();
        var instance = new WorkflowInstance
        {
            Id = workflowInstanceId,
            WorkflowDefinitionId = "workflow",
            UserId = Guid.NewGuid(),
            CurrentStep = 1,
            Status = WorkflowStatus.Running,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.WorkflowInstances.Add(instance);
        await _context.SaveChangesAsync();

        // Act: Record handoff with first service instance
        await _handoffService.RecordHandoffAsync(
            workflowInstanceId,
            "agent-1",
            "agent-2",
            "step-1",
            "Test handoff");

        // Assert: Retrieve with new context instance
        var newContext = new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(_databaseName)
                .Options);
        
        var newService = new AgentHandoffService(newContext, _loggerMock.Object);
        var handoffs = await newService.GetHandoffsAsync(workflowInstanceId);

        // Both should find the same handoff (since in-memory database is shared)
        Assert.Single(handoffs);
        Assert.Equal("agent-1", handoffs[0].FromAgentId);
    }

    #endregion

    #region Data Integrity Tests

    [Fact]
    public async Task HandoffRecord_PreservesAllData()
    {
        // Arrange
        var workflowInstanceId = Guid.NewGuid();
        var instance = new WorkflowInstance
        {
            Id = workflowInstanceId,
            WorkflowDefinitionId = "workflow",
            UserId = Guid.NewGuid(),
            CurrentStep = 1,
            Status = WorkflowStatus.Running,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.WorkflowInstances.Add(instance);
        await _context.SaveChangesAsync();

        var fromAgent = "agent-analysis";
        var toAgent = "agent-decision";
        var stepId = "evaluation-step";
        var reason = "Complexity exceeded threshold, escalating to expert agent";
        var beforeTime = DateTime.UtcNow;

        // Act
        await _handoffService.RecordHandoffAsync(
            workflowInstanceId,
            fromAgent,
            toAgent,
            stepId,
            reason);

        var afterTime = DateTime.UtcNow;

        // Assert: All data preserved exactly
        var handoff = await _context.AgentHandoffs
            .FirstOrDefaultAsync(h => h.WorkflowInstanceId == workflowInstanceId);

        Assert.NotNull(handoff);
        Assert.Equal(workflowInstanceId, handoff.WorkflowInstanceId);
        Assert.Equal(fromAgent, handoff.FromAgentId);
        Assert.Equal(toAgent, handoff.ToAgentId);
        Assert.Equal(stepId, handoff.WorkflowStepId);
        Assert.Equal(reason, handoff.Reason);
        Assert.True(handoff.Timestamp >= beforeTime && handoff.Timestamp <= afterTime);
        Assert.NotEqual(Guid.Empty, handoff.Id);
    }

    [Fact]
    public async Task MultipleHandoffs_MaintainChronologicalOrder()
    {
        // Arrange
        var workflowInstanceId = Guid.NewGuid();
        var instance = new WorkflowInstance
        {
            Id = workflowInstanceId,
            WorkflowDefinitionId = "workflow",
            UserId = Guid.NewGuid(),
            CurrentStep = 1,
            Status = WorkflowStatus.Running,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.WorkflowInstances.Add(instance);
        await _context.SaveChangesAsync();

        // Record in non-sequential order
        var handoffs = new[] 
        {
            ("agent-1", "agent-2", "step-1"),
            ("agent-2", "agent-3", "step-2"),
            ("agent-3", "agent-4", "step-3"),
            ("agent-4", "agent-5", "step-4"),
            ("agent-5", "agent-6", "step-5")
        };

        // Act
        foreach (var (from, to, step) in handoffs)
        {
            await _handoffService.RecordHandoffAsync(
                workflowInstanceId,
                from,
                to,
                step);
            await Task.Delay(10); // Ensure ordering
        }

        var retrieved = await _handoffService.GetHandoffsAsync(workflowInstanceId);

        // Assert: Maintains insertion order by timestamp
        for (int i = 0; i < retrieved.Count - 1; i++)
        {
            Assert.True(retrieved[i].Timestamp <= retrieved[i + 1].Timestamp);
        }

        Assert.Equal("agent-1", retrieved[0].FromAgentId);
        Assert.Equal("agent-5", retrieved[4].FromAgentId);
    }

    #endregion

    public void Dispose()
    {
        _context.Dispose();
    }
}
