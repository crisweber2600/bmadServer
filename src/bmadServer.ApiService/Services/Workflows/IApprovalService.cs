using bmadServer.ApiService.Models.Workflows;

namespace bmadServer.ApiService.Services.Workflows;

/// <summary>
/// Service for managing human approval requests in workflows.
/// Handles approval lifecycle: create, retrieve, approve/modify/reject.
/// </summary>
public interface IApprovalService
{
    /// <summary>
    /// Create a new approval request when an agent produces low-confidence output.
    /// Called by StepExecutor when AgentResult.RequiresHumanApproval is true.
    /// </summary>
    /// <param name="workflowInstanceId">The workflow instance requiring approval</param>
    /// <param name="agentId">The agent that produced the low-confidence result</param>
    /// <param name="stepId">The workflow step ID where approval is needed</param>
    /// <param name="proposedResponse">The agent's proposed response</param>
    /// <param name="confidenceScore">Confidence score (0-1, typically &lt; 0.7 for approval)</param>
    /// <param name="reasoning">Agent's explanation for the low confidence</param>
    /// <param name="requestedBy">User ID of the workflow owner</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created ApprovalRequest</returns>
    Task<ApprovalRequest> CreateApprovalRequestAsync(
        Guid workflowInstanceId,
        string agentId,
        string stepId,
        string proposedResponse,
        double confidenceScore,
        string? reasoning,
        Guid requestedBy,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieve a specific approval request by ID.
    /// </summary>
    /// <param name="approvalRequestId">The approval request ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The approval request or null if not found</returns>
    Task<ApprovalRequest?> GetApprovalRequestAsync(
        Guid approvalRequestId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the oldest pending approval request for a workflow.
    /// Returns null if no pending approvals exist.
    /// </summary>
    /// <param name="workflowInstanceId">The workflow instance ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The oldest pending approval or null</returns>
    Task<ApprovalRequest?> GetPendingApprovalAsync(
        Guid workflowInstanceId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Approve a request as-is without modifications.
    /// Workflow will resume with the original proposed response.
    /// </summary>
    /// <param name="approvalRequestId">The approval request ID</param>
    /// <param name="userId">User ID of the approver (must be workflow owner)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success result with updated approval request, or failure with error message</returns>
    Task<(bool Success, string? Message, ApprovalRequest? ApprovalRequest)> ApproveAsync(
        Guid approvalRequestId,
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Approve with modifications. Stores both original and modified responses.
    /// Workflow will resume with the modified response.
    /// </summary>
    /// <param name="approvalRequestId">The approval request ID</param>
    /// <param name="userId">User ID of the approver (must be workflow owner)</param>
    /// <param name="modifiedResponse">The user's modified version of the response</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success result with updated approval request, or failure with error message</returns>
    Task<(bool Success, string? Message, ApprovalRequest? ApprovalRequest)> ModifyAndApproveAsync(
        Guid approvalRequestId,
        Guid userId,
        string modifiedResponse,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Reject the proposed response. Agent should regenerate with additional context.
    /// May trigger a new approval request if regenerated result is still low-confidence.
    /// </summary>
    /// <param name="approvalRequestId">The approval request ID</param>
    /// <param name="userId">User ID of the rejecter (must be workflow owner)</param>
    /// <param name="rejectionReason">Reason for rejection (used as guidance for regeneration)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success result with updated approval request, or failure with error message</returns>
    Task<(bool Success, string? Message, ApprovalRequest? ApprovalRequest)> RejectAsync(
        Guid approvalRequestId,
        Guid userId,
        string rejectionReason,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get approval requests that have exceeded time thresholds.
    /// Used by ApprovalReminderService for notifications and auto-pause.
    /// </summary>
    /// <param name="reminderThreshold">Time since RequestedAt for reminder (e.g., 24 hours)</param>
    /// <param name="timeoutThreshold">Time since RequestedAt for auto-pause (e.g., 72 hours)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Tuple of (requests needing reminder, requests that timed out)</returns>
    Task<(List<ApprovalRequest> NeedReminder, List<ApprovalRequest> TimedOut)> GetTimedOutApprovalsAsync(
        TimeSpan reminderThreshold,
        TimeSpan timeoutThreshold,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Mark an approval request as timed out.
    /// Called by ApprovalReminderService when 72h threshold is exceeded.
    /// </summary>
    /// <param name="approvalRequestId">The approval request ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success result</returns>
    Task<bool> MarkAsTimedOutAsync(
        Guid approvalRequestId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all approval requests for a workflow instance.
    /// Used for audit/history display.
    /// </summary>
    /// <param name="workflowInstanceId">The workflow instance ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of all approval requests in chronological order</returns>
    Task<List<ApprovalRequest>> GetApprovalHistoryAsync(
        Guid workflowInstanceId,
        CancellationToken cancellationToken = default);
}
