namespace bmadServer.ApiService.Models.Agents;

/// <summary>
/// Defines an agent with its capabilities and configuration
/// </summary>
public class AgentDefinition
{
    /// <summary>
    /// Unique identifier for the agent
    /// </summary>
    public required string AgentId { get; init; }

    /// <summary>
    /// Human-readable name of the agent
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Description of the agent's purpose and role
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// List of capabilities the agent can handle (e.g., workflow step types)
    /// </summary>
    public required List<string> Capabilities { get; init; }

    /// <summary>
    /// System prompt used when invoking the agent
    /// </summary>
    public required string SystemPrompt { get; init; }

    /// <summary>
    /// Preferred model for this agent (e.g., "gpt-4", "claude-3")
    /// </summary>
    public required string ModelPreference { get; init; }
}
