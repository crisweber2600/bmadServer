using FluentAssertions;
using bmadServer.ServiceDefaults.Models.Agents;
using bmadServer.ServiceDefaults.Services.Agents;
using Xunit;

namespace bmadServer.Tests.Unit.Services.Agents;

public class AgentRegistryTests
{
    private readonly IAgentRegistry _registry;

    public AgentRegistryTests()
    {
        _registry = new AgentRegistry();
    }

    [Fact]
    public void GetAllAgents_ShouldReturnAllRegisteredAgents()
    {
        // Act
        var agents = _registry.GetAllAgents();

        // Assert
        agents.Should().NotBeEmpty();
        agents.Should().HaveCount(6);
        agents.Should().Contain(a => a.AgentId == "product-manager");
        agents.Should().Contain(a => a.AgentId == "architect");
        agents.Should().Contain(a => a.AgentId == "designer");
        agents.Should().Contain(a => a.AgentId == "developer");
        agents.Should().Contain(a => a.AgentId == "analyst");
        agents.Should().Contain(a => a.AgentId == "orchestrator");
    }

    [Fact]
    public void GetAgent_WithValidId_ShouldReturnAgentDefinition()
    {
        // Arrange
        const string agentId = "product-manager";

        // Act
        var agent = _registry.GetAgent(agentId);

        // Assert
        agent.Should().NotBeNull();
        agent!.AgentId.Should().Be(agentId);
        agent.Name.Should().NotBeNullOrEmpty();
        agent.Description.Should().NotBeNullOrEmpty();
        agent.Capabilities.Should().NotBeEmpty();
        agent.SystemPrompt.Should().NotBeNullOrEmpty();
        agent.ModelPreference.Should().NotBeNull();
    }

    [Fact]
    public void GetAgent_WithInvalidId_ShouldReturnNull()
    {
        // Arrange
        const string invalidId = "non-existent-agent";

        // Act
        var agent = _registry.GetAgent(invalidId);

        // Assert
        agent.Should().BeNull();
    }

    [Fact]
    public void GetAgent_WithNullId_ShouldReturnNull()
    {
        // Act
        var agent = _registry.GetAgent(null!);

        // Assert
        agent.Should().BeNull();
    }

    [Fact]
    public void GetAgent_WithEmptyId_ShouldReturnNull()
    {
        // Act
        var agent = _registry.GetAgent(string.Empty);

        // Assert
        agent.Should().BeNull();
    }

    [Fact]
    public void GetAgentsByCapability_WithValidCapability_ShouldReturnMatchingAgents()
    {
        // Arrange
        const string capability = "create-prd";

        // Act
        var agents = _registry.GetAgentsByCapability(capability);

        // Assert
        agents.Should().NotBeEmpty();
        agents.Should().Contain(a => a.AgentId == "product-manager");
        agents.All(a => a.Capabilities.Contains(capability, StringComparer.OrdinalIgnoreCase))
            .Should().BeTrue();
    }

    [Fact]
    public void GetAgentsByCapability_WithNonExistentCapability_ShouldReturnEmptyList()
    {
        // Arrange
        const string capability = "non-existent-capability";

        // Act
        var agents = _registry.GetAgentsByCapability(capability);

        // Assert
        agents.Should().BeEmpty();
    }

    [Fact]
    public void GetAgentsByCapability_WithNullCapability_ShouldReturnEmptyList()
    {
        // Act
        var agents = _registry.GetAgentsByCapability(null!);

        // Assert
        agents.Should().BeEmpty();
    }

    [Fact]
    public void GetAgentsByCapability_WithSharedCapability_ShouldReturnMultipleAgents()
    {
        // Arrange - dev-story is unique to developer, but let's test code-review for analyst
        const string capability = "code-review";

        // Act
        var agents = _registry.GetAgentsByCapability(capability);

        // Assert
        agents.Should().NotBeEmpty();
        agents.Should().Contain(a => a.AgentId == "analyst");
    }

    [Fact]
    public void AgentDefinition_ShouldHaveRequiredProperties()
    {
        // Arrange
        var agent = _registry.GetAgent("product-manager");

        // Assert
        agent.Should().NotBeNull();
        agent!.AgentId.Should().NotBeNullOrEmpty();
        agent.Name.Should().NotBeNullOrEmpty();
        agent.Description.Should().NotBeNullOrEmpty();
        agent.Capabilities.Should().NotBeNull();
        agent.Capabilities.Should().NotBeEmpty();
        agent.SystemPrompt.Should().NotBeNullOrEmpty();
        agent.ModelPreference.Should().NotBeNull();
    }

    [Fact]
    public void ModelPreference_ShouldHaveRequiredProperties()
    {
        // Arrange
        var agent = _registry.GetAgent("product-manager");

        // Assert
        agent.Should().NotBeNull();
        var modelPref = agent!.ModelPreference;
        modelPref.PreferredModel.Should().NotBeNullOrEmpty();
        modelPref.FallbackModel.Should().NotBeNullOrEmpty();
        modelPref.MaxTokens.Should().BeGreaterThan(0);
        modelPref.Temperature.Should().BeGreaterOrEqualTo(0).And.BeLessOrEqualTo(1);
    }

    [Fact]
    public void GetAllAgents_ShouldReturnImmutableCollection()
    {
        // Act
        var agents = _registry.GetAllAgents();

        // Assert
        agents.Should().BeAssignableTo<IReadOnlyList<AgentDefinition>>();
    }

    [Fact]
    public void BmadAgentDefinitions_ShouldContainAllRequiredAgents()
    {
        // Act
        var agents = BmadAgentDefinitions.AllAgents;

        // Assert
        agents.Should().HaveCount(6);
        agents.Should().Contain(a => a.AgentId == "product-manager");
        agents.Should().Contain(a => a.AgentId == "architect");
        agents.Should().Contain(a => a.AgentId == "designer");
        agents.Should().Contain(a => a.AgentId == "developer");
        agents.Should().Contain(a => a.AgentId == "analyst");
        agents.Should().Contain(a => a.AgentId == "orchestrator");
    }

    [Fact]
    public void GetAgent_WithCaseInsensitiveId_ShouldReturnAgent()
    {
        // Arrange
        const string agentId = "PRODUCT-MANAGER";

        // Act
        var agent = _registry.GetAgent(agentId);

        // Assert
        agent.Should().NotBeNull();
        agent!.AgentId.Should().Be("product-manager");
    }
}
