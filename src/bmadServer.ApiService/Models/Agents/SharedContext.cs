using System.Text.Json;

namespace bmadServer.ApiService.Models.Agents;

/// <summary>
/// Shared workflow context accessible to all agents
/// </summary>
public class SharedContext
{
    /// <summary>
    /// Workflow instance ID
    /// </summary>
    public required Guid WorkflowInstanceId { get; init; }

    /// <summary>
    /// Version number for optimistic concurrency control
    /// </summary>
    public int Version { get; set; }

    /// <summary>
    /// All step outputs indexed by step ID
    /// </summary>
    public Dictionary<string, JsonDocument> StepOutputs { get; init; } = new();

    /// <summary>
    /// Decision history
    /// </summary>
    public List<DecisionRecord> DecisionHistory { get; init; } = new();

    /// <summary>
    /// User preferences
    /// </summary>
    public JsonDocument? UserPreferences { get; set; }

    /// <summary>
    /// Artifact references
    /// </summary>
    public List<ArtifactReference> ArtifactReferences { get; init; } = new();

    /// <summary>
    /// Timestamp of last update
    /// </summary>
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Record of a decision made during workflow execution
/// </summary>
public class DecisionRecord
{
    public required string DecisionId { get; init; }
    public required string AgentId { get; init; }
    public required string Description { get; init; }
    public required JsonDocument Data { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Reference to an artifact created during workflow execution
/// </summary>
public class ArtifactReference
{
    public required string ArtifactId { get; init; }
    public required string Type { get; init; }
    public required string Location { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}
