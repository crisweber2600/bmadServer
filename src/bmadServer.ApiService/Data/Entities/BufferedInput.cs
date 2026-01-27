namespace bmadServer.ApiService.Data.Entities;

public class BufferedInput
{
    public Guid Id { get; set; }
    public Guid WorkflowInstanceId { get; set; }
    public Guid UserId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string FieldName { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public bool IsApplied { get; set; }
    public Guid? ConflictId { get; set; }

    public Workflow? WorkflowInstance { get; set; }
    public Conflict? Conflict { get; set; }
}
