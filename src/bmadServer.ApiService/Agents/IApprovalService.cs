namespace bmadServer.ApiService.Agents;

/// <summary>
/// Service for managing human approval requests for low-confidence agent decisions.
/// </summary>
public interface IApprovalService
{
    /// <summary>
    /// Checks if an agent response requires human approval based on confidence score.
    /// </summary>
    /// <param name="confidenceScore">The confidence score (0.0 to 1.0).</param>
    /// <param name="threshold">The threshold below which approval is required (default: 0.7).</param>
    /// <returns>True if approval is required, false otherwise.</returns>
    bool RequiresApproval(double confidenceScore, double threshold = 0.7);

    /// <summary>
    /// Creates a new approval request for a low-confidence decision.
    /// </summary>
    /// <param name="workflowInstanceId">The workflow instance ID.</param>
    /// <param name="agentId">The agent ID that generated the decision.</param>
    /// <param name="proposedResponse">The proposed response from the agent.</param>
    /// <param name="confidenceScore">The confidence score (0.0 to 1.0).</param>
    /// <param name="reasoning">The agent's reasoning for the proposed response.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created approval request ID.</returns>
    Task<Guid> CreateApprovalRequestAsync(
        Guid workflowInstanceId,
        string agentId,
        string proposedResponse,
        double confidenceScore,
        string? reasoning = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an approval request by ID.
    /// </summary>
    /// <param name="approvalRequestId">The approval request ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The approval request, or null if not found.</returns>
    Task<Data.Entities.ApprovalRequest?> GetApprovalRequestAsync(
        Guid approvalRequestId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Approves an approval request.
    /// </summary>
    /// <param name="approvalRequestId">The approval request ID.</param>
    /// <param name="userId">The user ID who is approving.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if approval was successful, false otherwise.</returns>
    Task<bool> ApproveAsync(
        Guid approvalRequestId,
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Modifies and approves an approval request.
    /// </summary>
    /// <param name="approvalRequestId">The approval request ID.</param>
    /// <param name="userId">The user ID who is modifying.</param>
    /// <param name="modifiedResponse">The modified response.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if modification was successful, false otherwise.</returns>
    Task<bool> ModifyAsync(
        Guid approvalRequestId,
        Guid userId,
        string modifiedResponse,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Rejects an approval request.
    /// </summary>
    /// <param name="approvalRequestId">The approval request ID.</param>
    /// <param name="userId">The user ID who is rejecting.</param>
    /// <param name="reason">The reason for rejection.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if rejection was successful, false otherwise.</returns>
    Task<bool> RejectAsync(
        Guid approvalRequestId,
        Guid userId,
        string reason,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets pending approval requests that need reminders.
    /// </summary>
    /// <param name="reminderThresholdHours">Hours since creation to trigger reminder (default: 24).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of approval requests needing reminders.</returns>
    Task<List<Data.Entities.ApprovalRequest>> GetPendingRequestsNeedingRemindersAsync(
        int reminderThresholdHours = 24,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets pending approval requests that have timed out.
    /// </summary>
    /// <param name="timeoutThresholdHours">Hours since creation to trigger timeout (default: 72).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of approval requests that have timed out.</returns>
    Task<List<Data.Entities.ApprovalRequest>> GetTimedOutRequestsAsync(
        int timeoutThresholdHours = 72,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks that a reminder has been sent for an approval request.
    /// </summary>
    /// <param name="approvalRequestId">The approval request ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if successful, false otherwise.</returns>
    Task<bool> MarkReminderSentAsync(
        Guid approvalRequestId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Times out an approval request after the timeout threshold.
    /// </summary>
    /// <param name="approvalRequestId">The approval request ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if successful, false otherwise.</returns>
    Task<bool> TimeoutRequestAsync(
        Guid approvalRequestId,
        CancellationToken cancellationToken = default);
}
