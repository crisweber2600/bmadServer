using System.Text.Json;

namespace bmadServer.ApiService.Models.Workflows;

public class WorkflowCheckpoint
{
    public Guid Id { get; set; }
    public Guid WorkflowId { get; set; }
    public string StepId { get; set; } = string.Empty;
    public CheckpointType CheckpointType { get; set; }
    public JsonDocument StateSnapshot { get; set; } = null!;
    public long Version { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid TriggeredBy { get; set; }
    public JsonDocument? Metadata { get; set; }
    
    // Navigation properties
    public WorkflowInstance Workflow { get; set; } = null!;
    public Data.Entities.User TriggeredByUser { get; set; } = null!;
}

public enum CheckpointType
{
    StepCompletion,       // Automatic: when workflow step completes
    DecisionConfirmation, // Automatic: when decision is confirmed
    AgentHandoff,         // Automatic: when agent hands off to another agent
    ExplicitSave          // Manual: user-initiated checkpoint
}
