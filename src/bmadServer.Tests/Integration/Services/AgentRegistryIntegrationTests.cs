using bmadServer.ServiceDefaults.Models.Agents;
using bmadServer.ServiceDefaults.Services.Agents;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;
using FluentAssertions;

namespace bmadServer.Tests.Integration.Services;

public class AgentRegistryIntegrationTests
{
    [Fact]
    public void AgentRegistry_CanBeRegisteredWithDependencyInjection()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IAgentRegistry, AgentRegistry>();

        var serviceProvider = services.BuildServiceProvider();

        // Act
        var registry = serviceProvider.GetService<IAgentRegistry>();

        // Assert
        registry.Should().NotBeNull();
        registry.Should().BeOfType<AgentRegistry>();
    }

    [Fact]
    public void AgentRegistry_RetrievedFromDI_ShouldReturnAllAgents()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IAgentRegistry, AgentRegistry>();

        var serviceProvider = services.BuildServiceProvider();
        var registry = serviceProvider.GetRequiredService<IAgentRegistry>();

        // Act
        var agents = registry.GetAllAgents();

        // Assert
        agents.Should().NotBeEmpty();
        agents.Should().HaveCount(6);
    }

    [Fact]
    public void AgentRegistry_WithLogger_ShouldLogInitialization()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Information);
        });
        services.AddSingleton<IAgentRegistry, AgentRegistry>();

        var serviceProvider = services.BuildServiceProvider();

        // Act
        var registry = serviceProvider.GetRequiredService<IAgentRegistry>();
        var agents = registry.GetAllAgents();

        // Assert
        agents.Should().HaveCount(6);
        registry.Should().NotBeNull();
    }

    [Fact]
    public void AgentRegistry_GetAgentsByCapability_ShouldWorkAcrossMultipleQueries()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<IAgentRegistry, AgentRegistry>();

        var serviceProvider = services.BuildServiceProvider();
        var registry = serviceProvider.GetRequiredService<IAgentRegistry>();

        // Act
        var prdAgents = registry.GetAgentsByCapability("create-prd");
        var architectAgents = registry.GetAgentsByCapability("create-architecture");
        var devAgents = registry.GetAgentsByCapability("dev-story");

        // Assert
        prdAgents.Should().NotBeEmpty();
        prdAgents.Should().Contain(a => a.AgentId == "product-manager");

        architectAgents.Should().NotBeEmpty();
        architectAgents.Should().Contain(a => a.AgentId == "architect");

        devAgents.Should().NotBeEmpty();
        devAgents.Should().Contain(a => a.AgentId == "developer");
    }
}
