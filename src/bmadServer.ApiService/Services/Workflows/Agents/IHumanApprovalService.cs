using bmadServer.ApiService.Models.Agents;

namespace bmadServer.ApiService.Services.Workflows.Agents;

/// <summary>
/// Service for managing human approval of low-confidence agent decisions
/// </summary>
public interface IHumanApprovalService
{
    /// <summary>
    /// Create an approval request
    /// </summary>
    Task<ApprovalRequest> CreateApprovalRequestAsync(
        Guid workflowInstanceId,
        string agentId,
        string proposedResponse,
        double confidenceScore,
        string reasoning,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get an approval request
    /// </summary>
    Task<ApprovalRequest?> GetApprovalRequestAsync(
        Guid approvalRequestId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get pending approval requests for a workflow
    /// </summary>
    Task<IEnumerable<ApprovalRequest>> GetPendingApprovalsAsync(
        Guid workflowInstanceId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Approve a request
    /// </summary>
    Task<bool> ApproveAsync(
        Guid approvalRequestId,
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Modify and approve a request
    /// </summary>
    Task<bool> ModifyAndApproveAsync(
        Guid approvalRequestId,
        Guid userId,
        string modifiedResponse,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Reject a request
    /// </summary>
    Task<bool> RejectAsync(
        Guid approvalRequestId,
        Guid userId,
        string reason,
        string? additionalGuidance = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Check for timed out approval requests
    /// </summary>
    Task<IEnumerable<ApprovalRequest>> CheckTimeoutsAsync(
        TimeSpan reminderThreshold,
        TimeSpan autoTimeoutThreshold,
        CancellationToken cancellationToken = default);
}
