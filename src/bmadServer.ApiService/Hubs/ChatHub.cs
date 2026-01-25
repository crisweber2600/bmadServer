using bmadServer.ApiService.Models;
using bmadServer.ApiService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace bmadServer.ApiService.Hubs;

/// <summary>
/// SignalR hub for real-time chat communication.
/// Manages session lifecycle: connection, disconnection, and recovery.
/// Supports real-time message streaming with interruption recovery.
/// </summary>
[Authorize]
public class ChatHub : Hub
{
    private readonly ISessionService _sessionService;
    private readonly IMessageStreamingService _streamingService;
    private readonly IChatHistoryService _chatHistoryService;
    private readonly ILogger<ChatHub> _logger;

    public ChatHub(
        ISessionService sessionService, 
        IMessageStreamingService streamingService,
        IChatHistoryService chatHistoryService,
        ILogger<ChatHub> logger)
    {
        _sessionService = sessionService;
        _streamingService = streamingService;
        _chatHistoryService = chatHistoryService;
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
    /// Message is acknowledged within 2 seconds per NFR1.
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

    /// <summary>
    /// Joins a specific workflow context.
    /// Groups are used for workflow-specific broadcasting.
    /// </summary>
    public async Task JoinWorkflow(string workflowName)
    {
        var userId = GetUserIdFromClaims();
        
        await Groups.AddToGroupAsync(Context.ConnectionId, $"workflow-{workflowName}");
        
        _logger.LogInformation("User {UserId} joined workflow {WorkflowName}", 
            userId, workflowName);
        
        await Clients.Caller.SendAsync("JoinedWorkflow", new
        {
            WorkflowName = workflowName,
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Leaves a specific workflow context.
    /// </summary>
    public async Task LeaveWorkflow(string workflowName)
    {
        var userId = GetUserIdFromClaims();
        
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"workflow-{workflowName}");
        
        _logger.LogInformation("User {UserId} left workflow {WorkflowName}", 
            userId, workflowName);
        
        await Clients.Caller.SendAsync("LeftWorkflow", new
        {
            WorkflowName = workflowName,
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Sends a message with streaming response.
    /// Streams tokens via MESSAGE_CHUNK events with first token within 5 seconds (NFR2).
    /// </summary>
    public async Task SendMessageStreaming(string message)
    {
        var userId = GetUserIdFromClaims();
        
        // Get active session
        var session = await _sessionService.GetActiveSessionAsync(userId, Context.ConnectionId);
        if (session == null)
        {
            throw new HubException("No active session found");
        }

        // Generate message ID
        var messageId = Guid.NewGuid().ToString();

        _logger.LogInformation("User {UserId} sent message in session {SessionId}, starting streaming", 
            userId, session.Id);

        // Stream response with callbacks
        await _streamingService.StreamResponseAsync(
            message,
            messageId,
            async (chunk, msgId, isComplete, agentId) =>
            {
                // Send MESSAGE_CHUNK to client
                await Clients.Caller.SendAsync("MESSAGE_CHUNK", new
                {
                    MessageId = msgId,
                    Chunk = chunk,
                    IsComplete = isComplete,
                    AgentId = agentId,
                    Timestamp = DateTime.UtcNow
                });
            },
            Context.ConnectionAborted);

        _logger.LogInformation("Streaming completed for message {MessageId} in session {SessionId}", 
            messageId, session.Id);
    }

    /// <summary>
    /// Gets paginated chat history for the current session.
    /// Returns last 50 messages by default, supports pagination for older messages.
    /// </summary>
    public async Task<ChatHistoryResponse> GetChatHistory(int pageSize = 50, int offset = 0)
    {
        var userId = GetUserIdFromClaims();
        
        var session = await _sessionService.GetActiveSessionAsync(userId, Context.ConnectionId);
        if (session == null)
        {
            throw new HubException("No active session found");
        }

        return await _chatHistoryService.GetChatHistoryAsync(userId, session.Id, pageSize, offset);
    }

    /// <summary>
    /// Stops an ongoing streaming response.
    /// Sends (Stopped) indicator to client.
    /// </summary>
    public async Task StopGenerating(string messageId)
    {
        var userId = GetUserIdFromClaims();
        
        _logger.LogInformation("User {UserId} stopping generation for message {MessageId}", 
            userId, messageId);

        await _streamingService.CancelStreamingAsync(messageId);
    }
}
