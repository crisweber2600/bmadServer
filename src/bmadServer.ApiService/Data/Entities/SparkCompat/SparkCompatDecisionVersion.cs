namespace bmadServer.ApiService.Data.Entities.SparkCompat;

public class SparkCompatDecisionVersion
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string DecisionId { get; set; } = string.Empty;
    public SparkCompatDecision Decision { get; set; } = null!;
    public int VersionNumber { get; set; }
    public string ValueJson { get; set; } = "{}";
    public Guid ChangedBy { get; set; }
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
    public string? Reason { get; set; }
    public string? AuditMetadataJson { get; set; }
}