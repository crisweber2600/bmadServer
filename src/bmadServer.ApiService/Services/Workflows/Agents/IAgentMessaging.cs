namespace bmadServer.ApiService.Services.Workflows.Agents;

/// <summary>
/// Interface for agent-to-agent messaging
/// </summary>
public interface IAgentMessaging
{
    /// <summary>
    /// Request information from another agent
    /// </summary>
    Task<AgentMessage> RequestFromAgent(
        string targetAgentId,
        string request,
        AgentMessageContext context,
        int timeoutSeconds = 30);

    /// <summary>
    /// Get message history for a workflow
    /// </summary>
    IReadOnlyList<AgentMessage> GetMessageHistory(Guid workflowInstanceId);
}
