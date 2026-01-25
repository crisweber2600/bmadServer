using System.Text.Json;
using bmadServer.ApiService.Models.Agents;
using bmadServer.ApiService.Services.Workflows.Agents;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace bmadServer.Tests.Unit.Services.Workflows.Agents;

public class SharedContextServiceTests
{
    private readonly Mock<ILogger<SharedContextService>> _mockLogger;
    private readonly SharedContextService _service;

    public SharedContextServiceTests()
    {
        _mockLogger = new Mock<ILogger<SharedContextService>>();
        _service = new SharedContextService(_mockLogger.Object);
    }

    [Fact]
    public async Task CreateContextAsync_CreatesNewContext()
    {
        // Arrange
        var workflowId = Guid.NewGuid();

        // Act
        var context = await _service.CreateContextAsync(workflowId);

        // Assert
        Assert.NotNull(context);
        Assert.Equal(workflowId, context.WorkflowInstanceId);
        Assert.Equal(1, context.Version);
        Assert.Empty(context.StepOutputs);
        Assert.Empty(context.DecisionHistory);
        Assert.Empty(context.ArtifactReferences);
    }

    [Fact]
    public async Task GetContextAsync_ReturnsCreatedContext()
    {
        // Arrange
        var workflowId = Guid.NewGuid();
        await _service.CreateContextAsync(workflowId);

        // Act
        var context = await _service.GetContextAsync(workflowId);

        // Assert
        Assert.NotNull(context);
        Assert.Equal(workflowId, context.WorkflowInstanceId);
    }

    [Fact]
    public async Task GetContextAsync_WithNonExistentWorkflow_ReturnsNull()
    {
        // Arrange
        var workflowId = Guid.NewGuid();

        // Act
        var context = await _service.GetContextAsync(workflowId);

        // Assert
        Assert.Null(context);
    }

    [Fact]
    public async Task AddStepOutputAsync_AddsOutputToContext()
    {
        // Arrange
        var workflowId = Guid.NewGuid();
        await _service.CreateContextAsync(workflowId);
        
        var output = JsonDocument.Parse("{\"result\": \"success\"}");

        // Act
        var result = await _service.AddStepOutputAsync(workflowId, "step-1", output);

        // Assert
        Assert.True(result);

        var context = await _service.GetContextAsync(workflowId);
        Assert.NotNull(context);
        Assert.Equal(2, context.Version); // Version incremented
        Assert.Single(context.StepOutputs);
        Assert.True(context.StepOutputs.ContainsKey("step-1"));
    }

    [Fact]
    public async Task GetStepOutputAsync_ReturnsCorrectOutput()
    {
        // Arrange
        var workflowId = Guid.NewGuid();
        await _service.CreateContextAsync(workflowId);
        
        var output = JsonDocument.Parse("{\"data\": \"test\"}");
        await _service.AddStepOutputAsync(workflowId, "step-2", output);

        // Act
        var retrieved = await _service.GetStepOutputAsync(workflowId, "step-2");

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal("test", retrieved.RootElement.GetProperty("data").GetString());
    }

    [Fact]
    public async Task GetStepOutputAsync_WithNonExistentStep_ReturnsNull()
    {
        // Arrange
        var workflowId = Guid.NewGuid();
        await _service.CreateContextAsync(workflowId);

        // Act
        var output = await _service.GetStepOutputAsync(workflowId, "non-existent");

        // Assert
        Assert.Null(output);
    }

    [Fact]
    public async Task AddDecisionAsync_AddsDecisionToContext()
    {
        // Arrange
        var workflowId = Guid.NewGuid();
        await _service.CreateContextAsync(workflowId);

        var decision = new DecisionRecord
        {
            DecisionId = "decision-1",
            AgentId = "architect",
            Description = "Chose microservices architecture",
            Data = JsonDocument.Parse("{\"architecture\": \"microservices\"}")
        };

        // Act
        var result = await _service.AddDecisionAsync(workflowId, decision);

        // Assert
        Assert.True(result);

        var context = await _service.GetContextAsync(workflowId);
        Assert.NotNull(context);
        Assert.Single(context.DecisionHistory);
        Assert.Equal("decision-1", context.DecisionHistory[0].DecisionId);
        Assert.Equal("architect", context.DecisionHistory[0].AgentId);
    }

    [Fact]
    public async Task UpdateContextAsync_WithCorrectVersion_UpdatesSuccessfully()
    {
        // Arrange
        var workflowId = Guid.NewGuid();
        var context = await _service.CreateContextAsync(workflowId);
        
        // Modify context
        context.UserPreferences = JsonDocument.Parse("{\"theme\": \"dark\"}");

        // Act
        var result = await _service.UpdateContextAsync(context);

        // Assert
        Assert.True(result);

        var updated = await _service.GetContextAsync(workflowId);
        Assert.NotNull(updated);
        Assert.Equal(2, updated.Version);
        Assert.NotNull(updated.UserPreferences);
    }

    [Fact]
    public async Task UpdateContextAsync_WithIncorrectVersion_Fails()
    {
        // Arrange
        var workflowId = Guid.NewGuid();
        var context = await _service.CreateContextAsync(workflowId);
        
        // Simulate concurrent modification
        var output = JsonDocument.Parse("{\"data\": \"concurrent\"}");
        await _service.AddStepOutputAsync(workflowId, "step-x", output);
        
        // Try to update with old version
        context.UserPreferences = JsonDocument.Parse("{\"theme\": \"light\"}");

        // Act
        var result = await _service.UpdateContextAsync(context);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task AddStepOutputAsync_WithNonExistentWorkflow_ReturnsFalse()
    {
        // Arrange
        var workflowId = Guid.NewGuid();
        var output = JsonDocument.Parse("{\"result\": \"test\"}");

        // Act
        var result = await _service.AddStepOutputAsync(workflowId, "step-1", output);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task MultipleStepOutputs_AreStoredCorrectly()
    {
        // Arrange
        var workflowId = Guid.NewGuid();
        await _service.CreateContextAsync(workflowId);

        var output1 = JsonDocument.Parse("{\"step\": 1}");
        var output2 = JsonDocument.Parse("{\"step\": 2}");
        var output3 = JsonDocument.Parse("{\"step\": 3}");

        // Act
        await _service.AddStepOutputAsync(workflowId, "step-1", output1);
        await _service.AddStepOutputAsync(workflowId, "step-2", output2);
        await _service.AddStepOutputAsync(workflowId, "step-3", output3);

        // Assert
        var context = await _service.GetContextAsync(workflowId);
        Assert.NotNull(context);
        Assert.Equal(3, context.StepOutputs.Count);
        Assert.Equal(4, context.Version); // 1 (initial) + 3 (additions)
    }

    [Fact]
    public async Task DecisionHistory_MaintainsOrder()
    {
        // Arrange
        var workflowId = Guid.NewGuid();
        await _service.CreateContextAsync(workflowId);

        var decision1 = new DecisionRecord
        {
            DecisionId = "d1",
            AgentId = "agent1",
            Description = "First decision",
            Data = JsonDocument.Parse("{}")
        };

        var decision2 = new DecisionRecord
        {
            DecisionId = "d2",
            AgentId = "agent2",
            Description = "Second decision",
            Data = JsonDocument.Parse("{}")
        };

        // Act
        await _service.AddDecisionAsync(workflowId, decision1);
        await Task.Delay(10); // Small delay to ensure timestamp difference
        await _service.AddDecisionAsync(workflowId, decision2);

        // Assert
        var context = await _service.GetContextAsync(workflowId);
        Assert.NotNull(context);
        Assert.Equal(2, context.DecisionHistory.Count);
        Assert.Equal("d1", context.DecisionHistory[0].DecisionId);
        Assert.Equal("d2", context.DecisionHistory[1].DecisionId);
    }
}
