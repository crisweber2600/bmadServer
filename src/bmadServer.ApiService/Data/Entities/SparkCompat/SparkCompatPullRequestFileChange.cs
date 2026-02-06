namespace bmadServer.ApiService.Data.Entities.SparkCompat;

public class SparkCompatPullRequestFileChange
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string PullRequestId { get; set; } = string.Empty;
    public SparkCompatPullRequest PullRequest { get; set; } = null!;
    public string Path { get; set; } = string.Empty;
    public string AdditionsJson { get; set; } = "[]";
    public string DeletionsJson { get; set; } = "[]";
    public string Status { get; set; } = "staged";
}