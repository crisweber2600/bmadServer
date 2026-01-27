namespace bmadServer.ApiService.Services;

public interface IPresenceTrackingService
{
    Task TrackUserOnlineAsync(Guid userId, Guid workflowId, string connectionId, CancellationToken cancellationToken = default);
    Task TrackUserOfflineAsync(Guid userId, Guid workflowId, CancellationToken cancellationToken = default);
    Task<List<Guid>> GetOnlineUsersAsync(Guid workflowId, CancellationToken cancellationToken = default);
}
