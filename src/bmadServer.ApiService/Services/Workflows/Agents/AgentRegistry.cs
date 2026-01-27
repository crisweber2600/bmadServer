namespace bmadServer.ApiService.Services.Workflows.Agents;

/// <summary>
/// Implementation of the agent registry that manages all available BMAD agents
/// </summary>
public class AgentRegistry : IAgentRegistry
{
    private readonly Dictionary<string, AgentDefinition> _agents = new(StringComparer.OrdinalIgnoreCase);
    private readonly ILogger<AgentRegistry> _logger;

    public AgentRegistry(ILogger<AgentRegistry> logger)
    {
        _logger = logger;
        InitializeDefaultAgents();
    }

    /// <inheritdoc />
    public IReadOnlyList<AgentDefinition> GetAllAgents()
    {
        _logger.LogDebug("Retrieving all {AgentCount} registered agents", _agents.Count);
        return _agents.Values.ToList().AsReadOnly();
    }

    /// <inheritdoc />
    public AgentDefinition? GetAgent(string agentId)
    {
        if (string.IsNullOrWhiteSpace(agentId))
        {
            _logger.LogWarning("Attempted to get agent with null or empty ID");
            return null;
        }

        if (_agents.TryGetValue(agentId, out var agent))
        {
            _logger.LogDebug("Found agent {AgentId}", agentId);
            return agent;
        }

        _logger.LogWarning("Agent not found: {AgentId}", agentId);
        return null;
    }

    /// <inheritdoc />
    public IReadOnlyList<AgentDefinition> GetAgentsByCapability(string capability)
    {
        if (string.IsNullOrWhiteSpace(capability))
        {
            _logger.LogWarning("Attempted to filter agents by null or empty capability");
            return [];
        }

        var matchingAgents = _agents.Values
            .Where(a => a.Capabilities.Contains(capability, StringComparer.OrdinalIgnoreCase))
            .ToList();

        _logger.LogDebug("Found {AgentCount} agents with capability {Capability}", matchingAgents.Count, capability);
        return matchingAgents.AsReadOnly();
    }

    /// <inheritdoc />
    public void RegisterAgent(AgentDefinition agent)
    {
        if (agent == null)
        {
            throw new ArgumentNullException(nameof(agent));
        }

        if (string.IsNullOrWhiteSpace(agent.AgentId))
        {
            throw new ArgumentException("Agent ID cannot be null or empty", nameof(agent));
        }

        _agents[agent.AgentId] = agent;
        _logger.LogInformation("Registered agent {AgentId} with {CapabilityCount} capabilities", 
            agent.AgentId, agent.Capabilities.Count);
    }

    /// <summary>
    /// Initialize the registry with default BMAD agents
    /// </summary>
    private void InitializeDefaultAgents()
    {
        var agents = new[]
        {
             new AgentDefinition
             {
                 AgentId = "product-manager",
                 Name = "Product Manager",
                 Description = "Gathers requirements and translates business needs into specifications",
                 SystemPrompt = "You are a product manager for a software development project. Your role is to gather requirements, understand user needs, and translate business objectives into clear technical specifications.",
                 Capabilities = ["gather-requirements", "create-specifications", "analyze-market", "prioritize-features"],
                 ModelPreference = "gpt-4",
                 Temperature = 0.7m
             },
             new AgentDefinition
             {
                 AgentId = "architect",
                 Name = "Architect",
                 Description = "Designs system architecture and technical solutions",
                 SystemPrompt = "You are a senior solutions architect. Your role is to design scalable, maintainable system architectures that meet both functional and non-functional requirements.",
                 Capabilities = ["create-architecture", "design-system", "evaluate-tradeoffs", "plan-migration"],
                 ModelPreference = "gpt-4-turbo",
                 Temperature = 0.7m
             },
             new AgentDefinition
             {
                 AgentId = "designer",
                 Name = "Designer",
                 Description = "Creates user experience and interface designs",
                 SystemPrompt = "You are a UX/UI designer. Your role is to create intuitive, accessible, and visually appealing user interfaces and experiences.",
                 Capabilities = ["create-ui-design", "design-ux-flow", "evaluate-usability", "create-wireframes"],
                 ModelPreference = "gpt-4",
                 Temperature = 0.8m
             },
             new AgentDefinition
             {
                 AgentId = "developer",
                 Name = "Developer",
                 Description = "Implements features and writes production code",
                 SystemPrompt = "You are a senior software developer. Your role is to implement features, write clean and maintainable code, and ensure code quality through testing and best practices.",
                 Capabilities = ["write-code", "implement-feature", "write-tests", "refactor-code", "fix-bugs"],
                 ModelPreference = "gpt-4-turbo",
                 Temperature = 0.5m
             },
             new AgentDefinition
             {
                 AgentId = "analyst",
                 Name = "Analyst",
                 Description = "Analyzes requirements, data, and provides insights",
                 SystemPrompt = "You are a business and technical analyst. Your role is to analyze requirements, identify risks and opportunities, and provide data-driven insights.",
                 Capabilities = ["analyze-requirements", "identify-risks", "analyze-data", "provide-recommendations"],
                 ModelPreference = "gpt-4",
                 Temperature = 0.6m
             },
             new AgentDefinition
             {
                 AgentId = "orchestrator",
                 Name = "Orchestrator",
                 Description = "Coordinates between agents and manages workflow execution",
                 SystemPrompt = "You are a workflow orchestrator. Your role is to coordinate between different agents, manage task distribution, and ensure smooth workflow execution.",
                 Capabilities = ["coordinate-agents", "manage-workflow", "route-tasks", "aggregate-results"],
                 ModelPreference = "gpt-4-turbo",
                 Temperature = 0.6m
             }
        };

        foreach (var agent in agents)
        {
            RegisterAgent(agent);
        }

        _logger.LogInformation("Initialized agent registry with {AgentCount} default agents", agents.Length);
    }
}
