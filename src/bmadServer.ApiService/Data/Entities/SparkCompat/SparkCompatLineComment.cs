namespace bmadServer.ApiService.Data.Entities.SparkCompat;

public class SparkCompatLineComment
{
    public string Id { get; set; } = string.Empty;
    public string PullRequestId { get; set; } = string.Empty;
    public SparkCompatPullRequest PullRequest { get; set; } = null!;
    public string FileId { get; set; } = string.Empty;
    public string? ParentId { get; set; }
    public int LineNumber { get; set; }
    public string LineType { get; set; } = "unchanged";
    public Guid AuthorUserId { get; set; }
    public string AuthorName { get; set; } = string.Empty;
    public string AuthorAvatar { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public bool Resolved { get; set; }

    public ICollection<SparkCompatLineCommentReaction> Reactions { get; set; } = new List<SparkCompatLineCommentReaction>();
}