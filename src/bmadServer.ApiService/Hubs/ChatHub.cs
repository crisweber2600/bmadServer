using bmadServer.ApiService.Data;
using bmadServer.ApiService.Services;
using bmadServer.ApiService.Services.Workflows;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace bmadServer.ApiService.Hubs;

/// <summary>
/// SignalR hub for real-time chat communication.
/// Manages session lifecycle: connection, disconnection, and recovery.
/// Routes chat messages to workflow engine when an active workflow exists.
/// </summary>
[Authorize]
public class ChatHub : Hub
{
    private readonly ISessionService _sessionService;
    private readonly IStepExecutor _stepExecutor;
    private readonly ITranslationService _translationService;
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<ChatHub> _logger;

    public ChatHub(
        ISessionService sessionService, 
        IStepExecutor stepExecutor,
        ITranslationService translationService,
        ApplicationDbContext dbContext,
        ILogger<ChatHub> logger)
    {
        _sessionService = sessionService;
        _stepExecutor = stepExecutor;
        _translationService = translationService;
        _dbContext = dbContext;
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
    /// Sends a chat message and routes to workflow engine if active workflow exists.
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

        // Route to workflow engine if active workflow exists
        var workflowInstanceId = session.WorkflowState?.ActiveWorkflowInstanceId;
        if (workflowInstanceId.HasValue)
        {
            await ExecuteWorkflowStepAsync(workflowInstanceId.Value, message);
        }
        else
        {
            // No active workflow - send acknowledgment that message was received
            await Clients.Caller.SendAsync("ReceiveMessage", new
            {
                Role = "system",
                Content = "Message received. No active workflow - start a workflow to begin.",
                Timestamp = DateTime.UtcNow
            });
        }
    }

    private async Task ExecuteWorkflowStepAsync(Guid workflowInstanceId, string userInput)
    {
        try
        {
            var result = await _stepExecutor.ExecuteStepAsync(workflowInstanceId, userInput);
            
            // Get user's persona type for translation
            var userId = GetUserIdFromClaims();
            var user = await _dbContext.Users.FindAsync(userId);
            var personaType = user?.PersonaType ?? Data.Entities.PersonaType.Hybrid;
            
            if (result.Success)
            {
                // Translate content based on persona
                var content = $"Step '{result.StepName}' completed successfully.";
                var translatedContent = await _translationService.TranslateToBusinessLanguageAsync(content, personaType);
                
                await Clients.Caller.SendAsync("ReceiveMessage", new
                {
                    Role = "agent",
                    Content = translatedContent,
                    OriginalContent = content, // Keep original for "Show Technical Details" feature
                    StepId = result.StepId,
                    NextStep = result.NextStep,
                    WorkflowStatus = result.NewWorkflowStatus?.ToString(),
                    Timestamp = DateTime.UtcNow
                });
                
                _logger.LogInformation(
                    "Workflow step {StepId} executed successfully for instance {InstanceId}", 
                    result.StepId, workflowInstanceId);
            }
            else
            {
                // Translate error messages for business users
                var errorContent = $"Step execution failed: {result.ErrorMessage}";
                var translatedError = await _translationService.TranslateToBusinessLanguageAsync(errorContent, personaType);
                
                await Clients.Caller.SendAsync("ReceiveMessage", new
                {
                    Role = "system",
                    Content = translatedError,
                    OriginalContent = errorContent,
                    StepId = result.StepId,
                    WorkflowStatus = result.NewWorkflowStatus?.ToString(),
                    Timestamp = DateTime.UtcNow
                });
                
                _logger.LogWarning(
                    "Workflow step {StepId} failed for instance {InstanceId}: {Error}", 
                    result.StepId, workflowInstanceId, result.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing workflow step for instance {InstanceId}", workflowInstanceId);
            
            await Clients.Caller.SendAsync("ReceiveMessage", new
            {
                Role = "system",
                Content = "An error occurred while processing your message. Please try again.",
                Timestamp = DateTime.UtcNow
            });
        }
    }
}
