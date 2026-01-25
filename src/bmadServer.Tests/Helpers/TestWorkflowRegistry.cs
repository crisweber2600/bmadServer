using bmadServer.ServiceDefaults.Models.Workflows;
using bmadServer.ServiceDefaults.Services.Workflows;
using Microsoft.Extensions.Logging;

namespace bmadServer.Tests.Helpers;

/// <summary>
/// Test-friendly workflow registry that allows dynamic registration
/// </summary>
public class TestWorkflowRegistry : IWorkflowRegistry
{
    private readonly Dictionary<string, WorkflowDefinition> _workflows;
    private readonly ILogger<TestWorkflowRegistry>? _logger;

    public TestWorkflowRegistry(ILogger<TestWorkflowRegistry>? logger = null)
    {
        _logger = logger;
        _workflows = new Dictionary<string, WorkflowDefinition>(StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyList<WorkflowDefinition> GetAllWorkflows()
    {
        return _workflows.Values.ToList().AsReadOnly();
    }

    public WorkflowDefinition? GetWorkflow(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            _logger?.LogWarning("GetWorkflow called with null or empty workflow id");
            return null;
        }

        if (_workflows.TryGetValue(id, out var workflow))
        {
            return workflow;
        }

        _logger?.LogWarning("Workflow with id '{WorkflowId}' not found", id);
        return null;
    }

    public bool ValidateWorkflow(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return false;
        }

        return _workflows.ContainsKey(id);
    }

    /// <summary>
    /// Register a workflow for testing
    /// </summary>
    public void RegisterWorkflow(WorkflowDefinition workflow)
    {
        if (workflow == null)
        {
            throw new ArgumentNullException(nameof(workflow));
        }

        _workflows[workflow.WorkflowId] = workflow;
        _logger?.LogInformation("Registered test workflow '{WorkflowId}'", workflow.WorkflowId);
    }

    /// <summary>
    /// Clear all registered workflows
    /// </summary>
    public void Clear()
    {
        _workflows.Clear();
    }
}
