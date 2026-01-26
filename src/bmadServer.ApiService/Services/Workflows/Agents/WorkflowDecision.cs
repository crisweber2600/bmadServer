namespace bmadServer.ApiService.Services.Workflows.Agents;

/// <summary>
/// Represents a decision made during workflow execution
/// </summary>
public class WorkflowDecision
{
    public required string DecisionId { get; init; }
    public required string StepId { get; init; }
    public required string DecisionType { get; init; }
    public required string Outcome { get; init; }
    public required DateTime Timestamp { get; init; }
    public string? Rationale { get; init; }
    public string? MadeBy { get; init; }
}
