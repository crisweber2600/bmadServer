using bmadServer.ApiService.Services.Workflows.Agents;

namespace bmadServer.ApiService.Services.Workflows;

/// <summary>
/// Interface for routing workflow steps to appropriate agent handlers
/// </summary>
public interface IAgentRouter
{
    /// <summary>
    /// Get the agent handler for the specified agent ID
    /// </summary>
    /// <param name="agentId">The agent ID from the workflow step definition</param>
    /// <returns>The agent handler, or null if not found</returns>
    IAgentHandler? GetHandler(string agentId);
    
    /// <summary>
    /// Register an agent handler
    /// </summary>
    void RegisterHandler(string agentId, IAgentHandler handler);
}
