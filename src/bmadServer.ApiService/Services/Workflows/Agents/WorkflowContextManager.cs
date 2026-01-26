using System.Collections.Concurrent;

namespace bmadServer.ApiService.Services.Workflows.Agents;

/// <summary>
/// Service for managing shared workflow contexts
/// </summary>
public class WorkflowContextManager : IWorkflowContextManager
{
    private readonly ConcurrentDictionary<Guid, SharedWorkflowContext> _contexts;
    private readonly ILogger<WorkflowContextManager> _logger;

    public WorkflowContextManager(ILogger<WorkflowContextManager> logger)
    {
        _logger = logger;
        _contexts = new ConcurrentDictionary<Guid, SharedWorkflowContext>();
    }

    /// <inheritdoc />
    public SharedWorkflowContext GetOrCreateContext(Guid workflowInstanceId)
    {
        return _contexts.GetOrAdd(workflowInstanceId, _ =>
        {
            _logger.LogDebug("Creating new workflow context for instance {WorkflowInstanceId}", workflowInstanceId);
            return new SharedWorkflowContext();
        });
    }

    /// <inheritdoc />
    public bool TryGetContext(Guid workflowInstanceId, out SharedWorkflowContext? context)
    {
        return _contexts.TryGetValue(workflowInstanceId, out context);
    }

    /// <inheritdoc />
    public void RemoveContext(Guid workflowInstanceId)
    {
        if (_contexts.TryRemove(workflowInstanceId, out _))
        {
            _logger.LogInformation("Removed workflow context for instance {WorkflowInstanceId}", workflowInstanceId);
        }
    }

    /// <inheritdoc />
    public int GetActiveContextCount()
    {
        return _contexts.Count;
    }
}
