namespace bmadServer.ApiService.Services.Workflows.Agents;

/// <summary>
/// Registry of all BMAD agents and their capabilities
/// </summary>
public class AgentRegistry : IAgentRegistry
{
    private readonly List<AgentDefinition> _agents;
    private readonly Dictionary<string, AgentDefinition> _agentsByIddictionary;

    public AgentRegistry()
    {
        _agents = InitializeAgents();
        _agentsByIddictionary = _agents.ToDictionary(a => a.AgentId, StringComparer.OrdinalIgnoreCase);
    }

    /// <inheritdoc />
    public IReadOnlyList<AgentDefinition> GetAllAgents()
    {
        return _agents.AsReadOnly();
    }

    /// <inheritdoc />
    public AgentDefinition? GetAgent(string agentId)
    {
        if (string.IsNullOrWhiteSpace(agentId))
        {
            return null;
        }

        return _agentsByIddictionary.GetValueOrDefault(agentId);
    }

    /// <inheritdoc />
    public IReadOnlyList<AgentDefinition> GetAgentsByCapability(string capability)
    {
        if (string.IsNullOrWhiteSpace(capability))
        {
            return Array.Empty<AgentDefinition>();
        }

        return _agents
            .Where(a => a.Capabilities.Contains(capability, StringComparer.OrdinalIgnoreCase))
            .ToList()
            .AsReadOnly();
    }

    private static List<AgentDefinition> InitializeAgents()
    {
        return new List<AgentDefinition>
        {
            new AgentDefinition
            {
                AgentId = "product-manager",
                Name = "ProductManager",
                Description = "Manages product requirements and user stories",
                Capabilities = new List<string>
                {
                    "requirements-gathering",
                    "story-creation",
                    "backlog-prioritization",
                    "stakeholder-communication"
                },
                SystemPrompt = "You are a Product Manager responsible for gathering requirements, creating user stories, and prioritizing the product backlog. Focus on business value and user needs.",
                ModelPreference = "gpt-4"
            },
            new AgentDefinition
            {
                AgentId = "architect",
                Name = "Architect",
                Description = "Designs system architecture and technical solutions",
                Capabilities = new List<string>
                {
                    "create-architecture",
                    "technical-design",
                    "system-modeling",
                    "technology-selection"
                },
                SystemPrompt = "You are a Software Architect responsible for designing robust, scalable system architectures. Focus on best practices, patterns, and long-term maintainability.",
                ModelPreference = "gpt-4"
            },
            new AgentDefinition
            {
                AgentId = "designer",
                Name = "Designer",
                Description = "Creates user interface and user experience designs",
                Capabilities = new List<string>
                {
                    "ui-design",
                    "ux-design",
                    "wireframing",
                    "user-flow-design"
                },
                SystemPrompt = "You are a UX/UI Designer responsible for creating intuitive, accessible, and visually appealing user interfaces. Focus on user experience and design best practices.",
                ModelPreference = "gpt-4"
            },
            new AgentDefinition
            {
                AgentId = "developer",
                Name = "Developer",
                Description = "Implements features and writes code",
                Capabilities = new List<string>
                {
                    "code-implementation",
                    "unit-testing",
                    "code-review",
                    "bug-fixing",
                    "refactoring"
                },
                SystemPrompt = "You are a Senior Software Developer responsible for implementing features with high-quality, well-tested code. Follow SOLID principles, write clean code, and ensure comprehensive test coverage.",
                ModelPreference = "gpt-4"
            },
            new AgentDefinition
            {
                AgentId = "analyst",
                Name = "Analyst",
                Description = "Analyzes requirements and creates test scenarios",
                Capabilities = new List<string>
                {
                    "requirements-analysis",
                    "test-planning",
                    "acceptance-criteria",
                    "quality-assurance"
                },
                SystemPrompt = "You are a Business Analyst and QA Specialist responsible for analyzing requirements, defining acceptance criteria, and ensuring quality standards are met.",
                ModelPreference = "gpt-4"
            },
            new AgentDefinition
            {
                AgentId = "orchestrator",
                Name = "Orchestrator",
                Description = "Coordinates workflow execution and agent handoffs",
                Capabilities = new List<string>
                {
                    "workflow-orchestration",
                    "agent-coordination",
                    "decision-routing",
                    "handoff-management"
                },
                SystemPrompt = "You are a Workflow Orchestrator responsible for coordinating multiple agents, managing handoffs, and ensuring smooth workflow execution.",
                ModelPreference = "gpt-3.5-turbo"
            }
        };
    }
}
