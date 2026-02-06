namespace bmadServer.ApiService.Data.Entities.SparkCompat;

public class SparkCompatCollaborationEvent
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string? ChatId { get; set; }
    public string? PrId { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string? MetadataJson { get; set; }
    public string? WorkflowMetadataJson { get; set; }
    public string? DecisionMetadataJson { get; set; }
}