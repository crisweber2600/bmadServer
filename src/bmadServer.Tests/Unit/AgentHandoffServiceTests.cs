using bmadServer.ApiService.Agents;
using bmadServer.ApiService.Data;
using bmadServer.ApiService.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace bmadServer.Tests.Unit;

public class AgentHandoffServiceTests
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IAgentRegistry _agentRegistry;
    private readonly Mock<ILogger<AgentHandoffService>> _loggerMock;
    private readonly AgentHandoffService _service;

    public AgentHandoffServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new ApplicationDbContext(options);
        _agentRegistry = new AgentRegistry();
        _loggerMock = new Mock<ILogger<AgentHandoffService>>();
        _service = new AgentHandoffService(_dbContext, _agentRegistry, _loggerMock.Object);
    }

    [Fact]
    public async Task RecordHandoffAsync_WithValidAgents_RecordsHandoff()
    {
        // Arrange
        var workflowInstanceId = Guid.NewGuid();

        // Act
        var result = await _service.RecordHandoffAsync(
            workflowInstanceId,
            "product-manager",
            "architect",
            "design-step",
            "Need architecture expertise");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(workflowInstanceId, result.WorkflowInstanceId);
        Assert.Equal("product-manager", result.FromAgent);
        Assert.Equal("architect", result.ToAgent);
        Assert.Equal("design-step", result.WorkflowStep);
        Assert.Equal("Need architecture expertise", result.Reason);
        Assert.Equal("Architect", result.ToAgentName);
        Assert.Equal("Product Manager", result.FromAgentName);
    }

    [Fact]
    public async Task RecordHandoffAsync_WithNullFromAgent_RecordsInitialHandoff()
    {
        // Arrange
        var workflowInstanceId = Guid.NewGuid();

        // Act
        var result = await _service.RecordHandoffAsync(
            workflowInstanceId,
            null,
            "product-manager",
            "initial-step",
            "Starting workflow");

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.FromAgent);
        Assert.Null(result.FromAgentName);
        Assert.Equal("product-manager", result.ToAgent);
        Assert.Equal("Product Manager", result.ToAgentName);
    }

    [Fact]
    public async Task RecordHandoffAsync_WithInvalidToAgent_ThrowsException()
    {
        // Arrange
        var workflowInstanceId = Guid.NewGuid();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _service.RecordHandoffAsync(
                workflowInstanceId,
                "product-manager",
                "invalid-agent",
                "step",
                "reason"));
    }

    [Fact]
    public async Task RecordHandoffAsync_PersistsToDatabase()
    {
        // Arrange
        var workflowInstanceId = Guid.NewGuid();

        // Act
        await _service.RecordHandoffAsync(
            workflowInstanceId,
            null,
            "product-manager",
            "step1",
            "Initial");

        // Assert
        var handoffs = await _dbContext.Set<AgentHandoff>()
            .Where(h => h.WorkflowInstanceId == workflowInstanceId)
            .ToListAsync();

        Assert.Single(handoffs);
        Assert.Equal("product-manager", handoffs[0].ToAgent);
    }

    [Fact]
    public async Task GetHandoffsAsync_ReturnsHandoffsInChronologicalOrder()
    {
        // Arrange
        var workflowInstanceId = Guid.NewGuid();

        await _service.RecordHandoffAsync(workflowInstanceId, null, "product-manager", "step1", "First");
        await Task.Delay(10);
        await _service.RecordHandoffAsync(workflowInstanceId, "product-manager", "architect", "step2", "Second");
        await Task.Delay(10);
        await _service.RecordHandoffAsync(workflowInstanceId, "architect", "product-manager", "step3", "Third");

        // Act
        var result = await _service.GetHandoffsAsync(workflowInstanceId);

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Equal("product-manager", result[0].ToAgent);
        Assert.Equal("architect", result[1].ToAgent);
        Assert.Equal("product-manager", result[2].ToAgent);
        Assert.True(result[0].Timestamp <= result[1].Timestamp);
        Assert.True(result[1].Timestamp <= result[2].Timestamp);
    }

    [Fact]
    public async Task GetHandoffsAsync_WithNoHandoffs_ReturnsEmptyList()
    {
        // Arrange
        var workflowInstanceId = Guid.NewGuid();

        // Act
        var result = await _service.GetHandoffsAsync(workflowInstanceId);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetCurrentAgentAsync_ReturnsLatestAgent()
    {
        // Arrange
        var workflowInstanceId = Guid.NewGuid();

        await _service.RecordHandoffAsync(workflowInstanceId, null, "product-manager", "step1", "First");
        await Task.Delay(10);
        await _service.RecordHandoffAsync(workflowInstanceId, "product-manager", "architect", "step2", "Second");

        // Act
        var result = await _service.GetCurrentAgentAsync(workflowInstanceId);

        // Assert
        Assert.Equal("architect", result);
    }

    [Fact]
    public async Task GetCurrentAgentAsync_WithNoHandoffs_ReturnsNull()
    {
        // Arrange
        var workflowInstanceId = Guid.NewGuid();

        // Act
        var result = await _service.GetCurrentAgentAsync(workflowInstanceId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetAgentDetailsAsync_ReturnsAgentDetails()
    {
        // Act
        var result = await _service.GetAgentDetailsAsync("architect");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("architect", result.AgentId);
        Assert.Equal("Architect", result.Name);
        Assert.NotEmpty(result.Description);
        Assert.NotEmpty(result.Capabilities);
        Assert.NotEmpty(result.Avatar!);
    }

    [Fact]
    public async Task GetAgentDetailsAsync_WithWorkflowStep_IncludesStepResponsibility()
    {
        // Act
        var result = await _service.GetAgentDetailsAsync("architect", "create-architecture");

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.CurrentStepResponsibility);
        Assert.Contains("create-architecture", result.CurrentStepResponsibility);
    }

    [Fact]
    public async Task GetAgentDetailsAsync_WithNonMatchingStep_NoStepResponsibility()
    {
        // Act
        var result = await _service.GetAgentDetailsAsync("architect", "gather-requirements");

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.CurrentStepResponsibility);
    }

    [Fact]
    public async Task GetAgentDetailsAsync_WithInvalidAgent_ReturnsNull()
    {
        // Act
        var result = await _service.GetAgentDetailsAsync("invalid-agent");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetAgentDetailsAsync_GeneratesAvatar()
    {
        // Act
        var result = await _service.GetAgentDetailsAsync("architect");

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.Avatar!);
        Assert.Equal("AR", result.Avatar);
    }

    [Fact]
    public async Task RecordHandoffAsync_LogsHandoff()
    {
        // Arrange
        var workflowInstanceId = Guid.NewGuid();

        // Act
        await _service.RecordHandoffAsync(
            workflowInstanceId,
            "product-manager",
            "architect",
            "design",
            "Need architect");

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Agent handoff recorded")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task MultipleWorkflows_HandoffsAreIsolated()
    {
        // Arrange
        var workflow1 = Guid.NewGuid();
        var workflow2 = Guid.NewGuid();

        await _service.RecordHandoffAsync(workflow1, null, "product-manager", "step1", "W1");
        await _service.RecordHandoffAsync(workflow2, null, "architect", "step1", "W2");

        // Act
        var handoffs1 = await _service.GetHandoffsAsync(workflow1);
        var handoffs2 = await _service.GetHandoffsAsync(workflow2);

        // Assert
        Assert.Single(handoffs1);
        Assert.Equal("product-manager", handoffs1[0].ToAgent);

        Assert.Single(handoffs2);
        Assert.Equal("architect", handoffs2[0].ToAgent);
    }
}
