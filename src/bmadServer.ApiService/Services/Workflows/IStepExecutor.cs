using bmadServer.ApiService.Models.Workflows;
using bmadServer.ApiService.Services.Workflows.Agents;

namespace bmadServer.ApiService.Services.Workflows;

/// <summary>
/// Interface for executing workflow steps
/// </summary>
public interface IStepExecutor
{
    /// <summary>
    /// Execute the current step of a workflow instance
    /// </summary>
    Task<StepExecutionResult> ExecuteStepAsync(Guid workflowInstanceId, string? userInput = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Execute the current step with streaming progress updates
    /// </summary>
    IAsyncEnumerable<StepProgress> ExecuteStepWithStreamingAsync(Guid workflowInstanceId, string? userInput = null, CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of step execution
/// </summary>
public class StepExecutionResult
{
    public bool Success { get; init; }
    public string? StepId { get; init; }
    public string? StepName { get; init; }
    public StepExecutionStatus Status { get; init; }
    public string? ErrorMessage { get; init; }
    public WorkflowStatus? NewWorkflowStatus { get; init; }
    public int? NextStep { get; init; }
}
