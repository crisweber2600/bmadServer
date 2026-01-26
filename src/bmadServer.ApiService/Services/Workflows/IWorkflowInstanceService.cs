using bmadServer.ApiService.DTOs;
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
    Task<(bool Success, string? Message)> CancelWorkflowAsync(Guid instanceId, Guid userId);
    Task<List<WorkflowInstance>> GetWorkflowInstancesAsync(Guid userId, bool showCancelled = false);
    Task<(bool Success, string? Message)> SkipCurrentStepAsync(Guid instanceId, Guid userId, string? skipReason = null);
    Task<(bool Success, string? Message)> GoToStepAsync(Guid instanceId, string stepId, Guid userId);
    
     // Story 4-7: Progress and status methods
    Task<WorkflowStatusResponse?> GetWorkflowStatusAsync(Guid instanceId);
    int CalculateProgress(WorkflowInstance instance, int totalSteps);
    Task<DateTime?> EstimateCompletionAsync(Guid instanceId);
    Task<PagedResult<WorkflowInstance>> GetFilteredWorkflowsAsync(
        Guid userId, 
        WorkflowStatus? status = null,
        string? workflowType = null,
        DateTime? createdAfter = null,
        DateTime? createdBefore = null,
        int page = 1,
        int pageSize = 20);
    
    // Story 5.4: Agent Handoff & Attribution methods
    Task<List<AgentHandoff>> GetWorkflowHandoffsAsync(Guid workflowInstanceId);
    
    // Story 5.5: Human Approval for Low-Confidence Decisions
    Task<(bool Success, string? Message)> ResumeAfterApprovalAsync(
        Guid workflowInstanceId,
        ApprovalRequest approvalRequest,
        CancellationToken cancellationToken = default);
}
