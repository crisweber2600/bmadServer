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
    /// System prompt to provide context to the agent (max 4000 characters)
    /// </summary>
    [Required]
    [StringLength(4000, MinimumLength = 10, ErrorMessage = "SystemPrompt must be between 10 and 4000 characters")]
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
    /// Maximum tokens for this agent's responses (1-128000)
    /// </summary>
    [Range(1, 128000, ErrorMessage = "MaxTokens must be between 1 and 128000")]
    public int? MaxTokens { get; init; }

    /// <summary>
    /// Temperature setting for this agent (0.0-1.0)
    /// </summary>
    [Range(0, 1, ErrorMessage = "Temperature must be between 0.0 and 1.0")]
    public decimal? Temperature { get; init; } = 0.7m;
}
