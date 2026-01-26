using bmadServer.ApiService.Services.Workflows.Agents;
using Xunit;
using FluentAssertions;

namespace bmadServer.Tests.Services.Workflows.Agents;

public class AgentRegistryTests
{
    [Fact]
    public void GetAllAgents_ShouldReturnAllBmadAgents()
    {
        // Arrange
        var registry = new AgentRegistry();

        // Act
        var agents = registry.GetAllAgents();

        // Assert
        agents.Should().NotBeNull();
        agents.Should().HaveCount(6);
        agents.Select(a => a.Name).Should().Contain(new[]
        {
            "ProductManager",
            "Architect",
            "Designer",
            "Developer",
            "Analyst",
            "Orchestrator"
        });
    }

    [Fact]
    public void GetAgent_WithValidId_ShouldReturnAgent()
    {
        // Arrange
        var registry = new AgentRegistry();

        // Act
        var agent = registry.GetAgent("developer");

        // Assert
        agent.Should().NotBeNull();
        agent!.AgentId.Should().Be("developer");
        agent.Name.Should().Be("Developer");
    }

    [Fact]
    public void GetAgent_WithInvalidId_ShouldReturnNull()
    {
        // Arrange
        var registry = new AgentRegistry();

        // Act
        var agent = registry.GetAgent("nonexistent");

        // Assert
        agent.Should().BeNull();
    }

    [Fact]
    public void GetAgentsByCapability_WithValidCapability_ShouldReturnMatchingAgents()
    {
        // Arrange
        var registry = new AgentRegistry();

        // Act
        var agents = registry.GetAgentsByCapability("create-architecture");

        // Assert
        agents.Should().NotBeNull();
        agents.Should().ContainSingle();
        agents.First().Name.Should().Be("Architect");
    }

    [Fact]
    public void GetAgentsByCapability_WithInvalidCapability_ShouldReturnEmptyList()
    {
        // Arrange
        var registry = new AgentRegistry();

        // Act
        var agents = registry.GetAgentsByCapability("nonexistent-capability");

        // Assert
        agents.Should().NotBeNull();
        agents.Should().BeEmpty();
    }

    [Fact]
    public void AgentDefinition_ShouldHaveAllRequiredProperties()
    {
        // Arrange
        var registry = new AgentRegistry();

        // Act
        var agent = registry.GetAgent("architect");

        // Assert
        agent.Should().NotBeNull();
        agent!.AgentId.Should().NotBeNullOrEmpty();
        agent.Name.Should().NotBeNullOrEmpty();
        agent.Description.Should().NotBeNullOrEmpty();
        agent.Capabilities.Should().NotBeNull().And.NotBeEmpty();
        agent.SystemPrompt.Should().NotBeNullOrEmpty();
        agent.ModelPreference.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void GetAllAgents_ShouldReturnAgentsWithUniqueIds()
    {
        // Arrange
        var registry = new AgentRegistry();

        // Act
        var agents = registry.GetAllAgents();

        // Assert
        var uniqueIds = agents.Select(a => a.AgentId).Distinct();
        uniqueIds.Should().HaveCount(agents.Count);
    }

    [Fact]
    public void GetAgentsByCapability_WithMultipleMatches_ShouldReturnAllMatching()
    {
        // Arrange
        var registry = new AgentRegistry();

        // Act
        var agents = registry.GetAgentsByCapability("code-implementation");

        // Assert
        agents.Should().NotBeNull();
        agents.Should().Contain(a => a.Name == "Developer");
    }
}
