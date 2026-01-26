namespace bmadServer.ApiService.Services.Workflows.Agents;

/// <summary>
/// Interface for managing human approval of agent decisions
/// </summary>
public interface IHumanApprovalService
{
    /// <summary>
    /// Request approval for a low-confidence decision
    /// </summary>
    void RequestApproval(ApprovalRequest request);

    /// <summary>
    /// Approve a pending request
    /// </summary>
    ApprovalDecision Approve(Guid requestId, Guid userId);

    /// <summary>
    /// Modify and approve a pending request
    /// </summary>
    ApprovalDecision Modify(Guid requestId, string modifiedResponse, Guid userId);

    /// <summary>
    /// Reject a pending request
    /// </summary>
    ApprovalDecision Reject(Guid requestId, string reason, Guid userId);

    /// <summary>
    /// Get all pending approvals for a workflow
    /// </summary>
    IReadOnlyList<ApprovalRequest> GetPendingApprovals(Guid workflowInstanceId);

    /// <summary>
    /// Get approval history for a workflow
    /// </summary>
    IReadOnlyList<ApprovalDecision> GetApprovalHistory(Guid workflowInstanceId);

    /// <summary>
    /// Check if approval is needed based on confidence score
    /// </summary>
    bool IsApprovalNeeded(double confidenceScore);
}
