namespace bmadServer.ApiService.DTOs;

/// <summary>
/// Represents a handoff event between agents with full attribution details.
/// Used in audit log responses and real-time handoff notifications.
/// </summary>
public class AgentHandoffDto
{
    /// <summary>
    /// Attribution details for the agent handing off responsibility.
    /// </summary>
    public required AgentAttributionDto FromAgent { get; set; }

    /// <summary>
    /// Attribution details for the agent receiving responsibility.
    /// </summary>
    public required AgentAttributionDto ToAgent { get; set; }

    /// <summary>
    /// Timestamp when the handoff occurred (UTC).
    /// </summary>
    public required DateTimeOffset Timestamp { get; set; }

    /// <summary>
    /// Name of the workflow step where the handoff occurred.
    /// </summary>
    public required string StepName { get; set; }

    /// <summary>
    /// Workflow step ID for correlation with step definitions.
    /// </summary>
    public required string StepId { get; set; }

    /// <summary>
    /// Reason for the handoff (e.g., "Step requires architect expertise").
    /// Non-PII, generic description of why handoff occurred.
    /// </summary>
    public string? Reason { get; set; }
}
