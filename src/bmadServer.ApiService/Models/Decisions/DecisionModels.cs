using System.Text.Json;

namespace bmadServer.ApiService.Models.Decisions;

/// <summary>
/// Request model for creating a new decision
/// </summary>
public class CreateDecisionRequest
{
    /// <summary>
    /// Workflow instance ID where the decision is being made
    /// </summary>
    public required Guid WorkflowInstanceId { get; set; }

    /// <summary>
    /// Identifier of the workflow step where the decision is being made
    /// </summary>
    public required string StepId { get; set; }

    /// <summary>
    /// Type/category of the decision
    /// </summary>
    public required string DecisionType { get; set; }

    /// <summary>
    /// The actual decision value (can be any valid JSON)
    /// </summary>
    public required JsonElement Value { get; set; }

    /// <summary>
    /// The question that was asked (optional)
    /// </summary>
    public string? Question { get; set; }

    /// <summary>
    /// Options that were presented (optional, can be any valid JSON)
    /// </summary>
    public JsonElement? Options { get; set; }

    /// <summary>
    /// Reasoning provided by the decision maker (optional)
    /// </summary>
    public string? Reasoning { get; set; }

    /// <summary>
    /// Contextual information at the time of decision (optional, can be any valid JSON)
    /// </summary>
    public JsonElement? Context { get; set; }
}

/// <summary>
/// Response model for decision information
/// </summary>
public class DecisionResponse
{
    /// <summary>
    /// Unique identifier for the decision
    /// </summary>
    public required Guid Id { get; set; }

    /// <summary>
    /// Workflow instance ID where this decision was made
    /// </summary>
    public required Guid WorkflowInstanceId { get; set; }

    /// <summary>
    /// Identifier of the workflow step where the decision was made
    /// </summary>
    public required string StepId { get; set; }

    /// <summary>
    /// Type/category of the decision
    /// </summary>
    public required string DecisionType { get; set; }

    /// <summary>
    /// The actual decision value
    /// </summary>
    public required JsonElement Value { get; set; }

    /// <summary>
    /// User ID who made this decision
    /// </summary>
    public required Guid DecidedBy { get; set; }

    /// <summary>
    /// Timestamp when the decision was made
    /// </summary>
    public required DateTime DecidedAt { get; set; }

    /// <summary>
    /// The question that was asked
    /// </summary>
    public string? Question { get; set; }

    /// <summary>
    /// Options that were presented
    /// </summary>
    public JsonElement? Options { get; set; }

    /// <summary>
    /// Reasoning provided by the decision maker
    /// </summary>
    public string? Reasoning { get; set; }

    /// <summary>
    /// Contextual information at the time of decision
    /// </summary>
    public JsonElement? Context { get; set; }
}
