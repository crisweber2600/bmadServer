using bmadServer.ApiService.Services.Workflows.Agents;
using Xunit;
using FluentAssertions;
using Moq;
using Microsoft.Extensions.Logging;

namespace bmadServer.Tests.Services.Workflows.Agents;

public class HumanApprovalServiceTests
{
    private readonly Mock<ILogger<HumanApprovalService>> _mockLogger;
    private readonly HumanApprovalService _service;

    public HumanApprovalServiceTests()
    {
        _mockLogger = new Mock<ILogger<HumanApprovalService>>();
        _service = new HumanApprovalService(_mockLogger.Object);
    }

    [Fact]
    public void RequestApproval_LowConfidence_CreatesApprovalRequest()
    {
        // Arrange
        var request = new ApprovalRequest
        {
            RequestId = Guid.NewGuid(),
            WorkflowInstanceId = Guid.NewGuid(),
            AgentId = "architect",
            ProposedResponse = "Let's use microservices architecture",
            ConfidenceScore = 0.65,
            Reasoning = "Multiple architectures viable",
            RequestedAt = DateTime.UtcNow
        };

        // Act
        _service.RequestApproval(request);
        var pending = _service.GetPendingApprovals(request.WorkflowInstanceId);

        // Assert
        pending.Should().ContainSingle();
        pending.First().ConfidenceScore.Should().Be(0.65);
    }

    [Fact]
    public void Approve_ValidRequest_MarksApproved()
    {
        // Arrange
        var request = new ApprovalRequest
        {
            RequestId = Guid.NewGuid(),
            WorkflowInstanceId = Guid.NewGuid(),
            AgentId = "developer",
            ProposedResponse = "Use JWT for auth",
            ConfidenceScore = 0.68,
            Reasoning = "Standard approach",
            RequestedAt = DateTime.UtcNow
        };

        _service.RequestApproval(request);
        var userId = Guid.NewGuid();

        // Act
        var result = _service.Approve(request.RequestId, userId);

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be(ApprovalStatus.Approved);
        result.ApprovedBy.Should().Be(userId);
        result.FinalResponse.Should().Be("Use JWT for auth");
    }

    [Fact]
    public void Modify_ValidRequest_UsesModifiedVersion()
    {
        // Arrange
        var request = new ApprovalRequest
        {
            RequestId = Guid.NewGuid(),
            WorkflowInstanceId = Guid.NewGuid(),
            AgentId = "architect",
            ProposedResponse = "Use REST API",
            ConfidenceScore = 0.65,
            Reasoning = "Simple approach",
            RequestedAt = DateTime.UtcNow
        };

        _service.RequestApproval(request);
        var userId = Guid.NewGuid();
        var modifiedResponse = "Use GraphQL API with REST fallback";

        // Act
        var result = _service.Modify(request.RequestId, modifiedResponse, userId);

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be(ApprovalStatus.Modified);
        result.ApprovedBy.Should().Be(userId);
        result.OriginalResponse.Should().Be("Use REST API");
        result.FinalResponse.Should().Be(modifiedResponse);
    }

    [Fact]
    public void Reject_ValidRequest_MarksRejected()
    {
        // Arrange
        var request = new ApprovalRequest
        {
            RequestId = Guid.NewGuid(),
            WorkflowInstanceId = Guid.NewGuid(),
            AgentId = "developer",
            ProposedResponse = "Skip testing",
            ConfidenceScore = 0.55,
            Reasoning = "Save time",
            RequestedAt = DateTime.UtcNow
        };

        _service.RequestApproval(request);
        var userId = Guid.NewGuid();
        var rejectionReason = "Testing is critical for quality";

        // Act
        var result = _service.Reject(request.RequestId, rejectionReason, userId);

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be(ApprovalStatus.Rejected);
        result.ApprovedBy.Should().Be(userId);
        result.RejectionReason.Should().Be(rejectionReason);
    }

    [Fact]
    public void GetPendingApprovals_OnlyReturnsPending()
    {
        // Arrange
        var workflowId = Guid.NewGuid();
        
        var request1 = new ApprovalRequest
        {
            RequestId = Guid.NewGuid(),
            WorkflowInstanceId = workflowId,
            AgentId = "agent1",
            ProposedResponse = "Response 1",
            ConfidenceScore = 0.6,
            Reasoning = "Reason 1",
            RequestedAt = DateTime.UtcNow
        };

        var request2 = new ApprovalRequest
        {
            RequestId = Guid.NewGuid(),
            WorkflowInstanceId = workflowId,
            AgentId = "agent2",
            ProposedResponse = "Response 2",
            ConfidenceScore = 0.65,
            Reasoning = "Reason 2",
            RequestedAt = DateTime.UtcNow
        };

        _service.RequestApproval(request1);
        _service.RequestApproval(request2);
        _service.Approve(request1.RequestId, Guid.NewGuid());

        // Act
        var pending = _service.GetPendingApprovals(workflowId);

        // Assert
        pending.Should().ContainSingle();
        pending.First().RequestId.Should().Be(request2.RequestId);
    }

    [Fact]
    public void IsApprovalNeeded_HighConfidence_ReturnsFalse()
    {
        // Arrange
        var confidenceScore = 0.85;

        // Act
        var needed = _service.IsApprovalNeeded(confidenceScore);

        // Assert
        needed.Should().BeFalse();
    }

    [Fact]
    public void IsApprovalNeeded_LowConfidence_ReturnsTrue()
    {
        // Arrange
        var confidenceScore = 0.65;

        // Act
        var needed = _service.IsApprovalNeeded(confidenceScore);

        // Assert
        needed.Should().BeTrue();
    }

    [Fact]
    public void GetApprovalHistory_ReturnsAllDecisions()
    {
        // Arrange
        var workflowId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var request = new ApprovalRequest
        {
            RequestId = Guid.NewGuid(),
            WorkflowInstanceId = workflowId,
            AgentId = "agent1",
            ProposedResponse = "Response 1",
            ConfidenceScore = 0.6,
            Reasoning = "Reason 1",
            RequestedAt = DateTime.UtcNow
        };

        _service.RequestApproval(request);
        _service.Approve(request.RequestId, userId);

        // Act
        var history = _service.GetApprovalHistory(workflowId);

        // Assert
        history.Should().ContainSingle();
        history.First().Status.Should().Be(ApprovalStatus.Approved);
    }
}
