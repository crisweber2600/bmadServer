using bmadServer.ApiService.DTOs;
using bmadServer.ApiService.Models.Agents;

namespace bmadServer.ApiService.Services.Workflows.Agents;

/// <summary>
/// Service for managing agent handoffs and attribution
/// </summary>
public interface IAgentHandoffService
{
    /// <summary>
    /// Record an agent handoff
    /// </summary>
    Task<AgentHandoff> RecordHandoffAsync(
        string fromAgent,
        string toAgent,
        string workflowStep,
        string reason,
        Guid workflowInstanceId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get handoff history for a workflow instance
    /// </summary>
    Task<IEnumerable<AgentHandoff>> GetHandoffHistoryAsync(
        Guid workflowInstanceId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the current active agent for a workflow
    /// </summary>
    Task<string?> GetCurrentAgentAsync(
        Guid workflowInstanceId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Create a handoff indicator for UI display
    /// </summary>
    Task<AgentHandoffIndicator?> CreateHandoffIndicatorAsync(
        string agentId,
        string currentStep,
        CancellationToken cancellationToken = default);
}
