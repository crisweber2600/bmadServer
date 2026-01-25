using bmadServer.ServiceDefaults.Models.Agents;

namespace bmadServer.ServiceDefaults.Services.Agents;

public static class BmadAgentDefinitions
{
    public static AgentDefinition ProductManager => new()
    {
        AgentId = "product-manager",
        Name = "Product Manager",
        Description = "Creates PRDs, defines requirements, and prioritizes features",
        Capabilities = new List<string>
        {
            "create-prd",
            "define-requirements",
            "prioritize-features",
            "create-user-stories"
        }.AsReadOnly(),
        SystemPrompt = "You are an experienced Product Manager. Focus on user needs, business value, and clear requirements. Create comprehensive PRDs with well-defined acceptance criteria.",
        ModelPreference = new ModelPreference
        {
            PreferredModel = "gpt-4",
            FallbackModel = "gpt-3.5-turbo",
            MaxTokens = 4000,
            Temperature = 0.7
        }
    };

    public static AgentDefinition Architect => new()
    {
        AgentId = "architect",
        Name = "Architect",
        Description = "Designs system architecture, defines technical approach, and creates architectural documentation",
        Capabilities = new List<string>
        {
            "create-architecture",
            "design-system",
            "define-components",
            "integration-patterns"
        }.AsReadOnly(),
        SystemPrompt = "You are a Senior Software Architect. Design scalable, maintainable systems following best practices. Consider performance, security, and maintainability in all decisions.",
        ModelPreference = new ModelPreference
        {
            PreferredModel = "gpt-4",
            FallbackModel = "gpt-3.5-turbo",
            MaxTokens = 4000,
            Temperature = 0.6
        }
    };

    public static AgentDefinition Designer => new()
    {
        AgentId = "designer",
        Name = "Designer",
        Description = "Creates UX designs, user flows, and wireframes",
        Capabilities = new List<string>
        {
            "design-ux",
            "create-user-flows",
            "create-wireframes",
            "design-interactions"
        }.AsReadOnly(),
        SystemPrompt = "You are a UX/UI Designer. Create intuitive, user-centered designs that balance aesthetics with functionality. Focus on user experience and accessibility.",
        ModelPreference = new ModelPreference
        {
            PreferredModel = "gpt-4",
            FallbackModel = "gpt-3.5-turbo",
            MaxTokens = 3000,
            Temperature = 0.8
        }
    };

    public static AgentDefinition Developer => new()
    {
        AgentId = "developer",
        Name = "Developer",
        Description = "Implements features with TDD, writes clean code, and creates comprehensive tests",
        Capabilities = new List<string>
        {
            "dev-story",
            "write-tests",
            "implement-features",
            "refactor-code"
        }.AsReadOnly(),
        SystemPrompt = "You are a Senior Software Developer. Follow TDD practices, write clean and maintainable code, and ensure comprehensive test coverage. Adhere to SOLID principles and coding standards.",
        ModelPreference = new ModelPreference
        {
            PreferredModel = "gpt-4",
            FallbackModel = "gpt-3.5-turbo",
            MaxTokens = 4000,
            Temperature = 0.5
        }
    };

    public static AgentDefinition Analyst => new()
    {
        AgentId = "analyst",
        Name = "Analyst",
        Description = "Analyzes requirements, identifies gaps, and performs code reviews",
        Capabilities = new List<string>
        {
            "code-review",
            "analyze-requirements",
            "identify-issues",
            "quality-assurance"
        }.AsReadOnly(),
        SystemPrompt = "You are a Quality Analyst and Code Reviewer. Identify potential issues, security vulnerabilities, and areas for improvement. Provide constructive feedback focused on quality and best practices.",
        ModelPreference = new ModelPreference
        {
            PreferredModel = "gpt-4",
            FallbackModel = "gpt-3.5-turbo",
            MaxTokens = 3500,
            Temperature = 0.4
        }
    };

    public static AgentDefinition Orchestrator => new()
    {
        AgentId = "orchestrator",
        Name = "Orchestrator",
        Description = "Coordinates multi-agent workflows and manages agent-to-agent communication",
        Capabilities = new List<string>
        {
            "orchestrate-workflow",
            "coordinate-agents",
            "manage-handoffs",
            "resolve-conflicts"
        }.AsReadOnly(),
        SystemPrompt = "You are a Workflow Orchestrator. Coordinate multiple agents, manage workflow execution, and ensure smooth handoffs between agents. Handle conflicts and maintain workflow context.",
        ModelPreference = new ModelPreference
        {
            PreferredModel = "gpt-4",
            FallbackModel = "gpt-3.5-turbo",
            MaxTokens = 3000,
            Temperature = 0.5
        }
    };

    public static IReadOnlyList<AgentDefinition> AllAgents => new List<AgentDefinition>
    {
        ProductManager,
        Architect,
        Designer,
        Developer,
        Analyst,
        Orchestrator
    }.AsReadOnly();
}
