namespace bmadServer.ApiService.Agents;

/// <summary>
/// Interface for agent-to-agent messaging.
/// Enables agents to request information from other agents during workflow execution.
/// </summary>
public interface IAgentMessaging
{
    /// <summary>
    /// Sends a request from one agent to another.
    /// </summary>
    /// <param name="targetAgentId">ID of the agent to send the request to</param>
    /// <param name="request">The request containing all necessary information</param>
    /// <param name="context">Workflow context information</param>
    /// <param name="cancellationToken">Cancellation token for request timeout</param>
    /// <returns>Response from the target agent</returns>
    Task<AgentResponse> RequestFromAgentAsync(
        string targetAgentId, 
        AgentRequest request, 
        Dictionary<string, object> context,
        CancellationToken cancellationToken = default);
}
