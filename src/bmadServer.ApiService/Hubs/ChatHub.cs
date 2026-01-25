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

        _logger.LogInformation(
            "Connection established successfully for user {UserId} with connection ID {ConnectionId}",
            userId, connectionId);

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
        var receiveTime = DateTime.UtcNow;
        var userId = GetUserIdFromClaims();
        
        // Get active session
        var session = await _sessionService.GetActiveSessionAsync(userId, Context.ConnectionId);
        if (session == null)
        {
            throw new HubException("No active session found");
        }

        var userMessageId = Guid.NewGuid().ToString();

        // Update session state with new message
        await _sessionService.UpdateSessionStateAsync(session.Id, userId, s =>
        {
            s.WorkflowState ??= new Models.WorkflowState();
            
            s.WorkflowState.ConversationHistory.Add(new Models.ChatMessage
            {
                Id = userMessageId,
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

        var processTime = (DateTime.UtcNow - receiveTime).TotalMilliseconds;
        _logger.LogInformation(
            "User {UserId} sent message in session {SessionId}. Processing time: {ProcessTimeMs}ms", 
            userId, session.Id, processTime);

        // Echo user message back
        await Clients.Caller.SendAsync("ReceiveMessage", new
        {
            Role = "user",
            Content = message,
            Timestamp = DateTime.UtcNow,
            MessageId = userMessageId
        });

        // Start streaming agent response (simulated for now)
        await StreamAgentResponse(userId, session.Id, message);
    }

    /// <summary>
    /// Simulates streaming an agent response with MESSAGE_CHUNK events.
    /// In production, this would integrate with an actual AI agent/LLM.
    /// </summary>
    private async Task StreamAgentResponse(Guid userId, Guid sessionId, string userMessage)
    {
        var messageId = Guid.NewGuid().ToString();
        var agentId = "bmad-agent-1";
        var fullResponse = GenerateSimulatedResponse(userMessage);
        
        // Simulate token-by-token streaming
        var words = fullResponse.Split(' ');
        var streamedContent = "";

        for (int i = 0; i < words.Length; i++)
        {
            var chunk = (i == 0 ? "" : " ") + words[i];
            streamedContent += chunk;
            var isComplete = i == words.Length - 1;

            await Clients.Caller.SendAsync("MESSAGE_CHUNK", new
            {
                MessageId = messageId,
                Chunk = chunk,
                IsComplete = isComplete,
                AgentId = agentId,
                Timestamp = DateTime.UtcNow
            });

            // Save partial message for recovery
            if (!isComplete)
            {
                await SavePartialMessage(sessionId, userId, messageId, streamedContent, agentId);
            }

            // Simulate streaming delay (50-100ms per token for realism)
            await Task.Delay(Random.Shared.Next(50, 100));
        }

        // Save complete message to session history
        await _sessionService.UpdateSessionStateAsync(sessionId, userId, s =>
        {
            s.WorkflowState ??= new Models.WorkflowState();
            s.WorkflowState.ConversationHistory.Add(new Models.ChatMessage
            {
                Id = messageId,
                Role = "agent",
                Content = fullResponse,
                Timestamp = DateTime.UtcNow,
                AgentId = agentId
            });
        });
    }

    /// <summary>
    /// Saves partial message for interruption recovery.
    /// </summary>
    private async Task SavePartialMessage(Guid sessionId, Guid userId, string messageId, 
        string partialContent, string agentId)
    {
        await _sessionService.UpdateSessionStateAsync(sessionId, userId, s =>
        {
            s.WorkflowState ??= new Models.WorkflowState();
            s.WorkflowState.PendingInput = System.Text.Json.JsonSerializer.Serialize(new
            {
                MessageId = messageId,
                PartialContent = partialContent,
                AgentId = agentId,
                IsStreaming = true
            });
        });
    }

    /// <summary>
    /// Stops message generation mid-stream.
    /// </summary>
    public async Task StopGenerating(string messageId)
    {
        var userId = GetUserIdFromClaims();
        _logger.LogInformation("User {UserId} requested to stop message {MessageId}", userId, messageId);

        // Signal client that generation stopped
        await Clients.Caller.SendAsync("GENERATION_STOPPED", new
        {
            MessageId = messageId,
            Timestamp = DateTime.UtcNow
        });
    }

    private string GenerateSimulatedResponse(string userMessage)
    {
        var lower = userMessage.ToLower();

        if (lower.Contains("help"))
        {
            return "I'd be happy to help! Here are some things I can do:\n\n- Answer questions about BMAD\n- Provide code examples\n- Explain concepts\n- And much more!\n\nWhat would you like to know?";
        }

        if (lower.Contains("code"))
        {
            return "Here's a simple code example:\n\n```javascript\nfunction greet(name) {\n  return `Hello, ${name}!`;\n}\n\nconsole.log(greet('World'));\n```\n\nThis function takes a name and returns a greeting.";
        }

        return $"You said: \"{userMessage}\"\n\nThat's interesting! I can help you with various tasks. Try asking about help, code, or links.";
    }

    /// <summary>
    /// Joins a workflow group for targeted messaging.
    /// </summary>
    public async Task JoinWorkflow(string workflowName)
    {
        var userId = GetUserIdFromClaims();
        await Groups.AddToGroupAsync(Context.ConnectionId, workflowName);
        
        _logger.LogInformation(
            "User {UserId} joined workflow group {WorkflowName}",
            userId, workflowName);
    }

    /// <summary>
    /// Leaves a workflow group.
    /// </summary>
    public async Task LeaveWorkflow(string workflowName)
    {
        var userId = GetUserIdFromClaims();
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, workflowName);
        
        _logger.LogInformation(
            "User {UserId} left workflow group {WorkflowName}",
            userId, workflowName);
    }
}
