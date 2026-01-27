using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace bmadServer.ApiService.Models.Decisions;

/// <summary>
/// Response model for conflict information
/// </summary>
public class ConflictResponse
{
    /// <summary>
    /// Conflict ID
    /// </summary>
    public required Guid Id { get; set; }

    /// <summary>
    /// First decision ID
    /// </summary>
    public required Guid DecisionId1 { get; set; }

    /// <summary>
    /// Second decision ID
    /// </summary>
    public required Guid DecisionId2 { get; set; }

    /// <summary>
    /// Type of conflict
    /// </summary>
    public required string ConflictType { get; set; }

    /// <summary>
    /// Description
    /// </summary>
    public required string Description { get; set; }

    /// <summary>
    /// Severity level
    /// </summary>
    public required string Severity { get; set; }

    /// <summary>
    /// Current status
    /// </summary>
    public required string Status { get; set; }

    /// <summary>
    /// When detected
    /// </summary>
    public required DateTime DetectedAt { get; set; }

    /// <summary>
    /// When resolved
    /// </summary>
    public DateTime? ResolvedAt { get; set; }

    /// <summary>
    /// Who resolved
    /// </summary>
    public Guid? ResolvedBy { get; set; }

    /// <summary>
    /// Resolution description
    /// </summary>
    public string? Resolution { get; set; }

    /// <summary>
    /// Override justification
    /// </summary>
    public string? OverrideJustification { get; set; }

    /// <summary>
    /// Nature of conflict (alias for ConflictType for test compatibility)
    /// </summary>
    public string Nature => ConflictType;
}

/// <summary>
/// Alternative naming for ConflictResponse for test compatibility
/// </summary>
public class DecisionConflictResponse
{
    /// <summary>
    /// Conflict ID
    /// </summary>
    public required Guid Id { get; set; }

    /// <summary>
    /// First decision ID
    /// </summary>
    public required Guid DecisionId1 { get; set; }

    /// <summary>
    /// Second decision ID
    /// </summary>
    public required Guid DecisionId2 { get; set; }

    /// <summary>
    /// Type of conflict
    /// </summary>
    public required string ConflictType { get; set; }

    /// <summary>
    /// Description
    /// </summary>
    public required string Description { get; set; }

    /// <summary>
    /// Severity level
    /// </summary>
    public required string Severity { get; set; }

    /// <summary>
    /// Current status
    /// </summary>
    public required string Status { get; set; }

    /// <summary>
    /// When detected
    /// </summary>
    public required DateTime DetectedAt { get; set; }

    /// <summary>
    /// When resolved
    /// </summary>
    public DateTime? ResolvedAt { get; set; }

    /// <summary>
    /// Who resolved
    /// </summary>
    public Guid? ResolvedBy { get; set; }

    /// <summary>
    /// Resolution description
    /// </summary>
    public string? Resolution { get; set; }

    /// <summary>
    /// Override justification
    /// </summary>
    public string? OverrideJustification { get; set; }

    /// <summary>
    /// Nature of conflict
    /// </summary>
    public string Nature { get; set; } = string.Empty;
}

/// <summary>
/// Request model for resolving a conflict
/// </summary>
public class ResolveConflictRequest
{
    /// <summary>
    /// Resolution action taken
    /// </summary>
    [Required(ErrorMessage = "Resolution is required")]
    [StringLength(500, MinimumLength = 5, ErrorMessage = "Resolution must be between 5 and 500 characters")]
    public required string Resolution { get; set; }
}

/// <summary>
/// Request model for overriding a conflict
/// </summary>
public class OverrideConflictRequest
{
    /// <summary>
    /// Justification for the override
    /// </summary>
    [Required(ErrorMessage = "Justification is required")]
    [StringLength(1000, MinimumLength = 5, ErrorMessage = "Justification must be between 5 and 1000 characters")]
    public required string Justification { get; set; }
}

/// <summary>
/// Response model for conflict rule information
/// </summary>
public class ConflictRuleResponse
{
    /// <summary>
    /// Rule ID
    /// </summary>
    public required Guid Id { get; set; }

    /// <summary>
    /// Rule name
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Conflict type
    /// </summary>
    public required string ConflictType { get; set; }

    /// <summary>
    /// Description
    /// </summary>
    public required string Description { get; set; }

    /// <summary>
    /// Configuration
    /// </summary>
    public JsonElement? Configuration { get; set; }

    /// <summary>
    /// Is active
    /// </summary>
    public required bool IsActive { get; set; }

    /// <summary>
    /// Severity level
    /// </summary>
    public required string Severity { get; set; }

    /// <summary>
    /// When created
    /// </summary>
    public required DateTime CreatedAt { get; set; }

    /// <summary>
    /// When updated
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// Response for conflict comparison (side by side)
/// </summary>
public class ConflictComparisonResponse
{
    /// <summary>
    /// Conflict information
    /// </summary>
    public required ConflictResponse Conflict { get; set; }

    /// <summary>
    /// First decision details
    /// </summary>
    public required DecisionResponse Decision1 { get; set; }

    /// <summary>
    /// Second decision details
    /// </summary>
    public required DecisionResponse Decision2 { get; set; }

    /// <summary>
    /// Suggested resolutions
    /// </summary>
    public List<string> SuggestedResolutions { get; set; } = new();
}
