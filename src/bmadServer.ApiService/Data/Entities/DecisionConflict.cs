namespace bmadServer.ApiService.Data.Entities;

/// <summary>
/// Represents a detected conflict between decisions
/// </summary>
public class DecisionConflict
{
    /// <summary>
    /// Unique identifier for this conflict
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// First decision involved in the conflict
    /// </summary>
    public Guid DecisionId1 { get; set; }

    /// <summary>
    /// Second decision involved in the conflict
    /// </summary>
    public Guid DecisionId2 { get; set; }

    /// <summary>
    /// Type of conflict (e.g., "Budget", "Timeline", "Scope")
    /// </summary>
    public required string ConflictType { get; set; }

    /// <summary>
    /// Description of the nature of the conflict
    /// </summary>
    public required string Description { get; set; }

    /// <summary>
    /// Severity of the conflict (Low, Medium, High, Critical)
    /// </summary>
    public required string Severity { get; set; }

    /// <summary>
    /// When the conflict was detected
    /// </summary>
    public DateTime DetectedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Current status of the conflict (Open, Resolved, Overridden)
    /// </summary>
    public string Status { get; set; } = "Open";

    /// <summary>
    /// When the conflict was resolved
    /// </summary>
    public DateTime? ResolvedAt { get; set; }

    /// <summary>
    /// User who resolved the conflict
    /// </summary>
    public Guid? ResolvedBy { get; set; }

    /// <summary>
    /// Resolution taken (if applicable)
    /// </summary>
    public string? Resolution { get; set; }

    /// <summary>
    /// Justification for override (if conflict was overridden)
    /// </summary>
    public string? OverrideJustification { get; set; }

    /// <summary>
    /// Navigation property to first decision
    /// </summary>
    public Decision? Decision1 { get; set; }

    /// <summary>
    /// Navigation property to second decision
    /// </summary>
    public Decision? Decision2 { get; set; }

    /// <summary>
    /// Navigation property to resolver user
    /// </summary>
    public User? Resolver { get; set; }
}

/// <summary>
/// Represents a conflict detection rule
/// </summary>
public class ConflictRule
{
    /// <summary>
    /// Unique identifier for this rule
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Name of the rule
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Type of conflict this rule detects
    /// </summary>
    public required string ConflictType { get; set; }

    /// <summary>
    /// Description of what the rule checks
    /// </summary>
    public required string Description { get; set; }

    /// <summary>
    /// Rule configuration stored as JSON
    /// </summary>
    public System.Text.Json.JsonDocument? Configuration { get; set; }

    /// <summary>
    /// Whether this rule is active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Severity level for conflicts detected by this rule
    /// </summary>
    public required string Severity { get; set; }

    /// <summary>
    /// When the rule was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the rule was last modified
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
}
