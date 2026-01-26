using System.Text.Json;
using System.ComponentModel.DataAnnotations;

namespace bmadServer.ApiService.Models.Workflows;

/// <summary>
/// Represents shared context available to all agents in a workflow.
/// Accumulates outputs, decisions, preferences, and artifact references across all workflow steps.
/// </summary>
public class SharedContext
{
    /// <summary>
    /// Outputs from all completed steps, indexed by stepId.
    /// Agents can query previous step outputs to make informed decisions.
    /// </summary>
    [Required]
    public Dictionary<string, JsonDocument> StepOutputs { get; set; } = new();

    /// <summary>
    /// History of all decisions made across workflow steps.
    /// Preserved in full for audit trail and decision context.
    /// </summary>
    [Required]
    public List<DecisionRecord> DecisionHistory { get; set; } = new();

    /// <summary>
    /// User preferences for agent personalization.
    /// Examples: verbosityLevel, technicalDepth, outputFormat
    /// </summary>
    [Required]
    public Dictionary<string, string> UserPreferences { get; set; } = new();

    /// <summary>
    /// References to generated artifacts (documents, diagrams, files).
    /// Enables traceability of what was generated at each step.
    /// </summary>
    [Required]
    public List<ArtifactReference> ArtifactReferences { get; set; } = new();

    // ========== CONCURRENCY CONTROL ==========
    // These fields implement optimistic concurrency control per architecture.md pattern

    /// <summary>
    /// Version number for optimistic concurrency control.
    /// Incremented on each successful update to detect conflicts.
    /// </summary>
    public int Version { get; set; } = 1;

    /// <summary>
    /// UTC timestamp of last modification.
    /// Used for audit trail and debugging concurrent access.
    /// </summary>
    public DateTime LastModifiedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Agent or system ID that last modified this context.
    /// Used for audit trail and attribution.
    /// </summary>
    public string LastModifiedBy { get; set; } = string.Empty;
}

/// <summary>
/// Record of a decision made during workflow execution.
/// Preserved in full throughout workflow lifecycle for audit and context.
/// </summary>
public class DecisionRecord
{
    /// <summary>
    /// The step ID where this decision was made.
    /// </summary>
    [Required]
    public required string StepId { get; init; }

    /// <summary>
    /// The decision value or choice made.
    /// Examples: "selected-architecture-pattern", "approved-design"
    /// </summary>
    [Required]
    public required string Decision { get; init; }

    /// <summary>
    /// UTC timestamp when decision was made.
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Optional agent ID that made this decision.
    /// Can be null for human decisions.
    /// </summary>
    public string? AgentId { get; init; }

    /// <summary>
    /// Optional reasoning or explanation for the decision.
    /// Provides context for future agents and human review.
    /// </summary>
    public string? Reasoning { get; init; }
}

/// <summary>
/// Reference to an artifact (file/document) generated during workflow.
/// Enables agents to access and track generated outputs.
/// </summary>
public class ArtifactReference
{
    /// <summary>
    /// The step ID where this artifact was created.
    /// </summary>
    [Required]
    public required string StepId { get; init; }

    /// <summary>
    /// Unique identifier for the artifact.
    /// Examples: "doc-123", "diagram-456"
    /// </summary>
    [Required]
    public required string ArtifactId { get; init; }

    /// <summary>
    /// Type of artifact for categorization.
    /// Examples: "architecture-diagram", "specification-document", "test-report"
    /// </summary>
    [Required]
    public required string ArtifactType { get; init; }

    /// <summary>
    /// File system path or URL to access the artifact.
    /// </summary>
    [Required]
    public required string Path { get; init; }

    /// <summary>
    /// UTC timestamp when artifact was created.
    /// </summary>
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}
