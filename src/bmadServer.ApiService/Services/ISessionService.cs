using bmadServer.ApiService.Data.Entities;

namespace bmadServer.ApiService.Services;

/// <summary>
/// Service for managing user sessions with persistence and recovery.
/// Supports 60-second recovery window (NFR6) and 30-minute idle timeout.
/// </summary>
public interface ISessionService
{
    /// <summary>
    /// Creates a new session for a user when they connect.
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="connectionId">SignalR connection ID</param>
    /// <returns>Created session</returns>
    Task<Session> CreateSessionAsync(Guid userId, string connectionId);

    /// <summary>
    /// Gets the active session for a user by connection ID.
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="connectionId">SignalR connection ID</param>
    /// <returns>Active session or null if not found</returns>
    Task<Session?> GetActiveSessionAsync(Guid userId, string connectionId);

    /// <summary>
    /// Gets the most recent active session for a user (used for recovery).
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>Most recent active session or null</returns>
    Task<Session?> GetMostRecentActiveSessionAsync(Guid userId);

    /// <summary>
    /// Updates the workflow state for a session with optimistic concurrency control.
    /// </summary>
    /// <param name="sessionId">Session ID</param>
    /// <param name="userId">User ID for concurrency tracking</param>
    /// <param name="updateAction">Action to update the workflow state</param>
    /// <returns>True if update succeeded, false if concurrency conflict</returns>
    Task<bool> UpdateSessionStateAsync(Guid sessionId, Guid userId, Action<Session> updateAction);

    /// <summary>
    /// Recovers or creates a session when a user reconnects.
    /// Implements NFR6: Direct recovery within 60 seconds, state recovery within 30 minutes.
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="newConnectionId">New SignalR connection ID</param>
    /// <returns>Recovered or new session with IsRecovered flag</returns>
    Task<(Session Session, bool IsRecovered)> RecoverSessionAsync(Guid userId, string newConnectionId);

    /// <summary>
    /// Marks a session as inactive (expired).
    /// Used by cleanup service and on explicit logout.
    /// </summary>
    /// <param name="sessionId">Session ID to expire</param>
    Task ExpireSessionAsync(Guid sessionId);

    /// <summary>
    /// Updates the LastActivityAt timestamp for a session.
    /// Called on every user action to keep session alive.
    /// </summary>
    /// <param name="sessionId">Session ID</param>
    Task UpdateActivityAsync(Guid sessionId);
}
