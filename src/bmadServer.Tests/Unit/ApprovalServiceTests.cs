using bmadServer.ApiService.Agents;
using bmadServer.ApiService.Data;
using bmadServer.ApiService.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace bmadServer.Tests.Unit;

public class ApprovalServiceTests
{
    private readonly ApplicationDbContext _context;
    private readonly IApprovalService _approvalService;
    private readonly Mock<ILogger<ApprovalService>> _mockLogger;

    public ApprovalServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _mockLogger = new Mock<ILogger<ApprovalService>>();
        _approvalService = new ApprovalService(_context, _mockLogger.Object);
    }

    [Fact]
    public void RequiresApproval_BelowThreshold_ReturnsTrue()
    {
        // Arrange
        var confidenceScore = 0.65;
        var threshold = 0.7;

        // Act
        var result = _approvalService.RequiresApproval(confidenceScore, threshold);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void RequiresApproval_AtThreshold_ReturnsFalse()
    {
        // Arrange
        var confidenceScore = 0.7;
        var threshold = 0.7;

        // Act
        var result = _approvalService.RequiresApproval(confidenceScore, threshold);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void RequiresApproval_AboveThreshold_ReturnsFalse()
    {
        // Arrange
        var confidenceScore = 0.85;
        var threshold = 0.7;

        // Act
        var result = _approvalService.RequiresApproval(confidenceScore, threshold);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task CreateApprovalRequestAsync_CreatesRequestWithPendingStatus()
    {
        // Arrange
        var workflowInstanceId = Guid.NewGuid();
        var agentId = "architect";
        var proposedResponse = "Proposed architecture design";
        var confidenceScore = 0.65;
        var reasoning = "Based on limited context";

        // Act
        var approvalRequestId = await _approvalService.CreateApprovalRequestAsync(
            workflowInstanceId,
            agentId,
            proposedResponse,
            confidenceScore,
            reasoning);

        // Assert
        var approvalRequest = await _context.ApprovalRequests.FirstOrDefaultAsync(ar => ar.Id == approvalRequestId);
        Assert.NotNull(approvalRequest);
        Assert.Equal(workflowInstanceId, approvalRequest.WorkflowInstanceId);
        Assert.Equal(agentId, approvalRequest.AgentId);
        Assert.Equal(proposedResponse, approvalRequest.ProposedResponse);
        Assert.Equal(confidenceScore, approvalRequest.ConfidenceScore);
        Assert.Equal(reasoning, approvalRequest.Reasoning);
        Assert.Equal("Pending", approvalRequest.Status);
    }

    [Fact]
    public async Task GetApprovalRequestAsync_ReturnsExistingRequest()
    {
        // Arrange
        var approvalRequest = new ApprovalRequest
        {
            WorkflowInstanceId = Guid.NewGuid(),
            AgentId = "architect",
            ProposedResponse = "Proposed solution",
            ConfidenceScore = 0.65,
            Status = "Pending"
        };
        _context.ApprovalRequests.Add(approvalRequest);
        await _context.SaveChangesAsync();

        // Act
        var result = await _approvalService.GetApprovalRequestAsync(approvalRequest.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(approvalRequest.Id, result.Id);
    }

    [Fact]
    public async Task GetApprovalRequestAsync_NonExistent_ReturnsNull()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _approvalService.GetApprovalRequestAsync(nonExistentId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task ApproveAsync_PendingRequest_ApprovesSuccessfully()
    {
        // Arrange
        var approvalRequest = new ApprovalRequest
        {
            WorkflowInstanceId = Guid.NewGuid(),
            AgentId = "architect",
            ProposedResponse = "Proposed solution",
            ConfidenceScore = 0.65,
            Status = "Pending"
        };
        _context.ApprovalRequests.Add(approvalRequest);
        await _context.SaveChangesAsync();
        var userId = Guid.NewGuid();

        // Act
        var result = await _approvalService.ApproveAsync(approvalRequest.Id, userId);

        // Assert
        Assert.True(result);
        var updated = await _context.ApprovalRequests.FirstOrDefaultAsync(ar => ar.Id == approvalRequest.Id);
        Assert.NotNull(updated);
        Assert.Equal("Approved", updated.Status);
        Assert.Equal(userId, updated.ApprovedByUserId);
        Assert.NotNull(updated.RespondedAt);
        Assert.Equal(updated.ProposedResponse, updated.FinalResponse);
    }

    [Fact]
    public async Task ApproveAsync_NonPendingRequest_ReturnsFalse()
    {
        // Arrange
        var approvalRequest = new ApprovalRequest
        {
            WorkflowInstanceId = Guid.NewGuid(),
            AgentId = "architect",
            ProposedResponse = "Proposed solution",
            ConfidenceScore = 0.65,
            Status = "Approved"
        };
        _context.ApprovalRequests.Add(approvalRequest);
        await _context.SaveChangesAsync();
        var userId = Guid.NewGuid();

        // Act
        var result = await _approvalService.ApproveAsync(approvalRequest.Id, userId);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ModifyAsync_PendingRequest_ModifiesSuccessfully()
    {
        // Arrange
        var approvalRequest = new ApprovalRequest
        {
            WorkflowInstanceId = Guid.NewGuid(),
            AgentId = "architect",
            ProposedResponse = "Original solution",
            ConfidenceScore = 0.65,
            Status = "Pending"
        };
        _context.ApprovalRequests.Add(approvalRequest);
        await _context.SaveChangesAsync();
        var userId = Guid.NewGuid();
        var modifiedResponse = "Modified solution with improvements";

        // Act
        var result = await _approvalService.ModifyAsync(approvalRequest.Id, userId, modifiedResponse);

        // Assert
        Assert.True(result);
        var updated = await _context.ApprovalRequests.FirstOrDefaultAsync(ar => ar.Id == approvalRequest.Id);
        Assert.NotNull(updated);
        Assert.Equal("Modified", updated.Status);
        Assert.Equal(userId, updated.ApprovedByUserId);
        Assert.NotNull(updated.RespondedAt);
        Assert.Equal(modifiedResponse, updated.FinalResponse);
        Assert.Equal("Original solution", updated.ProposedResponse);
    }

    [Fact]
    public async Task RejectAsync_PendingRequest_RejectsSuccessfully()
    {
        // Arrange
        var approvalRequest = new ApprovalRequest
        {
            WorkflowInstanceId = Guid.NewGuid(),
            AgentId = "architect",
            ProposedResponse = "Proposed solution",
            ConfidenceScore = 0.65,
            Status = "Pending"
        };
        _context.ApprovalRequests.Add(approvalRequest);
        await _context.SaveChangesAsync();
        var userId = Guid.NewGuid();
        var reason = "Needs more technical detail";

        // Act
        var result = await _approvalService.RejectAsync(approvalRequest.Id, userId, reason);

        // Assert
        Assert.True(result);
        var updated = await _context.ApprovalRequests.FirstOrDefaultAsync(ar => ar.Id == approvalRequest.Id);
        Assert.NotNull(updated);
        Assert.Equal("Rejected", updated.Status);
        Assert.Equal(userId, updated.ApprovedByUserId);
        Assert.NotNull(updated.RespondedAt);
        Assert.Equal(reason, updated.RejectionReason);
    }

    [Fact]
    public async Task GetPendingRequestsNeedingRemindersAsync_ReturnsOldPendingRequests()
    {
        // Arrange
        var oldRequest = new ApprovalRequest
        {
            WorkflowInstanceId = Guid.NewGuid(),
            AgentId = "architect",
            ProposedResponse = "Old request",
            ConfidenceScore = 0.65,
            Status = "Pending",
            CreatedAt = DateTime.UtcNow.AddHours(-25)
        };
        var recentRequest = new ApprovalRequest
        {
            WorkflowInstanceId = Guid.NewGuid(),
            AgentId = "architect",
            ProposedResponse = "Recent request",
            ConfidenceScore = 0.65,
            Status = "Pending",
            CreatedAt = DateTime.UtcNow.AddHours(-20)
        };
        _context.ApprovalRequests.AddRange(oldRequest, recentRequest);
        await _context.SaveChangesAsync();

        // Act
        var results = await _approvalService.GetPendingRequestsNeedingRemindersAsync(24);

        // Assert
        Assert.Single(results);
        Assert.Contains(results, r => r.Id == oldRequest.Id);
        Assert.DoesNotContain(results, r => r.Id == recentRequest.Id);
    }

    [Fact]
    public async Task GetPendingRequestsNeedingRemindersAsync_ExcludesAlreadyReminded()
    {
        // Arrange
        var requestWithReminder = new ApprovalRequest
        {
            WorkflowInstanceId = Guid.NewGuid(),
            AgentId = "architect",
            ProposedResponse = "Request with reminder",
            ConfidenceScore = 0.65,
            Status = "Pending",
            CreatedAt = DateTime.UtcNow.AddHours(-30),
            LastReminderSentAt = DateTime.UtcNow.AddHours(-6)
        };
        _context.ApprovalRequests.Add(requestWithReminder);
        await _context.SaveChangesAsync();

        // Act
        var results = await _approvalService.GetPendingRequestsNeedingRemindersAsync(24);

        // Assert
        Assert.Empty(results);
    }

    [Fact]
    public async Task GetTimedOutRequestsAsync_ReturnsRequestsOlderThanThreshold()
    {
        // Arrange
        var timedOutRequest = new ApprovalRequest
        {
            WorkflowInstanceId = Guid.NewGuid(),
            AgentId = "architect",
            ProposedResponse = "Timed out request",
            ConfidenceScore = 0.65,
            Status = "Pending",
            CreatedAt = DateTime.UtcNow.AddHours(-73)
        };
        var recentRequest = new ApprovalRequest
        {
            WorkflowInstanceId = Guid.NewGuid(),
            AgentId = "architect",
            ProposedResponse = "Recent request",
            ConfidenceScore = 0.65,
            Status = "Pending",
            CreatedAt = DateTime.UtcNow.AddHours(-60)
        };
        _context.ApprovalRequests.AddRange(timedOutRequest, recentRequest);
        await _context.SaveChangesAsync();

        // Act
        var results = await _approvalService.GetTimedOutRequestsAsync(72);

        // Assert
        Assert.Single(results);
        Assert.Contains(results, r => r.Id == timedOutRequest.Id);
    }

    [Fact]
    public async Task MarkReminderSentAsync_UpdatesTimestamp()
    {
        // Arrange
        var approvalRequest = new ApprovalRequest
        {
            WorkflowInstanceId = Guid.NewGuid(),
            AgentId = "architect",
            ProposedResponse = "Proposed solution",
            ConfidenceScore = 0.65,
            Status = "Pending"
        };
        _context.ApprovalRequests.Add(approvalRequest);
        await _context.SaveChangesAsync();

        // Act
        var result = await _approvalService.MarkReminderSentAsync(approvalRequest.Id);

        // Assert
        Assert.True(result);
        var updated = await _context.ApprovalRequests.FirstOrDefaultAsync(ar => ar.Id == approvalRequest.Id);
        Assert.NotNull(updated);
        Assert.NotNull(updated.LastReminderSentAt);
    }

    [Fact]
    public async Task TimeoutRequestAsync_UpdatesStatusToTimedOut()
    {
        // Arrange
        var approvalRequest = new ApprovalRequest
        {
            WorkflowInstanceId = Guid.NewGuid(),
            AgentId = "architect",
            ProposedResponse = "Proposed solution",
            ConfidenceScore = 0.65,
            Status = "Pending"
        };
        _context.ApprovalRequests.Add(approvalRequest);
        await _context.SaveChangesAsync();

        // Act
        var result = await _approvalService.TimeoutRequestAsync(approvalRequest.Id);

        // Assert
        Assert.True(result);
        var updated = await _context.ApprovalRequests.FirstOrDefaultAsync(ar => ar.Id == approvalRequest.Id);
        Assert.NotNull(updated);
        Assert.Equal("TimedOut", updated.Status);
        Assert.NotNull(updated.RespondedAt);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
