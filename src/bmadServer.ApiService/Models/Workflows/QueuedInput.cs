using System.Text.Json;

namespace bmadServer.ApiService.Models.Workflows;

public class QueuedInput
{
    public Guid Id { get; set; }
    public Guid WorkflowId { get; set; }
    public Guid UserId { get; set; }
    public string InputType { get; set; } = string.Empty;
    public JsonDocument Content { get; set; } = null!;
    public DateTime QueuedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedAt { get; set; }
    public InputStatus Status { get; set; } = InputStatus.Queued;
    public string? RejectionReason { get; set; }
    public long SequenceNumber { get; set; }
    
    // Navigation properties
    public WorkflowInstance Workflow { get; set; } = null!;
    public Data.Entities.User User { get; set; } = null!;
}

public enum InputStatus
{
    Queued,    // Waiting to be processed
    Processed, // Successfully applied
    Rejected,  // Failed validation
    Failed     // Processing error
}
