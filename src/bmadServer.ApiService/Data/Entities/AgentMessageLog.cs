using System.Text.Json;

namespace bmadServer.ApiService.Data.Entities;

/// <summary>
/// Entity for persisting agent message logs in the database
/// </summary>
public class AgentMessageLog
{
    public Guid Id { get; set; }

    public Guid MessageId { get; set; }

    public DateTime Timestamp { get; set; }

    public string SourceAgent { get; set; } = string.Empty;

    public string TargetAgent { get; set; } = string.Empty;

    public int MessageType { get; set; }

    public JsonDocument Content { get; set; } = JsonDocument.Parse("{}");

    public Guid WorkflowInstanceId { get; set; }

    public string? CorrelationId { get; set; }

    public Models.Workflows.WorkflowInstance? WorkflowInstance { get; set; }
}
