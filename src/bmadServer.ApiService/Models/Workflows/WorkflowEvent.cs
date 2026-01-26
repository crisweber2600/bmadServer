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
    
    // Enhanced attribution fields (Story 7.3)
    public string? DisplayName { get; set; }
    public JsonDocument? Payload { get; set; }
    public string? InputType { get; set; }
    public JsonDocument? AlternativesConsidered { get; set; }
    
    public WorkflowInstance? WorkflowInstance { get; set; }
}
