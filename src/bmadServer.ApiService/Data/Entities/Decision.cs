using bmadServer.ApiService.Models.Workflows;
using System.Text.Json;

namespace bmadServer.ApiService.Data.Entities;

/// <summary>
/// Represents a decision made during a workflow execution.
/// Captures decision metadata, value, context, and attribution.
/// </summary>
public class Decision
{
    /// <summary>
    /// Unique identifier for the decision
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Reference to the workflow instance where this decision was made
    /// </summary>
    public Guid WorkflowInstanceId { get; set; }

    /// <summary>
    /// Identifier of the workflow step where the decision was made
    /// </summary>
    public required string StepId { get; set; }

    /// <summary>
    /// Type/category of the decision (e.g., "approval", "selection", "configuration")
    /// </summary>
    public required string DecisionType { get; set; }

    /// <summary>
    /// The actual decision value stored as JSONB for flexibility
    /// Can store simple values or complex structured data
    /// </summary>
    public JsonDocument? Value { get; set; }

    /// <summary>
    /// User ID who made this decision
    /// </summary>
    public Guid DecidedBy { get; set; }

    /// <summary>
    /// Timestamp when the decision was made
    /// </summary>
    public DateTime DecidedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// The question that was asked to elicit this decision
    /// </summary>
    public string? Question { get; set; }

    /// <summary>
    /// Options that were presented (if applicable), stored as JSONB
    /// </summary>
    public JsonDocument? Options { get; set; }

    /// <summary>
    /// Reasoning provided by the decision maker (optional)
    /// </summary>
    public string? Reasoning { get; set; }

    /// <summary>
    /// Contextual information at the time of decision, stored as JSONB
    /// </summary>
    public JsonDocument? Context { get; set; }

    /// <summary>
    /// Current version number of this decision
    /// </summary>
    public int CurrentVersion { get; set; } = 1;

    /// <summary>
    /// Timestamp when this decision was last updated
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// User ID who last updated this decision
    /// </summary>
    public Guid? UpdatedBy { get; set; }

    /// <summary>
    /// Whether this decision is locked (cannot be modified)
    /// </summary>
    public bool IsLocked { get; set; } = false;

    /// <summary>
    /// User ID who locked this decision
    /// </summary>
    public Guid? LockedBy { get; set; }

    /// <summary>
    /// Timestamp when this decision was locked
    /// </summary>
    public DateTime? LockedAt { get; set; }

    /// <summary>
    /// Reason for locking (optional)
    /// </summary>
    public string? LockReason { get; set; }

    /// <summary>
    /// Current workflow status of the decision (Draft, UnderReview, Approved, etc.)
    /// </summary>
    public DecisionStatus Status { get; set; } = DecisionStatus.Draft;

    /// <summary>
    /// Navigation property to the workflow instance
    /// </summary>
    public WorkflowInstance? WorkflowInstance { get; set; }

    /// <summary>
    /// Navigation property to the user who made the decision
    /// </summary>
    public User? DecisionMaker { get; set; }

    /// <summary>
    /// Navigation property to version history
    /// </summary>
    public ICollection<DecisionVersion> Versions { get; set; } = new List<DecisionVersion>();
}
