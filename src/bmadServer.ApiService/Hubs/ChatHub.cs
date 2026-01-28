using bmadServer.ApiService.Models.Events;
using bmadServer.ApiService.Services;
using bmadServer.ApiService.Services.Workflows;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace bmadServer.ApiService.Hubs;

/// <summary>
/// SignalR hub for real-time chat communication.
/// Manages session lifecycle: connection, disconnection, and recovery.
/// Routes chat messages to workflow engine when an active workflow exists.
/// Applies persona-based translation to responses (Epic 8).
/// </summary>
[Authorize]
public class ChatHub : Hub
{
    private readonly ISessionService _sessionService;
    private readonly IStepExecutor _stepExecutor;
    private readonly ILogger<ChatHub> _logger;
    private readonly IParticipantService _participantService;
    private readonly IPresenceTrackingService _presenceService;
    private readonly IUpdateBatchingService _batchingService;
    private readonly ITranslationService _translationService;

    public ChatHub(
        ISessionService sessionService, 
        IStepExecutor stepExecutor,
        ILogger<ChatHub> logger,
        IParticipantService participantService,
        IPresenceTrackingService presenceService,
        IUpdateBatchingService batchingService,
        ITranslationService translationService)
    {
        _sessionService = sessionService;
        _stepExecutor = stepExecutor;
        _logger = logger;
        _participantService = participantService;
        _presenceService = presenceService;
        _batchingService = batchingService;
        _translationService = translationService;
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
        // Input validation
        if (string.IsNullOrWhiteSpace(message))
        {
            throw new HubException("Message cannot be empty");
        }
        if (message.Length > 10000)
        {
            throw new HubException("Message exceeds maximum length of 10000 characters");
        }

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
            var userId = GetUserIdFromClaims();
            var session = await _sessionService.GetActiveSessionAsync(userId, Context.ConnectionId);
            
            // Get effective persona for translation (Story 8.4 - session override or user default)
            var effectivePersona = session != null 
                ? await _sessionService.GetEffectivePersonaAsync(session.Id, userId)
                : Data.Entities.PersonaType.Hybrid;
            
            var result = await _stepExecutor.ExecuteStepAsync(workflowInstanceId, userInput);
            
            if (result.Success)
            {
                var content = $"Step '{result.StepName}' completed successfully.";
                
                // Apply persona-based translation (Story 8.2, 8.3, 8.5)
                var translationResult = await _translationService.TranslateToBusinessLanguageAsync(
                    content, effectivePersona, result.StepName);
                
                await Clients.Caller.SendAsync("ReceiveMessage", new
                {
                    Role = "agent",
                    Content = translationResult.Content,
                    OriginalContent = translationResult.WasTranslated ? translationResult.OriginalContent : null,
                    WasTranslated = translationResult.WasTranslated,
                    PersonaType = effectivePersona.ToString(),
                    StepId = result.StepId,
                    NextStep = result.NextStep,
                    WorkflowStatus = result.NewWorkflowStatus?.ToString(),
                    Timestamp = DateTime.UtcNow
                });
                
                _logger.LogInformation(
                    "Workflow step {StepId} executed successfully for instance {InstanceId} (Persona: {Persona}, Translated: {Translated})", 
                    result.StepId, workflowInstanceId, effectivePersona, translationResult.WasTranslated);
            }
            else
            {
                var errorContent = $"Step execution failed: {result.ErrorMessage}";
                
                // Apply translation to error messages too
                var translationResult = await _translationService.TranslateToBusinessLanguageAsync(
                    errorContent, effectivePersona, result.StepName);
                
                await Clients.Caller.SendAsync("ReceiveMessage", new
                {
                    Role = "system",
                    Content = translationResult.Content,
                    WasTranslated = translationResult.WasTranslated,
                    PersonaType = effectivePersona.ToString(),
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

    /// <summary>
    /// Join a workflow to receive real-time updates and enable presence tracking
    /// </summary>
    public async Task JoinWorkflow(Guid workflowId)
    {
        var userId = GetUserIdFromClaims();

        // Verify user is a participant or owner
        var isParticipant = await _participantService.IsParticipantAsync(workflowId, userId);
        var isOwner = await _participantService.IsWorkflowOwnerAsync(workflowId, userId);

        if (!isParticipant && !isOwner)
        {
            throw new HubException("Not authorized to join this workflow");
        }

        // Add to SignalR group
        await Groups.AddToGroupAsync(Context.ConnectionId, $"workflow-{workflowId}");

        // Track presence
        await _presenceService.TrackUserOnlineAsync(userId, workflowId, Context.ConnectionId);

        // Broadcast USER_ONLINE event
        var evt = new WorkflowEvent
        {
            EventType = "USER_ONLINE",
            WorkflowId = workflowId,
            UserId = userId,
            DisplayName = Context.User?.Identity?.Name ?? "Unknown User",
            Timestamp = DateTime.UtcNow,
            Data = new PresenceEvent { IsOnline = true, LastSeen = DateTime.UtcNow }
        };
        _batchingService.QueueUpdate(workflowId, evt);

        _logger.LogInformation("User {UserId} joined workflow {WorkflowId}", userId, workflowId);
    }

    /// <summary>
    /// Leave a workflow group
    /// </summary>
    public async Task LeaveWorkflow(Guid workflowId)
    {
        var userId = GetUserIdFromClaims();

        // Remove from SignalR group
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"workflow-{workflowId}");

        // Track offline
        await _presenceService.TrackUserOfflineAsync(userId, workflowId);

        // Broadcast USER_OFFLINE event
        var evt = new WorkflowEvent
        {
            EventType = "USER_OFFLINE",
            WorkflowId = workflowId,
            UserId = userId,
            DisplayName = Context.User?.Identity?.Name ?? "Unknown User",
            Timestamp = DateTime.UtcNow,
            Data = new PresenceEvent { IsOnline = false, LastSeen = DateTime.UtcNow }
        };
        _batchingService.QueueUpdate(workflowId, evt);

        _logger.LogInformation("User {UserId} left workflow {WorkflowId}", userId, workflowId);
    }

    /// <summary>
    /// Send typing indicator to other participants
    /// </summary>
    public async Task SendTypingIndicator(Guid workflowId)
    {
        var userId = GetUserIdFromClaims();

        // Authorization check: verify user is participant or owner
        var isParticipant = await _participantService.IsParticipantAsync(workflowId, userId);
        var isOwner = await _participantService.IsWorkflowOwnerAsync(workflowId, userId);
        if (!isParticipant && !isOwner)
        {
            throw new HubException("Not authorized to send typing indicator for this workflow");
        }

        // Broadcast typing indicator to others in the workflow group
        await Clients.OthersInGroup($"workflow-{workflowId}").SendAsync("USER_TYPING", new
        {
            eventType = "USER_TYPING",
            userId,
            userName = Context.User?.Identity?.Name ?? "Unknown User",
            timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Broadcast message to workflow participants
    /// </summary>
    public async Task BroadcastMessageToWorkflow(Guid workflowId, string message)
    {
        var userId = GetUserIdFromClaims();
        
        // Authorization check: verify user is participant or owner
        var isParticipant = await _participantService.IsParticipantAsync(workflowId, userId);
        var isOwner = await _participantService.IsWorkflowOwnerAsync(workflowId, userId);
        if (!isParticipant && !isOwner)
        {
            throw new HubException("Not authorized to broadcast to this workflow");
        }

        // Input validation
        if (string.IsNullOrWhiteSpace(message))
        {
            throw new HubException("Message cannot be empty");
        }
        if (message.Length > 10000)
        {
            throw new HubException("Message exceeds maximum length of 10000 characters");
        }

        var displayName = Context.User?.Identity?.Name ?? "Unknown User";

        var evt = new WorkflowEvent
        {
            EventType = "MESSAGE_RECEIVED",
            WorkflowId = workflowId,
            UserId = userId,
            DisplayName = displayName,
            Timestamp = DateTime.UtcNow,
            Data = new MessageReceivedEvent 
            { 
                Message = message, 
                MessageId = Guid.NewGuid() 
            }
        };

        await Clients.Group($"workflow-{workflowId}").SendAsync("MESSAGE_RECEIVED", evt);
    }

    /// <summary>
    /// Broadcast decision event to workflow participants
    /// </summary>
    public async Task BroadcastDecision(Guid workflowId, string decision, List<string>? alternatives = null, double? confidence = null)
    {
        var userId = GetUserIdFromClaims();
        
        // Authorization check
        var isParticipant = await _participantService.IsParticipantAsync(workflowId, userId);
        var isOwner = await _participantService.IsWorkflowOwnerAsync(workflowId, userId);
        if (!isParticipant && !isOwner)
        {
            throw new HubException("Not authorized to broadcast decisions to this workflow");
        }

        // Input validation
        if (string.IsNullOrWhiteSpace(decision))
        {
            throw new HubException("Decision cannot be empty");
        }
        if (confidence.HasValue && (confidence < 0 || confidence > 1))
        {
            throw new HubException("Confidence must be between 0 and 1");
        }

        var displayName = Context.User?.Identity?.Name ?? "Unknown User";

        var evt = new WorkflowEvent
        {
            EventType = "DECISION_MADE",
            WorkflowId = workflowId,
            UserId = userId,
            DisplayName = displayName,
            Timestamp = DateTime.UtcNow,
            Data = new DecisionMadeEvent 
            { 
                Decision = decision,
                Alternatives = alternatives,
                Confidence = confidence
            }
        };

        _batchingService.QueueUpdate(workflowId, evt);
    }

    /// <summary>
    /// Broadcast step change event to workflow participants
    /// </summary>
    public async Task BroadcastStepChange(Guid workflowId, string stepId, string stepName, string status)
    {
        var userId = GetUserIdFromClaims();
        
        // Authorization check
        var isParticipant = await _participantService.IsParticipantAsync(workflowId, userId);
        var isOwner = await _participantService.IsWorkflowOwnerAsync(workflowId, userId);
        if (!isParticipant && !isOwner)
        {
            throw new HubException("Not authorized to broadcast step changes to this workflow");
        }

        // Input validation
        if (string.IsNullOrWhiteSpace(stepId) || string.IsNullOrWhiteSpace(stepName))
        {
            throw new HubException("StepId and StepName are required");
        }

        var displayName = Context.User?.Identity?.Name ?? "Unknown User";

        var evt = new WorkflowEvent
        {
            EventType = "STEP_CHANGED",
            WorkflowId = workflowId,
            UserId = userId,
            DisplayName = displayName,
            Timestamp = DateTime.UtcNow,
            Data = new StepChangedEvent 
            { 
                StepId = stepId,
                StepName = stepName,
                Status = status
            }
        };

        _batchingService.QueueUpdate(workflowId, evt);
    }

    /// <summary>
    /// Broadcast conflict detected event
    /// </summary>
    public async Task BroadcastConflict(Guid workflowId, Guid conflictId, string fieldName, List<string> conflictingValues)
    {
        var userId = GetUserIdFromClaims();
        
        // Authorization check
        var isParticipant = await _participantService.IsParticipantAsync(workflowId, userId);
        var isOwner = await _participantService.IsWorkflowOwnerAsync(workflowId, userId);
        if (!isParticipant && !isOwner)
        {
            throw new HubException("Not authorized to broadcast conflicts to this workflow");
        }

        // Input validation
        if (string.IsNullOrWhiteSpace(fieldName))
        {
            throw new HubException("FieldName is required");
        }
        if (conflictingValues == null || conflictingValues.Count < 2)
        {
            throw new HubException("At least 2 conflicting values are required");
        }

        var displayName = Context.User?.Identity?.Name ?? "Unknown User";

        var evt = new WorkflowEvent
        {
            EventType = "CONFLICT_DETECTED",
            WorkflowId = workflowId,
            UserId = userId,
            DisplayName = displayName,
            Timestamp = DateTime.UtcNow,
            Data = new ConflictEvent
            {
                ConflictId = conflictId,
                FieldName = fieldName,
                ConflictingValues = conflictingValues
            }
        };

        await Clients.Group($"workflow-{workflowId}").SendAsync("CONFLICT_DETECTED", evt);
    }
}
