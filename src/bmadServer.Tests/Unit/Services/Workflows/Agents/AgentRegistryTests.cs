using Xunit;
using Microsoft.Extensions.Logging;
using Moq;
using bmadServer.ApiService.Services.Workflows.Agents;

namespace bmadServer.Tests.Unit.Services.Workflows.Agents;

public class AgentRegistryTests
{
    private readonly Mock<ILogger<AgentRegistry>> _mockLogger;
    private readonly AgentRegistry _registry;

    public AgentRegistryTests()
    {
        _mockLogger = new Mock<ILogger<AgentRegistry>>();
        _registry = new AgentRegistry(_mockLogger.Object);
    }

    [Fact]
    public void GetAllAgents_ReturnsAllDefaultAgents()
    {
        // Act
        var agents = _registry.GetAllAgents();

        // Assert
        Assert.NotEmpty(agents);
        Assert.Equal(6, agents.Count);
        Assert.Contains(agents, a => a.AgentId == "product-manager");
        Assert.Contains(agents, a => a.AgentId == "architect");
        Assert.Contains(agents, a => a.AgentId == "designer");
        Assert.Contains(agents, a => a.AgentId == "developer");
        Assert.Contains(agents, a => a.AgentId == "analyst");
        Assert.Contains(agents, a => a.AgentId == "orchestrator");
    }

    [Fact]
    public void GetAgent_WithValidId_ReturnsAgent()
    {
        // Act
        var agent = _registry.GetAgent("architect");

        // Assert
        Assert.NotNull(agent);
        Assert.Equal("architect", agent.AgentId);
        Assert.Equal("Architect", agent.Name);
        Assert.NotEmpty(agent.Capabilities);
    }

    [Fact]
    public void GetAgent_WithInvalidId_ReturnsNull()
    {
        // Act
        var agent = _registry.GetAgent("non-existent");

        // Assert
        Assert.Null(agent);
    }

    [Fact]
    public void GetAgent_WithNullId_ReturnsNull()
    {
        // Act
        var agent = _registry.GetAgent(null!);

        // Assert
        Assert.Null(agent);
    }

    [Fact]
    public void GetAgent_WithEmptyId_ReturnsNull()
    {
        // Act
        var agent = _registry.GetAgent("");

        // Assert
        Assert.Null(agent);
    }

    [Fact]
    public void GetAgentsByCapability_WithValidCapability_ReturnsMatchingAgents()
    {
        // Act
        var agents = _registry.GetAgentsByCapability("write-code");

        // Assert
        Assert.NotEmpty(agents);
        Assert.Single(agents);
        Assert.Equal("developer", agents[0].AgentId);
    }

    [Fact]
    public void GetAgentsByCapability_WithArchitectureCapability_ReturnsArchitectAgent()
    {
        // Act
        var agents = _registry.GetAgentsByCapability("create-architecture");

        // Assert
        Assert.NotEmpty(agents);
        Assert.Contains(agents, a => a.AgentId == "architect");
    }

    [Fact]
    public void GetAgentsByCapability_WithNonExistentCapability_ReturnsEmpty()
    {
        // Act
        var agents = _registry.GetAgentsByCapability("non-existent-capability");

        // Assert
        Assert.Empty(agents);
    }

    [Fact]
    public void GetAgentsByCapability_WithNullCapability_ReturnsEmpty()
    {
        // Act
        var agents = _registry.GetAgentsByCapability(null!);

        // Assert
        Assert.Empty(agents);
    }

    [Fact]
    public void RegisterAgent_WithValidAgent_SucceedsAndCanBeRetrieved()
    {
        // Arrange
        var newAgent = new AgentDefinition
        {
            AgentId = "test-agent",
            Name = "Test Agent",
            Description = "A test agent",
            SystemPrompt = "You are a test agent",
            Capabilities = ["test-capability"]
        };

        // Act
        _registry.RegisterAgent(newAgent);
        var retrieved = _registry.GetAgent("test-agent");

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal("test-agent", retrieved.AgentId);
        Assert.Equal("Test Agent", retrieved.Name);
    }

    [Fact]
    public void RegisterAgent_WithNullAgent_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _registry.RegisterAgent(null!));
    }

    [Fact]
    public void RegisterAgent_WithNullAgentId_ThrowsArgumentException()
    {
        // Arrange
        var agent = new AgentDefinition
        {
            AgentId = null!,
            Name = "Test",
            SystemPrompt = "Test",
            Capabilities = []
        };

        // Act & Assert
        Assert.Throws<ArgumentException>(() => _registry.RegisterAgent(agent));
    }

    [Fact]
    public void AllDefaultAgents_HaveRequiredProperties()
    {
        // Act
        var agents = _registry.GetAllAgents();

        // Assert
        foreach (var agent in agents)
        {
            Assert.NotNull(agent.AgentId);
            Assert.NotEmpty(agent.AgentId);
            Assert.NotNull(agent.Name);
            Assert.NotEmpty(agent.Name);
            Assert.NotNull(agent.SystemPrompt);
            Assert.NotEmpty(agent.SystemPrompt);
            Assert.NotNull(agent.Capabilities);
        }
    }

    [Fact]
    public void DeveloperAgent_HasCodeWritingCapabilities()
    {
        // Act
        var developer = _registry.GetAgent("developer");

        // Assert
        Assert.NotNull(developer);
        Assert.Contains("write-code", developer.Capabilities);
        Assert.Contains("implement-feature", developer.Capabilities);
        Assert.Contains("write-tests", developer.Capabilities);
    }

    [Fact]
    public void ArchitectAgent_HasArchitectureCapabilities()
    {
        // Act
        var architect = _registry.GetAgent("architect");

        // Assert
        Assert.NotNull(architect);
        Assert.Contains("create-architecture", architect.Capabilities);
        Assert.Contains("design-system", architect.Capabilities);
    }
}
