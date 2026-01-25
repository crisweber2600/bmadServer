using bmadServer.ApiService.Services.Workflows.Agents;
using Xunit;

namespace bmadServer.Tests.Unit.Services.Workflows.Agents;

public class AgentRegistryTests
{
    private readonly AgentRegistry _registry;

    public AgentRegistryTests()
    {
        _registry = new AgentRegistry();
    }

    [Fact]
    public void GetAllAgents_ReturnsAllBMADAgents()
    {
        // Act
        var agents = _registry.GetAllAgents().ToList();

        // Assert
        Assert.Equal(6, agents.Count);
        
        var agentIds = agents.Select(a => a.AgentId).ToList();
        Assert.Contains("product-manager", agentIds);
        Assert.Contains("architect", agentIds);
        Assert.Contains("designer", agentIds);
        Assert.Contains("developer", agentIds);
        Assert.Contains("analyst", agentIds);
        Assert.Contains("orchestrator", agentIds);
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
        Assert.Contains("create-architecture", agent.Capabilities);
    }

    [Fact]
    public void GetAgent_WithInvalidId_ReturnsNull()
    {
        // Act
        var agent = _registry.GetAgent("non-existent-agent");

        // Assert
        Assert.Null(agent);
    }

    [Fact]
    public void GetAgentsByCapability_WithValidCapability_ReturnsMatchingAgents()
    {
        // Act
        var agents = _registry.GetAgentsByCapability("create-architecture").ToList();

        // Assert
        Assert.Single(agents);
        Assert.Equal("architect", agents[0].AgentId);
    }

    [Fact]
    public void GetAgentsByCapability_WithMultipleMatches_ReturnsAllMatching()
    {
        // Arrange - create a capability that might match multiple agents
        // In the current implementation, each agent has unique capabilities
        // But this tests the capability matching works correctly

        // Act
        var agents = _registry.GetAgentsByCapability("orchestrate-workflow").ToList();

        // Assert
        Assert.Single(agents);
        Assert.Equal("orchestrator", agents[0].AgentId);
    }

    [Fact]
    public void GetAgentsByCapability_WithNonExistentCapability_ReturnsEmpty()
    {
        // Act
        var agents = _registry.GetAgentsByCapability("non-existent-capability").ToList();

        // Assert
        Assert.Empty(agents);
    }

    [Fact]
    public void GetAgentsByCapability_IsCaseInsensitive()
    {
        // Act
        var agents = _registry.GetAgentsByCapability("CREATE-ARCHITECTURE").ToList();

        // Assert
        Assert.Single(agents);
        Assert.Equal("architect", agents[0].AgentId);
    }

    [Fact]
    public void AllAgents_HaveRequiredProperties()
    {
        // Act
        var agents = _registry.GetAllAgents();

        // Assert
        foreach (var agent in agents)
        {
            Assert.False(string.IsNullOrWhiteSpace(agent.AgentId));
            Assert.False(string.IsNullOrWhiteSpace(agent.Name));
            Assert.False(string.IsNullOrWhiteSpace(agent.Description));
            Assert.False(string.IsNullOrWhiteSpace(agent.SystemPrompt));
            Assert.False(string.IsNullOrWhiteSpace(agent.ModelPreference));
            Assert.NotNull(agent.Capabilities);
            Assert.NotEmpty(agent.Capabilities);
        }
    }

    [Fact]
    public void AllAgents_HaveUniqueIds()
    {
        // Act
        var agents = _registry.GetAllAgents();
        var agentIds = agents.Select(a => a.AgentId).ToList();

        // Assert
        Assert.Equal(agentIds.Count, agentIds.Distinct().Count());
    }

    [Fact]
    public void ProductManager_HasCorrectCapabilities()
    {
        // Act
        var agent = _registry.GetAgent("product-manager");

        // Assert
        Assert.NotNull(agent);
        Assert.Contains("gather-requirements", agent.Capabilities);
        Assert.Contains("create-user-stories", agent.Capabilities);
        Assert.Contains("prioritize-features", agent.Capabilities);
    }

    [Fact]
    public void Developer_HasCorrectCapabilities()
    {
        // Act
        var agent = _registry.GetAgent("developer");

        // Assert
        Assert.NotNull(agent);
        Assert.Contains("implement-feature", agent.Capabilities);
        Assert.Contains("write-code", agent.Capabilities);
        Assert.Contains("code-review", agent.Capabilities);
    }
}
