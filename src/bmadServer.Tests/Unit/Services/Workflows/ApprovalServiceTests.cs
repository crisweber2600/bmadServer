using bmadServer.Tests.Helpers;
using FluentAssertions;
using bmadServer.ApiService.Data;
using bmadServer.ApiService.Models.Workflows;
using bmadServer.ApiService.Services.Workflows;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace bmadServer.Tests.Unit.Services.Workflows;

public class ApprovalServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly SqliteConnection _connection;
    private readonly Mock<ILogger<ApprovalService>> _mockLogger;
    private readonly ApprovalService _service;

    public ApprovalServiceTests()
    {
        var options = TestDatabaseHelper.CreateSqliteOptions(out _connection);
        _context = new ApplicationDbContext(options);
        _context.Database.EnsureCreated();
        _mockLogger = new Mock<ILogger<ApprovalService>>();
        _service = new ApprovalService(_context, _mockLogger.Object);
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    #region CreateApprovalRequestAsync Tests

    [Fact]
    public async Task CreateApprovalRequestAsync_WithValidInput_ShouldCreateAndReturnApprovalRequest()
    {
        // Arrange
        var workflowId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var agentId = "architect";
        var stepId = "design-system";
        var proposedResponse = "This is the proposed response.";
        var confidenceScore = 0.65;
        var reasoning = "Requirements are ambiguous.";

        // Act
        var result = await _service.CreateApprovalRequestAsync(
            workflowId,
            agentId,
            stepId,
            proposedResponse,
            confidenceScore,
            reasoning,
            userId);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().NotBeEmpty();
        result.WorkflowInstanceId.Should().Be(workflowId);
        result.AgentId.Should().Be(agentId);
        result.StepId.Should().Be(stepId);
        result.ProposedResponse.Should().Be(proposedResponse);
        result.ConfidenceScore.Should().Be(confidenceScore);
        result.Reasoning.Should().Be(reasoning);
        result.Status.Should().Be(ApprovalStatus.Pending);
        result.RequestedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        result.RequestedBy.Should().Be(userId);
        result.Version.Should().Be(1);
    }

    [Fact]
    public async Task CreateApprovalRequestAsync_WithInvalidWorkflowId_ShouldThrowArgumentException()
    {
        // Arrange
        var emptyWorkflowId = Guid.Empty;
        var userId = Guid.NewGuid();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.CreateApprovalRequestAsync(
                emptyWorkflowId,
                "agent-id",
                "step-id",
                "response",
                0.65,
                null,
                userId));
    }

    [Fact]
    public async Task CreateApprovalRequestAsync_WithInvalidConfidenceScore_ShouldThrowArgumentOutOfRangeException()
    {
        // Arrange
        var workflowId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            _service.CreateApprovalRequestAsync(
                workflowId,
                "agent-id",
                "step-id",
                "response",
                1.5, // Invalid: > 1.0
                null,
                userId));
    }

    [Fact]
    public async Task CreateApprovalRequestAsync_WithNullAgentId_ShouldThrowArgumentException()
    {
        // Arrange
        var workflowId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.CreateApprovalRequestAsync(
                workflowId,
                null!,
                "step-id",
                "response",
                0.65,
                null,
                userId));
    }

    #endregion

    #region GetApprovalRequestAsync Tests

    [Fact]
    public async Task GetApprovalRequestAsync_WithValidId_ShouldReturnApprovalRequest()
    {
        // Arrange
        var approval = CreateAndSaveApprovalRequest();

        // Act
        var result = await _service.GetApprovalRequestAsync(approval.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(approval.Id);
        result.WorkflowInstanceId.Should().Be(approval.WorkflowInstanceId);
        result.AgentId.Should().Be(approval.AgentId);
    }

    [Fact]
    public async Task GetApprovalRequestAsync_WithInvalidId_ShouldReturnNull()
    {
        // Arrange
        var invalidId = Guid.NewGuid();

        // Act
        var result = await _service.GetApprovalRequestAsync(invalidId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetApprovalRequestAsync_WithEmptyId_ShouldReturnNull()
    {
        // Act
        var result = await _service.GetApprovalRequestAsync(Guid.Empty);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetPendingApprovalAsync Tests

    [Fact]
    public async Task GetPendingApprovalAsync_WithPendingApproval_ShouldReturnOldestPending()
    {
        // Arrange
        var workflowId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var approval1 = await _service.CreateApprovalRequestAsync(
            workflowId, "agent-1", "step-1", "response-1", 0.65, null, userId);
        await Task.Delay(100); // Ensure different RequestedAt times
        var approval2 = await _service.CreateApprovalRequestAsync(
            workflowId, "agent-2", "step-2", "response-2", 0.60, null, userId);

        // Act
        var result = await _service.GetPendingApprovalAsync(workflowId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(approval1.Id); // Should return oldest
    }

    [Fact]
    public async Task GetPendingApprovalAsync_WithNoPendingApproval_ShouldReturnNull()
    {
        // Arrange
        var workflowId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        
        // Create WorkflowInstance first (required for ApproveAsync validation)
        var instance = new WorkflowInstance
        {
            Id = workflowId,
            WorkflowDefinitionId = "test-workflow",
            UserId = userId,
            Status = WorkflowStatus.Running,
            CurrentStep = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.WorkflowInstances.Add(instance);
        await _context.SaveChangesAsync();
        
        var approval = await _service.CreateApprovalRequestAsync(
            workflowId, "agent", "step", "response", 0.65, null, userId);

        // Mark as approved
        await _service.ApproveAsync(approval.Id, userId);

        // Act
        var result = await _service.GetPendingApprovalAsync(workflowId);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region ApproveAsync Tests

    [Fact]
    public async Task ApproveAsync_WithValidRequest_ShouldApproveAndReturnSuccess()
    {
        // Arrange
        var approval = CreateAndSaveApprovalRequest();
        var userId = approval.RequestedBy;

        // WorkflowInstance already created by CreateAndSaveApprovalRequest
        // Just fetch the existing instance to link it
        var workflowInstance = await _context.WorkflowInstances.FindAsync(approval.WorkflowInstanceId);
        approval.WorkflowInstance = workflowInstance!;
        await _context.SaveChangesAsync();

        // Act
        var (success, message, result) = await _service.ApproveAsync(approval.Id, userId);

        // Assert
        success.Should().BeTrue();
        message.Should().BeNull();
        result.Should().NotBeNull();
        result!.Status.Should().Be(ApprovalStatus.Approved);
        result.ResolvedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        result.ResolvedBy.Should().Be(userId);
    }

    [Fact]
    public async Task ApproveAsync_WithNonExistentRequest_ShouldReturnFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var (success, message, result) = await _service.ApproveAsync(Guid.NewGuid(), userId);

        // Assert
        success.Should().BeFalse();
        message.Should().NotBeNullOrEmpty();
        result.Should().BeNull();
    }

    [Fact]
    public async Task ApproveAsync_WithNonOwner_ShouldReturnFailure()
    {
        // Arrange
        var approval = CreateAndSaveApprovalRequest();
        var otherUserId = Guid.NewGuid();
        
        // WorkflowInstance already created by CreateAndSaveApprovalRequest
        var workflowInstance = await _context.WorkflowInstances.FindAsync(approval.WorkflowInstanceId);
        approval.WorkflowInstance = workflowInstance!;
        await _context.SaveChangesAsync();

        // Act
        var (success, message, result) = await _service.ApproveAsync(approval.Id, otherUserId);

        // Assert
        success.Should().BeFalse();
        message.Should().Contain("workflow owner");
        result.Should().BeNull();
    }

    #endregion

    #region ModifyAndApproveAsync Tests

    [Fact]
    public async Task ModifyAndApproveAsync_WithValidModification_ShouldUpdateAndReturnSuccess()
    {
        // Arrange
        var approval = CreateAndSaveApprovalRequest();
        var userId = approval.RequestedBy;
        var modifiedResponse = "This is the modified response.";
        
        // WorkflowInstance already created by CreateAndSaveApprovalRequest
        var workflowInstance = await _context.WorkflowInstances.FindAsync(approval.WorkflowInstanceId);
        approval.WorkflowInstance = workflowInstance!;
        await _context.SaveChangesAsync();

        // Act
        var (success, message, result) = await _service.ModifyAndApproveAsync(
            approval.Id, userId, modifiedResponse);

        // Assert
        success.Should().BeTrue();
        message.Should().BeNull();
        result.Should().NotBeNull();
        result!.Status.Should().Be(ApprovalStatus.Modified);
        result.ModifiedResponse.Should().Be(modifiedResponse);
        result.ProposedResponse.Should().NotBe(modifiedResponse); // Original should be preserved
    }

    [Fact]
    public async Task ModifyAndApproveAsync_WithEmptyModification_ShouldReturnFailure()
    {
        // Arrange
        var approval = CreateAndSaveApprovalRequest();
        var userId = approval.RequestedBy;

        // Act
        var (success, message, result) = await _service.ModifyAndApproveAsync(
            approval.Id, userId, string.Empty);

        // Assert
        success.Should().BeFalse();
        message.Should().NotBeNullOrEmpty();
        result.Should().BeNull();
    }

    #endregion

    #region RejectAsync Tests

    [Fact]
    public async Task RejectAsync_WithValidRejection_ShouldUpdateAndReturnSuccess()
    {
        // Arrange
        var approval = CreateAndSaveApprovalRequest();
        var userId = approval.RequestedBy;
        var rejectionReason = "Does not meet requirements.";
        
        // WorkflowInstance already created by CreateAndSaveApprovalRequest
        var workflowInstance = await _context.WorkflowInstances.FindAsync(approval.WorkflowInstanceId);
        approval.WorkflowInstance = workflowInstance!;
        await _context.SaveChangesAsync();

        // Act
        var (success, message, result) = await _service.RejectAsync(
            approval.Id, userId, rejectionReason);

        // Assert
        success.Should().BeTrue();
        message.Should().BeNull();
        result.Should().NotBeNull();
        result!.Status.Should().Be(ApprovalStatus.Rejected);
        result.RejectionReason.Should().Be(rejectionReason);
    }

    [Fact]
    public async Task RejectAsync_WithEmptyReason_ShouldReturnFailure()
    {
        // Arrange
        var approval = CreateAndSaveApprovalRequest();
        var userId = approval.RequestedBy;

        // Act
        var (success, message, result) = await _service.RejectAsync(
            approval.Id, userId, string.Empty);

        // Assert
        success.Should().BeFalse();
        message.Should().NotBeNullOrEmpty();
        result.Should().BeNull();
    }

    #endregion

    #region GetTimedOutApprovalsAsync Tests

    [Fact]
    public async Task GetTimedOutApprovalsAsync_WithOldRequests_ShouldReturnCorrectLists()
    {
        // Arrange
        var workflowId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        // Create WorkflowInstance first (required for foreign key)
        var instance = new WorkflowInstance
        {
            Id = workflowId,
            WorkflowDefinitionId = "test-workflow",
            UserId = userId,
            Status = WorkflowStatus.Running,
            CurrentStep = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.WorkflowInstances.Add(instance);
        await _context.SaveChangesAsync();

        // Create approval with old RequestedAt (> 24 hours)
        var approval = new ApprovalRequest
        {
            Id = Guid.NewGuid(),
            WorkflowInstanceId = workflowId,
            AgentId = "agent",
            StepId = "step",
            ProposedResponse = "response",
            ConfidenceScore = 0.65,
            Status = ApprovalStatus.Pending,
            RequestedAt = DateTime.UtcNow.AddHours(-30),
            RequestedBy = userId,
            Version = 1
        };
        _context.ApprovalRequests.Add(approval);
        await _context.SaveChangesAsync();

        // Act
        var (needReminder, timedOut) = await _service.GetTimedOutApprovalsAsync(
            TimeSpan.FromHours(24),
            TimeSpan.FromHours(72));

        // Assert
        needReminder.Should().ContainSingle();
        timedOut.Should().BeEmpty();
    }

    #endregion

    #region MarkAsTimedOutAsync Tests

    [Fact]
    public async Task MarkAsTimedOutAsync_WithPendingRequest_ShouldMarkAsTimedOut()
    {
        // Arrange
        var approval = CreateAndSaveApprovalRequest();

        // Act
        var result = await _service.MarkAsTimedOutAsync(approval.Id);

        // Assert
        result.Should().BeTrue();
        var updatedApproval = await _context.ApprovalRequests.FindAsync(approval.Id);
        updatedApproval!.Status.Should().Be(ApprovalStatus.TimedOut);
        updatedApproval.ResolvedAt.Should().NotBeNull();
    }

    #endregion

    #region GetApprovalHistoryAsync Tests

    [Fact]
    public async Task GetApprovalHistoryAsync_ShouldReturnAllApprovalsForWorkflow()
    {
        // Arrange
        var workflowId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        await _service.CreateApprovalRequestAsync(
            workflowId, "agent-1", "step-1", "response-1", 0.65, null, userId);
        await Task.Delay(100);
        await _service.CreateApprovalRequestAsync(
            workflowId, "agent-2", "step-2", "response-2", 0.60, null, userId);

        // Act
        var result = await _service.GetApprovalHistoryAsync(workflowId);

        // Assert
        result.Should().HaveCount(2);
        Assert.True(result[0].RequestedAt < result[1].RequestedAt);
    }

    [Fact]
    public async Task GetApprovalHistoryAsync_WithNonExistentWorkflow_ShouldReturnEmptyList()
    {
        // Act
        var result = await _service.GetApprovalHistoryAsync(Guid.NewGuid());

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region Helper Methods

    private ApprovalRequest CreateAndSaveApprovalRequest()
    {
        var workflowId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        // Create WorkflowInstance first (required for foreign key)
        var instance = new WorkflowInstance
        {
            Id = workflowId,
            WorkflowDefinitionId = "test-workflow",
            UserId = userId,
            Status = WorkflowStatus.Running,
            CurrentStep = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.WorkflowInstances.Add(instance);
        _context.SaveChanges();

        var approval = new ApprovalRequest
        {
            Id = Guid.NewGuid(),
            WorkflowInstanceId = workflowId,
            AgentId = "test-agent",
            StepId = "test-step",
            ProposedResponse = "Test response",
            ConfidenceScore = 0.65,
            Reasoning = "Test reasoning",
            Status = ApprovalStatus.Pending,
            RequestedAt = DateTime.UtcNow,
            RequestedBy = userId,
            Version = 1
        };

        _context.ApprovalRequests.Add(approval);
        _context.SaveChanges();
        return approval;
    }

    private WorkflowInstance CreateAndSaveWorkflowInstance(Guid workflowId, Guid userId)
    {
        var instance = new WorkflowInstance
        {
            Id = workflowId,
            WorkflowDefinitionId = "test-workflow",
            UserId = userId,
            Status = WorkflowStatus.Running,
            CurrentStep = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.WorkflowInstances.Add(instance);
        _context.SaveChanges();
        return instance;
    }

    #endregion
}
