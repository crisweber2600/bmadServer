using bmadServer.ApiService.Models.Agents;
using bmadServer.ApiService.Services.Workflows.Agents;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace bmadServer.Tests.Unit.Services.Workflows.Agents;

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
    public async Task CreateApprovalRequestAsync_CreatesRequest()
    {
        // Arrange
        var workflowId = Guid.NewGuid();

        // Act
        var request = await _service.CreateApprovalRequestAsync(
            workflowId,
            "architect",
            "Use microservices architecture",
            0.65,
            "Based on scalability requirements");

        // Assert
        Assert.NotNull(request);
        Assert.Equal(workflowId, request.WorkflowInstanceId);
        Assert.Equal("architect", request.AgentId);
        Assert.Equal(0.65, request.ConfidenceScore);
        Assert.Equal(ApprovalStatus.Pending, request.Status);
    }

    [Fact]
    public async Task GetApprovalRequestAsync_ReturnsCreatedRequest()
    {
        // Arrange
        var workflowId = Guid.NewGuid();
        var created = await _service.CreateApprovalRequestAsync(
            workflowId,
            "developer",
            "Implement feature X",
            0.6,
            "Standard implementation");

        // Act
        var retrieved = await _service.GetApprovalRequestAsync(created.ApprovalRequestId);

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal(created.ApprovalRequestId, retrieved.ApprovalRequestId);
    }

    [Fact]
    public async Task ApproveAsync_ApprovesRequest()
    {
        // Arrange
        var workflowId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var request = await _service.CreateApprovalRequestAsync(
            workflowId,
            "designer",
            "Use dark theme",
            0.68,
            "User preference data suggests dark theme");

        // Act
        var result = await _service.ApproveAsync(request.ApprovalRequestId, userId);

        // Assert
        Assert.True(result);

        var updated = await _service.GetApprovalRequestAsync(request.ApprovalRequestId);
        Assert.NotNull(updated);
        Assert.Equal(ApprovalStatus.Approved, updated.Status);
        Assert.Equal(userId, updated.RespondedByUserId);
        Assert.NotNull(updated.RespondedAt);
        Assert.Equal(updated.ProposedResponse, updated.ApprovedResponse);
    }

    [Fact]
    public async Task ModifyAndApproveAsync_ModifiesAndApproves()
    {
        // Arrange
        var workflowId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var request = await _service.CreateApprovalRequestAsync(
            workflowId,
            "analyst",
            "Original analysis",
            0.5,
            "Limited data available");

        // Act
        var result = await _service.ModifyAndApproveAsync(
            request.ApprovalRequestId,
            userId,
            "Modified analysis with additional insights");

        // Assert
        Assert.True(result);

        var updated = await _service.GetApprovalRequestAsync(request.ApprovalRequestId);
        Assert.NotNull(updated);
        Assert.Equal(ApprovalStatus.Modified, updated.Status);
        Assert.Equal(userId, updated.RespondedByUserId);
        Assert.Equal("Modified analysis with additional insights", updated.ApprovedResponse);
    }

    [Fact]
    public async Task RejectAsync_RejectsRequest()
    {
        // Arrange
        var workflowId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var request = await _service.CreateApprovalRequestAsync(
            workflowId,
            "orchestrator",
            "Proceed with deployment",
            0.55,
            "Tests passed");

        // Act
        var result = await _service.RejectAsync(
            request.ApprovalRequestId,
            userId,
            "Need more testing",
            "Run performance tests first");

        // Assert
        Assert.True(result);

        var updated = await _service.GetApprovalRequestAsync(request.ApprovalRequestId);
        Assert.NotNull(updated);
        Assert.Equal(ApprovalStatus.Rejected, updated.Status);
        Assert.Equal(userId, updated.RespondedByUserId);
        Assert.Equal("Need more testing", updated.RejectionReason);
        Assert.Equal("Run performance tests first", updated.AdditionalGuidance);
    }

    [Fact]
    public async Task GetPendingApprovalsAsync_ReturnsPendingOnly()
    {
        // Arrange
        var workflowId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var request1 = await _service.CreateApprovalRequestAsync(workflowId, "a1", "r1", 0.6, "reason");
        var request2 = await _service.CreateApprovalRequestAsync(workflowId, "a2", "r2", 0.5, "reason");
        var request3 = await _service.CreateApprovalRequestAsync(workflowId, "a3", "r3", 0.4, "reason");

        await _service.ApproveAsync(request1.ApprovalRequestId, userId);

        // Act
        var pending = await _service.GetPendingApprovalsAsync(workflowId);
        var pendingList = pending.ToList();

        // Assert
        Assert.Equal(2, pendingList.Count);
        Assert.Contains(pendingList, r => r.ApprovalRequestId == request2.ApprovalRequestId);
        Assert.Contains(pendingList, r => r.ApprovalRequestId == request3.ApprovalRequestId);
    }

    [Fact]
    public async Task ApproveAsync_WithNonPendingRequest_ReturnsFalse()
    {
        // Arrange
        var workflowId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var request = await _service.CreateApprovalRequestAsync(workflowId, "agent", "response", 0.6, "reason");

        await _service.ApproveAsync(request.ApprovalRequestId, userId);

        // Act - Try to approve again
        var result = await _service.ApproveAsync(request.ApprovalRequestId, Guid.NewGuid());

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ApproveAsync_WithNonExistentRequest_ReturnsFalse()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var result = await _service.ApproveAsync(Guid.NewGuid(), userId);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task CheckTimeoutsAsync_IdentifiesTimedOutRequests()
    {
        // Arrange
        var workflowId = Guid.NewGuid();
        var request = await _service.CreateApprovalRequestAsync(workflowId, "agent", "response", 0.6, "reason");

        // Act
        var timedOut = await _service.CheckTimeoutsAsync(
            TimeSpan.FromHours(24),
            TimeSpan.FromHours(72));

        // Assert
        // Since the request was just created, it shouldn't be in the timed out list
        Assert.Empty(timedOut);
    }

    [Fact]
    public async Task GetPendingApprovalsAsync_WithNoRequests_ReturnsEmpty()
    {
        // Arrange
        var workflowId = Guid.NewGuid();

        // Act
        var pending = await _service.GetPendingApprovalsAsync(workflowId);

        // Assert
        Assert.Empty(pending);
    }
}
