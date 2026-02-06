namespace bmadServer.ApiService.Data.Entities.SparkCompat;

public class SparkCompatMessage
{
    public string Id { get; set; } = string.Empty;
    public string ChatId { get; set; } = string.Empty;
    public SparkCompatChat Chat { get; set; } = null!;
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public Guid? UserId { get; set; }
    public string? FileChangesJson { get; set; }
    public string? WorkflowContextJson { get; set; }
    public string? AttributionJson { get; set; }
    public string? PersonaMetadataJson { get; set; }
}