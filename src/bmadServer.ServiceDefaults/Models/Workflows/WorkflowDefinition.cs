namespace bmadServer.ServiceDefaults.Models.Workflows;

public class WorkflowDefinition
{
    public required string WorkflowId { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required TimeSpan EstimatedDuration { get; init; }
    public required IReadOnlyList<string> RequiredRoles { get; init; }
    public required IReadOnlyList<WorkflowStep> Steps { get; init; }
}
