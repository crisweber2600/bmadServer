namespace bmadServer.ApiService.Services.Workflows.Agents;

/// <summary>
/// Interface for agent-to-agent messaging and communication
/// </summary>
public interface IAgentMessaging
{
    /// <summary>
    /// Send a request to another agent and wait for response
    /// </summary>
    /// <param name="targetAgentId">ID of the agent to send request to</param>
    /// <param name="requestType">Type of request (e.g., "get-architecture-input")</param>
    /// <param name="payload">Request payload as an object (will be serialized to JSON)</param>
    /// <param name="context">Workflow context for the request</param>
    /// <param name="timeout">Optional custom timeout (default: 30 seconds)</param>
    /// <param name="cancellationToken">Cancellation token for the request</param>
    /// <returns>Response from the target agent</returns>
    Task<AgentResponse> RequestFromAgentAsync(
        string targetAgentId,
        string requestType,
        object payload,
        WorkflowContext context,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the conversation history for a workflow instance
    /// </summary>
    /// <param name="workflowInstanceId">Workflow instance to get history for</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of messages, ordered by timestamp</returns>
    Task<List<AgentMessage>> GetConversationHistoryAsync(
        Guid workflowInstanceId,
        CancellationToken cancellationToken = default);
}
