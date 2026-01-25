using bmadServer.ServiceDefaults.Models.Agents;

namespace bmadServer.ServiceDefaults.Services.Agents;

public interface IAgentRegistry
{
    IReadOnlyList<AgentDefinition> GetAllAgents();
    AgentDefinition? GetAgent(string id);
    IReadOnlyList<AgentDefinition> GetAgentsByCapability(string capability);
}
