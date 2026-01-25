namespace bmadServer.ApiService.Models;

/// <summary>
/// Request to approve an approval request.
/// </summary>
public class ApproveRequestDto
{
    /// <summary>
    /// ID of the user approving the request.
    /// </summary>
    public required Guid UserId { get; set; }
}

/// <summary>
/// Request to modify an approval request.
/// </summary>
public class ModifyRequestDto
{
    /// <summary>
    /// ID of the user modifying the request.
    /// </summary>
    public required Guid UserId { get; set; }

    /// <summary>
    /// The modified response.
    /// </summary>
    public required string ModifiedResponse { get; set; }
}

/// <summary>
/// Request to reject an approval request.
/// </summary>
public class RejectRequestDto
{
    /// <summary>
    /// ID of the user rejecting the request.
    /// </summary>
    public required Guid UserId { get; set; }

    /// <summary>
    /// Reason for rejection.
    /// </summary>
    public required string Reason { get; set; }
}

/// <summary>
/// Response DTO for approval request details.
/// </summary>
public class ApprovalRequestDto
{
    /// <summary>
    /// Unique identifier for the approval request.
    /// </summary>
    public required Guid Id { get; set; }

    /// <summary>
    /// ID of the workflow instance.
    /// </summary>
    public required Guid WorkflowInstanceId { get; set; }

    /// <summary>
    /// ID of the agent that generated the decision.
    /// </summary>
    public required string AgentId { get; set; }

    /// <summary>
    /// The proposed response from the agent.
    /// </summary>
    public required string ProposedResponse { get; set; }

    /// <summary>
    /// Confidence score (0.0 to 1.0).
    /// </summary>
    public double ConfidenceScore { get; set; }

    /// <summary>
    /// Agent's reasoning for the proposed response.
    /// </summary>
    public string? Reasoning { get; set; }

    /// <summary>
    /// Current status of the approval request.
    /// </summary>
    public required string Status { get; set; }

    /// <summary>
    /// ID of the user who responded to the request.
    /// </summary>
    public Guid? ApprovedByUserId { get; set; }

    /// <summary>
    /// Timestamp when the request was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Timestamp when the request was responded to.
    /// </summary>
    public DateTime? RespondedAt { get; set; }

    /// <summary>
    /// The final response after approval/modification.
    /// </summary>
    public string? FinalResponse { get; set; }

    /// <summary>
    /// Reason for rejection (if rejected).
    /// </summary>
    public string? RejectionReason { get; set; }

    /// <summary>
    /// Timestamp of the last reminder sent.
    /// </summary>
    public DateTime? LastReminderSentAt { get; set; }
}
