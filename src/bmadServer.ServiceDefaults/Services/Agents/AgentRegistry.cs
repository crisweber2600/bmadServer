using bmadServer.ServiceDefaults.Models.Agents;
using Microsoft.Extensions.Logging;

namespace bmadServer.ServiceDefaults.Services.Agents;

public class AgentRegistry : IAgentRegistry
{
    private readonly Dictionary<string, AgentDefinition> _agents;
    private readonly ILogger<AgentRegistry>? _logger;

    public AgentRegistry(ILogger<AgentRegistry>? logger = null)
    {
        _logger = logger;
        _agents = new Dictionary<string, AgentDefinition>(StringComparer.OrdinalIgnoreCase);
        InitializeAgents();
    }

    public IReadOnlyList<AgentDefinition> GetAllAgents()
    {
        return _agents.Values.ToList().AsReadOnly();
    }

    public AgentDefinition? GetAgent(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            _logger?.LogWarning("GetAgent called with null or empty agent id");
            return null;
        }

        if (_agents.TryGetValue(id, out var agent))
        {
            return agent;
        }

        _logger?.LogWarning("Agent with id '{AgentId}' not found", id);
        return null;
    }

    public IReadOnlyList<AgentDefinition> GetAgentsByCapability(string capability)
    {
        if (string.IsNullOrWhiteSpace(capability))
        {
            _logger?.LogWarning("GetAgentsByCapability called with null or empty capability");
            return new List<AgentDefinition>().AsReadOnly();
        }

        var matchingAgents = _agents.Values
            .Where(a => a.Capabilities.Contains(capability, StringComparer.OrdinalIgnoreCase))
            .ToList();

        _logger?.LogDebug("Found {Count} agents with capability '{Capability}'", matchingAgents.Count, capability);

        return matchingAgents.AsReadOnly();
    }

    private void InitializeAgents()
    {
        // Register all BMAD agents from the static definitions
        foreach (var agent in BmadAgentDefinitions.AllAgents)
        {
            _agents[agent.AgentId] = agent;
        }

        _logger?.LogInformation("Initialized {Count} BMAD agents", _agents.Count);
    }
}
