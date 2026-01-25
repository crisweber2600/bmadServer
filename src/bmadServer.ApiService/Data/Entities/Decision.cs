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
    /// Navigation property to the workflow instance
    /// </summary>
    public WorkflowInstance? WorkflowInstance { get; set; }

    /// <summary>
    /// Navigation property to the user who made the decision
    /// </summary>
    public User? DecisionMaker { get; set; }
}
