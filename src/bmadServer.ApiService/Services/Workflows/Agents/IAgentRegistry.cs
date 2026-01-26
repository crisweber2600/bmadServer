namespace bmadServer.ApiService.Services.Workflows.Agents;

/// <summary>
/// Interface for the agent registry
/// </summary>
public interface IAgentRegistry
{
    /// <summary>
    /// Get all registered agents
    /// </summary>
    IReadOnlyList<AgentDefinition> GetAllAgents();

    /// <summary>
    /// Get a specific agent by ID
    /// </summary>
    AgentDefinition? GetAgent(string agentId);

    /// <summary>
    /// Get all agents that have a specific capability
    /// </summary>
    IReadOnlyList<AgentDefinition> GetAgentsByCapability(string capability);
}
