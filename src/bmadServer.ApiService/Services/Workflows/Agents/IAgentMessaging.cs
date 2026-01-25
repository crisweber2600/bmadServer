using System.Text.Json;
using bmadServer.ApiService.Models.Agents;

namespace bmadServer.ApiService.Services.Workflows.Agents;

/// <summary>
/// Service for agent-to-agent messaging
/// </summary>
public interface IAgentMessaging
{
    /// <summary>
    /// Request information from another agent
    /// </summary>
    /// <param name="targetAgentId">ID of the agent to request from</param>
    /// <param name="request">Request details</param>
    /// <param name="context">Workflow context</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Response from the target agent</returns>
    Task<AgentResponse> RequestFromAgentAsync(
        string targetAgentId,
        AgentRequest request,
        JsonDocument? context,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get message history for a workflow instance
    /// </summary>
    /// <param name="workflowInstanceId">Workflow instance ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of messages</returns>
    Task<IEnumerable<AgentMessage>> GetMessageHistoryAsync(
        Guid workflowInstanceId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Response from an agent
/// </summary>
public class AgentResponse
{
    /// <summary>
    /// Whether the request was successful
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Response payload
    /// </summary>
    public JsonDocument? Response { get; init; }

    /// <summary>
    /// Error message if request failed
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Whether the request timed out
    /// </summary>
    public bool TimedOut { get; init; }
}
