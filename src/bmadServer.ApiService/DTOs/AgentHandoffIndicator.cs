namespace bmadServer.ApiService.DTOs;

/// <summary>
/// DTO for displaying agent handoff in UI
/// </summary>
public class AgentHandoffIndicator
{
    /// <summary>
    /// Agent name receiving control
    /// </summary>
    public required string AgentName { get; init; }

    /// <summary>
    /// Agent description
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// Agent capabilities
    /// </summary>
    public required List<string> Capabilities { get; init; }

    /// <summary>
    /// Current workflow step
    /// </summary>
    public required string CurrentStep { get; init; }

    /// <summary>
    /// Handoff message
    /// </summary>
    public required string Message { get; init; }
}
