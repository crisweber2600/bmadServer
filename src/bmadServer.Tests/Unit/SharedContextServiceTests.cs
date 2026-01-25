using bmadServer.ApiService.Data;
using bmadServer.ApiService.WorkflowContext;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace bmadServer.Tests.Unit;

public class SharedContextServiceTests
{
    private ApplicationDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task CreateContextAsync_ShouldCreateNewContext()
    {
        // Arrange
        await using var dbContext = CreateInMemoryContext();
        var service = new SharedContextService(dbContext);
        var workflowId = Guid.NewGuid();

        // Act
        var result = await service.CreateContextAsync(workflowId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(workflowId, result.WorkflowInstanceId);
        Assert.Equal(1, result.Version);
    }

    [Fact]
    public async Task CreateContextAsync_ShouldPersistToDatabase()
    {
        // Arrange
        await using var dbContext = CreateInMemoryContext();
        var service = new SharedContextService(dbContext);
        var workflowId = Guid.NewGuid();

        // Act
        await service.CreateContextAsync(workflowId);

        // Assert
        var entity = await dbContext.WorkflowContexts
            .FirstOrDefaultAsync(wc => wc.WorkflowInstanceId == workflowId);
        Assert.NotNull(entity);
    }

    [Fact]
    public async Task CreateContextAsync_ShouldIncludeUserPreferences()
    {
        // Arrange
        await using var dbContext = CreateInMemoryContext();
        var service = new SharedContextService(dbContext);
        var workflowId = Guid.NewGuid();
        var preferences = new UserPreferences
        {
            PreferredLanguage = "en-US"
        };

        // Act
        var result = await service.CreateContextAsync(workflowId, preferences);

        // Assert
        Assert.NotNull(result.UserPreferences);
        Assert.Equal("en-US", result.UserPreferences.PreferredLanguage);
    }

    [Fact]
    public async Task GetContextAsync_ShouldReturnNull_WhenContextNotFound()
    {
        // Arrange
        await using var dbContext = CreateInMemoryContext();
        var service = new SharedContextService(dbContext);
        var workflowId = Guid.NewGuid();

        // Act
        var result = await service.GetContextAsync(workflowId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetContextAsync_ShouldReturnContext_WhenExists()
    {
        // Arrange
        await using var dbContext = CreateInMemoryContext();
        var service = new SharedContextService(dbContext);
        var workflowId = Guid.NewGuid();
        await service.CreateContextAsync(workflowId);

        // Act
        var result = await service.GetContextAsync(workflowId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(workflowId, result.WorkflowInstanceId);
    }

    [Fact]
    public async Task UpdateContextAsync_ShouldUpdateContext()
    {
        // Arrange
        await using var dbContext = CreateInMemoryContext();
        var service = new SharedContextService(dbContext);
        var workflowId = Guid.NewGuid();
        var context = await service.CreateContextAsync(workflowId);

        // Act
        context.AddStepOutput("test-step", new StepOutput
        {
            StepId = "test-step",
            Data = new { Result = "Success" },
            CompletedAt = DateTime.UtcNow,
            CompletedByAgent = "test-agent"
        });

        var updated = await service.UpdateContextAsync(context);

        // Assert
        Assert.Equal(2, updated.Version);
        Assert.Contains("test-step", updated.StepOutputs.Keys);
    }

    [Fact]
    public async Task UpdateContextAsync_ShouldThrow_WhenContextNotFound()
    {
        // Arrange
        await using var dbContext = CreateInMemoryContext();
        var service = new SharedContextService(dbContext);
        var context = new SharedContext
        {
            WorkflowInstanceId = Guid.NewGuid()
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await service.UpdateContextAsync(context));
    }

    [Fact]
    public async Task UpdateContextAsync_ShouldThrow_OnVersionMismatch()
    {
        // Arrange
        await using var dbContext = CreateInMemoryContext();
        var service = new SharedContextService(dbContext);
        var workflowId = Guid.NewGuid();
        _ = await service.CreateContextAsync(workflowId); // Create initial context in DB

        // Simulate version mismatch by creating a context with wrong version
        var staleCopy = new SharedContext
        {
            WorkflowInstanceId = workflowId,
            Version = 5 // Incorrect version
        };

        // Act & Assert
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(
            async () => await service.UpdateContextAsync(staleCopy));
    }

    [Fact]
    public async Task AddStepOutputAsync_ShouldAddOutputToContext()
    {
        // Arrange
        await using var dbContext = CreateInMemoryContext();
        var service = new SharedContextService(dbContext);
        var workflowId = Guid.NewGuid();
        await service.CreateContextAsync(workflowId);

        var output = new StepOutput
        {
            StepId = "test-step",
            Data = new { Result = "Success" },
            CompletedAt = DateTime.UtcNow,
            CompletedByAgent = "test-agent"
        };

        // Act
        await service.AddStepOutputAsync(workflowId, "test-step", output);

        // Assert
        var context = await service.GetContextAsync(workflowId);
        Assert.NotNull(context);
        Assert.Contains("test-step", context.StepOutputs.Keys);
    }

    [Fact]
    public async Task AddStepOutputAsync_ShouldIncrementVersion()
    {
        // Arrange
        await using var dbContext = CreateInMemoryContext();
        var service = new SharedContextService(dbContext);
        var workflowId = Guid.NewGuid();
        await service.CreateContextAsync(workflowId);

        var output = new StepOutput
        {
            StepId = "test-step",
            Data = new { },
            CompletedAt = DateTime.UtcNow,
            CompletedByAgent = "test-agent"
        };

        // Act
        await service.AddStepOutputAsync(workflowId, "test-step", output);

        // Assert
        var context = await service.GetContextAsync(workflowId);
        Assert.NotNull(context);
        Assert.Equal(2, context.Version);
    }

    [Fact]
    public async Task AddStepOutputAsync_ShouldThrow_WhenContextNotFound()
    {
        // Arrange
        await using var dbContext = CreateInMemoryContext();
        var service = new SharedContextService(dbContext);
        var workflowId = Guid.NewGuid();

        var output = new StepOutput
        {
            StepId = "test-step",
            Data = new { },
            CompletedAt = DateTime.UtcNow,
            CompletedByAgent = "test-agent"
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await service.AddStepOutputAsync(workflowId, "test-step", output));
    }

    [Fact]
    public async Task ConcurrentUpdates_ShouldDetectVersionConflict()
    {
        // Arrange
        await using var dbContext = CreateInMemoryContext();
        var service = new SharedContextService(dbContext);
        var workflowId = Guid.NewGuid();
        await service.CreateContextAsync(workflowId);

        var context1 = await service.GetContextAsync(workflowId);
        var context2 = await service.GetContextAsync(workflowId);

        // Act
        context1!.AddStepOutput("step-1", new StepOutput
        {
            StepId = "step-1",
            Data = new { },
            CompletedAt = DateTime.UtcNow,
            CompletedByAgent = "agent-1"
        });

        await service.UpdateContextAsync(context1);

        // Second update should fail due to version mismatch
        context2!.AddStepOutput("step-2", new StepOutput
        {
            StepId = "step-2",
            Data = new { },
            CompletedAt = DateTime.UtcNow,
            CompletedByAgent = "agent-2"
        });

        // Assert
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(
            async () => await service.UpdateContextAsync(context2));
    }

    [Fact]
    public async Task ContextPersistence_ShouldPreserveAllData()
    {
        // Arrange
        await using var dbContext = CreateInMemoryContext();
        var service = new SharedContextService(dbContext);
        var workflowId = Guid.NewGuid();
        
        var preferences = new UserPreferences
        {
            PreferredLanguage = "en-US",
            DisplaySettings = new Dictionary<string, object> { { "theme", "dark" } }
        };

        var context = await service.CreateContextAsync(workflowId, preferences);

        // Add step output
        context.AddStepOutput("step-1", new StepOutput
        {
            StepId = "step-1",
            Data = new { Value = "test" },
            CompletedAt = DateTime.UtcNow,
            CompletedByAgent = "agent-1"
        });
        context = await service.UpdateContextAsync(context);

        // Add decision
        context.AddDecision(new Decision
        {
            DecisionType = "test-decision",
            MadeBy = "agent-1",
            Timestamp = DateTime.UtcNow,
            Rationale = "test rationale",
            DecisionValue = "test-value"
        });
        context = await service.UpdateContextAsync(context);

        // Add artifact
        context.AddArtifactReference(new ArtifactReference
        {
            ArtifactType = "diagram",
            StorageLocation = "/path/to/artifact",
            CreatedAt = DateTime.UtcNow,
            CreatedByStep = "step-1"
        });
        await service.UpdateContextAsync(context);

        // Act - Retrieve from database
        var retrieved = await service.GetContextAsync(workflowId);

        // Assert
        Assert.NotNull(retrieved);
        Assert.Single(retrieved.StepOutputs);
        Assert.Single(retrieved.DecisionHistory);
        Assert.Single(retrieved.ArtifactReferences);
        Assert.NotNull(retrieved.UserPreferences);
        Assert.Equal("en-US", retrieved.UserPreferences.PreferredLanguage);
    }
}
