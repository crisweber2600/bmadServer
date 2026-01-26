using System.Text.Json;

namespace bmadServer.ApiService.Models.Workflows;

public class WorkflowEvent
{
    public Guid Id { get; set; }
    public Guid WorkflowInstanceId { get; set; }
    public required string EventType { get; set; }
    public WorkflowStatus? OldStatus { get; set; }
    public WorkflowStatus? NewStatus { get; set; }
    public DateTime Timestamp { get; set; }
    public Guid UserId { get; set; }
    
    /// <summary>
    /// User's display name at the time of the event (Story 7.3)
    /// </summary>
    public string? DisplayName { get; set; }
    
    /// <summary>
    /// Full event details stored as JSONB (e.g., decision text, confidence, rationale) (Story 7.3)
    /// </summary>
    public JsonDocument? Payload { get; set; }
    
    /// <summary>
    /// Type of input: Message, Decision, Checkpoint, etc. (Story 7.3)
    /// </summary>
    public string? InputType { get; set; }
    
    /// <summary>
    /// For decisions, tracks alternatives that were considered but not chosen (Story 7.3)
    /// </summary>
    public JsonDocument? AlternativesConsidered { get; set; }
    
    public WorkflowInstance? WorkflowInstance { get; set; }
}
