namespace bmadServer.ServiceDefaults.Models.Agents;

public class AgentDefinition
{
    public required string AgentId { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required IReadOnlyList<string> Capabilities { get; init; }
    public required string SystemPrompt { get; init; }
    public required ModelPreference ModelPreference { get; init; }
}
