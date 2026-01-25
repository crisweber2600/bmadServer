namespace bmadServer.ApiService.Agents;

/// <summary>
/// Service for tracking and managing agent handoffs in workflows.
/// Provides handoff notifications, attribution, and audit logging.
/// </summary>
public interface IAgentHandoffService
{
    /// <summary>
    /// Records a handoff from one agent to another.
    /// </summary>
    /// <param name="workflowInstanceId">The workflow instance ID.</param>
    /// <param name="fromAgent">The agent handing off (null for initial agent).</param>
    /// <param name="toAgent">The agent receiving control.</param>
    /// <param name="workflowStep">The current workflow step.</param>
    /// <param name="reason">Reason for the handoff.</param>
    /// <returns>The created handoff record.</returns>
    Task<AgentHandoffRecord> RecordHandoffAsync(
        Guid workflowInstanceId,
        string? fromAgent,
        string toAgent,
        string workflowStep,
        string? reason = null);

    /// <summary>
    /// Gets all handoffs for a workflow instance.
    /// </summary>
    /// <param name="workflowInstanceId">The workflow instance ID.</param>
    /// <returns>List of handoff records ordered by timestamp.</returns>
    Task<List<AgentHandoffRecord>> GetHandoffsAsync(Guid workflowInstanceId);

    /// <summary>
    /// Gets the current agent for a workflow instance.
    /// </summary>
    /// <param name="workflowInstanceId">The workflow instance ID.</param>
    /// <returns>The current agent ID, or null if no handoffs recorded.</returns>
    Task<string?> GetCurrentAgentAsync(Guid workflowInstanceId);

    /// <summary>
    /// Gets agent details for display in tooltips.
    /// </summary>
    /// <param name="agentId">The agent ID.</param>
    /// <param name="workflowStep">Optional current step for context.</param>
    /// <returns>Agent details for UI display.</returns>
    Task<AgentDetails?> GetAgentDetailsAsync(string agentId, string? workflowStep = null);
}

/// <summary>
/// Represents a handoff record with full agent details.
/// </summary>
public class AgentHandoffRecord
{
    public Guid Id { get; init; }
    public Guid WorkflowInstanceId { get; init; }
    public string? FromAgent { get; init; }
    public required string ToAgent { get; init; }
    public required DateTime Timestamp { get; init; }
    public required string WorkflowStep { get; init; }
    public string? Reason { get; init; }
    public required string ToAgentName { get; init; }
    public string? FromAgentName { get; init; }
}

/// <summary>
/// Agent details for UI display and tooltips.
/// </summary>
public class AgentDetails
{
    public required string AgentId { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required List<string> Capabilities { get; init; }
    public string? CurrentStepResponsibility { get; init; }
    public string? Avatar { get; init; }
}
