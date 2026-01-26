namespace bmadServer.ApiService.Services.Workflows.Agents;

/// <summary>
/// Represents an agent handoff event
/// </summary>
public class AgentHandoff
{
    public required Guid HandoffId { get; init; }
    public required Guid WorkflowInstanceId { get; init; }
    public required string FromAgent { get; init; }
    public required string ToAgent { get; init; }
    public required string WorkflowStep { get; init; }
    public required string Reason { get; init; }
    public required DateTime Timestamp { get; init; }
    public Dictionary<string, string>? Metadata { get; init; }
}
