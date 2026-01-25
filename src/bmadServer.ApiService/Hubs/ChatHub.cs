using bmadServer.ApiService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using System.Security.Claims;

namespace bmadServer.ApiService.Hubs;

[Authorize]
public class ChatHub : Hub
{
    private readonly ISessionService _sessionService;
    private readonly ILogger<ChatHub> _logger;
    private readonly IConfiguration _configuration;
    private readonly int _maxHistoryMessages;
    private readonly int _partialSaveThrottleMs;
    private readonly int _partialSaveChunkInterval;

    public ChatHub(ISessionService sessionService, ILogger<ChatHub> logger, IConfiguration configuration)
    {
        _sessionService = sessionService;
        _logger = logger;
        _configuration = configuration;
        _maxHistoryMessages = configuration.GetValue<int>("Chat:MaxHistoryMessages", 10);
        _partialSaveThrottleMs = configuration.GetValue<int>("Streaming:PartialSaveThrottleMs", 500);
        _partialSaveChunkInterval = configuration.GetValue<int>("Streaming:PartialSaveChunkInterval", 5);
    }

    public override async Task OnConnectedAsync()
    {
        var userId = GetUserIdFromClaims();
        var connectionId = Context.ConnectionId;

        _logger.LogInformation("User {UserId} connecting with connection {ConnectionId}", 
            userId, connectionId);

        var (session, isRecovered) = await _sessionService.RecoverSessionAsync(userId, connectionId);

        if (isRecovered && session.WorkflowState != null)
        {
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

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = GetUserIdFromClaims();
        var connectionId = Context.ConnectionId;

        _logger.LogInformation(
            "User {UserId} disconnected from connection {ConnectionId}. Exception: {Exception}", 
            userId, connectionId, exception?.Message);

        await base.OnDisconnectedAsync(exception);
    }

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

    public async Task SendMessage(string message)
    {
        var receiveTime = DateTime.UtcNow;
        var userId = GetUserIdFromClaims();
        
        var session = await _sessionService.GetActiveSessionAsync(userId, Context.ConnectionId);
        if (session == null)
        {
            throw new HubException("No active session found");
        }

        var userMessageId = Guid.NewGuid().ToString();

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

            if (s.WorkflowState.ConversationHistory.Count > _maxHistoryMessages)
            {
                s.WorkflowState.ConversationHistory = s.WorkflowState.ConversationHistory
                    .TakeLast(_maxHistoryMessages)
                    .ToList();
            }
        });

        var processTime = (DateTime.UtcNow - receiveTime).TotalMilliseconds;
        _logger.LogInformation(
            "User {UserId} sent message in session {SessionId}. Processing time: {ProcessTimeMs}ms", 
            userId, session.Id, processTime);

        await Clients.Caller.SendAsync("ReceiveMessage", new
        {
            Role = "user",
            Content = message,
            Timestamp = DateTime.UtcNow,
            MessageId = userMessageId
        });

        await StreamAgentResponse(userId, session.Id, message);
    }

    private async Task StreamAgentResponse(Guid userId, Guid sessionId, string userMessage)
    {
        var messageId = Guid.NewGuid().ToString();
        var agentId = "bmad-agent-1";
        var fullResponse = GenerateSimulatedResponse(userMessage);
        
        var words = fullResponse.Split(' ');
        var streamedContent = "";
        var lastSaveTime = DateTime.UtcNow;
        var chunksSinceLastSave = 0;

        for (int i = 0; i < words.Length; i++)
        {
            var chunk = (i == 0 ? "" : " ") + words[i];
            streamedContent += chunk;
            var isComplete = i == words.Length - 1;
            chunksSinceLastSave++;

            await Clients.Caller.SendAsync("MESSAGE_CHUNK", new
            {
                MessageId = messageId,
                Chunk = chunk,
                IsComplete = isComplete,
                AgentId = agentId,
                Timestamp = DateTime.UtcNow
            });

            var timeSinceLastSave = (DateTime.UtcNow - lastSaveTime).TotalMilliseconds;
            var shouldSave = !isComplete && 
                (timeSinceLastSave >= _partialSaveThrottleMs || chunksSinceLastSave >= _partialSaveChunkInterval);

            if (shouldSave)
            {
                await SavePartialMessage(sessionId, userId, messageId, streamedContent, agentId);
                lastSaveTime = DateTime.UtcNow;
                chunksSinceLastSave = 0;
            }

            var minDelay = _configuration.GetValue<int>("Streaming:MinDelayMs", 50);
            var maxDelay = _configuration.GetValue<int>("Streaming:MaxDelayMs", 100);
            await Task.Delay(Random.Shared.Next(minDelay, maxDelay + 1));
        }

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
            s.WorkflowState.PendingInput = null;
        });
    }

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

    public async Task StopGenerating(string messageId)
    {
        var userId = GetUserIdFromClaims();
        _logger.LogInformation("User {UserId} requested to stop message {MessageId}", userId, messageId);

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

    public async Task JoinWorkflow(string workflowName)
    {
        var userId = GetUserIdFromClaims();
        await Groups.AddToGroupAsync(Context.ConnectionId, workflowName);
        
        _logger.LogInformation(
            "User {UserId} joined workflow group {WorkflowName}",
            userId, workflowName);
    }

    public async Task LeaveWorkflow(string workflowName)
    {
        var userId = GetUserIdFromClaims();
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, workflowName);
        
        _logger.LogInformation(
            "User {UserId} left workflow group {WorkflowName}",
            userId, workflowName);
    }
}
