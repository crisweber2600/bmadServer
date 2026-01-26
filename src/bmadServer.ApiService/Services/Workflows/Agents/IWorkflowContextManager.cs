namespace bmadServer.ApiService.Services.Workflows.Agents;

/// <summary>
/// Interface for managing shared workflow contexts
/// </summary>
public interface IWorkflowContextManager
{
    /// <summary>
    /// Get or create context for a workflow instance
    /// </summary>
    SharedWorkflowContext GetOrCreateContext(Guid workflowInstanceId);

    /// <summary>
    /// Try to get an existing context
    /// </summary>
    bool TryGetContext(Guid workflowInstanceId, out SharedWorkflowContext? context);

    /// <summary>
    /// Remove context for a workflow instance
    /// </summary>
    void RemoveContext(Guid workflowInstanceId);

    /// <summary>
    /// Get count of active contexts
    /// </summary>
    int GetActiveContextCount();
}
