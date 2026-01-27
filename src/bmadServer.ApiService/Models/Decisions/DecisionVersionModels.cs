using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace bmadServer.ApiService.Models.Decisions;

/// <summary>
/// Request model for updating a decision
/// </summary>
public class UpdateDecisionRequest
{
    /// <summary>
    /// The new decision value (can be any valid JSON)
    /// </summary>
    [Required(ErrorMessage = "Decision value is required")]
    public required JsonElement Value { get; set; }

    /// <summary>
    /// The question (optional, can be updated)
    /// </summary>
    public string? Question { get; set; }

    /// <summary>
    /// Options (optional, can be updated)
    /// </summary>
    public JsonElement? Options { get; set; }

    /// <summary>
    /// Reasoning (optional, can be updated)
    /// </summary>
    public string? Reasoning { get; set; }

    /// <summary>
    /// Context (optional, can be updated)
    /// </summary>
    public JsonElement? Context { get; set; }

    /// <summary>
    /// Reason for this change (for version history)
    /// </summary>
    public string? ChangeReason { get; set; }
}

/// <summary>
/// Response model for decision version information
/// </summary>
public class DecisionVersionResponse
{
    /// <summary>
    /// Unique identifier for this version record
    /// </summary>
    public required Guid Id { get; set; }

    /// <summary>
    /// Version number
    /// </summary>
    public required int VersionNumber { get; set; }

    /// <summary>
    /// The decision value at this version
    /// </summary>
    public required JsonElement Value { get; set; }

    /// <summary>
    /// User ID who made this modification
    /// </summary>
    public required Guid ModifiedBy { get; set; }

    /// <summary>
    /// Timestamp when this version was created
    /// </summary>
    public required DateTime ModifiedAt { get; set; }

    /// <summary>
    /// Reason for the change
    /// </summary>
    public string? ChangeReason { get; set; }

    /// <summary>
    /// Question at this version
    /// </summary>
    public string? Question { get; set; }

    /// <summary>
    /// Options at this version
    /// </summary>
    public JsonElement? Options { get; set; }

    /// <summary>
    /// Reasoning at this version
    /// </summary>
    public string? Reasoning { get; set; }

    /// <summary>
    /// Context at this version
    /// </summary>
    public JsonElement? Context { get; set; }
}

/// <summary>
/// Request model for reverting to a previous version
/// </summary>
public class RevertDecisionRequest
{
    /// <summary>
    /// Reason for reverting
    /// </summary>
    public string? Reason { get; set; }
}

/// <summary>
/// Response model for version comparison (diff)
/// </summary>
public class DecisionVersionDiffResponse
{
    /// <summary>
    /// The two versions being compared
    /// </summary>
    public required int FromVersion { get; set; }
    public required int ToVersion { get; set; }

    /// <summary>
    /// Changes between the versions
    /// </summary>
    public required List<FieldChange> Changes { get; set; }
}

/// <summary>
/// Represents a change to a specific field
/// </summary>
public class FieldChange
{
    /// <summary>
    /// Name of the field that changed
    /// </summary>
    public required string FieldName { get; set; }

    /// <summary>
    /// Type of change (added, removed, modified)
    /// </summary>
    public required string ChangeType { get; set; }

    /// <summary>
    /// Old value (if applicable)
    /// </summary>
    public JsonElement? OldValue { get; set; }

    /// <summary>
    /// New value (if applicable)
    /// </summary>
    public JsonElement? NewValue { get; set; }
}
