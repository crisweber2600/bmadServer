namespace bmadServer.ApiService.Data.Entities.SparkCompat;

public class SparkCompatChat
{
    public string Id { get; set; } = string.Empty;
    public Guid CreatedByUserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Domain { get; set; }
    public string? Service { get; set; }
    public string? Feature { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<SparkCompatMessage> Messages { get; set; } = new List<SparkCompatMessage>();
}