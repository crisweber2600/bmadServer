using bmadServer.ApiService.Models.Workflows;
using Xunit;
using FluentAssertions;

namespace bmadServer.Tests.Unit.Services.Workflows;

public class WorkflowStateMachineTests
{
    [Theory]
    [InlineData(WorkflowStatus.Created, WorkflowStatus.Running, true)]
    [InlineData(WorkflowStatus.Running, WorkflowStatus.Paused, true)]
    [InlineData(WorkflowStatus.Running, WorkflowStatus.WaitingForInput, true)]
    [InlineData(WorkflowStatus.Running, WorkflowStatus.WaitingForApproval, true)]
    [InlineData(WorkflowStatus.Running, WorkflowStatus.Completed, true)]
    [InlineData(WorkflowStatus.Running, WorkflowStatus.Failed, true)]
    [InlineData(WorkflowStatus.Paused, WorkflowStatus.Running, true)]
    [InlineData(WorkflowStatus.Paused, WorkflowStatus.Cancelled, true)]
    [InlineData(WorkflowStatus.WaitingForInput, WorkflowStatus.Running, true)]
    [InlineData(WorkflowStatus.WaitingForInput, WorkflowStatus.Cancelled, true)]
    [InlineData(WorkflowStatus.WaitingForApproval, WorkflowStatus.Running, true)]
    [InlineData(WorkflowStatus.WaitingForApproval, WorkflowStatus.Cancelled, true)]
    public void ValidateTransition_WithValidTransitions_ShouldReturnTrue(
        WorkflowStatus from, WorkflowStatus to, bool expected)
    {
        // Act
        var result = WorkflowStatusExtensions.ValidateTransition(from, to);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(WorkflowStatus.Created, WorkflowStatus.Completed)]
    [InlineData(WorkflowStatus.Created, WorkflowStatus.Paused)]
    [InlineData(WorkflowStatus.Completed, WorkflowStatus.Running)]
    [InlineData(WorkflowStatus.Failed, WorkflowStatus.Running)]
    [InlineData(WorkflowStatus.Cancelled, WorkflowStatus.Running)]
    [InlineData(WorkflowStatus.Running, WorkflowStatus.Created)]
    public void ValidateTransition_WithInvalidTransitions_ShouldReturnFalse(
        WorkflowStatus from, WorkflowStatus to)
    {
        // Act
        var result = WorkflowStatusExtensions.ValidateTransition(from, to);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsTerminal_WithTerminalStates_ShouldReturnTrue()
    {
        // Assert
        WorkflowStatus.Completed.IsTerminal().Should().BeTrue();
        WorkflowStatus.Failed.IsTerminal().Should().BeTrue();
        WorkflowStatus.Cancelled.IsTerminal().Should().BeTrue();
    }

    [Fact]
    public void IsTerminal_WithNonTerminalStates_ShouldReturnFalse()
    {
        // Assert
        WorkflowStatus.Created.IsTerminal().Should().BeFalse();
        WorkflowStatus.Running.IsTerminal().Should().BeFalse();
        WorkflowStatus.Paused.IsTerminal().Should().BeFalse();
        WorkflowStatus.WaitingForInput.IsTerminal().Should().BeFalse();
        WorkflowStatus.WaitingForApproval.IsTerminal().Should().BeFalse();
    }
}
