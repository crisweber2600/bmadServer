using System.Collections.Concurrent;

namespace bmadServer.ApiService.Services;

public class PresenceTrackingService : IPresenceTrackingService, IAsyncDisposable, IDisposable
{
    private readonly ConcurrentDictionary<Guid, ConcurrentDictionary<Guid, DateTime>> _presenceMap = new();
    private readonly ILogger<PresenceTrackingService> _logger;
    private readonly Timer _cleanupTimer;
    private readonly TimeSpan _staleThreshold = TimeSpan.FromMinutes(30);
    private int _disposed; // 0 = not disposed, 1 = disposed (using int for Interlocked)

    public PresenceTrackingService(ILogger<PresenceTrackingService> logger)
    {
        _logger = logger;
        // Run cleanup every 5 minutes
        _cleanupTimer = new Timer(CleanupStaleEntries, null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
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

            // Clean up empty workflow entries
            if (workflowUsers.IsEmpty)
            {
                _presenceMap.TryRemove(workflowId, out _);
            }
        }

        return Task.CompletedTask;
    }

    public Task<List<Guid>> GetOnlineUsersAsync(
        Guid workflowId, 
        CancellationToken cancellationToken = default)
    {
        if (_presenceMap.TryGetValue(workflowId, out var workflowUsers))
        {
            // Filter out stale entries when querying
            var now = DateTime.UtcNow;
            var activeUsers = workflowUsers
                .Where(kvp => now - kvp.Value < _staleThreshold)
                .Select(kvp => kvp.Key)
                .ToList();
            return Task.FromResult(activeUsers);
        }

        return Task.FromResult(new List<Guid>());
    }

    private void CleanupStaleEntries(object? state)
    {
        // Prevent cleanup after disposal (thread-safe read)
        if (Interlocked.CompareExchange(ref _disposed, 0, 0) == 1)
        {
            return;
        }

        var now = DateTime.UtcNow;
        var removedCount = 0;

        foreach (var workflowEntry in _presenceMap)
        {
            var workflowUsers = workflowEntry.Value;
            var staleUsers = workflowUsers
                .Where(kvp => now - kvp.Value >= _staleThreshold)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var userId in staleUsers)
            {
                if (workflowUsers.TryRemove(userId, out _))
                {
                    removedCount++;
                }
            }

            // Remove empty workflow entries
            if (workflowUsers.IsEmpty)
            {
                _presenceMap.TryRemove(workflowEntry.Key, out _);
            }
        }

        if (removedCount > 0)
        {
            _logger.LogInformation(
                "Presence cleanup: removed {Count} stale user entries", 
                removedCount);
        }
    }

    public async ValueTask DisposeAsync()
    {
        // Thread-safe check-and-set using Interlocked
        if (Interlocked.CompareExchange(ref _disposed, 1, 0) == 0)
        {
            await _cleanupTimer.DisposeAsync();
        }
    }

    public void Dispose()
    {
        // Thread-safe check-and-set using Interlocked
        if (Interlocked.CompareExchange(ref _disposed, 1, 0) == 0)
        {
            // Synchronous disposal - safe for server applications
            _cleanupTimer.Dispose();
        }
    }
}
