using System.Text.Json;

namespace bmadServer.ApiService.Models.Workflows;

public class WorkflowStepHistory
{
    public Guid Id { get; set; }
    public Guid WorkflowInstanceId { get; set; }
    public required string StepId { get; set; }
    public required string StepName { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public StepExecutionStatus Status { get; set; }
    public JsonDocument? Input { get; set; }
    public JsonDocument? Output { get; set; }
    public string? ErrorMessage { get; set; }
    
    public WorkflowInstance? WorkflowInstance { get; set; }
}

public enum StepExecutionStatus
{
    Running,
    Completed,
    Failed,
    Skipped
}
