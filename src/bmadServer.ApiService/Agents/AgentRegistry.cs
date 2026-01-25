namespace bmadServer.ApiService.Agents;

/// <summary>
/// Central registry for all BMAD agents.
/// Provides lookup by ID, capability, and retrieval of all agents.
/// </summary>
public class AgentRegistry : IAgentRegistry
{
    private readonly List<AgentDefinition> _agents;

    public AgentRegistry()
    {
        _agents = InitializeAgents();
    }

    /// <summary>
    /// Gets all registered agents.
    /// </summary>
    /// <returns>Collection of all agent definitions</returns>
    public IEnumerable<AgentDefinition> GetAllAgents()
    {
        return _agents;
    }

    /// <summary>
    /// Gets a specific agent by ID.
    /// </summary>
    /// <param name="agentId">The unique agent identifier</param>
    /// <returns>Agent definition or null if not found</returns>
    public AgentDefinition? GetAgent(string agentId)
    {
        return _agents.FirstOrDefault(a => a.AgentId.Equals(agentId, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Gets all agents that have a specific capability.
    /// </summary>
    /// <param name="capability">The capability to search for</param>
    /// <returns>Collection of agents with the specified capability</returns>
    public IEnumerable<AgentDefinition> GetAgentsByCapability(string capability)
    {
        return _agents.Where(a => a.Capabilities.Contains(capability, StringComparer.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Initializes the BMAD agent registry with all core agents.
    /// </summary>
    private static List<AgentDefinition> InitializeAgents()
    {
        return new List<AgentDefinition>
        {
            new AgentDefinition
            {
                AgentId = "product-manager",
                Name = "Product Manager",
                Description = "Defines product requirements, user stories, and acceptance criteria. Creates PRDs and manages product vision.",
                Capabilities = new List<string>
                {
                    "create-prd",
                    "define-user-stories",
                    "prioritize-backlog",
                    "gather-requirements",
                    "validate-acceptance-criteria"
                },
                SystemPrompt = @"You are a Product Manager agent. Your role is to:
- Define clear product requirements and specifications
- Create comprehensive Product Requirements Documents (PRDs)
- Write detailed user stories with acceptance criteria
- Prioritize features based on business value and user needs
- Ensure all requirements are measurable and testable
- Collaborate with stakeholders to gather and refine requirements",
                ModelPreference = "gpt-4"
            },
            new AgentDefinition
            {
                AgentId = "architect",
                Name = "Architect",
                Description = "Designs system architecture, technical decisions, and data models. Creates architecture diagrams and technical specifications.",
                Capabilities = new List<string>
                {
                    "create-architecture",
                    "review-architecture",
                    "design-data-model",
                    "select-technology",
                    "create-technical-spec"
                },
                SystemPrompt = @"You are an Architect agent. Your role is to:
- Design scalable and maintainable system architectures
- Make informed technical decisions based on requirements
- Create data models and database schemas
- Select appropriate technologies and frameworks
- Document architectural decisions and patterns
- Ensure solutions align with best practices and standards",
                ModelPreference = "gpt-4"
            },
            new AgentDefinition
            {
                AgentId = "designer",
                Name = "Designer",
                Description = "Creates UI/UX designs, wireframes, and user interface specifications. Ensures consistent design patterns.",
                Capabilities = new List<string>
                {
                    "create-wireframes",
                    "design-ui",
                    "create-user-flows",
                    "design-components",
                    "validate-ux"
                },
                SystemPrompt = @"You are a Designer agent. Your role is to:
- Create intuitive and user-friendly interface designs
- Design wireframes and mockups for user interfaces
- Develop consistent design patterns and component libraries
- Ensure excellent user experience (UX)
- Create user flows and interaction designs
- Validate designs against usability standards",
                ModelPreference = "gpt-4"
            },
            new AgentDefinition
            {
                AgentId = "developer",
                Name = "Developer",
                Description = "Implements features, writes code, and creates tests. Follows architecture and design specifications.",
                Capabilities = new List<string>
                {
                    "implement-feature",
                    "write-tests",
                    "fix-bugs",
                    "refactor-code",
                    "code-review"
                },
                SystemPrompt = @"You are a Developer agent. Your role is to:
- Implement features according to specifications
- Write clean, maintainable, and well-tested code
- Create comprehensive unit and integration tests
- Follow coding standards and best practices
- Fix bugs and resolve technical issues
- Perform code reviews and provide constructive feedback",
                ModelPreference = "gpt-4"
            },
            new AgentDefinition
            {
                AgentId = "analyst",
                Name = "Analyst",
                Description = "Analyzes data, generates reports, and provides insights. Validates metrics and monitors system performance.",
                Capabilities = new List<string>
                {
                    "analyze-data",
                    "generate-reports",
                    "validate-metrics",
                    "monitor-performance",
                    "create-insights"
                },
                SystemPrompt = @"You are an Analyst agent. Your role is to:
- Analyze data to extract meaningful insights
- Generate comprehensive reports and visualizations
- Validate that metrics meet acceptance criteria
- Monitor system performance and identify issues
- Provide data-driven recommendations
- Track KPIs and success metrics",
                ModelPreference = "gpt-4"
            },
            new AgentDefinition
            {
                AgentId = "orchestrator",
                Name = "Orchestrator",
                Description = "Coordinates workflow execution, manages agent handoffs, and ensures smooth collaboration between agents.",
                Capabilities = new List<string>
                {
                    "coordinate-workflow",
                    "manage-handoffs",
                    "route-tasks",
                    "monitor-progress",
                    "resolve-conflicts"
                },
                SystemPrompt = @"You are an Orchestrator agent. Your role is to:
- Coordinate the execution of complex workflows
- Manage handoffs between different agents
- Route tasks to the appropriate agents based on capabilities
- Monitor overall progress and identify blockers
- Resolve conflicts and ensure smooth collaboration
- Maintain workflow state and context",
                ModelPreference = "gpt-4"
            }
        };
    }
}
