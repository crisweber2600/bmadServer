using bmadServer.ApiService.Models.Agents;

namespace bmadServer.ApiService.Services.Workflows.Agents;

/// <summary>
/// In-memory agent registry with predefined BMAD agents
/// </summary>
public class AgentRegistry : IAgentRegistry
{
    private readonly Dictionary<string, AgentDefinition> _agents;

    public AgentRegistry()
    {
        _agents = InitializeAgents();
    }

    private Dictionary<string, AgentDefinition> InitializeAgents()
    {
        var agents = new List<AgentDefinition>
        {
            new AgentDefinition
            {
                AgentId = "product-manager",
                Name = "ProductManager",
                Description = "Manages product requirements and user stories",
                Capabilities = new List<string> { "gather-requirements", "create-user-stories", "prioritize-features" },
                SystemPrompt = "You are a Product Manager responsible for understanding user needs and defining product requirements.",
                ModelPreference = "gpt-4"
            },
            new AgentDefinition
            {
                AgentId = "architect",
                Name = "Architect",
                Description = "Designs system architecture and technical solutions",
                Capabilities = new List<string> { "create-architecture", "design-system", "technical-review" },
                SystemPrompt = "You are a Software Architect responsible for designing scalable and maintainable systems.",
                ModelPreference = "gpt-4"
            },
            new AgentDefinition
            {
                AgentId = "designer",
                Name = "Designer",
                Description = "Creates user interface and user experience designs",
                Capabilities = new List<string> { "create-ui-design", "ux-research", "wireframing" },
                SystemPrompt = "You are a UX/UI Designer responsible for creating intuitive and beautiful user interfaces.",
                ModelPreference = "claude-3"
            },
            new AgentDefinition
            {
                AgentId = "developer",
                Name = "Developer",
                Description = "Implements features and writes code",
                Capabilities = new List<string> { "implement-feature", "write-code", "code-review" },
                SystemPrompt = "You are a Senior Software Developer responsible for implementing features with high quality code.",
                ModelPreference = "gpt-4"
            },
            new AgentDefinition
            {
                AgentId = "analyst",
                Name = "Analyst",
                Description = "Analyzes data and provides insights",
                Capabilities = new List<string> { "analyze-data", "generate-insights", "create-reports" },
                SystemPrompt = "You are a Business Analyst responsible for analyzing data and providing actionable insights.",
                ModelPreference = "claude-3"
            },
            new AgentDefinition
            {
                AgentId = "orchestrator",
                Name = "Orchestrator",
                Description = "Coordinates workflow execution and agent handoffs",
                Capabilities = new List<string> { "orchestrate-workflow", "coordinate-agents", "manage-handoffs" },
                SystemPrompt = "You are a Workflow Orchestrator responsible for coordinating multiple agents and ensuring smooth workflow execution.",
                ModelPreference = "gpt-4"
            }
        };

        return agents.ToDictionary(a => a.AgentId, a => a);
    }

    public IEnumerable<AgentDefinition> GetAllAgents()
    {
        return _agents.Values;
    }

    public AgentDefinition? GetAgent(string agentId)
    {
        _agents.TryGetValue(agentId, out var agent);
        return agent;
    }

    public IEnumerable<AgentDefinition> GetAgentsByCapability(string capability)
    {
        return _agents.Values
            .Where(a => a.Capabilities.Contains(capability, StringComparer.OrdinalIgnoreCase));
    }
}
