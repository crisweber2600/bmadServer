using bmadServer.ApiService.DTOs.Checkpoints;
using bmadServer.ApiService.Models.Workflows;

namespace bmadServer.ApiService.Services.Checkpoints;

public interface IInputQueueService
{
    Task<QueuedInput> EnqueueInputAsync(
        Guid workflowId, 
        Guid userId, 
        string inputType, 
        System.Text.Json.JsonDocument content, 
        CancellationToken cancellationToken = default);
    
    Task<InputProcessingResult> ProcessQueuedInputsAsync(
        Guid workflowId, 
        CancellationToken cancellationToken = default);
    
    Task<List<QueuedInput>> GetQueuedInputsAsync(
        Guid workflowId, 
        CancellationToken cancellationToken = default);
}
