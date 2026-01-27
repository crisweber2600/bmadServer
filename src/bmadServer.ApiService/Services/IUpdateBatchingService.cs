using bmadServer.ApiService.Models.Events;

namespace bmadServer.ApiService.Services;

public interface IUpdateBatchingService
{
    void QueueUpdate(Guid workflowId, WorkflowEvent evt);
    Task FlushBatchAsync(CancellationToken cancellationToken = default);
}
