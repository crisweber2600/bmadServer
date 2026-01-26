using bmadServer.ApiService.DTOs.Checkpoints;
using bmadServer.ApiService.Models.Workflows;

namespace bmadServer.ApiService.Services.Checkpoints;

public interface ICheckpointService
{
    Task<WorkflowCheckpoint> CreateCheckpointAsync(
        Guid workflowId, 
        string stepId, 
        CheckpointType type, 
        Guid triggeredBy, 
        CancellationToken cancellationToken = default);
    
    Task RestoreCheckpointAsync(
        Guid workflowId, 
        Guid checkpointId, 
        CancellationToken cancellationToken = default);
    
    Task<PagedResult<CheckpointResponse>> GetCheckpointsAsync(
        Guid workflowId, 
        int page = 1, 
        int pageSize = 20, 
        CancellationToken cancellationToken = default);
    
    Task<WorkflowCheckpoint?> GetLatestCheckpointAsync(
        Guid workflowId, 
        CancellationToken cancellationToken = default);
    
    Task<WorkflowCheckpoint?> GetCheckpointByIdAsync(
        Guid checkpointId, 
        CancellationToken cancellationToken = default);
}
