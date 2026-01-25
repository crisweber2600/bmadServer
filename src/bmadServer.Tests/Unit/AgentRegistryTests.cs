using bmadServer.ApiService.Agents;
using Xunit;

namespace bmadServer.Tests.Unit;

/// <summary>
/// Unit tests for AgentRegistry.
/// Verifies agent registry functionality for Story 5-1.
/// </summary>
public class AgentRegistryTests
{
    private readonly IAgentRegistry _agentRegistry;

    public AgentRegistryTests()
    {
        _agentRegistry = new AgentRegistry();
    }

    [Fact]
    public void GetAllAgents_ShouldReturnAllSixBMADAgents()
    {
        // Act
        var agents = _agentRegistry.GetAllAgents().ToList();

        // Assert
        Assert.NotNull(agents);
        Assert.Equal(6, agents.Count);
        
        // Verify all expected agents are present
        var expectedAgentIds = new[] 
        { 
            "product-manager", 
            "architect", 
            "designer", 
            "developer", 
            "analyst", 
            "orchestrator" 
        };
        
        foreach (var expectedId in expectedAgentIds)
        {
            Assert.Contains(agents, a => a.AgentId == expectedId);
        }
    }

    [Fact]
    public void GetAgent_ShouldReturnCorrectAgent_WhenIdExists()
    {
        // Act
        var agent = _agentRegistry.GetAgent("architect");

        // Assert
        Assert.NotNull(agent);
        Assert.Equal("architect", agent.AgentId);
        Assert.Equal("Architect", agent.Name);
        Assert.NotEmpty(agent.Description);
        Assert.NotEmpty(agent.Capabilities);
        Assert.NotEmpty(agent.SystemPrompt);
        Assert.NotEmpty(agent.ModelPreference);
    }

    [Fact]
    public void GetAgent_ShouldReturnNull_WhenIdDoesNotExist()
    {
        // Act
        var agent = _agentRegistry.GetAgent("non-existent-agent");

        // Assert
        Assert.Null(agent);
    }

    [Fact]
    public void GetAgent_ShouldBeCaseInsensitive()
    {
        // Act
        var agent1 = _agentRegistry.GetAgent("architect");
        var agent2 = _agentRegistry.GetAgent("ARCHITECT");
        var agent3 = _agentRegistry.GetAgent("Architect");

        // Assert
        Assert.NotNull(agent1);
        Assert.NotNull(agent2);
        Assert.NotNull(agent3);
        Assert.Equal(agent1.AgentId, agent2.AgentId);
        Assert.Equal(agent1.AgentId, agent3.AgentId);
    }

    [Fact]
    public void GetAgentsByCapability_ShouldReturnMatchingAgents()
    {
        // Act
        var agents = _agentRegistry.GetAgentsByCapability("create-architecture").ToList();

        // Assert
        Assert.NotEmpty(agents);
        Assert.Contains(agents, a => a.AgentId == "architect");
        Assert.All(agents, agent => 
            Assert.Contains("create-architecture", agent.Capabilities, StringComparer.OrdinalIgnoreCase));
    }

    [Fact]
    public void GetAgentsByCapability_ShouldReturnEmpty_WhenNoMatch()
    {
        // Act
        var agents = _agentRegistry.GetAgentsByCapability("non-existent-capability").ToList();

        // Assert
        Assert.NotNull(agents);
        Assert.Empty(agents);
    }

    [Fact]
    public void GetAgentsByCapability_ShouldBeCaseInsensitive()
    {
        // Act
        var agents1 = _agentRegistry.GetAgentsByCapability("create-architecture").ToList();
        var agents2 = _agentRegistry.GetAgentsByCapability("CREATE-ARCHITECTURE").ToList();

        // Assert
        Assert.Equal(agents1.Count, agents2.Count);
        Assert.All(agents1, a1 => Assert.Contains(agents2, a2 => a2.AgentId == a1.AgentId));
    }

    [Fact]
    public void AllAgents_ShouldHaveRequiredProperties()
    {
        // Act
        var agents = _agentRegistry.GetAllAgents();

        // Assert
        Assert.All(agents, agent =>
        {
            Assert.NotNull(agent.AgentId);
            Assert.NotEmpty(agent.AgentId);
            Assert.NotNull(agent.Name);
            Assert.NotEmpty(agent.Name);
            Assert.NotNull(agent.Description);
            Assert.NotEmpty(agent.Description);
            Assert.NotNull(agent.Capabilities);
            Assert.NotEmpty(agent.Capabilities);
            Assert.NotNull(agent.SystemPrompt);
            Assert.NotEmpty(agent.SystemPrompt);
            Assert.NotNull(agent.ModelPreference);
            Assert.NotEmpty(agent.ModelPreference);
        });
    }

    [Fact]
    public void AllAgents_ShouldHaveValidCapabilities()
    {
        // Act
        var agents = _agentRegistry.GetAllAgents();

        // Assert
        Assert.All(agents, agent =>
        {
            Assert.NotEmpty(agent.Capabilities);
            // Capabilities should be in kebab-case format (workflow step convention)
            Assert.All(agent.Capabilities, cap =>
            {
                Assert.False(string.IsNullOrWhiteSpace(cap));
                Assert.Matches(@"^[a-z]+(-[a-z]+)*$", cap);
            });
        });
    }

    [Fact]
    public void AllAgents_ShouldHaveValidModelPreferences()
    {
        // Act
        var agents = _agentRegistry.GetAllAgents();

        // Assert
        var validPrefixes = new[] { "gpt", "claude", "o1", "gemini", "llama" };
        Assert.All(agents, agent =>
        {
            Assert.True(
                validPrefixes.Any(prefix => agent.ModelPreference.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)),
                $"Agent {agent.AgentId} has invalid model preference: {agent.ModelPreference}"
            );
        });
    }

    [Theory]
    [InlineData("product-manager", "Product Manager")]
    [InlineData("architect", "Architect")]
    [InlineData("designer", "Designer")]
    [InlineData("developer", "Developer")]
    [InlineData("analyst", "Analyst")]
    [InlineData("orchestrator", "Orchestrator")]
    public void GetAgent_ShouldReturnCorrectName_ForEachAgent(string agentId, string expectedName)
    {
        // Act
        var agent = _agentRegistry.GetAgent(agentId);

        // Assert
        Assert.NotNull(agent);
        Assert.Equal(expectedName, agent.Name);
    }

    [Fact]
    public void ArchitectAgent_ShouldHaveCreateArchitectureCapability()
    {
        // Act
        var architect = _agentRegistry.GetAgent("architect");

        // Assert
        Assert.NotNull(architect);
        Assert.Contains("create-architecture", architect.Capabilities, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void ProductManagerAgent_ShouldHaveCreatePrdCapability()
    {
        // Act
        var pm = _agentRegistry.GetAgent("product-manager");

        // Assert
        Assert.NotNull(pm);
        Assert.Contains("create-prd", pm.Capabilities, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void DeveloperAgent_ShouldHaveImplementFeatureCapability()
    {
        // Act
        var developer = _agentRegistry.GetAgent("developer");

        // Assert
        Assert.NotNull(developer);
        Assert.Contains("implement-feature", developer.Capabilities, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void OrchestratorAgent_ShouldHaveCoordinateWorkflowCapability()
    {
        // Act
        var orchestrator = _agentRegistry.GetAgent("orchestrator");

        // Assert
        Assert.NotNull(orchestrator);
        Assert.Contains("coordinate-workflow", orchestrator.Capabilities, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void AllAgents_ShouldHaveUniqueIds()
    {
        // Act
        var agents = _agentRegistry.GetAllAgents().ToList();

        // Assert
        var agentIds = agents.Select(a => a.AgentId).ToList();
        var uniqueIds = agentIds.Distinct().ToList();
        Assert.Equal(agentIds.Count, uniqueIds.Count);
    }

    [Fact]
    public void AllAgents_ShouldHaveAtLeastOneCapability()
    {
        // Act
        var agents = _agentRegistry.GetAllAgents();

        // Assert
        Assert.All(agents, agent => Assert.NotEmpty(agent.Capabilities));
    }

    [Fact]
    public void AllAgents_ShouldHaveDescriptiveSystemPrompts()
    {
        // Act
        var agents = _agentRegistry.GetAllAgents();

        // Assert
        Assert.All(agents, agent =>
        {
            Assert.True(agent.SystemPrompt.Length > 50, 
                $"Agent {agent.AgentId} should have a descriptive system prompt");
            Assert.Contains("You are", agent.SystemPrompt);
        });
    }
}
