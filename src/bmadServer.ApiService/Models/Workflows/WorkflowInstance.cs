using System.Text.Json;

namespace bmadServer.ApiService.Models.Workflows;

public class WorkflowInstance
{
    public Guid Id { get; set; }
    public required string WorkflowDefinitionId { get; set; }
    public Guid UserId { get; set; }
    public int CurrentStep { get; set; }
    public WorkflowStatus Status { get; set; }
    public JsonDocument? StepData { get; set; }
    public JsonDocument? Context { get; set; }
    public JsonDocument? SharedContextJson { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? PausedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
}
