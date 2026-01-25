namespace bmadServer.ApiService.Models.Workflows;

public enum WorkflowStatus
{
    Created,
    Running,
    Paused,
    WaitingForInput,
    WaitingForApproval,
    Completed,
    Failed,
    Cancelled
}

public static class WorkflowStatusExtensions
{
    private static readonly Dictionary<WorkflowStatus, HashSet<WorkflowStatus>> ValidTransitions = new()
    {
        [WorkflowStatus.Created] = new() { WorkflowStatus.Running },
        [WorkflowStatus.Running] = new() 
        { 
            WorkflowStatus.Paused, 
            WorkflowStatus.WaitingForInput, 
            WorkflowStatus.WaitingForApproval, 
            WorkflowStatus.Completed, 
            WorkflowStatus.Failed 
        },
        [WorkflowStatus.Paused] = new() { WorkflowStatus.Running, WorkflowStatus.Cancelled },
        [WorkflowStatus.WaitingForInput] = new() { WorkflowStatus.Running, WorkflowStatus.Cancelled },
        [WorkflowStatus.WaitingForApproval] = new() { WorkflowStatus.Running, WorkflowStatus.Cancelled },
        [WorkflowStatus.Completed] = new(),
        [WorkflowStatus.Failed] = new(),
        [WorkflowStatus.Cancelled] = new()
    };

    public static bool ValidateTransition(WorkflowStatus from, WorkflowStatus to)
    {
        return ValidTransitions.TryGetValue(from, out var allowedStates) && allowedStates.Contains(to);
    }

    public static bool IsTerminal(this WorkflowStatus status)
    {
        return status is WorkflowStatus.Completed or WorkflowStatus.Failed or WorkflowStatus.Cancelled;
    }
}
