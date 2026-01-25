using bmadServer.ApiService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using System.Collections.Concurrent;
using System.Security.Claims;
using System.Text;

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
    
    // Track active streaming tasks per connection to prevent concurrent streams
    // NOTE: This static dictionary works for single-server deployments. For horizontal scaling
    // with SignalR backplane (Redis/Azure SignalR), consider moving to per-connection state
    // or a distributed cache. See Epic 4 for scalability improvements.
    private static readonly ConcurrentDictionary<string, CancellationTokenSource> _activeStreams = new();
    
    // Timer for periodic cleanup of orphaned streams (connections that didn't disconnect cleanly)
    private static readonly Timer _cleanupTimer = new(_ => CleanupOrphanedStreams(), null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
    
    private static void CleanupOrphanedStreams()
    {
        // Remove entries where the CancellationTokenSource has been disposed or cancelled
        foreach (var kvp in _activeStreams)
        {
            try
            {
                if (kvp.Value.IsCancellationRequested)
                {
                    if (_activeStreams.TryRemove(kvp.Key, out var removed))
                    {
                        removed.Dispose();
                    }
                }
            }
            catch (ObjectDisposedException)
            {
                // Already disposed, just remove it
                _activeStreams.TryRemove(kvp.Key, out _);
            }
        }
    }

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

        // Clean up any active streaming for this connection
        if (_activeStreams.TryRemove(connectionId, out var cts))
        {
            cts.Cancel();
            cts.Dispose();
            _logger.LogInformation("Cleaned up active stream for disconnected connection {ConnectionId}", connectionId);
        }

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

        // Cancel any existing stream for this connection
        var streamKey = Context.ConnectionId;
        if (_activeStreams.TryRemove(streamKey, out var existingCts))
        {
            existingCts.Cancel();
            existingCts.Dispose();
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

        // Create new cancellation token for this stream
        var cts = new CancellationTokenSource();
        _activeStreams[streamKey] = cts;

        try
        {
            await StreamAgentResponse(userId, session.Id, message, cts.Token);
        }
        finally
        {
            _activeStreams.TryRemove(streamKey, out _);
            cts.Dispose();
        }
    }

    private async Task StreamAgentResponse(Guid userId, Guid sessionId, string userMessage, CancellationToken cancellationToken)
    {
        var messageId = Guid.NewGuid().ToString();
        var agentId = "bmad-agent-1";
        var fullResponse = GenerateSimulatedResponse(userMessage);
        
        var words = fullResponse.Split(' ');
        var streamedContent = new StringBuilder();
        var lastSaveTime = DateTime.UtcNow;
        var chunksSinceLastSave = 0;

        try
        {
            for (int i = 0; i < words.Length; i++)
            {
                // Check for cancellation
                if (cancellationToken.IsCancellationRequested)
                {
                    _logger.LogInformation("Streaming cancelled for message {MessageId}", messageId);
                    return;
                }

                var chunk = (i == 0 ? "" : " ") + words[i];
                streamedContent.Append(chunk);
                var isComplete = i == words.Length - 1;
                chunksSinceLastSave++;

                await Clients.Caller.SendAsync("MESSAGE_CHUNK", new
                {
                    MessageId = messageId,
                    Chunk = chunk,
                    IsComplete = isComplete,
                    AgentId = agentId,
                    Timestamp = DateTime.UtcNow
                }, cancellationToken);

                var timeSinceLastSave = (DateTime.UtcNow - lastSaveTime).TotalMilliseconds;
                var shouldSave = !isComplete && 
                    (timeSinceLastSave >= _partialSaveThrottleMs || chunksSinceLastSave >= _partialSaveChunkInterval);

                if (shouldSave)
                {
                    await SavePartialMessage(sessionId, userId, messageId, streamedContent.ToString(), agentId);
                    lastSaveTime = DateTime.UtcNow;
                    chunksSinceLastSave = 0;
                }

                var minDelay = _configuration.GetValue<int>("Streaming:MinDelayMs", 50);
                var maxDelay = _configuration.GetValue<int>("Streaming:MaxDelayMs", 100);
                await Task.Delay(Random.Shared.Next(minDelay, maxDelay + 1), cancellationToken);
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
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Streaming operation cancelled for message {MessageId}", messageId);
            // Don't save partial message on cancellation
        }
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

        // Cancel the active streaming task for this connection
        var streamKey = Context.ConnectionId;
        if (_activeStreams.TryRemove(streamKey, out var cts))
        {
            cts.Cancel();
            cts.Dispose();
            _logger.LogInformation("Cancelled streaming for connection {ConnectionId}", streamKey);
        }

        await Clients.Caller.SendAsync("GENERATION_STOPPED", new
        {
            MessageId = messageId,
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Placeholder for agent response generation. Replace with actual agent routing in Epic 4.
    /// TODO(Epic-4): Connect to workflow orchestration engine for real agent responses.
    /// </summary>
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
