using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace bmadServer.ApiService.Data.Entities;

/// <summary>
/// Entity representing a human approval request for low-confidence agent decisions.
/// </summary>
[Table("approval_requests")]
public class ApprovalRequest
{
    /// <summary>
    /// Unique identifier for the approval request.
    /// </summary>
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// ID of the workflow instance requiring approval.
    /// </summary>
    [Required]
    public Guid WorkflowInstanceId { get; set; }

    /// <summary>
    /// ID of the agent that generated the low-confidence decision.
    /// </summary>
    [Required]
    [MaxLength(100)]
    public required string AgentId { get; set; }

    /// <summary>
    /// The proposed response from the agent.
    /// </summary>
    [Required]
    public required string ProposedResponse { get; set; }

    /// <summary>
    /// Confidence score of the proposed response (0.0 to 1.0).
    /// </summary>
    [Required]
    [Range(0.0, 1.0)]
    public double ConfidenceScore { get; set; }

    /// <summary>
    /// Agent's reasoning for the proposed response.
    /// </summary>
    public string? Reasoning { get; set; }

    /// <summary>
    /// Current status of the approval request.
    /// Values: Pending, Approved, Modified, Rejected, TimedOut
    /// </summary>
    [Required]
    [MaxLength(50)]
    public required string Status { get; set; }

    /// <summary>
    /// ID of the user who made the approval decision (nullable while pending).
    /// </summary>
    public Guid? ApprovedByUserId { get; set; }

    /// <summary>
    /// Timestamp when the approval request was created.
    /// </summary>
    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Timestamp when the approval was responded to.
    /// </summary>
    public DateTime? RespondedAt { get; set; }

    /// <summary>
    /// The final response after human approval/modification.
    /// </summary>
    public string? FinalResponse { get; set; }

    /// <summary>
    /// Reason provided for rejection (only set when Status is Rejected).
    /// </summary>
    public string? RejectionReason { get; set; }

    /// <summary>
    /// Timestamp of the last reminder sent (nullable if no reminder sent).
    /// </summary>
    public DateTime? LastReminderSentAt { get; set; }

    /// <summary>
    /// Additional metadata stored as JSON.
    /// </summary>
    [Column(TypeName = "jsonb")]
    public string? Metadata { get; set; }
}
