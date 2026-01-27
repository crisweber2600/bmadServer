using bmadServer.ApiService.Hubs;
using bmadServer.ApiService.Models.Events;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

namespace bmadServer.ApiService.Services;

public class UpdateBatchingService : IUpdateBatchingService, IDisposable
{
    private readonly IHubContext<ChatHub> _hubContext;
    private readonly ILogger<UpdateBatchingService> _logger;
    private readonly ConcurrentDictionary<Guid, List<WorkflowEvent>> _pendingUpdates = new();
    private readonly Timer _batchTimer;
    private readonly TimeSpan _batchWindow = TimeSpan.FromMilliseconds(50);

    public UpdateBatchingService(
        IHubContext<ChatHub> hubContext,
        ILogger<UpdateBatchingService> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
        _batchTimer = new Timer(OnTimerCallback, null, _batchWindow, _batchWindow);
    }

    public void QueueUpdate(Guid workflowId, WorkflowEvent evt)
    {
        _pendingUpdates.AddOrUpdate(
            workflowId,
            new List<WorkflowEvent> { evt },
            (_, list) =>
            {
                lock (list)
                {
                    list.Add(evt);
                }
                return list;
            });
    }

    public async Task FlushBatchAsync(CancellationToken cancellationToken = default)
    {
        var snapshots = _pendingUpdates.ToArray();
        _pendingUpdates.Clear();

        foreach (var (workflowId, events) in snapshots)
        {
            try
            {
                await _hubContext.Clients
                    .Group($"workflow-{workflowId}")
                    .SendAsync("BATCH_UPDATE", events, cancellationToken);

                _logger.LogDebug(
                    "Flushed {Count} updates for workflow {WorkflowId}",
                    events.Count, workflowId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, 
                    "Error flushing batch for workflow {WorkflowId}", 
                    workflowId);
            }
        }
    }

    private void OnTimerCallback(object? state)
    {
        _ = FlushBatchAsync();
    }

    public void Dispose()
    {
        _batchTimer?.Dispose();
    }
}
