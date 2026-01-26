using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using bmadServer.ApiService.Data;
using bmadServer.ApiService.Models.Workflows;
using bmadServer.ApiService.Services.Workflows;
using Xunit;

namespace bmadServer.Tests.Integration.Workflows;

public class SharedContextIntegrationTests
{
    private readonly ApplicationDbContext _dbContext;
    private readonly Mock<ILogger<SharedContextService>> _loggerMock;
    private readonly SharedContextService _service;

    public SharedContextIntegrationTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new ApplicationDbContext(options);
        _loggerMock = new Mock<ILogger<SharedContextService>>();
        _service = new SharedContextService(_dbContext, _loggerMock.Object);
    }

    [Fact]
    public async Task MultiStepWorkflow_ContextAccumulates()
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

        var output1 = JsonDocument.Parse("{\"step\": \"1\", \"data\": \"result-1\"}");
        var output2 = JsonDocument.Parse("{\"step\": \"2\", \"data\": \"result-2\"}");
        var output3 = JsonDocument.Parse("{\"step\": \"3\", \"data\": \"result-3\"}");

        await _service.AddStepOutputAsync(workflowId, "step-1", output1);
        await _service.AddStepOutputAsync(workflowId, "step-2", output2);
        await _service.AddStepOutputAsync(workflowId, "step-3", output3);

        var context = await _service.GetContextAsync(workflowId);

        Assert.NotNull(context);
        Assert.Equal(3, context.StepOutputs.Count);
        Assert.True(context.StepOutputs.ContainsKey("step-1"));
        Assert.True(context.StepOutputs.ContainsKey("step-2"));
        Assert.True(context.StepOutputs.ContainsKey("step-3"));
    }

    [Fact]
    public async Task StepCanAccessPreviousOutputs()
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

        var previousOutput = JsonDocument.Parse("{\"schema\": \"designed\"}");
        await _service.AddStepOutputAsync(workflowId, "design-step", previousOutput);

        var retrieved = await _service.GetStepOutputAsync(workflowId, "design-step");

        Assert.NotNull(retrieved);
        Assert.Equal("designed", retrieved.RootElement.GetProperty("schema").GetString());
    }

    [Fact]
    public async Task ContextPersistsAcrossReads()
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

        var output = JsonDocument.Parse("{\"persisted\": true}");
        await _service.AddStepOutputAsync(workflowId, "step-1", output);

        _dbContext.ChangeTracker.Clear();

        var retrieved1 = await _service.GetContextAsync(workflowId);
        _dbContext.ChangeTracker.Clear();
        var retrieved2 = await _service.GetContextAsync(workflowId);

        Assert.NotNull(retrieved1);
        Assert.NotNull(retrieved2);
        Assert.Equal(retrieved1.Version, retrieved2.Version);
        Assert.True(retrieved2.StepOutputs.ContainsKey("step-1"));
    }

    [Fact]
    public async Task ConcurrentOperations_VersionConflictDetected()
    {
        var workflowId = Guid.NewGuid();
        var context1 = new SharedContext { Version = 1 };
        var context2 = new SharedContext { Version = 1 };

        var workflow = new WorkflowInstance
        {
            Id = workflowId,
            WorkflowDefinitionId = "wf-1",
            UserId = Guid.NewGuid(),
            Status = WorkflowStatus.Running,
            SharedContextJson = JsonDocument.Parse(JsonSerializer.Serialize(context1)),
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.WorkflowInstances.Add(workflow);
        await _dbContext.SaveChangesAsync();

        var result1 = await _service.UpdateContextAsync(workflowId, context1);
        Assert.True(result1);

        _dbContext.ChangeTracker.Clear();

        var result2 = await _service.UpdateContextAsync(workflowId, context2);
        Assert.False(result2);
    }

    [Fact]
    public async Task VersionConflict_CallerCanRetry()
    {
        var workflowId = Guid.NewGuid();
        var initialContext = new SharedContext { Version = 1 };

        var workflow = new WorkflowInstance
        {
            Id = workflowId,
            WorkflowDefinitionId = "wf-1",
            UserId = Guid.NewGuid(),
            Status = WorkflowStatus.Running,
            SharedContextJson = JsonDocument.Parse(JsonSerializer.Serialize(initialContext)),
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.WorkflowInstances.Add(workflow);
        await _dbContext.SaveChangesAsync();

        var contextToUpdate = new SharedContext { Version = 1 };
        await _service.UpdateContextAsync(workflowId, contextToUpdate);

        _dbContext.ChangeTracker.Clear();

        var reloadedContext = await _service.GetContextAsync(workflowId);
        var retryResult = await _service.UpdateContextAsync(workflowId, reloadedContext);

        Assert.True(retryResult);
    }

    [Fact]
    public async Task AddStepOutput_UpdatesTimestamp()
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

        var before = DateTime.UtcNow;
        await _service.AddStepOutputAsync(workflowId, "step-1", JsonDocument.Parse("{}"));
        var after = DateTime.UtcNow;

        var context = await _service.GetContextAsync(workflowId);

        Assert.True(context.LastModifiedAt >= before);
        Assert.True(context.LastModifiedAt <= after);
    }

    [Fact]
    public async Task MultipleStepOutputs_SameStepUpdates()
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

        var output1 = JsonDocument.Parse("{\"attempt\": 1}");
        var output2 = JsonDocument.Parse("{\"attempt\": 2}");

        await _service.AddStepOutputAsync(workflowId, "step-1", output1);
        var v1 = (await _service.GetContextAsync(workflowId)).Version;

        await _service.AddStepOutputAsync(workflowId, "step-1", output2);
        var v2 = (await _service.GetContextAsync(workflowId)).Version;

        var final = await _service.GetStepOutputAsync(workflowId, "step-1");

        Assert.Equal(2, final.RootElement.GetProperty("attempt").GetInt32());
        Assert.True(v2 > v1);
    }

    [Fact]
    public async Task ContextWith_UserPreferences_PreservedAcrossUpdates()
    {
        var workflowId = Guid.NewGuid();
        var prefs = new Dictionary<string, string>
        {
            { "verbosityLevel", "detailed" },
            { "technicalDepth", "expert" }
        };
        var context = new SharedContext { UserPreferences = prefs };

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

        var output = JsonDocument.Parse("{\"step\": \"done\"}");
        await _service.AddStepOutputAsync(workflowId, "step-1", output);

        var retrieved = await _service.GetContextAsync(workflowId);

        Assert.Equal("detailed", retrieved.UserPreferences["verbosityLevel"]);
        Assert.Equal("expert", retrieved.UserPreferences["technicalDepth"]);
    }

    [Fact]
    public async Task ContextWith_DecisionHistory_PreservedAndAccumulates()
    {
        var workflowId = Guid.NewGuid();
        var decision1 = new DecisionRecord
        {
            StepId = "step-1",
            Decision = "approved",
            AgentId = "reviewer"
        };
        var context = new SharedContext
        {
            DecisionHistory = new List<DecisionRecord> { decision1 }
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

        var retrieved = await _service.GetContextAsync(workflowId);

        Assert.Single(retrieved.DecisionHistory);
        Assert.Equal("approved", retrieved.DecisionHistory[0].Decision);
    }
}
