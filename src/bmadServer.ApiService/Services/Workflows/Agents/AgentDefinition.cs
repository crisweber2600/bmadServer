using System.ComponentModel.DataAnnotations;

namespace bmadServer.ApiService.Services.Workflows.Agents;

/// <summary>
/// Defines a BMAD agent with capabilities, system prompt, and model preferences
/// </summary>
public class AgentDefinition
{
    /// <summary>
    /// Unique identifier for the agent (e.g., "architect", "developer")
    /// </summary>
    [Required]
    public required string AgentId { get; init; }

    /// <summary>
    /// Human-readable name (e.g., "Architect")
    /// </summary>
    [Required]
    public required string Name { get; init; }

    /// <summary>
    /// Description of the agent's role and responsibilities
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// System prompt to provide context to the agent
    /// </summary>
    [Required]
    public required string SystemPrompt { get; init; }

    /// <summary>
    /// List of capabilities this agent can handle (e.g., "create-architecture", "write-code")
    /// </summary>
    [Required]
    public required List<string> Capabilities { get; init; } = [];

    /// <summary>
    /// Preferred AI model for this agent (e.g., "gpt-4", "claude-opus")
    /// </summary>
    public string? ModelPreference { get; init; }

    /// <summary>
    /// Maximum tokens for this agent's responses
    /// </summary>
    public int? MaxTokens { get; init; }

    /// <summary>
    /// Temperature setting for this agent (0.0-1.0)
    /// </summary>
    public decimal? Temperature { get; init; } = 0.7m;
}
