namespace bmadServer.ApiService.DTOs;

/// <summary>
/// Represents agent metadata for display in UI, showing agent attribution and capabilities.
/// Used in chat messages, handoff indicators, and decision attribution.
/// </summary>
public class AgentAttributionDto
{
    /// <summary>
    /// Unique identifier for the agent (kebab-case format, e.g., "product-manager").
    /// </summary>
    public required string AgentId { get; set; }

    /// <summary>
    /// Display name for the agent (e.g., "Product Manager").
    /// </summary>
    public required string AgentName { get; set; }

    /// <summary>
    /// Description of the agent's role and responsibilities.
    /// </summary>
    public required string AgentDescription { get; set; }

    /// <summary>
    /// URL to agent avatar image. Can be placeholder or dynamic based on agent type.
    /// </summary>
    public string? AgentAvatarUrl { get; set; }

    /// <summary>
    /// List of agent capabilities/expertise areas (e.g., ["strategic-planning", "market-analysis"]).
    /// </summary>
    public required List<string> Capabilities { get; set; }

    /// <summary>
    /// Description of agent's current step responsibility in workflow context.
    /// </summary>
    public string? CurrentStepResponsibility { get; set; }
}
