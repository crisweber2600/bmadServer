using System.Text.Json;
using Moq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using bmadServer.ApiService.Data;
using bmadServer.ApiService.Models.Workflows;
using bmadServer.ApiService.Services.Workflows;
using Xunit;

namespace bmadServer.Tests.Unit.Services.Workflows;

public class SharedContextServiceTests
{
    private readonly ApplicationDbContext _dbContext;
    private readonly Mock<ILogger<SharedContextService>> _loggerMock;
    private readonly SharedContextService _service;

    public SharedContextServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new ApplicationDbContext(options);
        _loggerMock = new Mock<ILogger<SharedContextService>>();
        _service = new SharedContextService(_dbContext, _loggerMock.Object);
    }

    [Fact]
    public async Task GetContextAsync_WithExistingWorkflow_ReturnsContext()
    {
        var workflowId = Guid.NewGuid();
        var context = new SharedContext
        {
            StepOutputs = new Dictionary<string, JsonDocument>
            {
                { "step-1", JsonDocument.Parse("{\"key\": \"value\"}") }
            }
        };

        var workflow = new WorkflowInstance
        {
            Id = workflowId,
            WorkflowDefinitionId = "wf-1",
            UserId = Guid.NewGuid(),
            Status = WorkflowStatus.Running,
            SharedContextJson = JsonDocument.Parse(JsonSerializer.Serialize(context)),
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.WorkflowInstances.Add(workflow);
        await _dbContext.SaveChangesAsync();

        var result = await _service.GetContextAsync(workflowId);

        Assert.NotNull(result);
        Assert.Single(result.StepOutputs);
        Assert.True(result.StepOutputs.ContainsKey("step-1"));
    }

    [Fact]
    public async Task GetContextAsync_WithNonExistentWorkflow_ReturnsNull()
    {
        var nonExistentId = Guid.NewGuid();

        var result = await _service.GetContextAsync(nonExistentId);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetContextAsync_WithEmptyGuid_ReturnsNull()
    {
        var result = await _service.GetContextAsync(Guid.Empty);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetStepOutputAsync_WithCompletedStep_ReturnsOutput()
    {
        var workflowId = Guid.NewGuid();
        var stepOutput = JsonDocument.Parse("{\"result\": \"success\"}");
        var context = new SharedContext
        {
            StepOutputs = new Dictionary<string, JsonDocument>
            {
                { "step-1", stepOutput }
            }
        };

        var workflow = new WorkflowInstance
        {
            Id = workflowId,
            WorkflowDefinitionId = "wf-1",
            UserId = Guid.NewGuid(),
            Status = WorkflowStatus.Running,
            SharedContextJson = JsonDocument.Parse(JsonSerializer.Serialize(context)),
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.WorkflowInstances.Add(workflow);
        await _dbContext.SaveChangesAsync();

        var result = await _service.GetStepOutputAsync(workflowId, "step-1");

        Assert.NotNull(result);
        Assert.Equal("success", result.RootElement.GetProperty("result").GetString());
    }

    [Fact]
    public async Task GetStepOutputAsync_WithMissingStep_ReturnsNull()
    {
        var workflowId = Guid.NewGuid();

        var result = await _service.GetStepOutputAsync(workflowId, "step-999");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetStepOutputAsync_WithEmptyStepId_ReturnsNull()
    {
        var workflowId = Guid.NewGuid();

        var result = await _service.GetStepOutputAsync(workflowId, "");

        Assert.Null(result);
    }

    [Fact]
    public async Task AddStepOutputAsync_WithValidInput_PersistsOutput()
    {
        var workflowId = Guid.NewGuid();
        var workflow = new WorkflowInstance
        {
            Id = workflowId,
            WorkflowDefinitionId = "wf-1",
            UserId = Guid.NewGuid(),
            Status = WorkflowStatus.Running,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.WorkflowInstances.Add(workflow);
        await _dbContext.SaveChangesAsync();

        var stepOutput = JsonDocument.Parse("{\"step\": \"output\"}");
        await _service.AddStepOutputAsync(workflowId, "step-1", stepOutput);

        var retrieved = await _service.GetStepOutputAsync(workflowId, "step-1");
        Assert.NotNull(retrieved);
        Assert.Equal("output", retrieved.RootElement.GetProperty("step").GetString());
    }

    [Fact]
    public async Task AddStepOutputAsync_IncrementsVersion()
    {
        var workflowId = Guid.NewGuid();
        var workflow = new WorkflowInstance
        {
            Id = workflowId,
            WorkflowDefinitionId = "wf-1",
            UserId = Guid.NewGuid(),
            Status = WorkflowStatus.Running,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.WorkflowInstances.Add(workflow);
        await _dbContext.SaveChangesAsync();

        var output1 = JsonDocument.Parse("{\"id\": 1}");
        var output2 = JsonDocument.Parse("{\"id\": 2}");

        await _service.AddStepOutputAsync(workflowId, "step-1", output1);
        var context1 = await _service.GetContextAsync(workflowId);
        Assert.Equal(2, context1.Version);

        await _service.AddStepOutputAsync(workflowId, "step-2", output2);
        var context2 = await _service.GetContextAsync(workflowId);
        Assert.Equal(3, context2.Version);
    }

    [Fact]
    public async Task UpdateContextAsync_WithMatchingVersion_Succeeds()
    {
        var workflowId = Guid.NewGuid();
        var context = new SharedContext { Version = 1 };
        var workflow = new WorkflowInstance
        {
            Id = workflowId,
            WorkflowDefinitionId = "wf-1",
            UserId = Guid.NewGuid(),
            Status = WorkflowStatus.Running,
            SharedContextJson = JsonDocument.Parse(JsonSerializer.Serialize(context)),
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.WorkflowInstances.Add(workflow);
        await _dbContext.SaveChangesAsync();

        context.Version = 1;
        var result = await _service.UpdateContextAsync(workflowId, context);

        Assert.True(result);
        var updated = await _service.GetContextAsync(workflowId);
        Assert.Equal(2, updated.Version);
    }

    [Fact]
    public async Task UpdateContextAsync_WithVersionMismatch_ReturnsFalse()
    {
        var workflowId = Guid.NewGuid();
        var context = new SharedContext { Version = 1 };
        var workflow = new WorkflowInstance
        {
            Id = workflowId,
            WorkflowDefinitionId = "wf-1",
            UserId = Guid.NewGuid(),
            Status = WorkflowStatus.Running,
            SharedContextJson = JsonDocument.Parse(JsonSerializer.Serialize(context)),
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.WorkflowInstances.Add(workflow);
        await _dbContext.SaveChangesAsync();

        context.Version = 99;
        var result = await _service.UpdateContextAsync(workflowId, context);

        Assert.False(result);
    }

    [Fact]
    public async Task AddStepOutputAsync_WithNullWorkflow_ThrowsException()
    {
        var nonExistentId = Guid.NewGuid();
        var output = JsonDocument.Parse("{}");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.AddStepOutputAsync(nonExistentId, "step-1", output));
    }

    [Fact]
    public async Task AddStepOutputAsync_WithEmptyWorkflowId_ThrowsException()
    {
        var output = JsonDocument.Parse("{}");

        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.AddStepOutputAsync(Guid.Empty, "step-1", output));
    }

    [Fact]
    public async Task AddStepOutputAsync_WithEmptyStepId_ThrowsException()
    {
        var workflowId = Guid.NewGuid();
        var workflow = new WorkflowInstance
        {
            Id = workflowId,
            WorkflowDefinitionId = "wf-1",
            UserId = Guid.NewGuid(),
            Status = WorkflowStatus.Running,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.WorkflowInstances.Add(workflow);
        await _dbContext.SaveChangesAsync();

        var output = JsonDocument.Parse("{}");

        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.AddStepOutputAsync(workflowId, "", output));
    }
}

public class ContextSummarizationServiceTests
{
    private readonly Mock<ILogger<ContextSummarizationService>> _loggerMock;
    private readonly ContextSummarizationService _service;

    public ContextSummarizationServiceTests()
    {
        _loggerMock = new Mock<ILogger<ContextSummarizationService>>();
        _service = new ContextSummarizationService(_loggerMock.Object);
    }

    [Fact]
    public void EstimateTokenCount_WithJsonString_ReturnsApproximateTokens()
    {
        var json = "{\"key\": \"value\"}";
        var tokens = _service.EstimateTokenCount(json);

        Assert.True(tokens > 0);
        Assert.True(tokens <= json.Length);
    }

    [Fact]
    public void SummarizeIfNeeded_BelowLimit_ReturnsSameContext()
    {
        var context = new SharedContext
        {
            StepOutputs = new Dictionary<string, JsonDocument>
            {
                { "step-1", JsonDocument.Parse("{\"data\": \"value\"}") }
            }
        };

        var result = _service.SummarizeIfNeeded(context, 100000);

        Assert.Equal(context.StepOutputs.Count, result.StepOutputs.Count);
    }

    [Fact]
    public void SummarizeIfNeeded_ExceedsLimit_PreservesDecisionHistory()
    {
        var decision = new DecisionRecord { StepId = "step-1", Decision = "approved" };
        var context = new SharedContext
        {
            DecisionHistory = new List<DecisionRecord> { decision },
            StepOutputs = new Dictionary<string, JsonDocument>
            {
                { "step-1", JsonDocument.Parse("{\"a\": \"" + new string('x', 5000) + "\"}") },
                { "step-2", JsonDocument.Parse("{\"b\": \"" + new string('y', 5000) + "\"}") },
                { "step-3", JsonDocument.Parse("{\"c\": \"" + new string('z', 5000) + "\"}") },
                { "step-4", JsonDocument.Parse("{\"d\": \"value\"}") }
            }
        };

        var result = _service.SummarizeIfNeeded(context, 5000);

        Assert.NotEmpty(result.DecisionHistory);
        Assert.Contains(result.DecisionHistory, d => d.Decision == "approved");
    }

    [Fact]
    public void SummarizeIfNeeded_WithThreeOrFewerSteps_PreservesAll()
    {
        var context = new SharedContext
        {
            StepOutputs = new Dictionary<string, JsonDocument>
            {
                { "step-1", JsonDocument.Parse("{\"a\": 1}") },
                { "step-2", JsonDocument.Parse("{\"b\": 2}") },
                { "step-3", JsonDocument.Parse("{\"c\": 3}") }
            }
        };

        var result = _service.SummarizeIfNeeded(context, 100);

        Assert.Equal(3, result.StepOutputs.Count);
    }
}
