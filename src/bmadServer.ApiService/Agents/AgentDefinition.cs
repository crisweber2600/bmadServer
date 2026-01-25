namespace bmadServer.ApiService.Agents;

/// <summary>
/// Defines a BMAD agent with its capabilities, system prompt, and model preference.
/// </summary>
public class AgentDefinition
{
    /// <summary>
    /// Unique identifier for the agent (e.g., "product-manager", "architect").
    /// </summary>
    public required string AgentId { get; init; }

    /// <summary>
    /// Human-readable name of the agent (e.g., "Product Manager", "Architect").
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Description of the agent's role and responsibilities.
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// List of capabilities/workflow steps this agent can handle
    /// (e.g., ["create-architecture", "review-architecture"]).
    /// </summary>
    public required List<string> Capabilities { get; init; }

    /// <summary>
    /// System prompt used when invoking this agent.
    /// Defines the agent's persona and working instructions.
    /// </summary>
    public required string SystemPrompt { get; init; }

    /// <summary>
    /// Preferred AI model for this agent (e.g., "gpt-4", "claude-3").
    /// Used for cost/quality tradeoffs.
    /// </summary>
    public required string ModelPreference { get; init; }
}
