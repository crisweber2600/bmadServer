using bmadServer.ApiService.Services.Workflows.Agents;
using Xunit;
using FluentAssertions;
using Moq;
using Microsoft.Extensions.Logging;

namespace bmadServer.Tests.Services.Workflows.Agents;

public class AgentHandoffTrackerTests
{
    private readonly Mock<ILogger<AgentHandoffTracker>> _mockLogger;
    private readonly AgentHandoffTracker _tracker;

    public AgentHandoffTrackerTests()
    {
        _mockLogger = new Mock<ILogger<AgentHandoffTracker>>();
        _tracker = new AgentHandoffTracker(_mockLogger.Object);
    }

    [Fact]
    public void RecordHandoff_ValidHandoff_StoresInAuditLog()
    {
        // Arrange
        var workflowInstanceId = Guid.NewGuid();
        var handoff = new AgentHandoff
        {
            HandoffId = Guid.NewGuid(),
            WorkflowInstanceId = workflowInstanceId,
            FromAgent = "architect",
            ToAgent = "developer",
            WorkflowStep = "implementation",
            Reason = "Architecture complete",
            Timestamp = DateTime.UtcNow
        };

        // Act
        _tracker.RecordHandoff(handoff);
        var history = _tracker.GetHandoffHistory(workflowInstanceId);

        // Assert
        history.Should().ContainSingle();
        history.First().FromAgent.Should().Be("architect");
        history.First().ToAgent.Should().Be("developer");
    }

    [Fact]
    public void GetHandoffHistory_MultipleHandoffs_ReturnsInChronologicalOrder()
    {
        // Arrange
        var workflowInstanceId = Guid.NewGuid();
        
        _tracker.RecordHandoff(new AgentHandoff
        {
            HandoffId = Guid.NewGuid(),
            WorkflowInstanceId = workflowInstanceId,
            FromAgent = "pm",
            ToAgent = "architect",
            WorkflowStep = "design",
            Reason = "Requirements complete",
            Timestamp = DateTime.UtcNow.AddMinutes(-2)
        });

        _tracker.RecordHandoff(new AgentHandoff
        {
            HandoffId = Guid.NewGuid(),
            WorkflowInstanceId = workflowInstanceId,
            FromAgent = "architect",
            ToAgent = "developer",
            WorkflowStep = "implementation",
            Reason = "Design complete",
            Timestamp = DateTime.UtcNow.AddMinutes(-1)
        });

        // Act
        var history = _tracker.GetHandoffHistory(workflowInstanceId);

        // Assert
        history.Should().HaveCount(2);
        history[0].FromAgent.Should().Be("pm");
        history[1].FromAgent.Should().Be("architect");
    }

    [Fact]
    public void GetCurrentAgent_AfterHandoff_ReturnsLatestAgent()
    {
        // Arrange
        var workflowInstanceId = Guid.NewGuid();
        
        _tracker.RecordHandoff(new AgentHandoff
        {
            HandoffId = Guid.NewGuid(),
            WorkflowInstanceId = workflowInstanceId,
            FromAgent = "architect",
            ToAgent = "developer",
            WorkflowStep = "implementation",
            Reason = "Design complete",
            Timestamp = DateTime.UtcNow
        });

        // Act
        var currentAgent = _tracker.GetCurrentAgent(workflowInstanceId);

        // Assert
        currentAgent.Should().Be("developer");
    }

    [Fact]
    public void GetCurrentAgent_NoHandoffs_ReturnsNull()
    {
        // Arrange
        var workflowInstanceId = Guid.NewGuid();

        // Act
        var currentAgent = _tracker.GetCurrentAgent(workflowInstanceId);

        // Assert
        currentAgent.Should().BeNull();
    }

    [Fact]
    public void RecordHandoff_LogsHandoff()
    {
        // Arrange
        var handoff = new AgentHandoff
        {
            HandoffId = Guid.NewGuid(),
            WorkflowInstanceId = Guid.NewGuid(),
            FromAgent = "architect",
            ToAgent = "developer",
            WorkflowStep = "implementation",
            Reason = "Design complete",
            Timestamp = DateTime.UtcNow
        };

        // Act
        _tracker.RecordHandoff(handoff);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("handoff")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void GetHandoffHistory_IsolatesWorkflows()
    {
        // Arrange
        var workflow1 = Guid.NewGuid();
        var workflow2 = Guid.NewGuid();

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
            ToAgent = "designer",
            WorkflowStep = "ui-design",
            Reason = "Start",
            Timestamp = DateTime.UtcNow
        });

        // Act
        var history1 = _tracker.GetHandoffHistory(workflow1);
        var history2 = _tracker.GetHandoffHistory(workflow2);

        // Assert
        history1.Should().ContainSingle();
        history2.Should().ContainSingle();
        history1.First().ToAgent.Should().Be("architect");
        history2.First().ToAgent.Should().Be("designer");
    }
}
