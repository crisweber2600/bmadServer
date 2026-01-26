namespace bmadServer.ApiService.Services.Workflows.Agents;

/// <summary>
/// Defines a BMAD agent with its capabilities and configuration
/// </summary>
public class AgentDefinition
{
    public required string AgentId { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required List<string> Capabilities { get; init; }
    public required string SystemPrompt { get; init; }
    public required string ModelPreference { get; init; }
}
