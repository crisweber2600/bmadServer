namespace bmadServer.ApiService.Data.Entities.SparkCompat;

public class SparkCompatDecision
{
    public string Id { get; set; } = string.Empty;
    public string ChatId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string ValueJson { get; set; } = "{}";
    public string Status { get; set; } = "open";
    public bool IsLocked { get; set; }
    public Guid? LockedBy { get; set; }
    public DateTime? LockedAt { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public int CurrentVersion { get; set; } = 1;

    public ICollection<SparkCompatDecisionVersion> Versions { get; set; } = new List<SparkCompatDecisionVersion>();
    public ICollection<SparkCompatDecisionConflict> Conflicts { get; set; } = new List<SparkCompatDecisionConflict>();
}