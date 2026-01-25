namespace bmadServer.ApiService.Models;

/// <summary>
/// Response model for paginated chat history.
/// </summary>
public class ChatHistoryResponse
{
    public List<ChatMessage> Messages { get; set; } = new();
    public int TotalCount { get; set; }
    public bool HasMore { get; set; }
    public int Offset { get; set; }
    public int PageSize { get; set; }
}
