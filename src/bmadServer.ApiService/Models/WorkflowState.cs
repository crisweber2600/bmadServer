namespace bmadServer.ApiService.Models;

/// <summary>
/// Represents the workflow state stored in the Session's WorkflowState JSONB column.
/// Includes conversation history, decision locks, and concurrency control fields.
/// </summary>
public class WorkflowState
{
    public string? WorkflowName { get; set; }
    public int CurrentStep { get; set; }
    public List<ChatMessage> ConversationHistory { get; set; } = new();
    public Dictionary<string, bool> DecisionLocks { get; set; } = new();
    public string? PendingInput { get; set; }
    public string? AgentContext { get; set; }
    
    // Concurrency control fields per architecture.md
    public int _version { get; set; } = 1;
    public Guid _lastModifiedBy { get; set; }
    public DateTime _lastModifiedAt { get; set; }
}

/// <summary>
/// Represents a chat message in the conversation history.
/// Limited to last 10 messages per session as per AC.
/// </summary>
public class ChatMessage
{
    public string Id { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty; // "user" or "agent"
    public string Content { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string? AgentId { get; set; }
}
