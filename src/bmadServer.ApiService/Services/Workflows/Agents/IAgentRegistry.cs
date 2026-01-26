namespace bmadServer.ApiService.Services.Workflows.Agents;

/// <summary>
/// Interface for the agent registry that manages all available agents
/// </summary>
public interface IAgentRegistry
{
    /// <summary>
    /// Get all registered agents
    /// </summary>
    IReadOnlyList<AgentDefinition> GetAllAgents();

    /// <summary>
    /// Get an agent by its ID
    /// </summary>
    /// <param name="agentId">The unique identifier of the agent</param>
    /// <returns>The agent definition, or null if not found</returns>
    AgentDefinition? GetAgent(string agentId);

    /// <summary>
    /// Get all agents that have a specific capability
    /// </summary>
    /// <param name="capability">The capability to search for (e.g., "create-architecture")</param>
    /// <returns>List of agents with the specified capability</returns>
    IReadOnlyList<AgentDefinition> GetAgentsByCapability(string capability);

    /// <summary>
    /// Register a new agent definition
    /// </summary>
    /// <param name="agent">The agent definition to register</param>
    void RegisterAgent(AgentDefinition agent);
}
