using bmadServer.ApiService.Data;
using Microsoft.EntityFrameworkCore;

namespace bmadServer.ApiService.BackgroundServices;

/// <summary>
/// Background service that periodically cleans up expired sessions.
/// Runs every 5 minutes to check for sessions that have exceeded the idle timeout.
/// Marks expired sessions as inactive while preserving them for audit trail.
/// </summary>
public class SessionCleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SessionCleanupService> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(5);

    public SessionCleanupService(
        IServiceProvider serviceProvider,
        ILogger<SessionCleanupService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Session cleanup service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CleanupExpiredSessionsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during session cleanup");
            }

            await Task.Delay(_interval, stoppingToken);
        }

        _logger.LogInformation("Session cleanup service stopped");
    }

    private async Task CleanupExpiredSessionsAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var now = DateTime.UtcNow;

        // Find active sessions that have exceeded their expiry time
        var expiredSessions = await dbContext.Sessions
            .Where(s => s.IsActive && s.ExpiresAt < now)
            .ToListAsync(cancellationToken);

        if (expiredSessions.Count == 0)
        {
            return;
        }

        _logger.LogInformation("Found {Count} expired sessions to clean up", expiredSessions.Count);

        foreach (var session in expiredSessions)
        {
            session.IsActive = false;
            session.ConnectionId = null; // Clear connection to prevent reconnection

            _logger.LogInformation(
                "Expired session {SessionId} for user {UserId} (idle since {LastActivity})",
                session.Id, session.UserId, session.LastActivityAt);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Cleaned up {Count} expired sessions", expiredSessions.Count);
    }
}
