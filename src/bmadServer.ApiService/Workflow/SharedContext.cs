namespace bmadServer.ApiService.WorkflowContext;

/// <summary>
/// Represents the shared workflow context accessible to all agents in a workflow instance.
/// Contains step outputs, decision history, user preferences, and artifact references.
/// Implements optimistic concurrency control with version tracking.
/// </summary>
public class SharedContext
{
    /// <summary>
    /// Unique identifier for the workflow instance.
    /// </summary>
    public required Guid WorkflowInstanceId { get; init; }

    /// <summary>
    /// All step outputs indexed by step ID.
    /// </summary>
    public Dictionary<string, StepOutput> StepOutputs { get; init; } = new();

    /// <summary>
    /// History of all decisions made during the workflow.
    /// </summary>
    public List<Decision> DecisionHistory { get; init; } = new();

    /// <summary>
    /// User preferences for this workflow instance.
    /// </summary>
    public UserPreferences? UserPreferences { get; init; }

    /// <summary>
    /// References to artifacts created during the workflow.
    /// </summary>
    public List<ArtifactReference> ArtifactReferences { get; init; } = new();

    /// <summary>
    /// Version number for optimistic concurrency control.
    /// Incremented on each update.
    /// </summary>
    public int Version { get; set; } = 1;

    /// <summary>
    /// When the context was last modified.
    /// </summary>
    public DateTime LastModifiedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Summarized version of older context when exceeding token limits.
    /// </summary>
    public string? ContextSummary { get; set; }

    /// <summary>
    /// Estimated token count for the current context.
    /// </summary>
    public int EstimatedTokenCount { get; set; }

    /// <summary>
    /// Maximum token count before summarization triggers (default 8000).
    /// </summary>
    public const int MaxTokenCount = 8000;

    /// <summary>
    /// Retrieves the output from a specific workflow step.
    /// </summary>
    /// <param name="stepId">The ID of the step whose output to retrieve.</param>
    /// <returns>The step output if the step has completed, otherwise null.</returns>
    public StepOutput? GetStepOutput(string stepId)
    {
        return StepOutputs.TryGetValue(stepId, out var output) ? output : null;
    }

    /// <summary>
    /// Adds a step output to the context.
    /// </summary>
    /// <param name="stepId">The ID of the step.</param>
    /// <param name="output">The step output to add.</param>
    public void AddStepOutput(string stepId, StepOutput output)
    {
        StepOutputs[stepId] = output;
        Version++;
        LastModifiedAt = DateTime.UtcNow;
        UpdateTokenCount();
    }

    /// <summary>
    /// Adds a decision to the decision history.
    /// </summary>
    /// <param name="decision">The decision to add.</param>
    public void AddDecision(Decision decision)
    {
        DecisionHistory.Add(decision);
        Version++;
        LastModifiedAt = DateTime.UtcNow;
        UpdateTokenCount();
    }

    /// <summary>
    /// Adds an artifact reference to the context.
    /// </summary>
    /// <param name="artifact">The artifact reference to add.</param>
    public void AddArtifactReference(ArtifactReference artifact)
    {
        ArtifactReferences.Add(artifact);
        Version++;
        LastModifiedAt = DateTime.UtcNow;
        UpdateTokenCount();
    }

    /// <summary>
    /// Updates the estimated token count and triggers summarization if needed.
    /// </summary>
    private void UpdateTokenCount()
    {
        // Simple estimation: ~4 characters per token
        var contentLength = System.Text.Json.JsonSerializer.Serialize(this).Length;
        EstimatedTokenCount = contentLength / 4;

        if (EstimatedTokenCount > MaxTokenCount && string.IsNullOrEmpty(ContextSummary))
        {
            SummarizeOlderContext();
        }
    }

    /// <summary>
    /// Summarizes older context while preserving key decisions.
    /// </summary>
    private void SummarizeOlderContext()
    {
        // Create a summary of older step outputs
        var oldSteps = StepOutputs
            .OrderBy(kvp => kvp.Value.CompletedAt)
            .Take(StepOutputs.Count / 2)
            .ToList();

        var summaryParts = new List<string>
        {
            $"Summary of {oldSteps.Count} earlier steps:"
        };

        foreach (var (stepId, output) in oldSteps)
        {
            summaryParts.Add($"- {stepId}: {output.Summary ?? "Completed"}");
        }

        // Preserve all key decisions
        summaryParts.Add("\nKey Decisions:");
        foreach (var decision in DecisionHistory.OrderBy(d => d.Timestamp))
        {
            summaryParts.Add($"- {decision.DecisionType} by {decision.MadeBy}: {decision.Rationale}");
        }

        ContextSummary = string.Join("\n", summaryParts);
    }
}

/// <summary>
/// Represents the output from a completed workflow step.
/// </summary>
public class StepOutput
{
    /// <summary>
    /// The ID of the step that produced this output.
    /// </summary>
    public required string StepId { get; init; }

    /// <summary>
    /// The actual output data from the step.
    /// </summary>
    public required object Data { get; init; }

    /// <summary>
    /// When the step completed.
    /// </summary>
    public required DateTime CompletedAt { get; init; }

    /// <summary>
    /// The agent that completed this step.
    /// </summary>
    public required string CompletedByAgent { get; init; }

    /// <summary>
    /// Optional summary of the step output for context summarization.
    /// </summary>
    public string? Summary { get; init; }

    /// <summary>
    /// Metadata about the step execution.
    /// </summary>
    public Dictionary<string, object>? Metadata { get; init; }
}

/// <summary>
/// Represents a decision made during the workflow.
/// </summary>
public class Decision
{
    /// <summary>
    /// Unique identifier for the decision.
    /// </summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    /// Type of decision (e.g., "architecture-choice", "design-pattern").
    /// </summary>
    public required string DecisionType { get; init; }

    /// <summary>
    /// Who made the decision (agent ID or user ID).
    /// </summary>
    public required string MadeBy { get; init; }

    /// <summary>
    /// When the decision was made.
    /// </summary>
    public required DateTime Timestamp { get; init; }

    /// <summary>
    /// Rationale for the decision.
    /// </summary>
    public required string Rationale { get; init; }

    /// <summary>
    /// The actual decision content/value.
    /// </summary>
    public required object DecisionValue { get; init; }

    /// <summary>
    /// Optional context at the time of decision.
    /// </summary>
    public Dictionary<string, object>? Context { get; init; }
}

/// <summary>
/// User preferences for the workflow.
/// </summary>
public class UserPreferences
{
    /// <summary>
    /// Display settings (e.g., verbosity level, formatting preferences).
    /// </summary>
    public Dictionary<string, object> DisplaySettings { get; init; } = new();

    /// <summary>
    /// Model preferences for different agents.
    /// </summary>
    public Dictionary<string, string> ModelPreferences { get; init; } = new();

    /// <summary>
    /// Language preferences.
    /// </summary>
    public string? PreferredLanguage { get; init; }

    /// <summary>
    /// Custom preferences specific to this user.
    /// </summary>
    public Dictionary<string, object> CustomPreferences { get; init; } = new();
}

/// <summary>
/// Reference to an artifact created during the workflow.
/// </summary>
public class ArtifactReference
{
    /// <summary>
    /// Unique identifier for the artifact.
    /// </summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    /// Type of artifact (e.g., "diagram", "document", "code-snippet").
    /// </summary>
    public required string ArtifactType { get; init; }

    /// <summary>
    /// Storage location (URL, file path, or database reference).
    /// </summary>
    public required string StorageLocation { get; init; }

    /// <summary>
    /// When the artifact was created.
    /// </summary>
    public required DateTime CreatedAt { get; init; }

    /// <summary>
    /// The step that created this artifact.
    /// </summary>
    public required string CreatedByStep { get; init; }

    /// <summary>
    /// Optional description of the artifact.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Metadata about the artifact.
    /// </summary>
    public Dictionary<string, object>? Metadata { get; init; }
}
