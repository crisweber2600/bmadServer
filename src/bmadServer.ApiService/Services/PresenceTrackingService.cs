using System.Collections.Concurrent;

namespace bmadServer.ApiService.Services;

public class PresenceTrackingService : IPresenceTrackingService
{
    private readonly ConcurrentDictionary<Guid, ConcurrentDictionary<Guid, DateTime>> _presenceMap = new();
    private readonly ILogger<PresenceTrackingService> _logger;

    public PresenceTrackingService(ILogger<PresenceTrackingService> logger)
    {
        _logger = logger;
    }

    public Task TrackUserOnlineAsync(
        Guid userId, 
        Guid workflowId, 
        string connectionId, 
        CancellationToken cancellationToken = default)
    {
        var workflowUsers = _presenceMap.GetOrAdd(workflowId, _ => new ConcurrentDictionary<Guid, DateTime>());
        workflowUsers[userId] = DateTime.UtcNow;

        _logger.LogInformation(
            "User {UserId} is now online for workflow {WorkflowId}",
            userId, workflowId);

        return Task.CompletedTask;
    }

    public Task TrackUserOfflineAsync(
        Guid userId, 
        Guid workflowId, 
        CancellationToken cancellationToken = default)
    {
        if (_presenceMap.TryGetValue(workflowId, out var workflowUsers))
        {
            workflowUsers.TryRemove(userId, out _);

            _logger.LogInformation(
                "User {UserId} is now offline for workflow {WorkflowId}",
                userId, workflowId);
        }

        return Task.CompletedTask;
    }

    public Task<List<Guid>> GetOnlineUsersAsync(
        Guid workflowId, 
        CancellationToken cancellationToken = default)
    {
        if (_presenceMap.TryGetValue(workflowId, out var workflowUsers))
        {
            return Task.FromResult(workflowUsers.Keys.ToList());
        }

        return Task.FromResult(new List<Guid>());
    }
}
