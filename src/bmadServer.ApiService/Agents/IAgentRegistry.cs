namespace bmadServer.ApiService.Agents;

/// <summary>
/// Interface for the agent registry.
/// Provides access to all registered BMAD agents and their capabilities.
/// </summary>
public interface IAgentRegistry
{
    /// <summary>
    /// Gets all registered agents.
    /// </summary>
    /// <returns>Collection of all agent definitions</returns>
    IEnumerable<AgentDefinition> GetAllAgents();

    /// <summary>
    /// Gets a specific agent by ID.
    /// </summary>
    /// <param name="agentId">The unique agent identifier</param>
    /// <returns>Agent definition or null if not found</returns>
    AgentDefinition? GetAgent(string agentId);

    /// <summary>
    /// Gets all agents that have a specific capability.
    /// </summary>
    /// <param name="capability">The capability to search for</param>
    /// <returns>Collection of agents with the specified capability</returns>
    IEnumerable<AgentDefinition> GetAgentsByCapability(string capability);
}
