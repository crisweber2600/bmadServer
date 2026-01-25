using bmadServer.ApiService.Models.Workflows;

namespace bmadServer.ApiService.Services.Workflows;

public interface IWorkflowInstanceService
{
    Task<WorkflowInstance> CreateWorkflowInstanceAsync(string workflowId, Guid userId, Dictionary<string, object> initialContext);
    Task<bool> StartWorkflowAsync(Guid instanceId);
    Task<bool> TransitionStateAsync(Guid instanceId, WorkflowStatus newStatus);
    Task<WorkflowInstance?> GetWorkflowInstanceAsync(Guid instanceId);
    Task<(bool Success, string? Message)> PauseWorkflowAsync(Guid instanceId, Guid userId);
    Task<(bool Success, string? Message)> ResumeWorkflowAsync(Guid instanceId, Guid userId);
}
