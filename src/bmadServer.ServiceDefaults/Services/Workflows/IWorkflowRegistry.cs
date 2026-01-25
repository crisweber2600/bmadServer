using bmadServer.ServiceDefaults.Models.Workflows;

namespace bmadServer.ServiceDefaults.Services.Workflows;

public interface IWorkflowRegistry
{
    IReadOnlyList<WorkflowDefinition> GetAllWorkflows();
    WorkflowDefinition? GetWorkflow(string id);
    bool ValidateWorkflow(string id);
}
