using System.Text.Json;

namespace bmadServer.ApiService.Data.Entities;

/// <summary>
/// Represents a version in the history of a decision.
/// Each time a decision is modified, a new version is created.
/// </summary>
public class DecisionVersion
{
    /// <summary>
    /// Unique identifier for this version record
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Reference to the decision this version belongs to
    /// </summary>
    public Guid DecisionId { get; set; }

    /// <summary>
    /// Version number (starts at 1, increments with each change)
    /// </summary>
    public int VersionNumber { get; set; }

    /// <summary>
    /// The decision value at this version (JSONB)
    /// </summary>
    public JsonDocument? Value { get; set; }

    /// <summary>
    /// User who made this modification
    /// </summary>
    public Guid ModifiedBy { get; set; }

    /// <summary>
    /// Timestamp when this version was created
    /// </summary>
    public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Reason for the change (optional)
    /// </summary>
    public string? ChangeReason { get; set; }

    /// <summary>
    /// Question at this version (optional)
    /// </summary>
    public string? Question { get; set; }

    /// <summary>
    /// Options presented at this version (JSONB)
    /// </summary>
    public JsonDocument? Options { get; set; }

    /// <summary>
    /// Reasoning at this version (optional)
    /// </summary>
    public string? Reasoning { get; set; }

    /// <summary>
    /// Context at this version (JSONB)
    /// </summary>
    public JsonDocument? Context { get; set; }

    /// <summary>
    /// Navigation property to the decision
    /// </summary>
    public Decision? Decision { get; set; }

    /// <summary>
    /// Navigation property to the user who modified
    /// </summary>
    public User? Modifier { get; set; }
}
