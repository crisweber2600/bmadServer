namespace bmadServer.ApiService.Data.Entities.SparkCompat;

public class SparkCompatPullRequestComment
{
    public string Id { get; set; } = string.Empty;
    public string PullRequestId { get; set; } = string.Empty;
    public SparkCompatPullRequest PullRequest { get; set; } = null!;
    public Guid AuthorUserId { get; set; }
    public string AuthorName { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}