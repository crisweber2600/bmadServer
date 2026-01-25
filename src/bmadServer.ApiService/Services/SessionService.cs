using bmadServer.ApiService.Data;
using bmadServer.ApiService.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace bmadServer.ApiService.Services;

/// <summary>
/// Implements session management with persistence and recovery capabilities.
/// Handles 60-second recovery window (NFR6) and 30-minute idle timeout.
/// </summary>
public class SessionService : ISessionService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<SessionService> _logger;

    private const int RecoveryWindowSeconds = 60;
    private const int IdleTimeoutMinutes = 30;

    public SessionService(ApplicationDbContext dbContext, ILogger<SessionService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<Session> CreateSessionAsync(Guid userId, string connectionId)
    {
        var session = new Session
        {
            UserId = userId,
            ConnectionId = connectionId,
            LastActivityAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMinutes(IdleTimeoutMinutes),
            IsActive = true
        };

        _dbContext.Sessions.Add(session);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Created new session {SessionId} for user {UserId}", 
            session.Id, userId);

        return session;
    }

    public async Task<Session?> GetActiveSessionAsync(Guid userId, string connectionId)
    {
        return await _dbContext.Sessions
            .FirstOrDefaultAsync(s => 
                s.UserId == userId && 
                s.ConnectionId == connectionId && 
                s.IsActive);
    }

    public async Task<Session?> GetMostRecentActiveSessionAsync(Guid userId)
    {
        return await _dbContext.Sessions
            .Where(s => s.UserId == userId && s.IsActive)
            .OrderByDescending(s => s.LastActivityAt)
            .FirstOrDefaultAsync();
    }

    public async Task<bool> UpdateSessionStateAsync(Guid sessionId, Guid userId, Action<Session> updateAction)
    {
        try
        {
            var session = await _dbContext.Sessions.FindAsync(sessionId);
            if (session == null)
            {
                _logger.LogWarning("Session {SessionId} not found for update", sessionId);
                return false;
            }

            // Apply the update
            updateAction(session);

            // Update concurrency control fields in WorkflowState
            if (session.WorkflowState != null)
            {
                session.WorkflowState._version++;
                session.WorkflowState._lastModifiedBy = userId;
                session.WorkflowState._lastModifiedAt = DateTime.UtcNow;
            }

            // Update session activity
            session.LastActivityAt = DateTime.UtcNow;
            session.ExpiresAt = DateTime.UtcNow.AddMinutes(IdleTimeoutMinutes);

            await _dbContext.SaveChangesAsync();
            return true;
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex, "Concurrency conflict updating session {SessionId}", sessionId);
            return false;
        }
    }

    public async Task<(Session Session, bool IsRecovered)> RecoverSessionAsync(Guid userId, string newConnectionId)
    {
        // Find most recent active session for user
        var session = await GetMostRecentActiveSessionAsync(userId);

        if (session == null)
        {
            // No existing session - create new one
            var newSession = await CreateSessionAsync(userId, newConnectionId);
            return (newSession, false);
        }

        // Check if within 60-second recovery window (NFR6)
        if (session.IsWithinRecoveryWindow)
        {
            // Direct recovery - same session, update connection ID
            session.ConnectionId = newConnectionId;
            session.LastActivityAt = DateTime.UtcNow;
            session.ExpiresAt = DateTime.UtcNow.AddMinutes(IdleTimeoutMinutes);

            await _dbContext.SaveChangesAsync();

            _logger.LogInformation(
                "Recovered session {SessionId} for user {UserId} within 60s window", 
                session.Id, userId);

            return (session, true);
        }

        // Outside recovery window - check if still within idle timeout (30 min)
        var idleMinutes = DateTime.UtcNow.Subtract(session.LastActivityAt).TotalMinutes;
        if (idleMinutes < IdleTimeoutMinutes)
        {
            // Create new session but restore workflow state
            var newSession = new Session
            {
                UserId = userId,
                ConnectionId = newConnectionId,
                WorkflowState = session.WorkflowState, // Restore state!
                LastActivityAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMinutes(IdleTimeoutMinutes),
                IsActive = true
            };

            // Mark old session as inactive
            session.IsActive = false;
            session.ConnectionId = null;

            _dbContext.Sessions.Add(newSession);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation(
                "Recovered workflow state from session {OldSessionId} to new session {NewSessionId}", 
                session.Id, newSession.Id);

            return (newSession, true);
        }

        // Session expired - create fresh session
        _logger.LogInformation(
            "Session {SessionId} expired (idle > 30min), creating new session for user {UserId}", 
            session.Id, userId);

        var freshSession = await CreateSessionAsync(userId, newConnectionId);
        return (freshSession, false);
    }

    public async Task ExpireSessionAsync(Guid sessionId)
    {
        var session = await _dbContext.Sessions.FindAsync(sessionId);
        if (session == null)
        {
            return;
        }

        session.IsActive = false;
        session.ConnectionId = null;

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Expired session {SessionId}", sessionId);
    }

    public async Task UpdateActivityAsync(Guid sessionId)
    {
        var session = await _dbContext.Sessions.FindAsync(sessionId);
        if (session == null)
        {
            return;
        }

        session.LastActivityAt = DateTime.UtcNow;
        session.ExpiresAt = DateTime.UtcNow.AddMinutes(IdleTimeoutMinutes);

        await _dbContext.SaveChangesAsync();
    }
}
