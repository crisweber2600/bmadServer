using bmadServer.ApiService.Models.Agents;

namespace bmadServer.ApiService.Services.Workflows.Agents;

/// <summary>
/// Registry for managing and querying agent definitions
/// </summary>
public interface IAgentRegistry
{
    /// <summary>
    /// Get all registered agents
    /// </summary>
    IEnumerable<AgentDefinition> GetAllAgents();

    /// <summary>
    /// Get a specific agent by ID
    /// </summary>
    AgentDefinition? GetAgent(string agentId);

    /// <summary>
    /// Get all agents that have a specific capability
    /// </summary>
    IEnumerable<AgentDefinition> GetAgentsByCapability(string capability);
}
