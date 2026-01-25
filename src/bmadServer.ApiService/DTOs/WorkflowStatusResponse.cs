using System.Text.Json.Serialization;

namespace bmadServer.ApiService.DTOs;

/// <summary>
/// Response model for detailed workflow status with progress information
/// </summary>
public class WorkflowStatusResponse
{
    /// <summary>
    /// Workflow instance ID
    /// </summary>
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    /// <summary>
    /// Workflow definition ID (e.g., "create-prd")
    /// </summary>
    [JsonPropertyName("workflowId")]
    public string WorkflowId { get; set; } = string.Empty;

    /// <summary>
    /// Workflow name
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Current workflow status
    /// </summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Current step number (1-based)
    /// </summary>
    [JsonPropertyName("currentStep")]
    public int CurrentStep { get; set; }

    /// <summary>
    /// Total number of steps
    /// </summary>
    [JsonPropertyName("totalSteps")]
    public int TotalSteps { get; set; }

    /// <summary>
    /// Percentage complete (0-100)
    /// </summary>
    [JsonPropertyName("percentComplete")]
    public int PercentComplete { get; set; }

    /// <summary>
    /// When the workflow was started
    /// </summary>
    [JsonPropertyName("startedAt")]
    public DateTime? StartedAt { get; set; }

    /// <summary>
    /// Estimated completion time (nullable if cannot be estimated)
    /// </summary>
    [JsonPropertyName("estimatedCompletion")]
    public DateTime? EstimatedCompletion { get; set; }

    /// <summary>
    /// Detailed information about each step
    /// </summary>
    [JsonPropertyName("steps")]
    public List<WorkflowStepProgressDto> Steps { get; set; } = new();
}

/// <summary>
/// Progress information for a workflow step
/// </summary>
public class WorkflowStepProgressDto
{
    /// <summary>
    /// Step identifier
    /// </summary>
    [JsonPropertyName("stepId")]
    public string StepId { get; set; } = string.Empty;

    /// <summary>
    /// Step name
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Step status: Pending, Current, Completed, or Skipped
    /// </summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// When the step was completed (if applicable)
    /// </summary>
    [JsonPropertyName("completedAt")]
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Name of the agent executing this step
    /// </summary>
    [JsonPropertyName("agentName")]
    public string AgentName { get; set; } = string.Empty;
}
