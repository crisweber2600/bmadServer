namespace bmadServer.ApiService.Data.Entities.SparkCompat;

public class SparkCompatPullRequest
{
    public string Id { get; set; } = string.Empty;
    public string? ChatId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Guid AuthorUserId { get; set; }
    public string AuthorName { get; set; } = string.Empty;
    public string Status { get; set; } = "open";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public string ApprovalsJson { get; set; } = "[]";

    public ICollection<SparkCompatPullRequestFileChange> FileChanges { get; set; } = new List<SparkCompatPullRequestFileChange>();
    public ICollection<SparkCompatPullRequestComment> Comments { get; set; } = new List<SparkCompatPullRequestComment>();
    public ICollection<SparkCompatLineComment> LineComments { get; set; } = new List<SparkCompatLineComment>();
}