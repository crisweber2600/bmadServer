namespace bmadServer.ApiService.Services.Workflows.Agents;

/// <summary>
/// Interface for tracking agent handoffs
/// </summary>
public interface IAgentHandoffTracker
{
    /// <summary>
    /// Record an agent handoff
    /// </summary>
    void RecordHandoff(AgentHandoff handoff);

    /// <summary>
    /// Get handoff history for a workflow
    /// </summary>
    IReadOnlyList<AgentHandoff> GetHandoffHistory(Guid workflowInstanceId);

    /// <summary>
    /// Get the current agent for a workflow
    /// </summary>
    string? GetCurrentAgent(Guid workflowInstanceId);
}
