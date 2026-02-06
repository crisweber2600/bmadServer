namespace bmadServer.ApiService.Data.Entities.SparkCompat;

public class SparkCompatLineCommentReaction
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string LineCommentId { get; set; } = string.Empty;
    public SparkCompatLineComment LineComment { get; set; } = null!;
    public string Emoji { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
}