using bmadServer.ApiService.Services.Workflows.Agents;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using FluentAssertions;

namespace bmadServer.Tests.Integration.Workflows;

public class AgentRegistryIntegrationTests
{
    private readonly IAgentRegistry _agentRegistry;

    public AgentRegistryIntegrationTests()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IAgentRegistry, AgentRegistry>();
        var serviceProvider = services.BuildServiceProvider();
        _agentRegistry = serviceProvider.GetRequiredService<IAgentRegistry>();
    }

    [Fact]
    public void AgentRegistry_ShouldBeRegisteredAsSingleton()
    {
        // Arrange & Act
        var agents = _agentRegistry.GetAllAgents();

        // Assert
        agents.Should().NotBeNull();
        agents.Should().HaveCount(6);
    }

    [Fact]
    public void AgentRegistry_ShouldProvideConsistentResults()
    {
        // Arrange & Act
        var firstCall = _agentRegistry.GetAllAgents();
        var secondCall = _agentRegistry.GetAllAgents();

        // Assert
        firstCall.Should().Equal(secondCall);
    }

    [Fact]
    public void GetAgentsByCapability_ShouldFindDeveloperForCodeImplementation()
    {
        // Arrange & Act
        var agents = _agentRegistry.GetAgentsByCapability("code-implementation");

        // Assert
        agents.Should().ContainSingle();
        agents.First().AgentId.Should().Be("developer");
        agents.First().ModelPreference.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void GetAgent_CaseInsensitive_ShouldWork()
    {
        // Arrange & Act
        var agent1 = _agentRegistry.GetAgent("DEVELOPER");
        var agent2 = _agentRegistry.GetAgent("developer");
        var agent3 = _agentRegistry.GetAgent("Developer");

        // Assert
        agent1.Should().NotBeNull();
        agent2.Should().NotBeNull();
        agent3.Should().NotBeNull();
        agent1!.AgentId.Should().Be(agent2!.AgentId).And.Be(agent3!.AgentId);
    }

    [Fact]
    public void AllAgents_ShouldHaveModelPreferences()
    {
        // Arrange & Act
        var agents = _agentRegistry.GetAllAgents();

        // Assert
        agents.Should().AllSatisfy(agent =>
        {
            agent.ModelPreference.Should().NotBeNullOrEmpty();
            agent.ModelPreference.Should().MatchRegex(@"gpt-\d|gpt-\d\.\d");
        });
    }

    [Fact]
    public void AllAgents_ShouldHaveAtLeastOneCapability()
    {
        // Arrange & Act
        var agents = _agentRegistry.GetAllAgents();

        // Assert
        agents.Should().AllSatisfy(agent =>
        {
            agent.Capabilities.Should().NotBeNull().And.NotBeEmpty();
        });
    }
}
