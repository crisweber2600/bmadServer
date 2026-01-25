using bmadServer.ApiService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace bmadServer.ApiService.Hubs;

/// <summary>
/// SignalR hub for real-time chat communication.
/// Manages session lifecycle: connection, disconnection, and recovery.
/// </summary>
[Authorize]
public class ChatHub : Hub
{
    private readonly ISessionService _sessionService;
    private readonly ILogger<ChatHub> _logger;

    public ChatHub(ISessionService sessionService, ILogger<ChatHub> logger)
    {
        _sessionService = sessionService;
        _logger = logger;
    }

    /// <summary>
    /// Called when a client connects to the hub.
    /// Creates or recovers session based on NFR6 requirements.
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        var userId = GetUserIdFromClaims();
        var connectionId = Context.ConnectionId;

        _logger.LogInformation("User {UserId} connecting with connection {ConnectionId}", 
            userId, connectionId);

        // Attempt to recover existing session or create new one
        var (session, isRecovered) = await _sessionService.RecoverSessionAsync(userId, connectionId);

        if (isRecovered && session.WorkflowState != null)
        {
            // Send recovery message to client
            await Clients.Caller.SendAsync("SESSION_RESTORED", new
            {
                session.Id,
                session.WorkflowState.WorkflowName,
                session.WorkflowState.CurrentStep,
                session.WorkflowState.ConversationHistory,
                session.WorkflowState.PendingInput,
                Message = session.IsWithinRecoveryWindow 
                    ? "Session restored - resuming from where you left off"
                    : "Session recovered from last checkpoint"
            });

            _logger.LogInformation(
                "Sent SESSION_RESTORED message for session {SessionId}", 
                session.Id);
        }
        else
        {
            _logger.LogInformation(
                "Created new session {SessionId} for user {UserId}", 
                session.Id, userId);
        }

        await base.OnConnectedAsync();
    }

    /// <summary>
    /// Called when a client disconnects from the hub.
    /// Session is NOT immediately expired to allow for reconnection within 60s window.
    /// Cleanup service will expire sessions after idle timeout.
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = GetUserIdFromClaims();
        var connectionId = Context.ConnectionId;

        _logger.LogInformation(
            "User {UserId} disconnected from connection {ConnectionId}. Exception: {Exception}", 
            userId, connectionId, exception?.Message);

        // Don't expire session immediately - allow reconnection within 60s
        // Session cleanup service will handle expiration after idle timeout

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Extracts user ID from JWT claims.
    /// </summary>
    private Guid GetUserIdFromClaims()
    {
        var userIdClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? Context.User?.FindFirst("sub")?.Value;

        if (userIdClaim == null || !Guid.TryParse(userIdClaim, out var userId))
        {
            throw new HubException("User ID not found in claims");
        }

        return userId;
    }

    /// <summary>
    /// Sends a chat message and updates session activity.
    /// </summary>
    public async Task SendMessage(string message)
    {
        var userId = GetUserIdFromClaims();
        
        // Get active session
        var session = await _sessionService.GetActiveSessionAsync(userId, Context.ConnectionId);
        if (session == null)
        {
            throw new HubException("No active session found");
        }

        // Update session state with new message
        await _sessionService.UpdateSessionStateAsync(session.Id, userId, s =>
        {
            s.WorkflowState ??= new Models.WorkflowState();
            
            s.WorkflowState.ConversationHistory.Add(new Models.ChatMessage
            {
                Id = Guid.NewGuid().ToString(),
                Role = "user",
                Content = message,
                Timestamp = DateTime.UtcNow
            });

            // Keep only last 10 messages per AC
            if (s.WorkflowState.ConversationHistory.Count > 10)
            {
                s.WorkflowState.ConversationHistory = s.WorkflowState.ConversationHistory
                    .TakeLast(10)
                    .ToList();
            }
        });

        _logger.LogInformation("User {UserId} sent message in session {SessionId}", 
            userId, session.Id);

        // Echo message back (placeholder - real implementation would invoke workflow/agent)
        await Clients.Caller.SendAsync("ReceiveMessage", new
        {
            Role = "user",
            Content = message,
            Timestamp = DateTime.UtcNow
        });
    }
}
