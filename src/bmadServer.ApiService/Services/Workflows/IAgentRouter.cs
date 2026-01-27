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

    /// <summary>
    /// Get the model preference for an agent, respecting any configured override
    /// </summary>
    /// <param name="agentId">The agent ID</param>
    /// <returns>The model preference, or null if not configured</returns>
    string? GetModelPreference(string agentId);

    /// <summary>
    /// Set a global model override for all agents (for cost/quality tradeoffs)
    /// </summary>
    /// <param name="modelName">The model to use for all agents, or null to disable override</param>
    void SetModelOverride(string? modelName);
}
