using bmadServer.ApiService.Services.Workflows.Agents;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using FluentAssertions;

namespace bmadServer.Tests.Integration.Workflows;

public class AgentHandoffIntegrationTests
{
    private readonly IAgentHandoffTracker _tracker;
    private readonly IAgentRegistry _registry;

    public AgentHandoffIntegrationTests()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IAgentRegistry, AgentRegistry>();
        services.AddSingleton<IAgentHandoffTracker, AgentHandoffTracker>();
        
        var serviceProvider = services.BuildServiceProvider();
        _tracker = serviceProvider.GetRequiredService<IAgentHandoffTracker>();
        _registry = serviceProvider.GetRequiredService<IAgentRegistry>();
    }

    [Fact]
    public void EndToEnd_CompleteWorkflowHandoffs_TracksCorrectly()
    {
        // Arrange
        var workflowId = Guid.NewGuid();
        var agents = _registry.GetAllAgents();

        // Act - Simulate workflow progression
        _tracker.RecordHandoff(new AgentHandoff
        {
            HandoffId = Guid.NewGuid(),
            WorkflowInstanceId = workflowId,
            FromAgent = "orchestrator",
            ToAgent = "product-manager",
            WorkflowStep = "requirements",
            Reason = "Start workflow",
            Timestamp = DateTime.UtcNow.AddMinutes(-5)
        });

        _tracker.RecordHandoff(new AgentHandoff
        {
            HandoffId = Guid.NewGuid(),
            WorkflowInstanceId = workflowId,
            FromAgent = "product-manager",
            ToAgent = "architect",
            WorkflowStep = "design",
            Reason = "Requirements complete",
            Timestamp = DateTime.UtcNow.AddMinutes(-4)
        });

        _tracker.RecordHandoff(new AgentHandoff
        {
            HandoffId = Guid.NewGuid(),
            WorkflowInstanceId = workflowId,
            FromAgent = "architect",
            ToAgent = "developer",
            WorkflowStep = "implementation",
            Reason = "Design approved",
            Timestamp = DateTime.UtcNow.AddMinutes(-3)
        });

        // Assert
        var history = _tracker.GetHandoffHistory(workflowId);
        history.Should().HaveCount(3);
        history[0].ToAgent.Should().Be("product-manager");
        history[1].ToAgent.Should().Be("architect");
        history[2].ToAgent.Should().Be("developer");

        var currentAgent = _tracker.GetCurrentAgent(workflowId);
        currentAgent.Should().Be("developer");
    }

    [Fact]
    public void MultipleWorkflows_IsolateHandoffs()
    {
        // Arrange
        var workflow1 = Guid.NewGuid();
        var workflow2 = Guid.NewGuid();

        // Act
        _tracker.RecordHandoff(new AgentHandoff
        {
            HandoffId = Guid.NewGuid(),
            WorkflowInstanceId = workflow1,
            FromAgent = "pm",
            ToAgent = "architect",
            WorkflowStep = "design",
            Reason = "Start",
            Timestamp = DateTime.UtcNow
        });

        _tracker.RecordHandoff(new AgentHandoff
        {
            HandoffId = Guid.NewGuid(),
            WorkflowInstanceId = workflow2,
            FromAgent = "pm",
            ToAgent = "developer",
            WorkflowStep = "code",
            Reason = "Fast track",
            Timestamp = DateTime.UtcNow
        });

        // Assert
        var history1 = _tracker.GetHandoffHistory(workflow1);
        var history2 = _tracker.GetHandoffHistory(workflow2);

        history1.Should().ContainSingle();
        history2.Should().ContainSingle();
        
        _tracker.GetCurrentAgent(workflow1).Should().Be("architect");
        _tracker.GetCurrentAgent(workflow2).Should().Be("developer");
    }
}
