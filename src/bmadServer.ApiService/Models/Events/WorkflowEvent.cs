namespace bmadServer.ApiService.Models.Events;

public class WorkflowEvent
{
    public string EventType { get; set; } = string.Empty;
    public Guid WorkflowId { get; set; }
    public Guid UserId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public object? Data { get; set; }
}
