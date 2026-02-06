namespace bmadServer.ApiService.Data.Entities.SparkCompat;

public class SparkCompatDecisionConflict
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string DecisionId { get; set; } = string.Empty;
    public SparkCompatDecision Decision { get; set; } = null!;
    public string ConflictType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = "open";
    public DateTime DetectedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ResolvedAt { get; set; }
    public Guid? ResolvedBy { get; set; }
    public string? ResolutionJson { get; set; }
    public string? AuditMetadataJson { get; set; }
}