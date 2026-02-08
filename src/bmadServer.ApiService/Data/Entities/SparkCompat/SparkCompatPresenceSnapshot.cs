namespace bmadServer.ApiService.Data.Entities.SparkCompat;

public class SparkCompatPresenceSnapshot
{
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string AvatarUrl { get; set; } = string.Empty;
    public string Status { get; set; } = "online";
    public string Domain { get; set; } = "chat";
    public string? ActiveChatId { get; set; }
    public DateTime LastSeenAt { get; set; } = DateTime.UtcNow;
    public bool IsTyping { get; set; }
    public string? TypingChatId { get; set; }
    public string? CursorPositionJson { get; set; }
}