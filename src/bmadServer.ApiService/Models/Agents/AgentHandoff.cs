namespace bmadServer.ApiService.Models.Agents;

/// <summary>
/// Represents a handoff from one agent to another
/// </summary>
public class AgentHandoff
{
    /// <summary>
    /// Unique identifier for the handoff
    /// </summary>
    public Guid HandoffId { get; init; } = Guid.NewGuid();

    /// <summary>
    /// Agent handing off control
    /// </summary>
    public required string FromAgent { get; init; }

    /// <summary>
    /// Agent receiving control
    /// </summary>
    public required string ToAgent { get; init; }

    /// <summary>
    /// When the handoff occurred
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Workflow step where handoff occurred
    /// </summary>
    public required string WorkflowStep { get; init; }

    /// <summary>
    /// Reason for the handoff
    /// </summary>
    public required string Reason { get; init; }

    /// <summary>
    /// Workflow instance ID
    /// </summary>
    public required Guid WorkflowInstanceId { get; init; }
}
