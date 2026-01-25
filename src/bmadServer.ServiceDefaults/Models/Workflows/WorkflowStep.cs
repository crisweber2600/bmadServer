namespace bmadServer.ServiceDefaults.Models.Workflows;

public class WorkflowStep
{
    public required string StepId { get; init; }
    public required string Name { get; init; }
    public required string AgentId { get; init; }
    public string? InputSchema { get; init; }
    public string? OutputSchema { get; init; }
    public bool IsOptional { get; init; }
    public bool CanSkip { get; init; }
}
