using bmadServer.ApiService.Services.Workflows.Agents;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;
using FluentAssertions;

namespace bmadServer.Tests.Integration.Workflows;

public class AgentMessagingIntegrationTests
{
    private readonly IAgentMessaging _messaging;
    private readonly IAgentRegistry _registry;

    public AgentMessagingIntegrationTests()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IAgentRegistry, AgentRegistry>();
        services.AddSingleton<IAgentMessaging, AgentMessaging>();
        
        var serviceProvider = services.BuildServiceProvider();
        _messaging = serviceProvider.GetRequiredService<IAgentMessaging>();
        _registry = serviceProvider.GetRequiredService<IAgentRegistry>();
    }

    [Fact]
    public async Task RequestFromAgent_EndToEnd_Success()
    {
        // Arrange
        var workflowId = Guid.NewGuid();
        var context = new AgentMessageContext
        {
            WorkflowInstanceId = workflowId,
            SourceAgentId = "architect",
            ConversationHistory = new List<string>()
        };

        // Act
        var result = await _messaging.RequestFromAgent("developer", "Implement feature", context);

        // Assert
        result.Success.Should().BeTrue();
        result.Response.Should().Contain("Developer");
        result.WorkflowInstanceId.Should().Be(workflowId);
    }

    [Fact]
    public async Task GetMessageHistory_AfterMultipleMessages_ReturnsAll()
    {
        // Arrange
        var workflowId = Guid.NewGuid();
        var context = new AgentMessageContext
        {
            WorkflowInstanceId = workflowId,
            SourceAgentId = "architect",
            ConversationHistory = new List<string>()
        };

        await _messaging.RequestFromAgent("developer", "Request 1", context);
        await _messaging.RequestFromAgent("designer", "Request 2", context);

        // Act
        var history = _messaging.GetMessageHistory(workflowId);

        // Assert
        history.Should().HaveCount(2);
        history.Should().Contain(m => m.TargetAgent == "developer");
        history.Should().Contain(m => m.TargetAgent == "designer");
    }

    [Fact]
    public async Task RequestFromAgent_WithAllBmadAgents_AllSucceed()
    {
        // Arrange
        var agents = _registry.GetAllAgents();
        var workflowId = Guid.NewGuid();
        var context = new AgentMessageContext
        {
            WorkflowInstanceId = workflowId,
            SourceAgentId = "orchestrator",
            ConversationHistory = new List<string>()
        };

        // Act & Assert
        foreach (var agent in agents)
        {
            var result = await _messaging.RequestFromAgent(agent.AgentId, $"Test request to {agent.Name}", context);
            result.Success.Should().BeTrue($"Request to {agent.Name} should succeed");
            result.TargetAgent.Should().Be(agent.AgentId);
        }
    }

    [Fact]
    public async Task MessageHistory_AcrossWorkflows_IsIsolated()
    {
        // Arrange
        var workflow1 = Guid.NewGuid();
        var workflow2 = Guid.NewGuid();
        
        var context1 = new AgentMessageContext
        {
            WorkflowInstanceId = workflow1,
            SourceAgentId = "architect",
            ConversationHistory = new List<string>()
        };
        
        var context2 = new AgentMessageContext
        {
            WorkflowInstanceId = workflow2,
            SourceAgentId = "architect",
            ConversationHistory = new List<string>()
        };

        await _messaging.RequestFromAgent("developer", "Workflow 1 request", context1);
        await _messaging.RequestFromAgent("developer", "Workflow 2 request", context2);

        // Act
        var history1 = _messaging.GetMessageHistory(workflow1);
        var history2 = _messaging.GetMessageHistory(workflow2);

        // Assert
        history1.Should().HaveCount(1);
        history2.Should().HaveCount(1);
        history1.First().Content.Should().Be("Workflow 1 request");
        history2.First().Content.Should().Be("Workflow 2 request");
    }
}
