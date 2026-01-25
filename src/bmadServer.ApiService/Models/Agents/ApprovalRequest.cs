using System.Text.Json;

namespace bmadServer.ApiService.Models.Agents;

/// <summary>
/// Represents a request for human approval of an agent decision
/// </summary>
public class ApprovalRequest
{
    /// <summary>
    /// Unique identifier for the approval request
    /// </summary>
    public Guid ApprovalRequestId { get; init; } = Guid.NewGuid();

    /// <summary>
    /// Workflow instance ID
    /// </summary>
    public required Guid WorkflowInstanceId { get; init; }

    /// <summary>
    /// Agent that made the decision
    /// </summary>
    public required string AgentId { get; init; }

    /// <summary>
    /// Proposed response from the agent
    /// </summary>
    public required string ProposedResponse { get; init; }

    /// <summary>
    /// Confidence score (0.0 to 1.0)
    /// </summary>
    public required double ConfidenceScore { get; init; }

    /// <summary>
    /// Agent's reasoning for the decision
    /// </summary>
    public required string Reasoning { get; init; }

    /// <summary>
    /// When the request was created
    /// </summary>
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Current status of the request
    /// </summary>
    public ApprovalStatus Status { get; set; } = ApprovalStatus.Pending;

    /// <summary>
    /// User who approved/modified/rejected
    /// </summary>
    public Guid? RespondedByUserId { get; set; }

    /// <summary>
    /// When the user responded
    /// </summary>
    public DateTime? RespondedAt { get; set; }

    /// <summary>
    /// Final approved response (may be modified)
    /// </summary>
    public string? ApprovedResponse { get; set; }

    /// <summary>
    /// Rejection reason if rejected
    /// </summary>
    public string? RejectionReason { get; set; }

    /// <summary>
    /// Additional guidance for regeneration
    /// </summary>
    public string? AdditionalGuidance { get; set; }
}

/// <summary>
/// Status of an approval request
/// </summary>
public enum ApprovalStatus
{
    Pending,
    Approved,
    Modified,
    Rejected,
    TimedOut
}
