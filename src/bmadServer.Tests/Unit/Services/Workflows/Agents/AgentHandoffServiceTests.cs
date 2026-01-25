using bmadServer.ApiService.Services.Workflows.Agents;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace bmadServer.Tests.Unit.Services.Workflows.Agents;

public class AgentHandoffServiceTests
{
    private readonly Mock<IAgentRegistry> _mockRegistry;
    private readonly Mock<ILogger<AgentHandoffService>> _mockLogger;
    private readonly AgentHandoffService _service;

    public AgentHandoffServiceTests()
    {
        _mockRegistry = new Mock<IAgentRegistry>();
        _mockLogger = new Mock<ILogger<AgentHandoffService>>();
        _service = new AgentHandoffService(_mockRegistry.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task RecordHandoffAsync_CreatesHandoffRecord()
    {
        // Arrange
        var workflowId = Guid.NewGuid();

        // Act
        var handoff = await _service.RecordHandoffAsync(
            "product-manager",
            "architect",
            "design-phase",
            "Architecture design needed",
            workflowId);

        // Assert
        Assert.NotNull(handoff);
        Assert.Equal("product-manager", handoff.FromAgent);
        Assert.Equal("architect", handoff.ToAgent);
        Assert.Equal("design-phase", handoff.WorkflowStep);
        Assert.Equal(workflowId, handoff.WorkflowInstanceId);
    }

    [Fact]
    public async Task GetHandoffHistoryAsync_ReturnsOrderedHistory()
    {
        // Arrange
        var workflowId = Guid.NewGuid();

        await _service.RecordHandoffAsync("agent1", "agent2", "step1", "reason1", workflowId);
        await Task.Delay(10);
        await _service.RecordHandoffAsync("agent2", "agent3", "step2", "reason2", workflowId);
        await Task.Delay(10);
        await _service.RecordHandoffAsync("agent3", "agent4", "step3", "reason3", workflowId);

        // Act
        var history = await _service.GetHandoffHistoryAsync(workflowId);
        var handoffs = history.ToList();

        // Assert
        Assert.Equal(3, handoffs.Count);
        Assert.Equal("agent1", handoffs[0].FromAgent);
        Assert.Equal("agent2", handoffs[1].FromAgent);
        Assert.Equal("agent3", handoffs[2].FromAgent);
    }

    [Fact]
    public async Task GetCurrentAgentAsync_ReturnsLatestAgent()
    {
        // Arrange
        var workflowId = Guid.NewGuid();

        await _service.RecordHandoffAsync("agent1", "agent2", "step1", "reason", workflowId);
        await _service.RecordHandoffAsync("agent2", "agent3", "step2", "reason", workflowId);

        // Act
        var currentAgent = await _service.GetCurrentAgentAsync(workflowId);

        // Assert
        Assert.Equal("agent3", currentAgent);
    }

    [Fact]
    public async Task GetCurrentAgentAsync_WithNoHandoffs_ReturnsNull()
    {
        // Arrange
        var workflowId = Guid.NewGuid();

        // Act
        var currentAgent = await _service.GetCurrentAgentAsync(workflowId);

        // Assert
        Assert.Null(currentAgent);
    }

    [Fact]
    public async Task GetHandoffHistoryAsync_WithNoHandoffs_ReturnsEmpty()
    {
        // Arrange
        var workflowId = Guid.NewGuid();

        // Act
        var history = await _service.GetHandoffHistoryAsync(workflowId);

        // Assert
        Assert.Empty(history);
    }

    [Fact]
    public async Task CreateHandoffIndicatorAsync_WithValidAgent_ReturnsIndicator()
    {
        // Arrange
        var agent = new bmadServer.ApiService.Models.Agents.AgentDefinition
        {
            AgentId = "developer",
            Name = "Developer",
            Description = "Implements features",
            Capabilities = new List<string> { "implement-feature", "write-code" },
            SystemPrompt = "Test",
            ModelPreference = "gpt-4"
        };

        _mockRegistry.Setup(r => r.GetAgent("developer")).Returns(agent);

        // Act
        var indicator = await _service.CreateHandoffIndicatorAsync("developer", "implementation-phase");

        // Assert
        Assert.NotNull(indicator);
        Assert.Equal("Developer", indicator.AgentName);
        Assert.Equal("Implements features", indicator.Description);
        Assert.Equal(2, indicator.Capabilities.Count);
        Assert.Equal("implementation-phase", indicator.CurrentStep);
        Assert.Contains("Developer", indicator.Message);
    }

    [Fact]
    public async Task CreateHandoffIndicatorAsync_WithInvalidAgent_ReturnsNull()
    {
        // Arrange
        _mockRegistry.Setup(r => r.GetAgent("non-existent")).Returns((bmadServer.ApiService.Models.Agents.AgentDefinition?)null);

        // Act
        var indicator = await _service.CreateHandoffIndicatorAsync("non-existent", "step");

        // Assert
        Assert.Null(indicator);
    }

    [Fact]
    public async Task RecordHandoffAsync_LogsHandoff()
    {
        // Arrange
        var workflowId = Guid.NewGuid();

        // Act
        await _service.RecordHandoffAsync(
            "orchestrator",
            "analyst",
            "analysis-phase",
            "Data analysis required",
            workflowId);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Agent handoff recorded")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task MultipleWorkflows_MaintainSeparateHistory()
    {
        // Arrange
        var workflow1 = Guid.NewGuid();
        var workflow2 = Guid.NewGuid();

        // Act
        await _service.RecordHandoffAsync("a1", "a2", "s1", "r1", workflow1);
        await _service.RecordHandoffAsync("a2", "a3", "s2", "r2", workflow1);
        await _service.RecordHandoffAsync("b1", "b2", "s1", "r1", workflow2);

        var history1 = await _service.GetHandoffHistoryAsync(workflow1);
        var history2 = await _service.GetHandoffHistoryAsync(workflow2);

        // Assert
        Assert.Equal(2, history1.Count());
        Assert.Single(history2);
    }
}
