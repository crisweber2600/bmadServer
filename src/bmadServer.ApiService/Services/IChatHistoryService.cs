using bmadServer.ApiService.Models;

namespace bmadServer.ApiService.Services;

/// <summary>
/// Service interface for managing chat history and pagination.
/// </summary>
public interface IChatHistoryService
{
    /// <summary>
    /// Gets paginated chat history for a session.
    /// </summary>
    /// <param name="userId">User ID for authorization</param>
    /// <param name="sessionId">Session ID</param>
    /// <param name="pageSize">Number of messages per page (default 50)</param>
    /// <param name="offset">Offset for pagination (default 0)</param>
    /// <returns>Paginated chat history</returns>
    Task<ChatHistoryResponse> GetChatHistoryAsync(
        Guid userId, 
        Guid sessionId, 
        int pageSize = 50, 
        int offset = 0);
}
