using bmadServer.ApiService.Services.Workflows.Agents;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using FluentAssertions;

namespace bmadServer.Tests.Integration.Workflows;

public class HumanApprovalIntegrationTests
{
    private readonly IHumanApprovalService _approvalService;

    public HumanApprovalIntegrationTests()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IHumanApprovalService, HumanApprovalService>();
        
        var serviceProvider = services.BuildServiceProvider();
        _approvalService = serviceProvider.GetRequiredService<IHumanApprovalService>();
    }

    [Fact]
    public void EndToEnd_ApprovalWorkflow_WorksCorrectly()
    {
        // Arrange
        var workflowId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var request = new ApprovalRequest
        {
            RequestId = Guid.NewGuid(),
            WorkflowInstanceId = workflowId,
            AgentId = "architect",
            ProposedResponse = "Use monolithic architecture",
            ConfidenceScore = 0.62,
            Reasoning = "Simplicity for MVP",
            RequestedAt = DateTime.UtcNow
        };

        // Act & Assert - Request approval
        _approvalService.RequestApproval(request);
        var pending = _approvalService.GetPendingApprovals(workflowId);
        pending.Should().ContainSingle();

        // Act & Assert - Approve
        var decision = _approvalService.Approve(request.RequestId, userId);
        decision.Status.Should().Be(ApprovalStatus.Approved);
        decision.FinalResponse.Should().Be("Use monolithic architecture");

        // Act & Assert - Check history
        var history = _approvalService.GetApprovalHistory(workflowId);
        history.Should().ContainSingle();
        history.First().ApprovedBy.Should().Be(userId);

        // Act & Assert - Pending should be empty
        pending = _approvalService.GetPendingApprovals(workflowId);
        pending.Should().BeEmpty();
    }

    [Fact]
    public void MultipleApprovals_InSameWorkflow_TrackedSeparately()
    {
        // Arrange
        var workflowId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var request1 = new ApprovalRequest
        {
            RequestId = Guid.NewGuid(),
            WorkflowInstanceId = workflowId,
            AgentId = "architect",
            ProposedResponse = "Response 1",
            ConfidenceScore = 0.6,
            Reasoning = "Reason 1",
            RequestedAt = DateTime.UtcNow.AddMinutes(-2)
        };

        var request2 = new ApprovalRequest
        {
            RequestId = Guid.NewGuid(),
            WorkflowInstanceId = workflowId,
            AgentId = "developer",
            ProposedResponse = "Response 2",
            ConfidenceScore = 0.65,
            Reasoning = "Reason 2",
            RequestedAt = DateTime.UtcNow.AddMinutes(-1)
        };

        // Act
        _approvalService.RequestApproval(request1);
        _approvalService.RequestApproval(request2);
        
        _approvalService.Approve(request1.RequestId, userId);
        _approvalService.Modify(request2.RequestId, "Modified Response 2", userId);

        // Assert
        var history = _approvalService.GetApprovalHistory(workflowId);
        history.Should().HaveCount(2);
        history[0].Status.Should().Be(ApprovalStatus.Approved);
        history[1].Status.Should().Be(ApprovalStatus.Modified);
    }

    [Fact]
    public void ConfidenceThreshold_CorrectlyDeterminesApprovalNeed()
    {
        // Assert
        _approvalService.IsApprovalNeeded(0.85).Should().BeFalse(); // High confidence
        _approvalService.IsApprovalNeeded(0.70).Should().BeFalse(); // At threshold
        _approvalService.IsApprovalNeeded(0.69).Should().BeTrue();  // Below threshold
        _approvalService.IsApprovalNeeded(0.50).Should().BeTrue();  // Low confidence
    }
}
